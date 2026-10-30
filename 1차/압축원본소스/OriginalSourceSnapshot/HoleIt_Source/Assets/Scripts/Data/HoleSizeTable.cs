using UnityEngine;

/// <summary>Hole Size 단계 표. 단계별 반지름/필요 성장량/카메라 크기를 Inspector에서 수정한다.</summary>
[CreateAssetMenu(fileName = "HoleSizeTable", menuName = "HoleIt/Hole Size Table")]
public class HoleSizeTable : ScriptableObject
{
    /// <summary>씬 참조가 비어 있어도 항상 유효한 표를 돌려준다 (직렬화 참조 → Resources → 기본값 순).</summary>
    public static HoleSizeTable Resolve(HoleSizeTable serialized)
    {
        if (serialized != null) return serialized;
        HoleSizeTable t = Resources.Load<HoleSizeTable>("HoleSizeTable");
        if (t != null) return t;
        Debug.LogWarning("[HoleIt] HoleSizeTable 참조가 비어 있어 기본값을 사용합니다.");
        return CreateInstance<HoleSizeTable>();
    }

    [System.Serializable]
    public struct Tier
    {
        [Tooltip("이 단계에 도달하기 위해 필요한 누적 성장 포인트")]
        public float growthRequired;
        [Tooltip("이 단계의 Hole 반지름(월드 단위)")]
        public float radius;
        [Tooltip("이 단계의 카메라 Orthographic Size")]
        public float cameraSize;
    }

    public Tier[] tiers = new Tier[]
    {
        new Tier { growthRequired = 0f,  radius = 0.9f, cameraSize = 7.0f },
        new Tier { growthRequired = 6f,  radius = 1.2f, cameraSize = 8.0f },
        new Tier { growthRequired = 16f, radius = 1.6f, cameraSize = 9.2f },
        new Tier { growthRequired = 32f, radius = 2.1f, cameraSize = 10.6f },
        new Tier { growthRequired = 56f, radius = 2.7f, cameraSize = 12.0f },
        new Tier { growthRequired = 90f, radius = 3.4f, cameraSize = 13.5f },
    };

    public int TierCount { get { return tiers.Length; } }

    /// <summary>누적 성장 포인트로 Size 단계(1부터)를 계산.</summary>
    public int GetTierForGrowth(float growth)
    {
        int result = 1;
        for (int i = 0; i < tiers.Length; i++)
        {
            if (growth >= tiers[i].growthRequired) result = i + 1;
            else break;
        }
        return result;
    }

    /// <summary>1부터 시작하는 단계 번호를 안전하게 Tier로 반환.</summary>
    public Tier GetTier(int tierNumber)
    {
        int i = Mathf.Clamp(tierNumber - 1, 0, tiers.Length - 1);
        return tiers[i];
    }

    /// <summary>성장 포인트 기준으로 반지름을 단계 사이에서 부드럽게 보간.</summary>
    public float GetRadiusForGrowth(float growth)
    {
        return Interpolate(growth, true);
    }

    public float GetCameraSizeForGrowth(float growth)
    {
        return Interpolate(growth, false);
    }

    private float Interpolate(float growth, bool radius)
    {
        int n = tiers.Length;
        if (growth <= tiers[0].growthRequired) return radius ? tiers[0].radius : tiers[0].cameraSize;
        for (int i = 0; i < n - 1; i++)
        {
            float a = tiers[i].growthRequired;
            float b = tiers[i + 1].growthRequired;
            if (growth < b)
            {
                float t = Mathf.InverseLerp(a, b, growth);
                t = t * t * (3f - 2f * t);
                return radius
                    ? Mathf.Lerp(tiers[i].radius, tiers[i + 1].radius, t)
                    : Mathf.Lerp(tiers[i].cameraSize, tiers[i + 1].cameraSize, t);
            }
        }
        return radius ? tiers[n - 1].radius : tiers[n - 1].cameraSize;
    }

    /// <summary>다음 단계까지 진행도 0~1 (UI 게이지용). 최대 단계면 1.</summary>
    public float GetProgressToNext(float growth)
    {
        int cur = GetTierForGrowth(growth);
        if (cur >= tiers.Length) return 1f;
        return Mathf.InverseLerp(tiers[cur - 1].growthRequired, tiers[cur].growthRequired, growth);
    }
}
