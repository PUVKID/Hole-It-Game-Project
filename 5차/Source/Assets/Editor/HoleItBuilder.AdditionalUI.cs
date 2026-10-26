
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static partial class HoleItBuilder
{
    private static void BuildAdditionalUI(RectTransform root,RectTransform title,RectTransform pause,RectTransform clear,RectTransform fail,UIManager manager) {
        SetRef(manager,"settingsButton",NewButton("Settings",title,"SETTINGS",30,CardPurple,new Vector2(.25f,.10f),Vector2.zero,new Vector2(390,94)));
        SetRef(manager,"levelSelectButton",NewButton("Levels",title,"LEVELS",30,CardPurple,new Vector2(.75f,.10f),Vector2.zero,new Vector2(390,94)));
        SetRef(manager,"pauseRestartButton",NewButton("Restart",pause,"RESTART",32,CardPurple,new Vector2(.5f,.24f),Vector2.zero,new Vector2(500,94)));
        SetRef(manager,"pauseSettingsButton",NewButton("Settings",pause,"SETTINGS",32,CardPurple,new Vector2(.5f,.16f),Vector2.zero,new Vector2(500,94)));
        SetRef(manager,"clearRetryButton",NewButton("Replay",clear,"REPLAY",30,CardPurple,new Vector2(.5f,.12f),Vector2.zero,new Vector2(500,94)));
        SetRef(manager,"clearMissionsText",CenterText(clear,"Missions","",27,Mint,.41f,170,false));
        SetRef(manager,"failMissionsText",CenterText(fail,"Missions","",25,Mint,.58f,130,false));
        RectTransform options=NewPanel("SettingsPanel",root,true);options.GetComponent<Image>().color=DeepNavy;
        CenterText(options,"Header","SETTINGS",64,Color.white,.88f,130,true);
        Button mode=NewButton("ControlMode",options,"CONTROL: DRAG",34,CardPurple,new Vector2(.5f,.73f),Vector2.zero,new Vector2(830,112));
        Button sound=NewButton("Sound",options,"SOUND: ON",34,CardPurple,new Vector2(.5f,.64f),Vector2.zero,new Vector2(830,112));
        SetRef(manager,"settingsPanel",options.gameObject);SetRef(manager,"controlModeButton",mode);SetRef(manager,"controlModeText",mode.GetComponentInChildren<HoleItText>());SetRef(manager,"muteButton",sound);SetRef(manager,"muteText",sound.GetComponentInChildren<HoleItText>());
        SetRef(manager,"masterSlider",ExtraSlider(options,"MASTER",.52f,.85f));SetRef(manager,"musicSlider",ExtraSlider(options,"MUSIC",.40f,.3f));SetRef(manager,"sfxSlider",ExtraSlider(options,"SOUND EFFECTS",.28f,.8f));
        SetRef(manager,"settingsCloseButton",NewButton("Done",options,"DONE",38,Mint,new Vector2(.5f,.12f),Vector2.zero,new Vector2(830,110)));options.gameObject.SetActive(false);
        RectTransform selection=NewPanel("LevelSelect",root,true);selection.GetComponent<Image>().color=DeepNavy;
        CenterText(selection,"Header","CHOOSE A LEVEL",58,Color.white,.89f,110,true);Object[] buttons=new Object[30];
        for(int i=0;i<buttons.Length;i++)buttons[i]=NewButton("Level"+(i+1),selection,(i+1).ToString(),40,CardPurple,new Vector2(.5f,.80f),new Vector2((i%5-2)*180,-(i/5)*164),new Vector2(152,136));
        SetRefs(manager,"levelButtons",buttons);SetRef(manager,"levelSelectPanel",selection.gameObject);SetRef(manager,"levelSelectCloseButton",NewButton("Back",selection,"BACK",38,Mint,new Vector2(.5f,.12f),Vector2.zero,new Vector2(820,108)));selection.gameObject.SetActive(false);
        Object[] popups=new Object[8];for(int i=0;i<popups.Length;i++){var label=CenterText(root,"ScorePopup"+i,"+10",38,Gold,.5f,70,true);label.gameObject.SetActive(false);popups[i]=label;}SetRefs(manager,"popupTexts",popups);
    }
    private static Slider ExtraSlider(Transform parent,string label,float anchor,float value){
        CenterText(parent,label+"Label",label,30,Color.white,anchor+.04f,60,false);
        RectTransform root=Place(NewRect(label,parent),new Vector2(.5f,anchor),new Vector2(.5f,.5f),Vector2.zero,new Vector2(790,66));
        Image track=NewImage("Track",root,CardPurple,roundedSprite,true);Stretch(track.rectTransform);track.rectTransform.sizeDelta=new Vector2(0,-42);
        Image fill=NewImage("Fill",root,Mint,roundedSprite,false);Stretch(fill.rectTransform);fill.rectTransform.sizeDelta=new Vector2(-30,-46);
        Image handle=NewImage("Handle",root,Mint,roundedSprite,true);Place(handle.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(58,58));
        Slider slider=root.gameObject.AddComponent<Slider>();slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.minValue=0;slider.maxValue=1;slider.value=value;return slider;
    }
}
