using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>미션 한 줄: [아이콘] 이름  현재 / 필요  [✓]</summary>
public class MissionRowUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private GameObject checkMark;
    [SerializeField] private Image background;
    [SerializeField] private UIPunch iconPunch;
    [SerializeField] private UIPunch countPunch;
    [SerializeField] private Color normalColor = new Color(0.30f, 0.22f, 0.57f, 0.84f);
    [SerializeField] private Color doneColor = new Color(0.10f, 0.64f, 0.43f, 0.96f);

    private int required;

    public void Setup(EdibleData data, int required)
    {
        this.required = required;
        gameObject.SetActive(true);
        if (icon != null)
        {
            icon.sprite = data.icon;
            icon.color = data.icon != null ? Color.white : data.uiColor;
        }
        if (nameText != null) nameText.SetText(data.displayName);
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
