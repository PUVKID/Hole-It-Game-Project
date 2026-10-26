# 공개 사본의 제한된 변환

원본 파일과 최초 공개 준비본은 수정하지 않았습니다. 승인된 환경변수 변환과 공개 제외만 적용했습니다. 의존성이나 실험 알고리즘은 추가·수정하지 않았습니다.

| 환경변수 | 준비할 값 |
|---|---|
| `HOLEIT_WORKSPACE_ALIAS` | 실제 프로젝트 작업공간 별칭 경로 |

## 실제 변경

- `7차/Verification/cleanup_editor_cache.ps1`: missing_workspace_alias_explicit_error, private_workspace_alias_to_environment
- `7차/Verification/finish_gradle_recovery.ps1`: missing_workspace_alias_explicit_error, private_workspace_alias_to_environment
- `7차/Verification/fresh2abi_recovery_guard.ps1`: missing_workspace_alias_explicit_error, private_workspace_alias_to_environment
- `6차/Source/Assets/Scripts/UI/Korean.cs`: personal_ui_label_anonymized
- `6차/Source/Assets/Editor/HoleItBuilder.SmoothEdition.cs`: personal_ui_label_anonymized
- `5차/Source/Assets/Scripts/UI/Korean.cs`: personal_ui_label_anonymized
- `5차/Source/Assets/Editor/HoleItBuilder.SmoothEdition.cs`: personal_ui_label_anonymized
- `4차/Source/Assets/Scripts/UI/Korean.cs`: personal_ui_label_anonymized
- `4차/Source/Assets/Editor/HoleItBuilder.SmoothEdition.cs`: personal_ui_label_anonymized
- `3차/Source/Assets/Scripts/UI/Korean.cs`: personal_ui_label_anonymized
- `3차/Source/Assets/Editor/HoleItBuilder.SmoothEdition.cs`: personal_ui_label_anonymized
- `2차/Source/Assets/Scripts/UI/Korean.cs`: personal_ui_label_anonymized
- `2차/Source/Assets/Editor/HoleItBuilder.SmoothEdition.cs`: personal_ui_label_anonymized

## 검사 범위

UTF-8, 파일 SHA-256, 추가 원본 두 번의 안정성 읽기, Python AST 및 가능한 노트북 셀 문법, 노트북 출력·첨부·메타데이터 제거, 비밀정보·개인정보 후보 탐지를 검사했습니다. 기존 매직 명령 셀 등 일반 Python 구문이 아닌 셀은 실행하지 않았습니다. 인증 정보·입력 자료가 없으므로 실험 결과와 전체 실행은 확인하지 않았습니다.

최초 프로젝트 명세 SHA-256: `dbc7217f015f0ac8c90f7561f6c86ca19ea3a8341bee32d261dde6aaa99e63f8`.
