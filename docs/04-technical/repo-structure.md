# Repo Structure

## 목적

이 문서는 Claude Code가 monorepo를 생성할 때 따라야 할 폴더 구조와 책임 경계를 정의한다.

---

## 최상위 구조

```txt
/
  apps/
  packages/
  docs/
  infra/
  scripts/
  .devcontainer/
  docker-compose.yml
  README.md
```

---

## apps

실행 가능한 애플리케이션을 둔다.

```txt
/apps
  /client-unity
  /game-server
  /api
  /admin
```

### apps/client-unity

Unity 클라이언트. 초기에는 Unity 프로젝트 전체가 이 폴더에 위치한다.

담당:

* FPS 이동
* 조준
* 전투 입력
* 장비 장착
* 계약 UI
* 인벤토리 UI
* 발견 알림
* 월드 렌더링

### apps/game-server

실시간 게임 서버. 초기에는 Unity/Photon 기반 구조를 우선 검토한다.

담당:

* 세션 관리
* 플레이어 위치 동기화
* 몬스터 스폰
* 몬스터 AI
* 피격 판정
* 전투 결과 이벤트 생성

### apps/api

영속 세계 백엔드.

담당:

* Account
* Character
* Actor
* NPC
* Contract
* Item
* Discovery
* Knowledge
* Organization
* Settlement

초기 스택:

* TypeScript
* NestJS
* PostgreSQL
* Redis
* Prisma 또는 Drizzle

### apps/admin

개발자/운영자용 웹 도구. 초기 스택: Next.js 또는 Vite + React.

담당:

* 계약 조회
* 캐릭터 조회
* 지역 상태 조회
* 몬스터 상태 조회
* 정착지 성숙도 조회
* 테스트 데이터 생성

---

## packages

공유 코드와 도메인 로직을 둔다.

```txt
/packages
  /shared
  /domain
  /schemas
  /contracts
  /items
  /world
  /discovery
  /knowledge
  /settlements
  /config
```

### packages/shared

범용 유틸리티.

* id helper
* date helper
* result type
* error type
* pagination type

### packages/domain

핵심 도메인 타입.

* Actor
* Contract
* Organization
* Item
* Discovery
* Knowledge
* Region
* Monster
* Settlement

### packages/schemas

API 요청/응답 검증 스키마. 권장: Zod, JSON Schema.

### packages/contracts

계약 엔진.

담당:

* 계약 타입 정의
* 계약 상태 전이
* 완료 조건 평가
* 실패 조건 평가
* 보상 계산 인터페이스

MVP 계약: Collection, Survey, Hunt.

### packages/items

아이템과 인벤토리 도메인.

담당:

* Item type
* Equipment type
* Loadout
* Inventory
* Ownership

### packages/world

지역, 문명권, 위험도, 세계의 의지 관련 도메인.

담당:

* Region
* Civilization
* DangerLevel
* WorldWillEvent

### packages/discovery

발견 시스템.

담당:

* DiscoveryRecord
* 최초 발견 판정
* 발견 이벤트 처리
* Knowledge 생성 트리거

### packages/knowledge

지식 경제. 초기 MVP에서는 모델만 정의하고, 실제 거래는 이후 구현한다.

담당:

* Knowledge
* KnowledgeLicense
* LicenseTransaction
* Royalty calculation

### packages/settlements

정착지와 인프라 시스템. 초기 MVP에서는 최소 상태만 구현한다.

담당:

* Settlement
* Infrastructure
* Infrastructure maturity
* MigrationEvent
* SettlementStyleProfile

### packages/config

공통 환경 설정.

담당:

* env parsing
* service URLs
* feature flags

---

## docs

기획과 기술 문서를 둔다.

```txt
/docs
  /00-vision
  /01-world
  /02-systems
  /03-gameplay
  /04-technical
  /05-art
  /06-roadmap
```

---

## infra

인프라와 로컬 개발 환경을 둔다. 초기 MVP에서는 Terraform은 비워둔다.

```txt
/infra
  /docker
  /postgres
  /redis
  /terraform
```

---

## .devcontainer

개발 컨테이너 설정.

목표:

* Node.js
* pnpm
* PostgreSQL client
* Redis client
* Unity 관련 도구는 로컬 또는 별도 처리

---

## docker-compose.yml

초기 개발 서비스: postgres, redis, api, admin. Unity 클라이언트는 docker-compose에서 제외할 수 있다.

---

## 패키지 매니저

권장: pnpm workspace.

---

## TypeScript 경계

API와 packages는 TypeScript 기반이다. Unity 클라이언트는 C# 기반이다. Unity와 TypeScript 사이에는 API schema 또는 generated client를 사용한다.

---

## Claude Code 작업 원칙

Claude Code는 한 번에 전체 repo를 만들지 않는다.

작업 단위:

1. workspace scaffold
2. docs 배치
3. api scaffold
4. domain package
5. contract package
6. item package
7. discovery package
8. settlement package
9. 테스트 작성
10. Unity 연동은 별도 단계

---

## MVP 구현 순서

### Step 1

Monorepo scaffold. pnpm workspace. basic tsconfig. lint/test 설정.

### Step 2

API scaffold. health endpoint. database connection.

### Step 3

Domain model. Actor, Contract, Item, Region.

### Step 4

Contract engine MVP. Collection, Survey, Hunt.

### Step 5

Inventory and loadout. 양손 2슬롯.

### Step 6

Discovery MVP. 최초 발견 기록.

### Step 7

Settlement stub. 정착지 상태와 인프라 성숙도 최소 구현.

### Step 8

Unity prototype integration. 계약 조회, 계약 수락, 아이템 획득, 계약 완료.

---

## 금지 사항

초기부터 다음을 구현하지 않는다.

* 국가 전체 시스템
* 종교 전체 시스템
* 외교
* 상속
* 로열티 자동 정산
* 완전한 AI 도시 생성
* 대규모 경제 시뮬레이션

문서는 남겨두되 구현은 MVP 이후로 미룬다.
