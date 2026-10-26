using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;
public static class ReferenceCollisionBuilder {
 public static void Apply(){
  var c=Resources.Load<ReferenceCatalog>("ReferenceCatalog");int count=0;
  foreach(var path in Directory.GetFiles("Assets/ReferenceContent/CollisionHulls","*.bytes")){
   string key=Path.GetFileNameWithoutExtension(path),asset=Path.ChangeExtension(path,".asset");var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(asset);
   if(mesh==null){using(var r=new BinaryReader(File.OpenRead(path))){int n=r.ReadInt32();r.ReadInt32();var vertices=new Vector3[n];for(int i=0;i<n;i++){vertices[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());for(int k=0;k<5;k++)r.ReadSingle();}int len=r.ReadInt32();var triangles=new int[len];for(int i=0;i<len;i++)triangles[i]=r.ReadInt32();mesh=new Mesh{name=key+"_BoundedHull",vertices=vertices,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,asset);}}
   if(mesh.vertexCount>120||mesh.triangles.Length/3>240)throw new Exception("Physics hull exceeds budget: "+key);
   foreach(var model in c.models)foreach(var shape in model.collisionParts)if(shape.mesh.name==key||shape.mesh.name==mesh.name){shape.mesh=mesh;count++;}
  }
  EditorUtility.SetDirty(c);AssetDatabase.SaveAssets();Debug.Log("Bounded source collision hull assignments: "+count);
 }
}
