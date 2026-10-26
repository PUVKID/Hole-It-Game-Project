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
        bootProgress.fillAmount=.85f;bootStatus.text="Preparing your adventure";yield return null;
        InitializeGame();EnhancePresentation();BuildProfileUI();BuildExtraContentUI();BuildAttendanceUI();Destroy(bootCamera.gameObject);Ready=true;bootProgress.fillAmount=1;bootStatus.text="Ready!";
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
        Label("Dedication",bootCanvas.transform,"FOR GAEUN",30,Color.white,new Vector2(.5f,.82f),new Vector2(800,60),Vector2.zero);
        var bg=Panel("Loading Progress",bootCanvas.transform,new Vector2(.5f,.085f),new Vector2(760,38),new Color(.18f,.1f,.04f,.9f),Vector2.zero);bg.sprite=Resources.Load<Sprite>("ReferenceStyle/spScreen-loading_BG");
        bootProgress=Panel("Fill",bg.transform,new Vector2(.5f,.5f),new Vector2(740,24),Color.white,Vector2.zero);bootProgress.sprite=Resources.Load<Sprite>("ReferenceStyle/progress-green");bootProgress.type=Image.Type.Filled;bootProgress.fillMethod=Image.FillMethod.Horizontal;bootProgress.fillAmount=0;
        bootStatus=Label("Status",bootCanvas.transform,"Loading your adventure",25,Color.white,new Vector2(.5f,.12f),new Vector2(880,60),Vector2.zero);
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
        var controls=settings.transform.Find("Controls").GetComponent<TextMeshProUGUI>();controls.text="JOYSTICK\nDrag to move. Release to stop.\n\n465 LEVELS - 1,010 COLLECTABLES";controls.rectTransform.anchoredPosition=new Vector2(0,60);
        var soundButton=Btn("Sound",settings.transform,mute?"Sound: Off":"Sound: On",new Vector2(.5f,.42f),new Vector2(560,90),Vector2.zero,Blue,()=>{});
        soundButton.onClick.AddListener(()=>{mute=!mute;PlayerPrefs.SetInt("Gaeun.Ref.Mute",mute?1:0);PlayerPrefs.Save();soundButton.GetComponentInChildren<TextMeshProUGUI>().text=mute?"Sound: Off":"Sound: On";});
        RestoreReferenceTextStyles();
        var hint=Label("Level Count",home.transform,"465 levels to explore",24,Color.white,new Vector2(.5f,.225f),new Vector2(800,45),Vector2.zero);WhiteOutline(hint);
    }
}
