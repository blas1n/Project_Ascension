# Event Flow

## 목적

이 문서는 게임 내 주요 행동이 어떤 이벤트로 기록되고, 어떤 시스템으로 전달되는지 정의한다. 모든 중요한 변화는 이벤트로 남긴다.

---

## 기본 원칙

클라이언트는 결과를 확정하지 않는다. 게임 서버는 실시간 결과를 판정한다. API 서버는 영속 세계 상태를 확정한다.

---

## 주요 이벤트 종류

### Contract Events

* ContractCreated
* ContractAccepted
* ContractProgressed
* ContractCompleted
* ContractFailed
* ContractDelegated

### Combat Events

* MonsterSpawned
* MonsterDamaged
* MonsterKilled
* PlayerDied
* ItemDropped
* ItemLooted

### Discovery Events

* DiscoveryCandidateDetected
* DiscoveryCreated
* KnowledgeCreated
* KnowledgeRelationCreated

### Settlement Events

* FrontierSecured
* InfrastructureImproved
* SettlementStageChanged
* MigrationOccurred
* SettlementDamaged

### World Will Events

* MonsterRespawnTriggered
* DisasterTriggered
* CorruptionSpread
* SettlementPressured

---

## 계약 수주 흐름

1. Actor가 계약 시장 객체와 상호작용한다.
2. API 서버가 조회 가능한 계약 목록을 반환한다.
3. Actor가 계약을 수락한다.
4. ContractAccepted 이벤트가 생성된다.
5. Contract 상태가 Assigned로 변경된다.

---

## 계약 완료 흐름

1. 게임 서버가 목표 달성 이벤트를 감지한다.
2. API 서버에 ContractProgressed 이벤트를 보고한다.
3. API 서버가 조건을 검증한다.
4. 조건 충족 시 ContractCompleted 이벤트를 생성한다.
5. 보상을 지급한다.
6. ReputationEvent를 생성한다.

---

## 발견 흐름

1. 게임 서버가 발견 후보를 감지한다.
2. API 서버에 DiscoveryCandidateDetected를 전송한다.
3. API 서버가 최초 발견 여부를 판정한다.
4. 최초 발견이면 DiscoveryCreated 이벤트를 생성한다.
5. 거래 가능한 지식이면 KnowledgeCreated 이벤트를 생성한다.
6. 기존 지식과 연결되면 KnowledgeRelationCreated 이벤트를 생성한다.

---

## 사망 흐름

1. 게임 서버가 사망을 판정한다.
2. API 서버에 PlayerDied 또는 NPCDied 이벤트를 보낸다.
3. API 서버가 사망 패널티를 계산한다.
4. 계약 실패 또는 직위 상실 여부를 판정한다.
5. DeathEvent를 저장한다.
6. 필요한 경우 Position Contract 승계가 발생한다.

---

## 정착지 성장 흐름

1. 플레이어가 개척지 관련 계약을 완료한다.
2. InfrastructureImproved 이벤트가 생성된다.
3. 인프라 성숙도가 갱신된다.
4. 성장 조건을 만족하면 SettlementStageChanged 이벤트가 발생한다.
5. NPC 유입 조건을 만족하면 MigrationOccurred 이벤트가 발생한다.

---

## 재앙 흐름

1. World Will Simulation이 문명 압력을 계산한다.
2. 특정 지역의 위험도가 임계값을 넘는다.
3. DisasterTriggered 이벤트가 생성된다.
4. 몬스터 또는 괴수가 생성된다.
5. 정착지 피해, NPC 이탈, 계약 중단 등이 발생할 수 있다.

---

## 핵심 원칙

상태 변경은 이벤트로 남긴다. 이벤트는 세계의 역사이다. 중요한 사건은 명성, 영향력, 발견 기록으로 이어진다.
