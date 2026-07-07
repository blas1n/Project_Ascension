# ADR 0007 — 샌드박스 결정론 이펙트 그래프 (스킬 효과 DSL)

## 상태
Proposed — 착수 (Phase 1: 코어 모델 + 검증)

## 맥락

현재 발견 스킬은 **평면 프리미티브 목록**(예: `Dash x2, Projectile x1`)으로 조합된다.
엔진이 각 프리미티브의 결정론적 메커니즘을 구현하고, AI는 *어떤* 프리미티브를 *얼마나*
넣을지만 고른다.

한계: 스킬의 **구조/흐름**(트리거, 시퀀스, 조건)을 표현할 수 없다. 그래서 "더블 점프"처럼
*특정 입력에 반응하는* 효과를 만들려면 엔진에 `ExtraJumps` 같은 **일회성 특수 필드 + 분기**를
박아야 했다(PassiveEffect / PlayerSimulation). 새 개념이 나올 때마다 엔진 기능을 추가해야 하니,
**AI 동적 생성이 "엔진이 미리 만든 효과를 고르기"로 축소**된다 — 발견의 고유성(이 프로젝트의
핵심)이 무뎌진다.

## 결정

스킬 효과를 **결정론적 이펙트 그래프(작은 AST)**로 표현한다. AI는 **그래프의 구조**(어떤
노드를, 어떤 트리거/순서로)를 생성하고, **엔진은 노드의 메커니즘과 숫자를 소유**한다.
ADR 0002 유지: AI는 개념/구조를, 숫자·판정·밸런스는 서버 결정론.

### 노드 어휘 (엔진 구현, 화이트리스트)

- **Trigger(kind, child)** — `OnCast` / `OnJumpInAir` / `OnDodge` / `OnHit` / `Continuous`.
  child가 *언제* 실행되는가.
- **Sequence(children[])** — 결합자.
- **Emit(delivery, tier)** — projectile / beam / burst / nova (공격 전달).
- **Impulse(direction, tier)** — 이동 임펄스 (`Up` / `Forward` / `Aim`).
- **Damage / Control(kind) / Shield / Barrier / Heal / Leech (tier)** — 효과.

어휘는 확장 가능하며, 각 노드는 엔진이 결정론적으로 해석한다.

### 예

- **더블 점프** = `Trigger(OnJumpInAir, Impulse(Up, t))` — 특수 `ExtraJumps` 없이, 점프-인-에어에
  이동 임펄스. 세기(1단/다단)는 tier·조합에서 파생.
- **화염탄** = `Trigger(OnCast, Sequence[ Emit(Projectile, t), Damage(t) ])`.
- **항시 방어막** = `Trigger(Continuous, Shield(t))`.
- **회피 반격** = `Trigger(OnDodge, Sequence[ Impulse(Forward, t), Damage(t) ])`.

### 경계 (ADR 0002 준수)

- **숫자는 tier → 엔진 튜닝**으로 결정론 산출. AI는 tier(개념적 강도 등급)만; 실제 수치는
  DB/엔진 소유.
- **파워 예산**: `Σ(노드 비용) ≤ budget`. 검증기가 강제.
- **샌드박스**: 그래프는 *데이터*(임의 코드 아님). 엔진 인터프리터가 결정론 실행(서버 = 클라).
- **검증**: 노드 수 상한, 화이트리스트, 예산, 구조 규칙(발현별 — 예: Passive는 `Continuous`/
  이동 트리거만, 공격 `Emit` 금지). 실패 시 **defer**(결정론 폴백 없음, ADR 0002).

### 발현 재정의 (단계적)

발현(Weapon / Command / Passive)은 그래프의 **최상위 트리거 + 지배 노드**로 자연 도출된다:

- `OnCast` + 공격 `Emit` + **마법 컨텍스트** → **Weapon** (장착·발사; ADR 0005).
- `OnCast`(핫키) + 비마법 공격 / `Control` → **Command**.
- `Trigger(OnJumpInAir / OnDodge / Continuous)` → **Passive** (이동/항시 역량).

→ `SkillManifest`의 카테고리-집계 하드코딩을 그래프 구조 판정으로 대체(Phase 2).

## 단계

1. **(이번, Phase 1) 코어**: 노드 모델 + 결정론 비용 + 검증기. 서버(SkillForge) 측. 테스트.
   기존 평면 프리미티브 시스템은 **그대로 유지**(회귀 없음) — 그래프는 나란히 도입.
2. **이동/패시브 이관**: 더블 점프를 `Trigger(OnJumpInAir, Impulse)` 그래프로. 클라 인터프리터
   (GameSimulation, 결정론)에서 실행. 특수 `ExtraJumps` / `MovementCapabilityCatalog` 제거.
3. **AI 그래프 생성**: 프롬프트가 그래프(JSON)를 생성 → 파서 → 검증 → 인터프리터. 평면
   프리미티브를 그래프로 대체.
4. **공격/무기 이관**: `Emit`/`Damage` 그래프로. `SkillResolver`를 그래프 인터프리터로 수렴.

## 대안

- **평면 프리미티브 유지(현재)** — 구조 표현 불가, 특수케이스 누적. 기각(이 ADR의 동기).
- **완전 자유 스크립트(Lua 등)** — 결정론·샌드박스·검증·서버 권위 리스크. 기각.

## 결과

AI가 스킬의 **구조/로직**을 조합 → 진짜 동적 생성. 엔진은 노드 어휘·숫자·결정론을 소유.
"같은 조합 + 다른 행동 → 다른 스킬"이 *구조* 수준에서 성립하고, 새 개념도 노드 조합으로
표현되어 **엔진 특수케이스가 필요 없다**. 권위 경계는 ADR 0004/0002 그대로(프로덕션에선
서버가 그래프 실행을 검증).
