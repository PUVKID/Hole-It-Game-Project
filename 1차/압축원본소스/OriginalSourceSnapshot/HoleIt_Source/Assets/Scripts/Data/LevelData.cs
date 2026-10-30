using UnityEngine;

/// <summary>미션 1개 정의 (데이터). 런타임 진행도는 MissionManager가 별도 배열로 관리한다.</summary>
[System.Serializable]
public struct MissionData
{
    public EdibleData target;
    [Min(1)] public int requiredCount;
}

/// <summary>스폰 그룹: 한 영역에 같은 종류를 count개 배치한다 (원형 영역 안 랜덤, 시드 고정).</summary>
[System.Serializable]
public struct SpawnGroup
{
    public EdibleData edible;
    [Min(1)] public int count;
    [Tooltip("그룹 중심 (맵 로컬 XZ)")]
    public Vector2 center;
    [Tooltip("그룹 반경. 0이면 맵 전체에 흩뿌림")]
    public float radius;
    [Tooltip("true면 원형 링 형태로 배치 (radius 위에 균등 배치)")]
    public bool ring;
    [Tooltip("true면 이 그룹은 미션 대상이 아닌 장식으로 취급")]
    public bool decorative;
}

/// <summary>레벨 1개의 모든 설정. ScriptableObject 기반.</summary>
[CreateAssetMenu(fileName = "Level_", menuName = "HoleIt/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("기본")]
    public int levelId = 1;
    public string levelTitle = "LEVEL 1";
    [Range(1, 5)] public int difficulty = 1;
    [Tooltip("제한 시간(초). 0이면 무제한")]
    public float timeLimit = 60f;

    [Header("맵")]
    [Tooltip("맵 한 변 길이(정사각형). 바닥 Plane은 이 값에 맞춰 스케일된다")]
    public float mapSize = 30f;

    [Header("Hole")]
    [Min(1)] public int initialSizeTier = 1;
    [Tooltip("성장 배율 (레벨별 난이도 조절)")]
    public float growthMultiplier = 1f;
    [Tooltip("기본 이동 속도 (0이면 전역 설정 사용)")]
    public float movementSpeed = 0f;
    public Vector2 holeStart = Vector2.zero;

    [Header("카메라")]
    [Tooltip("카메라 Orthographic Size에 곱할 배율")]
    public float cameraZoomMultiplier = 1f;

    [Header("미션 (1~3개)")]
    public MissionData[] missions;
    public WrongTargetBehavior defaultWrongBehavior = WrongTargetBehavior.None;
    [Tooltip("Penalty일 때 깎이는 시간(초)")]
    public float penaltySeconds = 3f;
    public int penaltyScore = 20;

    [Header("스폰")]
    public int randomSeed = 1;
    public SpawnGroup[] spawnGroups;

    [Header("점수")]
    public int missionBonus = 100;
    public int clearBonus = 200;
    [Tooltip("남은 시간 1초당 보너스")]
    public int timeBonusPerSecond = 5;
}
