from pathlib import Path
p=Path(__file__).parent/'AndroidFinal/Assets/Scripts'
f=p/'ReferenceGame.cs';s=f.read_text(encoding='utf-8-sig')
s=s.replace('mute=PlayerPrefs.GetInt','LoadProfile();mute=PlayerPrefs.GetInt',1)
s=s.replace('public void StartPlay(){State=', 'public void StartPlay(){profile.Refresh(Now);if(!TestInput&&!Application.isEditor&&!FreePlay&&profile.hearts<=0){ShowHome();OpenStore();storeMessage.text="NO LIVES · USE FREE PLAY IN SETTINGS";return;}CloseProfilePanels();State=')
s=s.replace('homeLevel.text="LEVEL "+(LevelIndex+1);}', 'homeLevel.text="LEVEL "+(LevelIndex+1);CloseProfilePanels();RefreshWallet();}')
s=s.replace('void Update(){if(!Ready)return;', 'void Update(){if(!Ready)return;ProfileTick();')
s=s.replace('void Fail(string reason){State=', 'void Fail(string reason){RecordFailure();State=')
a=s.index('unlocked=Mathf.Max(unlocked',s.index('void Win()'));b=s.index('ShowResult(true',a);s=s[:a]+'RecordWin();'+s[b:]
s=s.replace('Mathf.Min(unlocked-1,LevelIndex+1)','Mathf.Min(FreePlay?Catalog.levels.Length-1:unlocked-1,LevelIndex+1)')
s=s.replace('LevelIndex+1<activation[index]||boostUsed[index])return;boostUsed[index]=true;', '(!FreePlay&&LevelIndex+1<activation[index])||boostUsed[index])return;if(!profile.Consume(index))return;SaveProfile();boostUsed[index]=true;')
s=s.replace('Sound(4);RefreshBoosters();','Sound(index==3?8:index==1?9:4);RefreshBoosters();')
a=s.index('void RefreshBoosters()');b=s.index('    void UpdateHoleLabels()',a)
s=s[:a]+'''void RefreshBoosters(){int[] activation={3,5,9,12};string[] names={"SIZE","SPEED","FIND","FREEZE"};for(int i=0;i<4;i++){bool available=FreePlay||LevelIndex+1>=activation[i];boostButtons[i].interactable=available&&!boostUsed[i]&&(FreePlay||profile.boosters[i]>0);boostLabels[i].text=!available?"LV "+activation[i]:boostUsed[i]?"USED":names[i]+(FreePlay?"":" × "+profile.boosters[i]);}}
'''+s[b:]
s=s.replace('Label("Label",im.transform,text,34', 'if(!name.StartsWith("Booster")){im.sprite=Style(color==Green?"btn-big-green":"btn-big-Blue")??im.sprite;im.type=Image.Type.Simple;im.color=Color.white;}Label("Label",im.transform,text,34')
f.write_text(s,encoding='utf-8')
f=p/'ReferenceGame.Presentation.cs';s=f.read_text();s=s.replace('Canvas bootCanvas;', 'Camera bootCamera;Canvas bootCanvas;')
s=s.replace('InitializeGame();EnhancePresentation();Ready=true;', 'InitializeGame();EnhancePresentation();BuildProfileUI();Destroy(bootCamera.gameObject);Ready=true;')
s=s.replace('font=Resources.Load<Font>', 'bootCamera=new GameObject("Loading Camera",typeof(Camera)).GetComponent<Camera>();bootCamera.cullingMask=0;bootCamera.clearFlags=CameraClearFlags.SolidColor;bootCamera.backgroundColor=new Color(.94f,.75f,.40f);\n        font=Resources.Load<Font>',1)
s=s.replace('Style("prebosster-toolkit-"+i)', 'Style("Booster"+new[]{0,2,3,1}[i])')
f.write_text(s,encoding='utf-8')
f=p/'ReferenceCapture.cs';s=f.read_text();s=s.replace('Directory.CreateDirectory(folder);yield return new WaitForEndOfFrame();','Directory.CreateDirectory(folder);yield return null;yield return new WaitForEndOfFrame();');f.write_text(s,encoding='utf-8')
f=p/'ReferenceGame.Physics.cs';s=f.read_text();s=s.replace('shape.mesh.name=="Cube"?', '(shape.mesh.name=="Cube"||shape.mesh.name=="G-3075635067240017007")?');s=s.replace('else if(col is BoxCollider box)box.size=Vector3.one;', 'else if(col is BoxCollider box){box.center=shape.mesh.bounds.center;box.size=shape.mesh.bounds.size;}');f.write_text(s,encoding='utf-8')
f=p.parent/'Editor/ReferenceFifthBuilder.cs';s=f.read_text();s=s.replace('public static void RefreshAudio(){','public static void RefreshAudio(){\n        foreach(var path in Directory.GetFiles("Assets/Resources/ReferenceStyle","Booster*.png")){var t=(TextureImporter)AssetImporter.GetAtPath(path.Replace(\'\\\\\',\'/\'));t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;t.mipmapEnabled=false;t.alphaIsTransparency=true;t.SaveAndReimport();}')
f.write_text(s,encoding='utf-8')
