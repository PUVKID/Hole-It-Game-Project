using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

public partial class ReferenceGame {
    ReferenceProfile profile;GameObject storePanel,levelsPanel;TextMeshProUGUI wallet,storeMessage,modeLabel,levelPageLabel;Button[] stageButtons;TextMeshProUGUI[] purchaseLabels;int levelPage;float walletClock;TextMeshProUGUI heartCount,heartStatus;Image heartInfinity;
    static long Now=>DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    public bool FreePlay=>IsChallenge||(profile!=null&&profile.freePlay);
    bool PersistProfile=>!Application.isEditor&&!TestInput;
    void LoadProfile(){try{profile=JsonUtility.FromJson<ReferenceProfile>(PlayerPrefs.GetString("Gaeun.Ref.Profile5",""));}catch{}if(profile==null)profile=new ReferenceProfile();profile.Refresh(Now);}
    void SaveProfile(){if(PersistProfile){PlayerPrefs.SetString("Gaeun.Ref.Profile5",JsonUtility.ToJson(profile));PlayerPrefs.Save();}}
    void RefreshWallet(){
        if(profile==null)return;profile.Refresh(Now);
        if(wallet!=null)wallet.text=profile.coins.ToString();
        if(heartCount!=null)heartCount.text=FreePlay?"":profile.hearts.ToString();
        if(heartInfinity!=null)heartInfinity.gameObject.SetActive(FreePlay);
        if(heartStatus!=null){bool full=profile.hearts==5;ApplyReferenceText(heartStatus,full?"HeartFull":"HeartTimer");long seconds=Math.Max(0,profile.refillAt-Now);heartStatus.text=FreePlay?"Free":full?"FULL":(seconds/60).ToString("00")+":"+(seconds%60).ToString("00");}
        if(modeLabel!=null)modeLabel.text=FreePlay?"Mode: Free Play":"Mode: Classic";
    }
    void ProfileTick(){if(Time.unscaledTime<walletClock)return;walletClock=Time.unscaledTime+1;RefreshWallet();RefreshAttendance();}
    void RecordFailure(){if(IsChallenge||TestInput||Application.isEditor)return;profile.Lose(Now);SaveProfile();RefreshWallet();}
    void RecordWin(){if(!FreePlay){profile.coins+=ReferenceProfile.WinReward;unlocked=Mathf.Max(unlocked,Mathf.Min(Catalog.levels.Length,LevelIndex+2));if(PersistProfile)PlayerPrefs.SetInt("Gaeun.Ref.Unlocked",unlocked);SaveProfile();}RefreshWallet();}
    void StyleCard(GameObject panel){panel.AddComponent<ReferencePanelMotion>();var card=panel.transform.Find("Card").GetComponent<Image>();card.sprite=Style("info-win-v3");card.type=Image.Type.Simple;card.color=Color.white;}
    void BuildProfileUI(){
        Btn("Daily Rewards",home.transform,"Daily Rewards",new Vector2(.5f,.425f),new Vector2(550,88),Vector2.zero,Green,OpenAttendance);
        BuildReferenceWallet();
        home.transform.Find("Settings").GetComponent<RectTransform>().anchorMin=home.transform.Find("Settings").GetComponent<RectTransform>().anchorMax=new Vector2(.18f,.065f);home.transform.Find("Settings").GetComponent<RectTransform>().sizeDelta=new Vector2(280,85);
        Btn("Shop",home.transform,"Boosters",new Vector2(.5f,.065f),new Vector2(280,85),Vector2.zero,Blue,OpenStore);
        Btn("Levels",home.transform,"Levels",new Vector2(.82f,.065f),new Vector2(280,85),Vector2.zero,Blue,()=>{levelPage=LevelIndex/20;RefreshLevelPage();levelsPanel.SetActive(true);});
        var mode=Btn("Mode",settings.transform,"",new Vector2(.5f,.5f),new Vector2(560,90),new Vector2(0,-10),Blue,()=>{profile.freePlay=!profile.freePlay;if(!FreePlay&&LevelIndex>=unlocked){LoadLevel(unlocked-1);ShowHome();settings.SetActive(true);}SaveProfile();RefreshWallet();RefreshBoosters();});modeLabel=mode.GetComponentInChildren<TextMeshProUGUI>();
        var controls=settings.transform.Find("Controls").GetComponent<TextMeshProUGUI>();controls.text="Move the Hole\nDrag to move. Release to stop.\nFree Play unlocks all levels.";controls.rectTransform.sizeDelta=new Vector2(770,160);controls.rectTransform.anchorMin=controls.rectTransform.anchorMax=new Vector2(.5f,.5f);controls.rectTransform.anchoredPosition=new Vector2(0,-165);controls.fontSize=30;controls.fontSizeMax=30;controls.fontSizeMin=26;controls.lineSpacing=10;ApplyTextStyle(controls,"Base",false,-2.5f);
        PlaceReferenceRect(settings.transform.Find("Done").GetComponent<RectTransform>(),new Vector2(.5f,.5f),new Vector2(540,100),new Vector2(0,-370));
        storePanel=Layer("Booster Store");CreateModal(storePanel,"Store");StyleCard(storePanel);
        var shopTitle=Label("Title",storePanel.transform,"Shop",90.5f,Color.white,new Vector2(.5f,.5f),new Vector2(825.78f,178.4057f),new Vector2(0,416));ApplyReferenceText(shopTitle,"ShopTitle",true);
        storeMessage=Label("Balance",storePanel.transform,"",27,Color.white,new Vector2(.5f,.625f),new Vector2(790,75),Vector2.zero);
        string[] names={"Potion","Lightning","Finder","Freeze"};purchaseLabels=new TextMeshProUGUI[4];
        for(int i=0;i<4;i++){int index=i;var b=Btn("Buy"+i,storePanel.transform,"",new Vector2(.5f,.565f-i*.056f),new Vector2(760,86),Vector2.zero,Blue,()=>{bool bought=profile.BuyBooster(index);SaveProfile();RefreshStore(bought?"Booster added":"Not enough coins");});purchaseLabels[i]=b.GetComponentInChildren<TextMeshProUGUI>();purchaseLabels[i].fontSize=26;purchaseLabels[i].fontSizeMax=26;purchaseLabels[i].fontSizeMin=20;}
        Btn("Refill",storePanel.transform,"Refill Lives - 900",new Vector2(.5f,.337f),new Vector2(760,80),Vector2.zero,Blue,()=>{bool ok=profile.RefillHearts();SaveProfile();RefreshStore(ok?"Lives refilled":profile.hearts==5?"Lives are full":"Not enough coins");});
        Btn("Done",storePanel.transform,"Done",new Vector2(.5f,.265f),new Vector2(500,90),Vector2.zero,Green,()=>storePanel.SetActive(false));storePanel.SetActive(false);
        levelsPanel=Layer("Level Select");CreateModal(levelsPanel,"Levels");StyleCard(levelsPanel);
        levelPageLabel=Label("Title",levelsPanel.transform,"Levels",45,Color.white,new Vector2(.5f,.725f),new Vector2(800,100),Vector2.zero);stageButtons=new Button[20];
        for(int i=0;i<20;i++){int slot=i;stageButtons[i]=Btn("Stage"+i,levelsPanel.transform,"",new Vector2(.5f,.61f),new Vector2(140,95),new Vector2((i%5-2)*157,-(i/5)*112),Blue,()=>{LoadLevel(levelPage*20+slot);ShowHome();});}
        Btn("BackPage",levelsPanel.transform,"<",new Vector2(.25f,.335f),new Vector2(160,85),Vector2.zero,Blue,()=>{levelPage=Mathf.Max(0,levelPage-1);RefreshLevelPage();});
        Btn("NextPage",levelsPanel.transform,">",new Vector2(.75f,.335f),new Vector2(160,85),Vector2.zero,Blue,()=>{levelPage=Mathf.Min((Catalog.levels.Length-1)/20,levelPage+1);RefreshLevelPage();});
        Btn("Done",levelsPanel.transform,"Done",new Vector2(.5f,.26f),new Vector2(500,90),Vector2.zero,Green,()=>levelsPanel.SetActive(false));levelsPanel.SetActive(false);RefreshWallet();
    }
    void CloseProfilePanels(){CloseAttendance();CloseExtraContentPanels();if(storePanel!=null)storePanel.SetActive(false);if(levelsPanel!=null)levelsPanel.SetActive(false);}
    void OpenStore(){RefreshStore("");storePanel.SetActive(true);}
    void RefreshStore(string message){RefreshWallet();storeMessage.text=message.Length>0?message:profile.coins+" Coins - Win a level to earn 20";string[] names={"Potion","Lightning","Finder","Freeze"};for(int i=0;i<4;i++)purchaseLabels[i].text=names[i]+"  x "+profile.boosters[i]+"      Buy - 1800";RefreshBoosters();}
    void RefreshLevelPage(){levelPageLabel.text="Levels "+(levelPage*20+1)+"-"+Mathf.Min(Catalog.levels.Length,levelPage*20+20);for(int i=0;i<20;i++){int stage=levelPage*20+i;stageButtons[i].gameObject.SetActive(stage<Catalog.levels.Length);stageButtons[i].interactable=FreePlay||stage<unlocked;stageButtons[i].GetComponentInChildren<TextMeshProUGUI>().text=(stage+1).ToString();}}
    void BuildReferenceWallet(){
        Transform Badge(string name,float x){var root=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();root.SetParent(home.transform,false);PlaceReferenceRect(root,new Vector2(.5f,1),new Vector2(258.8f,136),new Vector2(x,-162.4f));root.localScale=Vector3.one*.9f;ReferenceImage("DisplayBG",root,"point-panel",new Vector2(229.1783f,105.3212f),new Vector2(16.1f,.5f));return root;}
        var coins=Badge("Coin Display",145);ReferenceImage("Coin Icon",coins,"Coin-UI",new Vector2(120,165.9f),new Vector2(-73.2f,7.4f));
        wallet=Label("Wallet",coins,"0",37.9f,Color.white,new Vector2(.5f,.5f),new Vector2(100,67),new Vector2(43.6443f,5.5023f));ApplyReferenceText(wallet,"CoinCount");
        var hearts=Badge("Heart Display",-145);var icon=ReferenceImage("Heart Icon",hearts,"Heart",new Vector2(128.8f,200),new Vector2(-73.2f,9.6f));
        heartCount=Label("Heart Count",icon.transform,"5",50,Color.white,new Vector2(.5f,.5f),new Vector2(98.6234f,66.9771f),Vector2.zero);ApplyReferenceText(heartCount,"HeartCount");
        heartInfinity=ReferenceImage("Free Play Heart",icon.transform,"heart-loop",new Vector2(92,58),Vector2.zero);
        heartStatus=Label("Heart Status",hearts,"FULL",40,Color.white,new Vector2(.5f,.5f),new Vector2(110,67),new Vector2(48.2336f,5.5023f));ApplyReferenceText(heartStatus,"HeartFull");
    }

}
