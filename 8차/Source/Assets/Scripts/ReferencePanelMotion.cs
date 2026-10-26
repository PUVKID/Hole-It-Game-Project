using UnityEngine;
public class ReferencePanelMotion:MonoBehaviour {
    CanvasGroup group;float start;
    void OnEnable(){if(group==null)group=gameObject.AddComponent<CanvasGroup>();start=Time.unscaledTime;group.alpha=0;}
    void Update(){float t=Mathf.Clamp01((Time.unscaledTime-start)/.18f);group.alpha=1-Mathf.Pow(1-t,3);}
}
