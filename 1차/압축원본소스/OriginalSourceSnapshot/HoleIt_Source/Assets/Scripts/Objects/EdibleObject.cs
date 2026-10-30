using UnityEngine;

/// <summary>
/// 먹을 수 있는 오브젝트 1개. 풀링되어 재사용된다.
/// 상태: Normal → Attracted(Hole 중심으로 끌려감) → Falling(Falling 레이어, 바닥 통과) → Consumed(풀 반환)
/// 별도 Update 없이 HoleSwallow가 Tick()을 호출한다 (오브젝트 수백 개여도 Update 비용 없음).
/// </summary>
public class EdibleObject : MonoBehaviour
{
    [SerializeField] private EdibleData data;

    public EdibleData Data { get { return data; } }
    public SwallowState State { get; private set; }
    /// <summary>true면 이 인스턴스는 먹을 수 없는 장식.</summary>
    public bool LockedDecoration { get; private set; }
    public float FootprintRadius { get; private set; }

    private Transform tr;
    private Rigidbody body;
    private Collider[] cols;
    private Vector3 baseScale = Vector3.one;
    private bool cached;

    private Transform hole;
    private float stateTime;
    private Vector3 spinAxis;
    private float attractSpeedNow;

    private void Awake()
    {
        Cache();
    }

    private void Cache()
    {
        if (cached) return;
        tr = transform;
        body = GetComponent<Rigidbody>();
        cols = GetComponentsInChildren<Collider>(true);
        baseScale = tr.localScale;

        // footprint는 Renderer 경계로 계산 (Collider.bounds는 물리 동기화 전엔 부정확할 수 있음)
        bool has = false;
        Bounds b = new Bounds(tr.position, Vector3.zero);
        Renderer[] rs = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rs.Length; i++)
        {
            if (!has) { b = rs[i].bounds; has = true; }
            else b.Encapsulate(rs[i].bounds);
        }
        FootprintRadius = has ? 0.5f * Mathf.Max(b.size.x, b.size.z) : 0.5f;
        cached = true;
    }

    /// <summary>스폰 직후 호출. 상태를 초기화한다.</summary>
    public void Init(EdibleData d, bool decoration)
    {
        Cache();
        if (d != null) data = d;
        LockedDecoration = decoration;
        State = SwallowState.Normal;
        stateTime = 0f;
        hole = null;
        gameObject.layer = Layers.Edible;
        SetLayerRecursive(tr, Layers.Edible);
        tr.localScale = baseScale;
        if (body != null)
        {
            // kinematic 전환 전에 속도를 먼저 0으로 (kinematic 상태에서 속도 설정은 경고)
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.isKinematic = true;
            body.useGravity = false;
        }
    }

    /// <summary>현재 Hole 상태로 먹을 수 있는지 판정 (단계 + 크기 + 잠금).</summary>
    public bool CanBeEatenBy(int holeTier, float holeRadius, float maxRatio)
    {
        if (data == null || !data.canBeSwallowed || LockedDecoration) return false;
        if (State != SwallowState.Normal) return false;
        if (data.sizeTier > holeTier) return false;
        if (FootprintRadius > holeRadius * maxRatio) return false;
        return true;
    }

    public void BeginAttract(Transform holeTransform)
    {
        if (State != SwallowState.Normal) return;
        State = SwallowState.Attracted;
        hole = holeTransform;
        stateTime = 0f;
        attractSpeedNow = 0f;
        spinAxis = new Vector3(Random.Range(-0.4f, 0.4f), 1f, Random.Range(-0.4f, 0.4f)).normalized;
    }

    /// <summary>HoleSwallow가 매 프레임 호출. 처리가 끝났으면(Consumed/비활성) false 반환.</summary>
    public bool Tick(float dt, GameSettings s, float holeRadius)
    {
        if (!gameObject.activeSelf || State == SwallowState.Consumed) return false;

        if (State == SwallowState.Attracted)
        {
            stateTime += dt;
            attractSpeedNow = s.attractSpeed * (1f + stateTime * 3f);

            Vector3 p = tr.position;
            Vector3 c = hole.position;
            Vector3 d = c - p;
            d.y = 0f;
            float dist = d.magnitude;
            float step = attractSpeedNow * dt;
            if (dist <= step) { p.x = c.x; p.z = c.z; dist = 0f; }
            else p += d / dist * step;
            tr.position = p;

            tr.Rotate(spinAxis * (s.attractSpin * dt), Space.World);

            // 살짝 눌리는 squash + 축소
            float t = Mathf.Clamp01(stateTime / Mathf.Max(0.05f, s.swallowDuration * 0.5f));
            float shrink = Mathf.Lerp(1f, 0.85f, t);
            float squash = Mathf.Lerp(1f, 0.9f, t);
            tr.localScale = new Vector3(baseScale.x * shrink, baseScale.y * shrink * squash, baseScale.z * shrink);

            if (dist <= holeRadius * s.swallowEdgeFactor || stateTime > s.swallowDuration)
                BeginFall();
        }
        else if (State == SwallowState.Falling)
        {
            stateTime += dt;
            float t = Mathf.Clamp01(stateTime / Mathf.Max(0.05f, s.swallowDuration));
            float k = Mathf.Lerp(0.85f, 0.05f, t * t);
            tr.localScale = baseScale * k;
            // 트리거 미스 대비 안전장치
            if (stateTime > s.swallowDuration + 0.8f) Consume();
        }

        return State != SwallowState.Consumed;
    }

    private void BeginFall()
    {
        State = SwallowState.Falling;
        stateTime = 0f;
        gameObject.layer = Layers.Falling;
        SetLayerRecursive(tr, Layers.Falling);
        if (body != null)
        {
            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = new Vector3(0f, -1.5f, 0f);
            body.angularVelocity = Random.insideUnitSphere * 4f;
        }
    }

    /// <summary>HoleConsume 트리거(또는 안전 타이머)가 호출. 점수/미션 보고 후 풀로 반환.</summary>
    public void Consume()
    {
        if (State == SwallowState.Consumed) return;
        State = SwallowState.Consumed;
        if (GameManager.Instance != null) GameManager.Instance.OnEdibleConsumed(this);
        if (ObjectPooler.Instance != null) ObjectPooler.Instance.Return(gameObject);
        else gameObject.SetActive(false);
    }

    /// <summary>풀로 돌아갈 때 물리 상태 정리 (레벨 전환 등).</summary>
    public void ForceReset()
    {
        State = SwallowState.Consumed;
    }

    private static void SetLayerRecursive(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++) SetLayerRecursive(t.GetChild(i), layer);
    }
}
