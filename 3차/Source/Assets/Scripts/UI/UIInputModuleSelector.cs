using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// EventSystem에 붙인다. Active Input Handling 설정에 맞는 UI 입력 모듈을 자동으로 맞춰 준다.
/// - Old 전용      : StandaloneInputModule
/// - New 또는 Both : InputSystemUIInputModule (com.unity.inputsystem 패키지 필요)
/// 설정과 모듈이 맞지 않으면 UI 버튼이 전혀 눌리지 않으므로, 씬에는 모듈을 직접 넣지 않고 이 컴포넌트가 만든다.
/// </summary>
[RequireComponent(typeof(EventSystem))]
public class UIInputModuleSelector : MonoBehaviour
{
    private void Awake()
    {
        StandaloneInputModule legacy = GetComponent<StandaloneInputModule>();
#if ENABLE_INPUT_SYSTEM
        if (legacy != null)
        {
            legacy.enabled = false;
            Destroy(legacy);
        }
        UnityEngine.InputSystem.UI.InputSystemUIInputModule m = GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        if (m == null)
        {
            m = gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            m.AssignDefaultActions();
        }
#else
        if (legacy == null) gameObject.AddComponent<StandaloneInputModule>();
#endif
    }
}
