from pathlib import Path
import json
p=Path(__file__).parent/'AndroidFinal'
f=p/'ProjectSettings/ProjectSettings.asset'
s=f.read_text().replace('activeInputHandler: 0','activeInputHandler: 1').replace('bundleVersion: 4.0.0','bundleVersion: 4.1.0').replace('AndroidBundleVersionCode: 5','AndroidBundleVersionCode: 6')
f.write_text(s)
f=p/'Assets/Editor/ReferenceBuilder.cs'
s=f.read_text().replace('bundleVersion="4.0.0"','bundleVersion="4.1.0"').replace('bundleVersionCode=5','bundleVersionCode=6')
f.write_text(s)
f=p/'Assets/Scripts/ReferenceGame.cs'
s=f.read_text().replace('public void BeginStick(Vector2 p){Stick.Radius=Mathf.Clamp(Screen.width*.11f,42,120);Stick.Begin(p);}', '''public static bool NativeInputEnabled {
        get {
#if ENABLE_INPUT_SYSTEM
            return true;
#else
            return false;
#endif
        }
    }
    float JoystickRadius=>Mathf.Clamp(Screen.width*.11f,42,120);
    public Vector2 IdleJoystickPosition=>new Vector2(Screen.safeArea.x+Screen.safeArea.width*.18f,Screen.safeArea.y+Mathf.Max(420*canvas.scaleFactor,Screen.safeArea.height*.22f));
    public bool JoystickVisible=>stickBase.gameObject.activeInHierarchy;
    public void BeginStick(Vector2 p){Stick.Radius=JoystickRadius;var idle=IdleJoystickPosition;Stick.Begin(Vector2.Distance(p,idle)<=Stick.Radius?idle:p);Stick.Move(p);}''')
s=s.replace('public void ReleaseStick(){Stick.Release();Velocity=Vector2.zero;}','public void ReleaseStick(){Stick.Release();finger=-1;Velocity=Vector2.zero;}')
s=s.replace('FLOATING JOYSTICK\\n\\nHold to move. Release to stop.\\nPull farther for speed, up to the limit.', 'JOYSTICK\\n\\nDrag the stick to move. Release to stop.\\nYou can also drag on an empty area.')
s=s.replace('new Color(1,1,1,.24f),Vector2.zero);pad.sprite','new Color(1,1,1,.65f),Vector2.zero);pad.sprite').replace('new Color(1,1,1,.65f),Vector2.zero);knob.sprite','new Color(1,1,1,.9f),Vector2.zero);knob.sprite')
start=s.index('    void UpdateStickVisual()')
end=s.index('\n    void ShowResult',start)
s=s[:start]+'''    void UpdateStickVisual(){
        stickBase.gameObject.SetActive(State==ScreenState.Playing);
        if(State!=ScreenState.Playing)return;
        Vector2 origin=Stick.Held?Stick.Origin:IdleJoystickPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,origin,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var local);
        stickBase.anchoredPosition=local;
        float diameter=(Stick.Held?Stick.Radius:JoystickRadius)*2/canvas.scaleFactor;
        stickBase.sizeDelta=Vector2.one*diameter;stickKnob.sizeDelta=Vector2.one*diameter*.4f;
        stickKnob.anchoredPosition=(Stick.Held?Stick.Value:Vector2.zero)*diameter*.32f;
    }'''+s[end:]
f.write_text(s)
f=p/'Assets/Tests/Reference/Reference.Tests.asmdef'
a=json.loads(f.read_text());a['references'].append('Unity.InputSystem');f.write_text(json.dumps(a,indent=2))
