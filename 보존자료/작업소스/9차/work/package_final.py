from pathlib import Path
import shutil, zipfile, hashlib, xml.etree.ElementTree as ET, json, re
work=Path(__file__).parent.resolve()
project=work/'AndroidFinal'
dest=Path('C:/Users/PUVKID/Desktop/Hole It/2차')
assert dest.is_dir() and not list(dest.iterdir()), 'Final folder must be empty; do not overwrite an existing edition.'
apk=project/'Builds/Android/HoleIt.apk'
build=(project/'TestResults/AndroidBuild.txt').read_text()
assert 'Succeeded' in build and 'Errors: 0' in build,build
results={}
for name in ['gameplay-play','english-edit','english-play']:
    root=ET.parse(work/(name+'.xml')).getroot()
    assert root.attrib['result']=='Passed' and root.attrib.get('failed')=='0',name
    results[name]=int(root.attrib['passed'])
report=(project/'TestResults/PhysicalCampaign.txt').read_text()
assert len(re.findall(r'Level \d+: LevelClear;',report))==30
sign=(work/'apk-verification.txt').read_text(encoding='utf-8-sig')
assert 'Verifies' in sign and "versionName='3.0.0'" in sign
shutil.copy2(apk,dest/'PocketPit_Gaeun.apk')
source=dest/'Source';source.mkdir()
def ignore(folder,names):
    return [n for n in names if n.startswith('InitTestScene') or n in ['.DS_Store']]
for name in ['Assets','Packages','ProjectSettings']:
    shutil.copytree(project/name,source/name,ignore=ignore)
shutil.copy2(work/'Build-Android.ps1',source/'Build-Android.ps1')
(source/'.gitignore').write_text('Library/\nTemp/\nObj/\nLogs/\nUserSettings/\nBuilds/\nTestResults/\n*.csproj\n*.sln\n',encoding='utf-8')
(source/'AGENTS.md').write_text('''# Project instructions
- All player-facing UI is English.
- Bundle Nanum Gothic Bold (weight 700) and its OFL license.
- Save later deliverables to the next numbered folder (3차, 4차, ...) under C:/Users/PUVKID/Desktop/Hole It. Preserve earlier editions.
- The authored scene and assets are ready to build. Do not run legacy BuildAll / Prepare commands; they recreate older layouts.
- To regenerate the current gameplay data and English layout, use HoleItBuilder.BuildGameplayEdition. To build the existing authored release, run Build-Android.ps1.
''',encoding='utf-8')
shutil.copytree(work/'english-previews',dest/'Previews')
qa=dest/'Verification';qa.mkdir()
for name in ['gameplay-play','english-edit','english-play']:
    shutil.copy2(work/(name+'.xml'),qa/(name+'.xml'))
shutil.copy2(project/'TestResults/PhysicalCampaign.txt',qa/'PhysicalCampaign.txt')
shutil.copy2(project/'TestResults/AndroidBuild.txt',qa/'AndroidBuild.txt')
shutil.copy2(work/'apk-verification.txt',qa/'APK-Verification.txt')
(dest/'읽어주세요.txt').write_text('''포켓 핏 / Pocket Pit · 가은이 2차 · 3.0.0 (4)

설치: PocketPit_Gaeun.apk를 안드로이드 휴대전화로 옮겨 실행합니다.
Android 8.0 이상, ARM64 및 ARMv7을 지원합니다. 개인 설치용 테스트 서명 APK입니다.
기존 같은 패키지 앱에 업데이트 설치할 수 있도록 패키지 식별자를 유지했습니다.

게임 화면은 전부 영어입니다. 기본 모드는 CHALLENGE입니다.
PLAY: 시작 / TRY AGAIN: 재시도 / HOME: 메뉴 / SETTINGS: 설정
CHALLENGE: 시간 안에 목표 수집 / RELAXED: 제한 시간 없음
기존 저장에서 RELAXED를 선택했다면 메뉴의 모드 버튼을 눌러 CHALLENGE로 바꾸세요.
드래그: 손가락을 댄 위치로 순간 이동하지 않고, 움직인 거리와 방향을 따라갑니다.
처음 4개를 모으면 홀이 커집니다. 아래에 다음 성장까지 필요한 개수가 표시됩니다.
폭탄을 삼키면 바로 실패합니다. TRY AGAIN으로 같은 스테이지를 다시 시작합니다.

Source: Unity 6000.3.6f1 전체 프로젝트 소스(Assets, Packages, ProjectSettings)
Source/Build-Android.ps1: Android Build Support가 설치된 Unity로 APK 재빌드
Previews: 최종 영어 버전의 실제 Windows 렌더링 캡처
Verification: 자동 검사 결과, Android 빌드 기록, APK 서명 확인

원작의 화면 구성을 참고해 만든 별도 게임입니다. 원작의 비공개 소스나 모델을 사용하지 않았으며,
원작과 동일한 그래픽·물리·조작감을 보장하는 복제본은 아닙니다.
실제 안드로이드 기기가 연결되지 않아 휴대전화 터치감과 기기별 성능은 확인하지 못했습니다.
''',encoding='utf-8-sig')
(dest/'2차_변경사항.txt').write_text('''2차 변경 사항

1. 영어 UI: 메뉴, 목표, 설정, 성장 표시, 완료/실패 화면 전체 영어. 나눔고딕 Bold와 라이선스 내장.
2. 조작: 가상 조이스틱식 드래그를 손가락 이동량 기반으로 변경. 바닥 투영 좌표로 방향/거리 계산.
   짧은 지수 보간, 누락 없는 이동 목표, 터치 시작 시 점프 방지, 손을 떼면 멈춤.
   별도 조이스틱 모드 속도 3.8, 성장에 따른 속도 급증 제거.
3. 성장: 누적 수집 4/12/26/46/74개에서 성장. 반지름 0.9→1.2→1.6→2.1→2.7→3.4.
4. 낙하: 물건이 움직이는 홀 중심을 따라오는 연출 제거. 진입 위치에서 아래로 떨어짐.
   바닥에 닿은 물건부터 먹으며 위에 쌓인 물건은 중력으로 내려옴.
5. 배치: 30개 맵을 군집, 계단, 원형, 테라스, 성곽, 분산 기둥 6가지 방식으로 재구성.
6. 시각: 덜 선명한 파스텔 바닥, 약한 격자 대비, 부드러운 조명과 그림자.
   둥근 화장품 병/크림/비누, 사선 립스틱 끝, 재질별 광택. 단순한 구멍 테두리 유지.
7. 폭탄: 맵별 2~4개. 삼키면 즉시 실패하고 점수/성장 보상 없음. 마지막 목표와 겹쳐도 폭탄 실패 우선.
8. 시간: 맵별 25~45초. 자동 플레이 경로의 소요 시간을 참고해 단축.
   이는 인간 플레이 난이도 검증을 대신하지 않으며 실제 터치로 추가 조정할 수 있음.
9. 실패 화면: 기록 표를 숨기고 남은 목표와 TRY AGAIN 버튼에 집중하도록 정리.

참고 화면: https://play.google.com/store/apps/details?id=com.rocket.holeit&hl=en
공개 스크린샷의 단순한 홀, 부드러운 원색 모형, 반복/원형 배치, 상단 목표 카드를 참고함.
화장품 모델과 스테이지 구성은 이 프로젝트에서 별도로 제작함.
''',encoding='utf-8-sig')
(qa/'검사_설명.txt').write_text(f'''검사 결과

Android: {build.strip()}
최종 영어 버전 EditMode: {results['english-edit']}개 통과
최종 영어 버전 PlayMode: {results['english-play']}개 통과
영어 전환 직전 전체 PlayMode: {results['gameplay-play']}개 통과 (30개 맵 실제 이동/트리거/낙하 포함)

전체 30개 맵 검사는 제한 없는 모드, 자동 조이스틱 입력으로 수행했습니다.
그 뒤 영어 문구, 드래그 보간, 제한 시간을 변경했으므로 이전 보고서의 budget은 최종 시간과 다릅니다.
최종 검사에서는 드래그 이동량/정지, 물건의 낙하 위치, 첫 4개 성장, 시간초과/재시도,
폭탄 실패/보상 차단/마지막 목표 동시 진입, 메뉴/폰트/영어 화면과 기본 캠페인 동작을 다시 확인했습니다.
최종 시간: 25~45초. 완벽한 자동 경로를 기준으로 한 1차 조정이며 인간 플레이 난이도는 미검증입니다.
캡처는 Android 단말 화면이 아니라 같은 씬을 실행한 Windows 빌드의 540×960 렌더링입니다.
실제 Android 기기 실행, 터치 지연, 프레임률/발열 측정은 수행하지 못했습니다.
''',encoding='utf-8-sig')
manifest=[]
for f in sorted(dest.rglob('*')):
    if f.is_file(): manifest.append(hashlib.sha256(f.read_bytes()).hexdigest()+'  '+f.relative_to(dest).as_posix())
(dest/'SHA256SUMS.txt').write_text('\n'.join(manifest)+'\n',encoding='utf-8')
archive=dest/'HoleIt_2차_APK_Source.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
    for f in sorted(dest.rglob('*')):
        if f.is_file() and f!=archive:z.write(f,f.relative_to(dest).as_posix())
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None
    assert hashlib.sha256(z.read('PocketPit_Gaeun.apk')).hexdigest()==hashlib.sha256(apk.read_bytes()).hexdigest()
    for needed in ['Source/Assets/Scenes/Main.unity','Source/Assets/Resources/Fonts/NanumGothic-Bold.ttf','Source/Assets/Resources/Fonts/OFL.txt','Source/Build-Android.ps1']:
        assert needed in z.namelist(),needed
    assert not any('/Library/' in n or 'InitTestScene' in n for n in z.namelist())
print(json.dumps({'folder':str(dest),'apkBytes':apk.stat().st_size,'zipBytes':archive.stat().st_size,'apkSHA256':hashlib.sha256(apk.read_bytes()).hexdigest(),'testResults':results},ensure_ascii=False,indent=2))
