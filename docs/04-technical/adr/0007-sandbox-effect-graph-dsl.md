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

## 진행 현황 (2026-07)

- **Phase 1 ✅** (#115): 노드 모델 + 비용 + 검증기.
- **AI 생성 + 시뮬 + 벽타기 작곡 ✅** (#116): 프롬프트→JSON→파서→검증, 라이브 LLM 입증.
- **Phase 2 (이동 런타임) ✅**: 2a(#117) 서버가 스킬마다 그래프 생성·저장·서빙 /
  2b(#118) 클라 인터프리터(`Effects/` + 의존성 없는 `MiniJson`/`EffectGraphReader`,
  `MovementCapability`) + **`PassiveEffect.ExtraJumps` 제거** / 2c(#119) 벽 접촉 감지 →
  벽타기 인게임. 더블점프·벽타기 = 그래프 구동, 엔진 특수케이스 0.
- **Phase 4a ✅** (#120): 발현을 그래프에서 도출(`ManifestationFromGraph`), primitive 폴백.
- **Phase 4b ⏳** (아래 설계): 공격 **실행** 이관.

## Phase 4b — 공격 실행 이관 (상세 설계)

### 문제

현 오펜시브 실행은 `SkillResolver`(GameSimulation.Combat)가 **평면 프리미티브**를 전투 수치로
해석한다: `single(Projectile/Beam) / area(Area) / DoT / spread(Chain·Fork·Pierce) / control /
leech / shield / homing`. 그런데 현재 그래프의 공격 어휘는 `Emit{Projectile/Beam/Burst/Nova} +
Damage + Control` 뿐이라 **DoT·연쇄·관통·유도·흡혈이 표현 불가**. 이대로 `SkillResolver`를 그래프로
바꾸면 **전투 다양성이 후퇴** → CLAUDE.md "discovery 시스템 보호" 위반. 따라서 **어휘 확장이 선행**.

### 결정 1 — 관심사 분리형 어휘 확장

프리미티브는 한 노드에 magnitude/range를 뭉쳐(예: Projectile이 데미지 *와* spread 동시 기여)
있었다. 그래프는 더 깔끔하게 **역할별 노드**로 분리한다(엔진 소유 화이트리스트에 추가):

- **`Emit(delivery, tier)`** — 전달 *형태* + 기본 데미지. `Projectile/Beam` = 단일 타겟,
  `Burst/Nova` = 광역(area). (기존)
- **`Damage(tier)`** — 추가 데미지(중첩). (기존)
- **`Dot(tier, duration)`** — 지속 피해. (신규)
- **`Spread(tier)`** — 추가 타겟 수(연쇄/관통/분열을 *수치상* 하나로 수렴; 형태 구분은 VFX가
  전달로 표현). (신규)
- **`Homing(tier)`** — 유도(수치 없음, 투사체 거동/VFX만). (신규)
- **`Control(kind, tier)`** — Knockback/Slow/Stun. (기존)
- **`Ward(Leech, tier)`** — 명중 시 흡혈(오펜시브 시퀀스 안에서). Shield/Barrier/Heal은 방어.

프리미티브 → 그래프 매핑:

| primitive | 그래프 노드 |
|---|---|
| Projectile / Beam | `Emit(Projectile/Beam)` (단일) |
| Area | `Emit(Burst/Nova)` (광역) |
| Damage 크기 | `Emit.tier` + `Damage(tier)` |
| DamageOverTime | `Dot(tier, duration)` |
| Chain / Fork / Pierce | `Spread(tier)` |
| Homing | `Homing(tier)` |
| Knockback / Slow / Stun | `Control(kind, tier)` |
| Leech | `Ward(Leech, tier)` |
| Shield / Barrier | `Ward(Shield/Barrier, tier)` |
| Dash / Blink | `Impulse` (이동, Phase 2 완료) |

비용(엔진 소유, 잠정): `Dot (tier+1)*3`, `Spread (tier+1)*2`, `Homing 2(고정)`. 검증기에 반영.

### 결정 2 — 파리티 인터프리터

`GraphSkillResolver.Resolve(EffectNode graph, int availableTargets, CombatTuning) → SkillResolution`
를 신설한다. 그래프의 트리거 child를 walk 하며 `SkillResolver`와 **동일한 수식**으로
single/area/dotPerTick/dotTicks/spread/control/leech/shield 를 누적 → 같은 `SkillResolution` 산출.
숫자는 `CombatTuning`(DB 구동) 그대로 사용하므로 **결정론·서버=클라 동일** 유지. 목표는 동등한
그래프에 대해 프리미티브 경로와 **수치 파리티**(테스트로 고정).

### 결정 3 — VFX를 그래프에서 구동

VFX(`SkillVfx`)는 이미 `delivery`(형태) + name(색) + powerCost(강도)로 조립된다. 그래프의
`Emit.Delivery`가 별도 `delivery` 문자열을 **대체**한다: 클라는 파싱된 그래프의 Emit에서 형태를
읽고, `Homing`이 있으면 유도 투사체 거동을 켠다. `delivery` 문자열은 파생/레거시로 남긴다.

### 결정 4 — 무회귀 스위치

`SkillCaster` 발사 경로: **그래프가 있으면** `GraphSkillResolver` + 그래프 기반 VFX, **없으면**
기존 `SkillResolver`(프리미티브) + `delivery` 문자열. 새 discovery는 그래프를 갖고(Phase 2a),
레거시/그래프-실패 스킬은 프리미티브로 계속 동작. 프리미티브 경로는 전면 스위치가 검증될
때까지 유지.

### 결정 5 — 프롬프트 + 다양성 시뮬 게이트

`EffectGraphPrompt`에 신규 노드(Dot/Spread/Homing) 어휘·사용 지침 추가. `EffectGraphSimulation`을
확장해 **오펜시브 그래프의 다양성**(전달·DoT·연쇄·유도·컨트롤 분포)이 프리미티브 시절만큼
넓은지 측정 — 파리티가 시뮬로 확인되기 전엔 전면 스위치하지 않는다.

### 하위 단계

- **4b-1**: 어휘 확장(`Dot`/`Spread`/`Homing`) + 비용/검증기 + JSON 직렬화/파서(서버·클라) + 테스트.
- **4b-2**: `GraphSkillResolver`(파리티 인터프리터) + 프리미티브 경로와의 수치 파리티 테스트.
- **4b-3**: `SkillCaster` 스위치(그래프 우선) + 그래프 기반 VFX(Emit/Homing).
- **4b-4**: 프롬프트 어휘 + 오펜시브 다양성 시뮬. **플레이테스트 게이트** 후 프리미티브 폴백 축소.

4b-1~4b-4 완료(#121~#124). 런타임 실행은 헤드리스 시뮬(모드 A/B/C, #125~#127) + 스트레스 감사(#128)로
검증됨 — tier 단조·dead-skill 0·이동 누수 0.

## Phase 4c — 프리미티브 축소 (설계)

### 동기

프리미티브는 여전히 **(1) discovery마다 두 번째 LLM 호출**(`LlmSkillComposer`)로 그래프와 별도로
생성되고, **(2) ~7개 서브시스템**이 소비한다. 프리미티브 *생성*을 멈추면 **LLM 비용 절반 + "두 표현
발산" 버그류 제거**(반복된 중복-스킬 이슈의 뿌리 — 이름/프리미티브/그래프가 각자 놀던 문제). 단,
모든 소비처에 그래프 경로가 생기기 전엔 못 뺀다.

### 소비처 → 그래프 등가물 (전부 저난이도, 노드에 동일 로직 미러링)

| 소비처 | 프리미티브 로직 | 그래프 등가물 | 상태 |
|---|---|---|---|
| 전투 | `SkillResolver` | `GraphSkillResolver` | ✅ 4b |
| 발현 | `SkillManifest` | `ManifestationFromGraph` | ✅ 4a |
| 이동 | (제거됨) | `MovementCapability` | ✅ 2b |
| 패시브 방어 | `PassiveResolver`(Shield/Barrier/Leech) | **`GraphPassiveResolver`**(Ward 노드) | 신규 |
| 포커스 비용 | `FocusCost`(Σ mag+range+dur) | **그래프 Cost**(skill.PowerCost) | 신규 |
| 지식 가치 | `KnowledgeValuation` | **그래프 Cost** | 신규 |
| VFX 액센트 | `SkillVfx.PlayImpactModifiers` | **`EffectGraphQuery` 플래그**(spread/control/leech/dot) | 신규 |
| 스킬 요약 | `SkillSummary` | 그래프 요약 또는 이미 있는 AI `Description` | 신규 |
| 중복 제거(dedup) | `CompositionPipeline.KindSignature`(프리미티브) | **그래프 정규 직렬화 서명** | 신규 |

### 하위 단계 (각 PR 가산·무회귀)

- **4c-1**: 그래프 등가물 신설 — `GraphPassiveResolver`, 그래프 포커스/가치, `EffectGraphQuery` 액센트
  플래그, 그래프 요약. 그래프 없으면 프리미티브로 폴백(동작 변화 없음).
- **4c-2**: 각 소비처를 그래프 우선으로 스위치(전투/발현처럼). 그래프 스킬은 런타임에서 프리미티브
  **완전 미사용**.
- **4c-3**: 프리미티브 *생성* 중단 — 컴포지션에서 `LlmSkillComposer` 호출 제거, **그래프가 유일한
  작곡 산출물**. 이때 그래프는 *필수*가 되어 무효 시 defer(결정론 폴백 없음, ADR 0002). dedup은
  그래프 서명으로 이관. `PrimitivesJson`은 레거시 행 위해 nullable 유지. 프리미티브 리졸버는
  **graphless-레거시 폴백으로만** 잔존.
- **4c-4** (슬라이스 이후): 레거시 프리미티브-전용 행을 그래프로 마이그레이션(또는 잔존 허용) 후
  프리미티브 시스템 전면 삭제.

### 핵심 결정 (확인 필요)

**4c-3에서 그래프를 필수화하고 프리미티브 생성을 멈춘다** — 새 스킬은 그래프-전용, 프리미티브는
레거시 폴백으로만 축소. 근거: 시뮬이 그래프 생성 신뢰도를 입증(생성 5/5·6/6 valid, 실행 무회귀).
리스크: 그래프 생성이 실패하면 discovery가 defer(프리미티브 폴백 없음) — 단 이는 프리미티브도
이미 그랬던 defer 정책과 동일(ADR 0002). 슬라이스에선 4c-4(레거시 삭제)까지는 가지 않는다.
