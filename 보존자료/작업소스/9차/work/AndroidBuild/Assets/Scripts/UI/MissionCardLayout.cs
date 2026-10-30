using UnityEngine;
public class MissionCardLayout:MonoBehaviour
{
    private RectTransform rect;private int count=-1;
    private void Awake(){rect=(RectTransform)transform;}
    private void LateUpdate(){int active=0;for(int i=0;i<transform.childCount;i++)if(transform.GetChild(i).GetComponent<MissionRowUI>()!=null&&transform.GetChild(i).gameObject.activeSelf)active++;
        if(active==count)return;count=active;rect.sizeDelta=new Vector2(rect.sizeDelta.x,66+active*78);
    }
}
