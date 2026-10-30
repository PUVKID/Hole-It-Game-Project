from pathlib import Path
p=Path(__file__).resolve().parent.parent/'Source/Assets/Editor/ReferenceBuilder.cs'
s=p.read_text(encoding='utf-8-sig')
changes=[
('public float mass=1,linearDamping,angularDamping=1;', 'public float mass=1,linearDamping,angularDamping=1,objectSize;public bool useGravity=true;'),
('public int direction;}', 'public int direction,group,materialKind;public bool sourceEnabled=true;}'),
('var collision=new List<CombineInstance>();Bounds bounds=', 'var collision=new List<CombineInstance>();var contacts=new List<ReferenceCollision>();\n            void Contact(Mesh mesh,Matrix4x4 matrix,ColliderInfo info){collision.Add(new CombineInstance{mesh=mesh,transform=matrix});contacts.Add(new ReferenceCollision{mesh=mesh,local=matrix,group=info.group,sourceEnabled=info.sourceEnabled,materialKind=info.materialKind});}\n            Bounds bounds='),
('collision.Add(new CombineInstance{mesh=GetMesh(col.mesh),transform=matrices[i]});', 'Contact(GetMesh(col.mesh),matrices[i],col);'),
('collision.Add(new CombineInstance{mesh=mesh,transform=matrices[i]*Matrix4x4.TRS(V(col.center),orientation,size)});', 'Contact(mesh,matrices[i]*Matrix4x4.TRS(V(col.center),orientation,size),col);'),
('collisionParts=collision.Select(c=>new ReferenceCollision{mesh=c.mesh,local=c.transform}).ToArray()', 'collisionParts=contacts.ToArray()'),
('angularDamping=a.angularDamping,id=a.id', 'angularDamping=a.angularDamping,objectSize=a.objectSize,useGravity=a.useGravity,id=a.id')
]
for old,new in changes:
    assert s.count(old)==1,(old,s.count(old))
    s=s.replace(old,new)
p.write_text(s,encoding='utf-8')
print('Builder preserves authored collider groups, enable states, materials, and object metadata.')
