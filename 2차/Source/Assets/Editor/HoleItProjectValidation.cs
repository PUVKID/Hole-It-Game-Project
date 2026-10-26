
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class HoleItProjectValidation
{
    [MenuItem("HoleIt/3. 프로젝트 검사")]
    public static void ValidateMenu(){ValidateOrThrow();Debug.Log("[HoleIt] Editor asset/scene validation PASS. This is not a player build or full playthrough.");}
    public static void ValidateCli(){try{ValidateOrThrow();EditorApplication.Exit(0);}catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}}
    public static void CheckMesh(Mesh mesh){
        if(mesh==null||mesh.vertexCount<3)throw new InvalidDataException("Missing/empty mesh");
        Vector3[] v=mesh.vertices;foreach(var p in v)if(!Finite(p))throw new InvalidDataException(mesh.name+": nonfinite vertex");
        foreach(var n in mesh.normals)if(!Finite(n))throw new InvalidDataException(mesh.name+": nonfinite normal");
        foreach(var uv in mesh.uv)if(!NumberGuard.Finite(uv.x)||!NumberGuard.Finite(uv.y))throw new InvalidDataException(mesh.name+": nonfinite UV");
        for(int s=0;s<mesh.subMeshCount;s++){var indices=mesh.GetIndices(s);if(indices.Length%3!=0)throw new InvalidDataException(mesh.name+": incomplete triangles");foreach(int index in indices)if(index<0||index>=v.Length)throw new InvalidDataException(mesh.name+": index out of bounds");}
        if(!Finite(mesh.bounds.center)||!Finite(mesh.bounds.extents)||mesh.bounds.size.sqrMagnitude<=0)throw new InvalidDataException(mesh.name+": invalid bounds");
    }
    private static bool Finite(Vector3 p){return NumberGuard.Finite(p.x)&&NumberGuard.Finite(p.y)&&NumberGuard.Finite(p.z);}
    public static void ValidateOrThrow(){
        var sizes=AssetDatabase.LoadAssetAtPath<HoleSizeTable>("Assets/HoleIt/Resources/HoleSizeTable.asset");
        if(sizes==null||!sizes.IsValid())throw new InvalidDataException("Invalid HoleSizeTable");
        var ids=new HashSet<int>();int count=0;
        foreach(string g in AssetDatabase.FindAssets("t:LevelData",new[]{"Assets/HoleIt/Resources/Data/Levels"})){
            var level=AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(g));if(!ids.Add(level.levelId))throw new InvalidDataException("Duplicate level ID");
            var errors=LevelValidator.Validate(level,sizes);if(errors.Count>0)throw new InvalidDataException(level.name+": "+string.Join("; ",errors));count++;
        }
        if(count!=30)throw new InvalidDataException("Expected 30 levels, found "+count);
        foreach(string g in AssetDatabase.FindAssets("t:Mesh",new[]{"Assets/HoleIt/Meshes"}))CheckMesh(AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(g)));
        foreach(string g in AssetDatabase.FindAssets("t:Material",new[]{"Assets/HoleIt/Materials"})){
            var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
            if(material==null||material.shader==null||material.shader.name=="Hidden/InternalErrorShader"||ShaderUtil.ShaderHasError(material.shader))throw new InvalidDataException("Shader/material failure: "+AssetDatabase.GUIDToAssetPath(g));
        }
        foreach(string g in AssetDatabase.FindAssets("t:EdibleData",new[]{"Assets/HoleIt/Data/Edibles"})){
            var edible=AssetDatabase.LoadAssetAtPath<EdibleData>(AssetDatabase.GUIDToAssetPath(g));if(edible.prefab==null||edible.icon==null||edible.prefab.GetComponent<EdibleObject>()==null||edible.prefab.GetComponent<Collider>()==null)throw new InvalidDataException("Broken EdibleData: "+edible.name);
        }
        const string main="Assets/Scenes/Main.unity";if(!File.Exists(main))throw new FileNotFoundException(main);
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool open=active.path==main;
        var scene=open?active:EditorSceneManager.OpenScene(main,OpenSceneMode.Additive);
        try{
            var managers=new List<LevelManager>();
            foreach(GameObject root in scene.GetRootGameObjects()){
                managers.AddRange(root.GetComponentsInChildren<LevelManager>(true));
                foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true)){
                    if(component==null)throw new InvalidDataException("Missing scene script under "+root.name);
                    if(!(component is GameManager)&&!(component is LevelManager)&&!(component is HoleController)&&!(component is InputManager)&&!(component is UIManager))continue;
                    var serialized=new SerializedObject(component);var property=serialized.GetIterator();
                    while(property.NextVisible(true)){
                        if(property.propertyType==SerializedPropertyType.ObjectReference&&property.name!="m_Script"&&property.objectReferenceValue==null)throw new InvalidDataException(component.GetType().Name+" missing "+property.propertyPath);
                    }
                }
            }
            if(managers.Count!=1||managers[0].LevelCount!=30)throw new InvalidDataException("Main scene needs exactly one manager with 30 serialized levels");
        }finally{if(!open)EditorSceneManager.CloseScene(scene,true);}
        Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/EditorValidation.txt",DateTime.UtcNow.ToString("O")+"\nPASS: 30 levels, mesh data, imported shaders, scene references\nNOT RUN by this validator: complete playthrough, performance, Android build\n");
    }
}
