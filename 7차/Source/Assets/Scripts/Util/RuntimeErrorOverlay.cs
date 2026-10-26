using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 기기(APK)에서 로그를 볼 수 없어도 문제를 알 수 있도록, 에러/예외와 입력 설정 오류를 화면 상단에 빨간 박스로 표시한다.
/// 문제가 없으면 아무것도 그리지 않는다. (프로토타입 단계용 - 배포 시 GameSystems에서 제거 가능)
/// </summary>
public class RuntimeErrorOverlay : MonoBehaviour
{
    private const int MaxMessages = 4;

    private readonly List<string> messages = new List<string>(MaxMessages);
    private GUIStyle style;

    private void Awake()
    {
        Application.logMessageReceived += OnLog;
        CheckInputSetup();
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;
    }

    private void CheckInputSetup()
    {
#if !ENABLE_LEGACY_INPUT_MANAGER && !ENABLE_INPUT_SYSTEM
        Add("INPUT SETTING ERROR: 입력 시스템이 모두 비활성입니다. Player Settings > Active Input Handling 을 'Both' 로 설정하세요.");
#elif !ENABLE_LEGACY_INPUT_MANAGER
        // New 전용: UIInputModuleSelector / InputManager가 Input System 경로로 동작한다 (com.unity.inputsystem 패키지 필요)
#else
        try
        {
            int n = Input.touchCount;
            if (n < 0) Add("Input returned invalid value");
        }
        catch (System.InvalidOperationException e)
        {
            Add("INPUT ERROR: " + e.Message);
        }
#endif
    }

    private void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
        string first = stackTrace;
        int nl = string.IsNullOrEmpty(stackTrace) ? -1 : stackTrace.IndexOf('\n');
        if (nl > 0) first = stackTrace.Substring(0, nl);
        Add(condition + (string.IsNullOrEmpty(first) ? "" : "\n  at " + first));
    }

    private void Add(string msg)
    {
        if (messages.Count >= MaxMessages) return;
        for (int i = 0; i < messages.Count; i++) if (messages[i] == msg) return;
        messages.Add(msg);
    }

    private void OnGUI()
    {
        if (messages.Count == 0) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box);
            style.wordWrap = true;
            style.alignment = TextAnchor.UpperLeft;
            style.normal.textColor = Color.white;
        }
        style.fontSize = Mathf.Max(18, Screen.height / 55);

        string text = string.Join("\n\n", messages);
        float w = Screen.width - 20f;
        float h = style.CalcHeight(new GUIContent(text), w);
        Color old = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.55f, 0f, 0f, 0.95f);
        GUI.Box(new Rect(10f, 10f, w, Mathf.Min(h + 20f, Screen.height * 0.6f)), text, style);
        GUI.backgroundColor = old;
    }
}
