from pathlib import Path
import sys,json,struct,collections
w=Path(r'C:\Users\PUVKID\Documents\Codex\2026-10-13\new-chat\work')
sys.path.insert(0,str(w/'ref-python'))
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler
root=Path(r'C:\Users\PUVKID\Desktop\Hole It\7차')
out=root/'Source/Assets/ReferenceContent'
d=json.loads((out/'Models.json').read_text(encoding='utf8'))
meta={x['modelId']:x for x in json.loads((root/'Audit/CollectableMetadata.json').read_text(encoding='utf8'))}
env=UnityPy.load(*[str(f) for f in (w/'reference').glob('*.bundle')])
for attempt in range(8):
    try: containers=dict(env.container);break
    except RuntimeError: pass
meshes={x['key']:x for x in d['meshes']};stats=collections.Counter();large=[]
def mesh(p):
    key='G'+str(p.path_id)
    if key in meshes:return key
    obj=p.deref_parse_as_object();h=MeshHandler(obj);h.process();tris=h.get_triangles();verts=h.m_Vertices;norms=h.m_Normals or [(0,1,0)]*len(verts);uv=h.m_UV0 or [(0,0)]*len(verts)
    with (out/(key+'.bytes')).open('wb') as f:
        f.write(struct.pack('<ii',len(verts),len(tris)))
        for v,n,u in zip(verts,norms,uv):f.write(struct.pack('<8f',*v[:3],*n[:3],*u[:2]))
        for sub in tris:
            flat=[i for tri in sub for i in tri];f.write(struct.pack('<i',len(flat)));f.write(struct.pack('<'+'i'*len(flat),*flat))
    meshes[key]={'key':key,'name':obj.m_Name,'vertices':len(verts)};stats['addedMeshes']+=1
    if len(verts)>120:large.append((key,len(verts)))
    return key
def update(model):
    go=containers[model['source']].read();info=meta.get(model['id'],{'objectSize':0,'isPhysics':0,'complexColliders':[],'simpleColliders':[],'specialColliders':[]})
    model['objectSize']=info['objectSize'];groups={}
    mf=next((co.component.deref().read() for co in go.m_Component if co.component.deref().type.name=='MeshFilter'),None)
    model['holeSizeMesh']=mesh(mf.m_Mesh) if mf and mf.m_Mesh.path_id else ''
    model['maxRadiusReductionRate']=0.0
    if info.get('itemInfo',{}).get('m_PathID',0):
        from UnityPy.classes.PPtr import PPtr
        ip=info['itemInfo'];item=PPtr(m_FileID=ip['m_FileID'],m_PathID=ip['m_PathID'],assetsfile=containers[model['source']].assetsfile).deref().read_typetree()
        model['maxRadiusReductionRate']=item.get('itemColliderData',{}).get('maxRadiusReductionRate',0.0)
    for group,key in zip([1,2,4],['complexColliders','simpleColliders','specialColliders']):
        for ref in info[key]:
            groups[ref['m_PathID']]=groups.get(ref['m_PathID'],0)|group
    rb=next((co.component.deref().read_typetree() for co in go.m_Component if co.component.deref().type.name=='Rigidbody'),{})
    model['useGravity']=bool(rb.get('m_UseGravity',True));model['sourcePhysicsMode']=int(info['isPhysics'])
    nodeIndex=0
    def walk(obj,active=True):
        nonlocal nodeIndex
        node=model['nodes'][nodeIndex];nodeIndex+=1
        assert node['name']==obj.m_Name,(model['id'],nodeIndex,node['name'],obj.m_Name)
        active=active and bool(obj.m_IsActive);shapes=[]
        comps=[p.component.deref() for p in obj.m_Component]
        for co in comps:
            if co.type.name not in ['BoxCollider','SphereCollider','CapsuleCollider','MeshCollider']:continue
            ct=co.read_typetree()
            if ct.get('m_IsTrigger',False):continue
            matKind=0
            if ct.get('m_Material',{}).get('m_PathID',0):
                name=co.read().m_Material.deref().read_typetree()['m_Name']
                matKind={'NoFriction':1,'LessFrictionObject':2,'dragMat':3}.get(name,0)
                assert name in ('NoFriction','LessFrictionObject','dragMat'),name
            center=ct.get('m_Center',{'x':0,'y':0,'z':0});size=ct.get('m_Size',{'x':1,'y':1,'z':1})
            shape={'kind':co.type.name,'center':[center[k] for k in 'xyz'],'size':[size[k] for k in 'xyz'],'radius':ct.get('m_Radius',.5),'height':ct.get('m_Height',1),'direction':ct.get('m_Direction',1),'mesh':'','group':groups.get(co.path_id,0),'sourceEnabled':bool(ct.get('m_Enabled',True)) and active,'materialKind':matKind}
            if co.type.name=='MeshCollider' and co.read().m_Mesh.path_id:shape['mesh']=mesh(co.read().m_Mesh)
            shapes.append(shape);stats['allShapes']+=1;stats['group'+str(shape['group'])]+=1
            if not shape['sourceEnabled']:stats['disabledShapes']+=1
        node['colliders']=shapes
        tr=next(o.read() for o in comps if o.type.name in ['Transform','RectTransform'])
        for p in tr.m_Children:walk(p.deref_parse_as_object().m_GameObject.deref_parse_as_object(),active)
    walk(go)
    assert nodeIndex==len(model['nodes'])
for model in d['models']:update(model)
d['meshes']=list(meshes.values())
(out/'Models.json').write_text(json.dumps(d,separators=(',',':')),encoding='utf8')
(root/'Audit/PhysicsExportSummary.json').write_text(json.dumps({'stats':dict(stats),'newLargeMeshes':large},indent=2),encoding='utf8')
print(dict(stats));print('New large collision meshes',large)
