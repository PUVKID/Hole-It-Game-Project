using System;
using UnityEngine;
[Serializable] public class ReferencePart { public Mesh mesh; public Material material; public int submesh; public Matrix4x4 local=Matrix4x4.identity; }
[Serializable] public class ReferenceModel { public int id; public string label; public Sprite icon; public Bounds bounds; public ReferencePart[] parts; }
[Serializable] public class ReferencePoint { public int id; public float[] position,rotation; public float scale,points=1; }
[Serializable] public class ReferenceEnvironment { public int kind; public float[] position,rotation,scale; }
[Serializable] public class ReferenceGoal { public int id,amount; }
[Serializable] public class ReferenceLevel { public int index,time,bombs,difficulty,goalType; public string source; public ReferenceGoal[] goals; public ReferencePoint[] points; public ReferenceEnvironment[] environment; }
[Serializable] public class ReferenceLevels { public ReferenceLevel[] levels; }
public class ReferenceCatalog:ScriptableObject { public ReferenceModel[] models,environments; public ReferenceModel bomb; public ReferenceLevel[] levels; public GameObject holeVisual; public Material groundMaterial; public Font font; public AudioClip[] sounds; }

/// <summary>A floating joystick: displacement chooses velocity, never a destination.</summary>
public class FloatingStick {
    public Vector2 Origin {get;private set;} public Vector2 Value {get;private set;} public bool Held {get;private set;}
    public float Radius=56,DeadZone=.06f;
    public void Begin(Vector2 screen){Origin=screen;Held=true;Value=Vector2.zero;}
    public void Move(Vector2 screen){if(!Held)return;Vector2 d=(screen-Origin)/Mathf.Max(1,Radius);float length=d.magnitude;Value=length<DeadZone?Vector2.zero:d/length*Mathf.Clamp01((length-DeadZone)/(1-DeadZone));}
    public void Release(){Held=false;Value=Vector2.zero;}
}
