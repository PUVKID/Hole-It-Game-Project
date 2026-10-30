from pathlib import Path
w=Path(__file__).parent
s=(w/'export_reference_content.py').read_text()
s=s.replace("['level_list_info'][:30]", "['level_list_info']")
s=s.replace("n={'name':go.m_Name", "n={'colliders':[],'name':go.m_Name")
needle="    nodes.append(n)"
add='''    for co in comps:
        if co.type.name not in ['BoxCollider','SphereCollider','CapsuleCollider','MeshCollider']:continue
        cd=co.read();ct=co.read_typetree()
        if not ct.get('m_Enabled',True) or ct.get('m_IsTrigger',False):continue
        center=ct.get('m_Center',{'x':0,'y':0,'z':0});size=ct.get('m_Size',{'x':1,'y':1,'z':1})
        c={'kind':co.type.name,'center':[center[k] for k in 'xyz'],'size':[size[k] for k in 'xyz'],'radius':ct.get('m_Radius',.5),'height':ct.get('m_Height',1),'direction':ct.get('m_Direction',1),'mesh':''}
        if co.type.name=='MeshCollider' and cd.m_Mesh.path_id:c['mesh']=mesh(cd.m_Mesh)
        n['colliders'].append(c)
'''
s=s.replace(needle,add+needle)
s=s.replace("for p in tr.m_Children:walk(resolve(resolve(p).m_GameObject),nodes,idx)", "for p in tr.m_Children:walk(resolve(resolve(p).m_GameObject),nodes,idx)")
s=s.replace("models.append({'id':id", "rb=next((co.component.deref().read_typetree() for co in go.m_Component if co.component.deref().type.name=='Rigidbody'),{})\n    models.append({'mass':rb.get('m_Mass',1),'linearDamping':rb.get('m_LinearDamping',0),'angularDamping':rb.get('m_AngularDamping',1),'id':id")
# Do not reintroduce editor-only wall geometry from the reference hierarchy.
s=s.replace("if mf and mr and mr.m_Enabled", "if 'Editor' not in go.m_Name and mf and mr and mr.m_Enabled")
(w/'export_reference5_content.py').write_text(s)
