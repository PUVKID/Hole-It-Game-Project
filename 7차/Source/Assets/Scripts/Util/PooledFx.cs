using UnityEngine;
public class PooledFx : MonoBehaviour
{
    [SerializeField] private ParticleSystem ps;
    [SerializeField] private float lifetime=0.85f;
    [SerializeField] private Transform pulse;
    [SerializeField] private Transform[] motes;
    private float timer;
    private void Awake(){if(ps==null)ps=GetComponentInChildren<ParticleSystem>();}
    private void OnEnable(){timer=Mathf.Max(0.05f,lifetime);if(pulse!=null)pulse.localScale=Vector3.one*0.2f;if(motes!=null)for(int i=0;i<motes.Length;i++)if(motes[i]!=null){motes[i].localPosition=Vector3.zero;motes[i].localScale=Vector3.one*0.12f;}if(ps!=null){ps.Clear(true);ps.Play(true);}}
    private void OnDisable(){if(ps!=null)ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
    private void Update(){
        timer-=Time.deltaTime;float t=1-Mathf.Clamp01(timer/Mathf.Max(0.05f,lifetime));
        if(pulse!=null){pulse.localScale=Vector3.one*Mathf.Lerp(0.2f,1.8f,t);pulse.localPosition=new Vector3(0,t*0.18f,0);}
        if(motes!=null)for(int i=0;i<motes.Length;i++)if(motes[i]!=null){float angle=i*2.399963f;float travel=(0.45f+(i%3)*0.2f)*t;motes[i].localPosition=new Vector3(Mathf.Cos(angle)*travel,Mathf.Sin(t*Mathf.PI)*(0.5f+(i%2)*0.3f),Mathf.Sin(angle)*travel);motes[i].localScale=Vector3.one*(0.13f*(1-t));motes[i].localRotation=Quaternion.Euler(t*260,i*47+t*150,0);}
        if(timer<=0&&ObjectPooler.Instance!=null)ObjectPooler.Instance.Return(gameObject);
    }
    public static void Spawn(GameObject prefab,Vector3 position,float scale){if(prefab==null||ObjectPooler.Instance==null)return;GameObject go=ObjectPooler.Instance.Get(prefab,position,Quaternion.identity);if(go!=null)go.transform.localScale=Vector3.one*Mathf.Clamp(scale,0.1f,8);}
}
