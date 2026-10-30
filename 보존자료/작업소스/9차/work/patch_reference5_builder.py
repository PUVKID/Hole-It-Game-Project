from pathlib import Path
w=Path(__file__).parent;p=w/'AndroidFinal'
f=p/'Assets/Editor/ReferenceBuilder.cs';s=f.read_text()
s=s.replace('public string name,icon;public Node[] nodes;', 'public string name,icon;public Node[] nodes;public float mass=1,linearDamping,angularDamping=1;')
s=s.replace('class Node {public int parent;', 'class ColliderInfo {public string kind,mesh;public float[] center,size;public float radius,height;public int direction;}\n    [Serializable] class Node {public ColliderInfo[] colliders;public int parent;')
s=s.replace('ti.maxTextureSize=512;', 'ti.maxTextureSize=2048;ti.filterMode=FilterMode.Trilinear;')
s=s.replace('mat.enableInstancing=true;', 'mat.enableInstancing=true;mat.SetFloat("_Smoothness",a.smoothness);')
s=s.replace('var parts=new List<ReferencePart>();Bounds bounds', 'var parts=new List<ReferencePart>();var collision=new List<CombineInstance>();Bounds bounds')
s=s.replace('if(string.IsNullOrEmpty(n.mesh))continue;', '''if(n.colliders!=null)foreach(var col in n.colliders){
                    if(!string.IsNullOrEmpty(col.mesh))collision.Add(new CombineInstance{mesh=GetMesh(col.mesh),transform=matrices[i]});
                    else {var primitive=GameObject.CreatePrimitive(col.kind=="SphereCollider"?PrimitiveType.Sphere:col.kind=="CapsuleCollider"?PrimitiveType.Capsule:PrimitiveType.Cube);var mesh=primitive.GetComponent<MeshFilter>().sharedMesh;Vector3 size=col.kind=="BoxCollider"?V(col.size):col.kind=="SphereCollider"?Vector3.one*col.radius*2:new Vector3(col.radius*2,col.height/2,col.radius*2);Quaternion orientation=col.kind=="CapsuleCollider"&&col.direction!=1?Quaternion.Euler(col.direction==2?90:0,0,col.direction==0?90:0):Quaternion.identity;collision.Add(new CombineInstance{mesh=mesh,transform=matrices[i]*Matrix4x4.TRS(V(col.center),orientation,size)});UnityEngine.Object.DestroyImmediate(primitive);}
                }if(string.IsNullOrEmpty(n.mesh))continue;''')
s=s.replace('models.Add(new ReferenceModel{id=a.id', '''Mesh collisionMesh=null;if(collision.Count>0){string path=Folder+"/Built/Collision_"+a.id+".asset";collisionMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(collisionMesh==null){collisionMesh=new Mesh{name="Collision "+a.name,indexFormat=IndexFormat.UInt32};collisionMesh.CombineMeshes(collision.ToArray());AssetDatabase.CreateAsset(collisionMesh,path);}}
            models.Add(new ReferenceModel{collisionMesh=collisionMesh,mass=a.mass,linearDamping=a.linearDamping,angularDamping=a.angularDamping,id=a.id''')
s=s.replace('bundleVersion="4.1.0"','bundleVersion="5.0.0"').replace('bundleVersionCode=6','bundleVersionCode=7')
s=s.replace('c.levels.Length!=30||c.models.Length!=259','c.levels.Length!=465||c.models.Length!=869')
f.write_text(s)
f=p/'ProjectSettings/ProjectSettings.asset';s=f.read_text().replace('bundleVersion: 4.1.0','bundleVersion: 5.0.0').replace('AndroidBundleVersionCode: 6','AndroidBundleVersionCode: 7');f.write_text(s)
f=p/'Assets/Scripts/ReferenceGame.cs';s=f.read_text().replace('public class ReferenceGame:MonoBehaviour','public partial class ReferenceGame:MonoBehaviour')
s=s.replace('public ReferenceModel model;public Vector3 pos;', 'public PhysicalBody body;public long cellKey;public ReferenceModel model;public Vector3 pos;')
s=s.replace('1,30);CreateWorld()', '1,Catalog.levels.Length);CreateWorld()').replace('Mathf.Min(30,LevelIndex+2)', 'Mathf.Min(Catalog.levels.Length,LevelIndex+2)').replace('Mathf.Min(29,LevelIndex+1)', 'Mathf.Min(Catalog.levels.Length-1,LevelIndex+1)')
s=s.replace('LevelIndex=Mathf.Clamp(index', 'ResetPhysicalWorld();LevelIndex=Mathf.Clamp(index')
s=s.replace('list.Add(i);}', 'list.Add(i);actors[i].cellKey=key;}')
s=s.replace('void OnDestroy(){if(Instance==this)', 'void OnDestroy(){DisposePhysicalWorld();if(Instance==this)')
start=s.index('    void UpdateFalls(float dt)');end=s.index('\n    void UpdateMouth',start)
s=s[:start]+'''    void UpdateFalls(float dt){StepPhysicalWorld(dt);}
'''+s[end:]
old='a.dropTime+=dt;float t=a.dropTime/.28f;a.pos.y=a.dropY-4*t*t;a.rotation=Quaternion.AngleAxis(dt*35,Vector3.right)*a.rotation;if(t<1)continue;'
new='a.dropTime+=dt;if(a.body==null){a.fallSpeed+=5.81f*dt;a.pos.y-=a.fallSpeed*dt;}if(a.Top>-.25f)continue;ReleasePhysicalBody(a);'
assert old in s;s=s.replace(old,new)
s=s.replace('a.dropY=a.pos.y;dropping.Add(i);', 'a.dropY=a.pos.y;BeginPhysicalDrop(i);dropping.Add(i);')
s=s.replace('var matrix=Matrix4x4.TRS(a.pos,a.rotation,Vector3.one*a.scale);','var matrix=Matrix4x4.TRS(RenderPosition(a),RenderRotation(a),Vector3.one*a.scale);')
f.write_text(s)
