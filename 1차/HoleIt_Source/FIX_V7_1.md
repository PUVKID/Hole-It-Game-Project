# V7.1 FIX — 홀이 안 보이던 문제

원인: `Assets/Editor/HoleItBuilder.Scene.cs` 가 `Prim(...)` 헬퍼를 3번 호출(마스크 쿼드 / Pit 원통 / 림 쿼드)하는데
프로젝트 어디에도 정의가 없어 Editor 어셈블리 전체가 컴파일 실패(CS0103) 했습니다.
→ HoleIt 메뉴, 첫 실행 자동 구성, Main.unity 생성이 모두 동작하지 않아 홀이 보이지 않았습니다.

수정: `Assets/Editor/HoleItBuilder.cs` 에 `Prim(...)` 추가 (콜라이더 제거, 그림자 끔).

적용 후:
1. 기존에 만들어진 `Assets/Scenes/Main.unity` 가 있으면 삭제하거나, 메뉴 `HoleIt > 1. 프로젝트 전체 구성` 을 직접 실행하세요.
   (첫 실행 자동 구성은 Main.unity 가 이미 있으면 건너뜁니다)
2. Console 에 빨간 에러가 없는지 확인한 뒤 Play.

## 추가 수정 (컴파일 오류 CS0136)
- `HoleItBuilder.Models.cs` 370행: 감자튀김 루프의 `width` → `fryWidth` (같은 switch 블록의 자동차 case 에서 `width` 를 이미 선언)
- `HoleItBuilder.Models.cs` 552행: 타워 창문 루프의 `col` → `winCol` (메서드 하단의 `BoxCollider col` 과 충돌)

## 런타임 수정 (Play 로그 기준)
- **Invalid worldAABB / abnormal mesh bounds (Combined_BananaS...)**: `BuildBananaMesh` 에서 `Mathf.Pow(Mathf.Sin(PI*u), .55f)` 가 u=1 에서 음수^0.55 = NaN 이 되어 바나나 메쉬 전체가 NaN 이었음. `Mathf.Max(0, ...)` 로 수정.
- **TMP 글리프 경고 (▶, Ⅱ)**: 기본 폰트(LiberationSans SDF)에 없는 문자라 사각형으로 표시됨. 일시정지 버튼 `II`, 시작 버튼 `PLAY`, 그 외 › ‹ • 도 ASCII 로 교체.
- **[ObjectPooler] 풀 부족, 확장: Fx_Swallow**: FX 프리웜 수를 늘림 (swallow 8→24 등).
- **미션 대상이 아닌 물체도 먹기 (Hole it! 방식)**: 레벨 기본 `defaultWrongBehavior` 를 Blocked → None 으로 변경,
  모든 오브젝트 `canBeSwallowed = true`, 점수/성장 0 이던 소품(가로등/벤치/콘/소화전/기둥/바리케이드/신호등)에 기본 점수·성장 부여.
  이제 크기(Tier/반지름)만 맞으면 무엇이든 먹히고, 미션 카운트는 미션 대상만 올라갑니다.
- 반영하려면 `Assets/HoleIt` 폴더와 `Assets/Scenes/Main.unity` 를 지우고 다시 열거나 `HoleIt > 1. 프로젝트 전체 구성` 을 실행하세요.
  (에셋/ScriptableObject/메쉬가 이미 저장되어 있으면 새 값이 안 들어갑니다. 바나나처럼 NaN 으로 저장된 메쉬는 빌더가 자동으로 다시 만들지만, 나머지 값은 폴더를 지워야 확실합니다.)

## 물체 배치: 균일 격자 (LevelManager.Spawn)
- 랜덤 흩뿌림 대신, 맵을 같은 크기의 칸으로 나눠 스폰 그룹 순서(같은 종류끼리 이웃)로 지그재그 순서로 칸 중앙에 배치합니다.
- 물체 수가 칸보다 적으면 일정 간격으로 건너뛰며 맵 전체에 고르게 퍼집니다. 칸이 모자라면 남은 물체만 빈 곳에 랜덤 배치합니다.
- Hole 시작 위치 주변(광장)은 비워 두고, 모든 물체는 정면이 카메라를 향하게(yaw 180) 정렬합니다.
- 칸 크기/광장 크기는 `LevelManager` 의 `startClearRadius`, `edgeMargin` 및 `Spawn` 안의 `fit`, `cell` 계산에서 조정합니다.
- 이 변경은 스크립트(런타임) 쪽이라 씬을 다시 만들지 않아도 바로 적용됩니다.
