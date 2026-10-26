using System.Text;
using UnityEngine;

public partial class ReferenceGame {
    public string VerificationDiagnosticForTest(){
        var s=new StringBuilder();s.Append("sim=").Append(simulatedPhysicsTime.ToString("F2")).Append(" collected=").Append(Collected).Append(" hole=").Append(HolePosition).Append(" aperture=").Append(SourceApertureRadius.ToString("F3")).Append(" active=").Append(ActivePhysicsBodies);
        var a=verificationTarget;if(a==null)return s.Append(" target=none").ToString();
        s.Append(" target=").Append(actors.IndexOf(a)).Append('/').Append(a.model.id).Append('/').Append(a.model.label).Append(" pivot=").Append(a.pos).Append(" fit=").Append(SourceFitRadius(a).ToString("F3")).Append(" bottom=").Append(a.Bottom.ToString("F3"));
        var b=a.body;if(b==null)return s.Append(" body=none").ToString();
        s.Append(" kinematic=").Append(b.rigid.isKinematic).Append(" velocity=").Append(b.rigid.linearVelocity).Append(" angular=").Append(b.rigid.angularVelocity).Append(" group=").Append(b.selectedGroup).Append(" switch=").Append(b.switchTier).Append(" latch=").Append(b.collectorEntered).Append(" initial=").Append(b.initialSettling);
        Bounds bounds=default;bool has=false;int enabled=0;foreach(var col in b.colliders){if(!col.enabled||!col.gameObject.activeInHierarchy)continue;enabled++;if(!has){bounds=col.bounds;has=true;}else bounds.Encapsulate(col.bounds);}
        if(has)s.Append(" activeBoundsCenter=").Append(bounds.center).Append(" size=").Append(bounds.size);
        s.Append(" enabled=").Append(enabled).Append(" detection=").Append(IntersectsSourceDetection(b));
        var hits=new RaycastHit[32];int count=physicsScene.Raycast(new Vector3(a.pos.x,20,a.pos.z),Vector3.down,hits,40,~0,QueryTriggerInteraction.Ignore);float floorY=float.NaN;
        for(int i=0;i<count;i++)if(hits[i].collider==physicalFloor||hits[i].collider==physicalInnerFloor)if(float.IsNaN(floorY)||hits[i].point.y>floorY)floorY=hits[i].point.y;
        s.Append(" floorRayY=").Append(float.IsNaN(floorY)?"aperture":floorY.ToString("F3"));return s.ToString();
    }
}
