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
    private void Refresh(){modeLabel.SetText(Relaxed?"RELAXED · NO TIME LIMIT":"CHALLENGE · BEAT THE CLOCK");}
    private void Update(){
        var gm=GameManager.Instance;if(gm==null)return;
        if(gm.State!=previous){previous=gm.State;hintStart=Time.unscaledTime;
            var level=FindFirstObjectByType<LevelManager>().Current;
            if(level!=null)hint.SetText(level.defaultWrongBehavior==WrongTargetBehavior.Blocked?"Collect targets. Avoid bombs!":level.defaultWrongBehavior==WrongTargetBehavior.Penalty?"Wrong items cost time. Avoid bombs!":"Drag to collect. Avoid bombs!");
        }
        hint.gameObject.SetActive(gm.State==GameState.Playing && Time.unscaledTime-hintStart<8);
    }
}
