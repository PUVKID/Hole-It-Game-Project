using UnityEngine;
using UnityEngine.Rendering;

/// <summary>원본 HoleProgressBar의 열린 원호, 반지름, 두께와 색으로 진행도를 그립니다.</summary>
public sealed class ReferenceGrowthRing : MonoBehaviour {
    const int Segments=128;
    readonly Vector3[] vertices=new Vector3[(Segments+1)*2];
    readonly int[] triangles=new int[Segments*6];
    Mesh inner,outer;
    Material innerMaterial,outerMaterial;
    MeshRenderer innerRenderer,outerRenderer;
    public float Progress {get;private set;}
    public void Initialize(){
        for(int i=0;i<Segments;i++){int t=i*6,v=i*2;triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;triangles[t+3]=v+1;triangles[t+4]=v+3;triangles[t+5]=v+2;}
        inner=Create("InnerProgressBar",new Color(.772549f,.443137f,0),out innerMaterial,out innerRenderer);
        outer=Create("OutlineProgressBar",new Color(.075294f,.075294f,.075294f),out outerMaterial,out outerRenderer);
    }
    Mesh Create(string name,Color color,out Material material,out MeshRenderer renderer){
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
        var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.triangles=triangles;go.GetComponent<MeshFilter>().sharedMesh=mesh;
        material=new Material(Shader.Find("Unlit/Color"));material.color=color;renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return mesh;
    }
    public void SetProgress(float progress,Vector3 position,Vector3 scale){
        Progress=Mathf.Clamp01(progress);transform.position=position;transform.localScale=scale;
        innerRenderer.enabled=outerRenderer.enabled=Progress>0;
        if(Progress<=0)return;
        float radius=Mathf.Lerp(1.05f,1.09f,Progress);
        Fill(outer,radius,Mathf.Lerp(.12f,.2f,Progress),0);
        Fill(inner,radius,Mathf.Lerp(.06f,.14f,Progress),.002f);
    }
    void Fill(Mesh mesh,float radius,float thickness,float y){
        for(int i=0;i<=Segments;i++){
            float angle=Mathf.Lerp(4.2f,-1.1f,Progress*i/Segments);
            Vector3 direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            vertices[i*2]=direction*(radius-thickness*.5f)+Vector3.up*y;
            vertices[i*2+1]=direction*(radius+thickness*.5f)+Vector3.up*y;
        }
        mesh.vertices=vertices;mesh.RecalculateBounds();
    }
    void OnDestroy(){if(inner!=null)Destroy(inner);if(outer!=null)Destroy(outer);if(innerMaterial!=null)Destroy(innerMaterial);if(outerMaterial!=null)Destroy(outerMaterial);}
}
