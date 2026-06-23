# 다음 세션 프롬프트 — Phase 1: Unity Player Controller

---

## 현재 상태

PR #1 (feat/phase1-scaffold) 머지 완료. `main`에 다음이 포함돼 있다:

- `packages/Domain` — 엔티티, 열거형, 인터페이스
- `packages/Contracts` — DTO, GameMessages (Domain 참조)
- `packages/GameSimulation` — PlayerSimulation, PhysicsWorld, xUnit 테스트
- `packages/Shared` — Result\<T\>, Error
- `apps/api` — ASP.NET Core + EF Core + PostgreSQL
- `apps/game-server` — ENet-CSharp + GameSimulation 연결

---

## 이 세션의 목표

CLAUDE.md Phase 1 구현: **PlayerController (이동, 점프, 회피, FPS 카메라)**

Unity는 렌더링 + 입력 셸이다. 실제 이동 판정은 `packages/GameSimulation/PlayerSimulation`에 있다.

---

## 사전 조건 (수동 작업 — Claude Code 불가)

세션 시작 전 사용자가 완료해야 한다:

1. Unity Hub에서 **Unity 6 LTS** 프로젝트 생성
   - 경로: `apps/client_unity/`
   - 템플릿: 3D (URP)

---

## 패키지 공유 전략 (스크립트 없음)

### UPM Local Package

`packages/` 하위 4개 패키지를 Unity에서 소스 수준으로 직접 참조한다.  
DLL 빌드 없음. 소스 변경이 즉시 반영된다.

각 패키지에 `package.json` + `.asmdef` 추가:

```
packages/Domain/ProjectAscension.Domain/
  package.json
  ProjectAscension.Domain.asmdef
packages/Contracts/ProjectAscension.Contracts/
  package.json
  ProjectAscension.Contracts.asmdef   ← Domain 참조
packages/GameSimulation/ProjectAscension.GameSimulation/
  package.json
  ProjectAscension.GameSimulation.asmdef  ← allowUnsafeCode: true
packages/Shared/ProjectAscension.Shared/
  package.json
  ProjectAscension.Shared.asmdef
```

### BepuPhysics 문제와 해결

`PhysicsWorld.cs`는 BepuPhysics에 의존하며 서버 전용이다.  
Unity 클라이언트는 BepuPhysics가 필요 없다. (클라이언트 예측은 `PlayerSimulation`의 순수 수식만 사용)

`PhysicsWorld.cs` 상단에 조건부 컴파일 추가:

```csharp
#if !UNITY_5_3_OR_NEWER
// BepuPhysics 의존 코드
#endif
```

이렇게 하면 GameSimulation.asmdef가 BepuPhysics를 참조하지 않아도 Unity에서 컴파일된다.

---

## Step 1 — package.json 파일 생성

각 패키지 폴더에 추가:

**packages/Domain/ProjectAscension.Domain/package.json**
```json
{
  "name": "com.projectascension.domain",
  "version": "0.1.0",
  "displayName": "ProjectAscension Domain",
  "unity": "6000.0"
}
```

**packages/Contracts/ProjectAscension.Contracts/package.json**
```json
{
  "name": "com.projectascension.contracts",
  "version": "0.1.0",
  "displayName": "ProjectAscension Contracts",
  "unity": "6000.0"
}
```

**packages/GameSimulation/ProjectAscension.GameSimulation/package.json**
```json
{
  "name": "com.projectascension.gamesimulation",
  "version": "0.1.0",
  "displayName": "ProjectAscension GameSimulation",
  "unity": "6000.0"
}
```

**packages/Shared/ProjectAscension.Shared/package.json**
```json
{
  "name": "com.projectascension.shared",
  "version": "0.1.0",
  "displayName": "ProjectAscension Shared",
  "unity": "6000.0"
}
```

---

## Step 2 — .asmdef 파일 생성

**ProjectAscension.Domain.asmdef**
```json
{
  "name": "ProjectAscension.Domain",
  "rootNamespace": "ProjectAscension.Domain",
  "references": [],
  "allowUnsafeCode": false
}
```

**ProjectAscension.Contracts.asmdef**
```json
{
  "name": "ProjectAscension.Contracts",
  "rootNamespace": "ProjectAscension.Contracts",
  "references": ["ProjectAscension.Domain"],
  "allowUnsafeCode": false
}
```

**ProjectAscension.GameSimulation.asmdef**
```json
{
  "name": "ProjectAscension.GameSimulation",
  "rootNamespace": "ProjectAscension.GameSimulation",
  "references": [],
  "allowUnsafeCode": true
}
```

**ProjectAscension.Shared.asmdef**
```json
{
  "name": "ProjectAscension.Shared",
  "rootNamespace": "ProjectAscension.Shared",
  "references": [],
  "allowUnsafeCode": false
}
```

---

## Step 3 — PhysicsWorld.cs 수정

```csharp
#if !UNITY_5_3_OR_NEWER
using BepuPhysics;
// ... 나머지 using

namespace ProjectAscension.GameSimulation.Physics;

public sealed class PhysicsWorld : IDisposable
{
    // ... 기존 코드 전체
}

// NarrowPhaseCallbacks, PoseIntegratorCallbacks 포함
#endif
```

---

## Step 4 — Unity manifest.json 수정

`apps/client_unity/Packages/manifest.json`:

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": ["jp.hadashikick"]
    }
  ],
  "dependencies": {
    "com.projectascension.domain":        "file:../../../packages/Domain/ProjectAscension.Domain",
    "com.projectascension.contracts":     "file:../../../packages/Contracts/ProjectAscension.Contracts",
    "com.projectascension.gamesimulation":"file:../../../packages/GameSimulation/ProjectAscension.GameSimulation",
    "com.projectascension.shared":        "file:../../../packages/Shared/ProjectAscension.Shared",
    "com.unity.inputsystem":              "1.11.2",
    "com.unity.cinemachine":              "3.1.3",
    "jp.hadashikick.vcontainer":          "1.16.9",
    "com.unity.render-pipelines.universal": "17.0.3"
  }
}
```

---

## Step 5 — Unity 폴더 구조 생성

`unity-architecture.md` 기준:

```
apps/client_unity/Assets/
  Scripts/
    Core/
      ProjectAscension.Core.asmdef
    Player/
      ProjectAscension.Player.asmdef   ← GameSimulation, Input System 참조
    Equipment/
    Weapons/
    Combat/
    Monsters/
    Discovery/
    Contracts/
    City/
    Network/
    API/
    UI/
  Data/
    ScriptableObjects/
      Weapons/
      Monsters/
      Regions/
  Prefabs/
  Scenes/
```

---

## Step 6 — Core 스크립트

**Bootstrap.cs** — Bootstrap 씬 진입점. RootLifetimeScope 확인 후 City 씬 로드.

**RootLifetimeScope.cs** — VContainer 루트. 씬 간 공유 서비스 등록.
```csharp
// 등록: ApiClient, ContractService(Unity), DiscoveryService, CharacterStateService
```

씬:
- `Bootstrap.unity` — Bootstrap + RootLifetimeScope
- `City.unity` — 빈 씬 (이 세션에서는 placeholder)
- `Frontier_01.unity` — PlayerController 테스트 씬

---

## Step 7 — Player 스크립트 (Phase 1 핵심)

### PlayerInputHandler.cs

New Input System 래퍼. `PlayerInputActions.inputactions` 에셋에서 읽는다.

```csharp
public event Action<Vector2> MoveInput;
public event Action JumpPressed;
public event Action DodgePressed;
```

### PlayerMovement.cs

`CharacterController` + `PlayerSimulation.ApplyInput()` 기반.

- `PlayerSimulation.ApplyInput()`으로 다음 상태를 계산한다 (클라이언트 예측)
- `CharacterController.Move()`는 렌더링 위치 동기화 전용
- 서버 보정(reconciliation)은 ClientReconciliation이 담당 (이 세션에서는 stub)

### PlayerCamera.cs

Cinemachine `CinemachineCamera` 제어.

- Player Transform을 Follow/LookAt 타겟으로 설정
- 마우스 입력 → yaw/pitch 회전

### PlayerController.cs

3개를 통합하는 MonoBehaviour. VContainer `[Inject]` 사용.

```csharp
[Inject] private PlayerInputHandler _input;
[Inject] private PlayerMovement _movement;
[Inject] private PlayerCamera _camera;
```

---

## Step 8 — FrontierLifetimeScope.cs

```csharp
// FrontierLifetimeScope : LifetimeScope
// 등록: PlayerController, PlayerInputHandler, PlayerMovement, PlayerCamera
```

---

## Step 9 — ScriptableObject 데이터

`PlayerData.cs` (ScriptableObject):
```csharp
public float MoveSpeed;
public float JumpVelocity;
public float Gravity;
```

수치를 코드에 하드코딩하지 않는다. `PlayerMovement`가 `PlayerData`를 주입받아 사용한다.

---

## 성공 기준

Frontier_01 씬에서 Play:
- WASD 이동 가능
- Space 점프 가능
- 마우스 FPS 카메라 회전 가능
- Console 에러 없음

---

## 핵심 원칙

1. **PlayerMovement는 `PlayerSimulation.ApplyInput()`을 호출한다.** CharacterController 자체 물리 계산을 쓰지 않는다.
2. **VContainer `[Inject]`를 사용한다.** Inspector 직접 참조 최소화.
3. **수치는 ScriptableObject에서 읽는다.** 하드코딩 금지.
4. **스크립트 없음.** DLL 빌드, 복사 스크립트를 만들지 않는다.

---

## 참고 문서

- `docs/04-technical/unity-architecture.md`
- `docs/04-technical/game-server-architecture.md`
- `docs/04-technical/repo-structure.md`
- `CLAUDE.md`
