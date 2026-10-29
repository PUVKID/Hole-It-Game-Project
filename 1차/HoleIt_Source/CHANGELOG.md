# 변경 요약 — v8 인계본

원본 파일 **50개 중 36개 수정, 14개 동일 유지, 삭제 0개**입니다. 원본 전체 ZIP과 SHA-256 목록을 Documentation에 보관했습니다. 신규 파일에는 씬, 직렬화 데이터, 프리팹/메시/아이콘/음원/메타, 런타임 보완, 테스트, 생성/검사 도구와 문서를 포함했습니다. 전체 목록은 `Documentation/CHANGE_MANIFEST.json`을 참고합니다.

## 기존 주요 파일 수정

- `GameManager.cs`, `LevelManager.cs`: 상태/세션 정리, 30레벨 실제 참조, 점수/시간/부스터/벌칙, 해금 및 종료 처리 연결.
- `InputManager.cs`, `VirtualJoystick.cs`, `HoleController.cs`: EnhancedTouch, Drag/FixedJoystick, UI/포커스 차단, 성장 및 경계/Scale 일치.
- `HoleSwallow.cs`, `EdibleObject.cs`, `ObjectPooler.cs`: 예약/단일 보상, 흡수 애니메이션과 취소, 콜백 중 목록 변경 방어, 풀 중복 반환/플레이 중 확장 제한.
- `MissionManager.cs`, `LevelData.cs`, `EdibleData.cs`: 명시적 색상/종류/Tier/부스터 목표, 미완료 목표와 예약, 저장 배치 및 데이터 검증.
- `UIManager.cs`, `SafeAreaFitter.cs`, `HighScoreManager.cs`: 메뉴/설정/결과, Safe Area, 레벨별 TOP 3와 기존 키 보존.
- 기존 Editor Builder partial 파일: Main 보호, 생성 순서, 실제 참조 연결, 원형 메시/모델 재사용, NaN/루트 Scale/지역 변수 범위 회귀 방어.
- `Packages/manifest.json` 및 프로젝트 설정: 필요한 패키지/입력/Android/빌드 씬 설정 유지·연결.

## 주요 신규 파일/에셋

- `Assets/Scenes/Main.unity`, `Assets/HoleIt/Prefabs/Interface.prefab` 및 실제 게임 프리팹 56개.
- `Assets/HoleIt/Resources/Data/Levels/Level_01.asset` ~ `Level_30.asset`, 6개 ThemeData, 기본 레시피.
- `Assets/HoleIt/Meshes`, `Data/Edibles`, `Textures`, `Audio`, 신규 VertexLit 셰이더와 머티리얼.
- `LevelLayout.cs`, `LevelValidator.cs`, `NumberGuard.cs`, `AudioManager.cs` 등 기존 책임을 보완하는 구성 요소.
- `HoleItProjectValidation.cs`, EditMode/PlayMode 테스트, CLI 및 오프라인 검증/생성 도구.
- 한국어 README, 빌드/테스트/제한사항/레벨/에셋 출처 문서와 실제 검사 JSON.

과거 인계/수정 문서는 원형대로 유지했습니다. 최신 기능/레벨/설정 설명은 `README_KR.md`와 현재 소스를 기준으로 합니다. 실행하지 않은 Unity 검사를 성공으로 기록하지 않았습니다.
