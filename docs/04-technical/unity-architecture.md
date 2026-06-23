# Unity Architecture

## 기본 결정

* **엔진:** Unity 6 + URP
* **입력:** Unity New Input System
* **카메라:** Cinemachine
* **DI:** VContainer
* **실시간 전송:** ENet-CSharp
* **직렬화:** MessagePack-CSharp
* **기본 시점:** FPS (TPS 선택적)

Unity는 **렌더링 + 입력 셸**이다. 게임 로직은 `packages/GameSimulation/`에 있고 Unity는 이를 참조한다.

---

## 폴더 구조

```
apps/client_unity/Assets/
  Scripts/
    Core/
    Player/
    Equipment/
    Weapons/
    Combat/
    Monsters/
    Discovery/
    Contracts/
    City/
    API/
    UI/
  Data/
    ScriptableObjects/
      Weapons/
      Monsters/
      Regions/
  Prefabs/
  Scenes/
  Materials/
  Animations/
```

---

## 씬 구조

```
Bootstrap.unity   — 앱 초기화. RootLifetimeScope 등록. 씬 로드 관리.
City.unity        — 도시 허브. 계약 게시판, 상점, 귀환 지점.
Frontier_01.unity — 프론티어 존. 전투, 탐험, 발견.
```

---

## VContainer 구조

### RootLifetimeScope

씬 간 공유 서비스. Bootstrap 씬에서 등록되며 앱 종료까지 유지된다.

* `ApiClient`
* `ContractService`
* `DiscoveryService`
* `CharacterStateService`

### CityLifetimeScope

도시 씬 전용. RootLifetimeScope를 부모로 가진다.

* `ContractBoardPresenter`
* `InventoryPresenter`

### FrontierLifetimeScope

프론티어 씬 전용. RootLifetimeScope를 부모로 가진다.

* `PlayerController`
* `CombatSystem`
* `BehaviorTracker`
* `MonsterSpawner`

---

## 핵심 클래스

### Core

```
Bootstrap.cs              — 앱 시작점. 씬 로드 순서 관리.
RootLifetimeScope.cs      — VContainer 루트 등록.
```

### Player

```
PlayerController.cs       — 이동, 점프, 회피 통합. Input → Movement → Animation.
PlayerInputHandler.cs     — New Input System 래퍼. 입력 이벤트 발행.
PlayerMovement.cs         — CharacterController 기반 이동 로직.
PlayerCamera.cs           — Cinemachine Brain 제어. FPS/TPS 전환.
```

### Equipment

```
Loadout.cs                — 현재 좌우 슬롯 상태 관리.
EquipmentSlot.cs          — LEFT / RIGHT 슬롯. IEquippable 보유.
IEquippable.cs            — 장착 가능한 오브젝트 인터페이스.
```

### Weapons

```
WeaponBase.cs             — MonoBehaviour + IEquippable. 공통 무기 동작.
SwordWeapon.cs            — 근접 공격. 히트박스 기반.
BowWeapon.cs              — 발사체 생성. 차징 지원.
PistolWeapon.cs           — 레이캐스트 또는 발사체.
CatalystWeapon.cs         — 술식 시전. DiscoveryService 연동.
```

### Combat

```
CombatSystem.cs           — 데미지 계산, 히트 처리 통합.
HitReceiver.cs            — IDamageable 구현체. 체력 관리.
Projectile.cs             — 발사체 이동 및 충돌.
StatusEffectController.cs — 화상, 출혈, 둔화 적용/해제.
```

### Monsters

```
MonsterBase.cs            — 체력, 팩션, 드롭. HitReceiver 보유.
MonsterStateMachine.cs    — Idle → Chase → Attack → Dead 전환.
MeleeMonster.cs           — 근접형. 돌진 패턴.
RangedMonster.cs          — 원거리형. 발사체 사용.
EliteMonster.cs           — 정예형. 복합 패턴. 특수 드롭.
MonsterSpawner.cs         — 스폰 포인트 관리. 리스폰 타이머.
```

### Discovery

```
DiscoveryService.cs       — 발견 후보 평가. API 보고 담당.
BehaviorTracker.cs        — 행동 빈도/패턴 카운터 관리.
DiscoveryContext.cs       — 현재 환경, 장비, 보유 지식 스냅샷.
DiscoveryNotification.cs  — 발견 발생 시 UI 알림 발행.
```

### Contracts

```
ContractService.cs        — 수주, 진행, 완료 처리. API 연동.
ContractBoardPresenter.cs — 게시판 UI 데이터 바인딩.
```

### GameServer (실시간)

```
GameServerClient.cs       — ENet-CSharp 연결 관리. 연결/재연결 처리.
PacketSender.cs           — PlayerInputMessage 등 직렬화 후 송신.
PacketReceiver.cs         — WorldStateMessage 수신 후 디스패치.
ClientReconciliation.cs   — 서버 스냅샷 수신 시 로컬 상태 보정.
```

### API (영속화)

```
ApiClient.cs              — HttpClient 래퍼.
ContractApiService.cs     — /api/contracts 엔드포인트 호출.
DiscoveryApiService.cs    — /api/discoveries 엔드포인트 호출.
CharacterApiService.cs    — /api/characters 엔드포인트 호출.
ItemApiService.cs         — /api/items 엔드포인트 호출.
```

---

## ScriptableObject 데이터

```
WeaponData.cs    — 무기 스탯, 슬롯 타입, 발견 가중치.
MonsterData.cs   — 체력, 공격력, 드롭 테이블, 티어.
RegionData.cs    — 지역 환경 태그, 위험도, 발견 후보 목록.
```

런타임 수치를 코드에 하드코딩하지 않는다. 모두 ScriptableObject에서 읽는다.

---

## 설계 규칙

* MonoBehaviour는 씬 오브젝트 생명주기만 담당한다. 게임 로직은 `packages/GameSimulation/`에 있다.
* Inspector 직접 참조 최소화. VContainer로 주입한다.
* 실시간 통신은 `GameServer/` 레이어를 통한다. 영속화는 `API/` 레이어를 통한다.
* `packages/Contracts` DTO와 `packages/GameSimulation` 타입을 Unity에서 그대로 사용한다. Unity 전용 모델을 별도로 만들지 않는다.
* Unity PhysX는 렌더링용 시각적 보정에만 사용한다. 판정 기준은 서버의 BEPUphysics2다.

---

## Vertical Slice 범위

구현:
* Bootstrap → City → Frontier 씬 전환
* PlayerController (이동, 점프, 회피, FPS 카메라)
* Loadout (좌우 슬롯)
* WeaponBase + 4종 무기
* MeleeMonster, RangedMonster, EliteMonster
* BehaviorTracker + DiscoveryService (기본 행동 발견)
* ContractService (Hunt, Survey, Collection)
* ApiClient + 각 ApiService

미구현:
* TPS 전환
* 상태 이상 전체
* 발견 그래프 시각화
* 조직/정착지 UI
