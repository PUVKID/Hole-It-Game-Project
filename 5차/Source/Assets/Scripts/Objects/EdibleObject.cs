using UnityEngine;
/// <summary>Kinematic attraction and below-floor fall; rewards are claimed exactly once per spawn.</summary>
public class EdibleObject : MonoBehaviour
{
    [SerializeField] private EdibleData data;
    public EdibleData Data {get{return data;}}
    public SwallowState State {get;private set;}
    public bool LockedDecoration {get;private set;}
    public float FootprintRadius {get;private set;}
    public float BottomHeight { get { Cache();float y=float.MaxValue;foreach(var col in cols)if(col.enabled)y=Mathf.Min(y,col.bounds.min.y);return y==float.MaxValue?tr.position.y:y; } }
    private Transform tr, hole;
    private Rigidbody body;
    private Collider[] cols;
    private Vector3 baseScale, startScale, fallStart;
    private Quaternion baseRotation;
    private float elapsed;
    private int session;
    private bool cached,reported;
    private static readonly Collider[] wakeBuffer=new Collider[128];
    private void WakeStack(){
        int count=Physics.OverlapBoxNonAlloc(tr.position+Vector3.up*4,new Vector3(1.5f,5,1.5f),wakeBuffer,Quaternion.identity,Layers.EdibleMask,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++){var rb=wakeBuffer[i].attachedRigidbody;if(rb!=null&&!rb.isKinematic)rb.WakeUp();wakeBuffer[i]=null;}
    }
    private void Awake(){Cache();}
    private void Cache(){if(cached)return;tr=transform;body=GetComponent<Rigidbody>();cols=GetComponentsInChildren<Collider>(true);baseScale=tr.localScale;cached=true;}
    public void Init(EdibleData d,bool decoration) {
        Cache(); data=d; LockedDecoration=decoration; State=SwallowState.Normal;elapsed=0;hole=null;reported=false;
        session=GameManager.Instance!=null?GameManager.Instance.SessionId:0;
        tr.localScale=baseScale; baseRotation=tr.rotation;
        FootprintRadius=d!=null?Mathf.Max(0.01f,d.footprintRadius)*Mathf.Max(Mathf.Abs(tr.lossyScale.x),Mathf.Abs(tr.lossyScale.z)):0.5f;
        SetLayer(tr,Layers.Edible);for(int i=0;i<cols.Length;i++)cols[i].enabled=true;
        if(body!=null){if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}body.isKinematic=true;body.useGravity=false;}
    }
    public bool CanBeEatenBy(int tier,float radius,float maxRatio){return data!=null && data.canBeSwallowed && !LockedDecoration && State==SwallowState.Normal && data.sizeTier<=tier && FootprintRadius<=radius*maxRatio;}
    public void EnableStackPhysics(){if(body==null)return;body.isKinematic=false;body.useGravity=true;body.linearDamping=.4f;body.angularDamping=2;body.solverIterations=10;body.solverVelocityIterations=4;body.interpolation=RigidbodyInterpolation.Interpolate;body.constraints=RigidbodyConstraints.FreezeRotation;}
    private void FixedUpdate(){if(State==SwallowState.Normal&&body!=null&&!body.isKinematic&&body.constraints!=RigidbodyConstraints.None&&body.linearVelocity.y<-.9f){body.constraints=RigidbodyConstraints.None;body.angularVelocity=new Vector3(.3f,.1f,.25f);}}
    public void BeginAttract(Transform target){if(State!=SwallowState.Normal||target==null)return;State=SwallowState.Attracted;hole=target;elapsed=0;startScale=tr.localScale;baseRotation=tr.rotation;if(body!=null){body.isKinematic=true;body.useGravity=false;body.constraints=RigidbodyConstraints.None;}if(AudioManager.Instance!=null)AudioManager.Instance.PlayAttract();}
    public bool Tick(float dt,GameSettings settings,float holeRadius){
        if(!gameObject.activeSelf || State==SwallowState.Consumed)return false;
        if(hole==null){ForceReset();if(ObjectPooler.Instance!=null)ObjectPooler.Instance.Return(gameObject);return false;}
        if(dt<=0)return true;
        elapsed+=dt;
        if(State==SwallowState.Attracted){
            // Entry is decided by the mouth. Once entered, drop at this fixed world position.
            // Do not chase the moving hole or pull objects sideways through their neighbours.
            State=SwallowState.Falling;elapsed=0;fallStart=tr.position;SetLayer(tr,Layers.Falling);for(int i=0;i<cols.Length;i++)cols[i].enabled=false;WakeStack();
        }else if(State==SwallowState.Falling){
            float t=Mathf.Clamp01(elapsed/Mathf.Max(0.2f,settings.swallowDuration));
            Vector3 p=fallStart;p.y=fallStart.y-4.5f*t*t;tr.position=p;
            tr.rotation=Quaternion.AngleAxis(12*t,Vector3.right)*baseRotation;tr.localScale=baseScale;
            if(t>=1)Consume();
        }
        return State!=SwallowState.Consumed;
    }
    public void Consume(){if(State!=SwallowState.Falling)return;State=SwallowState.Consumed;if(GameManager.Instance!=null)GameManager.Instance.OnEdibleConsumed(this);if(ObjectPooler.Instance!=null)ObjectPooler.Instance.Return(gameObject);else gameObject.SetActive(false);}
    public bool TryClaimReward(int currentSession){if(reported||State!=SwallowState.Consumed||session!=currentSession||data==null)return false;reported=true;return true;}
    public void ForceReset(){if((State==SwallowState.Attracted||State==SwallowState.Falling)&&GameManager.Instance!=null)GameManager.Instance.CancelSwallow(data);State=SwallowState.Consumed;reported=true;hole=null;elapsed=0;if(body!=null){body.isKinematic=true;body.useGravity=false;}if(cached)tr.localScale=baseScale;}
    private static void SetLayer(Transform t,int layer){t.gameObject.layer=layer;for(int i=0;i<t.childCount;i++)SetLayer(t.GetChild(i),layer);}
}
