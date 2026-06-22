# Backend Architecture

## 기본 결정

* **프레임워크:** ASP.NET Core 8
* **ORM:** EF Core (복잡 쿼리는 Dapper 혼용)
* **DB:** PostgreSQL
* **패턴:** Clean Architecture (Controller → Application → Domain → Infrastructure)

---

## 프로젝트 구조

Application과 Infrastructure는 별도 패키지가 아니라 `apps/api/` 내부 폴더로 유지한다. 공유가 필요한 코드만 `packages/`에 둔다.

```
apps/api/
  ProjectAscension.Api/
    Controllers/
    Services/          — Application 레이어 (API 내부, 서비스/유스케이스)
    Data/              — Infrastructure 레이어 (EF Core, 리포지토리, 마이그레이션)
    Middleware/
    Program.cs

packages/
  Domain/
    ProjectAscension.Domain/      — 엔티티, 열거형, 인터페이스 (API + GameServer 공유)
  Contracts/
    ProjectAscension.Contracts/   — DTO + GameMessages (Unity + API + GameServer 공유)
  GameSimulation/
    ProjectAscension.GameSimulation/ — 순수 C# 게임 로직 (Unity + GameServer 공유)
  Shared/
    ProjectAscension.Shared/      — 공통 유틸리티
```

---

## 참조 관계

```
Api          →  Domain, Contracts, Shared
GameServer   →  Domain, Contracts, GameSimulation
client-unity →  Contracts, GameSimulation
```

Domain과 Contracts는 외부 의존성이 없다. 순환 참조 금지.

---

## 타겟 프레임워크

`net9.0` (.NET SDK 9 기준)

---

## API 엔드포인트 (Vertical Slice)

### Characters

```
GET  /api/characters/{id}              — 캐릭터 상태 조회
```

### Contracts

```
GET  /api/contracts?regionId={id}      — 지역 계약 목록
POST /api/contracts/{id}/accept        — 계약 수주
POST /api/contracts/{id}/progress      — 진행 보고
POST /api/contracts/{id}/complete      — 계약 완료
```

### Items

```
GET  /api/items?actorId={id}           — 인벤토리 조회
```

### Loadout

```
GET  /api/loadouts/{actorId}           — 현재 장비 조회
PUT  /api/loadouts/{actorId}           — 장비 변경
```

### Discoveries

```
POST /api/discoveries                  — 발견 기록
GET  /api/discoveries?actorId={id}     — 발견 저널 조회
```

---

## 프로젝트 내부 구조

### ProjectAscension.Api

```
Controllers/
  ContractsController.cs
  CharactersController.cs
  DiscoveriesController.cs
  ItemsController.cs
  LoadoutsController.cs
Middleware/
  ExceptionMiddleware.cs
Program.cs
appsettings.json
appsettings.Development.json  (gitignore)
```

### ProjectAscension.Application

```
Services/
  ContractService.cs
  DiscoveryService.cs
  ItemService.cs
  CharacterService.cs
  LoadoutService.cs
Interfaces/
  IContractService.cs
  IDiscoveryService.cs
  ...
```

### ProjectAscension.Infrastructure

```
Data/
  AppDbContext.cs
  Migrations/
  Configurations/       (EF 엔티티 설정)
    ContractConfiguration.cs
    DiscoveryConfiguration.cs
    ...
Repositories/
  ContractRepository.cs
  DiscoveryRepository.cs
  ItemRepository.cs
  CharacterRepository.cs
```

### ProjectAscension.Domain

```
Entities/
  Actor.cs
  Contract.cs
  Item.cs
  Equipment.cs
  Loadout.cs
  Discovery.cs
  DiscoveryCandidate.cs
  DiscoveryProgress.cs
  Knowledge.cs
  Region.cs
  Monster.cs
  MonsterSpecies.cs
  Character.cs
  NPC.cs
Enums/
  ContractKind.cs
  ContractStatus.cs
  ContractPurpose.cs
  DiscoveryType.cs
  ItemType.cs
  RegionType.cs
  MonsterTier.cs
Interfaces/
  IContractRepository.cs
  IDiscoveryRepository.cs
  IItemRepository.cs
  ICharacterRepository.cs
```

---

## EF Core 규칙

* 엔티티는 `Domain`에 정의한다. EF 어노테이션을 사용하지 않는다.
* 매핑은 `Infrastructure/Configurations/`의 `IEntityTypeConfiguration<T>`로 분리한다.
* 마이그레이션은 `Infrastructure` 프로젝트에서 관리한다.
* 복잡한 집계 쿼리는 Dapper로 직접 SQL을 작성한다.

---

## 이벤트 처리

Vertical Slice에서는 도메인 이벤트를 인메모리로 처리한다. 서비스 메서드 완료 시 `Application` 레이어에서 이벤트를 발행하고, 동일 요청 내에서 핸들러가 처리한다.

메시지 큐(RabbitMQ 등)는 MMO 단계에서 도입한다.

---

## 설계 규칙

* 컨트롤러는 얇게 유지한다. 로직은 Application 서비스에 있다.
* 리포지토리 인터페이스는 Domain에 정의한다. 구현은 Infrastructure에 있다.
* `packages/Contracts` DTO를 컨트롤러 입출력으로 직접 사용한다. API 전용 모델을 별도로 만들지 않는다.
* 모든 엔드포인트는 `Result<T>` 패턴으로 성공/실패를 반환한다.

---

## Vertical Slice 범위

구현:
* Character, Actor, Contract, Item, Equipment, Loadout
* Region, Monster, MonsterSpecies
* Discovery, DiscoveryCandidate, DiscoveryProgress
* 위 엔티티에 해당하는 EF Core 설정 및 마이그레이션
* 위 엔드포인트 전체

미구현:
* Knowledge, KnowledgeLineage, KnowledgeLicense
* Organization, Settlement, Infrastructure
* WorldWillEvent, ReputationEvent
* 인증/계정 시스템 (로컬 개발에서는 actorId 직접 전달)
