from pathlib import Path
import shutil,zipfile,hashlib,json,xml.etree.ElementTree as ET
w=Path(__file__).parent.resolve();p=w/'AndroidFinal';d=Path('C:/Users/PUVKID/Desktop/Hole It/5차')
assert not (d/'HoleIt_5차_APK_Source.zip').exists(),'Preserve finished release'
assert not d.exists() or set(x.name for x in d.iterdir()).issubset({'Source','Gaeun_Hole_5.apk'}),'Unexpected release contents'
assert 'Succeeded' in (p/'TestResults/AndroidBuild.txt').read_text()
v=(w/'reference5-apk-verification.txt').read_text(encoding='utf-8-sig')
assert 'Verifies' in v and "versionName='5.0.0'" in v and "versionCode='7'" in v
for n in ['reference5-final-tests.xml']:
 assert ET.parse(w/n).getroot().attrib['result']=='Passed',n
assert '03b2e61e715a8e2fd3854fb1acddf905a453766389b9765f1e15281b4767c5fa' in v
assert (w/'reference5-final-previews/00-loading.png').stat().st_size>100000
assert 'activeInputHandler: 1' in (p/'ProjectSettings/ProjectSettings.asset').read_text()
assert 'start valid=False' not in (p/'TestResults/FifthStageAudit.txt').read_text()
d.mkdir(parents=True,exist_ok=True);shutil.copy2(p/'Builds/Android/HoleIt.apk',d/'Gaeun_Hole_5.apk')
s=d/'Source';s.mkdir(exist_ok=True)
for n in ['Assets','Packages','ProjectSettings']:
 shutil.copytree(p/n,s/n,dirs_exist_ok=True,ignore=lambda folder,names:[n for n in names if n.startswith('InitTestScene') or n=='.DS_Store'])
(s/'Build-Android.ps1').write_text((w/'Build-Android.ps1').read_text(encoding='utf-8-sig').replace('AndroidRelease.Build','ReferenceBuilder.BuildAndroid'),encoding='utf-8-sig')
for n in ['.gitignore','AGENTS.md']:shutil.copy2(d.parent/'4차/Source'/n,s/n)
(s/'README.txt').write_text('''Gaeun's Hole — edition 5 — Unity 6000.3.6f1
Open Assets/Scenes/Main.unity. Build with Build-Android.ps1 and Android Build Support.
App version 5.0.0 (7), com.pocketplayground.pocketpit; ARM64 and ARMv7.
Input System Package (New) is required. ReferenceInputBuildGuard validates it.
Run ReferenceTests, ReferenceTouchTests, ReferenceFifthTests, ReferenceProfileTests.

Runtime: ReferenceGame and its Physics, Presentation, Profile partial files.
Content: ReferenceCatalog, 465 main levels and 869 collectable models. Export data
and built assets are included. No original APK or original native binaries are required.
English UI uses bundled Nanum Gothic Bold and OFL. CotonArt contains newly generated
loading/home art; see Art/Coton-Art-Prompts.txt. Reference audio and visual assets
are derived from the supplied game and remain their owners' content.

The serialized catalog is ready to build. ReferenceBuilder.Prepare reconstructs
content from JSON but resets some presentation settings. For a content regeneration,
run ReferenceFifthBuilder.Prepare then ReferenceFifthBuilder.RefreshAudio.
ReferenceBuilder.BuildAndroid is the release build entry. Do not use legacy builders.

See Verification/Reference-Comparison.txt for retained data, approximated behavior,
and verification limits. Windows previews are not Android screenshots. No physical
Android device was connected during this build.
''',encoding='utf-8')
art=d/'Art';art.mkdir();shutil.copy2(w/'Coton-Art-Prompts.txt',art/'Coton-Art-Prompts.txt')
for n in ['Loading.png','Home.png']:shutil.copy2(p/'Assets/Resources/CotonArt'/n,art/n)
pre=d/'Previews';shutil.copytree(w/'reference5-final-previews',pre)
qa=d/'Verification';qa.mkdir()
for n in ['reference5-final-tests.xml','reference5-apk-verification.txt','reference5-final-preview-v2.log']:shutil.copy2(w/n,qa/n)
for n in ['AndroidBuild.txt','FifthStageAudit.txt','ReferenceCampaign.txt','ReferenceStructures.txt']:
 if (p/'TestResults'/n).exists():shutil.copy2(p/'TestResults'/n,qa/n)
shutil.copy2(w/'fifth-colliders/report.json',qa/'Collision-Hulls.json')
for n in ['game-settings.json','package-size.json']:shutil.copy2(w/'reference5-audit'/n,qa/n)
shutil.copy2(w/'reference5-comparison.txt',qa/'Reference-Comparison.txt')
(d/'읽어주세요.txt').write_text('''가은이 홀 게임 · 5차 · 5.0.0 (7)

설치: Gaeun_Hole_5.apk
APK + 전체 소스: HoleIt_5차_APK_Source.zip
기존 차수와 앱 식별자·서명이 같아 업데이트 설치가 가능합니다.
4차 및 이전 차수는 보존했습니다.

꼬똥 드 툴레아 캐릭터로 로딩과 홈 배경을 새로 제작했습니다.
465개 메인 맵, 869종 수집 모형, 원본 배치·목표·제한 시간·성장 점수를
반영했습니다. 성장 비용은 단계마다 차감되며 남은 점수는 이월됩니다.
가까운 물체에 물리 접촉·회전·중력을 적용하고 물체를 끌어오는 흡입은
넣지 않았습니다. 폭탄을 삼키면 실패하며 RETRY로 다시 시작합니다.

PLAY → 왼쪽 아래 조이스틱을 끌면 이동, 손을 떼면 멈춥니다.
빈 바닥을 드래그해도 조이스틱이 나타납니다. 멀리 끌어도 속도 상한이 있습니다.
SETTINGS → MODE: FREE PLAY를 켜면 465개 맵을 모두 선택할 수 있고
생명과 부스터를 소비하지 않습니다. 자유 플레이는 기본 진행도와 분리됩니다.
CLASSIC은 순서대로 잠금 해제됩니다. 승리 보상 20코인, 실패 시 생명 1개,
생명 1개 충전 30분, 부스터 1800코인, 생명 충전 900코인입니다.
BOOSTERS에서 게임 내 코인으로 구매합니다. 현금 결제는 없습니다.
부스터는 SIZE 3레벨 / SPEED 5레벨 / FIND 9레벨 / FREEZE 12레벨부터입니다.
영어 UI이며 나눔고딕 Bold 실제 글꼴과 라이선스를 포함했습니다.

Verification에 테스트와 빌드·서명 검사 결과를 넣었습니다.
465개 맵의 데이터·시작 위치 검사는 전체 플레이 클리어 검사와 다릅니다.
Previews는 Windows 실행 화면입니다. Android 실제 터치/FPS는 미검증입니다.
원본과 모든 요소가 100% 동일하거나 오류가 전혀 없다고 보증하는 빌드는 아닙니다.
구체적인 구현 차이는 Verification/Reference-Comparison.txt에 기록했습니다.
''',encoding='utf-8-sig')
manifest={str(f.relative_to(d)).replace('\\','/'):hashlib.sha256(f.read_bytes()).hexdigest() for f in d.rglob('*') if f.is_file()}
(d/'SHA256.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False),encoding='utf-8')
zpath=d/'HoleIt_5차_APK_Source.zip'
with zipfile.ZipFile(zpath,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
 for f in d.rglob('*'):
  if f.is_file() and f!=zpath:z.write(f,str(f.relative_to(d)))
with zipfile.ZipFile(zpath) as z:
 assert z.testzip() is None
 assert hashlib.sha256(z.read('Gaeun_Hole_5.apk')).hexdigest()==manifest['Gaeun_Hole_5.apk']
 for n in ['Source/Assets/Resources/Fonts/OFL.txt','Source/Assets/Scripts/ReferenceGame.Physics.cs','Art/Home.png']:assert n in z.namelist()
print(json.dumps({'folder':str(d),'apkBytes':(d/'Gaeun_Hole_5.apk').stat().st_size,'zipBytes':zpath.stat().st_size,'files':len(manifest)},ensure_ascii=False))
