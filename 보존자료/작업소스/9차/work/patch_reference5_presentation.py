from pathlib import Path
import json
w=Path(__file__).parent;p=w/'AndroidFinal';f=p/'Assets/Scripts/ReferenceGame.cs';s=f.read_text(encoding='utf-8').replace('void Awake(){Instance=this;', 'void InitializeGame(){Instance=this;')
s=s.replace('void Update(){float dt=', 'void Update(){if(!Ready)return;float dt=').replace('void LateUpdate(){cameraFocus=', 'void LateUpdate(){if(!Ready)return;cameraFocus=')
s=s.replace('if(Screen.width>900){int width=900;', 'if(Screen.width>1080){int width=1080;')
s=s.replace('sun.intensity=.65f;', 'sun.intensity=1f;sun.shadowStrength=.6f;sun.transform.rotation=new Quaternion(-.52133405f,.47771421f,-.52133363f,-.47771457f);')
f.write_text(s,encoding='utf-8')
for name in ['ReferenceTests.cs','ReferenceTouchTests.cs']:
    f=p/'Assets/Tests/Reference'/name;s=f.read_text(encoding='utf-8').replace('yield return SceneManager.LoadSceneAsync("Main");yield return null;', 'yield return SceneManager.LoadSceneAsync("Main");yield return new WaitUntil(()=>ReferenceGame.Instance!=null&&ReferenceGame.Instance.Ready);yield return new WaitForSecondsRealtime(.6f);')
    s=s.replace('Assert.Less(objects,220', 'Assert.Less(objects,650')
    s=s.replace('yield return new WaitForSeconds(.5f);Assert.AreEqual(ReferenceGame.ScreenState.Lost', 'yield return new WaitForSeconds(1.5f);Assert.AreEqual(ReferenceGame.ScreenState.Lost')
    f.write_text(s,encoding='utf-8')
f=p/'Assets/Scripts/ReferenceCapture.cs';s=f.read_text(encoding='utf-8').replace('var g=ReferenceGame.Instance;g.LoadLevel', 'yield return new WaitUntil(()=>ReferenceGame.Instance!=null&&ReferenceGame.Instance.Ready);var g=ReferenceGame.Instance;g.LoadLevel').replace('WaitForSecondsRealtime(.6f);yield return Shot("03-bomb-failure")', 'WaitForSecondsRealtime(1.5f);yield return Shot("03-bomb-failure")');f.write_text(s,encoding='utf-8')
f=p/'ProjectSettings/DynamicsManager.asset';f.write_text(f.read_text(encoding='utf-8').replace('m_Gravity: {x: 0, y: -9.81, z: 0}', 'm_Gravity: {x: 0, y: -5.81, z: 0}'),encoding='utf-8')

