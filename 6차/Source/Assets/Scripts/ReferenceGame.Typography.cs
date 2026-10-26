using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class ReferenceGame {
    readonly Dictionary<string,Material> typographyMaterials=new Dictionary<string,Material>();
    TMP_FontAsset LoadTypographyFont(){
        var original=Resources.Load<TMP_FontAsset>("ReferenceTypography/LinikSansBlack");
        if(original==null)throw new InvalidOperationException("Original typography is missing. Run ReferenceTypographyBuilder.Prepare before building.");
        return original;
    }
    // Materials retain the reference's signed-distance-field outline and underlay values.
    void ApplyTextStyle(TextMeshProUGUI text,string materialKey,bool bold=false,float spacing=0){
        if(!typographyMaterials.TryGetValue(materialKey,out var material)){
            material=Resources.Load<Material>("ReferenceTypography/Materials/"+materialKey);
            if(material==null)throw new InvalidOperationException("Original text material missing: "+materialKey);
            typographyMaterials[materialKey]=material;
        }
        text.fontSharedMaterial=material;text.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;
        text.characterSpacing=spacing;text.extraPadding=true;
    }
    void SetInterfaceScale(float match){if(canvas!=null){var area=Screen.safeArea;float aspect=area.width/Mathf.Max(1,area.height);canvas.GetComponent<CanvasScaler>().matchWidthOrHeight=aspect<.5624f?0:match;}}
    void RestoreReferenceTextStyles(){
        ApplyTextStyle(timer,"Base",false,2.8f);timer.enableAutoSizing=false;timer.fontSize=50;
        ApplyTextStyle(levelLabel,"Base",false,1.8f);levelLabel.enableAutoSizing=false;levelLabel.fontSize=36.8f;
        var play=home.transform.Find("Play/Label").GetComponent<TextMeshProUGUI>();
        ApplyTextStyle(play,"Play_Green",true,-10);play.fontSize=67.5f;play.fontSizeMax=67.5f;play.fontSizeMin=50;
        ApplyTextStyle(homeLevel,"Play_Green",true,-10);
        ApplyTextStyle(holeSize,"Outline_InGame");ApplyTextStyle(growthToast,"Outline_InGame",true);
        foreach(var panel in new[]{result,pause,settings}){
            var title=panel.transform.Find("Title").GetComponent<TextMeshProUGUI>();
            ApplyTextStyle(title,"Outline_Overlay_Purple_Title",true,-10);title.fontSize=95;title.fontSizeMax=95;title.fontSizeMin=64;
            foreach(var text in panel.GetComponentsInChildren<TextMeshProUGUI>(true)){
                text.color=Color.white;
                if(text!=title&&text.transform.parent.GetComponent<Button>()==null)ApplyTextStyle(text,"Base");
            }
        }
    }
}
