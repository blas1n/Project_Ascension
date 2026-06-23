# ADR 0001 — Unity C# 9 호환성과 6.5(InstanceID→EntityId) 패키지 버전

- 상태: 채택됨
- 날짜: 2026-06-23
- 맥락: Phase 1 (Player Controller). `packages/`의 공유 C# 패키지를 Unity 클라이언트가 **소스 수준**으로 직접 참조(UPM local package). 에디터는 Unity 6000.5.0f1(=Unity 6.5).

## 배경

서버 측 공유 패키지(Domain/Contracts/Shared/GameSimulation)는 .NET 9 + 최신 C#으로 작성됐다:

- file-scoped namespace (C# 10)
- struct에 `with` 식 (C# 10)
- `record` + `init` 접근자
- `ImplicitUsings` (암묵 using)
- `Nullable` 컨텍스트 전역 활성

그러나 Unity 6.5는 스크립트를 **C# 9**로 컴파일하며, asmdef 어셈블리에는 langversion / ImplicitUsings / nullable 전역 설정을 적용할 수단이 없다. DLL 빌드는 프로젝트 규칙상 금지(소스 공유 필수)다.

별개로, Unity 6.5는 `Object.GetInstanceID()`를 **error급으로 폐기**(EntityId로 이행, CS0619)했다. 이를 호출하는 구버전 서드파티 소스 패키지가 컴파일되지 않는다.

## 결정 1 — 공유 패키지를 C# 9 + Unity 양립으로 조정

서버(.NET 9)와 Unity(C# 9) **양쪽에서 컴파일되도록** 공유 소스를 맞춘다.

1. **block namespace로 통일.** 모든 file-scoped namespace를 `namespace X { }`로 변환. editorconfig에 namespace 스타일 규칙이 없어 `dotnet format`이 되돌리지 않는다. → 앞으로 공유 패키지는 block namespace 컨벤션.
2. **struct `with` 제거.** `Vector3`(struct)에 쓰던 `with`를 `new Vector3(...)` 명시 생성으로 교체(동작 동일). record `with`(`state with`, `MovementSettings with`)는 C# 9에서 유효하므로 유지.
3. **`IsExternalInit` 폴리필.** Unity의 .NET Standard 프로파일에 없는 `System.Runtime.CompilerServices.IsExternalInit`를 각 패키지에 `#if UNITY_5_3_OR_NEWER` 가드로 정의. record `init`이 Unity에서 컴파일된다.
4. **명시적 using.** 암묵 using에 의존하던 파일에 `using System;` 등을 명시. 서버는 ImplicitUsings가 켜져 있어 중복일 뿐 무해.
5. **`#nullable enable` 디렉티브.** 참조형 nullable 주석을 쓰는 파일 상단에 명시. 엔티티는 non-nullable string을 초기화하고 있어 CS8618이 발생하지 않는다.
6. **MessagePack은 Unity 전용 shim.** `GameMessages`의 MessagePack 어트리뷰트를 `#if UNITY_5_3_OR_NEWER`에서 no-op 스텁으로 대체. Unity는 MessagePack 코드 생성(IL2CPP/AOT) 셋업 없이 Contracts를 컴파일. 실시간 네트워킹 단계에서 실제 패키지로 교체.

## 결정 2 — 6.5 호환 패키지 버전 사용 (기술 스택 유지)

설계 문서가 명시한 DI(VContainer)와 카메라(Cinemachine)는 **폐기하지 않는다.** 두 패키지 모두 6.5의 InstanceID 폐기에 대응한 버전이 이미 릴리스됐으므로 버전만 올린다.

- **VContainer 1.16.9 → 1.18.0.** 릴리스 노트 "resolve GetInstanceID deprecation in Unity 6.4+". OpenUPM(`jp.hadashikick`)에서 받는다.
- **Cinemachine 3.1.3 → 3.1.7.** 3.1.6에서 "Converted code using InstanceID references and API to EntityID", 3.1.7(2026-06-08)이 최신.

교훈: 6.5는 신규/베타 스트림이라 구버전 서드파티 소스가 광범위하게 깨진다. 기술 스택을 버리기 전에 **호환 버전부터 확인**한다.

## 영향

- 서버 빌드/CI: 영향 없음(추가 using/디렉티브는 무해, 폴리필·shim은 Unity 가드).
- Unity 클라이언트: 4개 공유 패키지 전부 소스 수준 컴파일 + VContainer/Cinemachine 정상.
- 컨벤션: 공유 패키지는 block namespace + 명시적 using 유지.

## 참고

- Unity Manual — Migrate from InstanceID to EntityId (6000.5)
- VContainer v1.18.0 릴리스 노트
- Cinemachine 3.1 changelog (3.1.6 / 3.1.7)
