from pathlib import Path
import sys,json,gzip,base64,struct,collections,re
sys.path.insert(0,str(Path(__file__).parent/'ref-python'))
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler
root=Path(__file__).parent/'reference'
out=Path(__file__).parent/'AndroidFinal/Assets/ReferenceContent';out.mkdir(exist_ok=True)
env=UnityPy.load(*[str(f) for f in root.glob('*.bundle')])
for attempt in range(8):
    try:containers=dict(env.container);break
    except RuntimeError:pass
items={d['item']['id']:d for d in json.loads((root/'all-items.json').read_text())}
texts={d['m_Name']:json.loads(d['m_Script']) for f in root.glob('text*json') if (d:=json.loads(f.read_text())).get('m_Name','').startswith('Level_')}
levels=[];ids=set()
for index,l in enumerate(json.loads((root/'list_main_fall_back.txt').read_text())['level_list_info'][:30]):
    d=texts['Level_'+l['i']];c=json.loads(gzip.decompress(base64.b64decode(d['c'])))
    points=[]
    for row in c['cisd']:
        r=row.split('|');id=int(r[0]);ids.add(id)
        points.append({'id':id,'position':[int(v)/100 for v in r[1].split(',')], 'rotation':[int(v)/102 for v in r[2].split(',')], 'scale':int(r[3])/100,'points':float(r[5])})
    environment=[]
    for row in c.get('ed',[]):
        r=row.split('|');kind=int(r[0])
        if kind>5:continue
        environment.append({'kind':kind,'position':[float(v) for v in r[1].split(',')],'rotation':[float(v) for v in r[2].split(',')],'scale':[float(v) for v in r[3].split(',')]})
    levels.append({'index':index+1,'source':l['i'],'time':l['t'],'bombs':l.get('b',0),'difficulty':l.get('d',0),'goalType':d['r']['goalType'],'goals':d['r']['goalInfos'],'points':points,'environment':environment})
(out/'Levels.json').write_text(json.dumps({'levels':levels},separators=(',',':')),encoding='utf-8')
def resolve(p):return p.deref_parse_as_object()
def vec(v):return [v.x,v.y,v.z]
meshes={};materials={};textures={};models=[]
def texture(p):
    if not p.path_id:return ''
    key='T'+str(p.path_id)
    if key not in textures:
        d=resolve(p);d.image.save(out/(key+'.png'));textures[key]=True
    return key
def material(p):
    key='M'+str(p.path_id)
    if key in materials:return key
    d=p.deref().read_typetree();props=d['m_SavedProperties'];ts=dict(props['m_TexEnvs']);cs=dict(props['m_Colors']);fs=dict(props['m_Floats'])
    tex=ts.get('_BaseMap',ts.get('_MainTex',{}));ref=tex.get('m_Texture',{});texid=ref.get('m_PathID',0)
    texkey=''
    if texid:
        from UnityPy.classes.PPtr import PPtr
        texkey=texture(PPtr(m_FileID=ref['m_FileID'],m_PathID=texid,assetsfile=p.deref().assets_file))
    color=cs.get('_BaseColor',cs.get('_Color',{'r':1,'g':1,'b':1,'a':1}))
    materials[key]={'key':key,'name':d['m_Name'],'texture':texkey,'color':[color[k] for k in ['r','g','b','a']], 'smoothness':fs.get('_Smoothness',fs.get('_Glossiness',.3))}
    return key
def mesh(p):
    key='G'+str(p.path_id)
    if key in meshes:return key
    d=resolve(p);h=MeshHandler(d);h.process()
    tris=h.get_triangles();verts=h.m_Vertices;norms=h.m_Normals or [(0,1,0)]*len(verts);uv=h.m_UV0 or [(0,0)]*len(verts)
    with (out/(key+'.bytes')).open('wb') as f:
        f.write(struct.pack('<ii',len(verts),len(tris)))
        for v,n,u in zip(verts,norms,uv):f.write(struct.pack('<8f',*v[:3],*n[:3],*u[:2]))
        for sub in tris:
            flat=[i for tri in sub for i in tri];f.write(struct.pack('<i',len(flat)));f.write(struct.pack('<'+'i'*len(flat),*flat))
    meshes[key]={'key':key,'name':d.m_Name,'vertices':len(verts)};return key
def walk(go,nodes,parent=-1):
    comps=[pair.component.deref() for pair in go.m_Component];tr=next(o.read() for o in comps if o.type.name in ['Transform','RectTransform']);idx=len(nodes)
    n={'name':go.m_Name,'parent':parent,'position':vec(tr.m_LocalPosition),'rotation':[tr.m_LocalRotation.x,tr.m_LocalRotation.y,tr.m_LocalRotation.z,tr.m_LocalRotation.w],'scale':vec(tr.m_LocalScale),'mesh':'','materials':[]}
    mf=next((o.read() for o in comps if o.type.name=='MeshFilter'),None)
    mr=next((o.read() for o in comps if o.type.name=='MeshRenderer'),None)
    if mf and mr and mr.m_Enabled and (go.m_IsActive or parent<0) and mf.m_Mesh.path_id:n['mesh']=mesh(mf.m_Mesh);n['materials']=[material(p) for p in mr.m_Materials]
    nodes.append(n)
    for p in tr.m_Children:walk(resolve(resolve(p).m_GameObject),nodes,idx)
missing=[]
for id in sorted(ids):
    item=items[id]['item'];name=item['ItemName']
    candidates=[(k,v) for k,v in containers.items() if k.endswith('.prefab') and k.rsplit('/',1)[-1].lower()==(name+'_Prefab.prefab').lower()]
    if not candidates:
        candidates=[(k,v) for k,v in containers.items() if k.endswith('.prefab') and name.lower() in k.rsplit('/',1)[-1].lower()]
    if not candidates:missing.append((id,name));continue
    key,p=candidates[0];go=p.read();nodes=[];walk(go,nodes)
    icon=''
    cand=[(k,v) for k,v in containers.items() if k.endswith(name+'_Goal_No_Outline.png')]
    if cand:
        icon='Icon'+str(id);cand[0][1].read().image.save(out/(icon+'.png'))
    models.append({'id':id,'name':name,'nodes':nodes,'icon':icon,'source':key})
    print(id,name,len(nodes),flush=True)
environments=[]
for key,p in containers.items():
    match=re.search(r'/Variation(\d+)/Prefabs/([^/]+)\.prefab$',key)
    if not match:continue
    variant=int(match[1]);name=match[2].lower()
    kind=0 if 'concave' in name else 1 if 'convex' in name else 2 if 'quarter' in name else 3 if 'straight' in name else 4 if 'grass' in name else 5 if 'play' in name else -1
    if kind<0:continue
    nodes=[];walk(p.read(),nodes)
    environments.append({'id':variant*10+kind,'name':match[2],'nodes':nodes,'icon':'','source':key})
(out/'Models.json').write_text(json.dumps({'models':models,'environments':environments,'meshes':list(meshes.values()),'materials':list(materials.values())},separators=(',',':')),encoding='utf-8')
print('MISSING',missing);print('EXPORTED',len(models),len(meshes),len(materials),len(textures))
assert not missing,missing
