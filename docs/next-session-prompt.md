# 다음 세션 프롬프트

---

## 컨텍스트

Project_Ascension 구현 첫 세션이다. 문서 작업은 완료된 상태이며, 이 세션부터 실제 코드를 작성한다.

**반드시 먼저 읽을 문서:**
- `docs/04-technical/repo-structure.md` — 전체 프로젝트 구조
- `docs/04-technical/architecture.md` — Vertical Slice 범위
- `docs/04-technical/game-server-architecture.md` — 게임 서버 구조
- `docs/04-technical/unity-architecture.md` — Unity 클래스 구조
- `docs/04-technical/backend-architecture.md` — API 서버 구조
- `docs/04-technical/domain-model.md` — 도메인 모델 (C# 엔티티 기준)
- `CLAUDE.md` — 개발 원칙 및 구현 순서

---

## 확정된 기술 스택

| 영역 | 기술 |
|---|---|
| Unity 클라이언트 | Unity 6 + URP, VContainer, Cinemachine, New Input System |
| 실시간 전송 | ENet-CSharp |
| 직렬화 | MessagePack-CSharp |
| 게임 서버 | 순수 C# 콘솔 앱 + BEPUphysics2 |
| 영속화 API | ASP.NET Core 8 + EF Core + Dapper |
| DB | PostgreSQL |
| 공유 패키지 | C# (.NET 클래스 라이브러리) |

---

## 핵심 아키텍처 원칙

1. **Unity는 렌더링 + 입력 셸이다.** 게임 로직은 `packages/GameSimulation/`에 순수 C#으로 작성한다.
2. **게임 서버는 Unity 의존성이 없다.** `dotnet build` / `dotnet test`가 동작해야 한다.
3. **서버 권위 아키텍처.** 전투 판정, 이동 권위는 게임 서버에 있다.
4. **Domain은 다른 패키지를 참조하지 않는다.** 순환 참조 금지.
5. **문서가 코드보다 우선한다.** 구현이 문서와 충돌하면 문서 기준으로 ADR 작성 후 확인.

---

## 구현 시작점 (Phase 1)

CLAUDE.md의 구현 순서를 따른다.

### Step 1 — 솔루션 Scaffold

다음 구조를 생성한다:

```
ProjectAscension.sln
apps/
  api/ProjectAscension.Api/
  game-server/ProjectAscension.GameServer/
packages/
  Domain/ProjectAscension.Domain/
  Contracts/ProjectAscension.Contracts/
  GameSimulation/ProjectAscension.GameSimulation/
  Shared/ProjectAscension.Shared/
```

참조 관계:
- `GameServer` → `GameSimulation`, `Contracts`, `Domain`
- `Api` → `Domain`, `Contracts`, `Shared`
- `GameSimulation` → (없음, 순수 C#)
- `Contracts` → (없음)
- `Domain` → (없음)

### Step 2 — packages/GameSimulation

BEPUphysics2 기반 PlayerSimulation부터 시작한다.

```csharp
// 구현 대상
PlayerSimulation.cs      // 이동, 점프, 회피
PlayerState.cs           // 서버 권위 상태
PhysicsWorld.cs          // BEPUphysics2 래퍼
```

xUnit 테스트를 함께 작성한다.

### Step 3 — packages/Domain

`domain-model.md` 기준으로 MVP 필수 엔티티를 먼저 작성한다:
- Actor, Character, NPC
- Contract, ContractMarketAccessPoint
- Item, Equipment, Loadout
- Region, Monster, MonsterSpecies
- Discovery, DiscoveryCandidate, DiscoveryProgress

### Step 4 — apps/api

ASP.NET Core + EF Core scaffold.
`backend-architecture.md` 참조.
MVP 엔드포인트: Characters, Contracts, Items, Loadouts, Discoveries.

### Step 5 — apps/game-server

ENet-CSharp 서버 루프 + GameSimulation 연결.
`game-server-architecture.md` 참조.

### Step 6 — apps/client-unity

Unity 프로젝트는 `apps/client-unity/`에 생성한다.
`unity-architecture.md` 참조.
PlayerController부터 시작한다.

---

## 주의사항

- MMO 시스템(조직, 주권, 지식 경제, 정착지 성장, World Will)은 구현하지 않는다. 데이터 모델만 정의한다.
- AI는 이름/설명/로어 생성에만 사용한다. 판정 로직에 AI를 사용하지 않는다.
- Unity 프로젝트는 솔루션과 별개로 `apps/client-unity/`에 위치한다. C# 패키지는 DLL 참조로 연결한다.
