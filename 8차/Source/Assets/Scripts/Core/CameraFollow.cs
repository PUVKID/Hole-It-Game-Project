using UnityEngine;
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private HoleController hole;
    [SerializeField] private float lookAhead=0.10f;
    [SerializeField] private float verticalOffset=0.4f;
    private Camera cam;private Transform tr;private float zoomMultiplier=1;private Vector3 smoothPos;private Vector2 half=new Vector2(8,8);
    private void Awake(){cam=GetComponent<Camera>();tr=transform;cam.orthographic=true;}
    public void ResetForLevel(LevelData level){
        if(level==null||hole==null)return;half=level.Size*0.5f;zoomMultiplier=Mathf.Clamp(level.cameraZoomMultiplier,0.7f,1.6f);
        cam.orthographicSize=DesiredSize();smoothPos=Focus();Apply(smoothPos);
    }
    private float DesiredSize(){return hole.SizeTable.GetCameraSizeForGrowth(hole.Growth)*zoomMultiplier;}
    private Vector3 Focus(){
        Vector3 p=hole.Position;Vector2 v=hole.Velocity;p.x+=v.x*lookAhead;p.z+=v.y*lookAhead+verticalOffset;
        float yaw=hole.Settings.cameraYaw*Mathf.Deg2Rad;
        float ex=cam.orthographicSize*Mathf.Max(0.2f,cam.aspect),ez=cam.orthographicSize/Mathf.Max(0.25f,Mathf.Sin(hole.Settings.cameraPitch*Mathf.Deg2Rad));
        float bx=Mathf.Abs(Mathf.Cos(yaw))*ex+Mathf.Abs(Mathf.Sin(yaw))*ez,bz=Mathf.Abs(Mathf.Sin(yaw))*ex+Mathf.Abs(Mathf.Cos(yaw))*ez;
        float lx=Mathf.Max(0,half.x-bx),lz=Mathf.Max(0,half.y-bz);
        p.x=Mathf.Clamp(p.x,-lx,lx);p.z=Mathf.Clamp(p.z,-lz,lz);return p;
    }
    private void LateUpdate(){if(hole==null)return;float dt=Time.deltaTime;cam.orthographicSize=Mathf.Lerp(cam.orthographicSize,DesiredSize(),1-Mathf.Exp(-hole.Settings.cameraZoomSmooth*dt));smoothPos=Vector3.Lerp(smoothPos,Focus(),1-Mathf.Exp(-hole.Settings.cameraSmooth*dt));Apply(smoothPos);}
    private void Apply(Vector3 focus){Quaternion rotation=Quaternion.Euler(hole.Settings.cameraPitch,hole.Settings.cameraYaw,0);tr.rotation=rotation;tr.position=focus-rotation*Vector3.forward*hole.Settings.cameraDistance;}
}
