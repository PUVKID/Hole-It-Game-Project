using UnityEngine;

/// <summary>
/// Hole 이동/성장 담당. 물리 힘을 쓰지 않고 입력으로 직접 위치를 제어한다.
/// - 이동: 가속/감속 스무딩 + 크기 보정 속도(나눗셈 방식 금지, 곡선 기반)
/// - 경계: 홀 전체와 성장 펀치가 아레나 안에 머물도록 반지름만큼 제한
/// - 성장: 누적 성장 포인트 → Size 단계/반지름, 소비 직후 짧은 Punch
/// </summary>
public class HoleController : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private HoleSizeTable sizeTable;
    [SerializeField] private HoleVisualController visual;

    public float Radius { get; private set; }
    public float Growth { get; private set; }
    public int Tier { get; private set; }
    public bool ControlEnabled { get; private set; }
    public Vector3 Position { get { return transform.position; } }
    public Vector2 Velocity { get { return velocity; } }

    public event System.Action<int> OnTierUp;
    public event System.Action<float> OnGrowth;

    private Vector2 velocity;
    private float targetRadius;
    private float punchTimer;
    private Vector2 mapHalf = new Vector2(15f,15f);
    private float speedOverride;
    private float growthMultiplier = 1f;
    private float tier1Radius = 1f;

    public GameSettings Settings { get { return settings; } }
    public HoleSizeTable SizeTable { get { return sizeTable; } }

    private void Awake()
    {
        settings = GameSettings.Resolve(settings);
        sizeTable = HoleSizeTable.Resolve(sizeTable);
        if (visual == null) visual = GetComponentInChildren<HoleVisualController>();
    }

    /// <summary>레벨 시작 시 호출. 위치/크기/속도 설정을 초기화한다.</summary>
    public void ResetForLevel(LevelData level)
    {
        mapHalf = level.Size * 0.5f;
        speedOverride = level.movementSpeed;
        growthMultiplier = level.growthMultiplier <= 0f ? 1f : level.growthMultiplier;
        tier1Radius = Mathf.Max(0.01f, sizeTable.tiers[0].radius);

        int startTier = Mathf.Clamp(level.initialSizeTier, 1, sizeTable.TierCount);
        Growth = sizeTable.GetTier(startTier).growthRequired;
        Tier = sizeTable.GetTierForGrowth(Growth);
        Radius = targetRadius = sizeTable.GetRadiusForGrowth(Growth);
        velocity = Vector2.zero;
        punchTimer = 0f;

        Vector2 s = level.holeStart;
        transform.position = new Vector3(Mathf.Clamp(s.x, -mapHalf.x + Radius, mapHalf.x - Radius), 0f, Mathf.Clamp(s.y, -mapHalf.y + Radius, mapHalf.y - Radius));
        ApplyVisual();
    }

    public void SetControlEnabled(bool enabled)
    {
        ControlEnabled = enabled;
        if (!enabled) velocity = Vector2.zero;
    }

    public void AddGrowth(float amount)
    {
        if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        int before = Tier;
        Growth = Mathf.Min(sizeTable.GetTier(sizeTable.TierCount).growthRequired, Growth + amount * growthMultiplier);
        Tier = sizeTable.GetTierForGrowth(Growth);
        targetRadius = sizeTable.GetRadiusForGrowth(Growth);
        punchTimer = settings.growthPunchDuration;
        if (OnGrowth != null) OnGrowth(sizeTable.GetProgressToNext(Growth));
        if (Tier > before && OnTierUp != null) OnTierUp(Tier);
    }

    /// <summary>현재 크기 기준 이동 속도 (기본 + 크기 보정 곡선).</summary>
    public float CurrentMaxSpeed()
    {
        float baseSpeed = speedOverride > 0f ? speedOverride : settings.baseSpeed;
        float comp = settings.speedByRadius.Evaluate(Radius / tier1Radius);
        return Mathf.Min(settings.maxSpeed, baseSpeed * comp);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 반지름은 목표값을 지수 보간으로 따라간다 (즉각 체감 + 부드러움)
        float k = 1f - Mathf.Exp(-settings.radiusFollowSpeed * dt);
        Radius = Mathf.Lerp(Radius, targetRadius, k);

        if (punchTimer > 0f) punchTimer = Mathf.Max(0f, punchTimer - dt);

        var controls=InputManager.Instance;
        if(ControlEnabled&&controls!=null&&controls.HasDragTarget){
            Vector3 before=transform.position;
            transform.position=ClampPosition(Vector3.Lerp(before,controls.DragTarget,1f-Mathf.Exp(-35f*dt)));
            Vector3 step=transform.position-before;velocity=dt>0?new Vector2(step.x,step.z)/dt:Vector2.zero;
            ApplyVisual();return;
        }
        Vector2 input = Vector2.zero;
        if (ControlEnabled && InputManager.Instance != null) input = InputManager.Instance.MoveVector;

        float yaw = settings.cameraYaw * Mathf.Deg2Rad;
        Vector2 worldInput = new Vector2(input.x * Mathf.Cos(yaw) + input.y * Mathf.Sin(yaw), -input.x * Mathf.Sin(yaw) + input.y * Mathf.Cos(yaw));
        Vector2 desired = worldInput * CurrentMaxSpeed();
        float rate = desired.sqrMagnitude >= velocity.sqrMagnitude ? settings.acceleration : settings.deceleration;
        velocity = desired.sqrMagnitude<.00001f?Vector2.zero:Vector2.MoveTowards(velocity, desired, rate * dt);

        Vector3 p = transform.position;
        float margin=Radius*(1f+settings.growthPunch)+0.12f;
        float lx=Mathf.Max(0f,mapHalf.x-margin), lz=Mathf.Max(0f,mapHalf.y-margin);
        p.x = Mathf.Clamp(p.x + velocity.x * dt, -lx, lx);
        p.z = Mathf.Clamp(p.z + velocity.y * dt, -lz, lz);
        transform.position = p;

        ApplyVisual();
    }

    public Vector3 ClampPosition(Vector3 p){float margin=Radius*(1f+settings.growthPunch)+.12f;float x=Mathf.Max(0,mapHalf.x-margin),z=Mathf.Max(0,mapHalf.y-margin);return new Vector3(Mathf.Clamp(p.x,-x,x),0,Mathf.Clamp(p.z,-z,z));}

    private void ApplyVisual()
    {
        if (visual == null) return;
        float punch = 0f;
        if (punchTimer > 0f)
        {
            // 1.00 → 1.05 → 1.02 → 1.0 형태의 짧은 펀치
            float t = 1f - punchTimer / Mathf.Max(0.01f, settings.growthPunchDuration);
            punch = settings.growthPunch * Mathf.Sin(t * Mathf.PI) * (1f - t * 0.5f);
        }
        visual.SetRadius(Radius, 1f + punch);
    }
}
