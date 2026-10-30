from pathlib import Path
import hashlib
import json
import shutil
import zipfile
import xml.etree.ElementTree as ET

release = Path(__file__).resolve().parent.parent
source = release / 'Source'
verification = release / 'Verification'
verification.mkdir(exist_ok=True)
apk_source = source / 'Builds/Android/HoleIt.apk'
apk = release / 'Gaeun_Hole_6.apk'
assert apk_source.stat().st_size >= 500_000_000, 'APK under 500 MB'
tests = ET.parse(release / 'Audit/Tests.xml').getroot()
assert tests.attrib['failed'] == '0' and int(tests.attrib['passed']) >= 40, tests.attrib
build_result = (source / 'TestResults/AndroidBuild.txt').read_text(encoding='utf-8-sig')
assert 'Succeeded' in build_result and 'Errors: 0' in build_result, build_result
for required in ['ApkSignature.txt', 'ApkManifest.txt', 'ApkAlignment.txt']:
    assert (release / 'Audit' / required).stat().st_size > 0, required
shutil.copy2(apk_source, apk)
for file in (source / 'TestResults').glob('*'):
    if file.is_file(): shutil.copy2(file, verification / file.name)
for name in ['Tests.xml', 'Tests.log', 'AndroidBuild.log', 'WindowsBuild.log', 'Preview.log', 'Preview-tall.log', 'ApkSignature.txt', 'ApkManifest.txt', 'ApkAlignment.txt', 'LevelFidelityAudit.txt', 'LevelFidelityAudit.json', 'ContentExpansion.json', 'VerifiedDuplicateCleanup.json', 'BuildCacheCleanup.json']:
    path = release / 'Audit' / name
    if path.exists(): shutil.copy2(path, verification / name)

def sha(path):
    digest = hashlib.sha256()
    with path.open('rb') as f:
        for chunk in iter(lambda: f.read(4 * 1024 * 1024), b''): digest.update(chunk)
    return digest.hexdigest()

with zipfile.ZipFile(apk) as z:
    assert z.testzip() is None
    apk_entries = z.infolist()
    data = {
        'apkBytes': apk.stat().st_size,
        'apkSHA256': sha(apk),
        'apkMinimumBytes': 500_000_000,
        'entryUncompressedBytes': sum(f.file_size for f in apk_entries),
        'storedEntryBytes': sum(f.file_size for f in apk_entries if f.compress_type == zipfile.ZIP_STORED),
        'passedTests': int(tests.attrib['passed']),
        'failedTests': int(tests.attrib['failed']),
        'campaignEntries': 465, 'extraChallenges': 111, 'collectibles': 1010,
        'authoredPoisonBombs': 153, 'authoredPillars': 24,
        'dailyCoins': 1000, 'attendanceCycleDays': 31,
        'sizeMethod': 'Usable game assets and native libraries stored without ZIP compression; no filler.'
    }
(verification / 'ReleaseFacts.json').write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
(release / '읽어주세요.txt').write_text(f'''가은이를 위한 Hole It - 6차

설치 파일: Gaeun_Hole_6.apk
소스 코드: Source 폴더 (Unity 6000.3.6f1)
APK 실제 용량: {apk.stat().st_size:,} bytes ({apk.stat().st_size / 1_000_000:.1f} MB)
검증: 자동 테스트 {tests.attrib['passed']}개 통과, 실패 {tests.attrib['failed']}개.

원본에서 확인한 Linik Sans Black 글꼴/외곽선, 모형 1,010개,
기본 맵 465개, 별도 연습 맵 111개, 고정 폭탄 153개와 벽 기둥을 반영했습니다.
꼬똥 드 툴레아 로딩·홈 일러스트는 유지했습니다.
Daily Rewards: 하루 한 번 1,000 게임 코인. 누적 31일 출석 후 다음 수령일부터 새 출석판.
빠진 날짜 때문에 출석 진도가 초기화되지 않습니다.
Challenges는 생명/부스터를 소모하지 않는 연습이며 기본 진행도와 코인 보상에 영향을 주지 않습니다.
Collection에서 모형을 넘기고 손가락으로 돌려 볼 수 있습니다.

APK는 실제 게임 자산과 네이티브 라이브러리를 압축 없이 담아 500 MB 이상으로 만들었습니다.
용량 채우기용 더미 파일은 없습니다. ZIP은 다시 압축되므로 APK보다 작을 수 있습니다.
ZIP에는 Library/Temp/중복 빌드 결과와 원본 XAPK, 광고 SDK를 넣지 않았습니다.

검증 범위와 차이:
- 모든 기본·추가 맵의 데이터와 시작 지점, 고정 폭탄 위치, 출석 저장/중복방지, 실제 입력 코드 경로를 자동 점검했습니다.
- Previews는 Windows 실행 화면입니다. 실제 Android 기기에서의 성능 측정은 수행하지 않았습니다.
- 원본의 온라인 이벤트, 서버 설정과 광고/결제 기능은 복제하지 않았습니다.
- 물리 엔진 동작과 모든 세부 디자인이 원본과 100% 같다는 보증은 아닙니다.
  정확하게 반영한 범위와 남은 차이는 Verification/LevelFidelityAudit.txt에 기록했습니다.
''', encoding='utf-8-sig')
archive = release / 'HoleIt_6차_APK_Source.zip'
included = []
for directory in ['Source/Assets', 'Source/Packages', 'Source/ProjectSettings', 'Source/Tools', 'Previews', 'Verification']:
    included.extend(p for p in (release / directory).rglob('*') if p.is_file())
for name in ['Source/AGENTS.md', 'Source/.gitignore', 'Source/Build-Android.ps1', 'Source/README.txt', '읽어주세요.txt', 'Gaeun_Hole_6.apk']:
    included.append(release / name)
with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED, compresslevel=6, allowZip64=True) as z:
    for path in sorted(set(included)):
        assert path.stat().st_size > 0 or path.suffix not in ('.cs', '.json', '.asset')
        z.write(path, path.relative_to(release).as_posix())
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None, 'Release archive CRC error'
    assert hashlib.sha256(z.read('Gaeun_Hole_6.apk')).hexdigest() == data['apkSHA256']
    assert 'Source/Assets/Scripts/ReferenceAttendance.cs' in z.namelist()
    assert 'Source/Assets/Resources/ReferenceTypography/LinikSansBlack.asset' in z.namelist()
data['zipBytes'] = archive.stat().st_size
data['zipSHA256'] = sha(archive)
data['zipFiles'] = len(set(included))
(release / 'SHA256.json').write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(data, ensure_ascii=True))
