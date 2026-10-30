using UnityEngine;

/// <summary>전역 튜닝 값. 코드에 상수를 박지 않고 모두 여기서 조정한다.</summary>
[CreateAssetMenu(fileName = "GameSettings", menuName = "HoleIt/Game Settings")]
public class GameSettings : ScriptableObject
{
    /// <summary>씬 참조가 비어 있어도 항상 유효한 설정을 돌려준다 (직렬화 참조 → Resources → 기본값 순).</summary>
    public static GameSettings Resolve(GameSettings serialized)
    {
        if (serialized != null) return serialized;
        GameSettings s = Resources.Load<GameSettings>("GameSettings");
        if (s != null) return s;
        Debug.LogWarning("[HoleIt] GameSettings 참조가 비어 있어 기본값을 사용합니다. 'HoleIt ▸ 1. 프로젝트 전체 구성'을 다시 실행하세요.");
        return CreateInstance<GameSettings>();
    }

    [Header("입력 / 조이스틱")]
    [Tooltip("조이스틱 반경(픽셀, 1080p 기준. 실제 값은 Screen.dpi/해상도로 보정)")]
    public float joystickRadius = 82f;
    [Range(0f, 0.5f)] public float joystickDeadZone = 0.08f;

    [Header("Hole 이동")]
    public float baseSpeed = 6f;
    public float maxSpeed = 11f;
    [Tooltip("가속 (units/s^2) - 빠르게")]
    public float acceleration = 60f;
    [Tooltip("감속 (units/s^2) - 약간 부드럽게")]
    public float deceleration = 38f;
    [Tooltip("크기 단계가 올라갈 때 속도 보정 곡선 (x: 반지름/초기반지름, y: 속도 배율). 감소폭을 작게 유지")]
    public AnimationCurve speedByRadius = new AnimationCurve(
        new Keyframe(1f, 1f), new Keyframe(2f, 1.18f), new Keyframe(4f, 1.35f));

    [Header("Hole 성장 연출")]
    [Tooltip("반지름이 목표값으로 따라가는 속도")]
    public float radiusFollowSpeed = 9f;
    [Tooltip("성장 직후 Punch 배율 (예: 0.04 = 4%)")]
    public float growthPunch = 0.05f;
    public float growthPunchDuration = 0.25f;

    [Header("흡수 연출")]
    [Tooltip("물체 중심이 Hole 반지름 x 이 값 안에 들어오면 끌려오기 시작")]
    public float attractRadiusFactor = 1.0f;
    public float attractSpeed = 5f;
    public float attractSpin = 540f;
    public float swallowDuration = 0.45f;
    [Tooltip("여러 오브젝트가 동시에 접근할 때 순차 흡수 간격(초)")]
    public float swallowStagger = 0.06f;
    public float fallGravity = 22f;
    [Tooltip("끌려오던 물체 중심이 Hole 반지름 x 이 값 이내로 오면 낙하(Falling) 시작")]
    public float swallowEdgeFactor = 0.5f;

    [Header("물체 크기 판정")]
    [Tooltip("물체 footprint 반지름이 Hole 반지름의 이 비율 이하일 때만 먹을 수 있음 (단계 조건과 함께 적용)")]
    public float maxObjectToHoleRatio = 1.0f;

    [Header("카메라")]
    public float cameraSmooth = 7f;
    public float cameraZoomSmooth = 3.5f;
    [Tooltip("카메라 피치(도). 쿼터뷰")]
    public float cameraPitch = 58f;
    public float cameraYaw = 0f;
    public float cameraDistance = 40f;

    [Header("레벨 템포")]
    public float readyDelay = 1.2f;
    [Range(0.3f, 0.8f)] public float clearDelay = 0.6f;
    public float lowTimeWarning = 10f;

    [Header("저장")]
    public int topScoreCount = 3;
}
