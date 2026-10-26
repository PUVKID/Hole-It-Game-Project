using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>Interactive render of the actual collectible mesh, not a substitute thumbnail.</summary>
public sealed class ReferenceModelPreview:MonoBehaviour,IDragHandler {
    Camera previewCamera;RenderTexture target;GameObject root;Transform modelRoot;float yaw=25;
    public void Show(ReferenceModel model){
        if(root==null){
            root=new GameObject("Collection Preview Stage");root.transform.position=new Vector3(2000,100,2000);
            var cameraObject=new GameObject("Collection Camera");cameraObject.transform.SetParent(root.transform,false);previewCamera=cameraObject.AddComponent<Camera>();
            previewCamera.clearFlags=CameraClearFlags.SolidColor;previewCamera.backgroundColor=new Color(.12f,.06f,.22f,0);previewCamera.cullingMask=1<<29;previewCamera.fieldOfView=35;previewCamera.nearClipPlane=.01f;previewCamera.farClipPlane=100;previewCamera.allowMSAA=true;
            target=new RenderTexture(768,768,24,RenderTextureFormat.ARGB32){antiAliasing=4,name="Collectible Preview"};target.Create();previewCamera.targetTexture=target;GetComponent<RawImage>().texture=target;
        }
        root.SetActive(true);if(modelRoot!=null){modelRoot.gameObject.SetActive(false);ReleaseMeshes();Destroy(modelRoot.gameObject);}
        modelRoot=new GameObject("Collectible").transform;modelRoot.SetParent(root.transform,false);
        var center=new GameObject("Geometry").transform;center.SetParent(modelRoot,false);center.localPosition=-model.bounds.center;
        foreach(var part in model.parts){
            var node=new GameObject("Mesh",typeof(MeshFilter),typeof(MeshRenderer));node.layer=29;node.transform.SetParent(center,false);
            node.transform.localPosition=part.local.GetColumn(3);node.transform.localRotation=part.local.rotation;node.transform.localScale=part.local.lossyScale;
            var mesh=new Mesh{name="Collection Submesh"};mesh.indexFormat=part.mesh.indexFormat;mesh.vertices=part.mesh.vertices;mesh.normals=part.mesh.normals;mesh.uv=part.mesh.uv;mesh.triangles=part.mesh.GetTriangles(part.submesh);mesh.RecalculateBounds();
            node.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=node.GetComponent<MeshRenderer>();renderer.sharedMaterial=part.material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
        float radius=Mathf.Max(.1f,model.bounds.extents.magnitude),distance=radius/Mathf.Sin(previewCamera.fieldOfView*Mathf.Deg2Rad*.5f)*1.13f;
        previewCamera.transform.localPosition=new Vector3(0,radius*.25f,-distance);previewCamera.transform.LookAt(root.transform.position);previewCamera.farClipPlane=distance+radius*4+10;
        yaw=25;modelRoot.localRotation=Quaternion.Euler(-8,yaw,0);previewCamera.Render();previewCamera.enabled=false;
    }
    public void OnDrag(PointerEventData data){if(modelRoot==null)return;yaw-=data.delta.x*.45f;modelRoot.localRotation=Quaternion.Euler(-8,yaw,0);previewCamera.Render();}
    void OnDisable(){if(root!=null)root.SetActive(false);}
    void ReleaseMeshes(){if(modelRoot!=null)foreach(var f in modelRoot.GetComponentsInChildren<MeshFilter>(true))if(f.sharedMesh!=null)Destroy(f.sharedMesh);}
    void OnDestroy(){
        ReleaseMeshes();
        if(root!=null)Destroy(root);if(target!=null){target.Release();Destroy(target);}
    }
}
