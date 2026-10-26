using UnityEngine;

/// <summary>
/// Visuals for a fixed-position, bottom-centre virtual joystick.
/// InputManager owns touch tracking and movement calculation; this component only draws the base and knob.
/// All joystick graphics are non-raycast targets so they never block gameplay input.
/// </summary>
public class VirtualJoystick : MonoBehaviour
{
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private RectTransform baseRect;
    [SerializeField] private RectTransform fillRect;
    [SerializeField] private RectTransform knobRect;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Vector2 fixedAnchor = new Vector2(0.5f, 0.145f);
    [Tooltip("How far the knob can travel inside the base radius.")]
    [SerializeField, Range(0.3f, 1f)] private float knobTravel = 0.78f;

    private float radiusPx;
    private float canvasScale = 1f;

    /// <summary>Show the fixed control when gameplay input becomes enabled.</summary>
    public void ShowFixed(float radiusInPixels)
    {
        radiusPx = Mathf.Max(32f, radiusInPixels);
        CacheCanvasScale();
        if (baseRect != null)
        {
            baseRect.anchorMin = fixedAnchor;
            baseRect.anchorMax = fixedAnchor;
            baseRect.pivot = new Vector2(0.5f, 0.5f);
            baseRect.anchoredPosition = Vector2.zero;

            float diameterCanvas = (radiusPx * 2f) / canvasScale;
            baseRect.sizeDelta = new Vector2(diameterCanvas, diameterCanvas);
            if (fillRect != null) fillRect.sizeDelta = new Vector2(diameterCanvas * 0.885f, diameterCanvas * 0.885f);
            if (knobRect != null) knobRect.sizeDelta = new Vector2(diameterCanvas * 0.37f, diameterCanvas * 0.37f);
        }
        ResetStick();
        if (group != null) group.alpha = 1f;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    /// <summary>Kept as a compatibility shim for scenes built by older versions of the editor builder.</summary>
    public void Show(Vector2 ignoredTouchPosition, float radiusInPixels)
    {
        ShowFixed(radiusInPixels);
    }

    /// <summary>True only for touches that start on the fixed joystick pad.</summary>
    public bool ContainsScreenPoint(Vector2 screenPosition)
    {
        if (baseRect == null || !gameObject.activeInHierarchy || radiusPx <= 0f) return false;
        Vector2 centre = GetCenterScreenPosition();
        float hitRadius = radiusPx * 1.16f;
        return (screenPosition - centre).sqrMagnitude <= hitRadius * hitRadius;
    }

    public Vector2 GetCenterScreenPosition()
    {
        if (baseRect == null) return new Vector2(Screen.width * 0.5f, Screen.height * fixedAnchor.y);
        return RectTransformUtility.WorldToScreenPoint(null, baseRect.position);
    }

    /// <summary>Move only the knob; the base stays fixed at the bottom centre of the display.</summary>
    public void MoveFixed(Vector2 deltaPx)
    {
        if (knobRect == null) return;
        CacheCanvasScale();
        Vector2 delta = deltaPx / canvasScale;
        float maxTravel = (radiusPx / canvasScale) * knobTravel;
        if (delta.sqrMagnitude > maxTravel * maxTravel)
            delta = delta.normalized * maxTravel;
        knobRect.anchoredPosition = delta;
    }

    /// <summary>Compatibility shim for prior dynamic-joystick scenes; the base no longer moves.</summary>
    public void Move(Vector2 ignoredBaseScreenPosition, Vector2 deltaPx)
    {
        MoveFixed(deltaPx);
    }

    public void ResetStick()
    {
        if (knobRect != null) knobRect.anchoredPosition = Vector2.zero;
    }

    public void Hide()
    {
        ResetStick();
        if (group != null) group.alpha = 0f;
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    private void CacheCanvasScale()
    {
        Canvas c = canvasRect != null ? canvasRect.GetComponent<Canvas>() : null;
        canvasScale = (c != null && c.scaleFactor > 0f) ? c.scaleFactor : 1f;
        // Include the safe-area logical-fit transform in the screen-pixel conversion.
        Transform parent=baseRect!=null?baseRect.parent:null;
        while(parent!=null && (c==null||parent!=c.transform)){canvasScale*=Mathf.Abs(parent.localScale.x);parent=parent.parent;}
        canvasScale=Mathf.Max(.001f,canvasScale);
    }
}
