using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hole 입구 Trigger. 접근한 Edible 후보를 모아 순차적으로 흡수(Attracted → Falling)를 시작시키고,
/// 흡수 중인 오브젝트의 Tick을 구동한다. 후보/활성 리스트는 미리 할당되어 GC가 발생하지 않는다.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class HoleSwallow : MonoBehaviour
{
    [SerializeField] private HoleController hole;
    [Tooltip("트리거 구 높이(로컬 Y). 바닥 위 물체의 몸통 높이에 맞춘다")]
    [SerializeField] private float triggerHeight = 0.6f;
    [Tooltip("트리거 구가 후보 판정 반경보다 얼마나 더 크게 잡을지")]
    [SerializeField] private float triggerPadding = 0.6f;

    private SphereCollider sphere;
    private readonly List<EdibleObject> candidates = new List<EdibleObject>(128);
    private readonly List<EdibleObject> active = new List<EdibleObject>(64);
    private float nextSwallowTime;
    private float lastSphereRadius = -1f;
    public bool HasPendingBombs { get { foreach(var e in active)if(e!=null&&e.Data.objectType==ObjectType.Bomb&&(e.State==SwallowState.Attracted||e.State==SwallowState.Falling))return true;return false; } }

    private void Awake()
    {
        sphere = GetComponent<SphereCollider>();
        sphere.isTrigger = true;
        if (hole == null) hole = GetComponentInParent<HoleController>();
    }

    private void OnTriggerEnter(Collider other) { Register(other); }
    private void OnTriggerStay(Collider other) { Register(other); }

    private void Register(Collider other)
    {
        if (other.gameObject.layer != Layers.Edible) return;
        EdibleObject e = other.GetComponentInParent<EdibleObject>();
        if (e == null || e.State != SwallowState.Normal) return;
        if (!candidates.Contains(e)) candidates.Add(e);
    }

    /// <summary>레벨 전환 시 목록 정리.</summary>
    public void ClearAll()
    {
        for (int i=0; i<active.Count; i++) if(active[i]!=null) { active[i].ForceReset(); if(ObjectPooler.Instance!=null) ObjectPooler.Instance.Return(active[i].gameObject); }
        candidates.Clear();
        active.Clear();
        nextSwallowTime = 0f;
    }

    private void Update()
    {
        if (hole == null) return;
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
        GameSettings s = hole.Settings;
        float r = hole.Radius;

        float sr = r * s.attractRadiusFactor + triggerPadding;
        if (Mathf.Abs(sr - lastSphereRadius) > 0.01f)
        {
            sphere.radius = sr;
            sphere.center = new Vector3(0f, triggerHeight, 0f);
            lastSphereRadius = sr;
        }

        float dt = Time.deltaTime;

        // 흡수 진행 중인 오브젝트 Tick
        for (int i = active.Count - 1; i >= 0; i--)
        {
            bool stillActive=active[i]!=null && active[i].Tick(dt,s,r);
            // A consume callback can finish/fail a level and clear the active list synchronously.
            if(GameManager.Instance==null || GameManager.Instance.State!=GameState.Playing)return;
            if(!stillActive && i<active.Count){active[i]=active[active.Count-1];active.RemoveAt(active.Count-1);}
        }

        if (!hole.ControlEnabled) return;

        // 후보 정리 + 가장 우선순위 높은 후보 선택
        Vector3 hp = hole.Position;
        float reach = r * s.attractRadiusFactor;
        EdibleObject best = null;
        float bestDist = float.MaxValue;
        int bestPrio = int.MaxValue;

        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            EdibleObject e = candidates[i];
            if (e == null || !e.gameObject.activeInHierarchy || e.State != SwallowState.Normal)
            {
                RemoveCandidate(i);
                continue;
            }
            Vector3 d = e.transform.position - hp;
            d.y = 0f;
            float dist = d.magnitude;
            if (dist > reach + e.FootprintRadius + triggerPadding + 0.5f)
            {
                RemoveCandidate(i);
                continue;
            }
            // Hazards are swallowed only when their whole body crosses the opening.
            // This keeps a large attraction field from unfairly pulling in a nearby bomb.
            float objectReach=Mathf.Max(.08f,r-e.FootprintRadius*.65f);
            if (dist > objectReach) continue;
            if(e.BottomHeight>.22f)continue;
            if (!e.CanBeEatenBy(hole.Tier, r, s.maxObjectToHoleRatio)) continue;
            if (GameManager.Instance != null && GameManager.Instance.IsBlocked(e.Data)) continue;

            int prio = e.Data.swallowPriority;
            if (prio < bestPrio || (prio == bestPrio && dist < bestDist))
            {
                best = e; bestPrio = prio; bestDist = dist;
            }
        }

        // 순차 흡수: 아주 짧은 시간차로 하나씩 시작
        if (best != null && Time.time >= nextSwallowTime)
        {
            if(GameManager.Instance!=null&&!GameManager.Instance.ReserveSwallow(best.Data))return;
            best.BeginAttract(hole.transform);
            active.Add(best);
            nextSwallowTime = Time.time + s.swallowStagger;
        }
    }

    private void RemoveCandidate(int i)
    {
        candidates[i] = candidates[candidates.Count - 1];
        candidates.RemoveAt(candidates.Count - 1);
    }
}
