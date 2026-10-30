using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레벨 로딩 담당: 이전 오브젝트 회수 → 풀 Prewarm → 시드 고정 배치 → Hole/미션 초기화.
/// Instantiate는 풀 Prewarm 시점(로딩)에만 발생하고 플레이 중에는 발생하지 않는다.
/// </summary>
public class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelData[] levels;
    [SerializeField] private ObjectPooler pooler;
    [SerializeField] private HoleController hole;
    [SerializeField] private HoleSwallow holeSwallow;
    [SerializeField] private MissionManager missionManager;
    [Tooltip("바닥 Plane (10x10 기본 Plane 기준으로 mapSize에 맞춰 스케일)")]
    [SerializeField] private Transform ground;
    [Tooltip("바닥 아래 넓은 Consume 트리거")]
    [SerializeField] private Transform consumeZone;
    [Tooltip("Hole 시작 위치 주변 비워둘 반경")]
    [SerializeField] private float startClearRadius = 3.2f;
    [SerializeField] private float edgeMargin = 1.0f;

    private readonly List<Vector3> placed = new List<Vector3>(512); // x, z, radius
    private readonly Dictionary<EdibleData, int> spawnedCount = new Dictionary<EdibleData, int>(16);
    private readonly Dictionary<GameObject, int> prewarmCount = new Dictionary<GameObject, int>(16);

    public int LevelCount { get { return levels != null ? levels.Length : 0; } }
    public LevelData Current { get; private set; }
    public int CurrentIndex { get; private set; }

    public LevelData GetLevel(int index)
    {
        if (levels == null || levels.Length == 0) return null;
        return levels[Mathf.Clamp(index, 0, levels.Length - 1)];
    }

    /// <summary>
    /// Scene에 LevelData 참조가 빠졌거나 오래된 씬을 사용한 경우 Resources에서 레벨 카탈로그를 복구한다.
    /// 생성기는 레벨 에셋을 Assets/HoleIt/Resources/Data/Levels에 저장한다.
    /// </summary>
    private bool EnsureLevelCatalog()
    {
        bool hasMissing = levels == null || levels.Length == 0;
        if (!hasMissing)
        {
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] == null) { hasMissing = true; break; }
            }
        }

        if (hasMissing)
        {
            LevelData[] loaded = Resources.LoadAll<LevelData>("Data/Levels");
            if (loaded != null && loaded.Length > 0)
            {
                System.Array.Sort(loaded, delegate(LevelData a, LevelData b)
                {
                    if (a == null) return b == null ? 0 : 1;
                    if (b == null) return -1;
                    return a.levelId.CompareTo(b.levelId);
                });

                int valid = 0;
                for (int i = 0; i < loaded.Length; i++) if (loaded[i] != null) valid++;
                LevelData[] clean = new LevelData[valid];
                int dst = 0;
                for (int i = 0; i < loaded.Length; i++)
                    if (loaded[i] != null) clean[dst++] = loaded[i];

                if (clean.Length > 0)
                {
                    levels = clean;
                    Debug.LogWarning("[LevelManager] Scene의 LevelData 참조가 비어 있어 Resources에서 " + levels.Length + "개 레벨을 복구했습니다.");
                }
            }
        }

        // null 항목을 제거해 LevelCount와 실제 인덱스가 일치하게 한다.
        if (levels != null && levels.Length > 0)
        {
            int valid = 0;
            for (int i = 0; i < levels.Length; i++) if (levels[i] != null) valid++;
            if (valid != levels.Length)
            {
                LevelData[] clean = new LevelData[valid];
                int dst = 0;
                for (int i = 0; i < levels.Length; i++)
                    if (levels[i] != null) clean[dst++] = levels[i];
                levels = clean;
            }
        }

        if (levels == null || levels.Length == 0)
        {
            Current = null;
            Debug.LogError("[LevelManager] LevelData가 없습니다. Unity에서 'HoleIt > 1. 프로젝트 전체 구성'을 실행한 뒤 Android APK를 다시 빌드하세요. Resources.Load 경로: Data/Levels (에셋 경로: Assets/HoleIt/Resources/Data/Levels)");
            return false;
        }
        return true;
    }

    public void Load(int index)
    {
        if (!EnsureLevelCatalog()) return;

        CurrentIndex = Mathf.Clamp(index, 0, levels.Length - 1);
        LevelData level = levels[CurrentIndex];
        if (level == null)
        {
            Current = null;
            Debug.LogError("[LevelManager] 선택한 레벨 데이터가 null입니다. 'HoleIt > 1. 프로젝트 전체 구성'을 다시 실행하세요.");
            return;
        }
        Current = level;
        if (level.spawnGroups == null) level.spawnGroups = new SpawnGroup[0];
        if (level.missions == null) level.missions = new MissionData[0];

        if (pooler == null || holeSwallow == null || hole == null || missionManager == null)
        {
            Debug.LogError("[LevelManager] 필수 참조 누락: " +
                (pooler == null ? "ObjectPooler " : string.Empty) +
                (holeSwallow == null ? "HoleSwallow " : string.Empty) +
                (hole == null ? "HoleController " : string.Empty) +
                (missionManager == null ? "MissionManager " : string.Empty));
            return;
        }

        pooler.ReturnAllActive();
        holeSwallow.ClearAll();
        placed.Clear();
        spawnedCount.Clear();

        float half = level.mapSize * 0.5f;
        if (ground != null) ground.localScale = new Vector3(level.mapSize / 10f, 1f, level.mapSize / 10f);
        if (consumeZone != null) consumeZone.localScale = new Vector3(level.mapSize * 6f, 1f, level.mapSize * 6f);

        missionManager.LoadLevel(level);
        hole.ResetForLevel(level);

        Prewarm(level);
        Spawn(level, half);
        ValidateMissions(level);
    }

    private void Prewarm(LevelData level)
    {
        prewarmCount.Clear();
        for (int g = 0; g < level.spawnGroups.Length; g++)
        {
            SpawnGroup grp = level.spawnGroups[g];
            if (grp.edible == null || grp.edible.prefab == null) continue;
            int c;
            prewarmCount.TryGetValue(grp.edible.prefab, out c);
            prewarmCount[grp.edible.prefab] = c + grp.count;
        }
        foreach (KeyValuePair<GameObject, int> kv in prewarmCount)
            pooler.Prewarm(kv.Key, kv.Value);
    }

    private struct PendingItem
    {
        public EdibleData edible;
        public EdibleObject obj;
        public float diameter;
    }

    private readonly List<PendingItem> items = new List<PendingItem>(512);
    private readonly List<Vector2> cells = new List<Vector2>(1024);
    private readonly List<float> diameters = new List<float>(512);

    /// <summary>
    /// 균일 격자 배치: 맵 전체를 같은 크기의 칸으로 나누고, 스폰 그룹 순서대로(같은 종류가 이웃하도록, 뱀 모양 순서)
    /// 칸 중앙에 일정한 간격으로 놓는다. 물체 수가 칸보다 적으면 맵 전체에 고르게 퍼지도록 간격을 띄운다.
    /// Hole 시작 위치 주변은 광장처럼 비워 둔다. (랜덤 흩뿌림 대신 정돈된 진열 느낌)
    /// </summary>
    private void Spawn(LevelData level, float half)
    {
        System.Random rng = new System.Random(level.randomSeed);
        Vector2 start = level.holeStart;

        items.Clear();
        diameters.Clear();
        for (int g = 0; g < level.spawnGroups.Length; g++)
        {
            SpawnGroup grp = level.spawnGroups[g];
            if (grp.edible == null || grp.edible.prefab == null) continue;
            for (int n = 0; n < grp.count; n++)
            {
                GameObject go = pooler.Get(grp.edible.prefab, new Vector3(0f, 0.02f, 0f), Quaternion.identity);
                EdibleObject e = go.GetComponent<EdibleObject>();
                e.Init(grp.edible, grp.decorative);
                PendingItem it = new PendingItem();
                it.edible = grp.edible;
                it.obj = e;
                it.diameter = e.FootprintRadius * 2f;
                items.Add(it);
                diameters.Add(it.diameter);

                int c0;
                spawnedCount.TryGetValue(grp.edible, out c0);
                spawnedCount[grp.edible] = c0 + 1;
            }
        }
        if (items.Count == 0) return;

        // 칸 크기: 큰 물체 기준(상위 15% 지름)보다 약간 크게, 물체 수에 맞춰 맵을 균일하게 채운다.
        diameters.Sort();
        float p85 = diameters[Mathf.Min(diameters.Count - 1, Mathf.FloorToInt(diameters.Count * 0.85f))];
        p85 = Mathf.Max(0.8f, p85);
        float span = (half - edgeMargin) * 2f;
        float plazaR = startClearRadius + 0.5f;
        float fit = Mathf.Sqrt(span * span * 0.75f / Mathf.Max(1f, items.Count / 0.7f));
        float cell = Mathf.Clamp(fit, p85 * 1.05f, 3.4f);

        BuildCells(half, cell, start, plazaR);
        int guard = 0;
        while (cells.Count < items.Count && cell > p85 * 0.8f && guard++ < 40)
        {
            cell *= 0.95f;
            BuildCells(half, cell, start, plazaR);
        }

        int total = items.Count;
        int avail = cells.Count;
        float lim = half - edgeMargin;
        Quaternion facing = Quaternion.Euler(0f, 180f, 0f);   // 카메라(-Z)를 향해 정면이 보이도록 (집 문, 자동차 앞 등)

        for (int i = 0; i < total; i++)
        {
            PendingItem it = items[i];
            Vector2 pos;
            if (avail >= total)
            {
                // 칸이 남으면 일정한 간격으로 건너뛰며 맵 전체에 고르게 배치
                int idx = Mathf.Min(avail - 1, Mathf.FloorToInt((i + 0.5f) * avail / total));
                pos = cells[idx];
            }
            else if (i < avail)
            {
                pos = cells[i];
            }
            else
            {
                // 칸이 모자라는 경우의 안전장치: 남은 물체만 빈 곳에 랜덤 배치
                float fr = it.obj.FootprintRadius;
                pos = Vector2.zero;
                bool ok = false;
                for (int attempt = 0; attempt < 30 && !ok; attempt++)
                {
                    pos = new Vector2(
                        (float)(rng.NextDouble() * 2.0 - 1.0) * (lim - fr),
                        (float)(rng.NextDouble() * 2.0 - 1.0) * (lim - fr));
                    ok = IsFree(pos, fr, start);
                }
            }

            pos.x = Mathf.Clamp(pos.x, -half + edgeMargin, half - edgeMargin);
            pos.y = Mathf.Clamp(pos.y, -half + edgeMargin, half - edgeMargin);
            it.obj.transform.SetPositionAndRotation(new Vector3(pos.x, 0.02f, pos.y), facing);
            placed.Add(new Vector3(pos.x, pos.y, it.obj.FootprintRadius));
        }
    }

    /// <summary>맵을 cell 크기의 정사각 칸으로 나눈 뒤, 뱀 모양(지그재그) 순서로 칸 중심을 cells 에 채운다.</summary>
    private void BuildCells(float half, float cell, Vector2 start, float plazaR)
    {
        cells.Clear();
        float span = (half - edgeMargin) * 2f;
        int cols = Mathf.Max(1, Mathf.FloorToInt(span / cell + 0.001f));
        float step = span / cols;
        float origin = -half + edgeMargin + step * 0.5f;
        for (int r = 0; r < cols; r++)
        {
            for (int k = 0; k < cols; k++)
            {
                int col = (r % 2 == 0) ? k : cols - 1 - k;
                Vector2 p = new Vector2(origin + col * step, origin + r * step);
                if ((p - start).magnitude < plazaR) continue;
                cells.Add(p);
            }
        }
    }

    private Vector2 SamplePosition(System.Random rng, SpawnGroup grp, int index, float half, float footprint)
    {
        float lim = half - edgeMargin - footprint;
        if (grp.radius <= 0.01f)
        {
            return new Vector2(
                (float)(rng.NextDouble() * 2.0 - 1.0) * lim,
                (float)(rng.NextDouble() * 2.0 - 1.0) * lim);
        }

        if (grp.ring)
        {
            float a = (index + (float)rng.NextDouble() * 0.2f) / Mathf.Max(1, grp.count) * Mathf.PI * 2f;
            return grp.center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * grp.radius;
        }

        float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
        float rad = Mathf.Sqrt((float)rng.NextDouble()) * grp.radius;
        return grp.center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad;
    }

    private bool IsFree(Vector2 p, float r, Vector2 holeStart)
    {
        if ((p - holeStart).magnitude < startClearRadius + r) return false;
        for (int i = 0; i < placed.Count; i++)
        {
            Vector3 q = placed[i];
            float dx = p.x - q.x;
            float dz = p.y - q.y;
            float min = r + q.z + 0.08f;
            if (dx * dx + dz * dz < min * min) return false;
        }
        return true;
    }

    private void ValidateMissions(LevelData level)
    {
        for (int i = 0; i < level.missions.Length; i++)
        {
            MissionData m = level.missions[i];
            if (m.target == null) continue;
            int have;
            spawnedCount.TryGetValue(m.target, out have);
            if (have < m.requiredCount)
                Debug.LogWarning("[LevelManager] " + level.name + " 미션 '" + m.target.displayName + "' 필요 " + m.requiredCount + " > 배치 " + have);
        }
    }
}
