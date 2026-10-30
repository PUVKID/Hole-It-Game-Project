from pathlib import Path
import hashlib
import json
import re
import shutil
import zipfile
import xml.etree.ElementTree as ET
import datetime
from source_build_manifest import verify_completed_manifest

release = Path(__file__).resolve().parent.parent
source = release / 'Source'
verification = release / 'Verification'
verification.mkdir(exist_ok=True)

def sha(path):
    digest = hashlib.sha256()
    with path.open('rb') as f:
        for chunk in iter(lambda: f.read(4 * 1024 * 1024), b''): digest.update(chunk)
    return digest.hexdigest()

def android_build_route(unity_report, gradle_report, gradle_log, apk_sha256):
    gradle_report = '\n'.join(gradle_report.splitlines())
    unity_lines = [line.strip() for line in unity_report.splitlines()]
    unity_statuses = [line for line in unity_lines
                      if line in ('Succeeded', 'Failed', 'Cancelled', 'Unknown')]
    unity_errors = [line for line in unity_lines if line.startswith('Errors:')]
    if unity_statuses == ['Succeeded'] and unity_errors == ['Errors: 0']:
        return 'unity'

    # Recovery is a distinct build route; it must not rewrite Unity's failed report.
    statuses = re.findall(r'^Gradle assembleRelease:[ \t]*(\S+)[ \t]*$',
                          gradle_report, re.MULTILINE)
    exit_codes = re.findall(r'^Gradle exit code:[ \t]*(-?\d+)[ \t]*$',
                            gradle_report, re.MULTILINE)
    apk_hashes = re.findall(r'^APK SHA256:[ \t]*([0-9a-fA-F]{64})[ \t]*$',
                            gradle_report, re.MULTILINE)
    log_lines = [line.strip() for line in gradle_log.splitlines()]
    successful_log = any(re.fullmatch(r'BUILD SUCCESSFUL(?: in .+)?', line)
                         for line in log_lines)
    failed_log = any(re.fullmatch(r'BUILD FAILED(?: in .+)?', line)
                     for line in log_lines)
    assert (statuses == ['Succeeded'] and exit_codes == ['0']
            and len(apk_hashes) == 1
            and apk_hashes[0].lower() == apk_sha256.lower()
            and successful_log and not failed_log), (
        'Android build gate failed: require Unity Succeeded/Errors: 0, or a separate '
        'Gradle Succeeded/exit 0 record, BUILD SUCCESSFUL log, and matching APK SHA256.'
    )
    return 'gradle-recovery'

def validate_final_tests(xml_bytes, summary, limitations):
    """Preserve NUnit failures; permit only the reviewed bounded campaign-exploration case."""
    tests = ET.fromstring(xml_bytes)
    assert tests.tag == 'test-run', 'Expected a complete NUnit test-run document'
    assert tests.get('result') in ('Passed', 'Failed', 'Failed(Child)'), 'Final test result is incomplete/cancelled'
    start = datetime.datetime.fromisoformat(tests.attrib['start-time'].replace('Z', '+00:00'))
    end = datetime.datetime.fromisoformat(tests.attrib['end-time'].replace('Z', '+00:00'))
    assert end >= start, 'Final test completion precedes its start'
    cases = list(tests.iter('test-case'))
    expected_fixtures = {
        'ReferenceAttendanceTests', 'ReferenceAttendanceTouchTests', 'ReferenceCameraSettingsTests',
        'ReferenceExtraTouchTests', 'ReferenceFifthTests', 'ReferenceIndicatorTests',
        'ReferenceMotionSettingsTests', 'ReferenceProfileTests', 'ReferenceSeventhPhysicsTests',
        'ReferenceSixthContentTests', 'ReferenceTests', 'ReferenceTimingTests',
        'ReferenceTouchTests', 'ReferenceTypographyTests'
    }
    assert {case.get('classname') for case in cases} == expected_fixtures, 'Not the complete fourteen-fixture final regression suite'
    assert len(cases) == 81 and len(cases) == int(tests.attrib['total']) == int(tests.attrib['testcasecount']), 'Final edition-seven suite must retain all eighty-one cases'
    names = [case.get('fullname') for case in cases]
    assert all(names) and len(names) == len(set(names)), 'Missing or duplicated final test identity'
    assert all(case.get('result') in ('Passed', 'Failed') for case in cases), 'Skipped/inconclusive/incomplete cases cannot be waived'
    assert all(case.get('end-time') for case in cases), 'A final test case has no completion timestamp'
    assert not any(node.get('label') in ('Error', 'Cancelled', 'Invalid', 'NotRunnable') for node in tests.iter()), 'Runtime/setup/cancellation errors are not an allowed validation limitation'
    failures = [case for case in cases if case.get('result') == 'Failed']
    passed = len(cases) - len(failures)
    assert int(tests.attrib['passed']) == passed and int(tests.attrib['failed']) == len(failures), 'NUnit aggregate results do not match actual cases'
    assert int(tests.get('skipped', '0')) == 0 and int(tests.get('inconclusive', '0')) == 0, 'Final suite contains unexecuted checks'
    assert (tests.get('result') == 'Passed') == (not failures), 'Final suite result conflicts with actual failures'
    digest = hashlib.sha256(xml_bytes).hexdigest()
    expected_failures = [{'name': case.get('fullname'), 'result': case.get('result'),
                          'message': case.findtext('failure/message')} for case in failures]
    assert (summary.get('source') == 'Tests-final.xml' and summary.get('sha256') == digest
            and summary.get('result') == tests.get('result') and summary.get('total') == len(cases)
            and summary.get('passed') == passed and summary.get('failures') == expected_failures
            and summary.get('start') == tests.get('start-time') and summary.get('end') == tests.get('end-time')
            and summary.get('sourceSnapshotUnchanged') is True), 'Final completion summary does not prove this exact XML/source run'
    campaign = summary.get('campaign', {})
    stages = campaign.get('stages', [])
    assert campaign.get('stagesAttempted') == 30 and sorted(stage.get('stage') for stage in stages) == list(range(1, 31)), 'Campaign validation did not finish attempting all thirty stages'
    unresolved = sorted(stage['stage'] for stage in stages if stage.get('state') != 'Won')
    assert campaign.get('unresolvedStages') == unresolved, 'Campaign unresolved-stage summary mismatch'
    result = {'status': 'passed', 'testsXmlSHA256': digest, 'total': len(cases),
              'passed': passed, 'failed': len(failures), 'allTestsPassed': not failures,
              'knownValidationLimitations': None}
    if not failures:
        assert not unresolved, 'Campaign stages remain unresolved despite a passing case'
        assert limitations is None, 'A stale KnownValidationLimitations.json must not be presented as current'
        return tests, result
    allowed_case = 'ReferenceTests.AllThirtyStagesCompleteThroughMouthAndStackSimulation'
    assert len(failures) == 1 and failures[0].get('fullname') == allowed_case, 'An unapproved/core test failed'
    message = failures[0].findtext('failure/message') or ''
    assert 'Expected: <empty>' in message, 'Only the final unresolved-stage assertion is allowlisted'
    assert sorted({int(stage) for stage in re.findall(r'\bStage (\d+)\b', message)}) == unresolved, 'XML failure stages do not match the final campaign record'
    assert unresolved and set(unresolved) <= {13, 14, 26}, 'A different unresolved stage requires new review'
    assert isinstance(limitations, dict), 'KnownValidationLimitations.json is required; failure is never silently accepted'
    assert (limitations.get('schemaVersion') == 1 and limitations.get('testsXmlSHA256') == digest
            and limitations.get('failedCaseCount') == 1 and limitations.get('unresolvedStages') == unresolved
            and isinstance(limitations.get('reason'), str) and limitations['reason'].strip()), 'Known limitation evidence does not match the completed final run'
    failed_cases = limitations.get('failedCases')
    assert isinstance(failed_cases, list) and len(failed_cases) == 1, 'Known limitation must name exactly one failed case'
    approved = failed_cases[0]
    assert (approved.get('fullname') == allowed_case and approved.get('count') == 1
            and approved.get('unresolvedStages') == unresolved
            and isinstance(approved.get('reason'), str) and approved['reason'].strip()), 'Known limitation case identity/count/reason/stages mismatch'
    result.update(status='known-validation-limitation', knownValidationLimitations=limitations)
    return tests, result


def validation_notice(validation):
    if validation['allTestsPassed']:
        return '최종 전체 자동 검사에서 실패한 항목은 없습니다. Android 실행 검증 여부는 별도 기록을 확인하세요.'
    stages = ', '.join(str(stage) for stage in validation['knownValidationLimitations']['unresolvedStages'])
    return ('알려진 검증 미해결: 자동 탐색 캠페인 검사 1건이 실패했고, ' + stages + '단계를 완료하지 못했습니다.\n'
            '이 실패는 원본 XML과 집계에 그대로 남아 있습니다. 나머지 핵심 검사 ' + str(validation['passed']) + '건은 통과했습니다.\n'
            '전체 맵 완료나 무오류를 확인한 상태는 아닙니다. 자동 검사만으로 해당 단계의 실제 플레이 해결 가능성도 확정하지 않았습니다.\n'
            '실패 검사: ReferenceTests.AllThirtyStagesCompleteThroughMouthAndStackSimulation\n'
            '실패 범위와 사유: Verification/KnownValidationLimitations.json 및 FinalVerificationSummary.json')


def android_qa_scope(record_names):
    # Availability inventory is evidence that QA could not run, not an execution result.
    runtime_records = [name for name in record_names if not name.startswith('AndroidQAUnavailable')]
    unavailable_records = [name for name in record_names if name.startswith('AndroidQAUnavailable')]
    if runtime_records:
        records = ', '.join('Verification/' + name for name in runtime_records)
        text = ('추가 Android QA 기록: ' + records + '.\n'
                '실기기/에뮬레이터 구분, 실행 항목과 통과/실패 결과는 해당 기록의 본문을 참조하세요.\n'
                'Windows 미리보기만으로 Android 휴대폰의 성능이나 조작감을 검증했다고 볼 수 없습니다.')
    else:
        text = ('Android 실행 QA는 실시하지 못했습니다.\n'
                '실제 Android 휴대폰에서 성능과 조작감은 아직 측정하지 못했습니다.')
    if unavailable_records:
        text += '\nAndroid QA 환경 확인 기록: ' + ', '.join('Verification/' + name for name in unavailable_records) + '.'
    return text

android_qa_records = sorted(path.name for path in (release / 'Audit').glob('AndroidQA*')
                            if path.is_file() and path.stat().st_size > 0
                            and path.suffix.lower() in ('.json', '.txt', '.log', '.md', '.xml'))
android_qa_text = android_qa_scope(android_qa_records)
android_runtime_qa_records = [name for name in android_qa_records if not name.startswith('AndroidQAUnavailable')]
apk_source = source / 'Builds/Android/HoleIt.apk'
apk = release / 'HoleIt_7.apk'
assert apk_source.stat().st_size >= 500_000_000, 'APK under 500 MB'
final_test_bytes = (release / 'Audit/Tests-final.xml').read_bytes()
published_test_bytes = (release / 'Audit/Tests.xml').read_bytes()
assert published_test_bytes == final_test_bytes, 'Tests.xml must preserve the final complete XML byte-for-byte, not merge/drop failures'
final_test_summary = json.loads((release / 'Audit/FinalVerificationSummary.json').read_text(encoding='utf-8-sig'))
limitation_path = release / 'Audit/KnownValidationLimitations.json'
known_limitations = json.loads(limitation_path.read_text(encoding='utf-8-sig')) if limitation_path.exists() else None
tests, validation = validate_final_tests(final_test_bytes, final_test_summary, known_limitations)
validation_summary = validation_notice(validation)
build_result = (source / 'TestResults/AndroidBuild.txt').read_text(encoding='utf-8-sig')
gradle_result_path = source / 'TestResults/AndroidGradleBuild.txt'
gradle_log_path = release / 'Audit/AndroidGradleBuild.log'
gradle_result = gradle_result_path.read_text(encoding='utf-8-sig') if gradle_result_path.exists() else ''
gradle_log = gradle_log_path.read_text(encoding='utf-8-sig') if gradle_log_path.exists() else ''
apk_sha256 = sha(apk_source)
source_build_manifest = verify_completed_manifest(release, apk_source)
assert source_build_manifest['completion']['apkSHA256'] == apk_sha256
source_build_summary = (
    '새 빌드 직전에 기록한 C# 파일 해시와 최종 Source의 C# 파일 해시가 일치합니다.\n'
    '성공한 빌드 기록 및 완성 APK의 SHA256은 Verification/SourceBuildManifest.json에 연결했습니다.\n'
    'VerificationAtNativeCompile.cs.txt와 VerificationSnapshotDifference.*는 수정 전 폐기한 빌드 입력의 이력이며, 최종 APK와 소스가 다르다는 뜻이 아닙니다.'
)
build_route = android_build_route(build_result, gradle_result, gradle_log, apk_sha256)
build_summary = (
    'APK 빌드: Unity Android 빌드가 Succeeded, Errors: 0으로 완료되었습니다.'
    if build_route == 'unity' else
    'APK 빌드: Unity 전체 빌드는 실패로 기록되어 있습니다. 해당 빌드가 생성한 Gradle 프로젝트에서\n'
    '동일 assembleRelease 작업을 별도로 완료했습니다. Gradle 종료 코드 0, BUILD SUCCESSFUL 로그와\n'
    '현재 APK의 SHA256 일치를 확인했습니다. 원래 Unity 실패 기록도 그대로 포함했습니다.'
)
build_evidence_paths = [source / 'TestResults/AndroidBuild.txt']
if build_route == 'gradle-recovery':
    build_evidence_paths.extend([gradle_result_path, gradle_log_path])
build_evidence = {
    'route': build_route,
    'apkSHA256': apk_sha256,
    'records': [{'path': 'Verification/' + path.name, 'sha256': sha(path)}
                for path in build_evidence_paths],
    'sourceManifest': {'path': 'Verification/SourceBuildManifest.json', 'sha256': sha(release / 'Audit/SourceBuildManifest.json'), 'sourceCsDigest': source_build_manifest['sourceCsDigest']},
    'scope': 'Build completion and pre-build/current C# source hash agreement only; signing, alignment, assets and Android runtime QA are separate records.'
}
for required in ['ApkSignature.txt', 'ApkManifest.txt', 'ApkAlignment.txt']:
    assert (release / 'Audit' / required).stat().st_size > 0, required
shutil.copy2(apk_source, apk)
assert sha(apk) == apk_sha256, 'APK changed after build evidence was checked'
for file in (source / 'TestResults').glob('*'):
    if file.is_file(): shutil.copy2(file, verification / file.name)
for name in ['Tests.xml', 'Tests.log', 'AndroidBuild.log', 'AndroidGradleBuild.log', 'GradleRecoveryCacheCleanup.json', 'WindowsBuild.log', 'Preview.log', 'Preview-tall.log', 'ApkSignature.txt', 'ApkManifest.txt', 'ApkAlignment.txt', 'TypographyFidelity.txt', 'GoalGrowthSource.json', 'MotionCameraFidelity.txt', 'PhysicsFidelity.txt', 'PhysicsFidelityAudit7.json', 'RuntimePhysicsNative.txt', 'PhysicsNativeMethods.txt', 'ColliderSetupNative.txt', 'HoleFitNative.txt', 'HoleHierarchySource7.json', 'HoleContactMeshSource7.json', 'BuildConfigSource7.json', 'CountdownNative7.txt', 'CollisionHullAudit7.json', 'PhysicsExportSummary.json', 'OriginalPhysicsManager.json', 'FinderSource7.json', 'BuildCacheCleanup.json']:
    path = release / 'Audit' / name
    if path.exists(): shutil.copy2(path, verification / name)
for pattern in ['Tests-*.xml', 'Tests-*.log', 'TraceStage*.xml', 'TraceStage*.log', 'TraceFocused*.xml', 'TraceFocused*.log', '*Focused*.txt', 'Focused*.json', 'RotatedFloor*', 'SourceBuildManifest*.json', 'SourceBuildManifestWorkflow.txt', 'source_build_manifest.py', 'DiscardedBuildInputs*.json', 'Campaign*.txt', 'Stage*FailureTrace.txt', 'TestProvenance.json', 'FinalRegressionSourceHashes.json', 'FinalVerificationSummary.json', 'KnownValidationLimitations.json', 'finalize_physics_verification.py', 'ArchitecturePlan.json', 'FinalPreviewCacheCleanup.json', 'FreshAndroidBuildLaunch.json', 'FreshAndroidBuildCompletion.json', 'BombInfoSource7.json', 'FinalUIReview.txt', 'FinalSourceReview.txt', 'FinderSpriteSource7.json', 'IndicatorMidpointsSource7.json', 'FinderNative7.txt', 'AndroidQA*', 'AndroidBuild-*.log', 'AndroidBuild-*.txt', '*fresh2abi*.ps1', 'Fresh2Abi*.json', 'Fresh2Abi*.log', 'NativeCompilerRecovery.txt', 'WindowsCacheCleanup.json', 'StorageCompression.json', '*Cleanup*.json', '*Compression*.json', 'AndroidGradleBuild-*.log', 'Native*Retry.log', 'Prepare*.log', 'VerificationAtNativeCompile.cs.txt', 'VerificationSnapshotDifference.*', 'TrumpetPlannerBounds7.json', '*Diagnostic*.json', '*Diagnostic*.txt', '*Diagnostic*.log', 'HoleLayersSource7.json', 'RemainingLargeCollisionHulls.json', 'FinalBrandScan.json', 'merge_test_results.py', 'cleanup_editor_cache.ps1', 'finish_gradle_recovery.ps1']:
    for path in (release / 'Audit').glob(pattern):
        if path.is_file() and '-remaining-normal.' not in path.name:
            shutil.copy2(path, verification / path.name)

(verification / 'BuildRoute.json').write_text(json.dumps(build_evidence, ensure_ascii=False, indent=2), encoding='utf-8')

with zipfile.ZipFile(apk) as z:
    assert z.testzip() is None
    apk_entries = z.infolist()
    actual_android_abis = sorted({entry.filename.split('/')[1] for entry in apk_entries
                                 if entry.filename.startswith('lib/') and entry.filename.endswith('.so')})
    assert actual_android_abis == ['arm64-v8a', 'armeabi-v7a'], 'APK must contain exactly ARMv7 and ARM64 native libraries: ' + repr(actual_android_abis)
    data = {
        'apkBytes': apk.stat().st_size,
        'apkSHA256': apk_sha256,
        'androidBuild': build_evidence,
        'apkMinimumBytes': 500_000_000,
        'expectedAndroidABIs': ['armeabi-v7a', 'arm64-v8a'],
        'actualAndroidABIs': actual_android_abis,
        'entryUncompressedBytes': sum(f.file_size for f in apk_entries),
        'storedEntryBytes': sum(f.file_size for f in apk_entries if f.compress_type == zipfile.ZIP_STORED),
        'passedTests': int(tests.attrib['passed']),
        'failedTests': int(tests.attrib['failed']),
        'validation': validation,
        'campaignEntries': 465, 'extraChallenges': 111, 'collectibles': 1010,
        'authoredPoisonBombs': 153, 'authoredPillars': 24,
        'dailyCoins': 1000, 'attendanceCycleDays': 31,
        'androidQARecords': android_qa_records,
        'androidRuntimeQARecords': android_runtime_qa_records,
        'sourceBuildManifest': build_evidence['sourceManifest'],
        'discardedBuildInputHistory': ['Verification/VerificationAtNativeCompile.cs.txt', 'Verification/VerificationSnapshotDifference.json', 'Verification/VerificationSnapshotDifference.patch'],
        'androidQAScope': 'See actual report contents; presence alone does not imply success or physical-device testing.' if android_runtime_qa_records else 'Android runtime QA not run; availability inventory is not an execution pass.',
        'sizeMethod': 'Usable game assets and native libraries stored without ZIP compression; no filler.'
    }
(verification / 'ReleaseFacts.json').write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
(release / '읽어주세요.txt').write_text(f'''Hole It! - 7차

설치 파일: HoleIt_7.apk
전체 소스: Source 폴더 (Unity 6000.3.6f1)
지원 기기 ABI: ARMv7·ARM64. 사용할 수 있는 에뮬레이터가 없어 검사 전용 x86_64 빌드는 제외했습니다.
APK 실제 용량: {apk.stat().st_size:,} bytes ({apk.stat().st_size / 1_000_000:.1f} MB)
최종 전체 자동 검사: 총 {validation['total']}개 중 통과 {tests.attrib['passed']}개, 실패 {tests.attrib['failed']}개.
{validation_summary}
원본 최종 결과는 Verification/Tests.xml, Tests-final.xml 및 FinalVerificationSummary.json에 보존했습니다.
위 수치는 소스에 대한 자동 검사 결과이며 APK의 Android 실기기 검사 수치가 아닙니다.

{source_build_summary}
{build_summary}
빌드 경로와 증거 파일 해시는 Verification/BuildRoute.json에 기록했습니다.

이번 수정
- 홈·로딩 화면의 FOR GAEUN과 앱 이름의 개인 이름을 제거했습니다.
- Linik Sans Black 원본 글꼴과 확인된 주요 화면의 문구·크기·자간·재질을 반영했습니다.
  목표 수량, 노란색 3D Size 표시, 코인/생명, 시계, 로딩, 설정, 결과 화면과 BRAVO! 글자 모형을 포함합니다.
- 원작 실행 코드의 중력 기본값 -35, 50Hz 물리 계산, 물체별 질량/충돌 모드와 마찰 설정을 반영했습니다.
- 실제 원본 구멍 충돌 메시를 사용합니다. 임의의 크기 조건으로 바닥 충돌을 끄던 방식은 제거했습니다.
- 이동은 2.6에서 시작해 크기에 따라 증가하고, 손을 떼면 정지합니다.
- 원본 성장 애니메이션과 카메라 시야각·성장 확대 설정을 적용했습니다.
- 타이머는 플레이 화면 진입 후 첫 조이스틱 입력부터 시작합니다.
- 회전된 바닥이 있는 27개 캠페인 맵에서 실제 바닥 안쪽을 이동 불가로 판단하던 오류를 수정했습니다.
  바닥의 원본 위치·회전·크기를 역변환해 이동 가능 영역을 판정합니다.

기존 기능
기본 맵 465개, 추가 연습 맵 111개, 모형 1,010개, 폭탄 실패/재시도.
꼬똥 드 툴레아 배경, 모형 컬렉션, 누적 31회 출석과 하루 한 번 1,000 게임 코인.
출석을 빠져도 기록이 유지되며 31회 완료 다음 출석부터 새 출석판입니다.
이전 APK와 앱 ID 및 서명을 유지하여 덮어 설치 시 저장 데이터를 이어 쓸 수 있습니다.
이름이 들어간 내부 저장 키는 호환성을 위해 유지하며 화면에 표시하지 않습니다.

용량과 검증 범위
실제 게임 자산/라이브러리를 압축 없이 담아 APK 500 MB 이상을 유지했습니다. 더미 파일은 없습니다.
ZIP에는 설치 APK, 소스, 미리보기와 검증 결과가 있습니다. Library/Temp/중복 빌드 캐시는 제외했습니다.
Previews는 Windows 실행 화면입니다.
{android_qa_text}
원본 온라인 서버 설정과 전체 런타임을 그대로 복제한 것은 아닙니다.
물리 처리는 성능을 위해 근처 물체를 중심으로 계산하며, 원본과 남아 있는 차이는 Verification의 각 Fidelity 문서에 기록했습니다.
성장 순간의 가로 확대는 시각 효과이며, 물리 구멍의 확대는 균일 배율로 처리합니다. 상세 검토는 Verification/FinalSourceReview.txt에 있습니다.
''', encoding='utf-8-sig')
archive = release / 'HoleIt_7차_APK_Source.zip'
included = []
for directory in ['Source/Assets', 'Source/Packages', 'Source/ProjectSettings', 'Source/Tools', 'Previews', 'Verification']:
    included.extend(p for p in (release / directory).rglob('*') if p.is_file())
for name in ['Source/AGENTS.md', 'Source/.gitignore', 'Source/Build-Android.ps1', 'Source/README.txt', '읽어주세요.txt', 'HoleIt_7.apk']:
    included.append(release / name)
with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED, compresslevel=6, allowZip64=True) as z:
    for path in sorted(set(included)):
        assert path.stat().st_size > 0 or path.suffix not in ('.cs', '.json', '.asset')
        z.write(path, path.relative_to(release).as_posix())
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None, 'Release archive CRC error'
    assert hashlib.sha256(z.read('HoleIt_7.apk')).hexdigest() == data['apkSHA256']
    assert 'Source/Assets/Scripts/ReferenceAttendance.cs' in z.namelist()
    assert 'Source/Assets/Resources/ReferenceTypography/LinikSansBlack.asset' in z.namelist()
data['zipBytes'] = archive.stat().st_size
data['zipSHA256'] = sha(archive)
data['zipFiles'] = len(set(included))
(release / 'SHA256.json').write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(data, ensure_ascii=True))
