using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 프리팹별 오브젝트 풀. 레벨 시작 시 Prewarm 하고 이후에는 Instantiate/Destroy 없이 재사용한다.
/// 풀이 부족할 때만 경고 후 1개씩 확장한다.
/// </summary>
public class ObjectPooler : MonoBehaviour
{
    private class Pool
    {
        public GameObject prefab;
        public readonly Stack<GameObject> free = new Stack<GameObject>(64);
        public int total;
    }

    [SerializeField] private Transform poolRoot;

    private readonly Dictionary<GameObject, Pool> pools = new Dictionary<GameObject, Pool>(16);
    private readonly Dictionary<GameObject, Pool> owner = new Dictionary<GameObject, Pool>(256);

    public static ObjectPooler Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        if (poolRoot == null) poolRoot = transform;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>필요 수량만큼 미리 만들어 둔다 (로딩 시점에만 호출).</summary>
    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null) return;
        Pool p = GetPool(prefab);
        while (p.total < count) Create(p);
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;
        Pool p = GetPool(prefab);
        if (p.free.Count == 0)
        {
            Debug.LogWarning("[ObjectPooler] 풀 부족, 확장: " + prefab.name);
            Create(p);
        }
        GameObject go = p.free.Pop();
        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);
        return go;
    }

    public void Return(GameObject go)
    {
        if (go == null) return;
        Pool p;
        if (!owner.TryGetValue(go, out p))
        {
            go.SetActive(false);
            return;
        }
        go.SetActive(false);
        go.transform.SetParent(poolRoot, false);
        p.free.Push(go);
    }

    /// <summary>활성 중인 풀 오브젝트를 모두 반환 (레벨 전환 시).</summary>
    public void ReturnAllActive()
    {
        foreach (KeyValuePair<GameObject, Pool> kv in owner)
        {
            GameObject go = kv.Key;
            if (go != null && go.activeSelf) Return(go);
        }
    }

    private Pool GetPool(GameObject prefab)
    {
        Pool p;
        if (!pools.TryGetValue(prefab, out p))
        {
            p = new Pool { prefab = prefab };
            pools.Add(prefab, p);
        }
        return p;
    }

    private void Create(Pool p)
    {
        GameObject go = Instantiate(p.prefab, poolRoot);
        go.SetActive(false);
        p.total++;
        p.free.Push(go);
        owner.Add(go, p);
    }
}
