# Solution Scaffold Design — Phase 1

**Date:** 2026-06-22  
**Scope:** Steps 1~5 (Unity 제외)

---

## 목표

Project_Ascension C# 백엔드/서버 솔루션의 첫 번째 실행 가능한 scaffold를 생성한다.  
이 세션이 끝나면 `dotnet build`와 `dotnet test`가 통과해야 한다.

---

## 프로젝트 구조

```
ProjectAscension.sln
apps/
  api/ProjectAscension.Api/
    Controllers/
    Services/
    Data/
    Middleware/
    Program.cs
  game-server/ProjectAscension.GameServer/
    Program.cs
    GameLoop.cs
    SessionManager.cs
    ZoneInstance.cs
    Network/
    ApiReporter.cs
packages/
  Domain/ProjectAscension.Domain/
    Entities/
    Enums/
    Interfaces/
  Contracts/ProjectAscension.Contracts/
    Requests/
    Responses/
    Enums/
    GameMessages/
  GameSimulation/ProjectAscension.GameSimulation/
    Physics/
    Player/
    Combat/
    Monsters/
    Discovery/
  Shared/ProjectAscension.Shared/
```

---

## 참조 관계

| 프로젝트 | 참조 |
|---------|------|
| `GameServer` | `Domain`, `Contracts`, `GameSimulation` |
| `Api` | `Domain`, `Contracts`, `Shared` |
| `GameSimulation` | (없음) |
| `Contracts` | (없음) |
| `Domain` | (없음) |
| `Shared` | (없음) |

순환 참조 금지. Domain/Contracts는 외부 의존 없음.

---

## Step 1 — 솔루션 Scaffold

- `dotnet new sln -n ProjectAscension`
- 프로젝트 6개 생성 (모두 `net9.0`)
  - `Api`: `webapi`
  - `GameServer`: `console`
  - `Domain`, `Contracts`, `GameSimulation`, `Shared`: `classlib`
- `dotnet sln add` 로 솔루션에 등록
- `dotnet add reference` 로 참조 연결

---

## Step 2 — GameSimulation

타겟: BEPUphysics2 NuGet 추가 후 PlayerSimulation 구현.

```
Physics/PhysicsWorld.cs         — BEPUphysics2 Simulation 래퍼
Physics/CharacterBody.cs        — 캐릭터 물리 바디
Player/PlayerState.cs           — 서버 권위 상태 (위치, 속도, 입력 시퀀스)
Player/PlayerSimulation.cs      — 이동, 점프, 회피 판정
```

xUnit 테스트 프로젝트 `ProjectAscension.GameSimulation.Tests` 함께 생성.

---

## Step 3 — Domain

`domain-model.md` 기준 MVP 필수 엔티티:

**Entities:** Actor, Character, NPC, Contract, ContractMarketAccessPoint, Item, Equipment, Loadout, Region, Monster, MonsterSpecies, Discovery, DiscoveryCandidate, DiscoveryProgress

**Enums:** ContractKind, ContractStatus, ContractPurpose, DiscoveryType, ItemType, RegionType, MonsterTier, EquipmentType, SlotType

**Interfaces:** IContractRepository, IDiscoveryRepository, IItemRepository, ICharacterRepository

EF 어노테이션 사용 금지. 매핑은 Infrastructure에서 처리.

---

## Step 4 — Api

- ASP.NET Core 9 minimal API 설정
- EF Core + Npgsql 추가
- `AppDbContext`: MVP 엔티티 DbSet 등록
- `Configurations/`: IEntityTypeConfiguration<T> 파일별 분리
- Controllers: Characters, Contracts, Items, Loadouts, Discoveries
- Services: 각 도메인별 서비스 인터페이스 + 구현체
- `ExceptionMiddleware`: 전역 예외 처리
- `Result<T>` 패턴으로 성공/실패 반환
- Health check 엔드포인트: `GET /health`

---

## Step 5 — GameServer

- ENet-CSharp NuGet 추가
- MessagePack-CSharp NuGet 추가
- 기본 서버 루프: 20Hz 이동 틱, 64Hz 전투 틱
- `ENetTransport.cs`: 연결/수신/송신 래퍼
- `PacketHandler.cs`: PlayerInputMessage 수신 → GameSimulation 호출
- `ApiReporter.cs`: HTTP 클라이언트로 API 이벤트 보고
- `ZoneInstance.cs`: 단일 존 상태 보유

---

## 성공 기준

- [ ] `dotnet build` 전체 통과
- [ ] `dotnet test` 통과 (GameSimulation 기본 이동 테스트)
- [ ] `GET /health` 200 응답
- [ ] EF Core 첫 마이그레이션 생성 성공

---

## 참조 문서

- `docs/04-technical/repo-structure.md`
- `docs/04-technical/backend-architecture.md`
- `docs/04-technical/game-server-architecture.md`
- `docs/04-technical/domain-model.md`
- `docs/next-session-prompt.md`
