using System.Collections.Generic;
using UnityEngine;
[DefaultExecutionOrder(-300)]
public class ObjectPooler : MonoBehaviour
{
    private class Pool { public GameObject prefab; public readonly Stack<GameObject> free=new Stack<GameObject>(64); public int total,active,peak; }
    [SerializeField] private Transform poolRoot;
    [SerializeField,Min(1)] private int maximumPerPool=512;
    private readonly Dictionary<GameObject,Pool> pools=new Dictionary<GameObject,Pool>(64);
    private readonly Dictionary<GameObject,Pool> owner=new Dictionary<GameObject,Pool>(1024);
    private readonly HashSet<GameObject> leased=new HashSet<GameObject>();
    public static ObjectPooler Instance { get; private set; }
    public int ActiveCount { get { return leased.Count; } }
    private void Awake(){Instance=this;if(poolRoot==null)poolRoot=transform;}
    private void OnDestroy(){if(Instance==this)Instance=null;}
    public void Prewarm(GameObject prefab,int count) {
        if(prefab==null) return;
        if(GameManager.Instance!=null && GameManager.Instance.State==GameState.Playing) throw new System.InvalidOperationException("Prewarm is only permitted during loading.");
        if(count<0 || count>maximumPerPool) throw new System.ArgumentOutOfRangeException("count",prefab.name+" exceeds pool limit");
        Pool p=GetPool(prefab); while(p.total<count) Create(p);
    }
    public GameObject Get(GameObject prefab,Vector3 position,Quaternion rotation) {
        if(prefab==null) return null;
        Pool p=GetPool(prefab);
        if(p.free.Count==0) {
            if((GameManager.Instance!=null && GameManager.Instance.State==GameState.Playing)||p.total>=maximumPerPool) return null;
            Create(p);
        }
        GameObject go=p.free.Pop(); leased.Add(go); p.active++; p.peak=Mathf.Max(p.peak,p.active);
        go.transform.SetParent(poolRoot,false); go.transform.localScale=p.prefab.transform.localScale;
        go.transform.SetPositionAndRotation(position,rotation); go.SetActive(true); return go;
    }
    public void Return(GameObject go) {
        if(go==null || !leased.Remove(go)) return; // Duplicate return must not duplicate stack entries.
        Pool p;if(!owner.TryGetValue(go,out p)) return;
        EdibleObject e=go.GetComponent<EdibleObject>();if(e!=null)e.ForceReset();
        go.SetActive(false);go.transform.SetParent(poolRoot,false);go.transform.localScale=p.prefab.transform.localScale;
        p.active=Mathf.Max(0,p.active-1);p.free.Push(go);
    }
    public void ReturnAllActive(){foreach(var kv in owner) if(kv.Key!=null && leased.Contains(kv.Key)) Return(kv.Key);}
    public string Usage(){var s=new System.Text.StringBuilder();foreach(var p in pools.Values)s.Append(p.prefab.name).Append(": ").Append(p.active).Append('/').Append(p.total).Append(" peak ").Append(p.peak).Append('\n');return s.ToString();}
    private Pool GetPool(GameObject prefab){Pool p;if(!pools.TryGetValue(prefab,out p)){p=new Pool{prefab=prefab};pools.Add(prefab,p);}return p;}
    private void Create(Pool p){GameObject go=Instantiate(p.prefab,poolRoot);go.SetActive(false);p.total++;p.free.Push(go);owner.Add(go,p);}
}
