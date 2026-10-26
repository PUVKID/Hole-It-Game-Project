using System.Collections.Generic;
using UnityEngine;

/// <summary>원본의 수집 점수, 단계 예약, 0.3초 후 확대 순서를 보존합니다.</summary>
public sealed class ReferenceGrowthProgress {
    public const float CallbackDelay=.3f;
    readonly List<float> due=new List<float>();
    float clock;
    bool increasing;
    public int Tier {get;private set;}
    public int AppliedTier {get;private set;}
    public float Remainder {get;private set;}
    public float DisplayProgress {get;private set;}
    public bool Pending=>due.Count>0;
    public int Required=>ReferenceGame.GrowthCost(Tier);
    public void Reset(){due.Clear();clock=0;Tier=AppliedTier=0;Remainder=DisplayProgress=0;increasing=false;}
    public void Add(float amount){Remainder+=Mathf.Max(0,amount);UpdateProgress();}
    void UpdateProgress(){
        int cost=Required;
        if(!increasing)DisplayProgress=Mathf.Clamp01(Remainder/cost);
        if(Remainder<cost)return;
        // 원본은 수집 응답 한 번에 다음 단계 한 개를 예약합니다.
        Remainder-=cost;Tier++;increasing=true;due.Add(clock+CallbackDelay);
    }
    public bool Advance(float unscaledDelta){
        clock+=Mathf.Max(0,unscaledDelta);bool expanded=false;
        while(due.Count>0&&due[0]<=clock){
            due.RemoveAt(0);AppliedTier=Tier;DisplayProgress=0;increasing=false;expanded=true;
            if(Remainder>0)UpdateProgress();
        }
        return expanded;
    }
}
