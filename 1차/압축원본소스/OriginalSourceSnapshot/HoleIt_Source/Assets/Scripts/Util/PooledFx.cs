using UnityEngine;

/// <summary>
/// 풀링되는 파티클 프리팹에 붙인다. 활성화되면 재생하고 lifetime 후 풀로 자동 반환한다.
/// (파티클은 Swallow / Growth / Mission Complete / Level Clear 4종만 사용)
/// </summary>
public class PooledFx : MonoBehaviour
{
    [SerializeField] private ParticleSystem ps;
    [SerializeField] private float lifetime = 1.2f;

    private float timer;

    private void Awake()
    {
        if (ps == null) ps = GetComponentInChildren<ParticleSystem>();
    }

    private void OnEnable()
    {
        timer = lifetime;
        if (ps != null) { ps.Clear(true); ps.Play(true); }
    }

    private void Update()
    {
        timer -= Time.unscaledDeltaTime;
        if (timer <= 0f && ObjectPooler.Instance != null) ObjectPooler.Instance.Return(gameObject);
    }

    /// <summary>풀에서 꺼내 위치에 재생하는 헬퍼.</summary>
    public static void Spawn(GameObject prefab, Vector3 position, float scale)
    {
        if (prefab == null || ObjectPooler.Instance == null) return;
        GameObject go = ObjectPooler.Instance.Get(prefab, position, Quaternion.identity);
        go.transform.localScale = Vector3.one * scale;
    }
}
