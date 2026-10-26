# 오프라인 절차 생성 도구

이 폴더는 실제 제출 메시·아이콘·기초 캠페인·WAV를 작성한 Python 코드의 이식본입니다. Unity를 흉내 내어 컴파일하거나 실행하지 않습니다. **프로젝트를 열고 플레이하는 데 Python은 필요 없습니다.** 일반적인 편집은 Unity Inspector와 HoleIt 메뉴를 사용합니다.

`generate_models.py`는 specs.json의 51종 카탈로그에서 메시, vertex-color 머티리얼, 프리팹, EdibleData, 128×128 아이콘을 생성합니다. `campaign.py`는 30개 기본 레벨/저장 배치, 6개 테마, 성장/게임 설정 및 17개 WAV를 생성합니다. `unity_assets.py`는 Unity 텍스트 직렬화 형식을 작성하는 유틸리티입니다. 최초 작성 때 실행된 로직을 프로젝트 상대경로로 바꿨으며, 이 이식본의 전체 재생성 실행은 별도 확인하지 않았습니다. 구문 검사와 보호장치 검사는 수행했습니다.

이 도구는 Main 씬이나 완성 UI를 새로 작성하지 않습니다. 이미 포함된 `Assets/Scenes/Main.unity`와 `Interface.prefab`을 사용합니다. 씬 재구성은 기존 사용자 씬을 백업하는 C# Editor 빌더에서 수행합니다.

## 보호장치
기본 실행은 거부됩니다. 사용자 조정값을 덮어쓸 수 있으므로 **원본 프로젝트와 다른 백업 복사본**에서만 다음 순서로 사용합니다.

```sh
python -m pip install -r Tools/OfflineAuthoring/requirements-authoring.txt
export HOLEIT_ALLOW_ASSET_REGEN=1
python Tools/OfflineAuthoring/generate_models.py
python Tools/OfflineAuthoring/campaign.py
python Tools/validate_static.py
```

Windows PowerShell에서는 `export` 대신 `$env:HOLEIT_ALLOW_ASSET_REGEN="1"`을 사용합니다. 패키지 버전은 제작 환경에서 실제 확인한 버전이며 Unity 패키지가 아닙니다. 생성 메시를 직접 볼 때는 반드시 Unity 임포트/렌더 검증을 별도로 수행해야 합니다.
