# POCKET PIT — HoleIt_Source 인계 프로젝트

**대상: Unity 6000.3.6f1 / Built-in Render Pipeline / 세로 화면 / Android 우선.**

기존 `HoleIt_Full_Project_ModelRefresh_v7_fixed.zip`의 소스를 보존·수정하고 실제 씬, 프리팹, 메시, 레벨 데이터, UI, 아이콘 및 음원을 추가한 프로젝트입니다. 게임 내 독자 타이틀은 **POCKET PIT**, 프로젝트 및 Editor 메뉴 이름은 기존 **HoleIt**을 유지했습니다.

## 먼저 확인할 검증 범위

이 인계본은 **구현 파일 작성 및 오프라인 정적 검증을 마친 상태**입니다. 작업 환경에 Unity Editor, C# 컴파일러, Android 빌드 모듈이 없어 **Unity 임포트·C# 컴파일·셰이더 컴파일·실제 플레이·APK 빌드는 수행하지 못했습니다.** 출시 검증이 끝난 실행 빌드 또는 무오류 보증본으로 해석하면 안 됩니다.

`Main.unity`, `.prefab`, `.asset`는 실제로 포함되어 있으나 Unity 외부에서 텍스트 직렬화로 작성했습니다. 파일·참조·메시 버퍼 검사를 통과한 것과 Unity가 성공적으로 임포트/실행한 것은 다릅니다. 자세한 결과는 `BUILD_REPORT.md`, `Documentation/STATIC_VALIDATION.json`, `Documentation/KNOWN_LIMITATIONS.md`에 있습니다.

## 1. 열기 및 첫 실행

1. ZIP을 해제하고 **Assets / Packages / ProjectSettings가 직접 들어 있는 `HoleIt_Source` 폴더**를 Unity Hub에 추가합니다. ZIP 파일 자체나 상위 폴더를 선택하지 않습니다.
2. Unity **6000.3.6f1**로 엽니다. 기존 버전 파일의 changeset은 `bbb010bdb8a3`입니다. 패키지 다운로드/임포트가 끝날 때까지 편집 작업을 시작하지 않습니다.
3. Console에 오류가 있으면 먼저 확인합니다. 특히 최초 패키지 해결, 네이티브 텍스트 에셋 임포트 및 스크립트 컴파일은 이번 제작 환경에서 검증하지 못한 항목입니다.
4. `Assets/Scenes/Main.unity`를 엽니다. 정상 제출 구조에는 이미 Main 씬과 직렬화된 30개 레벨 참조가 있으므로 전체 구성 메뉴를 먼저 실행할 필요가 없습니다.
5. `HoleIt > 3. 프로젝트 검사`를 실행한 뒤 Game View를 세로 비율로 두고 Play Mode를 시작합니다. 메인 메뉴의 `PLAY`를 누릅니다.

Main 씬이 없을 때 기존 첫 실행 설정 흐름이 프로젝트 구성을 시도하도록 유지했습니다. Main이 있으면 자동으로 덮어쓰지 않습니다. `HoleIt > 1. 프로젝트 전체 구성`은 필요할 때 수동 실행하며, 기존 씬을 백업하고 재구성합니다. 이 Editor 경로 역시 실제 실행 검증은 필요합니다.

## 2. 게임 구성

| 항목 | 포함한 내용 |
|---|---|
| 캠페인 | 레벨 데이터 30개, 테마 6개, 저장된 오브젝트 배치 957개 |
| 오브젝트 | 독립 모델·수집 프리팹·데이터 51종. 차량 색상별 변형, 디저트, 도시/공원/장난감 소품, 시간 부스터 등 |
| 시각 자료 | 메시 56개, 수집물 아이콘 51개와 UI 이미지 4개, 머티리얼 10개, 셰이더 5개 |
| 오디오 | 절차 생성 WAV 17개: 효과/이동음 11개 및 테마 음악 6개 |
| 기본 진행 | 이동 → 크기 판정 → 끌림/기울임/낙하/축소 → 보상 → 미션/성장 → 클리어 또는 실패 |
| 메뉴 | 시작, 잠금 표시가 있는 30레벨 선택, 설정, 일시정지, 클리어/실패 결과 |
| 저장 | 레벨 해금, 완료 상태, 레벨별 TOP 3 점수, 조작 모드, 음량/음소거 |

일부 제공 모델은 편집용 카탈로그에 포함되지만 기본 캠페인의 목표에는 사용되지 않습니다. 모든 51종이 각 레벨에 배치되는 것은 아닙니다.

## 3. 조작과 UI

기본 조작은 **Drag**입니다. 화면의 게임 공간을 누른 뒤 드래그하면 방향 입력이 전달되며, 손을 떼면 감속합니다. 홀을 손가락 위치로 순간이동시키지 않습니다. Editor에서는 마우스 드래그와 키보드 이동을 지원하는 코드가 있습니다.

설정에서 **FixedJoystick**을 선택하면 하단 중앙 고정 조이스틱으로 같은 이동 시스템을 사용합니다. UI 위에서 시작한 입력은 게임 이동으로 전달하지 않으며, 멀티터치에서 소유 손가락만 사용합니다. 일시정지·결과·메뉴 상태 및 포커스 상실 시 이동 입력을 초기화합니다.

상단에는 시간·점수·Tier·성장 게이지와 최대 3개의 미션 카드가 표시됩니다. 일시정지에서 Resume / Restart / Home / Sound를 사용합니다. 설정에서 Master / Music / SFX 음량, 음소거, 조작 모드를 조절합니다. 게임 표시 문구는 **영어**, 인계 문서는 한국어입니다. 한글 UI 현지화는 구현하지 않았습니다.

Safe Area와 Canvas Scaler를 사용하며 짧은 화면에서는 전체 논리 레이아웃을 축소합니다. 5가지 화면/인셋 조합의 수식 검사를 수행했으나 실제 기기 UI와 터치는 별도 확인해야 합니다.

## 4. Tier·흡수·보상 규칙

오브젝트는 현재 홀 Tier 이하이고 논리적 반지름도 홀 안에 들어갈 때만 흡수됩니다. 시각 모델의 수평 범위를 계산해 프리팹 Scale과 판정 크기를 맞췄습니다. 보상은 흡수 애니메이션 완료 시 한 번만 지급합니다. 대기/흡수 중인 오브젝트의 예약, 세션 번호, 보상 지급 여부를 확인하며, 재시작·홈·실패에서 이전 작업을 정리합니다.

누적 성장 기준은 Tier 1~6에 대해 `0 / 6 / 16 / 32 / 56 / 90`, 기본 반지름은 `0.9 / 1.2 / 1.6 / 2.1 / 2.7 / 3.4`입니다. 단계 사이 시각 크기는 부드럽게 보간하고 카메라 크기도 성장 표를 참조합니다. 레벨 1~15 성장 배율은 1.0, 16~30은 1.5로 설정했습니다.

기본 점수는 오브젝트 데이터에 있습니다. 미션 완료 보너스는 각 100점, 클리어 보너스 200점, 제한시간 레벨의 남은 시간 보너스는 초당 5점입니다. 무제한 레벨에서는 시간 보너스를 계산하지 않습니다. 점수 정수 범위와 비정상 성장 수치를 방어합니다.

### 오답 규칙

| 설정 | 동작 |
|---|---|
| `None` | 비목표도 정상 흡수하고 일반 점수/성장을 지급합니다. 목표와 맞지 않으면 미션은 진행되지 않습니다. |
| `Penalty` | 비목표도 흡수하되 기본 2초 및 8점의 벌칙을 한 번 적용합니다. 시간이 0이 되면 실패합니다. |
| `Blocked` | 현재 미완료 미션에 기여하지 않는 물체를 흡수하지 않습니다. 이미 예약된 물체도 목표 수량에 포함해 초과 예약을 막습니다. |

기존 enum 직렬화 호환성을 위해 내부 숫자는 **None=0, Blocked=1, Penalty=2**입니다. 문서의 표시 순서로 숫자를 추정하지 말고 Inspector의 이름으로 편집합니다.

시간 부스터는 기본 **8초**를 추가합니다. 시간 제한이 없는 1·2레벨에서는 일반 점수/성장만 반영하며 타이머를 시작하지 않습니다. 10초 이하 경고는 상태 전환 시 관리합니다.

## 5. 주요 데이터 편집

| 위치 | 용도 |
|---|---|
| `Assets/HoleIt/Resources/Data/Levels/Level_01.asset` ~ `Level_30.asset` | 각 레벨의 시간, 목표, 규칙, 맵, 시작 위치, 저장 배치 |
| `Assets/HoleIt/Data/Edibles/Edible_*.asset` | 종류, 색상, Tier, 논리 반지름, 점수, 성장, 프리팹, 아이콘, 부스터 |
| `Assets/HoleIt/Data/Themes/*.asset` | 바닥/체크/배경/림/조명/UI 색상 및 테마 음악 |
| `Assets/HoleIt/Resources/HoleSizeTable.asset` | Tier별 누적 성장량, 반지름, 카메라 크기 |
| `Assets/HoleIt/Resources/GameSettings.asset` | 이동 감도/속도/가감속, 흡수 간격, 연출 시간, 카메라, 경고 기준 |
| `Assets/HoleIt/Data/CampaignRecipe.json` | 누락 레벨을 재구성할 때 사용할 기본 30레벨 레시피 |

`LevelData.timeLimit=0`은 무제한입니다. `missions`에는 `ExactObject`, `ObjectCategory`, `Color`, `Tier`, `TimeBooster` 조건을 설정할 수 있습니다. 색상 미션은 시각 색을 읽지 않고 `ColorVariant`와 카테고리를 판정합니다. 기본 캠페인은 명시적인 오브젝트 목표가 중심이며 일반화된 미션 조건은 코드와 테스트에 포함되어 있습니다.

현재 UI/예약 저장소는 **레벨당 최대 3개 미션**을 기준으로 작성했습니다. 더 늘릴 경우 UI와 검증기를 함께 확장해야 합니다. 활성 미션을 모두 완료해야 클리어됩니다.

`placements`는 실제 플레이에서 사용하는 저장 배치입니다. `spawnGroups`는 배치 생성 재료입니다. 그룹만 바꾸고 배치를 재생성하지 않으면 실제 위치가 바뀌지 않습니다. 해당 LevelData를 선택하고 **HoleIt > Tools > Rebuild Selected Level Layout**을 실행한 뒤 프로젝트 검사를 실행합니다. Seed가 같으면 동일한 생성 순서를 사용하도록 작성했습니다.

Tier, 수량, 반지름, 맵 크기 또는 미션을 수정한 뒤에는 반드시 레벨 검사를 다시 수행합니다. 특히 `Blocked`에서는 낮은 Tier 목표를 통해 높은 Tier 목표까지 성장할 수 있어야 합니다. 목표 수량만 충분하다고 완료 가능성이 보장되지 않습니다.

## 6. 저장 정책

새 저장에서는 레벨 1부터 시작하며 클리어 시 다음 레벨을 해금합니다. TOP 3는 **각 레벨의 클리어 점수**를 내림차순으로 저장합니다. 실패 점수를 완료 기록으로 저장하지 않습니다.

저장 키는 `HoleIt.MaxLevel`, `HoleIt.V2.L{levelId}.Score0/1/2` 및 완료/설정 키 계열을 사용합니다. 기존 전역 점수 기록은 지우지 않지만 레벨을 알 수 없으므로 특정 레벨의 기록으로 임의 이관하지 않습니다. 테스트는 사용한 키를 백업/복구하는 코드로 작성했고 `PlayerPrefs.DeleteAll()`로 사용자의 전체 저장을 초기화하지 않습니다. 테스트 실행 자체는 이번 환경에서 하지 못했습니다.

## 7. Editor 도구와 재구성 범위

`HoleIt > 1. 프로젝트 전체 구성`은 기존 Builder를 재사용하며, 새 30레벨 레시피와 포함된 에셋을 연결합니다. 생성 순서는 데이터/프리팹 저장 → 참조 연결 → 씬 저장 → 검사입니다. 기존 Main은 수동 재구성 시 `Documentation/SceneBackups`에 백업하도록 작성했습니다. 기존 Inspector 조정값과 GUID를 불필요하게 다시 만들지 않습니다.

`HoleIt > 3. 프로젝트 검사`는 Unity에서 임포트된 메시/셰이더, 씬 연결, 레벨 데이터를 검사하고 `TestResults` 아래에 실행 결과를 남깁니다. 이 메뉴를 아직 실행하지 않았으므로 그 결과 파일을 미리 만들어 성공으로 표시하지 않았습니다.

51종의 절차 모델 생성 원본 코드, 기본 캠페인/음원 생성 코드는 `Tools/OfflineAuthoring`에 있습니다. Python은 정상적인 Unity 실행에 필요하지 않습니다. 해당 도구는 기본 실행이 차단되며, 의도적인 원형 에셋 재생성은 **백업 복사본에서만** 수행합니다. 세부 사항은 그 폴더의 README를 읽습니다. 이식된 도구의 구문 및 보호장치 검사만 실행했고 전체 재생성은 재실행하지 않았습니다.

**복구 한계:** Main만 누락된 경우를 위한 Editor 자동 설정 경로와, 모든 데이터를 삭제한 뒤 완전한 원형을 복원하는 작업은 다릅니다. 6개 ThemeData까지 삭제했다면 원본 ZIP에서 복원하거나 보호된 오프라인 캠페인 생성기를 사용해야 합니다. 완성 UI 프리팹도 제출본을 보존하는 것이 기본입니다.

## 8. 검증 실행

### 오프라인 정적 검사

```sh
python -m pip install -r Tools/requirements-static.txt
python Tools/validate_static.py
```

결과는 `Documentation/STATIC_VALIDATION.json`에 기록됩니다. 실제 포함된 파일을 읽어 13개 검사 그룹과 7개 잘못된 데이터 감지 사례를 확인합니다. **Unity를 실행하거나 C#을 컴파일하지 않습니다.**

### Unity Test Runner

Unity에서 EditMode의 `CampaignEditModeTests`, PlayMode의 `CampaignPlayModeTests`를 실행합니다. EditMode는 10개 일반 테스트와 점수 포화 3개 매개변수 사례, PlayMode는 5개 테스트로 작성했습니다. 포함된 검사는 30레벨 데이터/로드, 배치 재현성, 크기, 미션 중복/예약, 저장, 풀 반환, 일시정지, 흡수 보상, 엄격 모드, 재시작 등입니다.

테스트 전에 열린 씬을 저장하고 별도 작업 복사본을 사용하는 것을 권장합니다. PlayMode 테스트는 Main을 단일 씬으로 로드하므로 현재 테스트 씬을 바꿉니다. 실제 장시간 완주나 모든 사용자 동작을 자동으로 보장하는 테스트 묶음은 아닙니다.

### 명령행 실행

같은 프로젝트를 Editor와 배치 프로세스에서 동시에 열지 않습니다. Windows PowerShell 예:

```powershell
$env:UNITY_EDITOR="C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Unity.exe"
.\Tools\unity.ps1 validate
.\Tools\unity.ps1 editmode
.\Tools\unity.ps1 playmode
.\Tools\unity.ps1 android
```

설치 위치가 다르면 실제 Unity.exe 경로를 지정합니다. Linux/macOS는 `UNITY_EDITOR`를 설정한 뒤 `bash Tools/unity.sh validate` 등의 같은 동작을 사용합니다. PowerShell 래퍼는 작성만 했고 이 환경에서 실행하지 않았습니다. 배치 PlayMode/에셋 재생성에는 환경에 따라 그래픽 표시 장치가 필요할 수 있습니다.

## 9. Android APK

제출본에 APK는 없습니다. Unity Hub에서 해당 Editor의 Android Build Support 및 관련 SDK/NDK/JDK 환경을 준비한 뒤 Console 및 프로젝트 검사 오류를 먼저 해결합니다.

현재 기본값은 Portrait, IL2CPP, ARM64, 최소 API 26, Target API 자동, OpenGLES3, 패키지명 `com.pocketpit.game`입니다. 패키지명·서명키·스토어 제출 설정은 실제 배포자 기준으로 검토해야 합니다. 현재 스토어 승인 요건을 검증했다는 의미가 아닙니다.

`HoleIt > 2. Android APK 빌드`를 실행합니다. Editor의 활성 빌드 대상이 Android가 아니면 전환 후 재컴파일을 기다리고 메뉴를 **다시 실행**하도록 설계했습니다. 배치 실행은 시작할 때 `-buildTarget Android`를 지정해야 하며 제공 래퍼에 포함했습니다. 배치 안에서 뒤늦게 플랫폼을 바꾸지 않습니다.

빌드가 실제 성공하면 기본 출력 경로는 `Builds/Android/HoleIt.apk`입니다. Editor 메뉴는 모듈, 씬, 패키지명, 화면 방향, IL2CPP/ARM64 및 빌드 결과와 파일 존재 여부를 검사하도록 작성했습니다. 모듈·라이선스·도구가 없으면 실패 원인을 보고하고 APK 성공을 표시하지 않습니다.

## 10. 파일 구조와 유지보수

`Assets/Scripts`는 기존 역할별 구조를 유지했습니다. `Assets/Editor`에는 Builder/첫 실행/검증/Android 도구, `Assets/Tests`에는 실제 Unity에서 실행할 테스트가 있습니다. `Assets/HoleIt`에 실제 에셋을 보관했습니다. 별도의 중복 게임 매니저나 스포너를 추가하지 않았습니다.

원본 ZIP은 `Documentation/OriginalSourceSnapshot.zip`, 원본 50개 파일 해시는 `Documentation/OriginalInventory.json`에 보관했습니다. 수정·신규·삭제 여부는 `Documentation/CHANGE_MANIFEST.json`에서 확인합니다. 과거 인계 문서도 보존했습니다. 과거 문서의 20레벨, 입력 설정, TMP 관련 설명이 현재와 다르면 **이 README와 현재 소스/데이터를 기준**으로 합니다.

추가 참고: `Documentation/LEVEL_MATRIX.md`, `Documentation/TEST_MATRIX.md`, `Documentation/ASSET_PROVENANCE.md`, `Documentation/KNOWN_LIMITATIONS.md`.
