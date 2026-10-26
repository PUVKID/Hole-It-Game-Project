using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class ReferenceGame {
    struct AuthoredPlaygroundTile {public Rect local;public Matrix4x4 matrix,inverse;public Vector3 scale;}
    readonly List<AuthoredPlaygroundTile> authoredPlayground=new List<AuthoredPlaygroundTile>();
    void AddAuthoredPlayground(ReferenceModel model,ReferenceEnvironment placement){
        var matrix=Matrix4x4.TRS(new Vector3(placement.position[0],placement.position[1],placement.position[2]),new Quaternion(placement.rotation[0],placement.rotation[1],placement.rotation[2],placement.rotation[3]),new Vector3(placement.scale[0],placement.scale[1],placement.scale[2]));
        var bounds=model.bounds;var rect=Rect.MinMaxRect(bounds.min.x,bounds.min.z,bounds.max.x,bounds.max.z);
        authoredPlayground.Add(new AuthoredPlaygroundTile{local=rect,matrix=matrix,inverse=matrix.inverse,scale=matrix.lossyScale});
        Bounds world=default;for(int c=0;c<4;c++){var p=matrix.MultiplyPoint3x4(new Vector3((c&1)==0?rect.xMin:rect.xMax,bounds.center.y,(c&2)==0?rect.yMin:rect.yMax));if(c==0)world=new Bounds(p,Vector3.zero);else world.Encapsulate(p);}
        playground.Add(Rect.MinMaxRect(world.min.x,world.min.z,world.max.x,world.max.z));
    }
    bool AuthoredPlaygroundContains(Vector2 point){
        if(authoredPlayground.Count==0)return true;
        foreach(var tile in authoredPlayground){var local=tile.inverse.MultiplyPoint3x4(new Vector3(point.x,tile.matrix.m13,point.y));if(tile.local.Contains(new Vector2(local.x,local.z)))return true;}
        return false;
    }
    // Indices and parameters from the supplied EnvironmentVariantSo. Campaign th defaults to 0.
    static readonly int[] SourceThemeVariants={1,2,3,8,9};
    static readonly float[] SourceThemeShadows={.6f,1f,1f,1f,.6f};
    static readonly float[] SourceCharacterRadii={.62f,.62f,.28f,.28f,.62f};
    int activeChallenge=-1,pendingChallenge=-1,campaignBeforeChallenge,challengePage,collectionIndex;
    GameObject challengesPanel,collectionPanel;
    Button[] challengeButtons;
    TextMeshProUGUI challengePageLabel,collectionTitle;
    ReferenceModelPreview collectionPreview;
    public bool IsChallenge=>activeChallenge>=0;
    public int ChallengeIndex=>activeChallenge;
    public int CurrentThemeIndex=>Mathf.Clamp(level==null?0:level.themeIndex,0,SourceThemeVariants.Length-1);
    public int CurrentThemeVariant=>SourceThemeVariants[CurrentThemeIndex];
    public float SourceCharacterStartRadius=>SourceCharacterRadii[CurrentThemeIndex];
    public int AuthoredBombCount=>level.obstacles==null?0:level.obstacles.Length;
    public string CurrentStageLabel=>IsChallenge?"Challenge "+(activeChallenge+1):"Level "+(LevelIndex+1);

    ReferenceLevel ResolveLevelForLoad(int mainIndex){
        activeChallenge=pendingChallenge;pendingChallenge=-1;
        return IsChallenge?Catalog.challengeLevels[activeChallenge]:Catalog.levels[mainIndex];
    }
    void ApplyThemeSettings(){sun.shadowStrength=SourceThemeShadows[CurrentThemeIndex];}
    ReferenceModel EnvironmentModel(int kind){
        int id=kind==9?9:CurrentThemeVariant*10+kind;
        // The source deliberately references the convex prefab for both corners in theme index 2.
        if(CurrentThemeIndex==2&&kind==0)id=31;
        foreach(var m in Catalog.environments)if(m.id==id)return m;
        return null;
    }
    void BuildReferenceBombs(){
        int firstBomb=actors.Count;
        var model=Catalog.bomb;
        if(model==null||model.parts==null||model.parts.Length==0)throw new InvalidOperationException("Source poison bomb model missing");
        if(level.obstacles!=null&&level.obstacles.Length>0){
            foreach(var o in level.obstacles){
                if(o.type!=1)throw new InvalidOperationException("Unsupported source obstacle "+o.type);
                AddPoison(model,new Vector3(o.position[0],o.position[1],o.position[2]),new Quaternion(o.rotation[0],o.rotation[1],o.rotation[2],o.rotation[3]),o.scale,false,o.isPhysics);
            }
            // Authored obstacle stages retain the exact source transforms, without invented extras.
            AttachBombSupports(firstBomb);return;
        }
        if(level.bombs<=0)return;
        // Thin stacked props can have no support-graph children even when other items sit above
        // them. Test the entire new bomb volume before replacing any collectible.
        var sourceBounds=new Bounds[actors.Count];
        for(int i=0;i<sourceBounds.Length;i++){var a=actors[i];sourceBounds[i]=WorldBounds(a.model.bounds,a.rotation,a.scale);sourceBounds[i].center+=a.pos;}
        var bombBounds=WorldBounds(model.bounds,Quaternion.identity,1.5f);
        float bombHalfX=bombBounds.extents.x,bombHalfZ=bombBounds.extents.z,bombHeight=bombBounds.size.y;
        bool ClearSpawn(Vector3 position,int excluded){
            if((new Vector2(position.x-HolePosition.x,position.z-HolePosition.z)).sqrMagnitude<25)return false;
            float rx=bombHalfX+.25f,rz=bombHalfZ+.25f;
            foreach(var corner in new[]{new Vector2(-rx,-rz),new Vector2(-rx,rz),new Vector2(rx,-rz),new Vector2(rx,rz)})
                if(!OnPlayground(new Vector2(position.x,position.z)+corner))return false;
            for(int j=0;j<sourceBounds.Length;j++){
                if(j==excluded||actors[j].state==2)continue;var bounds=sourceBounds[j];
                if(bounds.min.y>bombHeight+.25f||bounds.max.y<-.05f)continue;
                if(Mathf.Abs(bounds.center.x-position.x)<bounds.extents.x+bombHalfX+.3f&&Mathf.Abs(bounds.center.z-position.z)<bounds.extents.z+bombHalfZ+.3f)return false;
            }
            return true;
        }
        var candidates=new List<int>();
        for(int i=0;i<actors.Count;i++){
            var a=actors[i];if(a.model.id<0||a.state!=0||a.Bottom>.15f||a.above.Count>0||(a.pos-HolePosition).sqrMagnitude<25)continue;
            bool goal=false;foreach(var g in level.goals)if(g.id==a.model.id){goal=true;break;}
            if(!goal&&OnPlayground(new Vector2(a.pos.x,a.pos.z)))candidates.Add(i);
        }
        int seed=17;foreach(char ch in level.source)seed=unchecked(seed*31+ch);
        var random=new System.Random(seed);var chosen=new List<Vector3>();
        for(int n=0;n<level.bombs&&candidates.Count>0;){
            int slot=random.Next(candidates.Count),i=candidates[slot];candidates.RemoveAt(slot);var a=actors[i];
            bool close=false;foreach(var p in chosen)if((p-a.pos).sqrMagnitude<9){close=true;break;}if(close)continue;
            if(!ClearSpawn(a.pos,i))continue;
            a.state=2;chosen.Add(a.pos);AddPoison(model,a.pos,Quaternion.identity,1.5f,true);n++;
        }
        // Dense collect-all stacks may have no replaceable footprint. Empty authored floor
        // remains a valid hazard location and preserves every collectible in that stack.
        int firstTile=playground.Count>0?random.Next(playground.Count):0;
        for(int t=0;t<playground.Count&&chosen.Count<level.bombs;t++){
            var tile=playground[(firstTile+t)%playground.Count];
            for(int z=0;z<3&&chosen.Count<level.bombs;z++)for(int x=0;x<3&&chosen.Count<level.bombs;x++){
                var p=new Vector3(Mathf.Lerp(tile.xMin,tile.xMax,(x+1)*.25f),0,Mathf.Lerp(tile.yMin,tile.yMax,(z+1)*.25f));
                bool close=false;foreach(var prior in chosen)if((new Vector2(prior.x-p.x,prior.z-p.z)).sqrMagnitude<9){close=true;break;}
                if(close||!ClearSpawn(p,-1))continue;chosen.Add(p);AddPoison(model,p,Quaternion.identity,1.5f,true);
            }
        }
        if(chosen.Count<level.bombs)Debug.LogWarning("Stage "+level.source+" has only "+chosen.Count+" non-overlapping random bomb positions for "+level.bombs+" requested bombs.");
        AttachBombSupports(firstBomb);
    }
    public int RandomBombOverlapCountForTest(){
        if(level.obstacles!=null&&level.obstacles.Length>0)return 0;int count=0;
        foreach(var bomb in actors){if(bomb.model.id!=-1)continue;var bb=WorldBounds(bomb.model.bounds,bomb.rotation,bomb.scale);bb.center+=bomb.pos;
            foreach(var a in actors){if(a.model.id<0||a.state==2)continue;var b=WorldBounds(a.model.bounds,a.rotation,a.scale);b.center+=a.pos;
                if(b.min.y>=bb.max.y||b.max.y<=bb.min.y)continue;
                if(Mathf.Abs(b.center.x-bb.center.x)<b.extents.x+bb.extents.x&&Mathf.Abs(b.center.z-bb.center.z)<b.extents.z+bb.extents.z)count++;
            }
        }
        return count;
    }
    void AttachBombSupports(int firstBomb){
        for(int i=firstBomb;i<actors.Count;i++){
            var a=actors[i];if(a.Bottom<.15f)continue;float highest=-1;
            Query(a.pos,a.radius+2,nearby);
            foreach(int j in nearby){if(i==j)continue;var b=actors[j];if(b.state!=0||b.Bottom>=a.Bottom-.1f||b.Top>a.Bottom+.35f)continue;
                float dx=a.pos.x-b.pos.x,dz=a.pos.z-b.pos.z;if(dx*dx+dz*dz>Mathf.Pow(a.radius+b.radius*.75f,2))continue;
                if(b.Top>highest){highest=b.Top;a.support=j;}
            }
            if(a.support>=0)actors[a.support].above.Add(i);
        }
    }
    void AddPoison(ReferenceModel model,Vector3 position,Quaternion rotation,float scale,bool restOnFloor,bool authoredPhysics=false){
        if(Quaternion.Dot(rotation,rotation)<.01f)rotation=Quaternion.identity;else rotation=Quaternion.Normalize(rotation);
        var b=WorldBounds(model.bounds,rotation,scale);
        if(restOnFloor)position.y=-b.min.y;
        var actor=new Actor{model=model,placement=new ReferencePoint{id=-1,physicsMode=authoredPhysics?1:0},pos=position,rotation=rotation,scale=scale,radius=Mathf.Max(.08f,Mathf.Min(b.extents.x,b.extents.z)),bottomOffset=b.min.y,topOffset=b.max.y};
        actors.Add(actor);AddCell(actors.Count-1);
    }
    public Vector3[] BombPositionsForTest(){var result=new List<Vector3>();foreach(var a in actors)if(a.model.id==-1)result.Add(a.pos);return result.ToArray();}
    public int PillarCountForTest(){int count=0;foreach(var p in level.environment)if(p.kind==9)count++;return count;}
    public void LoadChallenge(int index){
        if(Catalog.challengeLevels==null||Catalog.challengeLevels.Length==0)return;
        if(!IsChallenge)campaignBeforeChallenge=LevelIndex;
        pendingChallenge=Mathf.Clamp(index,0,Catalog.challengeLevels.Length-1);LoadLevel(0);StartPlay();
    }
    void ReturnFromChallenge(){if(IsChallenge){activeChallenge=-1;pendingChallenge=-1;LoadLevel(campaignBeforeChallenge);}}
    void RetryStage(){if(IsChallenge){LoadChallenge(activeChallenge);return;}LoadLevel(LevelIndex);StartPlay();}
    void NextStage(){if(IsChallenge){LoadChallenge((activeChallenge+1)%Catalog.challengeLevels.Length);return;}LoadLevel(Mathf.Min(Catalog.levels.Length-1,LevelIndex+1));StartPlay();}
    void CloseExtraContentPanels(){if(challengesPanel!=null)challengesPanel.SetActive(false);if(collectionPanel!=null)collectionPanel.SetActive(false);}
    void BuildExtraContentUI(){
        Btn("Challenges",home.transform,"CHALLENGES",new Vector2(.29f,.355f),new Vector2(380,85),Vector2.zero,Blue,()=>{CloseProfilePanels();RefreshChallengePage();challengesPanel.SetActive(true);});
        Btn("Collection",home.transform,"COLLECTION",new Vector2(.71f,.355f),new Vector2(380,85),Vector2.zero,Blue,()=>{CloseProfilePanels();collectionPanel.SetActive(true);ShowCollectionModel(0);});
        challengesPanel=Layer("Bonus Challenges");CreateModal(challengesPanel,"Challenges");StyleCard(challengesPanel);
        challengePageLabel=Label("Title",challengesPanel.transform,"CHALLENGES",43,Color.white,new Vector2(.5f,.74f),new Vector2(850,90),Vector2.zero);
        Label("Explanation",challengesPanel.transform,"EXTRA MAPS - FREE PRACTICE",25,Color.white,new Vector2(.5f,.675f),new Vector2(830,60),Vector2.zero);
        challengeButtons=new Button[20];
        for(int i=0;i<20;i++){int slot=i;challengeButtons[i]=Btn("Challenge"+i,challengesPanel.transform,"",new Vector2(.5f,.61f),new Vector2(140,95),new Vector2((i%5-2)*157,-(i/5)*112),Blue,()=>LoadChallenge(challengePage*20+slot));}
        Btn("Previous",challengesPanel.transform,"<",new Vector2(.25f,.335f),new Vector2(160,85),Vector2.zero,Blue,()=>{challengePage=Mathf.Max(0,challengePage-1);RefreshChallengePage();});
        Btn("Next",challengesPanel.transform,">",new Vector2(.75f,.335f),new Vector2(160,85),Vector2.zero,Blue,()=>{challengePage=Mathf.Min((Catalog.challengeLevels.Length-1)/20,challengePage+1);RefreshChallengePage();});
        Btn("Done",challengesPanel.transform,"DONE",new Vector2(.5f,.26f),new Vector2(500,90),Vector2.zero,Green,()=>challengesPanel.SetActive(false));challengesPanel.SetActive(false);
        collectionPanel=Layer("3D Collection");CreateModal(collectionPanel,"Collection");StyleCard(collectionPanel);
        collectionTitle=Label("Title",collectionPanel.transform,"COLLECTION",40,Color.white,new Vector2(.5f,.73f),new Vector2(830,120),Vector2.zero);
        var image=new GameObject("Model View",typeof(RectTransform),typeof(RawImage));image.transform.SetParent(collectionPanel.transform,false);
        var rect=image.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,.52f);rect.sizeDelta=new Vector2(640,640);
        image.GetComponent<RawImage>().raycastTarget=false;collectionPreview=image.AddComponent<ReferenceModelPreview>();
        Label("Caption",collectionPanel.transform,"DRAG TO ROTATE",23,Color.white,new Vector2(.5f,.345f),new Vector2(650,45),Vector2.zero);
        image.GetComponent<RawImage>().raycastTarget=true;
        Btn("Previous",collectionPanel.transform,"<",new Vector2(.25f,.31f),new Vector2(160,85),Vector2.zero,Blue,()=>ShowCollectionModel(-1));
        Btn("Next",collectionPanel.transform,">",new Vector2(.75f,.31f),new Vector2(160,85),Vector2.zero,Blue,()=>ShowCollectionModel(1));
        Btn("Done",collectionPanel.transform,"DONE",new Vector2(.5f,.25f),new Vector2(420,85),Vector2.zero,Green,()=>collectionPanel.SetActive(false));collectionPanel.SetActive(false);
    }
    void RefreshChallengePage(){
        challengePageLabel.text="CHALLENGES "+(challengePage*20+1)+"-"+Mathf.Min(Catalog.challengeLevels.Length,challengePage*20+20);
        for(int i=0;i<challengeButtons.Length;i++){int index=challengePage*20+i;challengeButtons[i].gameObject.SetActive(index<Catalog.challengeLevels.Length);challengeButtons[i].GetComponentInChildren<TextMeshProUGUI>().text=(index+1).ToString();}
    }
    void ShowCollectionModel(int delta){collectionIndex=(collectionIndex+delta+Catalog.models.Length)%Catalog.models.Length;var model=Catalog.models[collectionIndex];collectionTitle.text=model.label.ToUpperInvariant()+"\n"+(collectionIndex+1)+" / "+Catalog.models.Length;collectionPreview.Show(model);}
}
