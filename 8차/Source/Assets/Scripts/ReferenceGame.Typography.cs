using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class ReferenceGame {
    [Serializable] sealed class ReferenceTextPreset {
        public string key,scene,hierarchy,text,material;public int sourceId,style,horizontal,vertical,wrapping,overflow;
        public float size,min,max,spacing,wordSpacing,lineSpacing,paragraphSpacing;public bool autoSize,extraPadding;
        public Vector4 margin;public Color color;
    }
    [Serializable] sealed class ReferenceTextPresetCollection {public ReferenceTextPreset[] presets;}
    readonly Dictionary<string,Material> typographyMaterials=new Dictionary<string,Material>();
    readonly Dictionary<string,ReferenceTextPreset> referenceTextPresets=new Dictionary<string,ReferenceTextPreset>();
    TextMeshProUGUI resultOutcome;GameObject resultBravo;TextMeshPro worldHoleSize;Image referenceFreezeLogo,referenceBombInfo;
    TMP_FontAsset LoadTypographyFont(){
        var original=Resources.Load<TMP_FontAsset>("ReferenceTypography/LinikSansBlack");
        if(original==null)throw new InvalidOperationException("Original typography is missing. Run ReferenceTypographyBuilder.Prepare before building.");
        return original;
    }
    // Materials retain the reference's signed-distance-field outline and underlay values.
    void ApplyTextStyle(TMP_Text text,string materialKey,bool bold=false,float spacing=0){
        if(!typographyMaterials.TryGetValue(materialKey,out var material)){
            material=Resources.Load<Material>("ReferenceTypography/Materials/"+materialKey);
            if(material==null)throw new InvalidOperationException("Original text material missing: "+materialKey);
            typographyMaterials[materialKey]=material;
        }
        text.fontSharedMaterial=material;text.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;
        text.characterSpacing=spacing;text.extraPadding=false;
    }
    // Values are exported from actual TMP components; scene/path IDs remain in the JSON.
    void ApplyReferenceText(TMP_Text text,string key,bool originalContent=false){
        if(referenceTextPresets.Count==0){
            var data=Resources.Load<TextAsset>("ReferenceTypography/TextPresets");
            if(data==null)throw new InvalidOperationException("Original UI text presets are missing.");
            foreach(var item in JsonUtility.FromJson<ReferenceTextPresetCollection>(data.text).presets)referenceTextPresets.Add(item.key,item);
        }
        var p=referenceTextPresets[key];ApplyTextStyle(text,p.material);
        text.color=p.color;text.fontStyle=(FontStyles)p.style;text.enableAutoSizing=p.autoSize;
        text.fontSizeMin=p.min;text.fontSizeMax=p.max;text.fontSize=p.size;
        text.horizontalAlignment=(HorizontalAlignmentOptions)p.horizontal;text.verticalAlignment=(VerticalAlignmentOptions)p.vertical;
        text.characterSpacing=p.spacing;text.wordSpacing=p.wordSpacing;text.lineSpacing=p.lineSpacing;text.paragraphSpacing=p.paragraphSpacing;
        text.textWrappingMode=(TextWrappingModes)p.wrapping;text.overflowMode=(TextOverflowModes)p.overflow;text.margin=p.margin;text.extraPadding=p.extraPadding;
        if(originalContent)text.text=p.text.TrimEnd('\r','\n');
    }
    static void PlaceReferenceRect(RectTransform rect,Vector2 anchor,Vector2 size,Vector2 offset){rect.anchorMin=rect.anchorMax=anchor;rect.sizeDelta=size;rect.anchoredPosition=offset;}
    void SetInterfaceScale(float match){if(canvas!=null){var area=Screen.safeArea;float aspect=area.width/Mathf.Max(1,area.height);canvas.GetComponent<CanvasScaler>().matchWidthOrHeight=aspect<.5624f?0:match;}}
    void RestoreReferenceTextStyles(){
        ApplyReferenceText(timer,"Timer");ApplyReferenceText(levelLabel,"HUDLevel");
        var play=home.transform.Find("Play/Label").GetComponent<TextMeshProUGUI>();ApplyReferenceText(play,"HomeLevel");
        ApplyReferenceText(homeLevel,"HomeLevel");
        holeSize.gameObject.SetActive(false);growthToast.gameObject.SetActive(false);
        var settingsTitle=settings.transform.Find("Title").GetComponent<TextMeshProUGUI>();ApplyReferenceText(settingsTitle,"SettingsTitle",true);
        PlaceReferenceRect(settingsTitle.rectTransform,new Vector2(.5f,.5f),new Vector2(825.78f,178.4057f),new Vector2(0,415.7f));
        var pauseTitle=pause.transform.Find("Title").GetComponent<TextMeshProUGUI>();ApplyReferenceText(pauseTitle,"PauseTitle",true);
        PlaceReferenceRect(pauseTitle.rectTransform,new Vector2(.5f,.5f),new Vector2(825.78f,178.4057f),new Vector2(0,416));
        var resume=pause.transform.Find("Resume").GetComponent<RectTransform>();PlaceReferenceRect(resume,new Vector2(.5f,.5f),new Vector2(703,265),new Vector2(0,42));resume.localScale=Vector3.one*.75f;
        var resumeText=resume.Find("Label").GetComponent<TextMeshProUGUI>();PlaceReferenceRect(resumeText.rectTransform,new Vector2(.5f,.5f),new Vector2(500,200),Vector2.zero);ApplyReferenceText(resumeText,"Continue",true);
        // Extra restart/home controls remain available in this offline edition.
        var retry=pause.transform.Find("Retry/Label").GetComponent<TextMeshProUGUI>();retry.text="Try Again";
        ApplyTextStyle(retry,"Outline_Overlay_Blue_Button",true,-8.47f);
        foreach(var panel in new[]{result,pause,settings})foreach(var text in panel.GetComponentsInChildren<TextMeshProUGUI>(true))text.color=Color.white;
    }
    void ApplyReferenceResultPresentation(bool won,string reason){
        if(resultOutcome==null)return;
        resultBravo.SetActive(won);resultTitle.gameObject.SetActive(!won);
        bool bomb=!won&&reason.StartsWith("BOOM",StringComparison.Ordinal);
        referenceBombInfo.gameObject.SetActive(bomb);resultDetail.gameObject.SetActive(!bomb);
        resultOutcome.transform.SetParent(bomb?referenceBombInfo.transform:result.transform,false);
        PlaceReferenceRect(resultOutcome.rectTransform,new Vector2(.5f,.5f),bomb?new Vector2(338.0251f,73.518f):new Vector2(800,135),bomb?new Vector2(0,-130.1f):new Vector2(0,225));
        if(!won){ApplyReferenceText(resultTitle,reason=="TIME UP"?"TimeUp":"LevelTitle");resultTitle.text=reason=="TIME UP"?"Time Is Up!":CurrentStageLabel;}
        ApplyReferenceText(resultOutcome,won?"WinCoins":reason.StartsWith("BOOM",StringComparison.Ordinal)?"BombFailed":"LevelFailed");
        resultOutcome.text=won?(FreePlay?"Level Complete!":ReferenceProfile.WinReward.ToString()):reason.StartsWith("BOOM",StringComparison.Ordinal)?"You Blasted!":"Level Failed!";
        if(won&&FreePlay){resultOutcome.fontSize=48;resultOutcome.fontSizeMin=30;resultOutcome.fontSizeMax=48;}
        var next=result.transform.Find("Continue/Label").GetComponent<TextMeshProUGUI>();ApplyReferenceText(next,won?"Next":"Retry",true);
        // Source retry font is on a 0.75-scale button; keep that effective scale here.
        next.rectTransform.localScale=Vector3.one*(won?1:.75f);
    }
    void ApplyReferenceGoalText(TextMeshProUGUI text,bool hasGoals){
        ApplyReferenceText(text,hasGoals?"GoalCount":"StarGoalCount");
        // Original prefab is scaled 1.2; preserve the measured text scale inside our row.
        text.rectTransform.localScale=Vector3.one*1.2f;
        PlaceReferenceRect(text.rectTransform,new Vector2(.5f,.5f),new Vector2(114.78f,35.4744f),new Vector2(-.467748f,hasGoals?-76.08f:-61.44f));
        if(hasGoals){var complete=ReferenceImage("Complete",text.transform.parent,"OK",new Vector2(60,50.9091f),new Vector2(-.467748f,-76.08f));complete.gameObject.SetActive(false);}
    }
    void SetReferenceGoalCount(TextMeshProUGUI text,int remaining){
        text.text=Mathf.Max(0,remaining).ToString();text.enabled=remaining>0;
        var complete=text.transform.parent.Find("Complete");if(complete!=null)complete.gameObject.SetActive(remaining<=0);
    }
    void CreateReferenceWorldHoleLabel(){
        if(worldHoleSize!=null)return;
        worldHoleSize=new GameObject("Original World Hole Size",typeof(RectTransform),typeof(TextMeshPro)).GetComponent<TextMeshPro>();
        worldHoleSize.font=font;ApplyReferenceText(worldHoleSize,"WorldHoleSize",true);
        worldHoleSize.rectTransform.sizeDelta=new Vector2(1.3808f,.2384f);worldHoleSize.isOrthographic=false;
        var renderer=worldHoleSize.GetComponent<MeshRenderer>();renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
        UpdateReferenceWorldHoleLabel();
        var ringObject=new GameObject("Original Growth Progress");sourceGrowthRing=ringObject.AddComponent<ReferenceGrowthRing>();sourceGrowthRing.Initialize();
        // 원본의 열린 원형 게이지를 사용하고 임의의 점수 문장은 표시하지 않습니다.
        foreach(string name in new[]{"GrowthChip","GrowthProgress","Growth"}){var child=hud.transform.Find(name);if(child!=null)child.gameObject.SetActive(false);}
    }
    void UpdateReferenceWorldHoleLabel(){
        if(worldHoleSize==null)return;
        worldHoleSize.gameObject.SetActive(State==ScreenState.Playing);
        if(sizeLabel!=null)sizeLabel.gameObject.SetActive(false);
        if(referenceFreezeLogo!=null)referenceFreezeLogo.gameObject.SetActive(playTime<freezeUntil);
        worldHoleSize.text="Size "+(AppliedGrowthTier+1);
        if(sourceGrowthRing!=null){sourceGrowthRing.gameObject.SetActive(State==ScreenState.Playing);sourceGrowthRing.SetProgress(GrowthProgressFraction,HolePosition+Vector3.up*(-.041f+.06f*growthAnimation.Scale.y),growthAnimation.Scale);}
        // 원본 level2:2063의 글자 위치와 홀 확대 비율을 유지합니다.
        // 홀 컨테이너와 부모의 높이를 합한 기준 높이는 -0.041입니다.
        worldHoleSize.transform.SetPositionAndRotation(HolePosition+new Vector3(0,.1f*VisualRadius-.041f,-1.121f*VisualRadius),Quaternion.Euler(90,0,0));
        float pulse=1;
        if(Time.unscaledTime<growthFlash){float elapsed=.5f-(growthFlash-Time.unscaledTime);pulse=elapsed<.25f?Mathf.LerpUnclamped(1,1.4f,ReferenceMotionSettings.GrowthOutBack(elapsed/.25f)):Mathf.Lerp(1.4f,1,(elapsed-.25f)/.25f);}
        worldHoleSize.transform.localScale=Vector3.one*VisualRadius*pulse;
        if(holeSize!=null)holeSize.gameObject.SetActive(false);if(growthToast!=null)growthToast.gameObject.SetActive(false);
    }

}
