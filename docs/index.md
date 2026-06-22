# Documentation Index

Project_Ascension 문서 모음.

이 문서는 프로젝트의 전체 구조와 권장 읽기 순서를 정의한다.

---

# 프로젝트 개요

Project_Ascension은 문명 확장을 중심으로 하는 MMOFPS이다.

플레이어는 문명을 확장한다.

NPC는 문명을 유지한다.

세계의 의지는 문명을 침식한다.

---

핵심 루프.

도시

↓

계약

↓

원정

↓

전투

↓

발견

↓

귀환

↓

문명 성장

---

# 읽기 순서

새로운 기여자는 아래 순서대로 문서를 읽는다.

---

## 1단계. 비전

프로젝트 철학.

### 문서

* 00-vision/vision.md
* 01-world/world-constitution.md

### 핵심 질문

* 이 게임은 무엇을 목표로 하는가?
* 무엇을 만들지 않을 것인가?

---

## 2단계. 세계관

세계와 문명.

### 문서

* 01-world/world-bible.md
* 01-world/world-will.md
* 01-world/monster.md

### 핵심 질문

* 세계는 어떻게 구성되는가?
* 왜 문명이 붕괴했는가?
* 왜 플레이어가 필요한가?

---

## 3단계. 핵심 시스템

핵심 게임 시스템.

### 문서

* 02-systems/contracts.md
* 02-systems/contract-market-system.md
* 02-systems/discovery.md
* 02-systems/knowledge-economy.md
* 02-systems/combat-framework.md
* 02-systems/reputation-and-influence.md
* 02-systems/death-and-inheritance.md
* 02-systems/progression-model.md

### 핵심 질문

* 플레이어는 어떻게 성장하는가?
* 발견은 어떻게 발생하는가?
* 계약은 어떻게 사회를 구성하는가?

---

## 4단계. 조직과 권력

사회와 권력 구조.

### 문서

* 02-systems/organizations.md

### 핵심 질문

* 국가는 무엇인가?
* 상단은 무엇인가?
* 권력은 어떻게 획득되고 상실되는가?

---

## 5단계. 정착지

개척과 도시 성장.

### 문서

* 02-systems/settlement-evolution.md

### 핵심 질문

* 정착지는 어떻게 성장하는가?
* NPC는 왜 유입되는가?
* AI는 어디까지 관여하는가?

---

## 6단계. 기술 설계

기술 아키텍처.

### 문서

* 04-technical/architecture.md
* 04-technical/repo-structure.md
* 04-technical/unity-architecture.md
* 04-technical/backend-architecture.md
* 04-technical/domain-model.md
* 04-technical/db-schema-draft.md
* 04-technical/event-flow.md
* 04-technical/ai-boundaries.md

### 핵심 질문

* 무엇을 룰 엔진이 결정하는가?
* 무엇을 AI가 설명하는가?
* 시스템은 어떻게 구현되는가?

---

## 7단계. Vertical Slice

첫 번째 플레이 가능한 버전.

### 문서

* 06-roadmap/vertical-slice.md
* 06-roadmap/alpha-test.md
* 06-roadmap/mvp.md

### 핵심 질문

* 무엇을 검증할 것인가?
* 무엇을 아직 만들지 않을 것인가?

---

# 참고 문서

읽기 순서에 포함되지 않는 세부 참고 자료.

## 게임플레이

* 03-gameplay/equipment-system.md
* 03-gameplay/first-hour-experience.md

## 발견 시스템 상세

* 02-systems/discovery-examples.md
* 02-systems/spellcraft-and-knowledge.md

## 경제

* 02-systems/economy-balance.md
* 02-systems/itemization.md
* 02-systems/crafting.md

## 계약 예시

* 02-systems/contract-example.md

## 아트

* 05-art/art-direction.md
* 05-art/vertical-slice-art-and-tech-decisions.md

---

# 현재 상태

## 세계관

완료

---

## 핵심 시스템

완료

---

## 도메인 설계

완료

---

## Vertical Slice 범위

확정

---

## Claude Code 구현

준비 완료

---

# 핵심 원칙

## 문명이 주인공이다

플레이어는 중요하다.

하지만 궁극적으로 이 게임의 주인공은 문명이다.

---

## 계약이 사회를 만든다

모든 권한.

모든 책임.

모든 직위.

모든 의무.

계약으로 표현된다.

---

## 발견이 성장을 만든다

레벨은 존재하지 않는다.

발견이 성장이다.

---

## 플레이어가 역사를 만든다

플레이어는 세상을 소비하지 않는다.

플레이어는 세상을 변화시킨다.

---

## 세계는 저항한다

문명은 확장한다.

세계의 의지는 이를 되돌리려 한다.

이 긴장이 게임의 핵심 동력이다.
