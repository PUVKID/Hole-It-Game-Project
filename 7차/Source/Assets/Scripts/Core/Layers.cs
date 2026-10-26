using UnityEngine;

/// <summary>물리 레이어 캐시. Ground / Edible / Falling 레이어는 Editor 빌더가 TagManager에 등록한다.</summary>
public static class Layers
{
    public const string GroundName = "Ground";
    public const string EdibleName = "Edible";
    public const string FallingName = "Falling";

    public static int Ground { get; private set; }
    public static int Edible { get; private set; }
    public static int Falling { get; private set; }
    public static int EdibleMask { get; private set; }

    private static bool initialized;

    public static void Init()
    {
        if (initialized) return;
        Ground = Resolve(GroundName, 8);
        Edible = Resolve(EdibleName, 9);
        Falling = Resolve(FallingName, 10);
        EdibleMask = 1 << Edible;

        // Falling 레이어는 바닥을 통과해 떨어지고, 다른 물체를 밀지 않는다.
        Physics.IgnoreLayerCollision(Falling, Ground, true);
        Physics.IgnoreLayerCollision(Falling, Edible, true);
        Physics.IgnoreLayerCollision(Falling, Falling, true);
        initialized = true;
    }

    private static int Resolve(string layerName, int fallback)
    {
        int l = LayerMask.NameToLayer(layerName);
        if (l < 0)
        {
            Debug.LogWarning("[Layers] 레이어 '" + layerName + "' 가 없습니다. 메뉴 HoleIt/1. 프로젝트 전체 구성 을 실행하세요. 임시로 " + fallback + " 사용");
            return fallback;
        }
        return l;
    }
}
