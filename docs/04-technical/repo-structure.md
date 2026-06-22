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

역할: **렌더링 + 입력만 담당.** 게임 로직은 `packages/GameSimulation/`에 있다.

담당:
* FPS/TPS 렌더링 및 카메라
* 플레이어 입력 수집
* 게임 서버 통신 (ENet-CSharp)
* UI (계약, 발견, 인벤토리)
* 클라이언트 예측 및 서버 보정

---

## apps/game-server

순수 C# 콘솔 앱. Unity 의존성 없음. 상세 구조는 `game-server-architecture.md` 참조.

담당:
* 플레이어 이동/위치 권위
* 전투 히트 판정
* 몬스터 AI 실행
* 발견 후보 감지
* 영속화 이벤트 → API 보고

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

### packages/GameSimulation

순수 C# 게임 시뮬레이션 로직. `dotnet test` 가능.

* PlayerSimulation (이동, 점프, 회피)
* CombatSimulation (히트 판정, 데미지)
* MonsterSimulation (AI 상태 머신)
* PhysicsWorld (BEPUphysics2 래퍼)
* BehaviorCounter (발견 후보 행동 추적)

Unity 클라이언트와 게임 서버가 동일한 로직을 참조한다. 클라이언트 예측과 서버 보정의 기반이 된다.

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
GameMessages/
  PlayerInputMessage.cs
  WorldStateMessage.cs
  GameEventMessage.cs
```

**규칙:** DTO만 포함. 비즈니스 로직 없음.

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

| 영역 | 언어 | Unity 의존 |
|---|---|---|
| Unity 클라이언트 | C# | ✓ (렌더링/입력) |
| 게임 서버 | C# (콘솔 앱) | ✗ |
| API 서버 | C# (ASP.NET Core) | ✗ |
| 공유 패키지 | C# (.NET 클래스 라이브러리) | ✗ |
| DB 마이그레이션 | EF Core (C#) | ✗ |

---

## 패키지 참조 규칙

```
client-unity   →  Contracts, GameSimulation
game-server    →  Domain, Contracts, GameSimulation
api            →  Domain, Contracts, Shared
```

packages 간 순환 참조 금지. Domain과 Contracts는 다른 package를 참조하지 않는다.

Discovery/Items 도메인 로직은 `packages/Domain/`에 포함한다. VS 규모에서 별도 패키지로 분리하지 않는다.
Application/Infrastructure 레이어는 `apps/api/` 내부 폴더로 유지한다.

---

## MVP 구현 순서

1. 솔루션 scaffold — 프로젝트 구조, 참조 관계 설정
2. `packages/Domain` — 핵심 엔티티 정의
3. `packages/Contracts` — DTO 및 GameMessages 정의
4. `packages/GameSimulation` — PlayerSimulation, CombatSimulation, MonsterSimulation
5. `apps/api` scaffold — health endpoint, DB 연결, 엔드포인트 구현
6. `apps/game-server` scaffold — ENet 서버 루프, GameSimulation 연결
7. Unity 클라이언트 — 렌더링 셸, ENet 클라이언트, API 연동

---

## 금지 사항

초기부터 구현하지 않는다.

* 별도 게임 서버 프로세스
* Redis, 메시지 큐
* 마이크로서비스 분리
* 실시간 동기화 서버
