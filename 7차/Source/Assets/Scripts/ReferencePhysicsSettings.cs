using System.Collections.Generic;
using UnityEngine;

/// <summary>Recovered Hole it 1.31.17 release settings. See Audit/RuntimePhysicsNative.txt.</summary>
public static class ReferencePhysicsSettings {
    // LevelManager.OnEnable uses default_gravity in release, not the serialized scene -5.81
    // or the 40 stored by the debug smooth-physics preset.
    public const float GravityMagnitude=35f,FixedStep=.02f,MaximumDeltaTime=.33333334f;
    public const float ItemMass=1,LinearDamping=0,AngularDamping=1;
    public const float ContactOffset=.01f,SleepThreshold=.005f,CollectableSleepThreshold=0,MaximumAngularSpeed=7,MaximumDepenetrationSpeed=10;
    public const int SolverIterations=6,SolverVelocityIterations=1;
    public const float InitialRestDelay=1.5f,ObstacleRestDelay=2f;
    static readonly Dictionary<Mesh,Vector3[]> sourceVertices=new Dictionary<Mesh,Vector3[]>();

    public static void ApplyGlobalSettings(){
        Physics.gravity=new Vector3(0,-GravityMagnitude,0);
        Physics.bounceThreshold=2;Physics.defaultMaxDepenetrationVelocity=MaximumDepenetrationSpeed;
        Physics.sleepThreshold=SleepThreshold;Physics.defaultContactOffset=ContactOffset;
        Physics.defaultSolverIterations=SolverIterations;Physics.defaultSolverVelocityIterations=SolverVelocityIterations;
        Physics.defaultMaxAngularSpeed=MaximumAngularSpeed;Physics.queriesHitBackfaces=false;Physics.queriesHitTriggers=true;
        Physics.autoSyncTransforms=false;Physics.reuseCollisionCallbacks=true;
        Time.fixedDeltaTime=FixedStep;Time.maximumDeltaTime=MaximumDeltaTime;
    }
    public static float Mass(ReferencePoint point)=>point!=null&&point.massOverride>0?point.massOverride:ItemMass;
    public static float RestDelay(int modelId)=>modelId<0?ObstacleRestDelay:InitialRestDelay;
    // Obstacle.Awake and the release item smooth preset both replace prefab damping.
    public static void ApplyRuntimeDamping(Rigidbody body){body.linearDamping=LinearDamping;body.angularDamping=AngularDamping;}
    public static float GrowthValue(ReferencePoint point,ReferenceModel model)=>point.points<0?0:point.points==0?model.objectSize:point.points;
    public static bool InitiallyDynamic(ReferencePoint point,int id)=>point!=null&&point.physicsMode==1&&id!=59;

    // CollectableManager.HoleSizeToGetInside, GetRadius, IsLongObjectDynamic and
    // GetEffectiveRadiusForLongObject use the root MeshFilter, not the combined render bounds.
    public static float EffectiveFitRadius(ReferenceModel model,Quaternion rotation,float scale){
        var mesh=model.holeSizeMesh;if(mesh==null)return -1;
        var size=mesh.bounds.size;float smallest=Mathf.Min(size.x,Mathf.Min(size.y,size.z));
        float largest=Mathf.Max(size.x,Mathf.Max(size.y,size.z));float middle=size.x+size.y+size.z-smallest-largest;
        bool isLong=largest/Mathf.Max(.000001f,(smallest+middle)/1.75f)>=1.5f;
        float radius=0;
        if(isLong)radius=(Mathf.Min(size.x,size.z)+Mathf.Max(size.x,size.z)*.2f)*.5f*scale;
        else {
            if(!sourceVertices.TryGetValue(mesh,out var vertices)){vertices=mesh.vertices;sourceVertices[mesh]=vertices;}
            var center=mesh.bounds.center;
            foreach(var v in vertices){var delta=rotation*((v-center)*scale);radius=Mathf.Max(radius,Mathf.Sqrt(delta.x*delta.x+delta.z*delta.z));}
        }
        return radius*(1-model.maxRadiusReductionRate);
    }
    public static int FitTier(float radius){
        for(int tier=0;tier<=20;tier++){
            float opening=tier==0?.325f:(.7f+(tier-1)*.25f)*1.55f*.5f;
            if(opening>radius)return Mathf.Max(1,tier);
        }
        return 1; // Native returns -1 when none fits; AssignHoleSize clamps it to 1.
    }
    public static int ColliderSwitchTier(ReferencePoint point,ReferenceModel model,Quaternion rotation,float scale){
        if(point!=null&&point.holeColliderActive==1&&point.holeColliderLevel!=0)return point.holeColliderLevel;
        return FitTier(EffectiveFitRadius(model,rotation,scale));
    }
    public static int ColliderGroup(ReferencePoint point,ReferenceModel model,int switchTier,int holeTier){
        int groups=0;if(model.collisionParts!=null)foreach(var shape in model.collisionParts)groups|=shape.group;
        if(point!=null&&point.specialCollision==1&&(groups&4)!=0)return 4;
        if((groups&1)!=0)return switchTier>holeTier?1:2;
        return (groups&2)!=0?2:0;
    }
    public static bool ColliderEnabled(ReferenceCollision shape,int selectedGroup)=>shape.group==0?shape.sourceEnabled:(shape.group&selectedGroup)!=0;
}
