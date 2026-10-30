using UnityEngine;

/// <summary>
/// 노치/펀치홀/제스처 바 등 Safe Area에 맞춰 자신의 RectTransform 앵커를 조정한다.
/// HUD 루트(미션/타이머/버튼)에 붙이고, 전체 화면 입력 영역은 붙이지 않는다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rt;
    private Rect lastSafe;
    private Vector2Int lastScreen;

    private void Awake()
    {
        rt = (RectTransform)transform;
        Apply();
    }

    private void OnEnable() { Apply(); }

    private void Update()
    {
        if (Screen.safeArea != lastSafe || lastScreen.x != Screen.width || lastScreen.y != Screen.height) Apply();
    }

    private void Apply()
    {
        if (rt == null) rt = (RectTransform)transform;
        Rect safe = Screen.safeArea;
        lastSafe = safe;
        lastScreen = new Vector2Int(Screen.width, Screen.height);
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Vector2 min = safe.position;
        Vector2 max = safe.position + safe.size;
        min.x /= Screen.width; min.y /= Screen.height;
        max.x /= Screen.width; max.y /= Screen.height;
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
