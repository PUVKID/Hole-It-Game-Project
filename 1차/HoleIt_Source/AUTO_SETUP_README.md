# HoleIt Auto Setup (v4)

<!-- user-levels:start -->
<details>
<summary><strong>읽기 수준 선택</strong></summary>

| 레벨 | 읽는 목적 |
| --- | --- |
| [LEVEL 1 - Beginner](../../docs/%EC%9D%BD%EA%B8%B0%EC%95%88%EB%82%B4/%EB%A0%88%EB%B2%A81.md) | 목적과 입력·출력을 쉬운 말로 이해하기 |
| [LEVEL 2 - Explorer](../../docs/%EC%9D%BD%EA%B8%B0%EC%95%88%EB%82%B4/%EB%A0%88%EB%B2%A82.md) | 주요 기능과 전체 처리 흐름 살펴보기 |
| [LEVEL 3 - Practitioner](../../docs/%EC%9D%BD%EA%B8%B0%EC%95%88%EB%82%B4/%EB%A0%88%EB%B2%A83.md) | 설정·실행 순서와 결과 확인하기 |
| [LEVEL 4 - Advanced](../../docs/%EC%9D%BD%EA%B8%B0%EC%95%88%EB%82%B4/%EB%A0%88%EB%B2%A84.md) | 구현 선택과 조건·한계 비교하기 |
| [LEVEL 5 - Expert](../../docs/%EC%9D%BD%EA%B8%B0%EC%95%88%EB%82%B4/%EB%A0%88%EB%B2%A85.md) | 세부 근거와 재현 조건 검토하기 |

</details>
<!-- user-levels:end -->

이번 버전은 사용자가 ZIP을 푼 뒤 별도 메뉴를 찾지 않아도 되도록 첫 Unity Editor 로드 시 자동으로 프로젝트 에셋 및 `Assets/Scenes/Main.unity`를 생성합니다.

## 이번에 고친 점

- `Assets/Editor/HoleItFirstOpenSetup.cs` 추가: 첫 실행에 `Main.unity`가 없으면 에셋과 씬 자동 생성 후 해당 씬을 엽니다.  
  이미 씬이 있으면 덮어쓰지 않습니다.
- TMP Essential Resources를 수동으로 가져오지 않아도 UI 텍스트를 만들 수 있도록 `LiberationSans SDF` 폰트 에셋을 TMP 패키지에서 직접 찾아 생성된 텍스트에 명시적으로 지정합니다.
- 기존 메뉴는 유지합니다: `HoleIt > 1. 프로젝트 전체 구성`, `HoleIt > 2. Android APK 빌드`.

## 사용

1. ZIP을 ASCII 경로에 풀기(예: `C:\UnityProjects\HoleIt_Source`).
2. Unity Hub에서 `HoleIt_Source` 폴더를 엽니다.
3. package resolve / script compilation / 자동 구성이 끝날 때까지 기다립니다.
4. `Assets/Scenes/Main.unity`가 생성된 뒤 자동으로 열려야합니다. Game 탭에서 Play를 누릅니다.

## 한계

여기서는 Unity Editor 실행 파일이 없어서 Unity 실제 컴파일이나 APK 빌드를 실행해 보지는 못했습니다.  
자동 구성이 실패하면 Console에서 첫 번째 `[HoleItBuilder]` 또는 `[HoleIt Setup]` 오류를 확인해야 합니다.

V5 FIX
------
Removed TMP outline material mutation from the generated TextMeshProUGUI helper.

On fresh package imports that setter could throw NullReferenceException before Main.unity was saved.

A normal Unity UI Shadow is used instead.
