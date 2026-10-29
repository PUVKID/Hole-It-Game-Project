# 기술 확인에 사용한 공식 자료

요구사항의 기준은 사용자가 첨부한 `붙여넣은 텍스트(1).txt`입니다. 코드 통합과 실제 파일 검사는 첨부 원본과 제출 프로젝트 파일을 대상으로 했습니다. 외부 자료는 API/버전 확인에만 사용했으며 원작 게임 리소스 수집에는 사용하지 않았습니다.

- Unity 6000.3.6f1 공식 릴리스: https://unity.com/releases/editor/whats-new/6000.3.6f1
  - 2026-01-29 릴리스, revision `bbb010bdb8a3` 확인. 원본 버전 파일 유지.
- Unity 6000.3 `EditorUserBuildSettings.SwitchActiveBuildTarget`: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/EditorUserBuildSettings.SwitchActiveBuildTarget.html
  - 배치 모드 중 활성 대상 변경 대신 시작 인수 `-buildTarget` 사용. Editor 전환 후 스크립트 재컴파일 필요성을 Android 빌더에 반영.
- Input System EnhancedTouch API: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/api/UnityEngine.InputSystem.EnhancedTouch.Touch.html
  - 터치 폴링/손가락 소유권 처리 참고. Input 1.14.0 기존 버전 유지.

패키지 manifest와 lock의 JSON/의존성은 파일 기준으로 확인했지만, 실제 Unity Package Manager 다운로드 및 해결은 이번 환경에서 미실행입니다.
