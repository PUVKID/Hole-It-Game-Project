using System;
using UnityEngine;

[Serializable] public sealed class ReferenceProfile {
    public int coins,hearts=5;public long refillAt;public int[] boosters={1,1,1,1};public bool freePlay;
    public const int WinReward=20,BoosterPrice=1800,HeartPrice=900,RefillSeconds=1800;
    public void Refresh(long now){
        hearts=Mathf.Clamp(hearts,0,5);coins=Mathf.Max(0,coins);
        if(boosters==null||boosters.Length!=4)boosters=new[]{1,1,1,1};
        if(hearts==5){refillAt=0;return;}if(refillAt<=0){refillAt=now+RefillSeconds;return;}
        if(now>=refillAt){long added=(now-refillAt)/RefillSeconds+1;hearts=(int)Math.Min(5,hearts+added);refillAt=hearts==5?0:refillAt+added*RefillSeconds;}
    }
    public void Lose(long now){Refresh(now);if(freePlay)return;if(hearts==5)refillAt=now+RefillSeconds;hearts=Mathf.Max(0,hearts-1);}
    public bool BuyBooster(int i){if(i<0||i>=4||coins<BoosterPrice)return false;coins-=BoosterPrice;boosters[i]++;return true;}
    public bool RefillHearts(){if(hearts>=5||coins<HeartPrice)return false;coins-=HeartPrice;hearts=5;refillAt=0;return true;}
    public bool Consume(int i){if(freePlay)return true;if(boosters[i]<=0)return false;boosters[i]--;return true;}
}
