from pathlib import Path
root=Path(__file__).resolve().parent.parent
previous=root.parent/'6차/Audit/package_release.py'
script=previous.read_text(encoding='utf-8-sig')
script=script.replace('Gaeun_Hole_6.apk','HoleIt_7.apk').replace('HoleIt_6차_APK_Source.zip','HoleIt_7차_APK_Source.zip')
script=script.replace(">= 40", ">= 44")
script=script.replace("'LevelFidelityAudit.txt', 'LevelFidelityAudit.json', 'ContentExpansion.json', 'VerifiedDuplicateCleanup.json', 'BuildCacheCleanup.json'", "'TypographyFidelity.txt', 'GoalGrowthSource.json', 'MotionCameraFidelity.txt', 'PhysicsFidelity.txt', 'PhysicsExportSummary.json', 'OriginalPhysicsManager.json', 'FinderSource7.json', 'BuildCacheCleanup.json'")
start=script.index("(release / '읽어주세요.txt').write_text(f'''")
end=script.index("''', encoding='utf-8-sig')",start)+len("''', encoding='utf-8-sig')")
readme="""(release / '읽어주세요.txt').write_text(f'''Hole It! - 7차

설치 파일: HoleIt_7.apk
전체 소스: Source 폴더 (Unity 6000.3.6f1)
APK 실제 용량: {apk.stat().st_size:,} bytes ({apk.stat().st_size / 1_000_000:.1f} MB)
자동 검사: {tests.attrib['passed']}개 통과, 실패 {tests.attrib['failed']}개.

이번 수정
- 홈·로딩 화면의 FOR GAEUN과 앱 이름의 개인 이름을 제거했습니다.
- Linik Sans Black 원본 글꼴과 실제 화면의 문구·크기·자간·재질을 복원했습니다.
  목표 수량, 노란색 3D Size 표시, 코인/생명, 시계, 로딩, 설정, 결과 화면과 BRAVO! 글자 모형을 포함합니다.
- 원작 실행 코드의 중력 기본값 -35, 50Hz 물리 계산, 물체별 질량/충돌 모드와 마찰 설정을 반영했습니다.
- 실제 원본 구멍 충돌 메시를 사용합니다. 임의의 크기 조건으로 바닥 충돌을 끄던 방식은 제거했습니다.
- 이동은 2.6에서 시작해 크기에 따라 증가하고, 손을 떼면 정지합니다.
- 원본 성장 애니메이션과 카메라 시야각·성장 확대 설정을 적용했습니다.
- 타이머는 플레이 화면 진입 후 첫 조이스틱 입력부터 시작합니다.

기존 기능
기본 맵 465개, 추가 연습 맵 111개, 모형 1,010개, 폭탄 실패/재시도.
꼬똥 드 툴레아 배경, 모형 컬렉션, 누적 31회 출석과 하루 한 번 1,000 게임 코인.
출석을 빠져도 기록이 유지되며 31회 완료 다음 출석부터 새 출석판입니다.
이전 APK와 앱 ID 및 서명을 유지하여 덮어 설치 시 저장 데이터를 이어 쓸 수 있습니다.
이름이 들어간 내부 저장 키는 호환성을 위해 유지하며 화면에 표시하지 않습니다.

용량과 검증 범위
실제 게임 자산/라이브러리를 압축 없이 담아 APK 500 MB 이상을 유지했습니다. 더미 파일은 없습니다.
ZIP에는 설치 APK, 소스, 미리보기와 검증 결과가 있습니다. Library/Temp/중복 빌드 캐시는 제외했습니다.
Previews는 Windows 실행 화면입니다. 실제 Android 휴대폰에서 성능과 조작감은 아직 측정하지 못했습니다.
원본 온라인 서버 설정과 전체 런타임을 그대로 복제한 것은 아닙니다.
물리 처리는 성능을 위해 근처 물체를 중심으로 계산하며, 원본과 남아 있는 차이는 Verification의 각 Fidelity 문서에 기록했습니다.
''', encoding='utf-8-sig')"""
script=script[:start]+readme+script[end:]
(root/'Audit/package_release.py').write_text(script,encoding='utf-8')
print('Edition 7 packaging script prepared.')
