# BUILD REPORT — POCKET PIT / HoleIt_Source

작성일: **2026-10-10**. 대상 파일: `HoleIt_Source_Completed.zip`.

## 결론

**구현 파일 및 오프라인 정적 검증을 완료한 인계본입니다. Unity 컴파일/실행/출시 빌드를 완료한 것은 아닙니다.** 실제 제출 파일 안에는 Main 씬, 필수 프리팹/메시/레벨/메타, Packages, ProjectSettings, 생성 도구와 테스트 코드가 있습니다. Unity가 없는 환경에서 네이티브 텍스트 에셋을 작성했으므로 최초 Unity 임포트부터 확인이 필요합니다.

## 1. 환경 및 원본

| 항목 | 확인 결과 |
|---|---|
| 입력 원본 | HoleIt_Full_Project_ModelRefresh_v7_fixed.zip, 원본 파일 50개 |
| Unity 목표 | 6000.3.6f1, changeset bbb010bdb8a3 |
| 렌더링 | Built-in, customRenderPipeline 미지정 |
| 화면/플랫폼 | Portrait, Android 우선 |
| 제작 환경 | Linux, Python 3.13.5 |
| Unity / dotnet / mcs / csc | 모두 실행 파일 없음 |
| Unity 실행 사전검사 | bash 래퍼 실제 실행, exit 69 BLOCKED; Unity 프로세스 실행 안 됨 |

원본 ZIP과 해시 목록을 Documentation에 보관했으며, 기존 원본 파일을 삭제하지 않았습니다. 변경 목록과 SHA-256 비교는 `Documentation/CHANGE_MANIFEST.json`에 기록합니다.

## 2. 실제 포함한 구현

**게임플레이:** 기존 GameManager, LevelManager, HoleController, HoleSwallow, EdibleObject, ObjectPooler를 재사용·수정했습니다. 끌림/기울임/낙하/축소 후 보상, Tier와 논리 반지름의 이중 조건, 성장을 통한 반지름/카메라 갱신, 상태별 입력/타이머 차단, 실패/재시작/홈에서 진행 중 흡수 정리를 반영했습니다. 보상 중 상태가 바뀌어 흡수 목록이 비워지는 경우의 접근도 방어했습니다.

**미션:** 명시적 오브젝트·카테고리·색상·Tier·시간 부스터 조건을 데이터로 지원합니다. 동시 미션은 최대 3개, 전부 완료해야 클리어합니다. None/Penalty/Blocked를 분리했습니다. Blocked는 미완료 목표와 흡수 중 예약을 함께 계산하고, Penalty는 오브젝트당 한 번 적용하며 시간이 0일 때 실패합니다.

**입력/UI:** EnhancedTouch 기반 손가락 소유권, 마우스/키보드, Drag/FixedJoystick 선택, UI Raycast 차단, 포커스 및 해상도 변경 초기화, Safe Area 축소 레이아웃을 작성했습니다. Main에는 메뉴, 30레벨 선택, 3미션 HUD, 8개 팝업 풀, 일시정지/결과/설정 패널을 직렬화했습니다. 영어 UI이며 한글 게임 현지화는 없습니다.

**저장/오디오/효과:** PlayerPrefs로 레벨 해금·완료·레벨별 클리어 TOP 3·조작·음량을 저장하는 코드를 연결했습니다. 캐시된 17개 절차 WAV를 재생하고 재사용 링/메시 입자 효과를 사용합니다. 실청취·모바일 오디오·실제 저장 복원은 테스트하지 못했습니다.

**콘텐츠:** 레벨 30개, 테마 6개, 저장 배치 957개, 수집 모델/프리팹/EdibleData 51종, 메시 56개, 게임 PNG 55개, WAV 17개, 머티리얼 10개, 셰이더 5개, 프리팹 총 57개가 실제 파일로 있습니다. 51종 아이콘의 오프라인 모음 이미지를 별도 포함했습니다.

**Editor:** 기존 자동 설정과 전체 구성/Android 메뉴를 유지하고 프로젝트 검사와 선택 레벨 배치 재저장 메뉴를 추가했습니다. 기존 Main 보호 및 수동 재구성 백업, 데이터 저장 후 씬 참조 연결, 30레벨 기본 레시피, native 메시 재사용, 과거 모델 fallback의 NaN/Scale/지역 변수 범위 보완을 반영했습니다. 실제 Editor 재실행 멱등성은 미검증입니다.

## 3. 패키지와 설정

기존 Input System 1.14.0 및 uGUI 2.0.0을 유지했습니다. 불필요한 별도 TMP 패키지 대신 내장 폰트를 사용하는 uGUI 래퍼를 사용하고 Test Framework 1.4.5를 추가했습니다. 직접 의존성 13개, 잠금 항목 14개, asmdef 4개입니다. **lock은 수동 작성한 seed이며 실제 UPM 해결을 거치지 않았습니다.**

Android 기본값은 IL2CPP/ARM64, OpenGLES3, 최소 API 26, 타깃 API 자동, 패키지명 com.pocketpit.game입니다. Android 대상 변경 후 재컴파일을 먼저 마치고 다시 빌드하도록 작성했습니다. 배치 래퍼는 처음부터 `-buildTarget Android`를 전달합니다. 실제 설정 수용 및 빌드 도구 설치 상태는 Unity에서 확인해야 합니다.

## 4. 실제 검증 결과

| 범위 | 상태 | 실행 증거 |
|---|---|---|
| 오프라인 검사 13그룹 | PASS | Tools/validate_static.py 및 STATIC_VALIDATION.json |
| 에셋 메타 | PASS | 374개 고유 GUID |
| 네이티브 파일/참조 | PASS | 220파일/2,410객체, 외부 2,529 + 로컬 4,803 참조, 누락 0 |
| Main 직렬화 | PASS | 215 GameObject, 30레벨 참조, 30선택 버튼, 8팝업 |
| C# 어휘·일부 구조 | PASS | 52파일/55타입. C# 의미 분석/컴파일 아님 |
| 메시 수치/인덱스 | PASS | 56메시/27,440정점/52,840삼각형, 비유한 값 0 |
| 이미지·음원 파일 무결성 | PASS | PNG 55개/WAV 17개/총 음원 길이 52.51초 |
| 30레벨 데이터/성장 경로 | PASS | 957개 배치 검사 및 완료에 필요한 성장 순서 검증 |
| 잘못된 데이터 감지 | PASS | 서로 다른 7개 오류 주입 후 모두 검출 |
| Safe Area 수식 | PASS | 5화면 조합. 실기기 렌더 아님 |
| 이식 생성기 보호장치 | PASS | 무단 재생성 실행 차단, 에셋 바이트 변경 없음 |
| Unity C# 및 셰이더 컴파일 | BLOCKED | Editor/컴파일러 없음 |
| Unity 임포트/씬 실행/UPM | NOT RUN | Editor 없음 |
| Unity EditMode/PlayMode | NOT RUN | 작성한 테스트를 아직 실행하지 않음 |
| 실제 완주/모바일 터치/60fps | NOT RUN | 플레이/실기기/Profiler 없음 |
| Android APK | BLOCKED | 빌드 환경 없음, APK 파일 없음 |
| 최종 ZIP CRC/재압축 해제 검사 | 별도 제출 기록 | HoleIt_ZIP_VALIDATION.json에 실제 최종 검사 결과 기록 |

에디터용 검증 파일, NUnit XML, APK 또는 실제 게임 스크린샷을 만들어 놓은 것처럼 보고하지 않습니다. `STATIC_VALIDATION.json`에 나오는 PASS는 각 항목에 표시한 정적 범위의 PASS입니다.

## 5. 테스트 코드

EditMode는 일반 테스트 10개와 점수 포화 테스트의 매개변수 3사례, PlayMode는 5개를 작성했습니다. 미션 예약/중복 보상, 저장 TOP 3, 풀 중복 반환, 30레벨 데이터/로드, 결정적 배치, 메시, 일시정지, Blocked와 재시작 검사를 포함합니다. PlayerPrefs 사용 키를 백업/복구하도록 작성했습니다. 실제 Test Runner 실행 결과는 없습니다.

프로젝트에 `Tools/unity.sh`와 `Tools/unity.ps1`을 포함했습니다. bash 래퍼의 환경 부재 사전검사를 실제 실행했고, PowerShell 실행은 미진행입니다. 이식된 오프라인 생성기의 전체 재생성은 기존 제출 에셋을 보존하기 위해 실행하지 않았습니다.

## 6. 주된 설계 가정

독자 타이틀은 POCKET PIT, 화면 문구는 영어, 기준 논리 화면은 1080×1920입니다. 처음에는 레벨 1만 해금하고, TOP 3는 레벨별 클리어 기록으로 정의했습니다. 1·2레벨은 무제한, 부스터는 8초, 벌칙은 2초/8점, 후반 성장 배율은 1.5입니다. 저장 중간 이어하기는 범위에 넣지 않았습니다.

고급 소용돌이 효과, 햅틱, 광고/결제/분석, 온라인 순위, 한국어 UI는 포함하지 않았습니다. 자료 생성의 독자성과 배포 권리/상표 검토 완료는 별개입니다.

## 7. 남은 확인 사항

`Documentation/KNOWN_LIMITATIONS.md`와 `Documentation/TEST_MATRIX.md`를 기준으로 Unity 최초 임포트/컴파일 → Main 렌더 → 프로젝트 검사 → EditMode/PlayMode → 30레벨 실제 완주 → Android 실기기 검증을 수행해야 합니다. 후반 Penalty 레벨의 시간은 데이터상 성장 가능성만 확인했으므로 실제 경로/벌칙 밸런스 조정이 필요할 수 있습니다.

전체 데이터 삭제 후 완전 복구와 Main만 없을 때의 자동 설정은 같은 범위가 아닙니다. ThemeData/UI 원형 복구는 원본 제출 ZIP 또는 보호된 생성 도구를 사용합니다. 최초 package lock 해결 결과도 확인이 필요합니다.
