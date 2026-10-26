#!/usr/bin/env python3
"""Offline file/data validation; does NOT compile C#, import Unity assets or simulate PhysX.
Usage: python Tools/validate_static.py [--root PROJECT] [--report OUTPUT.json]
Requires PyYAML, numpy, Pillow and Pygments. The Unity Editor validators require none of these.
"""
from __future__ import annotations
import argparse, collections, copy, csv, hashlib, json, math, re, shutil, struct, sys, wave
from datetime import datetime, timezone
from pathlib import Path
import numpy as np
import yaml
from PIL import Image
from pygments import lex
from pygments.lexers import CSharpLexer
from pygments.token import Comment, String, Error


def unity_docs(path: Path) -> dict[int, tuple[str, dict]]:
    text=path.read_text(encoding='utf-8-sig')
    if not text.startswith('%YAML'):return {}
    chunks=re.split(r'^--- !u!(\d+) &(\-?\d+)[^\n]*\n',text,flags=re.M)
    result={}
    for i in range(1,len(chunks),3):
        fid=int(chunks[i+1]);obj=yaml.safe_load(chunks[i+2]);name=next(iter(obj))
        if fid in result:raise ValueError(f'{path}: duplicate local fileID {fid}')
        result[fid]=(name,obj[name])
    return result

def walk(value):
    if isinstance(value,dict):
        yield value
        for child in value.values():yield from walk(child)
    elif isinstance(value,list):
        for child in value:yield from walk(child)

def dist(a,b):return math.hypot(a[0]-b[0],a[1]-b[1])
def v2(v):return (v['x'],v['y'])
def finite(*vs):return all(math.isfinite(x) for x in vs)

def mission_matches(m,d):
    kind=m['kind']
    if kind==0:return m['target'].get('guid')==d['_guid']
    if kind==1:return m['category']==d['objectType']
    if kind==2:return m['color']!=0 and m['color']==d.get('colorVariant',0) and m['category']==d['objectType']
    if kind==3:return m['tier']==d['sizeTier']
    if kind==4:return d['objectType']==7
    return False

def radius_at(x,tiers):
    if x<=tiers[0]['growthRequired']:return tiers[0]['radius']
    for a,b in zip(tiers,tiers[1:]):
        if x<b['growthRequired']:
            t=(x-a['growthRequired'])/(b['growthRequired']-a['growthRequired']);t=t*t*(3-2*t)
            return a['radius']+(b['radius']-a['radius'])*t
    return tiers[-1]['radius']
def tier_at(x,tiers):return sum(x>=t['growthRequired'] for t in tiers)

def validate_level(l,edibles,tiers,settings):
    errors=[];ps=l.get('placements',[]);missions=l.get('missions',[])
    if l.get('levelId',0)<1:errors.append('invalid id')
    if not missions or len(missions)>3:errors.append('invalid mission array')
    if not ps:errors.append('empty placements')
    if l.get('defaultWrongBehavior') not in [0,1,2]:errors.append('invalid wrong-answer rule')
    if not finite(l.get('timeLimit',-1)) or not 0<=l.get('timeLimit',-1)<=3600:errors.append('invalid time')
    if not finite(l.get('growthMultiplier',0)) or l.get('growthMultiplier',0)<=0:errors.append('invalid growth multiplier')
    if not 1<=l.get('initialSizeTier',0)<=len(tiers):errors.append('invalid start tier')
    sizes=v2(l['arenaSize']);half=[v*.5 for v in sizes];start=v2(l['holeStart']);clear=l['startClearRadius']
    if not finite(*sizes,*start,clear) or min(sizes)<8 or clear<0:errors.append('invalid bounds/start')
    counts=collections.Counter();objects=[]
    for i,p in enumerate(ps):
        d=edibles.get(p.get('edible',{}).get('guid'));pos=v2(p['position'])
        if d is None:errors.append(f'missing edible {i}');continue
        objects.append(d);r=d['footprintRadius'];counts[d['_guid']]+=1
        if not finite(*pos,p['yaw'],r,d['growthValue']) or r<=0 or d['growthValue']<0:errors.append(f'invalid object numbers {i}')
        if not 1<=d['sizeTier']<=len(tiers):errors.append(f'invalid object tier {i}')
        if not d['prefab'].get('fileID'):errors.append(f'missing prefab {i}')
        if any(abs(pos[k])+r>half[k]+1e-5 for k in range(2)):errors.append(f'outside arena {i}')
        if dist(pos,start)<clear+r-1e-3:errors.append(f'start obstructed {i}')
        # Full-grown hole cannot leave the floor; require every placement to remain reachable.
        margin=tiers[-1]['radius']*(1+settings['growthPunch'])+.12
        nearest=[max(-max(0,h-margin),min(pos[k],max(0,h-margin))) for k,h in enumerate(half)]
        if dist(pos,nearest)>tiers[-1]['radius']*settings['attractRadiusFactor']+1e-4:errors.append(f'unreachable corner {i}')
        for j,q in enumerate(ps[:i]):
            other=edibles.get(q['edible'].get('guid'))
            if other and dist(pos,v2(q['position']))<r+other['footprintRadius']+.08-1e-5:errors.append(f'overlap {j},{i}')
    recipe=collections.Counter()
    for group in l.get('spawnGroups',[]):
        if group.get('count',0)<=0 or group['edible'].get('guid') not in edibles:errors.append('invalid recipe entry')
        recipe[group['edible'].get('guid')]+=group['count']
    if recipe!=counts:errors.append('recipe/placement counts differ')
    mission_ids=set()
    for m in missions:
        if not m.get('missionId') or m['missionId'] in mission_ids:errors.append('duplicate/empty mission id')
        mission_ids.add(m.get('missionId'))
        if m['requiredCount']<=0 or m['kind'] not in range(5):errors.append('invalid mission')
        if m.get('icon',{}).get('fileID',0)==0:errors.append('missing mission icon')
        available=sum(1 for p,d in zip(ps,objects) if not p['decorative'] and d['canBeSwallowed'] and mission_matches(m,d))
        if available<m['requiredCount']:errors.append('insufficient targets')
    result={'level':l.get('levelId'),'placements':len(ps),'missions':len(missions),'time_limit':l['timeLimit'],'rule':{0:'None',1:'Blocked',2:'Penalty'}.get(l['defaultWrongBehavior'],'INVALID'),'growth_path':False}
    if errors:return errors,result
    used=set();progress=[0]*len(missions);xp=tiers[l['initialSizeTier']-1]['growthRequired'];path=[];penalties=0;boosts=0
    # Same low-tier-first constructive witness as the C# validator, including footprint and active-target caps.
    for step in range(len(ps)):
        eligible=[]
        for i,(p,d) in enumerate(zip(ps,objects)):
            if i in used or p['decorative'] or not d['canBeSwallowed']:continue
            if d['sizeTier']>tier_at(xp,tiers) or d['footprintRadius']>radius_at(xp,tiers)*settings['maxObjectToHoleRatio']+1e-3:continue
            active=any(progress[k]<m['requiredCount'] and mission_matches(m,d) for k,m in enumerate(missions))
            rule=d['wrongBehavior'] if d['overrideWrongBehavior'] else l['defaultWrongBehavior']
            if rule==1 and not active:continue
            eligible.append((d['sizeTier'],i))
        if not eligible:break
        _,index=min(eligible);d=objects[index];used.add(index);path.append(index)
        xp=min(tiers[-1]['growthRequired'],xp+d['growthValue']*l['growthMultiplier'])
        target=any(mission_matches(m,d) for m in missions);rule=d['wrongBehavior'] if d['overrideWrongBehavior'] else l['defaultWrongBehavior']
        if not target and rule==2:penalties+=l['penaltySeconds']
        if d['objectType']==7:boosts+=d['specialTimeBonus']
        for k,m in enumerate(missions):
            if mission_matches(m,d):progress[k]=min(m['requiredCount'],progress[k]+1)
        if all(progress[k]==m['requiredCount'] for k,m in enumerate(missions)):
            result.update(growth_path=True,witness_consumptions=len(path),final_tier=tier_at(xp,tiers),final_growth=round(xp,4),witness_placement_indices=path,penalty_seconds_in_witness=penalties,booster_seconds_in_witness=boosts)
            # Descriptive distance only, NOT feasibility certification or physics simulation.
            point=start;length=0
            for j in path:q=v2(ps[j]['position']);length+=dist(point,q);point=q
            result['witness_center_distance']=round(length,2)
            break
    if not result['growth_path']:errors.append('no constructive growth path')
    return errors,result

class Validator:
    def __init__(self,root):self.root=root;self.checks=[];self.guidmap={};self.docs={};self.level_results=[];self.mesh_results=[]
    def check(self,name,fn):
        try:detail=fn();self.checks.append({'name':name,'status':'PASS','details':detail})
        except Exception as exc:self.checks.append({'name':name,'status':'FAIL','details':str(exc)})
    def require(self,condition,msg):
        if not condition:raise ValueError(msg)
    def structure(self):
        required=['Assets/Scenes/Main.unity','Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/ProjectVersion.txt','ProjectSettings/EditorBuildSettings.asset','Assets/Scripts/Core/GameManager.cs','Assets/Editor/HoleItBuilder.cs','Assets/Tests/EditMode/CampaignEditModeTests.cs','Assets/Tests/PlayMode/CampaignPlayModeTests.cs']
        for rel in required:self.require((self.root/rel).is_file(),f'missing {rel}')
        version=(self.root/'ProjectSettings/ProjectVersion.txt').read_text();self.require('6000.3.6f1' in version,version)
        self.require(not any(self.root.glob('Assets/**/*.ttf')) and not any(self.root.glob('Assets/**/*.otf')),'External font files must not be distributed')
        return {'required_files':len(required),'unity':'6000.3.6f1','external_font_files':0}
    def metadata(self):
        assets=self.root/'Assets';errors=[]
        for p in assets.rglob('*'):
            if p.name.endswith('.meta'):continue
            meta=Path(str(p)+'.meta')
            if not meta.is_file():errors.append('missing meta '+str(p.relative_to(self.root)));continue
            m=yaml.safe_load(meta.read_text());g=m.get('guid','')
            if not re.fullmatch('[0-9a-f]{32}',g):errors.append('invalid guid '+str(meta))
            elif g in self.guidmap:errors.append('duplicate guid '+g)
            else:self.guidmap[g]=p
            if p.is_dir() and m.get('folderAsset') not in ['yes',True]:errors.append('folder meta lacks folderAsset yes '+str(p))
        self.require(not errors,'; '.join(errors[:20]));return {'unique_guids':len(self.guidmap)}
    def parse(self):
        for p in self.root.rglob('*'):
            if p.suffix in ['.unity','.prefab','.asset','.mat'] and p.is_file():
                d=unity_docs(p)
                if d:self.docs[p]=d
        return {'native_text_files':len(self.docs),'serialized_objects':sum(map(len,self.docs.values()))}
    def refs(self):
        errors=[];external=local=0
        for path,docs in self.docs.items():
            for fid,(typ,b) in docs.items():
                for v in walk(b):
                    if 'fileID' not in v or v['fileID']==0:continue
                    target=v['fileID'];g=v.get('guid')
                    if g:
                        external+=1;p=self.guidmap.get(g)
                        if p is None:errors.append(f'{path.name}:{fid} unknown GUID {g}');continue
                        if p in self.docs and target not in self.docs[p]:errors.append(f'{path.name}:{fid} invalid external fileID {target} in {p.name}')
                        if p.suffix=='.cs' and target!=11500000:errors.append(f'bad MonoScript fileID {p.name}')
                        if p.suffix=='.png' and target not in [21300000,2800000]:errors.append(f'bad texture/sprite ID {p.name}')
                        if p.suffix=='.wav' and target!=8300000:errors.append(f'bad AudioClip ID {p.name}')
                    elif path.suffix in ['.unity','.prefab']:
                        local+=1
                        if target not in docs:errors.append(f'{path.name}:{fid} orphan local fileID {target}')
        self.require(not errors,'; '.join(errors[:25]));return {'external_references':external,'local_references':local,'missing':0,'note':'File/GUID/fileID closure only; Unity import is NOT RUN.'}
    def hierarchy(self):
        total=0;errors=[]
        for path,docs in self.docs.items():
            if path.suffix not in ['.unity','.prefab']:continue
            gos={i:b for i,(typ,b) in docs.items() if typ=='GameObject'};total+=len(gos)
            for i,b in gos.items():
                comps=[c['component']['fileID'] for c in b['m_Component']]
                self.require(len(comps)==len(set(comps)),f'{path.name} duplicate component list')
                transforms=0
                for cid in comps:
                    typ,c=docs[cid];self.require(c['m_GameObject']['fileID']==i,f'{path.name} component ownership')
                    if typ in ['Transform','RectTransform']:transforms+=1
                self.require(transforms==1,f'{path.name}:{b["m_Name"]} invalid transform count')
            for i,(typ,b) in docs.items():
                if typ not in ['Transform','RectTransform']:continue
                father=b['m_Father']['fileID'];children=[v['fileID'] for v in b['m_Children']]
                self.require(len(children)==len(set(children)),f'duplicate children {path.name}')
                if father:self.require(i in [v['fileID'] for v in docs[father][1]['m_Children']],f'{path.name} missing parent->child')
                for child in children:self.require(docs[child][1]['m_Father']['fileID']==i,f'{path.name} wrong child->parent')
        return {'gameobjects_checked':total,'ownership_and_parent_links':'closed'}
    def scene(self):
        path=self.root/'Assets/Scenes/Main.unity';docs=self.docs[path];scripts=collections.Counter();levels=None;manager=None
        for _,(typ,b) in docs.items():
            if typ!='MonoBehaviour':continue
            script=self.guidmap[b['m_Script']['guid']];scripts[script.stem]+=1
            if script.stem=='LevelManager':levels=b['levels']
            if script.stem=='UIManager':manager=b
            if script.stem in ['GameManager','LevelManager','HoleController','InputManager','UIManager']:
                for key,value in b.items():
                    if key.startswith('m_'):continue
                    for r in walk(value):
                        if 'fileID' in r:self.require(r['fileID']!=0,f'{script.stem}.{key} null')
            # Every authored custom field must exist in its source (inherited uGUI m_ fields excluded).
            text=script.read_text()
            for key in b:
                if key.startswith('m_'):continue
                self.require(re.search(r'\b'+re.escape(key)+r'\b',text),f'{script.name} has no token for serialized {key}')
        for name in ['GameManager','LevelManager','InputManager','MissionManager','HighScoreManager','HoleController','UIManager','HoleItEventSystem','UIInputModuleSelector']:
            self.require(scripts[name]==1,f'expected one {name}, got {scripts[name]}')
        self.require(len(levels)==30 and len({x['guid'] for x in levels})==30,'scene 30-level references')
        self.require(len(manager['levelButtons'])==30,'level selection button count')
        self.require(len(manager['popupTexts'])==8,'popup pool count')
        return {'serialized_levels':30,'level_buttons':30,'popup_pool':8,'gameobjects':sum(typ=='GameObject' for typ,b in docs.values()),'unity_scene_load':'NOT RUN'}
    def packages(self):
        manifest=json.loads((self.root/'Packages/manifest.json').read_text())['dependencies'];lock=json.loads((self.root/'Packages/packages-lock.json').read_text())['dependencies']
        for key,version in manifest.items():self.require(key in lock and lock[key]['version']==version,f'lock mismatch: {key}')
        for key,entry in lock.items():
            for dep,ver in entry.get('dependencies',{}).items():self.require(dep in lock and lock[dep]['version']==ver,f'transitive lock entry mismatch {key}->{dep}')
        self.require('com.unity.textmeshpro' not in manifest,'separate unused TMP package')
        asmdefs={}
        for p in self.root.rglob('*.asmdef'):
            a=json.loads(p.read_text());self.require(a['name'] not in asmdefs,'duplicate assembly name');asmdefs[a['name']]=a
            if '/Editor/' in str(p):self.require(a['includePlatforms']==['Editor'],'Editor assembly platform')
        return {'direct_packages':len(manifest),'lock_entries':len(lock),'asmdefs':len(asmdefs),'upm_resolution':'NOT RUN; hand-authored seed lock'}
    def source(self):
        scripts=list((self.root/'Assets').rglob('*.cs'));defs=collections.defaultdict(list);errors=[]
        for p in scripts:
            source=p.read_text();tokens=list(lex(source,CSharpLexer()));masked=''.join((' '*len(s) if t in Comment or t in String else s) for t,s in tokens)
            stack=[]
            for c in masked:
                if c in '{([':stack.append(c)
                elif c in '})]':
                    if not stack or stack.pop()!={'}':'{',')':'(',']':'['}[c]:errors.append(f'{p.name} mismatched delimiter');break
            if stack:errors.append(f'{p.name} unclosed delimiters {stack[-4:]}')
            depth=0
            for ma in re.finditer(r'\b((?:public\s+|static\s+|sealed\s+|abstract\s+|partial\s+)*)((?:class|enum|struct))\s+(\w+)',masked):
                name=ma[3];level=masked[:ma.start()].count('{')-masked[:ma.start()].count('}')
                if level==0:defs[name].append((p,'partial' in ma[1]))
            if '/Assets/Scripts/' in str(p) and re.search(r'\bUnityEditor\b',masked):errors.append(f'{p.name} runtime Editor dependency')
            if re.search(r'using System\s*;',source) and re.search(r'(?<![.\w])Object\.',masked) and 'using Object = UnityEngine.Object;' not in source:errors.append(f'{p.name} ambiguous Object')
        for name,items in defs.items():
            if len(items)>1 and not all(partial for p,partial in items):errors.append('duplicate top-level type '+name)
        self.require(not errors,'; '.join(errors));return {'csharp_files':len(scripts),'top_level_types':len(defs),'lexical_delimiters':'balanced','semantic_CSharp_compile':'NOT RUN','warning':'Lexical checks are not a C# compiler and do not prove all symbol/API bindings.'}
    def meshes(self):
        for p,docs in self.docs.items():
            for _,(typ,m) in docs.items():
                if typ!='Mesh':continue
                vd=m['m_VertexData'];raw=bytes.fromhex(vd['_typelessdata']);n=vd['m_VertexCount'];self.require(len(raw)==vd['m_DataSize']==n*48,f'{p.name} vertex buffer size')
                floats=np.frombuffer(raw,dtype='<f4').reshape(n,12);self.require(np.isfinite(floats).all(),f'{p.name} NaN/Infinity');verts=floats[:,:3];normal=floats[:,3:6]
                indices=np.frombuffer(bytes.fromhex(m['m_IndexBuffer']),dtype='<u2' if m['m_IndexFormat']==0 else '<u4')
                self.require(len(indices)%3==0 and indices.min()>=0 and indices.max()<n,f'{p.name} index range')
                for sub in m['m_SubMeshes']:
                    self.require(sub['indexCount']<=len(indices) and sub['firstByte']+sub['indexCount']*indices.dtype.itemsize<=indices.nbytes,f'{p.name} submesh range')
                lengths=np.linalg.norm(normal,axis=1);self.require(np.all((lengths>.9)&(lengths<1.1)),f'{p.name} non-unit/zero normals')
                ctr=np.array(list(m['m_LocalAABB']['m_Center'].values()));ext=np.array(list(m['m_LocalAABB']['m_Extent'].values()))
                self.require(np.isfinite(ctr).all() and np.isfinite(ext).all() and (ext>=0).all() and np.linalg.norm(ext)>0,f'{p.name} bounds')
                self.require(np.all(verts.min(0)>=ctr-ext-1e-5) and np.all(verts.max(0)<=ctr+ext+1e-5),f'{p.name} vertices outside bounds')
                self.mesh_results.append({'name':p.stem,'vertices':n,'triangles':len(indices)//3})
        self.require(len(self.mesh_results)==56,'expected 51 models + 5 shared geometry meshes')
        return {'meshes':len(self.mesh_results),'vertices':sum(m['vertices'] for m in self.mesh_results),'triangles':sum(m['triangles'] for m in self.mesh_results),'nonfinite_values':0}
    def media(self):
        pngs=list((self.root/'Assets').rglob('*.png'));wavs=list((self.root/'Assets').rglob('*.wav'));seconds=0
        for p in pngs:
            with Image.open(p) as im:self.require(im.size==(128,128),f'{p.name} unexpected icon size');im.verify()
        for p in wavs:
            with wave.open(str(p),'rb') as w:
                self.require(w.getframerate()==22050 and w.getnchannels()==1 and w.getsampwidth()==2,'invalid PCM');arr=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2');seconds+=len(arr)/w.getframerate()
                self.require(len(arr)>100 and np.max(np.abs(arr.astype(np.int32)))<30000 and np.any(arr!=0),f'{p.name} silence/clipping')
        return {'pngs':len(pngs),'wav_clips':len(wavs),'audio_seconds':round(seconds,2),'audio_audition':'NOT RUN'}
    def ui_fit_math(self):
        results=[]
        for w,h,top,bottom in [(1080,1920,0,0),(1080,2340,90,80),(750,1334,44,34),(1536,2048,24,24),(1080,1800,90,60)]:
            scale=math.sqrt(w/1080*h/1920);available=(w/scale,(h-top-bottom)/scale)
            fit=min(1,available[0]/1080,available[1]/1920);logical=(available[0]/fit,available[1]/fit)
            self.require(logical[0]>=1080-1e-6 and logical[1]>=1920-1e-6,'safe-area logical minimum')
            brand_bottom=logical[1]-380-175;hero_top=logical[1]/2+270+195/2
            self.require(brand_bottom>hero_top,'title heading/icon overlap')
            self.require(logical[0]/2-505>=0,'HUD card clipped horizontally')
            results.append({'screen':[w,h],'safe_top_bottom':[top,bottom],'logical_size':[round(x,2) for x in logical],'fit':round(fit,4)})
        return {'cases':results,'scope':'Scalar layout formula checks only, not Unity render or real-device safe-area testing'}
    def campaign(self):
        edibles={};levels=[];tiers=None;settings=None
        for p,docs in self.docs.items():
            if p.suffix!='.asset':continue
            for _,(typ,b) in docs.items():
                if typ!='MonoBehaviour':continue
                s=self.guidmap[b['m_Script']['guid']].stem
                if s=='EdibleData':d=copy.deepcopy(b);d['_guid']=yaml.safe_load(Path(str(p)+'.meta').read_text())['guid'];edibles[d['_guid']]=d
                elif s=='LevelData':levels.append(b)
                elif s=='HoleSizeTable':tiers=b['tiers']
                elif s=='GameSettings':settings=b
        self.require(len(edibles)==51,'expected 51 edible data assets');self.require(sorted(l['levelId'] for l in levels)==list(range(1,31)),'30 unique level IDs required')
        self.require(tiers[0]['growthRequired']==0 and len(tiers)==6,'growth baseline')
        self.require(all(b['growthRequired']>a['growthRequired'] and b['radius']>=a['radius'] for a,b in zip(tiers,tiers[1:])),'non-monotonic growth')
        for d in edibles.values():
            meshpath=self.root/('Assets/HoleIt/Meshes/Completed_'+d['objectId']+'.asset');mesh=self.docs[meshpath][4300000][1];ext=mesh['m_LocalAABB']['m_Extent'];actual=math.hypot(ext['x'],ext['z'])
            self.require(abs(actual-d['footprintRadius'])<2e-5,f'{d["objectId"]} logical/mesh radius mismatch')
            prefab=self.docs[self.guidmap[d['prefab']['guid']]];goid=d['prefab']['fileID'];trs=[b for _,(typ,b) in prefab.items() if typ=='Transform' and b['m_GameObject']['fileID']==goid]
            self.require(len(trs)==1 and list(trs[0]['m_LocalScale'].values())==[1.,1.,1.],f'{d["objectId"]} prefab root not identity')
        failures=[]
        for l in sorted(levels,key=lambda l:l['levelId']):
            errs,result=validate_level(l,edibles,tiers,settings);self.level_results.append(result)
            if errs:failures.append(f'{l["levelId"]}: '+', '.join(errs))
        self.require(not failures,'; '.join(failures))
        self.levels=levels;self.edibles=edibles;self.tiers=tiers;self.settings=settings
        return {'levels':len(levels),'themes':len({l['theme']['guid'] for l in levels}),'placements':sum(len(l['placements']) for l in levels),'constructive_growth_paths':len(self.level_results),'timed_complete_playthrough':'NOT RUN'}
    def mutation_tests(self):
        # Deliberately corrupt copies, never production assets, and demand concrete detection.
        good=sorted(self.levels,key=lambda l:l['levelId'])[0];cases=[]
        bad=copy.deepcopy(good);bad['missions'][0]['requiredCount']=9999;cases.append(('target_shortage',bad,self.edibles))
        bad=copy.deepcopy(good);bad['placements'][0]['position']={'x':10000,'y':0};cases.append(('outside_arena',bad,self.edibles))
        bad=copy.deepcopy(good);bad['placements'][1]['position']=copy.deepcopy(bad['placements'][0]['position']);cases.append(('overlap',bad,self.edibles))
        bad=copy.deepcopy(good);bad['placements'][0]['position']=copy.deepcopy(bad['holeStart']);cases.append(('blocked_start',bad,self.edibles))
        bad=copy.deepcopy(good);bad['timeLimit']=-5;cases.append(('negative_time',bad,self.edibles))
        bad=copy.deepcopy(good);bad['placements'][0]['edible']={'fileID':11400000,'guid':'0'*32};cases.append(('missing_data',bad,self.edibles))
        bad=copy.deepcopy(good);bad['defaultWrongBehavior']=1;db=copy.deepcopy(self.edibles)
        for d in db.values():d['sizeTier']=6
        cases.append(('growth_deadlock',bad,db))
        results=[]
        for name,bad,db in cases:
            errors,_=validate_level(bad,db,self.tiers,self.settings);self.require(bool(errors),'mutation was not detected: '+name);results.append({'case':name,'detected':errors})
        return results
    def run(self):
        for name,fn in [('project_structure',self.structure),('metadata_guids',self.metadata),('native_yaml_parse',self.parse),('asset_reference_closure',self.refs),('prefab_scene_hierarchy',self.hierarchy),('main_scene_serialization',self.scene),('package_json_and_assemblies',self.packages),('csharp_lexical_checks',self.source),('native_mesh_buffers',self.meshes),('png_wav_integrity',self.media),('safe_area_layout_formula',self.ui_fit_math),('30_level_data_and_growth',self.campaign),('negative_mutation_tests',self.mutation_tests)]:self.check(name,fn)
        return {'generated_utc':datetime.now(timezone.utc).isoformat(),'scope':'OFFLINE STATIC VALIDATION ONLY','root':str(self.root),'status':'PASS' if all(c['status']=='PASS' for c in self.checks) else 'FAIL','checks':self.checks,'level_results':self.level_results,'mesh_results':self.mesh_results,'unity_compile':'BLOCKED: Unity Editor executable not available in authoring container','unity_tests':'NOT RUN','android_build':'BLOCKED: no Unity Editor/Android Build Support in authoring container','performance':'NOT RUN'}

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1]);parser.add_argument('--report',type=Path);args=parser.parse_args()
    v=Validator(args.root.resolve());report=v.run();dest=args.report or args.root/'Documentation/STATIC_VALIDATION.json';dest.parent.mkdir(parents=True,exist_ok=True);dest.write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    for c in report['checks']:print(c['status'],c['name'],str(c['details'])[:200])
    print('Result:',report['status'],'Report:',dest);return 0 if report['status']=='PASS' else 1
if __name__=='__main__':sys.exit(main())
