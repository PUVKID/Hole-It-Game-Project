using UnityEngine;
using UnityEngine.UI;

/// <summary>미션 한 줄: [아이콘] 이름  현재 / 필요  [✓]</summary>
public class MissionRowUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private HoleItText nameText;
    [SerializeField] private HoleItText countText;
    [SerializeField] private GameObject checkMark;
    [SerializeField] private Image background;
    [SerializeField] private UIPunch iconPunch;
    [SerializeField] private UIPunch countPunch;
    [SerializeField] private Color normalColor = new Color(0.30f, 0.22f, 0.57f, 0.84f);
    [SerializeField] private Color doneColor = new Color(0.10f, 0.64f, 0.43f, 0.96f);

    private int required;
    public void UseCompactCard(){
        normalColor=new Color(1,.95f,.98f,.97f);doneColor=new Color(.64f,1,.82f,.98f);
        nameText.color=new Color(.24f,.16f,.35f);nameText.alignment=TextAnchor.MiddleCenter;nameText.fontSize=27;nameText.resizeTextMaxSize=27;
        countText.color=new Color(.84f,.16f,.40f);countText.alignment=TextAnchor.MiddleCenter;countText.fontSize=44;countText.resizeTextMaxSize=44;
        Place(icon.rectTransform,new Vector2(.5f,1),new Vector2(0,-12),new Vector2(83,83));
        Place(nameText.rectTransform,new Vector2(.5f,1),new Vector2(0,-105),new Vector2(252,38));
        Place(countText.rectTransform,new Vector2(.5f,1),new Vector2(0,-148),new Vector2(245,55));
        Place(checkMark.transform as RectTransform,new Vector2(1,1),new Vector2(-27,-25),new Vector2(24,24));
    }
    private static void Place(RectTransform r,Vector2 a,Vector2 p,Vector2 s){r.anchorMin=r.anchorMax=a;r.pivot=new Vector2(.5f,1);r.anchoredPosition=p;r.sizeDelta=s;}

    public void Setup(MissionData mission)
    {
        this.required = mission.requiredCount;
        EdibleData data = mission.target;
        gameObject.SetActive(true);
        if (icon != null)
        {
            icon.sprite = mission.Icon;
            icon.color = mission.Icon != null ? Color.white : data != null ? data.uiColor : Color.white;
        }
        if (nameText != null) nameText.SetText(mission.Label);
        if (checkMark != null) checkMark.SetActive(false);
        if (background != null) background.color = normalColor;
        SetCount(0, false);
    }

    public void Hide() { gameObject.SetActive(false); }

    public void SetCount(int current, bool punch)
    {
        if (countText != null) countText.SetText("{0} / {1}", current, required);
        if (punch && countPunch != null) countPunch.Play();
    }

    public void SetComplete()
    {
        if (checkMark != null) checkMark.SetActive(true);
        if (background != null) background.color = doneColor;
        if (iconPunch != null) iconPunch.Play(1.6f);
        if (countPunch != null) countPunch.Play(1.6f);
    }
}
