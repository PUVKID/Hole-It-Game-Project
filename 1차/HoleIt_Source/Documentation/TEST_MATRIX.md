# 검증 상태표

상태 정의: **PASS**는 명시된 범위의 도구를 실제 실행해 통과, **FAIL**은 실제 검사 실패, **NOT RUN**은 미실행, **BLOCKED**는 실행 환경 부족입니다. 오프라인 검사의 PASS를 Unity 검사 PASS로 확장하지 않습니다.

| 검사 | 결과 | 근거 / 경계 |
|---|---|---|
| 프로젝트 루트·필수 파일·버전 | PASS | 실제 디렉터리/파일/버전 문자열 확인 |
| 메타 GUID | PASS | 374개 고유 GUID, 필수 파일/폴더 메타 존재 |
| Unity 텍스트 YAML 파싱 | PASS | 220파일, 2,410개 직렬화 객체. Unity 임포트 아님 |
| 외부/로컬 참조 | PASS | 2,529개 외부 및 4,803개 로컬 참조, 누락 0 |
| 씬/프리팹 계층 | PASS | GameObject 501개 소유/부모 연결 확인 |
| Main 직렬화 | PASS | 레벨 30참조, 선택 버튼 30개, 팝업 풀 8개, GameObject 215개 |
| 패키지 JSON/asmdef | PASS | 직접 패키지 13개, 잠금 항목 14개, asmdef 4개. UPM 해결 아님 |
| C# 어휘/구조 검사 | PASS | 52파일, 최상위 타입 55개, 구분자/일부 중복 및 Editor 분리 검사. 의미 분석 아님 |
| 메시 버퍼 | PASS | 메시 56개, 정점 27,440개, 삼각형 52,840개. 좌표/노멀/UV/Bounds/인덱스 검사 |
| PNG/WAV 파일 | PASS | PNG 55개, WAV 17개. 음원 청취/Unity 디코딩 아님 |
| Safe Area 수식 | PASS | 5화면/인셋 조합. 실제 렌더·터치 아님 |
| 30레벨 데이터 및 성장 | PASS | 테마 6개, 저장 배치 957개, 목표/경계/시작 공간/겹침/성장 경로 |
| 오류 데이터 감지 | PASS | 수량 부족, 경계 이탈, 겹침, 시작 막힘, 음수 시간, 데이터 누락, 성장 교착 7사례를 의도적으로 주입해 검출 |
| Python 구문·오프라인 생성기 보호장치 | PASS | AST 파싱 및 환경변수 미설정 실행 거부 확인. 전체 이식본 재생성 아님 |
| Unity 실행 래퍼 사전검사 | BLOCKED | 실제 bash 래퍼 exit 69; Unity 경로 없음. Unity 프로세스는 실행되지 않음 |
| Unity Package Manager 해결 | NOT RUN | Unity 없음 |
| Unity 전체 C# / Editor / Test 컴파일 | BLOCKED | Unity 및 독립 C# 컴파일러 없음 |
| 셰이더 컴파일 / 네이티브 에셋 임포트 | BLOCKED | Unity 없음 |
| Unity 프로젝트 검사 메뉴 | NOT RUN | 실행 가능한 Editor 없음 |
| Unity EditMode 테스트 | NOT RUN | 10일반 테스트 + 3점수 매개변수 사례 작성 |
| Unity PlayMode 테스트 | NOT RUN | 5개 테스트 작성 |
| 30레벨 실제 로드/완주/시간 밸런스 | NOT RUN | 데이터 검증만 수행 |
| 모바일 터치/노치/화면비/앱 복귀 | NOT RUN | 실기기 없음 |
| 효과음 청취 / 실제 3D 렌더 확인 | NOT RUN | 아이콘 모음만 오프라인으로 확인 |
| 장시간 실행 / Profiler / 60fps 측정 | NOT RUN | 측정 환경 없음 |
| Android APK / IL2CPP 빌드 | BLOCKED | Unity 및 Android 빌드 체인 없음, APK 미생성 |
| PowerShell 래퍼 실행 | NOT RUN | 작성만 완료 |
| 전체 오프라인 생성 이식본 재실행 | NOT RUN | 제출 에셋 보존을 위해 전체 재생성하지 않음 |

원시 기록: `STATIC_VALIDATION.json`, `ENVIRONMENT_REPORT.json`, `GENERATOR_GUARD_TEST.json`. ZIP 무결성·재압축 해제 검증은 최종 제출 시 별도 `HoleIt_ZIP_VALIDATION.json`에 기록합니다.
