from pathlib import Path
import shutil,zipfile,hashlib,json,xml.etree.ElementTree as ET
w=Path(__file__).parent.resolve();p=w/'AndroidFinal'
d=Path('C:/Users/PUVKID/Desktop/Hole It/4차')
assert not d.exists() or not list(d.iterdir()),'Preserve existing release'
assert 'Succeeded' in (p/'TestResults/AndroidBuild.txt').read_text()
verification=(w/'reference4-apk-verification.txt').read_text(encoding='utf-8-sig')
assert 'Verifies' in verification and "versionName='4.1.0'" in verification and "versionCode='6'" in verification
for n in ['reference4-touch-tests.xml','reference4-regression-tests.xml']:
    assert ET.parse(w/n).getroot().attrib['result']=='Passed',n
assert 'activeInputHandler: 1' in (p/'ProjectSettings/ProjectSettings.asset').read_text()
d.mkdir(parents=True,exist_ok=True)
shutil.copy2(p/'Builds/Android/HoleIt.apk',d/'Gaeun_Hole_4.apk')
s=d/'Source';s.mkdir()
for name in ['Assets','Packages','ProjectSettings']:
    shutil.copytree(p/name,s/name,ignore=lambda folder,names:[n for n in names if n.startswith('InitTestScene') or n=='.DS_Store'])
(s/'Build-Android.ps1').write_text((w/'Build-Android.ps1').read_text(encoding='utf-8-sig').replace('AndroidRelease.Build','ReferenceBuilder.BuildAndroid'),encoding='utf-8-sig')
for n in ['.gitignore','AGENTS.md','README.txt']:
    text=(d.parent/'3차/Source'/n).read_text(encoding='utf-8-sig').replace('edition 3','edition 4')
    (s/n).write_text(text,encoding='utf-8')
with (s/'AGENTS.md').open('a',encoding='utf-8') as f:f.write('\n- Active Input Handling must remain Input System Package (New), activeInputHandler: 1. ReferenceInputBuildGuard rejects incompatible builds. Run ReferenceTouchTests after changes to input or UI.\n')
with (s/'README.txt').open('a',encoding='utf-8') as f:f.write('\nEdition 4 fixes the disabled input backend and displays the joystick at idle.\nInput tests inject TouchState into an actual Input System Touchscreen device,\nincluding PLAY/PAUSE/RESUME UI clicks, drag movement, release, cancellation and\nsecond-finger ownership. Tests do not replace the movement input with TestDirection.\n')
pre=d/'Previews';pre.mkdir()
for n in ['01-home.png','02-first-level.png','04-joystick.png']:
    shutil.copy2(w/'reference4-previews'/n,pre/n)
qa=d/'Verification';qa.mkdir()
for n in ['reference4-touch-tests.xml','reference4-regression-tests.xml','reference4-apk-verification.txt']:
    shutil.copy2(w/n,qa/n)
shutil.copy2(p/'TestResults/AndroidBuild.txt',qa/'AndroidBuild.txt')
shutil.copy2(w/'reference-comparison.txt',qa/'Reference-Comparison.txt')
(qa/'Input-Fix.txt').write_text('''Root cause: edition 3 selected the legacy backend (activeInputHandler: 0),
but ReferenceGame.ReadInput was entirely guarded by ENABLE_INPUT_SYSTEM.
The Android player therefore contained no executable joystick polling path.
Earlier movement tests used TestInput/TestDirection and missed this defect.

Edition 4 selects the new Input System (activeInputHandler: 1). The joystick
is visible while playing even at rest, above the booster buttons. Touching
it uses its displayed center. Dragging another empty area starts a floating
stick there. Movement is speed limited; releasing or canceling stops it.
ReferenceInputBuildGuard rejects inconsistent backend configurations.

ReferenceTouchTests exercises Input System Touchscreen events and the real
runtime/UI code, without movement override. This is desktop simulation,
not physical Android touchscreen verification. No phone was connected.
Previews are Windows standalone renders. The Android APK build, package
version, architectures and signature were checked separately.
''',encoding='utf-8')
(d/'읽어주세요.txt').write_text('''가은이 홀 게임 · 4차 · 4.1.0 (6)

설치 파일: Gaeun_Hole_4.apk
전체 소스와 APK: HoleIt_4차_APK_Source.zip

3차에서 조이스틱 입력 코드와 APK의 입력 방식 설정이 달라 이동 입력이
실행되지 않던 오류를 수정했습니다. 3차와 동일한 앱 식별자/서명으로
제작하여 기존 앱 위에 업데이트 설치할 수 있습니다. 진행 기록은 유지합니다.

PLAY를 누르면 왼쪽 아래, 부스터 버튼 위에 조이스틱이 항상 보입니다.
조이스틱을 원하는 방향으로 당긴 채 누르고 있으면 계속 이동합니다.
손을 떼면 즉시 멈춥니다. 화면의 다른 빈 곳을 눌러 드래그해도 됩니다.
멀리 당겨도 최고 속도를 넘지 않습니다.

첫 30개 맵, 모형, 성장 규칙, 제한 시간 및 폭탄 규칙은 3차를 유지합니다.
화면 언어는 영어, 실제 Nanum Gothic Bold 글꼴과 OFL을 포함합니다.
Source는 Unity 6000.3.6f1 프로젝트입니다. Build-Android.ps1로 빌드합니다.

가상 터치로 PLAY / 드래그 / 정지 / 취소 / 멀티터치 / 일시정지·재개를
검사했습니다. APK 빌드 및 서명 검증도 통과했습니다.
연결된 Android 기기가 없어 실제 휴대전화에서의 터치와 FPS는 미검증입니다.
Previews는 Windows 실행 화면이며, 검사 기록은 Verification에 있습니다.

원작 자료를 활용한 별도 재구현이며 원작 전체 앱의 완전한 복제본은 아닙니다.
원작 자산의 권리는 원권리자에게 있습니다.
''',encoding='utf-8-sig')
manifest={str(f.relative_to(d)).replace('\\','/'):hashlib.sha256(f.read_bytes()).hexdigest() for f in d.rglob('*') if f.is_file()}
(d/'SHA256.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False),encoding='utf-8')
archive=d/'HoleIt_4차_APK_Source.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
    for f in d.rglob('*'):
        if f.is_file() and f!=archive:z.write(f,str(f.relative_to(d)))
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None
    assert hashlib.sha256(z.read('Gaeun_Hole_4.apk')).hexdigest()==manifest['Gaeun_Hole_4.apk']
    for n in ['Source/Assets/Editor/ReferenceInputBuildGuard.cs','Source/Assets/Tests/Reference/ReferenceTouchTests.cs','Source/Assets/Resources/Fonts/OFL.txt']:assert n in z.namelist()
print(json.dumps({'folder':str(d),'apkBytes':(d/'Gaeun_Hole_4.apk').stat().st_size,'zipBytes':archive.stat().st_size,'files':len(manifest)},ensure_ascii=False))
