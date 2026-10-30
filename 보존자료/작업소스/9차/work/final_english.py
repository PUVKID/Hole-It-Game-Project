from pathlib import Path
import re, math
p=Path(__file__).parent/'AndroidFinal'/'Assets'
def edit(path,old,new):
    f=p/path;s=f.read_text(encoding='utf-8-sig');assert old in s,(path,old);f.write_text(s.replace(old,new),encoding='utf-8')
# Keep the old class name for existing builder compatibility, but migrate only legacy Korean.
f=p/'Scripts/UI/Korean.cs';s=f.read_text(encoding='utf-8-sig');s=s[:s.index('    public static string Text')]+'''    // Legacy scene migration. English strings pass through unchanged.
    public static string Text(string value){
        if(string.IsNullOrEmpty(value))return value??"";
        foreach(var pair in words)if(value==pair.Value)return pair.Key;
        return value.Replace("나의 최고 기록","TOP SCORES").Replace("최고 기록!","NEW BEST!")
            .Replace("잠깐 쉬어가요","PAUSED").Replace("최고 기록","BEST SCORE").Replace("점수","SCORE")
            .Replace("스테이지 ","LEVEL ").Replace("크기 ","SIZE ")
            .Replace("수집  +","Objects  +").Replace("미션  +","Mission  +").Replace("시간  +","Time  +").Replace("완료  +","Clear  +")
            .Replace("물건을 모으고, 폭탄은 피하세요!","COLLECT. GROW. AVOID BOMBS.")
            .Replace("도전 모드 · 시간 안에 모으기","CHALLENGE · BEAT THE CLOCK")
            .Replace("여유 모드 · 시간 제한 없음","RELAXED · NO TIME LIMIT");
    }
}
''';f.write_text(s,encoding='utf-8')
edit(Path('Scripts/UI/FamilyMode.cs'),'여유 모드 · 시간 제한 없음','RELAXED · NO TIME LIMIT')
edit(Path('Scripts/UI/FamilyMode.cs'),'도전 모드 · 시간 안에 모으기','CHALLENGE · BEAT THE CLOCK')
edit(Path('Scripts/UI/FamilyMode.cs'),'목표만 모으고 폭탄은 피하세요!','Collect targets. Avoid bombs!')
edit(Path('Scripts/UI/FamilyMode.cs'),'다른 물건은 감점 · 폭탄은 실패!','Wrong items cost time. Avoid bombs!')
edit(Path('Scripts/UI/FamilyMode.cs'),'물건을 모아 주세요 · 폭탄을 삼키면 실패!','Drag to collect. Avoid bombs!')
edit(Path('Scripts/Core/GameManager.cs'),'폭탄을 삼켰어요!','BOOM! TRY AGAIN')
edit(Path('Scripts/UI/UIManager.cs'),'"크기 "+tier+"  ·  성장까지 "+Mathf.CeilToInt(hole.SizeTable.GetTier(tier+1).growthRequired-hole.Growth)+"개"','"SIZE "+tier+"  ·  "+Mathf.CeilToInt(hole.SizeTable.GetTier(tier+1).growthRequired-hole.Growth)+" TO GROW"')
edit(Path('Scripts/UI/UIManager.cs'),'"크기 "+tier+"  ·  최대 크기"','"SIZE "+tier+"  ·  MAX SIZE"')
edit(Path('Scripts/Core/InputManager.cs'),'hole.ClampPosition(hole.Position+Vector3.ClampMagnitude(DragTarget+worldDelta-hole.Position,1.2f))','hole.ClampPosition(DragTarget+worldDelta)')
edit(Path('Scripts/Hole/HoleController.cs'),'Vector3.MoveTowards(before,controls.DragTarget,CurrentMaxSpeed()*dt)','Vector3.Lerp(before,controls.DragTarget,1f-Mathf.Exp(-35f*dt))')
edit(Path('Tests/PlayMode/PrecisionPlayTests.cs'),'Vector3.ClampMagnitude((b.GetPoint(db)-a.GetPoint(da))*.85f,1.2f)','(b.GetPoint(db)-a.GetPoint(da))*.85f')
edit(Path('Tests/PlayMode/PrecisionPlayTests.cs'),'성장까지 8개','8 TO GROW')
edit(Path('Tests/PlayMode/PrecisionPlayTests.cs'),'시간이 다 됐어요','TIME UP')
edit(Path('Tests/PlayMode/PrecisionPlayTests.cs'),'gm.TimeRemaining,65','gm.TimeRemaining,45')
edit(Path('Tests/PlayMode/PrecisionPlayTests.cs'),'gm.TimeRemaining,40','gm.TimeRemaining,20')
edit(Path('Tests/PlayMode/BombPlayTests.cs'),'폭탄을 삼켰어요!','BOOM! TRY AGAIN')
edit(Path('Tests/PlayMode/ReleasePlayTests.cs'),'Assert.AreEqual("NanumGothic-Bold",text.font.name);','Assert.AreEqual("NanumGothic-Bold",text.font.name);Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(text.text,"[가-힣]"),text.name+": "+text.text);')
# English names and deterministic, shorter challenge budgets.
f=p/'Editor/HoleItBuilder.GameplayEdition.cs';s=f.read_text(encoding='utf-8-sig')
s=s.replace('"립스틱","향수","매니큐어","크림","팩트","로션","비누","브러시","세럼","마카롱","주스","초콜릿"','"Lipstick","Perfume","Nail Polish","Cream","Compact","Lotion","Soap","Brush","Serum","Macaron","Juice","Chocolate"')
s=s.replace('        for(int i=0;i<beauty.Length;i++)','        foreach(var spec in AllSpecs()){var d=Edible(spec.key);d.displayName=spec.display;EditorUtility.SetDirty(d);}\n        Edible("Bomb").displayName="Bomb";\n        for(int i=0;i<beauty.Length;i++)')
s=s.replace('"화장대","디저트 가게","목욕 시간","피크닉","장난감 방","케이크 가게","향기로운 선물","작은 카페","자동차 거리","간식 파티"','"Vanity","Dessert Shop","Bath Time","Picnic","Toy Room","Cake Shop","Beauty Gifts","Little Cafe","Car Street","Snack Party"')
s=s.replace('"작은 모둠","계단 광장","동그란 길","낮은 언덕","작은 성","나선 정원"','"Clusters","Steps","Circle","Terraces","Castle","Columns"')
report=(p.parent/'TestResults/PhysicalCampaign.txt').read_text()
times=[float(x) for x in re.findall(r'simulated seconds=([\d.]+)',report)]
assert len(times)==30,len(times)
budgets=[min(45,max(25,math.ceil((t*1.5+8)/5)*5)) for t in times]
s=s.replace('level.timeLimit=45+round*5+(pattern==2?10:pattern==4?5:0);','int[] budgets={'+','.join(map(str,budgets))+'};level.timeLimit=budgets[level.levelId-1];')
s=s.replace('LayoutKoreanUI();PlayerSettings.bundleVersion','LayoutEnglishUI();PlayerSettings.productName="Pocket Pit";PlayerSettings.bundleVersion')
s=s.replace('LayoutKoreanUI()','LayoutEnglishUI()')
s=s.replace('            foreach(var shadow','            text.text=Korean.Text(text.text);\n            foreach(var shadow')
s=s.replace('"물건을 모으고, 폭탄은 피하세요!"','"COLLECT. GROW. AVOID BOMBS."')
s=s.replace('        var ui=UnityEngine.Object.FindFirstObjectByType<UIManager>();','''        foreach(string name in new[]{"Logo","Tagline","Best"}){var t=title.Find(name).GetComponent<HoleItText>();t.color=new Color(.26f,.20f,.30f);}
        var fail=canvas.transform.Find("FailPanel");
        foreach(string name in new[]{"Top3Header","Top1","Top2","Top3"})fail.Find(name).gameObject.SetActive(false);
        UIRect(fail.Find("ResultCard"),new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(850,1160));
        MoveUI(fail.Find("Title"),.72f);MoveUI(fail.Find("Score"),.64f);MoveUI(fail.Find("NewBest"),.57f);
        MoveUI(fail.Find("Missions"),.47f);MoveUI(fail.Find("RetryButton"),.33f);MoveUI(fail.Find("HomeButton"),.24f);
        var ui=UnityEngine.Object.FindFirstObjectByType<UIManager>();''')
s=s.replace('        var mode=canvas.GetComponentInChildren<FamilyMode>', '''        foreach(var text in canvas.GetComponentsInChildren<HoleItText>(true))if(System.Text.RegularExpressions.Regex.IsMatch(text.text,"[가-힣]"))throw new Exception("Untranslated UI: "+text.name+" = "+text.text);
        var mode=canvas.GetComponentInChildren<FamilyMode>''')
f.write_text(s,encoding='utf-8')
print('English UI, accurate drag and challenge budgets prepared:',budgets)
