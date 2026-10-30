from pathlib import Path
p=Path(__file__).parent/'AndroidFinal'
f=p/'Assets/Scripts/ReferenceCatalog.cs';s=f.read_text(encoding='utf-8').replace('[Serializable] public class ReferenceModel', '[Serializable] public class ReferenceCollision {public Mesh mesh;public Matrix4x4 local=Matrix4x4.identity;}\n[Serializable] public class ReferenceModel').replace('public Mesh collisionMesh;', 'public ReferenceCollision[] collisionParts;public Mesh collisionMesh;');f.write_text(s,encoding='utf-8')
f=p/'Assets/Editor/ReferenceBuilder.cs';s=f.read_text(encoding='utf-8').replace('collisionMesh=collisionMesh,mass=a.mass', 'collisionParts=collision.Select(c=>new ReferenceCollision{mesh=c.mesh,local=c.transform}).ToArray(),collisionMesh=collisionMesh,mass=a.mass');f.write_text(s,encoding='utf-8')
f=p/'Assets/Scripts/ReferenceGame.Physics.cs';s=f.read_text(encoding='utf-8')
s=s.replace('public MeshCollider collider;', 'public readonly List<MeshCollider> colliders=new List<MeshCollider>();')
s=s.replace('if(a.model.collisionMesh==null)return null;', 'if(a.model.collisionParts==null||a.model.collisionParts.Length==0)return null;')
s=s.replace('b.collider=root.AddComponent<MeshCollider>();b.collider.convex=true;b.collider.sharedMaterial=friction;b.collider.contactOffset=.01f;', '')
s=s.replace('b.collider.sharedMesh=a.model.collisionMesh;', '''for(int c=0;c<a.model.collisionParts.Length;c++){
            if(c==b.colliders.Count){var child=new GameObject("Authored Contact Shape");child.transform.SetParent(b.root.transform,false);var collider=child.AddComponent<MeshCollider>();collider.convex=true;collider.sharedMaterial=friction;collider.contactOffset=.01f;b.colliders.Add(collider);}
            var shape=a.model.collisionParts[c];var col=b.colliders[c];col.gameObject.SetActive(true);col.transform.localPosition=shape.local.GetColumn(3);col.transform.localRotation=shape.local.rotation;col.transform.localScale=shape.local.lossyScale;col.sharedMesh=shape.mesh;
        }
        for(int c=a.model.collisionParts.Length;c<b.colliders.Count;c++)b.colliders[c].gameObject.SetActive(false);
        ''')
s=s.replace('Physics.IgnoreCollision(b.collider,physicalFloor,a.state==1);', 'IgnoreFloor(b,a.state==1);').replace('Physics.IgnoreCollision(b.collider,physicalFloor,true);', 'IgnoreFloor(b,true);')
s=s.replace('    void ReleasePhysicalBody(Actor a){', '''    void IgnoreFloor(PhysicalBody b,bool ignore){foreach(var col in b.colliders)if(col.gameObject.activeInHierarchy)Physics.IgnoreCollision(col,physicalFloor,ignore);}
    void ReleasePhysicalBody(Actor a){''')
s=s.replace('if(a.state!=0){a.falling=false;falling.RemoveAt(k);continue;}', 'if(a.state!=0||!a.falling){a.falling=false;falling.RemoveAt(k);continue;}')
s=s.replace('var bounds=b.collider.bounds;', 'var bounds=b.colliders[0].bounds;foreach(var col in b.colliders)if(col.gameObject.activeInHierarchy)bounds.Encapsulate(col.bounds);')
f.write_text(s,encoding='utf-8')
