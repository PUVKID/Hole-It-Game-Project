from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent/'Source'
path=ROOT/'Assets/Scripts/ReferenceGame.cs'
s=path.read_text(encoding='utf-8-sig')
s=s.replace('    ReferenceGrowthAnimation growthAnimation;', '''    readonly ReferenceGrowthProgress growthProgress=new ReferenceGrowthProgress();
    public int CurrentGrowthTier=>growthProgress.Tier;
    public int AppliedGrowthTier=>growthProgress.AppliedTier;
    public float GrowthProgressFraction=>growthProgress.DisplayProgress;
    ReferenceGrowthRing sourceGrowthRing;
    readonly Plane[] renderFrustum=new Plane[6];
    ReferenceGrowthAnimation growthAnimation;''')
s=s.replace('Collected=0;GrowthPoints=0;Radius=', 'Collected=0;GrowthPoints=0;growthProgress.Reset();Radius=')
s=s.replace('        if(State==ScreenState.Home)StepAuthoredRest(dt);','''        if(State==ScreenState.Home)StepAuthoredRest(dt);
        if(State==ScreenState.Won)StepPhysicalWorld(dt);''')
old='UpdateFalls(dt);if(State==ScreenState.Playing)UpdateMouth(dt);int tier=GrowthTier(GrowthPoints);growthSpeed.SetTier(tier);Radius=ReferenceMotionSettings.ContactRadiusForTier(SourceCharacterStartRadius,tier)+(playTime<sizeUntil?.30f:0);growthAnimation.Retarget(GrowthVisualRadius(GrowthPoints)*(playTime<sizeUntil?1.3f:1));'
new='''UpdateFalls(dt);if(State==ScreenState.Playing)UpdateMouth(dt);
            bool expanded=growthProgress.Advance(Time.unscaledDeltaTime);if(expanded){growthFlash=Time.unscaledTime+.5f;Sound(1);}
            int tier=AppliedGrowthTier;growthSpeed.SetTier(tier);Radius=ReferenceMotionSettings.ContactRadiusForTier(SourceCharacterStartRadius,tier)+(playTime<sizeUntil?.30f:0);growthAnimation.Retarget(ReferenceMotionSettings.VisualScaleForTier(tier)*(playTime<sizeUntil?1.3f:1));'''
assert old in s
s=s.replace(old,new)
start=s.index('    void UpdateMouth(float dt){')
end=s.index('    void Sound(int id)',start)
s=s[:start]+'''    void UpdateMouth(float dt){
        // 원본 수집 트리거가 기록한 물체를 같은 프레임에 처리합니다.
        for(int k=dropping.Count-1;k>=0;k--){CollectPhysicalActor(dropping[k]);dropping.RemoveAt(k);if(State!=ScreenState.Playing)return;}
        for(int i=0;i<actors.Count;i++){
            if(!ReachedSourceCollectionTrigger(i))continue;
            CollectPhysicalActor(i);if(State!=ScreenState.Playing)return;
        }
        bool done=true;
        if(level.goals.Length==0){foreach(var a in actors)if(a.model.id!=-1&&a.state<2){done=false;break;}}
        else for(int g=0;g<goalCounts.Length;g++)if(goalCounts[g]<level.goals[g].amount)done=false;
        if(done)Win();
    }
    void CollectPhysicalActor(int index){
        var a=actors[index];if(a.state>=2)return;
        // 원본 OnCollected는 점수를 반영해도 강체와 렌더링을 제거하지 않습니다.
        a.state=3;BeginPhysicalDrop(index);
        if(a.model.id==-1){Fail("BOOM! TRY AGAIN");return;}
        Collected++;float value=Mathf.Max(0,a.points);GrowthPoints+=value;growthProgress.Add(value);Sound(0);
        for(int g=0;g<level.goals.Length;g++)if(level.goals[g].id==a.model.id)goalCounts[g]=Mathf.Min(goalCounts[g]+1,level.goals[g].amount);
        UpdateHUD();
    }
''' +s[end:]
s=s.replace('cameraState.SetGrowthTier(GrowthTier(GrowthPoints))','cameraState.SetGrowthTier(CurrentGrowthTier)')
start=s.index('    void RenderActors(){')
end=s.index('    public void RenderForCapture()',start)
s=s[:start]+'''    void RenderActors(){
        foreach(var b in environmentBatches)Graphics.DrawMeshInstanced(b.part.mesh,b.part.submesh,b.part.material,b.matrices,b.count,null,ShadowCastingMode.Off,true,0,cam,LightProbeUsage.Off);
        RenderedActors=0;foreach(var pair in batches)foreach(var b in pair.Value)b.count=0;
        GeometryUtility.CalculateFrustumPlanes(cam,renderFrustum);
        foreach(var a in actors){
            if(a.state==2)continue;
            var position=RenderPosition(a);var rotation=RenderRotation(a);var bounds=WorldBounds(a.model.bounds,rotation,a.scale);bounds.center+=position;
            // 중심점이 내려갔다는 이유로 아직 보이는 큰 물체를 통째로 숨기지 않습니다.
            if(!GeometryUtility.TestPlanesAABB(renderFrustum,bounds))continue;
            RenderedActors++;var matrix=Matrix4x4.TRS(position,rotation,Vector3.one*a.scale);
            foreach(var b in batches[a.model.id]){b.matrices[b.count++]=matrix*b.part.local;if(b.count==1023)Flush(b);}
        }
        foreach(var pair in batches)foreach(var b in pair.Value)Flush(b);
    }
''' +s[end:]
s=s.replace('float needed=GrowthCost(GrowthTier(GrowthPoints))-GrowthRemainder(GrowthPoints);growthFill.fillAmount=GrowthRemainder(GrowthPoints)/GrowthCost(GrowthTier(GrowthPoints));','float needed=growthProgress.Required-growthProgress.Remainder;growthFill.fillAmount=GrowthProgressFraction;')
s=s.replace('if(a.state!=2&&a.model.id!=-1)b.Append','if(a.state<2&&a.model.id!=-1)b.Append')
path.write_text(s,encoding='utf-8')

path=ROOT/'Assets/Scripts/ReferenceGame.Physics.cs'
s=path.read_text(encoding='utf-8-sig')
s=s.replace('MakePhysical(b);SetContactMaterial(b,3);IgnoreFloor(b,true);b.rigid.WakeUp();','SetContactMaterial(b,3);')
path.write_text(s,encoding='utf-8')

path=ROOT/'Assets/Scripts/ReferenceGame.Typography.cs'
s=path.read_text(encoding='utf-8-sig')
s=s.replace('        // The original has no persistent \'points to next size\' sentence or \'Size Up!\' overlay.',
'''        var ringObject=new GameObject("Original Growth Progress");sourceGrowthRing=ringObject.AddComponent<ReferenceGrowthRing>();sourceGrowthRing.Initialize();
        // 원본의 열린 원형 게이지를 사용하고 임의의 점수 문장은 표시하지 않습니다.''')
s=s.replace('worldHoleSize.text="Size "+(GrowthTier(GrowthPoints)+1);','''worldHoleSize.text="Size "+(AppliedGrowthTier+1);
        if(sourceGrowthRing!=null){sourceGrowthRing.gameObject.SetActive(State==ScreenState.Playing);sourceGrowthRing.SetProgress(GrowthProgressFraction,HolePosition+Vector3.up*.012f,growthAnimation.Scale);}''')
s=s.replace('worldHoleSize.transform.localScale=Vector3.one*VisualRadius;',
'''float pulse=1;
        if(Time.unscaledTime<growthFlash){float elapsed=.5f-(growthFlash-Time.unscaledTime);pulse=elapsed<.25f?Mathf.LerpUnclamped(1,1.4f,ReferenceMotionSettings.GrowthOutBack(elapsed/.25f)):Mathf.Lerp(1.4f,1,(elapsed-.25f)/.25f);}
        worldHoleSize.transform.localScale=Vector3.one*VisualRadius*pulse;''')
path.write_text(s,encoding='utf-8')
print('수집·성장·표시 수정 적용 완료')
