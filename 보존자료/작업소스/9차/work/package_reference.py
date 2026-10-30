from pathlib import Path
import shutil,zipfile,hashlib,json,re,xml.etree.ElementTree as ET
work=Path(__file__).parent.resolve();project=work/'AndroidFinal'
dest=Path('C:/Users/PUVKID/Desktop/Hole It/3차')
assert not dest.exists() or not list(dest.iterdir()),'Preserve an existing release'
apk=project/'Builds/Android/HoleIt.apk'
build=(project/'TestResults/AndroidBuild.txt').read_text()
assert 'Succeeded' in build and 'Errors: 0' in build
sign=(work/'reference-apk-verification.txt').read_text(encoding='utf-8-sig')
assert 'Verifies' in sign and "versionName='4.0.0'" in sign and "versionCode='5'" in sign
tests=ET.parse(work/'reference-release-tests.xml').getroot()
assert tests.attrib['result']=='Passed' and tests.attrib['failed']=='0'
border=ET.parse(work/'reference-border-tests.xml').getroot()
assert border.attrib['result']=='Passed'
assert ET.parse(work/'reference-start-tests.xml').getroot().attrib['result']=='Passed'
campaign=(project/'TestResults/ReferenceCampaign.txt').read_text()
assert len(re.findall(r'Level \d+: Won;',campaign))==30
dest.mkdir(parents=True,exist_ok=True)
shutil.copy2(apk,dest/'Gaeun_Hole_3.apk')
source=dest/'Source';source.mkdir()
for name in ['Assets','Packages','ProjectSettings']:
    shutil.copytree(project/name,source/name,ignore=lambda folder,names:[n for n in names if n.startswith('InitTestScene') or n=='.DS_Store'])
script=(work/'Build-Android.ps1').read_text(encoding='utf-8-sig').replace('AndroidRelease.Build','ReferenceBuilder.BuildAndroid')
(source/'Build-Android.ps1').write_text(script,encoding='utf-8-sig')
(source/'.gitignore').write_text('Library/\nTemp/\nObj/\nLogs/\nUserSettings/\nBuilds/\nTestResults/\n*.csproj\n*.sln\n',encoding='utf-8')
(source/'AGENTS.md').write_text('''# Project instructions
- All player-facing UI is English. Bundle Nanum Gothic Bold 700 and its OFL license.
- Save later editions under the next numbered Desktop/Hole It folder; preserve earlier editions.
- Main.unity boots ReferenceGame. ReferenceCatalog contains the supplied content converted into Unity assets.
- Build with Build-Android.ps1 / ReferenceBuilder.BuildAndroid. Do not run legacy builders, which replace Main with the older game.
- Edition2.unity and legacy scripts are archived references. Their test assemblies require POCKETPIT_LEGACY_TESTS and are disabled by default.
- The active tests are Assets/Tests/Reference. ReferenceBuilder.Prepare rebuilds the content catalog from Models.json/Levels.json and the supplied mesh/texture files; ReferenceStyleBuilder.Prepare rebuilds the style assets.
''',encoding='utf-8')
(source/'README.txt').write_text('''Gaeun's Hole · edition 3 · Unity 6000.3.6f1

Open this Source folder in Unity with Android Build Support, SDK/NDK/OpenJDK.
Main scene: Assets/Scenes/Main.unity. Runtime: Assets/Scripts/ReferenceGame.cs.
Build: powershell -ExecutionPolicy Bypass -File .\Build-Android.ps1
Output: Builds/Android/HoleIt.apk. Uses a personal test signature.

Assets/ReferenceContent contains 259 item models and the first 30 locally bundled
stage layouts converted from the user-supplied Hole it! 1.31.17 XAPK. Textures,
goal icons, environment geometry, hole geometry and selected UI sprites are from
that supplied app. The runtime C# code is this project's separate implementation.
The original app's C# source was not recovered. Raw native code, ad SDKs, account
configuration and the original APK are not included in this source project.

Growth uses per-placement points, subtracts the current level's cost, carries
remaining points forward, and caps physical growth at ten increments. The visual
scale increment is .25 and movement radius increment is .223, from a .7 baseline.
The original IL2CPP growth methods were inspected to distinguish sequential costs
from cumulative thresholds. See Verification/Reference-Comparison.txt.

The provided assets retain their original ownership. Nanum Gothic Bold and its
OFL license are bundled under Assets/Resources/Fonts.
''',encoding='utf-8')
shutil.copytree(work/'reference-previews-final',dest/'Previews')
(dest/'Previews/05-growth.png').rename(dest/'Previews/05-level-complete.png')
qa=dest/'Verification';qa.mkdir()
for filename in ['ReferenceCampaign.txt','ReferenceStructures.txt','AndroidBuild.txt']:
    shutil.copy2(project/'TestResults'/filename,qa/filename)
shutil.copy2(work/'reference-release-tests.xml',qa/'ReferenceTests.xml')
shutil.copy2(work/'reference-border-tests.xml',qa/'BorderRecoveryTest.xml')
shutil.copy2(work/'reference-start-tests.xml',qa/'FinalStartPositionTest.xml')
shutil.copy2(work/'reference-apk-verification.txt',qa/'APK-Verification.txt')
shutil.copy2(work/'reference-comparison.txt',qa/'Reference-Comparison.txt')
(dest/'읽어주세요.txt').write_text('''가은이 홀 게임 · 3차 · 4.0.0 (5)

설치 파일: Gaeun_Hole_3.apk
전체 묶음: HoleIt_3차_APK_Source.zip
소스 프로젝트: Source
실제 실행 화면: Previews (Windows 렌더링 캡처)
검증 기록: Verification

APK를 Android 휴대전화로 옮겨 설치하세요. Android 8.0 이상, ARM64/ARMv7용이며
개인 설치용 테스트 서명을 사용합니다. 2차와 패키지 식별자/서명을 유지했습니다.
이번 버전의 스테이지 진행 기록은 새로 시작합니다.

화면을 누른 지점이 조이스틱 중심입니다. 그 상태로 원하는 방향으로 당기면 이동하고,
같은 방향으로 누르고 있으면 계속 이동합니다. 손을 떼면 멈춥니다.
손가락을 멀리 뻗어도 최고 속도를 넘지 않으며, 손가락 위치로 홀이 이동하지 않습니다.

첨부된 원작 1.31.17의 첫 30개 스테이지 순서·목표·배치와 259종 모형을 사용했습니다.
제한 시간은 해당 데이터의 80~300초입니다. 첫 스테이지는 5분입니다.
성장은 단순 수집 개수가 아니라 각 배치에 지정된 점수로 계산합니다.
단계별 필요 점수는 10, 20, 100, 150, 200, 300, 400, 500, 600, 700이며
레벨업할 때 차감하고 초과 점수는 이월합니다. 실제 크기의 증가는 10단계에서 멈춥니다.
초기 스케일 .7, 외형 증가 .25, 이동 판정 반경 증가 .223을 반영했습니다.
원작 바닥/벽의 배치, 홀 모형과 텍스처, 목표 카드와 버튼 이미지도 반영했습니다.

폭탄은 원작 데이터상 21번째 맵부터 등장합니다. 먹으면 실패하며 TRY AGAIN으로
같은 맵을 다시 시작합니다. SIZE / SPEED / FIND / FREEZE는 각각 3/5/9/12 레벨부터
한 판에 한 번 쓸 수 있습니다. 화면 언어는 영어, 글꼴은 Nanum Gothic Bold입니다.

이 묶음은 원작 자료를 활용한 별도 재구현입니다. 원작 전체 앱의 완전한 복제본은 아닙니다.
전체 원작 스테이지/이벤트/광고/결제/온라인 시스템은 포함하지 않았습니다.
낙하 물리, 카메라 추적·확대, 조명, 일부 화면 배치와 메뉴는 자체 구현입니다.
바닥 테마 선택과 폭탄 좌표는 별도로 정했습니다. 자세한 차이는 검증 문서에 적었습니다.
자동 검사는 통과했지만 연결된 Android 기기가 없어 실제 휴대전화 터치감과 FPS는
확인하지 못했습니다. 30개 맵 완료 검사는 수집 위치를 이동시키는 검사로,
사람이 제한 시간 안에 완료하기 쉬운지 검증한 결과는 아닙니다.
''',encoding='utf-8-sig')
manifest={str(p.relative_to(dest)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest() for p in dest.rglob('*') if p.is_file()}
(dest/'SHA256.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False),encoding='utf-8')
archive=dest/'HoleIt_3차_APK_Source.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
    for p in dest.rglob('*'):
        if p.is_file() and p!=archive:z.write(p,str(p.relative_to(dest)))
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None
    assert hashlib.sha256(z.read('Gaeun_Hole_3.apk')).hexdigest()==manifest['Gaeun_Hole_3.apk']
    for n in ['Source/Assets/Scenes/Main.unity','Source/Assets/Resources/Fonts/NanumGothic-Bold.ttf','Source/Assets/Resources/Fonts/OFL.txt','Source/Assets/Scripts/ReferenceGame.cs']:assert n in z.namelist()
print(json.dumps({'folder':str(dest),'apkMB':round(apk.stat().st_size/1048576,1),'zipMB':round(archive.stat().st_size/1048576,1),'testsPassed':int(tests.attrib['passed'])+int(border.attrib['passed']),'files':len(manifest)},ensure_ascii=False))
