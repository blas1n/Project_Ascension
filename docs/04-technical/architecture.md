# Architecture - Vertical Slice

## 목적

이 문서는 Project_Ascension의 첫 번째 플레이 가능한 Vertical Slice를 구현하기 위한 기술 아키텍처를 정의한다. 최종 MMO 아키텍처가 아니다.

---

## 핵심 원칙

Vertical Slice는 작아야 한다.

목표:

* 전투 검증
* 계약 루프 검증
* 발견 시스템 검증
* 원정과 귀환 루프 검증

목표가 아닌 것:

* MMO 서버
* 대규모 경제
* 조직 운영
* 정착지 시뮬레이션
* 세계의 의지 시뮬레이션

---

## 개발 방식

초기 구현은 싱글플레이 또는 로컬 호스트 기반으로 시작한다. 멀티플레이는 Vertical Slice 성공 이후 검토한다.

---

## 권장 구조

```txt
/
  apps/
    client_unity/
    api/
    admin/

  packages/
    domain/
    contracts/
    discovery/
    items/
    schemas/
    shared/

  docs/
  infra/
```

---

## client_unity

Unity 클라이언트이다. Vertical Slice의 핵심 구현 대상이다.

### 담당

* 플레이어 이동
* FPS/TPS 카메라
* 양손 2슬롯 장비
* 기본 전투
* 몬스터 전투
* 계약 UI
* 발견 알림
* 귀환/완료 루프

---

## api

간단한 백엔드 서버이다. 초기에는 로컬 개발 서버로 충분하다.

### 담당

* 계약 저장
* 아이템 저장
* 발견 기록 저장
* 캐릭터 상태 저장
* 간단한 상점 데이터

---

## admin

선택 사항이다. 초기에는 없어도 된다. 필요 시 테스트 데이터 확인용으로 만든다.

---

## packages/domain

핵심 타입 정의.

* Actor
* Contract
* Item
* Discovery
* Knowledge
* Region
* Monster

---

## packages/contracts

계약 엔진 MVP.

### 구현

* 토벌 계약
* 조사 계약
* 수집 계약
* 상태 전이
* 완료 조건 검증

---

## packages/discovery

발견 시스템 MVP.

### 구현

* 발견 후보
* 발견 진행도
* 발견 기록
* 간단한 계보

---

## packages/items

아이템과 장비.

### 구현

* 인벤토리
* 장비
* 양손 2슬롯
* 기본 무기

---

## packages/schemas

API 요청/응답 검증 스키마. Zod 사용 권장.

---

## 데이터 저장

PostgreSQL을 사용한다.

---

## 실시간 서버

Vertical Slice에서도 별도 game-server를 구현한다. 전투 판정과 이동 권위는 game-server에 있다. Unity 클라이언트는 렌더링과 입력만 담당한다.

서버 권위를 처음부터 적용하는 이유:
* VS → MMO 전환 시 전투 레이어 재설계 비용 방지
* Unity 의존성 없는 순수 C# 서버는 `dotnet test` 가능 (Claude Code 기반 개발에 필수)
* Headless Unity보다 인프라 비용 낮음

상세 구조는 `game-server-architecture.md` 참조.

---

## AI 사용

Vertical Slice에서는 AI 호출을 선택 사항으로 둔다. AI 없이도 게임은 동작해야 한다.

### AI 허용

* 발견 이름 생성
* 발견 설명 생성

### AI 금지

* 전투 판정
* 발견 판정
* 계약 판정
* 보상 계산

---

## Vertical Slice 구현 순서

### Step 1

Unity 프로젝트 생성. 플레이어 이동, 카메라, 점프, 회피 구현.

### Step 2

양손 2슬롯 장비 시스템 구현. 검, 활, 권총, 초급 마도 촉매 추가.

### Step 3

기본 몬스터 3종 구현. 근접형, 원거리형, 정예형.

### Step 4

계약 시스템 MVP 구현. 토벌, 조사, 수집.

### Step 5

도시 계약 게시판 UI 구현. 계약 수주, 진행, 완료.

### Step 6

발견 시스템 MVP 구현. 행동 발견, 환경 발견, 지식 발견 일부.

### Step 7

귀환 루프 구현. 원정 후 도시 복귀, 계약 완료, 보상 지급.

### Step 8

발견 기록 UI 구현. 플레이어가 발견한 기술과 계보 확인.

---

## 제외 사항

Vertical Slice에서 구현하지 않는다.

* 멀티플레이
* MMO 서버
* 조직
* 주권
* 정착지 성장
* 지식 거래
* 로열티
* 학파
* 재앙
* 세계의 의지 시뮬레이션
* 대규모 경제
* PVP

---

## 성공 기준

다음이 검증되면 성공이다.

* 전투가 반복할 만한가
* 계약을 받고 원정 가는 흐름이 자연스러운가
* 발견이 플레이어 행동을 바꾸는가
* 다른 발견을 찾고 싶어지는가
* 도시로 귀환하는 이유가 생기는가

---

## 다음 단계

Vertical Slice 성공 이후에만 다음을 추가한다.

* 멀티플레이
* 지식 경제
* 정착지 성장
* 조직 시스템
* 재앙 시스템
* 세계의 의지 시뮬레이션
