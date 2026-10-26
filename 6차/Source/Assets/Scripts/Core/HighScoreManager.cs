using UnityEngine;
/// <summary>Version 2: per-level top three cleared scores. Legacy aggregate keys are not erased.</summary>
[DefaultExecutionOrder(-200)]
public class HighScoreManager : MonoBehaviour
{
    public const int Count=3;
    public const int CampaignCount=30;
    private readonly int[] scores=new int[Count];
    private int selectedLevel=1;
    public int BestScore{get{return scores[0];}}
    public int MaxUnlockedLevel{get;private set;}
    private void Awake(){Load();}
    private string Key(int rank){return "HoleIt.V2.L"+selectedLevel+".Score"+rank;}
    public int GetScore(int rank){return rank>=0&&rank<Count?scores[rank]:0;}
    public void SelectLevel(int level){selectedLevel=Mathf.Clamp(level,1,CampaignCount);Load();}
    public void Load(){
        MaxUnlockedLevel=Mathf.Clamp(PlayerPrefs.GetInt("HoleIt.MaxLevel",1),1,CampaignCount);
        for(int i=0;i<Count;i++)scores[i]=Mathf.Max(0,PlayerPrefs.GetInt(Key(i),0));
        System.Array.Sort(scores);System.Array.Reverse(scores);
    }
    public bool Submit(int score){
        if(score<=0)return false;int position=-1;
        for(int i=0;i<Count;i++)if(score>scores[i]){position=i;break;}
        if(position<0)return false;
        for(int i=Count-1;i>position;i--)scores[i]=scores[i-1];scores[position]=score;
        for(int i=0;i<Count;i++)PlayerPrefs.SetInt(Key(i),scores[i]);
        PlayerPrefs.SetInt("HoleIt.SaveVersion",2);PlayerPrefs.Save();return position==0;
    }
    public void CompleteLevel(int id,int score){SelectLevel(id);Submit(score);PlayerPrefs.SetInt("HoleIt.V2.L"+id+".Complete",1);UnlockLevel(id+1);PlayerPrefs.Save();}
    public bool IsCompleted(int id){return id>=1&&id<=CampaignCount&&PlayerPrefs.GetInt("HoleIt.V2.L"+id+".Complete",0)==1;}
    public void UnlockLevel(int n){n=Mathf.Clamp(n,1,CampaignCount);if(n<=MaxUnlockedLevel)return;MaxUnlockedLevel=n;PlayerPrefs.SetInt("HoleIt.MaxLevel",n);PlayerPrefs.Save();}
}
