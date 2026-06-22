# Repo Structure

## 목적

이 문서는 Project_Ascension 모노레포의 폴더 구조와 책임 경계를 정의한다.

---

## 최상위 구조

```
/
  apps/
    client-unity/
    api/
  packages/
    Domain/
    Contracts/
    Discovery/
    Items/
    Shared/
  docs/
  infra/
  tools/
```

---

## apps/client-unity

Unity 6 클라이언트. 상세 구조는 `unity-architecture.md` 참조.

담당:
* FPS/TPS 이동 및 카메라
* 양손 2슬롯 장비
* 전투 입력 및 피격
* 계약 UI
* 발견 알림 및 저널
* 월드 렌더링

---

## apps/api

ASP.NET Core 백엔드. 상세 구조는 `backend-architecture.md` 참조.

담당:
* 계약 생성/수주/완료
* 캐릭터 상태 저장
* 아이템 및 인벤토리
* 발견 기록 및 지식 생성
* 정착지 상태 (MVP 이후)
* 조직 시스템 (MVP 이후)

---

## packages

C# 클래스 라이브러리. Unity 클라이언트와 API 서버 양쪽에서 참조한다.

### packages/Domain

핵심 도메인 엔티티, 열거형, 인터페이스.

* Actor, Contract, Item, Discovery, Knowledge
* Region, Monster, Settlement, Organization
* 인터페이스: IContractRepository, IDiscoveryRepository 등

**규칙:** 외부 프레임워크 의존성 없음. 순수 C#.

---

### packages/Contracts

Unity 클라이언트와 API 서버가 공유하는 요청/응답 DTO.

```
Requests/
  AcceptContractRequest.cs
  RecordDiscoveryRequest.cs
  UpdateLoadoutRequest.cs
Responses/
  ContractResponse.cs
  DiscoveryResponse.cs
  CharacterResponse.cs
Enums/
  ContractKind.cs
  ContractStatus.cs
  DiscoveryType.cs
```

**규칙:** DTO만 포함. 비즈니스 로직 없음.

---

### packages/Discovery

발견 시스템 도메인 로직.

* DiscoveryCandidate 평가
* 발견 진행도 계산
* 최초 발견 판정 인터페이스

---

### packages/Items

아이템/장비 도메인 로직.

* Item, Equipment, Loadout 타입
* 양손 2슬롯 유효성 검사

---

### packages/Shared

범용 유틸리티.

* Result<T> 타입
* Error 타입
* ID 헬퍼
* 페이지네이션 타입

---

## infra

로컬 개발 환경.

```
infra/
  docker-compose.yml    (postgres, api)
  postgres/
    init.sql
```

---

## tools

개발 보조 도구. 초기에는 비워둔다.

---

## 언어 경계

| 영역 | 언어 |
|---|---|
| Unity 클라이언트 | C# |
| API 서버 | C# (ASP.NET Core) |
| 공유 패키지 | C# (.NET 클래스 라이브러리) |
| DB 마이그레이션 | EF Core (C#) |
| 인프라 설정 | Docker Compose, SQL |

---

## 패키지 참조 규칙

```
client-unity  →  packages/Contracts, packages/Domain (일부)
api           →  packages/Domain, packages/Contracts, packages/Discovery, packages/Items, packages/Shared
```

packages 간 순환 참조 금지. Domain은 다른 package를 참조하지 않는다.

---

## MVP 구현 순서

1. 솔루션 scaffold — 프로젝트 구조, 참조 관계 설정
2. `packages/Domain` — 핵심 엔티티 정의
3. `packages/Contracts` — DTO 정의
4. `apps/api` scaffold — health endpoint, DB 연결
5. `packages/Discovery`, `packages/Items` — 도메인 로직
6. API 엔드포인트 구현 — Contract, Character, Item, Discovery
7. Unity 클라이언트 — 패키지 참조 후 API 연동

---

## 금지 사항

초기부터 구현하지 않는다.

* 별도 게임 서버 프로세스
* Redis, 메시지 큐
* 마이크로서비스 분리
* 실시간 동기화 서버
