# ADR 0003 — 게임플레이 관찰: 입력이 아니라 실행에서, 단일 릴레이로

> **Note (ADR 0012):** the `Dodge`/`Dodged` signal referenced below is removed — there is no dodge
> button; evasion is movement.

- 상태: 채택됨
- 날짜: 2026-06-25
- 맥락: 발견 시스템이 플레이어 행동을 관찰한다. 관찰 조건은 계속 늘어난다(점프·회피·공격에서 시작해 콤보·환경·타겟·처치 등으로). 처음엔 점프 발견을 입력 이벤트(`JumpPressed`)에 걸었더니, 공중에서 스페이스 연타·쿨다운 중 회피 입력에도 진행도가 쌓이는 중복 버그가 났다.

## 결정

### 원칙: 입력(intent) ≠ 실행(fact)

관찰 이벤트는 **그 행동을 소유한 도메인 시스템이, 실제로 실행되는 순간에**(접지·쿨다운 게이트 통과 후) 발행한다. 입력은 의도일 뿐 사실이 아니다. 발견은 사실만 관찰한다.

### 구조: 사실 버스 1개, 관찰자 N개

사실은 한 번 발행되고, 임의 개수의 관찰자가 소비한다. `GameplayEvents` 단일 버스가
플레이어 실행 사실과 월드 사실을 모두 싣는다(기존 `CombatEvents`는 여기에 흡수).

```
[사실 발행]            플레이어 실행          월드
 (소유 시스템,          Jumped / Dodged       MonsterKilled (MonsterBase, 사망)
  소비자 모름)          Attacked(isMelee)     SampleCollected (Collectible, 획득)
                                             MarkerSurveyed (SurveyPoint, 도달)
        │
        ├───────────────────────────┬───────────────────────┐
        ▼                           ▼                         ▼
 [BehaviorTracker]            [ContractService]         (미래: 업적/분석)
  사실→BehaviorKind 매핑,      사실→진행도(Hunt/           동일 버스 구독만
  콤보 파생, 맥락 조립          Collection/Survey)          하면 됨
        │                       (순수 관찰자)
        ▼
 [DiscoveryEngine]
  Observation 평가
```

같은 `MonsterKilled` 하나로 계약 진행 + "몬스터 N 처치" 발견 + 미래 업적을 동시에 먹일 수
있다 — 사실이 한곳에서 나오므로 관찰자 추가가 발행처를 건드리지 않는다.

- **도메인 시스템은 발견을 모른다** — 자기 실행 사실만 발행(`GameplayEvents`). VFX/SFX/분석도 같은 사실을 재사용 가능. 발행 지점이 자연히 실행 위치에 고정돼 입력층에 잘못 걸 수 없다.
- **릴레이 1개만 발견을 안다** — 콤보 상관(dodge→attack 윈도우)과 맥락 조립이 한곳에 모인다. 발견 지식이 게임플레이 전반에 흩어지지 않는다.
- **`Observation` 구조체 페이로드** — `Kind` + `Context`(태그 집합). 새 조건(환경·타겟·크기)은 릴레이의 맥락 조립과 `Observation` 필드에만 추가되고, `도메인 → 릴레이 → 엔진` 시그니처는 안 바뀐다.

### 새 관찰 추가 절차

1. 도메인 시스템에 `GameplayEvents` 사실 1개 추가(실행 지점에서 발행).
2. 릴레이에 사실 → `BehaviorKind`/콤보 매핑 한 줄 + 필요한 맥락 태그.
3. 카탈로그(나중엔 트리거 함수, ADR 0002 핵심 4) 항목 추가.

## 영향

- 입력 기반 발행 금지 — 실행 사실 기반만 허용.
- 사실 버스는 `GameplayEvents` 하나로 통일(`CombatEvents` 흡수·삭제). 플레이어+월드 사실 단일 스트림.
- `PlayerController`는 발견/행동을 전혀 모른다(움직임 글루만). `PlayerCombat`은 콤보를 모른다(발사 사실만). `Collectible`/`SurveyPoint`/`MonsterBase`는 계약을 모른다(사실만 발행).
- `ContractService`는 `ReportSurvey/ReportCollect` push API를 버리고 순수 관찰자가 됐다.
- 발견·계약 외 소비자(업적·연출·분석)도 동일한 `GameplayEvents` 버스를 구독하면 된다.
