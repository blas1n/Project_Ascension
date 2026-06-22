# Game Server Architecture

## 기본 결정

* **프로세스:** 순수 C# 콘솔 앱 (Unity 의존성 없음)
* **전송:** ENet-CSharp (Reliable UDP)
* **직렬화:** MessagePack-CSharp
* **물리:** BEPUphysics2
* **게임 로직:** `packages/GameSimulation/` 참조

Unity 없이 `dotnet build` / `dotnet test` 가능. 모든 게임 로직은 테스트 가능한 순수 C#으로 작성한다.

---

## 역할

게임 서버는 실시간 게임 상태의 권위를 가진다.

* 플레이어 이동/위치 판정
* 전투 히트 판정 및 데미지 계산
* 몬스터 AI 실행
* 발견 후보 감지
* 사망 판정

게임 서버는 영속화를 직접 하지 않는다. 중요한 이벤트는 API 서버에 보고한다.

---

## 통신 흐름

```
Unity Client  →(ENet)→  Game Server  →(HTTP)→  ASP.NET Core API  →  PostgreSQL
              ←(ENet)←               
```

* Client → Game Server: 플레이어 입력
* Game Server → Client: 월드 상태 스냅샷, 이벤트
* Game Server → API: 영속화 이벤트 (계약 진행, 발견, 사망 등)

---

## 서버 루프

```
Fixed Tick: 20Hz (이동/상태 동기화)
Combat Tick: 64Hz (히트 판정)
```

각 틱에서: 입력 수집 → 시뮬레이션 업데이트 → 상태 스냅샷 발송

---

## 프로젝트 구조

```
apps/game-server/
  ProjectAscension.GameServer/
    Program.cs
    GameLoop.cs               — 메인 루프 (틱 관리)
    SessionManager.cs         — 플레이어 세션 관리
    ZoneInstance.cs           — 단일 존 인스턴스
    Network/
      ENetTransport.cs        — ENet-CSharp 래퍼
      PacketHandler.cs        — 메시지 디스패치
      PacketSender.cs         — 상태 스냅샷 발송
    ApiReporter.cs            — API 서버 HTTP 보고
```

---

## packages/GameSimulation/

게임 서버와 Unity 클라이언트가 공유하는 순수 C# 시뮬레이션 로직.

```
packages/GameSimulation/
  ProjectAscension.GameSimulation/
    Physics/
      PhysicsWorld.cs         — BEPUphysics2 래퍼
      CharacterBody.cs        — 캐릭터 물리 바디
    Player/
      PlayerSimulation.cs     — 이동, 점프, 회피 판정
      PlayerState.cs          — 서버 권위 플레이어 상태
    Combat/
      CombatSimulation.cs     — 히트 판정, 데미지 계산
      ProjectileSimulation.cs — 발사체 궤적
      StatusEffect.cs         — 화상, 출혈, 둔화
    Monsters/
      MonsterSimulation.cs    — AI 상태 머신 (Idle/Chase/Attack/Dead)
      MonsterState.cs
    Discovery/
      BehaviorCounter.cs      — 행동 빈도/패턴 카운터
      DiscoveryCandidateEvaluator.cs — 발견 후보 평가
```

Unity는 `GameSimulation`의 타입을 참조해 클라이언트 예측(prediction)과 보정(reconciliation)에 활용한다.

---

## 메시지 타입 (packages/Contracts/GameMessages/)

Unity와 게임 서버가 공유하는 패킷 정의.

### Client → Server

```csharp
PlayerInputMessage     — 이동 벡터, 점프, 회피, 공격 입력
UseAbilityMessage      — 술식/스킬 사용
```

### Server → Client

```csharp
WorldStateMessage      — 존 내 전체 엔티티 스냅샷
DeltaStateMessage      — 변경된 엔티티만 전송
GameEventMessage       — 발견, 사망, 계약 완료 등 이벤트
```

### Server → API

```csharp
ContractProgressEvent
DiscoveryCandidateEvent
PlayerDeathEvent
MonsterKilledEvent
ItemDroppedEvent
```

---

## 클라이언트 예측 / 서버 보정

FPS 특성상 클라이언트는 입력을 즉시 예측해 렌더링한다. 서버 상태와 차이가 생기면 보정한다.

* Client: 입력 즉시 로컬 시뮬레이션 (GameSimulation 참조)
* Server: 동일 시뮬레이션 로직으로 결과 확정
* Client: 서버 스냅샷 수신 시 차이 보정 (reconciliation)

GameSimulation 패키지가 양쪽에서 동일하므로 시뮬레이션 결과가 일치한다.

---

## Vertical Slice 범위

* 단일 존 (Frontier_01)
* 로컬 실행 (서버/클라이언트 같은 머신)
* 동시 접속: 개발/테스트 수준
* 인증 없음 (actorId 직접 전달)

미구현:
* 존 간 이동
* 서버 인스턴스 관리
* 매치메이킹
* 스케일 아웃

---

## 테스트 전략

GameSimulation은 순수 C#이므로 xUnit으로 직접 테스트한다.

```csharp
[Fact]
void PlayerMovement_Jump_AppliesVelocity() { ... }

[Fact]
void CombatSimulation_Hitscan_DetectsCollision() { ... }

[Fact]
void DiscoveryCandidateEvaluator_RepeatJump_TriggersCandidate() { ... }
```

게임 서버 통합 테스트는 로컬 ENet 연결로 수행한다.
