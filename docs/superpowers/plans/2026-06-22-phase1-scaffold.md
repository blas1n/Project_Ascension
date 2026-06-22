# Phase 1 — Solution Scaffold Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `dotnet build` 및 `dotnet test` 가 통과하는 ProjectAscension 백엔드/서버 솔루션 scaffold를 완성한다.

**Architecture:** 순수 C# 패키지 4개(Domain, Contracts, GameSimulation, Shared) + 앱 2개(Api, GameServer)로 구성. 순환 참조 없음. Api는 Domain/Contracts/Shared 참조. GameServer는 Domain/Contracts/GameSimulation 참조.

**Tech Stack:** .NET 9.0, ASP.NET Core 9, EF Core 9 + Npgsql, BepuPhysics 2.4, ENet-CSharp 2.4.3, MessagePack 3.1, xUnit 2.9

---

## 파일 맵

```
ProjectAscension.sln

packages/Shared/ProjectAscension.Shared/
  ProjectAscension.Shared.csproj
  Result.cs
  Error.cs

packages/Domain/ProjectAscension.Domain/
  ProjectAscension.Domain.csproj
  Enums/ContractKind.cs
  Enums/ContractStatus.cs
  Enums/ContractPurpose.cs
  Enums/DiscoveryType.cs
  Enums/ItemType.cs
  Enums/RegionType.cs
  Enums/MonsterTier.cs
  Enums/EquipmentType.cs
  Enums/SlotType.cs
  Entities/Actor.cs
  Entities/Character.cs
  Entities/NPC.cs
  Entities/Contract.cs
  Entities/ContractMarketAccessPoint.cs
  Entities/Item.cs
  Entities/Equipment.cs
  Entities/Loadout.cs
  Entities/Region.cs
  Entities/Monster.cs
  Entities/MonsterSpecies.cs
  Entities/Discovery.cs
  Entities/DiscoveryCandidate.cs
  Entities/DiscoveryProgress.cs
  Interfaces/IContractRepository.cs
  Interfaces/IDiscoveryRepository.cs
  Interfaces/IItemRepository.cs
  Interfaces/ICharacterRepository.cs

packages/Contracts/ProjectAscension.Contracts/
  ProjectAscension.Contracts.csproj
  Enums/ContractKind.cs
  Enums/ContractStatus.cs
  Enums/DiscoveryType.cs
  Requests/AcceptContractRequest.cs
  Requests/RecordDiscoveryRequest.cs
  Requests/UpdateLoadoutRequest.cs
  Responses/ContractResponse.cs
  Responses/DiscoveryResponse.cs
  Responses/CharacterResponse.cs
  GameMessages/PlayerInputMessage.cs
  GameMessages/WorldStateMessage.cs
  GameMessages/GameEventMessage.cs

packages/GameSimulation/ProjectAscension.GameSimulation/
  ProjectAscension.GameSimulation.csproj
  Player/PlayerInput.cs
  Player/PlayerState.cs
  Player/PlayerSimulation.cs
  Physics/PhysicsWorld.cs

packages/GameSimulation/ProjectAscension.GameSimulation.Tests/
  ProjectAscension.GameSimulation.Tests.csproj
  Player/PlayerSimulationTests.cs

apps/api/ProjectAscension.Api/
  ProjectAscension.Api.csproj
  Program.cs
  appsettings.json
  appsettings.Development.json
  Middleware/ExceptionMiddleware.cs
  Data/AppDbContext.cs
  Data/Configurations/ActorConfiguration.cs
  Data/Configurations/CharacterConfiguration.cs
  Data/Configurations/ContractConfiguration.cs
  Data/Configurations/ItemConfiguration.cs
  Data/Configurations/EquipmentConfiguration.cs
  Data/Configurations/LoadoutConfiguration.cs
  Data/Configurations/RegionConfiguration.cs
  Data/Configurations/MonsterSpeciesConfiguration.cs
  Data/Configurations/MonsterConfiguration.cs
  Data/Configurations/DiscoveryCandidateConfiguration.cs
  Data/Configurations/DiscoveryConfiguration.cs
  Data/Configurations/DiscoveryProgressConfiguration.cs
  Data/Repositories/ContractRepository.cs
  Data/Repositories/DiscoveryRepository.cs
  Data/Repositories/ItemRepository.cs
  Data/Repositories/CharacterRepository.cs
  Services/ICharacterService.cs
  Services/CharacterService.cs
  Services/IContractService.cs
  Services/ContractService.cs
  Services/IDiscoveryService.cs
  Services/DiscoveryService.cs
  Services/IItemService.cs
  Services/ItemService.cs
  Services/ILoadoutService.cs
  Services/LoadoutService.cs
  Controllers/CharactersController.cs
  Controllers/ContractsController.cs
  Controllers/DiscoveriesController.cs
  Controllers/ItemsController.cs
  Controllers/LoadoutsController.cs

apps/game-server/ProjectAscension.GameServer/
  ProjectAscension.GameServer.csproj
  Program.cs
  GameLoop.cs
  SessionManager.cs
  ZoneInstance.cs
  Network/ENetTransport.cs
  Network/PacketHandler.cs
  Network/PacketSender.cs
  ApiReporter.cs
```

---

## Task 1: 솔루션 및 프로젝트 Scaffold

**Files:**
- Create: `ProjectAscension.sln`
- Create: 모든 `.csproj` 파일 (6개 + 1 test)

- [ ] **Step 1: 솔루션 및 프로젝트 생성**

모노레포 루트에서 실행:

```powershell
dotnet new sln -n ProjectAscension

# 패키지 (classlib)
dotnet new classlib -n ProjectAscension.Shared   -o packages/Shared/ProjectAscension.Shared   -f net9.0
dotnet new classlib -n ProjectAscension.Domain   -o packages/Domain/ProjectAscension.Domain   -f net9.0
dotnet new classlib -n ProjectAscension.Contracts -o packages/Contracts/ProjectAscension.Contracts -f net9.0
dotnet new classlib -n ProjectAscension.GameSimulation -o packages/GameSimulation/ProjectAscension.GameSimulation -f net9.0

# 테스트 프로젝트
dotnet new xunit -n ProjectAscension.GameSimulation.Tests -o packages/GameSimulation/ProjectAscension.GameSimulation.Tests -f net9.0

# 앱
dotnet new webapi -n ProjectAscension.Api -o apps/api/ProjectAscension.Api -f net9.0 --no-openapi
dotnet new console -n ProjectAscension.GameServer -o apps/game-server/ProjectAscension.GameServer -f net9.0
```

- [ ] **Step 2: 솔루션에 프로젝트 등록**

```powershell
dotnet sln add packages/Shared/ProjectAscension.Shared/ProjectAscension.Shared.csproj
dotnet sln add packages/Domain/ProjectAscension.Domain/ProjectAscension.Domain.csproj
dotnet sln add packages/Contracts/ProjectAscension.Contracts/ProjectAscension.Contracts.csproj
dotnet sln add packages/GameSimulation/ProjectAscension.GameSimulation/ProjectAscension.GameSimulation.csproj
dotnet sln add packages/GameSimulation/ProjectAscension.GameSimulation.Tests/ProjectAscension.GameSimulation.Tests.csproj
dotnet sln add apps/api/ProjectAscension.Api/ProjectAscension.Api.csproj
dotnet sln add apps/game-server/ProjectAscension.GameServer/ProjectAscension.GameServer.csproj
```

- [ ] **Step 3: 프로젝트 참조 연결**

```powershell
# Api → Domain, Contracts, Shared
dotnet add apps/api/ProjectAscension.Api/ProjectAscension.Api.csproj reference packages/Domain/ProjectAscension.Domain/ProjectAscension.Domain.csproj
dotnet add apps/api/ProjectAscension.Api/ProjectAscension.Api.csproj reference packages/Contracts/ProjectAscension.Contracts/ProjectAscension.Contracts.csproj
dotnet add apps/api/ProjectAscension.Api/ProjectAscension.Api.csproj reference packages/Shared/ProjectAscension.Shared/ProjectAscension.Shared.csproj

# GameServer → Domain, Contracts, GameSimulation
dotnet add apps/game-server/ProjectAscension.GameServer/ProjectAscension.GameServer.csproj reference packages/Domain/ProjectAscension.Domain/ProjectAscension.Domain.csproj
dotnet add apps/game-server/ProjectAscension.GameServer/ProjectAscension.GameServer.csproj reference packages/Contracts/ProjectAscension.Contracts/ProjectAscension.Contracts.csproj
dotnet add apps/game-server/ProjectAscension.GameServer/ProjectAscension.GameServer.csproj reference packages/GameSimulation/ProjectAscension.GameSimulation/ProjectAscension.GameSimulation.csproj

# GameSimulation.Tests → GameSimulation
dotnet add packages/GameSimulation/ProjectAscension.GameSimulation.Tests/ProjectAscension.GameSimulation.Tests.csproj reference packages/GameSimulation/ProjectAscension.GameSimulation/ProjectAscension.GameSimulation.csproj
```

- [ ] **Step 4: NuGet 패키지 추가**

```powershell
# GameSimulation — BepuPhysics
dotnet add packages/GameSimulation/ProjectAscension.GameSimulation/ProjectAscension.GameSimulation.csproj package BepuPhysics --version 2.4.0

# Contracts — MessagePack (GameMessages 직렬화)
dotnet add packages/Contracts/ProjectAscension.Contracts/ProjectAscension.Contracts.csproj package MessagePack --version 3.1.3

# GameServer — ENet-CSharp, MessagePack
dotnet add apps/game-server/ProjectAscension.GameServer/ProjectAscension.GameServer.csproj package ENet-CSharp --version 2.4.3
dotnet add apps/game-server/ProjectAscension.GameServer/ProjectAscension.GameServer.csproj package MessagePack --version 3.1.3

# Api — EF Core + Npgsql
dotnet add apps/api/ProjectAscension.Api/ProjectAscension.Api.csproj package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.0.4
dotnet add apps/api/ProjectAscension.Api/ProjectAscension.Api.csproj package Microsoft.EntityFrameworkCore.Design --version 9.0.4
```

- [ ] **Step 5: 기본 클래스 파일 삭제 (템플릿 생성 파일 제거)**

```powershell
Remove-Item packages/Shared/ProjectAscension.Shared/Class1.cs -ErrorAction SilentlyContinue
Remove-Item packages/Domain/ProjectAscension.Domain/Class1.cs -ErrorAction SilentlyContinue
Remove-Item packages/Contracts/ProjectAscension.Contracts/Class1.cs -ErrorAction SilentlyContinue
Remove-Item packages/GameSimulation/ProjectAscension.GameSimulation/Class1.cs -ErrorAction SilentlyContinue
Remove-Item apps/api/ProjectAscension.Api/WeatherForecast.cs -ErrorAction SilentlyContinue
Remove-Item apps/api/ProjectAscension.Api/Controllers/WeatherForecastController.cs -ErrorAction SilentlyContinue
Remove-Item packages/GameSimulation/ProjectAscension.GameSimulation.Tests/UnitTest1.cs -ErrorAction SilentlyContinue
```

- [ ] **Step 6: GameSimulation.csproj에 AllowUnsafeBlocks 추가 (BepuPhysics 필요)**

`packages/GameSimulation/ProjectAscension.GameSimulation/ProjectAscension.GameSimulation.csproj`를 열어 `<PropertyGroup>`에 추가:

```xml
<AllowUnsafeBlocks>true</AllowUnsafeBlocks>
```

최종 파일:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="BepuPhysics" Version="2.4.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 7: 빌드 확인**

```powershell
dotnet build
```

Expected: `Build succeeded.` (경고 있어도 OK, 오류 없어야 함)

- [ ] **Step 8: Commit**

```powershell
git add -A
git commit -m "chore: scaffold solution with all projects and references"
```

---

## Task 2: Shared 패키지

**Files:**
- Create: `packages/Shared/ProjectAscension.Shared/Result.cs`
- Create: `packages/Shared/ProjectAscension.Shared/Error.cs`

- [ ] **Step 1: Error.cs 작성**

```csharp
// packages/Shared/ProjectAscension.Shared/Error.cs
namespace ProjectAscension.Shared;

public record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NotFound = new("NOT_FOUND", "Resource not found.");
    public static readonly Error Conflict = new("CONFLICT", "Resource already exists.");
    public static readonly Error Invalid = new("INVALID", "Invalid request.");
}
```

- [ ] **Step 2: Result.cs 작성**

```csharp
// packages/Shared/ProjectAscension.Shared/Result.cs
namespace ProjectAscension.Shared;

public class Result<T>
{
    public T? Value { get; }
    public Error Error { get; }
    public bool IsSuccess => Error == Error.None;

    private Result(T value) { Value = value; Error = Error.None; }
    private Result(Error error) { Error = error; }

    public static Result<T> Ok(T value) => new(value);
    public static Result<T> Fail(Error error) => new(error);
}
```

- [ ] **Step 3: 빌드 확인**

```powershell
dotnet build packages/Shared/ProjectAscension.Shared/ProjectAscension.Shared.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```powershell
git add packages/Shared/
git commit -m "feat: add Shared package with Result<T> and Error types"
```

---

## Task 3: Domain Enums

**Files:** `packages/Domain/ProjectAscension.Domain/Enums/` 하위 9개 파일

- [ ] **Step 1: Enums 디렉토리 생성 및 파일 작성**

```csharp
// Enums/ContractKind.cs
namespace ProjectAscension.Domain.Enums;
public enum ContractKind { Task, Recurring, Position, Ownership, Citizenship, License, Inheritance }
```

```csharp
// Enums/ContractStatus.cs
namespace ProjectAscension.Domain.Enums;
public enum ContractStatus { Draft, Open, Assigned, Completed, Failed, Cancelled, Expired }
```

```csharp
// Enums/ContractPurpose.cs
namespace ProjectAscension.Domain.Enums;
public enum ContractPurpose { Hunt, Survey, Collection, Escort, Defense, Transport, Research, Governance, Settlement, Trade, Education }
```

```csharp
// Enums/DiscoveryType.cs
namespace ProjectAscension.Domain.Enums;
public enum DiscoveryType { Region, Map, Skill, Command, Ruin, ResourceNode, MonsterSpecies, Knowledge, Recipe, MovementTechnique }
```

```csharp
// Enums/ItemType.cs
namespace ProjectAscension.Domain.Enums;
public enum ItemType { Resource, Weapon, MagicTool, Map, KnowledgeDocument, MonsterMaterial, ContractDocument, Equipment, Consumable }
```

```csharp
// Enums/RegionType.cs
namespace ProjectAscension.Domain.Enums;
public enum RegionType { City, SafeZone, Frontier, Wilderness, Ruin, CentralWaste, BabelArea, Borderland }
```

```csharp
// Enums/MonsterTier.cs
namespace ProjectAscension.Domain.Enums;
public enum MonsterTier { Common, Elite, Named, Beast, Disaster }
```

```csharp
// Enums/EquipmentType.cs
namespace ProjectAscension.Domain.Enums;
public enum EquipmentType { Weapon, MagicTool, Shield, Bow, Firearm, Catalyst }
```

```csharp
// Enums/SlotType.cs
namespace ProjectAscension.Domain.Enums;
public enum SlotType { Left, Right, Either, TwoHand }
```

- [ ] **Step 2: 빌드 확인**

```powershell
dotnet build packages/Domain/ProjectAscension.Domain/ProjectAscension.Domain.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```powershell
git add packages/Domain/ProjectAscension.Domain/Enums/
git commit -m "feat: add Domain enums"
```

---

## Task 4: Domain Entities

**Files:** `packages/Domain/ProjectAscension.Domain/Entities/` 하위 14개 파일

- [ ] **Step 1: Actor.cs, Character.cs, NPC.cs 작성**

```csharp
// Entities/Actor.cs
using ProjectAscension.Domain.Enums;
namespace ProjectAscension.Domain.Entities;

public class Actor
{
    public Guid Id { get; set; }
    public ActorType Type { get; set; }
    public Guid? CharacterId { get; set; }
    public Guid? NpcId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Character? Character { get; set; }
    public NPC? Npc { get; set; }
}

public enum ActorType { Player, NPC }
```

```csharp
// Entities/Character.cs
namespace ProjectAscension.Domain.Entities;

public class Character
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid OriginRegionId { get; set; }
    public Guid CurrentRegionId { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }

    public Actor? Actor { get; set; }
    public Region? CurrentRegion { get; set; }
}
```

```csharp
// Entities/NPC.cs
namespace ProjectAscension.Domain.Entities;

public class NPC
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid HomeRegionId { get; set; }
    public Guid CurrentRegionId { get; set; }
    public bool Alive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
```

- [ ] **Step 2: Contract.cs, ContractMarketAccessPoint.cs 작성**

```csharp
// Entities/Contract.cs
using ProjectAscension.Domain.Enums;
namespace ProjectAscension.Domain.Entities;

public class Contract
{
    public Guid Id { get; set; }
    public ContractKind Kind { get; set; }
    public ContractPurpose Purpose { get; set; }
    public Guid? IssuerActorId { get; set; }
    public Guid? AssigneeActorId { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Open;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RewardJson { get; set; } = "{}";
    public string ConditionsJson { get; set; } = "{}";
    public bool DelegationAllowed { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

```csharp
// Entities/ContractMarketAccessPoint.cs
namespace ProjectAscension.Domain.Entities;

public class ContractMarketAccessPoint
{
    public Guid Id { get; set; }
    public Guid RegionId { get; set; }
    public Guid? OrganizationId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Region? Region { get; set; }
}
```

- [ ] **Step 3: Item.cs, Equipment.cs, Loadout.cs 작성**

```csharp
// Entities/Item.cs
using ProjectAscension.Domain.Enums;
namespace ProjectAscension.Domain.Entities;

public class Item
{
    public Guid Id { get; set; }
    public ItemType Type { get; set; }
    public Guid TemplateId { get; set; }
    public Guid? OwnerActorId { get; set; }
    public Guid? CurrentRegionId { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
}
```

```csharp
// Entities/Equipment.cs
using ProjectAscension.Domain.Enums;
namespace ProjectAscension.Domain.Entities;

public class Equipment
{
    public Guid ItemId { get; set; }
    public EquipmentType EquipmentType { get; set; }
    public SlotType SlotType { get; set; }

    public Item? Item { get; set; }
}
```

```csharp
// Entities/Loadout.cs
namespace ProjectAscension.Domain.Entities;

public class Loadout
{
    public Guid ActorId { get; set; }
    public Guid? LeftItemId { get; set; }
    public Guid? RightItemId { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Actor? Actor { get; set; }
}
```

- [ ] **Step 4: Region.cs, Monster.cs, MonsterSpecies.cs 작성**

```csharp
// Entities/Region.cs
using ProjectAscension.Domain.Enums;
namespace ProjectAscension.Domain.Entities;

public class Region
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public RegionType Type { get; set; }
    public Guid? ParentRegionId { get; set; }
    public int DangerLevel { get; set; }
    public string EnvironmentTagsJson { get; set; } = "[]";
}
```

```csharp
// Entities/MonsterSpecies.cs
using ProjectAscension.Domain.Enums;
namespace ProjectAscension.Domain.Entities;

public class MonsterSpecies
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public MonsterTier Tier { get; set; }
    public string DropsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; }
}
```

```csharp
// Entities/Monster.cs
using ProjectAscension.Domain.Enums;
namespace ProjectAscension.Domain.Entities;

public class Monster
{
    public Guid Id { get; set; }
    public Guid SpeciesId { get; set; }
    public Guid RegionId { get; set; }
    public MonsterTier Tier { get; set; }
    public bool Alive { get; set; } = true;
    public DateTime SpawnedAt { get; set; }

    public MonsterSpecies? Species { get; set; }
    public Region? Region { get; set; }
}
```

- [ ] **Step 5: Discovery.cs, DiscoveryCandidate.cs, DiscoveryProgress.cs 작성**

```csharp
// Entities/Discovery.cs
using ProjectAscension.Domain.Enums;
namespace ProjectAscension.Domain.Entities;

public class Discovery
{
    public Guid Id { get; set; }
    public DiscoveryType Type { get; set; }
    public Guid DiscovererActorId { get; set; }
    public Guid RegionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DiscoveredAt { get; set; }

    public Actor? Discoverer { get; set; }
    public Region? Region { get; set; }
}
```

```csharp
// Entities/DiscoveryCandidate.cs
namespace ProjectAscension.Domain.Entities;

public class DiscoveryCandidate
{
    public Guid Id { get; set; }
    public string CandidateKey { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string RequiredContextJson { get; set; } = "{}";
    public int RequiredProgress { get; set; }
    public string Rarity { get; set; } = "Common";
    public DateTime CreatedAt { get; set; }
}
```

```csharp
// Entities/DiscoveryProgress.cs
namespace ProjectAscension.Domain.Entities;

public class DiscoveryProgress
{
    public Guid Id { get; set; }
    public Guid ActorId { get; set; }
    public Guid DiscoveryCandidateId { get; set; }
    public int Progress { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; }

    public Actor? Actor { get; set; }
    public DiscoveryCandidate? Candidate { get; set; }
}
```

- [ ] **Step 6: 빌드 확인**

```powershell
dotnet build packages/Domain/ProjectAscension.Domain/ProjectAscension.Domain.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 7: Commit**

```powershell
git add packages/Domain/ProjectAscension.Domain/Entities/
git commit -m "feat: add Domain MVP entities"
```

---

## Task 5: Domain Interfaces

**Files:** `packages/Domain/ProjectAscension.Domain/Interfaces/` 하위 4개 파일

- [ ] **Step 1: Repository 인터페이스 작성**

```csharp
// Interfaces/ICharacterRepository.cs
using ProjectAscension.Domain.Entities;
namespace ProjectAscension.Domain.Interfaces;

public interface ICharacterRepository
{
    Task<Character?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Actor?> GetActorByCharacterIdAsync(Guid characterId, CancellationToken ct = default);
}
```

```csharp
// Interfaces/IContractRepository.cs
using ProjectAscension.Domain.Entities;
namespace ProjectAscension.Domain.Interfaces;

public interface IContractRepository
{
    Task<IReadOnlyList<Contract>> GetByRegionAsync(Guid regionId, CancellationToken ct = default);
    Task<Contract?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(Contract contract, CancellationToken ct = default);
}
```

```csharp
// Interfaces/IDiscoveryRepository.cs
using ProjectAscension.Domain.Entities;
namespace ProjectAscension.Domain.Interfaces;

public interface IDiscoveryRepository
{
    Task<IReadOnlyList<Discovery>> GetByActorAsync(Guid actorId, CancellationToken ct = default);
    Task AddAsync(Discovery discovery, CancellationToken ct = default);
    Task<DiscoveryProgress?> GetProgressAsync(Guid actorId, Guid candidateId, CancellationToken ct = default);
    Task UpsertProgressAsync(DiscoveryProgress progress, CancellationToken ct = default);
}
```

```csharp
// Interfaces/IItemRepository.cs
using ProjectAscension.Domain.Entities;
namespace ProjectAscension.Domain.Interfaces;

public interface IItemRepository
{
    Task<IReadOnlyList<Item>> GetByActorAsync(Guid actorId, CancellationToken ct = default);
    Task<Loadout?> GetLoadoutAsync(Guid actorId, CancellationToken ct = default);
    Task UpsertLoadoutAsync(Loadout loadout, CancellationToken ct = default);
}
```

- [ ] **Step 2: 빌드 확인**

```powershell
dotnet build packages/Domain/ProjectAscension.Domain/ProjectAscension.Domain.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```powershell
git add packages/Domain/ProjectAscension.Domain/Interfaces/
git commit -m "feat: add Domain repository interfaces"
```

---

## Task 6: Contracts 패키지

**Files:** `packages/Contracts/ProjectAscension.Contracts/` 하위 파일들

- [ ] **Step 1: Contracts Enums 작성**

```csharp
// Enums/ContractKind.cs
namespace ProjectAscension.Contracts.Enums;
public enum ContractKind { Task, Recurring, Position, Ownership, Citizenship, License, Inheritance }
```

```csharp
// Enums/ContractStatus.cs
namespace ProjectAscension.Contracts.Enums;
public enum ContractStatus { Draft, Open, Assigned, Completed, Failed, Cancelled, Expired }
```

```csharp
// Enums/DiscoveryType.cs
namespace ProjectAscension.Contracts.Enums;
public enum DiscoveryType { Region, Map, Skill, Command, Ruin, ResourceNode, MonsterSpecies, Knowledge, Recipe, MovementTechnique }
```

- [ ] **Step 2: Request DTO 작성**

```csharp
// Requests/AcceptContractRequest.cs
namespace ProjectAscension.Contracts.Requests;
public record AcceptContractRequest(Guid ActorId);
```

```csharp
// Requests/RecordDiscoveryRequest.cs
using ProjectAscension.Contracts.Enums;
namespace ProjectAscension.Contracts.Requests;
public record RecordDiscoveryRequest(Guid ActorId, Guid RegionId, DiscoveryType Type, string Title, string Description);
```

```csharp
// Requests/UpdateLoadoutRequest.cs
namespace ProjectAscension.Contracts.Requests;
public record UpdateLoadoutRequest(Guid? LeftItemId, Guid? RightItemId);
```

- [ ] **Step 3: Response DTO 작성**

```csharp
// Responses/CharacterResponse.cs
namespace ProjectAscension.Contracts.Responses;
public record CharacterResponse(Guid Id, Guid ActorId, string Name, Guid CurrentRegionId, string Status);
```

```csharp
// Responses/ContractResponse.cs
using ProjectAscension.Contracts.Enums;
namespace ProjectAscension.Contracts.Responses;
public record ContractResponse(Guid Id, ContractKind Kind, string Title, string Description, ContractStatus Status, string RewardJson);
```

```csharp
// Responses/DiscoveryResponse.cs
using ProjectAscension.Contracts.Enums;
namespace ProjectAscension.Contracts.Responses;
public record DiscoveryResponse(Guid Id, DiscoveryType Type, string Title, string Description, DateTime DiscoveredAt);
```

- [ ] **Step 4: GameMessages 작성**

```csharp
// GameMessages/PlayerInputMessage.cs
using MessagePack;
namespace ProjectAscension.Contracts.GameMessages;

[MessagePackObject]
public record PlayerInputMessage(
    [property: Key(0)] Guid ActorId,
    [property: Key(1)] float MoveX,
    [property: Key(2)] float MoveZ,
    [property: Key(3)] bool Jump,
    [property: Key(4)] bool Dodge,
    [property: Key(5)] bool Attack,
    [property: Key(6)] int Sequence
);
```

```csharp
// GameMessages/WorldStateMessage.cs
using MessagePack;
namespace ProjectAscension.Contracts.GameMessages;

[MessagePackObject]
public record WorldStateMessage(
    [property: Key(0)] long Tick,
    [property: Key(1)] EntitySnapshot[] Entities
);

[MessagePackObject]
public record EntitySnapshot(
    [property: Key(0)] Guid ActorId,
    [property: Key(1)] float PosX,
    [property: Key(2)] float PosY,
    [property: Key(3)] float PosZ,
    [property: Key(4)] float VelX,
    [property: Key(5)] float VelY,
    [property: Key(6)] float VelZ
);
```

```csharp
// GameMessages/GameEventMessage.cs
using MessagePack;
namespace ProjectAscension.Contracts.GameMessages;

[MessagePackObject]
public record GameEventMessage(
    [property: Key(0)] string EventType,
    [property: Key(1)] Guid ActorId,
    [property: Key(2)] string PayloadJson
);
```

- [ ] **Step 5: 빌드 확인**

```powershell
dotnet build packages/Contracts/ProjectAscension.Contracts/ProjectAscension.Contracts.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 6: Commit**

```powershell
git add packages/Contracts/
git commit -m "feat: add Contracts package with DTOs and GameMessages"
```

---

## Task 7: GameSimulation — PlayerState & PlayerInput (TDD)

**Files:**
- Create: `packages/GameSimulation/ProjectAscension.GameSimulation/Player/PlayerInput.cs`
- Create: `packages/GameSimulation/ProjectAscension.GameSimulation/Player/PlayerState.cs`
- Create: `packages/GameSimulation/ProjectAscension.GameSimulation.Tests/Player/PlayerSimulationTests.cs`

- [ ] **Step 1: 실패할 테스트 작성**

```csharp
// packages/GameSimulation/ProjectAscension.GameSimulation.Tests/Player/PlayerSimulationTests.cs
using System.Numerics;
using ProjectAscension.GameSimulation.Player;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Player;

public class PlayerSimulationTests
{
    private readonly PlayerSimulation _sim = new();

    [Fact]
    public void ApplyInput_MoveForward_IncreasesZPosition()
    {
        var state = new PlayerState(Vector3.Zero, Vector3.Zero, IsGrounded: true, InputSequence: 0);
        var input = new PlayerInput(MoveX: 0f, MoveZ: 1f, Jump: false, Dodge: false, Attack: false, Sequence: 1);

        var next = _sim.ApplyInput(state, input, deltaTime: 0.016f);

        Assert.True(next.Position.Z > 0f);
    }

    [Fact]
    public void ApplyInput_JumpWhenGrounded_AppliesUpwardVelocity()
    {
        var state = new PlayerState(Vector3.Zero, Vector3.Zero, IsGrounded: true, InputSequence: 0);
        var input = new PlayerInput(MoveX: 0f, MoveZ: 0f, Jump: true, Dodge: false, Attack: false, Sequence: 1);

        var next = _sim.ApplyInput(state, input, deltaTime: 0.016f);

        Assert.True(next.Velocity.Y > 0f);
        Assert.False(next.IsGrounded);
    }

    [Fact]
    public void ApplyInput_JumpWhenAirborne_NoAdditionalVelocity()
    {
        var airState = new PlayerState(new Vector3(0, 2, 0), new Vector3(0, 3, 0), IsGrounded: false, InputSequence: 0);
        var input = new PlayerInput(MoveX: 0f, MoveZ: 0f, Jump: true, Dodge: false, Attack: false, Sequence: 1);

        var next = _sim.ApplyInput(airState, input, deltaTime: 0.016f);

        Assert.True(next.Velocity.Y < 3f); // gravity applies, no extra jump
    }

    [Fact]
    public void ApplyInput_Gravity_PullsDownWhenAirborne()
    {
        var airState = new PlayerState(new Vector3(0, 5, 0), Vector3.Zero, IsGrounded: false, InputSequence: 0);
        var input = new PlayerInput(MoveX: 0f, MoveZ: 0f, Jump: false, Dodge: false, Attack: false, Sequence: 1);

        var next = _sim.ApplyInput(airState, input, deltaTime: 0.016f);

        Assert.True(next.Velocity.Y < 0f);
    }

    [Fact]
    public void ApplyInput_LandsOnGround_SetsIsGrounded()
    {
        var nearGround = new PlayerState(new Vector3(0, 0.01f, 0), new Vector3(0, -1f, 0), IsGrounded: false, InputSequence: 0);
        var input = new PlayerInput(MoveX: 0f, MoveZ: 0f, Jump: false, Dodge: false, Attack: false, Sequence: 1);

        var next = _sim.ApplyInput(nearGround, input, deltaTime: 0.016f);

        Assert.True(next.IsGrounded);
        Assert.Equal(0f, next.Position.Y);
    }
}
```

- [ ] **Step 2: 테스트 실행 — 실패 확인**

```powershell
dotnet test packages/GameSimulation/ProjectAscension.GameSimulation.Tests/ProjectAscension.GameSimulation.Tests.csproj
```

Expected: 컴파일 오류 (`PlayerSimulation`, `PlayerState`, `PlayerInput` 미정의)

- [ ] **Step 3: PlayerInput, PlayerState 구현**

```csharp
// packages/GameSimulation/ProjectAscension.GameSimulation/Player/PlayerInput.cs
namespace ProjectAscension.GameSimulation.Player;

public record PlayerInput(
    float MoveX,
    float MoveZ,
    bool Jump,
    bool Dodge,
    bool Attack,
    int Sequence
);
```

```csharp
// packages/GameSimulation/ProjectAscension.GameSimulation/Player/PlayerState.cs
using System.Numerics;
namespace ProjectAscension.GameSimulation.Player;

public record PlayerState(
    Vector3 Position,
    Vector3 Velocity,
    bool IsGrounded,
    int InputSequence
);
```

- [ ] **Step 4: PlayerSimulation 구현**

```csharp
// packages/GameSimulation/ProjectAscension.GameSimulation/Player/PlayerSimulation.cs
using System.Numerics;
namespace ProjectAscension.GameSimulation.Player;

public class PlayerSimulation
{
    private const float MoveSpeed = 5f;
    private const float JumpVelocity = 6f;
    private const float Gravity = 20f;
    private const float GroundY = 0f;

    public PlayerState ApplyInput(PlayerState state, PlayerInput input, float deltaTime)
    {
        var velocity = state.Velocity;

        // 수평 이동
        velocity = velocity with
        {
            X = input.MoveX * MoveSpeed,
            Z = input.MoveZ * MoveSpeed
        };

        // 점프
        if (input.Jump && state.IsGrounded)
            velocity = velocity with { Y = JumpVelocity };

        // 중력
        if (!state.IsGrounded)
            velocity = velocity with { Y = velocity.Y - Gravity * deltaTime };

        // 위치 업데이트
        var position = state.Position + velocity * deltaTime;

        // 지면 판정
        bool isGrounded = position.Y <= GroundY;
        if (isGrounded)
        {
            position = position with { Y = GroundY };
            velocity = velocity with { Y = 0f };
        }

        return state with
        {
            Position = position,
            Velocity = velocity,
            IsGrounded = isGrounded,
            InputSequence = input.Sequence
        };
    }
}
```

- [ ] **Step 5: 테스트 실행 — 통과 확인**

```powershell
dotnet test packages/GameSimulation/ProjectAscension.GameSimulation.Tests/ProjectAscension.GameSimulation.Tests.csproj --verbosity normal
```

Expected: `Passed! - Failed: 0, Passed: 5`

- [ ] **Step 6: Commit**

```powershell
git add packages/GameSimulation/
git commit -m "feat: add PlayerSimulation with TDD (5 tests passing)"
```

---

## Task 8: GameSimulation — PhysicsWorld skeleton

**Files:**
- Create: `packages/GameSimulation/ProjectAscension.GameSimulation/Physics/PhysicsWorld.cs`

- [ ] **Step 1: PhysicsWorld.cs 작성**

BepuPhysics의 콜백 인터페이스를 최소 구현하고 Simulation 인스턴스를 래핑한다.

```csharp
// packages/GameSimulation/ProjectAscension.GameSimulation/Physics/PhysicsWorld.cs
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;

namespace ProjectAscension.GameSimulation.Physics;

public sealed class PhysicsWorld : IDisposable
{
    private readonly BufferPool _bufferPool;
    public Simulation Simulation { get; }

    public PhysicsWorld()
    {
        _bufferPool = new BufferPool();
        Simulation = Simulation.Create(
            _bufferPool,
            new NarrowPhaseCallbacks(),
            new PoseIntegratorCallbacks(new Vector3(0, -20f, 0)),
            new SolveDescription(8, 1));
    }

    public void Step(float deltaTime) => Simulation.Timestep(deltaTime);

    public void Dispose()
    {
        Simulation.Dispose();
        _bufferPool.Clear();
    }
}

// --- 최소 콜백 구현 ---

internal struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
{
    public void Initialize(Simulation simulation) { }
    public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin) => true;
    public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) => true;

    public bool ConfigureContactManifold<TManifold>(
        int workerIndex, CollidablePair pair, ref TManifold manifold,
        out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        pairMaterial = new PairMaterialProperties
        {
            FrictionCoefficient = 1f,
            MaximumRecoveryVelocity = 2f,
            SpringSettings = new SpringSettings(30, 1)
        };
        return true;
    }

    public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold) => true;
    public void Dispose() { }
}

internal struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
{
    private Vector3 _gravity;
    private Vector3Wide _gravityDtWide;

    public PoseIntegratorCallbacks(Vector3 gravity) { _gravity = gravity; _gravityDtWide = default; }

    public readonly AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
    public readonly bool AllowSubstepsForUnconstrainedBodies => false;
    public readonly bool IntegrateVelocityForKinetics => false;

    public void Initialize(Simulation simulation) { }

    public void PrepareForIntegration(float dt)
    {
        _gravityDtWide = Vector3Wide.Broadcast(_gravity * dt);
    }

    public void IntegrateVelocity(
        Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation,
        BodyInertiaWide localInertia, Vector<int> integrationMask,
        int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
    {
        velocity.Linear += _gravityDtWide;
    }
}
```

- [ ] **Step 2: 빌드 확인**

```powershell
dotnet build packages/GameSimulation/ProjectAscension.GameSimulation/ProjectAscension.GameSimulation.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```powershell
git add packages/GameSimulation/ProjectAscension.GameSimulation/Physics/
git commit -m "feat: add PhysicsWorld BepuPhysics2 wrapper"
```

---

## Task 9: Api — AppDbContext 및 EF 설정

**Files:**
- Modify: `apps/api/ProjectAscension.Api/Program.cs`
- Create: `apps/api/ProjectAscension.Api/appsettings.json`
- Create: `apps/api/ProjectAscension.Api/appsettings.Development.json`
- Create: `apps/api/ProjectAscension.Api/Data/AppDbContext.cs`
- Create: `apps/api/ProjectAscension.Api/Data/Configurations/*.cs` (12개)

- [ ] **Step 1: appsettings.json 작성**

```json
// apps/api/ProjectAscension.Api/appsettings.json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=project_ascension;Username=postgres;Password=postgres"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

```json
// apps/api/ProjectAscension.Api/appsettings.Development.json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=project_ascension_dev;Username=postgres;Password=postgres"
  }
}
```

- [ ] **Step 2: AppDbContext.cs 작성**

```csharp
// apps/api/ProjectAscension.Api/Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Actor> Actors => Set<Actor>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<NPC> NPCs => Set<NPC>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractMarketAccessPoint> ContractMarketAccessPoints => Set<ContractMarketAccessPoint>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<Loadout> Loadouts => Set<Loadout>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<MonsterSpecies> MonsterSpecies => Set<MonsterSpecies>();
    public DbSet<Monster> Monsters => Set<Monster>();
    public DbSet<Discovery> Discoveries => Set<Discovery>();
    public DbSet<DiscoveryCandidate> DiscoveryCandidates => Set<DiscoveryCandidate>();
    public DbSet<DiscoveryProgress> DiscoveryProgresses => Set<DiscoveryProgress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

- [ ] **Step 3: EF Configurations 작성**

각 파일을 `Data/Configurations/`에 생성한다.

```csharp
// Data/Configurations/ActorConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class ActorConfiguration : IEntityTypeConfiguration<Actor>
{
    public void Configure(EntityTypeBuilder<Actor> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Type).HasConversion<string>();
        builder.HasOne(a => a.Character).WithOne(c => c.Actor)
            .HasForeignKey<Actor>(a => a.CharacterId);
    }
}
```

```csharp
// Data/Configurations/CharacterConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
    }
}
```

```csharp
// Data/Configurations/ContractConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Kind).HasConversion<string>();
        builder.Property(c => c.Purpose).HasConversion<string>();
        builder.Property(c => c.Status).HasConversion<string>();
        builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
    }
}
```

```csharp
// Data/Configurations/ItemConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Type).HasConversion<string>();
    }
}
```

```csharp
// Data/Configurations/EquipmentConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.HasKey(e => e.ItemId);
        builder.Property(e => e.EquipmentType).HasConversion<string>();
        builder.Property(e => e.SlotType).HasConversion<string>();
        builder.HasOne(e => e.Item).WithOne()
            .HasForeignKey<Equipment>(e => e.ItemId);
    }
}
```

```csharp
// Data/Configurations/LoadoutConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class LoadoutConfiguration : IEntityTypeConfiguration<Loadout>
{
    public void Configure(EntityTypeBuilder<Loadout> builder)
    {
        builder.HasKey(l => l.ActorId);
        builder.HasOne(l => l.Actor).WithOne()
            .HasForeignKey<Loadout>(l => l.ActorId);
    }
}
```

```csharp
// Data/Configurations/RegionConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class RegionConfiguration : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Type).HasConversion<string>();
        builder.Property(r => r.Name).IsRequired().HasMaxLength(200);
    }
}
```

```csharp
// Data/Configurations/MonsterSpeciesConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class MonsterSpeciesConfiguration : IEntityTypeConfiguration<MonsterSpecies>
{
    public void Configure(EntityTypeBuilder<MonsterSpecies> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Tier).HasConversion<string>();
        builder.Property(m => m.Name).IsRequired().HasMaxLength(100);
    }
}
```

```csharp
// Data/Configurations/MonsterConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class MonsterConfiguration : IEntityTypeConfiguration<Monster>
{
    public void Configure(EntityTypeBuilder<Monster> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Tier).HasConversion<string>();
        builder.HasOne(m => m.Species).WithMany().HasForeignKey(m => m.SpeciesId);
        builder.HasOne(m => m.Region).WithMany().HasForeignKey(m => m.RegionId);
    }
}
```

```csharp
// Data/Configurations/DiscoveryCandidateConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class DiscoveryCandidateConfiguration : IEntityTypeConfiguration<DiscoveryCandidate>
{
    public void Configure(EntityTypeBuilder<DiscoveryCandidate> builder)
    {
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => d.CandidateKey).IsUnique();
        builder.Property(d => d.CandidateKey).IsRequired().HasMaxLength(200);
    }
}
```

```csharp
// Data/Configurations/DiscoveryConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class DiscoveryConfiguration : IEntityTypeConfiguration<Discovery>
{
    public void Configure(EntityTypeBuilder<Discovery> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Type).HasConversion<string>();
        builder.Property(d => d.Title).IsRequired().HasMaxLength(200);
        builder.HasOne(d => d.Discoverer).WithMany().HasForeignKey(d => d.DiscovererActorId);
        builder.HasOne(d => d.Region).WithMany().HasForeignKey(d => d.RegionId);
    }
}
```

```csharp
// Data/Configurations/DiscoveryProgressConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class DiscoveryProgressConfiguration : IEntityTypeConfiguration<DiscoveryProgress>
{
    public void Configure(EntityTypeBuilder<DiscoveryProgress> builder)
    {
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => new { d.ActorId, d.DiscoveryCandidateId }).IsUnique();
        builder.HasOne(d => d.Actor).WithMany().HasForeignKey(d => d.ActorId);
        builder.HasOne(d => d.Candidate).WithMany().HasForeignKey(d => d.DiscoveryCandidateId);
    }
}
```

- [ ] **Step 4: 빌드 확인**

```powershell
dotnet build apps/api/ProjectAscension.Api/ProjectAscension.Api.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```powershell
git add apps/api/ProjectAscension.Api/Data/ apps/api/ProjectAscension.Api/appsettings*.json
git commit -m "feat: add AppDbContext with EF Core configurations"
```

---

## Task 10: Api — Repositories, Services, Controllers, Middleware

**Files:** Api 나머지 파일들

- [ ] **Step 1: Repositories 작성**

```csharp
// Data/Repositories/CharacterRepository.cs
using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Data.Repositories;

public class CharacterRepository : ICharacterRepository
{
    private readonly AppDbContext _db;
    public CharacterRepository(AppDbContext db) => _db = db;

    public Task<Character?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Characters.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Actor?> GetActorByCharacterIdAsync(Guid characterId, CancellationToken ct = default)
        => _db.Actors.FirstOrDefaultAsync(a => a.CharacterId == characterId, ct);
}
```

```csharp
// Data/Repositories/ContractRepository.cs
using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Data.Repositories;

public class ContractRepository : IContractRepository
{
    private readonly AppDbContext _db;
    public ContractRepository(AppDbContext db) => _db = db;

    public Task<IReadOnlyList<Contract>> GetByRegionAsync(Guid regionId, CancellationToken ct = default)
        => _db.Contracts
            .Where(c => c.Status == Domain.Enums.ContractStatus.Open)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<Contract>)t.Result, ct);

    public Task<Contract?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Contracts.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task UpdateAsync(Contract contract, CancellationToken ct = default)
    {
        _db.Contracts.Update(contract);
        await _db.SaveChangesAsync(ct);
    }
}
```

```csharp
// Data/Repositories/DiscoveryRepository.cs
using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Data.Repositories;

public class DiscoveryRepository : IDiscoveryRepository
{
    private readonly AppDbContext _db;
    public DiscoveryRepository(AppDbContext db) => _db = db;

    public Task<IReadOnlyList<Discovery>> GetByActorAsync(Guid actorId, CancellationToken ct = default)
        => _db.Discoveries
            .Where(d => d.DiscovererActorId == actorId)
            .OrderByDescending(d => d.DiscoveredAt)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<Discovery>)t.Result, ct);

    public async Task AddAsync(Discovery discovery, CancellationToken ct = default)
    {
        _db.Discoveries.Add(discovery);
        await _db.SaveChangesAsync(ct);
    }

    public Task<DiscoveryProgress?> GetProgressAsync(Guid actorId, Guid candidateId, CancellationToken ct = default)
        => _db.DiscoveryProgresses
            .FirstOrDefaultAsync(p => p.ActorId == actorId && p.DiscoveryCandidateId == candidateId, ct);

    public async Task UpsertProgressAsync(DiscoveryProgress progress, CancellationToken ct = default)
    {
        var existing = await GetProgressAsync(progress.ActorId, progress.DiscoveryCandidateId, ct);
        if (existing is null)
            _db.DiscoveryProgresses.Add(progress);
        else
        {
            existing.Progress = progress.Progress;
            existing.MetadataJson = progress.MetadataJson;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }
}
```

```csharp
// Data/Repositories/ItemRepository.cs
using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Data.Repositories;

public class ItemRepository : IItemRepository
{
    private readonly AppDbContext _db;
    public ItemRepository(AppDbContext db) => _db = db;

    public Task<IReadOnlyList<Item>> GetByActorAsync(Guid actorId, CancellationToken ct = default)
        => _db.Items
            .Where(i => i.OwnerActorId == actorId)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<Item>)t.Result, ct);

    public Task<Loadout?> GetLoadoutAsync(Guid actorId, CancellationToken ct = default)
        => _db.Loadouts.FirstOrDefaultAsync(l => l.ActorId == actorId, ct);

    public async Task UpsertLoadoutAsync(Loadout loadout, CancellationToken ct = default)
    {
        var existing = await GetLoadoutAsync(loadout.ActorId, ct);
        if (existing is null)
            _db.Loadouts.Add(loadout);
        else
        {
            existing.LeftItemId = loadout.LeftItemId;
            existing.RightItemId = loadout.RightItemId;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 2: Service Interfaces & Implementations 작성**

```csharp
// Services/ICharacterService.cs
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Shared;
namespace ProjectAscension.Api.Services;
public interface ICharacterService
{
    Task<Result<CharacterResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
}
```

```csharp
// Services/CharacterService.cs
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Interfaces;
using ProjectAscension.Shared;
namespace ProjectAscension.Api.Services;

public class CharacterService : ICharacterService
{
    private readonly ICharacterRepository _repo;
    public CharacterService(ICharacterRepository repo) => _repo = repo;

    public async Task<Result<CharacterResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var character = await _repo.GetByIdAsync(id, ct);
        if (character is null) return Result<CharacterResponse>.Fail(Error.NotFound);

        var actor = await _repo.GetActorByCharacterIdAsync(id, ct);
        if (actor is null) return Result<CharacterResponse>.Fail(Error.NotFound);

        return Result<CharacterResponse>.Ok(new CharacterResponse(
            character.Id, actor.Id, character.Name, character.CurrentRegionId, character.Status));
    }
}
```

```csharp
// Services/IContractService.cs
using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Shared;
namespace ProjectAscension.Api.Services;
public interface IContractService
{
    Task<Result<IReadOnlyList<ContractResponse>>> GetByRegionAsync(Guid regionId, CancellationToken ct = default);
    Task<Result<ContractResponse>> AcceptAsync(Guid contractId, AcceptContractRequest request, CancellationToken ct = default);
    Task<Result<ContractResponse>> CompleteAsync(Guid contractId, CancellationToken ct = default);
}
```

```csharp
// Services/ContractService.cs
using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Enums;
using ProjectAscension.Domain.Interfaces;
using ProjectAscension.Shared;
namespace ProjectAscension.Api.Services;

public class ContractService : IContractService
{
    private readonly IContractRepository _repo;
    public ContractService(IContractRepository repo) => _repo = repo;

    public async Task<Result<IReadOnlyList<ContractResponse>>> GetByRegionAsync(Guid regionId, CancellationToken ct = default)
    {
        var contracts = await _repo.GetByRegionAsync(regionId, ct);
        var responses = contracts.Select(ToResponse).ToList();
        return Result<IReadOnlyList<ContractResponse>>.Ok(responses);
    }

    public async Task<Result<ContractResponse>> AcceptAsync(Guid contractId, AcceptContractRequest request, CancellationToken ct = default)
    {
        var contract = await _repo.GetByIdAsync(contractId, ct);
        if (contract is null) return Result<ContractResponse>.Fail(Error.NotFound);
        if (contract.Status != ContractStatus.Open) return Result<ContractResponse>.Fail(Error.Conflict);

        contract.Status = ContractStatus.Assigned;
        contract.AssigneeActorId = request.ActorId;
        await _repo.UpdateAsync(contract, ct);
        return Result<ContractResponse>.Ok(ToResponse(contract));
    }

    public async Task<Result<ContractResponse>> CompleteAsync(Guid contractId, CancellationToken ct = default)
    {
        var contract = await _repo.GetByIdAsync(contractId, ct);
        if (contract is null) return Result<ContractResponse>.Fail(Error.NotFound);
        if (contract.Status != ContractStatus.Assigned) return Result<ContractResponse>.Fail(Error.Conflict);

        contract.Status = ContractStatus.Completed;
        contract.CompletedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(contract, ct);
        return Result<ContractResponse>.Ok(ToResponse(contract));
    }

    private static ContractResponse ToResponse(Domain.Entities.Contract c) =>
        new(c.Id, (Contracts.Enums.ContractKind)(int)c.Kind, c.Title, c.Description,
            (Contracts.Enums.ContractStatus)(int)c.Status, c.RewardJson);
}
```

```csharp
// Services/IDiscoveryService.cs
using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Shared;
namespace ProjectAscension.Api.Services;
public interface IDiscoveryService
{
    Task<Result<DiscoveryResponse>> RecordAsync(RecordDiscoveryRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<DiscoveryResponse>>> GetByActorAsync(Guid actorId, CancellationToken ct = default);
}
```

```csharp
// Services/DiscoveryService.cs
using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;
using ProjectAscension.Shared;
namespace ProjectAscension.Api.Services;

public class DiscoveryService : IDiscoveryService
{
    private readonly IDiscoveryRepository _repo;
    public DiscoveryService(IDiscoveryRepository repo) => _repo = repo;

    public async Task<Result<DiscoveryResponse>> RecordAsync(RecordDiscoveryRequest request, CancellationToken ct = default)
    {
        var discovery = new Discovery
        {
            Id = Guid.NewGuid(),
            Type = (Domain.Enums.DiscoveryType)(int)request.Type,
            DiscovererActorId = request.ActorId,
            RegionId = request.RegionId,
            Title = request.Title,
            Description = request.Description,
            DiscoveredAt = DateTime.UtcNow
        };
        await _repo.AddAsync(discovery, ct);
        return Result<DiscoveryResponse>.Ok(new DiscoveryResponse(
            discovery.Id, request.Type, discovery.Title, discovery.Description, discovery.DiscoveredAt));
    }

    public async Task<Result<IReadOnlyList<DiscoveryResponse>>> GetByActorAsync(Guid actorId, CancellationToken ct = default)
    {
        var discoveries = await _repo.GetByActorAsync(actorId, ct);
        var responses = discoveries.Select(d => new DiscoveryResponse(
            d.Id, (Contracts.Enums.DiscoveryType)(int)d.Type, d.Title, d.Description, d.DiscoveredAt))
            .ToList();
        return Result<IReadOnlyList<DiscoveryResponse>>.Ok(responses);
    }
}
```

```csharp
// Services/IItemService.cs
using ProjectAscension.Shared;
namespace ProjectAscension.Api.Services;
public interface IItemService
{
    Task<Result<IReadOnlyList<Domain.Entities.Item>>> GetByActorAsync(Guid actorId, CancellationToken ct = default);
}
```

```csharp
// Services/ItemService.cs
using ProjectAscension.Domain.Interfaces;
using ProjectAscension.Shared;
namespace ProjectAscension.Api.Services;

public class ItemService : IItemService
{
    private readonly IItemRepository _repo;
    public ItemService(IItemRepository repo) => _repo = repo;

    public async Task<Result<IReadOnlyList<Domain.Entities.Item>>> GetByActorAsync(Guid actorId, CancellationToken ct = default)
    {
        var items = await _repo.GetByActorAsync(actorId, ct);
        return Result<IReadOnlyList<Domain.Entities.Item>>.Ok(items);
    }
}
```

```csharp
// Services/ILoadoutService.cs
using ProjectAscension.Contracts.Requests;
using ProjectAscension.Shared;
namespace ProjectAscension.Api.Services;
public interface ILoadoutService
{
    Task<Result<Domain.Entities.Loadout?>> GetAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<Domain.Entities.Loadout>> UpdateAsync(Guid actorId, UpdateLoadoutRequest request, CancellationToken ct = default);
}
```

```csharp
// Services/LoadoutService.cs
using ProjectAscension.Contracts.Requests;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;
using ProjectAscension.Shared;
namespace ProjectAscension.Api.Services;

public class LoadoutService : ILoadoutService
{
    private readonly IItemRepository _repo;
    public LoadoutService(IItemRepository repo) => _repo = repo;

    public async Task<Result<Loadout?>> GetAsync(Guid actorId, CancellationToken ct = default)
    {
        var loadout = await _repo.GetLoadoutAsync(actorId, ct);
        return Result<Loadout?>.Ok(loadout);
    }

    public async Task<Result<Loadout>> UpdateAsync(Guid actorId, UpdateLoadoutRequest request, CancellationToken ct = default)
    {
        var loadout = new Loadout
        {
            ActorId = actorId,
            LeftItemId = request.LeftItemId,
            RightItemId = request.RightItemId,
            UpdatedAt = DateTime.UtcNow
        };
        await _repo.UpsertLoadoutAsync(loadout, ct);
        return Result<Loadout>.Ok(loadout);
    }
}
```

- [ ] **Step 3: Controllers 작성**

```csharp
// Controllers/CharactersController.cs
using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Api.Services;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/characters")]
public class CharactersController : ControllerBase
{
    private readonly ICharacterService _service;
    public CharactersController(ICharacterService service) => _service = service;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }
}
```

```csharp
// Controllers/ContractsController.cs
using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/contracts")]
public class ContractsController : ControllerBase
{
    private readonly IContractService _service;
    public ContractsController(IContractService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetByRegion([FromQuery] Guid regionId, CancellationToken ct)
    {
        var result = await _service.GetByRegionAsync(regionId, ct);
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, [FromBody] AcceptContractRequest request, CancellationToken ct)
    {
        var result = await _service.AcceptAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : Conflict(result.Error);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await _service.CompleteAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : Conflict(result.Error);
    }
}
```

```csharp
// Controllers/DiscoveriesController.cs
using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/discoveries")]
public class DiscoveriesController : ControllerBase
{
    private readonly IDiscoveryService _service;
    public DiscoveriesController(IDiscoveryService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetByActor([FromQuery] Guid actorId, CancellationToken ct)
    {
        var result = await _service.GetByActorAsync(actorId, ct);
        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Record([FromBody] RecordDiscoveryRequest request, CancellationToken ct)
    {
        var result = await _service.RecordAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetByActor), new { actorId = request.ActorId }, result.Value)
            : BadRequest(result.Error);
    }
}
```

```csharp
// Controllers/ItemsController.cs
using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Api.Services;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/items")]
public class ItemsController : ControllerBase
{
    private readonly IItemService _service;
    public ItemsController(IItemService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetByActor([FromQuery] Guid actorId, CancellationToken ct)
    {
        var result = await _service.GetByActorAsync(actorId, ct);
        return Ok(result.Value);
    }
}
```

```csharp
// Controllers/LoadoutsController.cs
using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/loadouts")]
public class LoadoutsController : ControllerBase
{
    private readonly ILoadoutService _service;
    public LoadoutsController(ILoadoutService service) => _service = service;

    [HttpGet("{actorId:guid}")]
    public async Task<IActionResult> Get(Guid actorId, CancellationToken ct)
    {
        var result = await _service.GetAsync(actorId, ct);
        return Ok(result.Value);
    }

    [HttpPut("{actorId:guid}")]
    public async Task<IActionResult> Update(Guid actorId, [FromBody] UpdateLoadoutRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(actorId, request, ct);
        return Ok(result.Value);
    }
}
```

- [ ] **Step 4: ExceptionMiddleware 작성**

```csharp
// Middleware/ExceptionMiddleware.cs
using System.Net;
using System.Text.Json;

namespace ProjectAscension.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Internal server error" }));
        }
    }
}
```

- [ ] **Step 5: Program.cs 작성**

```csharp
// apps/api/ProjectAscension.Api/Program.cs
using Microsoft.EntityFrameworkCore;
using ProjectAscension.Api.Data;
using ProjectAscension.Api.Data.Repositories;
using ProjectAscension.Api.Middleware;
using ProjectAscension.Api.Services;
using ProjectAscension.Domain.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Repositories
builder.Services.AddScoped<ICharacterRepository, CharacterRepository>();
builder.Services.AddScoped<IContractRepository, ContractRepository>();
builder.Services.AddScoped<IDiscoveryRepository, DiscoveryRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();

// Services
builder.Services.AddScoped<ICharacterService, CharacterService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IDiscoveryService, DiscoveryService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<ILoadoutService, LoadoutService>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
```

- [ ] **Step 6: 빌드 확인**

```powershell
dotnet build apps/api/ProjectAscension.Api/ProjectAscension.Api.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 7: Commit**

```powershell
git add apps/api/ProjectAscension.Api/
git commit -m "feat: add Api scaffold with controllers, services, and repositories"
```

---

## Task 11: GameServer Scaffold

**Files:** `apps/game-server/ProjectAscension.GameServer/` 하위 파일들

- [ ] **Step 1: ENetTransport.cs 작성**

```csharp
// Network/ENetTransport.cs
using ENet;

namespace ProjectAscension.GameServer.Network;

public sealed class ENetTransport : IDisposable
{
    private Host _host = null!;

    public event Action<Peer>? Connected;
    public event Action<Peer>? Disconnected;
    public event Action<Peer, byte[]>? PacketReceived;

    public void Start(ushort port, int maxClients = 100)
    {
        Library.Initialize();
        _host = new Host();
        var address = new Address { Port = port };
        _host.Create(address, maxClients);
    }

    public void Poll()
    {
        while (_host.Service(0, out var netEvent) > 0)
        {
            switch (netEvent.Type)
            {
                case EventType.Connect:
                    Connected?.Invoke(netEvent.Peer);
                    break;
                case EventType.Receive:
                    var data = new byte[netEvent.Packet.Length];
                    netEvent.Packet.CopyTo(data);
                    netEvent.Packet.Dispose();
                    PacketReceived?.Invoke(netEvent.Peer, data);
                    break;
                case EventType.Disconnect:
                    Disconnected?.Invoke(netEvent.Peer);
                    break;
            }
        }
    }

    public void Send(Peer peer, byte[] data, byte channelId = 0, PacketFlags flags = PacketFlags.Reliable)
    {
        var packet = default(Packet);
        packet.Create(data, flags);
        peer.Send(channelId, ref packet);
    }

    public void Dispose()
    {
        _host?.Dispose();
        Library.Deinitialize();
    }
}
```

- [ ] **Step 2: PacketHandler.cs, PacketSender.cs 작성**

```csharp
// Network/PacketHandler.cs
using ENet;
using MessagePack;
using ProjectAscension.Contracts.GameMessages;

namespace ProjectAscension.GameServer.Network;

public class PacketHandler
{
    public event Action<Peer, PlayerInputMessage>? InputReceived;

    public void Handle(Peer peer, byte[] data)
    {
        try
        {
            var input = MessagePackSerializer.Deserialize<PlayerInputMessage>(data);
            InputReceived?.Invoke(peer, input);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PacketHandler] Deserialize error: {ex.Message}");
        }
    }
}
```

```csharp
// Network/PacketSender.cs
using ENet;
using MessagePack;
using ProjectAscension.Contracts.GameMessages;

namespace ProjectAscension.GameServer.Network;

public class PacketSender
{
    private readonly ENetTransport _transport;
    public PacketSender(ENetTransport transport) => _transport = transport;

    public void SendWorldState(Peer peer, WorldStateMessage message)
    {
        var data = MessagePackSerializer.Serialize(message);
        _transport.Send(peer, data, channelId: 0, PacketFlags.Unreliable);
    }

    public void SendGameEvent(Peer peer, GameEventMessage message)
    {
        var data = MessagePackSerializer.Serialize(message);
        _transport.Send(peer, data, channelId: 1, PacketFlags.Reliable);
    }
}
```

- [ ] **Step 3: SessionManager.cs, ZoneInstance.cs 작성**

```csharp
// SessionManager.cs
using ENet;

namespace ProjectAscension.GameServer;

public class SessionManager
{
    private readonly Dictionary<uint, Guid> _peerToActor = new();
    private readonly Dictionary<Guid, Peer> _actorToPeer = new();

    public void Register(Peer peer, Guid actorId)
    {
        _peerToActor[peer.ID] = actorId;
        _actorToPeer[actorId] = peer;
    }

    public void Unregister(Peer peer)
    {
        if (_peerToActor.TryGetValue(peer.ID, out var actorId))
        {
            _peerToActor.Remove(peer.ID);
            _actorToPeer.Remove(actorId);
        }
    }

    public Guid? GetActorId(Peer peer)
        => _peerToActor.TryGetValue(peer.ID, out var id) ? id : null;

    public Peer? GetPeer(Guid actorId)
        => _actorToPeer.TryGetValue(actorId, out var peer) ? peer : null;

    public IEnumerable<Guid> AllActors => _actorToPeer.Keys;
}
```

```csharp
// ZoneInstance.cs
using ProjectAscension.GameSimulation.Player;

namespace ProjectAscension.GameServer;

public class ZoneInstance
{
    private readonly Dictionary<Guid, PlayerState> _playerStates = new();
    private readonly PlayerSimulation _sim = new();

    public void AddPlayer(Guid actorId)
        => _playerStates[actorId] = new PlayerState(
            System.Numerics.Vector3.Zero, System.Numerics.Vector3.Zero, IsGrounded: true, InputSequence: 0);

    public void RemovePlayer(Guid actorId) => _playerStates.Remove(actorId);

    public void ApplyInput(Guid actorId, PlayerInput input, float deltaTime)
    {
        if (!_playerStates.TryGetValue(actorId, out var state)) return;
        _playerStates[actorId] = _sim.ApplyInput(state, input, deltaTime);
    }

    public IReadOnlyDictionary<Guid, PlayerState> PlayerStates => _playerStates;
}
```

- [ ] **Step 4: ApiReporter.cs 작성**

```csharp
// ApiReporter.cs
namespace ProjectAscension.GameServer;

public class ApiReporter
{
    private readonly HttpClient _http;
    private readonly string _apiBase;

    public ApiReporter(string apiBaseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
        _apiBase = apiBaseUrl;
    }

    public async Task ReportMonsterKilledAsync(Guid actorId, Guid monsterId)
    {
        var payload = new { actorId, monsterId, killedAt = DateTime.UtcNow };
        await _http.PostAsJsonAsync("/api/internal/monster-killed", payload);
    }

    public async Task ReportDiscoveryCandidateAsync(Guid actorId, string candidateKey, int progress)
    {
        var payload = new { actorId, candidateKey, progress };
        await _http.PostAsJsonAsync("/api/internal/discovery-progress", payload);
    }
}
```

- [ ] **Step 5: GameLoop.cs 작성**

```csharp
// GameLoop.cs
using ENet;
using ProjectAscension.Contracts.GameMessages;
using ProjectAscension.GameServer.Network;
using ProjectAscension.GameSimulation.Player;

namespace ProjectAscension.GameServer;

public class GameLoop
{
    private const int MovementTickHz = 20;
    private const int CombatTickHz = 64;
    private readonly TimeSpan _movementInterval = TimeSpan.FromMilliseconds(1000.0 / MovementTickHz);
    private readonly TimeSpan _combatInterval = TimeSpan.FromMilliseconds(1000.0 / CombatTickHz);

    private readonly ENetTransport _transport;
    private readonly PacketHandler _handler;
    private readonly PacketSender _sender;
    private readonly SessionManager _sessions;
    private readonly ZoneInstance _zone;

    private long _tick;

    public GameLoop(ENetTransport transport, PacketHandler handler, PacketSender sender,
        SessionManager sessions, ZoneInstance zone)
    {
        _transport = transport;
        _handler = handler;
        _sender = sender;
        _sessions = sessions;
        _zone = zone;

        _transport.Connected += OnConnect;
        _transport.Disconnected += OnDisconnect;
        _transport.PacketReceived += (peer, data) => _handler.Handle(peer, data);

        _handler.InputReceived += OnInputReceived;
    }

    private void OnConnect(Peer peer)
    {
        Console.WriteLine($"[GameLoop] Peer {peer.ID} connected.");
        // actorId 인증은 추후 구현. 임시로 새 Guid 할당.
        var actorId = Guid.NewGuid();
        _sessions.Register(peer, actorId);
        _zone.AddPlayer(actorId);
    }

    private void OnDisconnect(Peer peer)
    {
        var actorId = _sessions.GetActorId(peer);
        if (actorId.HasValue) _zone.RemovePlayer(actorId.Value);
        _sessions.Unregister(peer);
        Console.WriteLine($"[GameLoop] Peer {peer.ID} disconnected.");
    }

    private void OnInputReceived(Peer peer, PlayerInputMessage msg)
    {
        var actorId = _sessions.GetActorId(peer);
        if (!actorId.HasValue) return;

        var input = new PlayerInput(msg.MoveX, msg.MoveZ, msg.Jump, msg.Dodge, msg.Attack, msg.Sequence);
        _zone.ApplyInput(actorId.Value, input, deltaTime: (float)_movementInterval.TotalSeconds);
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var lastMovement = DateTime.UtcNow;

        Console.WriteLine("[GameLoop] Running.");

        while (!ct.IsCancellationRequested)
        {
            _transport.Poll();

            var now = DateTime.UtcNow;
            if (now - lastMovement >= _movementInterval)
            {
                BroadcastWorldState();
                lastMovement = now;
                _tick++;
            }

            await Task.Delay(1, ct);
        }
    }

    private void BroadcastWorldState()
    {
        var snapshots = _zone.PlayerStates.Select(kv => new EntitySnapshot(
            kv.Key,
            kv.Value.Position.X, kv.Value.Position.Y, kv.Value.Position.Z,
            kv.Value.Velocity.X, kv.Value.Velocity.Y, kv.Value.Velocity.Z
        )).ToArray();

        var message = new WorldStateMessage(_tick, snapshots);

        foreach (var actorId in _sessions.AllActors)
        {
            var peer = _sessions.GetPeer(actorId);
            if (peer.HasValue) _sender.SendWorldState(peer.Value, message);
        }
    }
}
```

- [ ] **Step 6: Program.cs 작성**

```csharp
// apps/game-server/ProjectAscension.GameServer/Program.cs
using ProjectAscension.GameServer;
using ProjectAscension.GameServer.Network;

const ushort Port = 7777;
const string ApiBaseUrl = "http://localhost:5000";

using var transport = new ENetTransport();
var handler = new PacketHandler();
var sender = new PacketSender(transport);
var sessions = new SessionManager();
var zone = new ZoneInstance();
var reporter = new ApiReporter(ApiBaseUrl);
var loop = new GameLoop(transport, handler, sender, sessions, zone);

transport.Start(Port);
Console.WriteLine($"[GameServer] Listening on port {Port}");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

await loop.RunAsync(cts.Token);
Console.WriteLine("[GameServer] Stopped.");
```

- [ ] **Step 7: 빌드 확인**

```powershell
dotnet build apps/game-server/ProjectAscension.GameServer/ProjectAscension.GameServer.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 8: Commit**

```powershell
git add apps/game-server/ProjectAscension.GameServer/
git commit -m "feat: add GameServer scaffold with ENet loop and ZoneInstance"
```

---

## Task 12: 전체 빌드 및 테스트 검증

- [ ] **Step 1: 전체 솔루션 빌드**

```powershell
dotnet build
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 2: 전체 테스트 실행**

```powershell
dotnet test
```

Expected: `Passed! - Failed: 0, Passed: 5` (PlayerSimulationTests)

- [ ] **Step 3: EF Core 마이그레이션 생성 확인**

```powershell
dotnet ef migrations add InitialCreate --project apps/api/ProjectAscension.Api/ProjectAscension.Api.csproj --output-dir Data/Migrations
```

Expected: 오류 없이 `Data/Migrations/` 아래 마이그레이션 파일 3개 생성

(PostgreSQL 연결 없어도 마이그레이션 생성은 가능함)

- [ ] **Step 4: 최종 Commit**

```powershell
git add -A
git commit -m "feat: Phase 1 scaffold complete — dotnet build and test pass"
```

---

## 성공 기준 체크리스트

- [ ] `dotnet build` — 전체 솔루션 오류 없음
- [ ] `dotnet test` — 5개 PlayerSimulation 테스트 통과
- [ ] `dotnet ef migrations add InitialCreate` — 마이그레이션 생성 성공
- [ ] GameServer 바이너리 시작 가능 (`dotnet run --project apps/game-server/...`)
