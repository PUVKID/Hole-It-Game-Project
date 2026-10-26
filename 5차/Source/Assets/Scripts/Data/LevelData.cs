using UnityEngine;
[System.Serializable]
public struct MissionData
{
    public string missionId;
    public MissionKind kind;
    public EdibleData target; // Exact-object identity includes distinct car colour variants.
    public ObjectType category;
    public ColorVariant color;
    [Min(1)] public int tier;
    [Min(1)] public int requiredCount;
    public Sprite icon;
    public string description;
    public bool Matches(EdibleData d)
    {
        if (d == null || d.objectType == ObjectType.Bomb) return false;
        switch (kind)
        {
            case MissionKind.ExactObject: return target != null && d == target;
            case MissionKind.ObjectCategory: return d.objectType == category;
            case MissionKind.Color: return color != ColorVariant.None && d.colorVariant == color && d.objectType == category;
            case MissionKind.Tier: return d.sizeTier == tier;
            case MissionKind.TimeBooster: return d.objectType == ObjectType.TimeBoost;
            default: return false;
        }
    }
    public string Label { get { return !string.IsNullOrEmpty(description) ? description : target != null ? target.displayName : kind.ToString(); } }
    public Sprite Icon { get { return icon != null ? icon : target != null ? target.icon : null; } }
}
[System.Serializable]
public struct SpawnGroup
{
    public EdibleData edible;
    [Min(1)] public int count;
    public Vector2 center;
    public float radius;
    public bool ring;
    public bool decorative;
}
[System.Serializable]
public struct SpawnPlacement
{
    public EdibleData edible;
    public Vector2 position;
    public float yaw;
    public bool decorative;
    public float height;
    public bool stacked;
}
[CreateAssetMenu(fileName="Level_", menuName="HoleIt/Level Data")]
public class LevelData : ScriptableObject
{
    public int schemaVersion = 8;
    public int levelId = 1;
    public string levelTitle = "LEVEL 1";
    [Range(1,5)] public int difficulty = 1;
    [Min(0)] public float timeLimit = 60f; // Zero means untimed.
    public ThemeData theme;
    public float mapSize = 16f; // Retained square-map fallback for legacy data.
    public Vector2 arenaSize = new Vector2(16f,14f);
    public Vector2Int gridSize = new Vector2Int(8,7);
    [Min(0)] public float startClearRadius = 2.1f;
    [Min(1)] public int initialSizeTier = 1;
    [Min(0.01f)] public float growthMultiplier = 1f;
    public float movementSpeed = 0f;
    public Vector2 holeStart;
    public float cameraZoomMultiplier = 1f;
    public MissionData[] missions;
    public WrongTargetBehavior defaultWrongBehavior = WrongTargetBehavior.None;
    [Min(0)] public float penaltySeconds = 2f;
    [Min(0)] public int penaltyScore = 8;
    public int randomSeed = 1;
    public SpawnGroup[] spawnGroups;
    public SpawnPlacement[] placements;
    public int missionBonus = 100;
    public int clearBonus = 200;
    public int timeBonusPerSecond = 5;
    public int nextLevelId;
    // Untimed boosters give their ordinary score and growth; they do not start a timer.
    public Vector2 Size { get { return arenaSize.x > 0 && arenaSize.y > 0 ? arenaSize : new Vector2(mapSize,mapSize); } }
}
