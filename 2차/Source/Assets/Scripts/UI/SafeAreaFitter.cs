using UnityEngine;
/// <summary>Fits a responsive logical canvas inside the actual safe area, including short portrait tablets.</summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    [SerializeField] private Vector2 minimumLogicalSize=new Vector2(1080,1920);
    private RectTransform rt;private Canvas canvas;private Rect lastSafe;private Vector2Int lastScreen;private float lastCanvasScale=-1;
    private void Awake(){rt=(RectTransform)transform;canvas=GetComponentInParent<Canvas>();Apply();}
    private void OnEnable(){Apply();}
    private void Update(){float scale=canvas!=null?canvas.scaleFactor:1;if(Screen.safeArea!=lastSafe||Screen.width!=lastScreen.x||Screen.height!=lastScreen.y||Mathf.Abs(scale-lastCanvasScale)>.0001f)Apply();}
    private void Apply(){
        if(rt==null)rt=(RectTransform)transform;if(canvas==null)canvas=GetComponentInParent<Canvas>();
        if(Screen.width<=0||Screen.height<=0)return;
        Rect safe=Screen.safeArea;if(safe.width<=0||safe.height<=0)safe=new Rect(0,0,Screen.width,Screen.height);
        float canvasScale=canvas!=null?Mathf.Max(.001f,canvas.scaleFactor):1;Vector2 available=safe.size/canvasScale;
        float fit=Mathf.Min(1,Mathf.Min(available.x/Mathf.Max(1,minimumLogicalSize.x),available.y/Mathf.Max(1,minimumLogicalSize.y)));fit=Mathf.Max(.01f,fit);
        // Anchor to the safe area's centre. Logical size grows for tall/wide displays instead of clipping.
        Vector2 centre=new Vector2(safe.center.x/Screen.width,safe.center.y/Screen.height);
        rt.anchorMin=rt.anchorMax=centre;rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=Vector2.zero;
        rt.sizeDelta=available/fit;rt.localScale=new Vector3(fit,fit,1);
        lastSafe=Screen.safeArea;lastScreen=new Vector2Int(Screen.width,Screen.height);lastCanvasScale=canvasScale;
    }
}
