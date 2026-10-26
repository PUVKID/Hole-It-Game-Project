using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public partial class ReferenceGame {
    public bool Ready {get;private set;}
    Camera bootCamera;Canvas bootCanvas;Image bootProgress;TextMeshProUGUI bootStatus;
    void Awake(){Instance=this;CreateBootScreen();}
    IEnumerator Start(){
        yield return null;
        var request=Resources.LoadAsync<ReferenceCatalog>("ReferenceCatalog");
        while(!request.isDone){bootProgress.fillAmount=request.progress*.85f;yield return null;}
        bootProgress.fillAmount=.85f;bootStatus.text="Loading...";yield return null;
        InitializeGame();EnhancePresentation();BuildProfileUI();BuildExtraContentUI();BuildAttendanceUI();Destroy(bootCamera.gameObject);Ready=true;bootProgress.fillAmount=1;bootStatus.text="Loading...";
        yield return new WaitForSecondsRealtime(.25f);
        var group=bootCanvas.gameObject.AddComponent<CanvasGroup>();
        for(float t=0;t<.2f;t+=Time.unscaledDeltaTime){group.alpha=1-t/.2f;yield return null;}
        Destroy(bootCanvas.gameObject);
    }
    RawImage FullBleed(Transform parent,string resource){
        var image=new GameObject("Artwork",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter)).GetComponent<RawImage>();image.transform.SetParent(parent,false);image.texture=Resources.Load<Texture2D>(resource);image.raycastTarget=false;
        var rect=image.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var fit=image.GetComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=(float)image.texture.width/image.texture.height;image.transform.SetAsFirstSibling();return image;
    }
    void CreateBootScreen(){
        bootCamera=new GameObject("Loading Camera",typeof(Camera)).GetComponent<Camera>();bootCamera.cullingMask=0;bootCamera.clearFlags=CameraClearFlags.SolidColor;bootCamera.backgroundColor=new Color(.94f,.75f,.40f);
        font=LoadTypographyFont();
        bootCanvas=new GameObject("Coton Loading",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();bootCanvas.renderMode=RenderMode.ScreenSpaceOverlay;bootCanvas.sortingOrder=1000;
        var scaler=bootCanvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=.5f;
        FullBleed(bootCanvas.transform,"CotonArt/Loading");
        var shield=new GameObject("Loading Input Shield",typeof(RectTransform),typeof(Image)).GetComponent<Image>();shield.transform.SetParent(bootCanvas.transform,false);shield.color=Color.clear;shield.rectTransform.anchorMin=Vector2.zero;shield.rectTransform.anchorMax=Vector2.one;shield.rectTransform.offsetMin=shield.rectTransform.offsetMax=Vector2.zero;
        var title=Label("Title",bootCanvas.transform,"HOLE IT!",108,new Color(1,.8f,.18f),new Vector2(.5f,.88f),new Vector2(900,180),Vector2.zero);ApplyTextStyle(title,"DropShadow_Loading",true);
        var bg=Panel("Loading Progress",bootCanvas.transform,new Vector2(.5f,0),new Vector2(698,108),Color.white,new Vector2(0,161));bg.sprite=Resources.Load<Sprite>("ReferenceStyle/spScreen-loading_BG");bg.type=Image.Type.Simple;
        bootProgress=Panel("Fill",bg.transform,new Vector2(.5f,.5f),new Vector2(640,50),Color.white,new Vector2(-5.4f,5.8f));bootProgress.sprite=Resources.Load<Sprite>("ReferenceStyle/SpScreen-progress");bootProgress.type=Image.Type.Filled;bootProgress.fillMethod=Image.FillMethod.Horizontal;bootProgress.fillAmount=0;
        bootStatus=Label("Status",bg.transform,"Loading...",55,Color.white,new Vector2(.5f,.5f),new Vector2(319.5204f,81.2978f),new Vector2(0,86));ApplyReferenceText(bootStatus,"Loading",true);
    }
    void EnhancePresentation(){
        FullBleed(home.transform,"CotonArt/Home");
        homeTitle.text="HOLE IT!";homeTitle.color=new Color(1,.85f,.25f);homeTitle.fontSize=100;homeTitle.fontSizeMax=100;
        homeTitle.rectTransform.anchoredPosition=new Vector2(0,10);
        home.transform.Find("Play").GetComponent<Image>().sprite=Style("btn-mid-green-play");
        foreach(var panel in new[]{result,pause,settings}){
            panel.AddComponent<ReferencePanelMotion>();
            var titleRect=panel.transform.Find("Title").GetComponent<RectTransform>();titleRect.anchorMin=titleRect.anchorMax=new Vector2(.5f,.725f);
            var card=panel.transform.Find("Card").GetComponent<Image>();card.sprite=Style("info-win-v3");card.type=Image.Type.Simple;card.color=Color.white;
            
        }
        var pauseButton=hud.transform.Find("Pause");pauseButton.GetComponent<Image>().sprite=Style("btn-blue-settings");pauseButton.Find("Label").GetComponent<TextMeshProUGUI>().text="";var pauseIcon=Panel("Icon",pauseButton,new Vector2(.5f,.5f),new Vector2(50,50),Color.white,Vector2.zero);pauseIcon.sprite=Style("pause-Icn");pauseIcon.type=Image.Type.Simple;pauseIcon.preserveAspect=true;
        for(int i=0;i<4;i++){
            var rect=(RectTransform)boostButtons[i].transform;rect.sizeDelta=new Vector2(160,150);rect.anchoredPosition=new Vector2((i-1.5f)*218,104);
            var icon=Panel("Booster Icon",rect,new Vector2(.5f,.63f),new Vector2(94,90),Color.white,Vector2.zero);icon.sprite=Style("Booster"+new[]{0,2,3,1}[i]);icon.type=Image.Type.Simple;icon.preserveAspect=true;
            boostLabels[i].rectTransform.anchorMin=boostLabels[i].rectTransform.anchorMax=new Vector2(.5f,.17f);boostLabels[i].rectTransform.sizeDelta=new Vector2(155,45);boostLabels[i].fontSize=23;boostLabels[i].fontSizeMax=23;boostLabels[i].fontSizeMin=18;
        }
        BuildSoundToggle(settings.transform,"Sound",new Vector2(0,190));
        BuildSoundToggle(pause.transform,"Pause Sound",new Vector2(0,125));
        RestoreReferenceTextStyles();
        RestoreReferenceLayout();
        BuildReferenceResultArt();
        CreateReferenceWorldHoleLabel();
    }
    void BuildSoundToggle(Transform parent,string name,Vector2 offset){
        // Source SoundItem: 281x297, scale .7, 211x211 inner, 148x110 icon.
        var outer=Panel(name+" Frame",parent,new Vector2(.5f,.5f),new Vector2(281,297),Color.white,offset);outer.sprite=Style("btn-small-yellow");outer.type=Image.Type.Simple;outer.transform.localScale=Vector3.one*.7f;
        var title=Label("Sound Title",outer.transform,"Sound",55,Color.white,new Vector2(.5f,.5f),new Vector2(350,80),new Vector2(0,180));ApplyReferenceText(title,"SoundTitle",true);
        var inner=Panel(name,outer.transform,new Vector2(.5f,.5f),new Vector2(211,211),Color.white,new Vector2(0,10));inner.sprite=Style("btn-small-green");inner.type=Image.Type.Simple;inner.raycastTarget=true;
        var icon=Panel("Sound Icon",inner.transform,new Vector2(.5f,.5f),new Vector2(148,110),Color.white,Vector2.zero);icon.sprite=Style("sound-icon");icon.type=Image.Type.Simple;icon.preserveAspect=true;
        var cross=Panel("Muted",icon.transform,new Vector2(.5f,.5f),new Vector2(155.42f,175.92f),Color.white,Vector2.zero);cross.sprite=Style("cross");cross.type=Image.Type.Simple;cross.preserveAspect=true;cross.gameObject.SetActive(mute);
        var button=inner.gameObject.AddComponent<Button>();button.targetGraphic=inner;button.onClick.AddListener(()=>{mute=!mute;PlayerPrefs.SetInt("Gaeun.Ref.Mute",mute?1:0);PlayerPrefs.Save();foreach(var mark in new[]{settings.transform.Find("Sound Frame/Sound/Sound Icon/Muted"),pause.transform.Find("Pause Sound Frame/Pause Sound/Sound Icon/Muted")})if(mark!=null)mark.gameObject.SetActive(mute);});
    }
    Image ReferenceImage(string name,Transform parent,string sprite,Vector2 size,Vector2 offset){var image=Panel(name,parent,new Vector2(.5f,.5f),size,Color.white,offset);image.sprite=Style(sprite);image.type=Image.Type.Simple;image.preserveAspect=true;return image;}
    void RestoreReferenceLayout(){
        // Home play control uses the measured source button and inner text bounds.
        var play=home.transform.Find("Play").GetComponent<RectTransform>();PlaceReferenceRect(play,new Vector2(.5f,.5f),new Vector2(525,360),new Vector2(0,-584.3f));
        PlaceReferenceRect(play.Find("Label").GetComponent<RectTransform>(),new Vector2(.5f,.5f),new Vector2(370.32f,183.75f),new Vector2(6.85f,9.6f));
        homeLevel.gameObject.SetActive(false);
        PlaceReferenceRect(home.transform.Find("Previous").GetComponent<RectTransform>(),new Vector2(.5f,.5f),new Vector2(105,90),new Vector2(-365,-584.3f));
        PlaceReferenceRect(home.transform.Find("Next").GetComponent<RectTransform>(),new Vector2(.5f,.5f),new Vector2(105,90),new Vector2(365,-584.3f));
        var badge=hud.transform.Find("TimerBadge").GetComponent<RectTransform>();PlaceReferenceRect(badge,new Vector2(.5f,1),new Vector2(284.5626f,147.7273f),new Vector2(-2.6702f,-120));
        var timerPanel=ReferenceImage("Timer Text Panel",badge,"timer-text-panel",new Vector2(165.5234f,99.2424f),new Vector2(36.3202f,23.5f));
        timer.transform.SetParent(timerPanel.transform,false);PlaceReferenceRect(timer.rectTransform,new Vector2(.5f,.5f),timerPanel.rectTransform.sizeDelta,Vector2.zero);
        var clock=ReferenceImage("Clock",badge,"clock-body",new Vector2(112,128),new Vector2(-84.8f,28.8f));clock.transform.localScale=Vector3.one*.76f;
        ReferenceImage("Clock Face",clock.transform,"clock-white",new Vector2(110.895f,82.8177f),new Vector2(.5526f,-3.6866f));
        ReferenceImage("Clock Dial",clock.transform,"clock-dial",new Vector2(44.2235f,46.6295f),new Vector2(9.7f,10.1f));
        referenceFreezeLogo=ReferenceImage("Freeze Logo",clock.transform,"snow-flex",new Vector2(98.5956f,111.3682f),new Vector2(0,-1));referenceFreezeLogo.gameObject.SetActive(false);
        var levelBadge=ReferenceImage("Level Display",hud.transform,"LevelNumberBg",new Vector2(232.622f,111),Vector2.zero);PlaceReferenceRect(levelBadge.rectTransform,new Vector2(0,1),levelBadge.rectTransform.sizeDelta,new Vector2(151.6047f,-196));
        levelLabel.transform.SetParent(levelBadge.transform,false);PlaceReferenceRect(levelLabel.rectTransform,new Vector2(.5f,.5f),new Vector2(206.2308f,57.9651f),new Vector2(0,6.6f));
        PlaceReferenceRect(hud.transform.Find("Pause").GetComponent<RectTransform>(),new Vector2(0,1),new Vector2(104,104),new Vector2(151.6f,-80));
        PlaceReferenceRect(pause.transform.Find("Resume").GetComponent<RectTransform>(),new Vector2(.5f,.5f),new Vector2(703,265),new Vector2(0,-313));
        PlaceReferenceRect(pause.transform.Find("Retry").GetComponent<RectTransform>(),new Vector2(.5f,.5f),new Vector2(310,95),new Vector2(-175,-475));
        PlaceReferenceRect(pause.transform.Find("Home").GetComponent<RectTransform>(),new Vector2(.5f,.5f),new Vector2(310,95),new Vector2(175,-475));
        foreach(string key in new[]{"Retry","Home"})PlaceReferenceRect(pause.transform.Find(key+"/Label").GetComponent<RectTransform>(),new Vector2(.5f,.5f),new Vector2(290,75),Vector2.zero);
    }
    void BuildReferenceResultArt(){
        // The original win heading is six authored sprite letters, not TMP text.
        resultBravo=new GameObject("Bravo",typeof(RectTransform));resultBravo.transform.SetParent(result.transform,false);var rect=resultBravo.GetComponent<RectTransform>();PlaceReferenceRect(rect,new Vector2(.5f,.5f),Vector2.zero,new Vector2(-10,600.1f));rect.localScale=Vector3.one*.738f;
        string[] letters={"b","r","a","v","o","!"};float[] xs={-235.6989f,-125.8f,-23.8f,102.6f,226,329.8f};float[] ys={0,5.9f,4.9f,4.5f,6.8f,8.3f};
        for(int i=0;i<letters.Length;i++){var back=ReferenceImage("Letter "+i,rect,letters[i]+"-BG",new Vector2(256,256),new Vector2(xs[i],ys[i]));ReferenceImage("Face",back.transform,letters[i],i==0?new Vector2(171,185):new Vector2(256,256),Vector2.zero);}
        PlaceReferenceRect(resultTitle.rectTransform,new Vector2(.5f,.5f),new Vector2(825.78f,178.4057f),new Vector2(0,416));
        referenceBombInfo=ReferenceImage("Original Bomb Info",result.transform,"info-card-v1",new Vector2(759.6f,511.02f),new Vector2(0,132));
        ReferenceImage("Glow",referenceBombInfo.transform,"FreezeGLow",new Vector2(450,450),new Vector2(0,34));
        ReferenceImage("Bomb Icon",referenceBombInfo.transform,"poison-bomb",new Vector2(337.5f,337.5f),new Vector2(0,34));
        referenceBombInfo.gameObject.SetActive(false);
        resultOutcome=Label("Outcome",result.transform,"",81.45f,Color.white,new Vector2(.5f,.5f),new Vector2(800,135),new Vector2(0,225));
        PlaceReferenceRect(resultDetail.rectTransform,new Vector2(.5f,.5f),new Vector2(780,250),new Vector2(0,-5));ApplyTextStyle(resultDetail,"Base");
        var next=result.transform.Find("Continue/Label").GetComponent<RectTransform>();PlaceReferenceRect(next,new Vector2(.5f,.5f),new Vector2(500,200),Vector2.zero);
        resultBravo.SetActive(false);
    }

}
