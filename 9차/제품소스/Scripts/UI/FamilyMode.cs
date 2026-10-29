using UnityEngine;
using UnityEngine.UI;
/// <summary>Optional untimed play and contextual instructions; fully offline.</summary>
public class FamilyMode : MonoBehaviour
{
    public HoleItText modeLabel, hint;
    public Button modeButton;
    public static bool Relaxed { get { return PlayerPrefs.GetInt("PocketPit.Relaxed",0)==1; } }
    private GameState previous=GameState.Boot;
    private float hintStart;
    private void Start(){modeButton.onClick.AddListener(Toggle);Refresh();}
    private void Toggle(){PlayerPrefs.SetInt("PocketPit.Relaxed",Relaxed?0:1);PlayerPrefs.Save();Refresh();}
    private void Refresh(){modeLabel.SetText(Relaxed?"여유 모드 · 시간 제한 없음":"도전 모드 · 시간 안에 모으기");}
    private void Update(){
        var gm=GameManager.Instance;if(gm==null)return;
        if(gm.State!=previous){previous=gm.State;hintStart=Time.unscaledTime;
            var level=FindFirstObjectByType<LevelManager>().Current;
            if(level!=null)hint.SetText(level.defaultWrongBehavior==WrongTargetBehavior.Blocked?"목표 물건만 삼킬 수 있어요":level.defaultWrongBehavior==WrongTargetBehavior.Penalty?"다른 물건은 감점! 목표를 찾아요":"손가락으로 끌어 보세요 · 작은 물건부터 쏙!");
        }
        hint.gameObject.SetActive(gm.State==GameState.Playing && Time.unscaledTime-hintStart<8);
    }
}
