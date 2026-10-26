using System.Collections.Generic;
using UnityEngine;

public partial class ReferenceGame {
    Actor verificationTarget;float verificationArrival=-1,verificationFallHoldUntil=-1;bool verificationSizeFallback;int verificationProbe,verificationHeldProbe;Vector3 verificationHeldAim;
    readonly Dictionary<Mesh,Vector3[]> verificationVertices=new Dictionary<Mesh,Vector3[]>();
    readonly Dictionary<Actor,Vector3> verificationSupportCenters=new Dictionary<Actor,Vector3>();
    readonly List<Vector3> verificationSupportPoints=new List<Vector3>();int verificationSupportFrame=-1;
    public int VerificationInputStepForTest=>verificationTarget==null?-1:actors.IndexOf(verificationTarget)*16+verificationProbe;
    readonly Dictionary<Actor,float> verificationRevisit=new Dictionary<Actor,float>();
    readonly Dictionary<Actor,int> verificationFailedAtCount=new Dictionary<Actor,int>();
    void ResetVerificationTarget(){verificationTarget=null;verificationArrival=verificationFallHoldUntil=-1;verificationSizeFallback=false;verificationRevisit.Clear();verificationFailedAtCount.Clear();verificationSupportCenters.Clear();}

    // This only chooses test input positions. It never changes an actor, gravity,
    // contacts or collection. Human input continues through the ordinary joystick.
    Vector3 FindVerificationTarget(){
        testBombs.Clear();foreach(var a in actors)if(a.model.id==-1&&a.state!=2)testBombs.Add(a);
        // Test teleports can outrun the source collection volume. Wait for genuinely
        // falling nearby bodies; do not move, collect or otherwise alter those bodies.
        foreach(var body in physicalBodies){var item=body.actor;if(item==null||item.model.id<0||item.state!=0||body.collectorEntered||body.rigid.isKinematic)continue;
            if(item.Top<.15f&&item.Top>-3.7f&&body.rigid.linearVelocity.y<-.1f&&(new Vector2(item.pos.x-HolePosition.x,item.pos.z-HolePosition.z)).sqrMagnitude<100){verificationProbe=15;return HolePosition;}}
        if(verificationTarget!=null){
            var a=verificationTarget;
            if(a.state!=0||(a.body!=null&&a.body.collectorEntered)){verificationTarget=null;verificationArrival=-1;}
            else {
                if(a.body!=null&&a.Bottom<-.15f&&a.Top>-3.7f&&a.body.rigid.linearVelocity.y<-.5f)verificationFallHoldUntil=simulatedPhysicsTime+.35f;
                if(simulatedPhysicsTime<verificationFallHoldUntil){verificationProbe=15;return HolePosition;}
                if(VerificationAimIsSafe(verificationHeldAim)||TryVerificationStartAim(a,out verificationHeldAim,verificationSizeFallback)){
                float distance=Vector2.Distance(new Vector2(verificationHeldAim.x,verificationHeldAim.z),new Vector2(HolePosition.x,HolePosition.z));
                if(verificationArrival<0&&distance<.2f)verificationArrival=simulatedPhysicsTime;
                float elapsed=verificationArrival<0?0:simulatedPhysicsTime-verificationArrival;
                bool wide=NeedsWideVerificationProbe(a),tightCapsule=TryTightVerificationCapsule(a,out _);float hold=tightCapsule?2f:wide?1f:.175f,centerHold=tightCapsule?1.5f:.6f;int probes=wide?10:4;
                if(elapsed<centerHold+probes*hold){int probe=elapsed<centerHold?0:1+Mathf.Min(probes-1,Mathf.FloorToInt((elapsed-centerHold)/hold));verificationProbe=probe;if(probe!=verificationHeldProbe){verificationHeldProbe=probe;if(TryVerificationAim(a,probe,out var aim,verificationSizeFallback))verificationHeldAim=aim;}return verificationHeldAim;}
                // A supported or wedged item needs surrounding pieces removed first.
                verificationRevisit[a]=simulatedPhysicsTime+2;verificationFailedAtCount[a]=Collected;verificationTarget=null;verificationArrival=-1;
                }else {verificationTarget=null;verificationArrival=-1;}
            }
        }
        Actor best=null;Vector3 bestAim=HolePosition;float score=float.MaxValue;
        bool bestFallback=false;
        // Size estimates are only a search priority, never a verdict on 3D reachability.
        // Once normal candidates are exhausted, attempt the remaining shapes physically.
        for(int pass=0;pass<2&&best==null;pass++)foreach(var a in actors){
            if(a.state!=0||a.model.id<0||(a.body!=null&&a.body.collectorEntered))continue;
            if(verificationRevisit.TryGetValue(a,out float retryAt)&&retryAt>simulatedPhysicsTime)continue;
            if(verificationFailedAtCount.TryGetValue(a,out int failedAt)&&failedAt==Collected)continue;
            if(!TryVerificationStartAim(a,out var aim,pass==1))continue;
            float candidate=(aim-HolePosition).sqrMagnitude*(Needed(a.model.id)?1:1.3f);
            if(a.Bottom>.20f)candidate+=10000;
            if(pass==1)candidate+=Mathf.Max(0,SourceFitRadius(a)-SourceApertureRadius)*100000;
            if(candidate<score){score=candidate;best=a;bestAim=aim;bestFallback=pass==1;}
        }
        // Exhaustion ends one exploration pass, not the stage: a previous attempt may
        // have changed a body's orientation without collecting anything yet.
        if(best==null)verificationFailedAtCount.Clear();
        verificationTarget=best;verificationArrival=verificationFallHoldUntil=-1;verificationProbe=verificationHeldProbe=0;verificationHeldAim=bestAim;verificationSizeFallback=bestFallback;return bestAim;
    }
    bool VerificationAimIsSafe(Vector3 aim){if(!CanStandAt(aim))return false;foreach(var bomb in testBombs){var d=aim-bomb.pos;d.y=0;if(d.magnitude<SourceApertureRadius+SourceFitRadius(bomb)+.099f)return false;}return true;}
    public bool VerificationFallingProgressForTest=>verificationTarget!=null&&verificationTarget.body!=null&&verificationTarget.Bottom<-.15f&&verificationTarget.Top>-3.7f&&verificationTarget.body.rigid.linearVelocity.y<-.5f;
    Bounds VerificationBounds(Actor actor){
        Bounds bounds=default;bool has=false;
        if(actor.body!=null)foreach(var col in actor.body.colliders){if(!col.enabled||!col.gameObject.activeInHierarchy)continue;if(!has){bounds=col.bounds;has=true;}else bounds.Encapsulate(col.bounds);}
        if(!has){bounds=WorldBounds(actor.model.bounds,actor.rotation,actor.scale);bounds.center+=actor.pos;}
        return bounds;
    }
    bool NeedsWideVerificationProbe(Actor actor){var bounds=VerificationBounds(actor);return new Vector2(bounds.extents.x,bounds.extents.z).magnitude>SourceApertureRadius||SourceFitRadius(actor)>SourceApertureRadius;}
    Vector3 VerificationSupportCenter(Actor actor){
        if(verificationSupportFrame!=Time.frameCount){verificationSupportFrame=Time.frameCount;verificationSupportCenters.Clear();}
        if(verificationSupportCenters.TryGetValue(actor,out var cached))return cached;
        var result=VerificationBounds(actor).center;verificationSupportPoints.Clear();
        if(actor.body!=null)foreach(var col in actor.body.colliders){if(!col.enabled||!col.gameObject.activeInHierarchy)continue;
            if(col is MeshCollider mesh){if(!verificationVertices.TryGetValue(mesh.sharedMesh,out var vertices)){vertices=mesh.sharedMesh.vertices;verificationVertices[mesh.sharedMesh]=vertices;}foreach(var v in vertices)verificationSupportPoints.Add(col.transform.TransformPoint(v));}
            else if(col is BoxCollider box){for(int i=0;i<8;i++)verificationSupportPoints.Add(col.transform.TransformPoint(box.center+Vector3.Scale(box.size*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));}
            else if(col is CapsuleCollider capsule){var axis=capsule.direction==0?Vector3.right:capsule.direction==1?Vector3.up:Vector3.forward;float half=Mathf.Max(0,capsule.height*.5f-capsule.radius);float radius=capsule.radius*Mathf.Max(Mathf.Abs(col.transform.lossyScale.x),Mathf.Abs(col.transform.lossyScale.z));verificationSupportPoints.Add(col.transform.TransformPoint(capsule.center+axis*half)-Vector3.up*radius);verificationSupportPoints.Add(col.transform.TransformPoint(capsule.center-axis*half)-Vector3.up*radius);}
            else verificationSupportPoints.Add(new Vector3(col.bounds.center.x,col.bounds.min.y,col.bounds.center.z));
        }
        if(verificationSupportPoints.Count>0){float lowest=float.PositiveInfinity;foreach(var p in verificationSupportPoints)lowest=Mathf.Min(lowest,p.y);Bounds support=default;bool has=false;foreach(var p in verificationSupportPoints)if(p.y<=lowest+.035f){if(!has){support=new Bounds(p,Vector3.zero);has=true;}else support.Encapsulate(p);}if(has)result=support.center;}
        result.y=0;verificationSupportCenters[actor]=result;return result;
    }
    bool VerificationCapsuleEnd(Actor actor,int end,out Vector3 position){position=default;if(actor.body==null)return false;foreach(var col in actor.body.colliders)if(col.enabled&&col is CapsuleCollider c){var axis=c.direction==0?Vector3.right:c.direction==1?Vector3.up:Vector3.forward;position=c.transform.TransformPoint(c.center+axis*Mathf.Max(0,c.height*.5f-c.radius)*(end==0?1:-1));position.y=0;return true;}return false;}
    static float VerificationCapsuleWorldRadius(CapsuleCollider capsule){var scale=capsule.transform.lossyScale;return capsule.radius*(capsule.direction==0?Mathf.Max(Mathf.Abs(scale.y),Mathf.Abs(scale.z)):capsule.direction==1?Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z)):Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y)));}
    bool TryTightVerificationCapsule(Actor actor,out CapsuleCollider capsule){
        capsule=null;if(actor.body==null)return false;int count=0;foreach(var col in actor.body.colliders)if(col.enabled&&col.gameObject.activeInHierarchy){count++;capsule=col as CapsuleCollider;}
        if(count!=1||capsule==null)return false;float radius=VerificationCapsuleWorldRadius(capsule);return radius>SourceApertureRadius*.7f&&radius<SourceApertureRadius;
    }
    // Input geometry only: a tipped, nearly aperture-wide capsule must keep its minor
    // axis centered. Large world-cardinal probes repeatedly push it back onto the rim.
    static Vector3 CapsuleVerificationAim(CapsuleCollider capsule,float aperture,int probe){
        var center=capsule.transform.TransformPoint(capsule.center);center.y=0;
        var localAxis=capsule.direction==0?Vector3.right:capsule.direction==1?Vector3.up:Vector3.forward;
        var segment=capsule.transform.TransformVector(localAxis*Mathf.Max(0,capsule.height*.5f-capsule.radius));segment.y=0;
        if(probe==0||segment.sqrMagnitude<.0001f)return center;
        float small=Mathf.Min(.2f,Mathf.Max(.04f,aperture-VerificationCapsuleWorldRadius(capsule))),half=segment.magnitude;
        int pair=(probe-1)/2;float offset=pair==0?small*.5f:pair==1?small:pair==2?half*.25f:pair==3?half*.5f:half;
        return center+segment.normalized*offset*(probe%2==1?1:-1);
    }
    public Vector3 VerificationCenterForTest(int actorIndex)=>VerificationSupportCenter(actors[actorIndex]);
    bool TryVerificationStartAim(Actor actor,out Vector3 aim,bool allowSizeFallback=false){
        if(TryVerificationAim(actor,0,out aim,allowSizeFallback))return true;
        // Centering can be blocked by a wall or bomb while an offset catch remains legal.
        int probes=NeedsWideVerificationProbe(actor)?10:4;
        for(int probe=1;probe<=probes;probe++)if(TryVerificationAim(actor,probe,out aim,allowSizeFallback))return true;
        return false;
    }
    bool TryVerificationAim(Actor actor,int probe,out Vector3 aim,bool allowSizeFallback=false){
        bool tightCapsule=TryTightVerificationCapsule(actor,out var capsule);var center=tightCapsule?CapsuleVerificationAim(capsule,SourceApertureRadius,0):VerificationSupportCenter(actor);aim=center;float fit=SourceFitRadius(actor);
        // Native EffectiveFitRadius selects collider modes; it is not a swallow gate.
        // A rectangular near-fit body can tip through despite its circumscribed circle.
        // The test planner may attempt it when its narrower horizontal span fits;
        // actual floor contacts and gravity alone determine whether it falls.
        var bounds=VerificationBounds(actor);
        if(!allowSizeFallback&&fit>SourceApertureRadius&&Mathf.Min(bounds.extents.x,bounds.extents.z)>SourceApertureRadius)return false;
        bool wide=NeedsWideVerificationProbe(actor);
        float probeFraction=probe>4?.85f:.55f;
        float maximumOffset=probe>0&&wide?Mathf.Max(SourceApertureRadius*probeFraction,SourceApertureRadius-fit-.01f):Mathf.Max(.05f,SourceApertureRadius-fit)-.01f;
        // Exhausted-candidate fallback may expose only part of a support footprint.
        // This remains input selection: the unchanged rim decides whether it can fall.
        if(allowSizeFallback)maximumOffset=Mathf.Max(maximumOffset,SourceApertureRadius-Mathf.Min(fit*.2f,SourceApertureRadius*.1f));
        // Long authored objects need one supported edge exposed before gravity can tip them.
        // These are hole positions only, with no actor force or forced collection.
        if(tightCapsule){aim=CapsuleVerificationAim(capsule,SourceApertureRadius,probe);maximumOffset=Mathf.Max(maximumOffset,(aim-center).magnitude+.001f);}
        else if(probe>8){if(!VerificationCapsuleEnd(actor,probe-9,out aim))return false;maximumOffset=Mathf.Max(maximumOffset,(aim-center).magnitude+.001f);}
        else if(probe>0){float nudge=wide?SourceApertureRadius*probeFraction:Mathf.Min(SourceApertureRadius*.23f,Mathf.Max(0,SourceApertureRadius-fit-.04f));float angle=(probe>4?(probe-5)*.5f+.25f:(probe-1)*.5f)*Mathf.PI;aim+=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*nudge;}
        foreach(var bomb in testBombs){var away=aim-bomb.pos;away.y=0;float safe=SourceApertureRadius+SourceFitRadius(bomb)+.1f;if(away.magnitude<safe)aim+=(away.sqrMagnitude>.0001f?away.normalized:Vector3.right)*(safe-away.magnitude);}
        aim.x=Mathf.Clamp(aim.x,min.x+Radius,max.x-Radius);aim.z=Mathf.Clamp(aim.z,min.y+Radius,max.y-Radius);
        if(!CanStandAt(aim)&&!ProjectVerificationAim(center,aim,maximumOffset,out aim))return false;
        foreach(var bomb in testBombs){var separation=aim-bomb.pos;separation.y=0;float safe=SourceApertureRadius+SourceFitRadius(bomb)+.1f;if(separation.magnitude<safe-.001f)return false;}
        if(tightCapsule){var axis=CapsuleVerificationAim(capsule,SourceApertureRadius,9)-center;axis.y=0;var offset=aim-center;if(axis.sqrMagnitude>.0001f)offset-=Vector3.Project(offset,axis);if(offset.magnitude>(SourceApertureRadius-VerificationCapsuleWorldRadius(capsule))*.5f+.001f)return false;}
        return (aim-center).magnitude<=maximumOffset+.0001f;
    }
    public bool TryProbeAimForTest(int actorIndex,int probe,out Vector3 aim)=>TryVerificationAim(actors[actorIndex],probe,out aim);
    public bool TryBoundaryAimForTest(Vector3 desired,float objectRadius,out Vector3 aim)=>ProjectVerificationAim(desired,desired,SourceApertureRadius-objectRadius-.01f,out aim);
    bool ProjectVerificationAim(Vector3 itemCenter,Vector3 preferred,float maximumOffset,out Vector3 result){
        result=preferred;if(CanStandAt(preferred))return true;
        float best=float.PositiveInfinity,reachSquared=maximumOffset*maximumOffset;bool found=false;
        foreach(var shape in authoredPlayground){
            var tile=shape.local;var local=shape.inverse.MultiplyPoint3x4(preferred);float insetX=(Radius+.005f)/Mathf.Abs(shape.scale.x),insetZ=(Radius+.005f)/Mathf.Abs(shape.scale.z);
            float lowX=tile.xMin+insetX,highX=tile.xMax-insetX,lowZ=tile.yMin+insetZ,highZ=tile.yMax-insetZ;
            float x=lowX<=highX?Mathf.Clamp(local.x,lowX,highX):local.x>=tile.center.x?highX:lowX;
            float z=lowZ<=highZ?Mathf.Clamp(local.z,lowZ,highZ):local.z>=tile.center.y?highZ:lowZ;
            // Preserve the other coordinate across adjacent tiles; a tile seam is not a wall.
            for(int option=0;option<3;option++){
                var candidate=shape.matrix.MultiplyPoint3x4(new Vector3(option==1?local.x:x,0,option==0?local.z:z));candidate.y=0;
                if((candidate-itemCenter).sqrMagnitude>reachSquared)continue;
                float distance=(candidate-preferred).sqrMagnitude;if(distance>=best||!CanStandAt(candidate))continue;
                result=candidate;best=distance;found=true;
            }
        }
        return found;
    }
}
