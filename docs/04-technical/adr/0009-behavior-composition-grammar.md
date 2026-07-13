# ADR 0009 — 행동은 문법으로 관측한다 (Behavior Composition Grammar)

> **Note (ADR 0012):** the `Dodge` verb / `Dodging` quality referenced below is removed — there is
> no dodge button; evasion is movement.

## Status

Accepted (2026-07-13). ADR 0008(Synthesis)을 **일반화하여 흡수**한다.

## 문제

ADR 0008로 "무기 융합"을 관측하게 됐다. 그런데 그걸 만들고 나서 코드를 보니 이렇게 되어 있었다:

| 합성 개념 | 구현 |
|---|---|
| 회피 직후 공격 | `BehaviorDeriver.IsDodgeAttack` — 전용 창 |
| 연속 점프 | `BehaviorDeriver.Jumped` — 전용 스트릭 |
| 공중 공격 | `GameplayEvents.AirAttacked` — 전용 이벤트 |
| 차징 공격 | `GameplayEvents.ChargedAttacked` — 전용 이벤트 |
| 무기 융합 | `SynthesisDeriver` — 전용 창 |

**다섯 개의 일회용 관측기.** 조준을 넣으면 여섯 번째가, 패링을 넣으면 일곱 번째가 생긴다.

이것은 **ADR 0007을 낳은 바로 그 안티패턴**이다. 그때의 문제는 "이중 점프를 위해 엔진에
`ExtraJumps` 필드를 박은 것이 AI 동적 생성이라는 전제를 스스로 무너뜨린다"는 것이었다. 지금
행동 관측 계층에서 똑같은 일이 벌어지고 있었다. 새 행동 개념마다 엔진을 고쳐야 한다면, 그것은
확장이 아니라 누적이다. 그리고 이 계층은 **발견의 입력**이므로, 여기가 굳으면 발견의 다양성이
관측기의 개수만큼으로 상한이 잡힌다.

## 결정

특수 케이스를 지우고, **하나의 행위 스트림 + 소수의 일반 연산자**로 바꾼다.

### 행위 (Act)

플레이어가 한 일 = `(동사, 한정자, 시각, 성질)`

- 동사: `jump`, `dodge`, `attack`, `land`, …
- 한정자: 공격이면 **무엇으로** — `arcane`, `firearm`, `bow`, `melee`
- 성질(동시에 참이던 것): `airborne`, `charged`, `blocking`, `aiming`, `moving`, `dodging`

**토큰** = 한정자 ?? 동사. 그래서 "권총 사격"은 `firearm`, "점프"는 `jump`가 된다.

### 연산자 (4개)

| 연산자 | 의미 | 흡수하는 기존 특수 케이스 |
|---|---|---|
| `Fuse:a>b` | a 직후 b, **거의 동시에** (좁은 창) | 무기 융합 (ADR 0008) |
| `Seq:a>b` | a 직후 b (넓은 창) | 회피 직후 공격 |
| `While:a@s` | 성질 s를 띤 채 a | 공중 공격, 차징 공격 |
| `Chain:a` | a의 연쇄 | 연속 점프 |

**Fuse와 Seq의 분리가 "미묘한 타이밍"이다.** 0.1초 안에 엮은 것과 0.5초 만에 이어붙인 것은
다른 숙련이고, 다른 발견이어야 한다.

### 왜 이게 더 나은가

- **표현력이 오히려 늘었다.** 기존 `DodgeAttack`은 "회피 후 공격"이었다. 이제는
  `Seq:dodge>firearm`과 `Seq:dodge>melee`가 **다른 행동**이다 — 구르며 쏘는 것과 구르며 베는
  것은 같은 숙련이 아니다. 특수 케이스를 지우면서 해상도가 올라갔다.
- **새 행위가 공짜다.** 조준(`aim`), 패링(`parry`), 벽차기를 추가할 때 관측기를 만들지 않는다.
  스트림에 act를 넣으면 문법이 알아서 `Seq:aim>firearm`, `While:firearm@aiming`을 만든다.
  **엔진 수정 없이 발견 공간이 열린다** — 이것이 ADR 0007이 이펙트에 한 일이다.
- **조합 폭발은 시드 문제가 아니다.** 채점은 **접두**로 한다(`Fuse:`/`Seq:`/`While:`/`Chain:`).
  새 조합에 DB 행을 심을 필요가 없다.

## AI 경계 (ADR 0002 유지)

무엇을 했고, 무엇과 무엇을 어떤 간격으로 엮었는지는 **전부 결정론적으로 관측**된다. AI는 여전히
사실을 정하지 않는다 — 관측된 조합이 **무엇처럼 보일지**만 만든다.

## 결과

- `BehaviorDeriver`, `SynthesisDeriver`, `AirAttacked`, `ChargedAttacked`, 그리고
  `BehaviorKind`의 합성 멤버들(`DodgeAttack`/`AirAttack`/`RepeatedJump`/`ChargedAttack`)은 **삭제**된다.
  전부 문법의 특수 사례였다.
- 남는 raw 행동은 `Jump`/`Dodge`/`MeleeAttack`/`RangedAttack` — 순수한 "무엇을 몇 번" 뿐이다.
- 과적합 방어: 우리는 `arcane>firearm`을 열거하지 않는다. **"임의의 a>b"**를 관측할 뿐이다.
