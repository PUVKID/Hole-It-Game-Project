using UnityEngine;

/// <summary>
/// 먹을 수 있는 오브젝트 1종류의 데이터. 프리팹/크기/종류/점수/성장치를 Inspector에서 조정한다.
/// </summary>
[CreateAssetMenu(fileName = "Edible_", menuName = "HoleIt/Edible Data")]
public class EdibleData : ScriptableObject
{
    [Header("식별")]
    public string displayName = "오브젝트";
    public string objectId;
    public ColorVariant colorVariant;
    [Min(0.01f)] public float footprintRadius = 0.5f;
    [Tooltip("풀링에 사용할 프리팹")]
    public GameObject prefab;
    [Tooltip("미션 UI에 표시할 아이콘 (없으면 색상 사각형)")]
    public Sprite icon;
    public Color uiColor = Color.white;
    [Min(0.05f)] public float stackHeight = 0.5f;

    [Header("분류 (크기와 종류는 분리)")]
    public ObjectType objectType = ObjectType.Ball;
    [Tooltip("이 오브젝트를 먹기 위한 최소 Hole Size 단계 (1부터 시작)")]
    [Min(1)] public int sizeTier = 1;

    [Header("보상")]
    public int score = 10;
    [Tooltip("먹었을 때 Hole 성장 포인트")]
    public float growthValue = 1f;
    [Tooltip("TimeBoost 오브젝트를 먹었을 때 추가되는 시간(초). 다른 오브젝트에서는 무시됩니다.")]
    [Min(0f)] public float specialTimeBonus = 10f;

    [Header("흡수 규칙")]
    public bool canBeSwallowed = true;
    [Tooltip("여러 개가 동시에 접근할 때 숫자가 작을수록 먼저 흡수된다")]
    public int swallowPriority = 0;
    [Tooltip("미션 대상이 아닐 때의 처리 (레벨의 기본값을 덮어쓰려면 사용)")]
    public bool overrideWrongBehavior = false;
    public WrongTargetBehavior wrongBehavior = WrongTargetBehavior.None;
}
