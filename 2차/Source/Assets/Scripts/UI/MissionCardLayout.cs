using UnityEngine;
public class MissionCardLayout:MonoBehaviour
{
    private RectTransform rect;private int count=-1;
    private void Awake(){rect=(RectTransform)transform;var background=GetComponent<UnityEngine.UI.Image>();if(background!=null)background.color=Color.clear;foreach(var shadow in GetComponents<UnityEngine.UI.Shadow>())shadow.enabled=false;
        var title=transform.Find("Title");if(title!=null)title.gameObject.SetActive(false);
        foreach(var row in GetComponentsInChildren<MissionRowUI>(true))row.UseCompactCard();
    }
    private void LateUpdate(){int active=0;for(int i=0;i<transform.childCount;i++)if(transform.GetChild(i).GetComponent<MissionRowUI>()!=null&&transform.GetChild(i).gameObject.activeSelf)active++;
        if(active==count)return;count=active;rect.sizeDelta=new Vector2(960,144);int column=0;
        foreach(var row in GetComponentsInChildren<MissionRowUI>()){var r=(RectTransform)row.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,1);r.sizeDelta=new Vector2(290,144);r.anchoredPosition=new Vector2((column-(active-1)*.5f)*304,0);column++;}
    }
}
