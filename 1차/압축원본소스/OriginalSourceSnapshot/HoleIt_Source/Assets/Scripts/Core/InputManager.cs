using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Fixed bottom-centre virtual joystick input.
/// A touch starts only on the joystick pad; the base never follows the finger, while the knob and movement vector do.
/// UI buttons remain independent and cannot accidentally move the Hole.
/// </summary>
public class InputManager : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private VirtualJoystick joystickUI;
    [Tooltip("Allow keyboard movement for Editor/PC testing (WASD or arrow keys).")]
    [SerializeField] private bool allowKeyboard = true;

    public static InputManager Instance { get; private set; }
    public Vector2 MoveVector { get; private set; }
    public bool IsTouching { get { return tracking; } }

    private bool inputEnabled = true;
    private bool tracking;
    private int fingerId = -1;
    private Vector2 origin;
    private float radiusPx;

    private void Awake()
    {
        Instance = this;
        settings = GameSettings.Resolve(settings);
        if (joystickUI == null) joystickUI = FindFirstObjectByType<VirtualJoystick>(FindObjectsInactive.Include);
        radiusPx = CalculateRadiusPx();
        if (joystickUI != null) joystickUI.Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        radiusPx = CalculateRadiusPx();
        if (!enabled)
        {
            Release();
            if (joystickUI != null) joystickUI.Hide();
            return;
        }

        if (joystickUI != null) joystickUI.ShowFixed(radiusPx);
    }

    private float CalculateRadiusPx()
    {
        float r = settings != null ? settings.joystickRadius : 82f;
        r *= Screen.height / 1080f;
        return Mathf.Max(32f, r);
    }

    private void OnApplicationPause(bool paused) { if (paused) Release(); }
    private void OnApplicationFocus(bool focus) { if (!focus) Release(); }
    private void OnDisable() { Release(); }

    private void Update()
    {
        if (!inputEnabled)
        {
            MoveVector = Vector2.zero;
            return;
        }

        float updatedRadius = CalculateRadiusPx();
        if (Mathf.Abs(updatedRadius - radiusPx) > 0.5f)
        {
            radiusPx = updatedRadius;
            if (joystickUI != null) joystickUI.ShowFixed(radiusPx);
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount > 0) HandleTouch();
        else if (!Application.isMobilePlatform) HandleMouse();
        else if (tracking) Release();

        if (allowKeyboard && !tracking)
        {
            Vector2 k = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (k.sqrMagnitude > 1f) k.Normalize();
            MoveVector = k;
        }
#elif ENABLE_INPUT_SYSTEM
        HandleNewInputSystem();
#endif
    }

#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
    private void HandleNewInputSystem()
    {
        UnityEngine.InputSystem.Pointer p = UnityEngine.InputSystem.Pointer.current;
        if (p != null)
        {
            Vector2 pos = p.position.ReadValue();
            if (!tracking)
            {
                if (p.press.wasPressedThisFrame && CanBeginAt(pos, -1)) Begin(pos, -1);
            }
            else if (!p.press.isPressed) Release();
            else Drag(pos);
        }
        else if (tracking) Release();

        UnityEngine.InputSystem.Keyboard kb = UnityEngine.InputSystem.Keyboard.current;
        if (allowKeyboard && !tracking && kb != null)
        {
            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) -
                      (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
            float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) -
                      (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
            Vector2 k = new Vector2(x, y);
            if (k.sqrMagnitude > 1f) k.Normalize();
            MoveVector = k;
        }
    }
#endif

    private void HandleTouch()
    {
        if (!tracking)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase != TouchPhase.Began) continue;
                if (!CanBeginAt(t.position, t.fingerId)) continue;
                Begin(t.position, t.fingerId);
                break;
            }
            return;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.fingerId != fingerId) continue;
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) { Release(); return; }
            Drag(t.position);
            return;
        }
        Release();
    }

    private void HandleMouse()
    {
        if (!tracking)
        {
            if (Input.GetMouseButtonDown(0) && CanBeginAt(Input.mousePosition, -1)) Begin(Input.mousePosition, -1);
            return;
        }
        if (Input.GetMouseButtonUp(0) || !Input.GetMouseButton(0)) { Release(); return; }
        Drag(Input.mousePosition);
    }

    private bool CanBeginAt(Vector2 screenPosition, int id)
    {
        if (IsOverUI(id)) return false;
        // Fixed joystick should be visible only while playing and accept gestures only on its pad.
        return joystickUI == null || joystickUI.ContainsScreenPoint(screenPosition);
    }

    private static bool IsOverUI(int id)
    {
        EventSystem es = EventSystem.current;
        if (es == null) return false;
        return id >= 0 ? es.IsPointerOverGameObject(id) : es.IsPointerOverGameObject();
    }

    private void Begin(Vector2 pos, int id)
    {
        tracking = true;
        fingerId = id;
        origin = joystickUI != null ? joystickUI.GetCenterScreenPosition() : pos;
        MoveVector = Vector2.zero;
        if (joystickUI != null) joystickUI.ResetStick();
        Drag(pos);
    }

    private void Drag(Vector2 pos)
    {
        Vector2 delta = pos - origin;
        float dist = delta.magnitude;
        Vector2 clamped = delta;
        if (dist > radiusPx && dist > 0.001f) clamped = delta * (radiusPx / dist);

        float n = Mathf.Clamp01(dist / Mathf.Max(1f, radiusPx));
        float deadZone = settings != null ? settings.joystickDeadZone : 0.08f;
        if (n < deadZone || dist < 0.001f)
        {
            MoveVector = Vector2.zero;
        }
        else
        {
            float magnitude = Mathf.InverseLerp(deadZone, 1f, n);
            MoveVector = (delta / dist) * magnitude;
        }

        if (joystickUI != null) joystickUI.MoveFixed(clamped);
    }

    private void Release()
    {
        tracking = false;
        fingerId = -1;
        MoveVector = Vector2.zero;
        if (joystickUI != null)
        {
            if (inputEnabled) joystickUI.ResetStick();
            else joystickUI.Hide();
        }
    }
}
