using UnityEngine;
public class ArenaPresenter : MonoBehaviour
{
    [SerializeField] private Renderer floor;
    [SerializeField] private Renderer rim;
    [SerializeField] private Transform[] rails;
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private Light sun;
    private MaterialPropertyBlock floorBlock,rimBlock;
    public void Apply(LevelData level){
        if(level==null||level.theme==null)return;ThemeData t=level.theme;
        if(floorBlock==null)floorBlock=new MaterialPropertyBlock();
        if(rimBlock==null)rimBlock=new MaterialPropertyBlock();
        if(floor!=null){floorBlock.SetColor("_ColorA",t.floorA);floorBlock.SetColor("_ColorB",t.floorB);floorBlock.SetFloat("_Tiles",Mathf.Max(level.gridSize.x,level.gridSize.y));floor.SetPropertyBlock(floorBlock);}
        if(rim!=null){rimBlock.SetColor("_Color",t.rimColor);rim.SetPropertyBlock(rimBlock);}
        if(sceneCamera!=null)sceneCamera.backgroundColor=t.background;
        if(sun!=null)sun.color=t.lightColor;
        if(rails!=null&&rails.Length==4){float x=level.Size.x,z=level.Size.y;rails[0].localPosition=new Vector3(0,-0.08f,-z/2);rails[1].localPosition=new Vector3(0,-0.08f,z/2);rails[2].localPosition=new Vector3(-x/2,-0.08f,0);rails[3].localPosition=new Vector3(x/2,-0.08f,0);for(int i=0;i<4;i++)rails[i].localScale=i<2?new Vector3(x+0.3f,0.2f,0.3f):new Vector3(0.3f,0.2f,z+0.3f);}
        if(AudioManager.Instance!=null)AudioManager.Instance.SetMusic(t.music);
    }
}
