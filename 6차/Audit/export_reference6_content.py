"""Extend only edition six with locally supplied source content; no downloaded assets."""
from pathlib import Path
import sys,json,gzip,base64,struct,re,ast,collections
W=Path(r'C:\Users\PUVKID\Documents\Codex\2026-10-13\new-chat\work')
ROOT=Path(__file__).resolve().parents[1]
out=ROOT/'Source/Assets/ReferenceContent'
root=W/'reference'
sys.path.insert(0,str(W/'ref-python'))
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler
data=json.loads((out/'Models.json').read_text(encoding='utf-8'))
items={d['item']['id']:d for d in json.loads((root/'all-items.json').read_text(encoding='utf-8'))}
texts={}
for f in root.glob('text*json'):
    d=json.loads(f.read_text(encoding='utf-8'))
    if d.get('m_Name','').startswith('Level_'):texts[d['m_Name'][6:]]=json.loads(d['m_Script'])
campaign=json.loads((root/'list_main_fall_back.txt').read_text(encoding='utf-8'))['level_list_info']
def convert(source,index,config=None):
    d=texts[source];c=json.loads(gzip.decompress(base64.b64decode(d['c'])))
    cfg=config or {};points=[]
    for row in c['cisd']:
        r=row.split('|')
        points.append({'id':int(r[0]),'position':[int(v)/100 for v in r[1].split(',')],
            'rotation':[int(v)/102 for v in r[2].split(',')],'scale':int(r[3])/100,
            'points':float(r[5]),'physicsMode':int(r[4]),'massOverride':float(r[6]),
            'holeColliderActive':int(r[7]),'holeColliderLevel':int(r[8]),'specialCollision':int(r[9])})
    environment=[]
    for row in c.get('ed',[]):
        r=row.split('|')
        environment.append({'kind':int(r[0]),'position':[float(v) for v in r[1].split(',')],
            'rotation':[float(v) for v in r[2].split(',')],'scale':[float(v) for v in r[3].split(',')]})
    obstacles=[]
    for o in c.get('levelObstacleInfos',[]):
        obstacles.append({'type':o['type'],'scale':o['scale'],'position':[o['position'][k] for k in 'xyz'],
            'rotation':[o['rotation'][k] for k in 'xyzw'],'isPhysics':o['isPhysics']})
    return {'index':index,'source':source,'time':cfg.get('t',int(d['r']['levelTime'])),
        'bombs':cfg.get('b',c.get('randomBombs',0)),'difficulty':cfg.get('d',d['r'].get('ld',0)),
        'themeIndex':cfg.get('th',0),'goalType':d['r']['goalType'],'goals':d['r']['goalInfos'],
        'points':points,'environment':environment,'obstacles':obstacles}
levels=[convert(l['i'],i+1,l) for i,l in enumerate(campaign)]
main_sources={l['i'] for l in campaign}
challenges=[convert(s,i+1) for i,s in enumerate(sorted(set(texts)-main_sources))]
(out/'Levels.json').write_text(json.dumps({'levels':levels},separators=(',',':')),encoding='utf-8')
(out/'Challenges.json').write_text(json.dumps({'levels':challenges},separators=(',',':')),encoding='utf-8')
env=UnityPy.load(*[str(f) for f in root.glob('*.bundle')])
for attempt in range(8):
    try:containers=dict(env.container);break
    except RuntimeError:pass
meshes={m['key']:m for m in data['meshes']};materials={m['key']:m for m in data['materials']}
textures={f.stem:True for f in out.glob('T*.png')}
# Reuse reviewed binary mesh and hierarchy serialization, not its old top-level export.
source=ast.parse((W/'export_reference5_content.py').read_text(encoding='utf-8'))
functions=ast.Module(body=[n for n in source.body if isinstance(n,ast.FunctionDef)],type_ignores=[])
exec(compile(functions,'reference5-functions','exec'),globals())
models=data['models'];existing={m['id'] for m in models};added=[]
for id,d in sorted(items.items()):
    if id in existing:continue
    name=d['item']['ItemName'];matches=[(k,p) for k,p in containers.items() if k.endswith('.prefab') and k.rsplit('/',1)[-1].lower()==(name+'_Prefab.prefab').lower()]
    if not matches:raise ValueError('Missing exact prefab '+name)
    key,p=matches[0];go=p.read();nodes=[];walk(go,nodes)
    icon='';icons=[(k,v) for k,v in containers.items() if k.endswith('/'+name+'_Goal_No_Outline.png')]
    if icons:icon='Icon'+str(id);icons[0][1].read().image.save(out/(icon+'.png'))
    rb=next((co.component.deref().read_typetree() for co in go.m_Component if co.component.deref().type.name=='Rigidbody'),{})
    models.append({'mass':rb.get('m_Mass',1),'linearDamping':rb.get('m_LinearDamping',0),'angularDamping':rb.get('m_AngularDamping',1),'id':id,'name':name,'nodes':nodes,'icon':icon,'source':key})
    added.append({'id':id,'name':name,'source':key});print('ITEM',id,name,flush=True)
# The original poison prefab carries its proper mesh, material, bounds and collision.
bomb_key=next(k for k in containers if k.endswith('/PoisonBombSolidSelected.prefab'))
nodes=[];bomb_go=containers[bomb_key].read();walk(bomb_go,nodes)
rb=next((co.component.deref().read_typetree() for co in bomb_go.m_Component if co.component.deref().type.name=='Rigidbody'),{})
models=[m for m in models if m['id']!=-1]
models.append({'id':-1,'name':'PoisonBombSolidSelected','icon':'','source':bomb_key,'nodes':nodes,'mass':rb.get('m_Mass',1),'linearDamping':rb.get('m_LinearDamping',0),'angularDamping':rb.get('m_AngularDamping',1)})
pillar_key=next(k for k in containers if k.endswith('/ManualWallPillar.prefab'))
nodes=[];walk(containers[pillar_key].read(),nodes)
environments=[m for m in data['environments'] if m['id']!=9]
environments.append({'id':9,'name':'ManualWallPillar','icon':'','source':pillar_key,'nodes':nodes})
# EnvironmentManager.OnEnable disables its editor-only DemoWall during gameplay.
for environment in environments:
    for node in environment['nodes']:
        if node['name']=='DemoWall':node['mesh']='';node['materials']=[];node['colliders']=[]
# Recover complete custom material parameters for both old and additional models.
for o in env.objects:
    key='M'+str(o.path_id)
    if o.type.name!='Material' or key not in materials:continue
    d=o.read_typetree();pr=d['m_SavedProperties'];colors=dict(pr['m_Colors']);floats=dict(pr['m_Floats']);m=materials[key]
    c=colors.get('_MainColor',colors.get('_BaseColor',colors.get('_Color')))
    if c:m['color']=[c[k] for k in 'rgba']
    sh=colors.get('_ShadowColor',{'r':.566,'g':.566,'b':.566,'a':1});m['shadow']=[sh[k] for k in 'rgba']
    m['edgeStart']=floats.get('_ShadowEdgeStart',-1);m['edgeEnd']=floats.get('_ShadowEdgeEnd',2);m['gloss']=floats.get('_Glossiness',32)
(out/'Models.json').write_text(json.dumps({'models':models,'environments':environments,'meshes':list(meshes.values()),'materials':list(materials.values())},separators=(',',':')),encoding='utf-8')
report={'campaignEntries':len(levels),'campaignUniqueLayouts':len(main_sources),'challengeLayouts':len(challenges),'models':len(models)-1,'additionalModels':added,'campaignAuthoredBombs':sum(len(l['obstacles']) for l in levels),'campaignPillars':sum(sum(p['kind']==9 for p in l['environment']) for l in levels),'campaignThemeCounts':dict(collections.Counter(l['themeIndex'] for l in levels))}
(ROOT/'Audit/ContentExpansion.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('COMPLETE', {k:v for k,v in report.items() if k!='additionalModels'})
