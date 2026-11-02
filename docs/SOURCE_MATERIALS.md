<div align="center">

# 🗃️ 원본 제품자료와 압축 소스

기존 모든 차수는 [첫 화면 차수표](../README.md)에서 탐색합니다. 이번에는 코드·제품 설정·사용법·설계·실험 기록·자체 제작 리소스·라이선스를 원본 경로와 함께 보강했습니다. 아래는 기존 공개 기준과 바이트가 다른 제품자료 16342개 목록입니다. 전체 차수의 공개 파일은 각 차수 비교 명세에 별도로 있습니다.

**기술 분류:** 제품 원본 보관 · 코드·설정·사용 문서

[핵심 기능](#features) · [사용 흐름](#usage) · [자료 구성](#structure) · [준비와 실행](#setup) · [결과 읽기](#results) · [직전 변경](#changes)

</div>

[🏠 프로젝트](../README.md) · [🗂️ 전체 차수](REVISION_INDEX.md) · [📁 원본 자료](../%EB%B3%B4%EC%A1%B4%EC%9E%90%EB%A3%8C/%EC%9E%91%EC%97%85%EC%86%8C%EC%8A%A4)

## 🎯 목적과 이 차수의 역할

기존 모든 차수는 [첫 화면 차수표](../README.md)에서 탐색합니다. 이번에는 코드·제품 설정·사용법·설계·실험 기록·자체 제작 리소스·라이선스를 원본 경로와 함께 보강했습니다. 아래는 기존 공개 기준과 바이트가 다른 제품자료 16342개 목록입니다. 전체 차수의 공개 파일은 각 차수 비교 명세에 별도로 있습니다.

<a name="features"></a>

## ✨ 핵심 기능과 보존 범위

| 항목 | 이 차수에서 확인할 내용 |
|---|---|
| 1 | 기존 모든 차수는 [첫 화면 차수표](../README.md)에서 탐색합니다. 이번에는 코드·제품 설정·사용법·설계·실험 기록·자체 제작 리소스·라이선스를 원본 경로와 함께 보강했습니다. 아래는 기존 공개 기준과 바이트가 다른 제품자료 16342개 목록입니다. 전체 차수의 공개 파일은 각 차수 비교 명세에 별도로 있습니다. |
| 2 | ZIP은 중앙 목록과 경로 탈출·링크·암호화·크기 경계를 검사하고 소스 멤버를 실제 읽었습니다. 기존 공개 파일과 바이트 또는 BOM·개행만 같은 멤버는 대응 링크로 남기고, 고유 소스는 `압축원본소스`에 보존했습니다. 실행 바이너리·캐시·실제 인증 자료·실제 운영 DB·실거래 계좌 자료·원작 추출 표현 자산은 제외했습니다. 각 ZIP의 모든 멤버 판단은 비공개 검토 명세에, 공개 대응 멤버는 아래 JSON에 있습니다. |

## 🔄 입력·처리·출력

| 단계 | 내용 |
|---|---|
| 📥 입력 | 기존 차수에서 개별 보존한 작업 소스와 문서 |
| ⚙️ 처리 | 원래 경로·출처·파일 대응을 찾아 읽기 |
| 📤 출력 | 기능 소스·개발 도구·설명 문서의 보존 목록 |

<a name="usage"></a>

## 🖐️ 화면과 사용 흐름

1. 보존 목록에서 원래 차수·경로를 찾습니다.
2. 연결된 코드와 해당 차수의 설명을 함께 읽습니다.
3. 보조 도구와 제품 실행 코드를 구분하고 필요한 입력을 확인합니다.

<a name="structure"></a>

## 📁 실제 자료 구성

| 경로 | 자료 종류 |
|---|---|
| [9차/](../%EB%B3%B4%EC%A1%B4%EC%9E%90%EB%A3%8C/%EC%9E%91%EC%97%85%EC%86%8C%EC%8A%A4/9%EC%B0%A8) | 하위 자료 폴더 |

## 🧩 주요 모듈과 코드 읽기

| 파일 | 원문에 정의된 역할·구성 |
|---|---|
| [9차/work/AndroidBuild/Assets/Scripts/Core/GameManager.cs](../%EB%B3%B4%EC%A1%B4%EC%9E%90%EB%A3%8C/%EC%9E%91%EC%97%85%EC%86%8C%EC%8A%A4/9%EC%B0%A8/work/AndroidBuild/Assets/Scripts/Core/GameManager.cs) | 정의: `GameManager` |
| [9차/work/add_reference_arena.py](../%EB%B3%B4%EC%A1%B4%EC%9E%90%EB%A3%8C/%EC%9E%91%EC%97%85%EC%86%8C%EC%8A%A4/9%EC%B0%A8/work/add_reference_arena.py) | 소스 본문과 설정을 함께 읽는 구성요소 |
| [9차/work/debug_reference.py](../%EB%B3%B4%EC%A1%B4%EC%9E%90%EB%A3%8C/%EC%9E%91%EC%97%85%EC%86%8C%EC%8A%A4/9%EC%B0%A8/work/debug_reference.py) | 소스 본문과 설정을 함께 읽는 구성요소 |
| [9차/work/export_reference5_audio.py](../%EB%B3%B4%EC%A1%B4%EC%9E%90%EB%A3%8C/%EC%9E%91%EC%97%85%EC%86%8C%EC%8A%A4/9%EC%B0%A8/work/export_reference5_audio.py) | 소스 본문과 설정을 함께 읽는 구성요소 |
| [9차/work/export_reference5_content.py](../%EB%B3%B4%EC%A1%B4%EC%9E%90%EB%A3%8C/%EC%9E%91%EC%97%85%EC%86%8C%EC%8A%A4/9%EC%B0%A8/work/export_reference5_content.py) | 정의: `resolve`, `vec`, `texture`, `material`, `mesh`, `walk` |
| [9차/work/export_reference5_materials.py](../%EB%B3%B4%EC%A1%B4%EC%9E%90%EB%A3%8C/%EC%9E%91%EC%97%85%EC%86%8C%EC%8A%A4/9%EC%B0%A8/work/export_reference5_materials.py) | 소스 본문과 설정을 함께 읽는 구성요소 |
| [9차/work/export_reference5_style.py](../%EB%B3%B4%EC%A1%B4%EC%9E%90%EB%A3%8C/%EC%9E%91%EC%97%85%EC%86%8C%EC%8A%A4/9%EC%B0%A8/work/export_reference5_style.py) | 소스 본문과 설정을 함께 읽는 구성요소 |
| [9차/work/export_reference_content.py](../%EB%B3%B4%EC%A1%B4%EC%9E%90%EB%A3%8C/%EC%9E%91%EC%97%85%EC%86%8C%EC%8A%A4/9%EC%B0%A8/work/export_reference_content.py) | 정의: `resolve`, `vec`, `texture`, `material`, `mesh`, `walk` |
| [9차/work/export_reference_style.py](../%EB%B3%B4%EC%A1%B4%EC%9E%90%EB%A3%8C/%EC%9E%91%EC%97%85%EC%86%8C%EC%8A%A4/9%EC%B0%A8/work/export_reference_style.py) | 소스 본문과 설정을 함께 읽는 구성요소 |

<a name="setup"></a>

## 🚀 설치 환경과 실행·사용법

이 묶음은 서로 다른 시점의 작업 자료를 원래 경로로 보관합니다. 한 앱의 완전한 같은 시점 설치 환경으로 합치지 않습니다.

아래 원본 목록에서 필요한 파일을 고른 뒤 해당 차수의 실행 안내와 대조합니다. 검증·캐시 정리·복구·배포 도구에는 원래 경로와 기존 증거가 필요할 수 있으므로 일괄 실행하지 않습니다.

<a name="results"></a>

## 📊 처리 결과와 출력 읽기

원래 위치와 파일 크기·SHA-256으로 보존 대응을 확인합니다. 같은 이름의 파일도 시점과 경로가 다르면 다른 자료입니다.

## 🛠️ 제약과 문제 해결

| 상황 | 확인할 내용 |
|---|---|
| 한 폴더로 실행되지 않음 | 이 묶음은 여러 작업 시점의 보존 자료이며 단일 배포 제품이 아닙니다. |
| 이전 파일과 내용이 다름 | 원래 차수·경로·해시와 전체 비교를 함께 확인합니다. |

<a name="changes"></a>

## 🔍 직전 자료와 전체 비교

이 자료의 보존 범위와 기준 파일을 연결합니다.

[📄 전체 비교·자료 범위](SOURCE_MATERIALS.md)

추가·수정·삭제, 주석·공백·개행과 바이너리 크기·해시는 연결된 원래 비교 자료에서 확인합니다.

<details>
<summary>📚 파일별 비교·크기·해시 자세히 보기</summary>

| 차수·위치 | 추가·변경 자료 수 |
|---|---:|
| 1차 | 733 |
| 2차 | 2086 |
| 3차 | 2104 |
| 4차 | 2104 |
| 5차 | 2139 |
| 6차 | 2261 |
| 7차 | 2575 |
| 8차 | 2245 |
| 보존자료 | 95 |

[파일 전체 명세](source-materials/files.json) · [공개 ZIP 멤버 대응](source-materials/archive-members.json)

</details>

[🏠 프로젝트](../README.md) · [🗂️ 전체 차수](REVISION_INDEX.md) · [📁 원본 자료](../%EB%B3%B4%EC%A1%B4%EC%9E%90%EB%A3%8C/%EC%9E%91%EC%97%85%EC%86%8C%EC%8A%A4)
