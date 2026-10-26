using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif
/// <summary>One gesture owner, one movement vector, and one backend even in Both mode.</summary>
[DefaultExecutionOrder(-100)]
public class InputManager : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private VirtualJoystick joystickUI;
    [SerializeField] private bool allowKeyboard = true;
    public static InputManager Instance { get; private set; }
    public Vector2 MoveVector { get; private set; }
    public bool IsTouching { get { return tracking; } }
    public ControlMode Mode { get; private set; }
    private bool inputEnabled, tracking;
    private int fingerId = -1, width, height;
    private Vector2 origin;
    private float radiusPx;
    private PointerEventData pointer;
    private EventSystem pointerSystem;
    private readonly List<RaycastResult> hits = new List<RaycastResult>(16);
    private void Awake()
    {
        Instance = this; settings = GameSettings.Resolve(settings);
        Mode = (ControlMode)Mathf.Clamp(PlayerPrefs.GetInt("HoleIt.ControlMode",0),0,1);
        Recalculate(); SetInputEnabled(false);
    }
    private void OnEnable() {
#if ENABLE_INPUT_SYSTEM
        EnhancedTouchSupport.Enable();
#endif
    }
    private void OnDisable() {
        Release();
#if ENABLE_INPUT_SYSTEM
        EnhancedTouchSupport.Disable();
#endif
    }
    private void OnDestroy() { if(Instance==this) Instance=null; }
    public void SetMode(ControlMode mode)
    {
        Mode = mode == ControlMode.FixedJoystick ? mode : ControlMode.Drag;
        PlayerPrefs.SetInt("HoleIt.ControlMode",(int)Mode); PlayerPrefs.Save();
        Release(); UpdateJoystick();
    }
    public void SetInputEnabled(bool value) { inputEnabled=value; Release(); Recalculate(); UpdateJoystick(); }
    private void Recalculate() { width=Screen.width; height=Screen.height; radiusPx=Mathf.Max(30, settings.joystickRadius * Screen.width / 1080f); }
    private void UpdateJoystick()
    {
        if(joystickUI==null) return;
        if(inputEnabled && Mode==ControlMode.FixedJoystick) joystickUI.ShowFixed(radiusPx);
        else joystickUI.Hide();
    }
    private void OnApplicationPause(bool paused) { if(paused) Release(); }
    private void OnApplicationFocus(bool focused) { if(!focused) Release(); }
    private void Update()
    {
        if(width!=Screen.width || height!=Screen.height) { Release(); Recalculate(); UpdateJoystick(); }
        if(!inputEnabled) { MoveVector=Vector2.zero; return; }
#if ENABLE_INPUT_SYSTEM
        var touches=ETouch.activeTouches;
        if(tracking && fingerId>=0)
        {
            bool found=false;
            for(int i=0;i<touches.Count;i++) {
                var t=touches[i]; if(t.finger.index!=fingerId) continue; found=true;
                if(t.phase==UnityEngine.InputSystem.TouchPhase.Ended || t.phase==UnityEngine.InputSystem.TouchPhase.Canceled) Release();
                else Drag(t.screenPosition);
                break;
            }
            if(!found) Release();
        }
        else if(!tracking) {
            for(int i=0;i<touches.Count;i++) { var t=touches[i];
                if(t.phase==UnityEngine.InputSystem.TouchPhase.Began && CanBeginAt(t.screenPosition)) { Begin(t.screenPosition,t.finger.index); break; }
            }
        }
        if(!Application.isMobilePlatform && touches.Count==0 && Mouse.current!=null) {
            var mouse=Mouse.current;
            if(!tracking && mouse.leftButton.wasPressedThisFrame && CanBeginAt(mouse.position.ReadValue())) Begin(mouse.position.ReadValue(),-1);
            else if(tracking && fingerId==-1) { if(!mouse.leftButton.isPressed) Release(); else Drag(mouse.position.ReadValue()); }
        }
        if(allowKeyboard && !tracking && Keyboard.current!=null) {
            var k=Keyboard.current;
            MoveVector=Vector2.ClampMagnitude(new Vector2((k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0),
                (k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0)),1);
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if(Input.touchCount>0) {
            bool found=false;
            for(int i=0;i<Input.touchCount;i++) { Touch t=Input.GetTouch(i);
                if(!tracking && t.phase==TouchPhase.Began && CanBeginAt(t.position)) Begin(t.position,t.fingerId);
                if(tracking && t.fingerId==fingerId) { found=true; if(t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled) Release(); else Drag(t.position); }
            }
            if(tracking && !found) Release();
        } else if(!Application.isMobilePlatform) {
            if(!tracking && Input.GetMouseButtonDown(0) && CanBeginAt(Input.mousePosition)) Begin(Input.mousePosition,-1);
            else if(tracking) { if(!Input.GetMouseButton(0)) Release(); else Drag(Input.mousePosition); }
        } else if(tracking) Release();
        if(allowKeyboard && !tracking) MoveVector=Vector2.ClampMagnitude(new Vector2(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?-1:0,
            Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?-1:0),1);
#endif
    }
    private bool CanBeginAt(Vector2 pos)
    {
        var es=EventSystem.current;
        if(es!=null) {
            if(pointer==null || pointerSystem!=es) { pointerSystem=es; pointer=new PointerEventData(es); }
            pointer.Reset(); pointer.position=pos; hits.Clear(); es.RaycastAll(pointer,hits);
            if(hits.Count>0) return false;
        }
        return Mode==ControlMode.Drag || (joystickUI!=null && joystickUI.ContainsScreenPoint(pos));
    }
    private void Begin(Vector2 pos,int id) { tracking=true; fingerId=id; origin=Mode==ControlMode.FixedJoystick && joystickUI!=null?joystickUI.GetCenterScreenPosition():pos; Drag(pos); }
    private void Drag(Vector2 pos)
    {
        Vector2 delta=(pos-origin)*(Mode==ControlMode.Drag?settings.dragSensitivity:1f);
        float dist=delta.magnitude, n=Mathf.Clamp01(dist/radiusPx);
        MoveVector=dist<0.001f || n<settings.joystickDeadZone?Vector2.zero:delta/dist*Mathf.InverseLerp(settings.joystickDeadZone,1,n);
        if(Mode==ControlMode.FixedJoystick && joystickUI!=null) joystickUI.MoveFixed(Vector2.ClampMagnitude(delta,radiusPx));
    }
    private void Release() { tracking=false; fingerId=-1; MoveVector=Vector2.zero; if(joystickUI!=null) joystickUI.ResetStick(); }
}
