# Domain Model

## 목적

이 문서는 Project_Ascension의 핵심 개념을 구현 가능한 도메인 모델로 정리한다. 세계관 문서가 철학을 정의한다면, 이 문서는 코드와 데이터베이스의 기준을 정의한다.

---

## 핵심 설계 원칙

### Actor 중심 설계

플레이어와 NPC는 모두 Actor이다. 계약, 조직, 아이템, 지식, 발견, 사망 기록의 주체는 Actor이다.

### Contract 중심 사회

직위, 의뢰, 시민권, 소유권, 위임, 라이선스는 모두 Contract로 표현된다.

### Discovery와 Knowledge 분리

Discovery는 개인의 발견 기록이다. Knowledge는 사회화된 지식 자산이다.

### Organization은 지속된다

개인은 죽을 수 있다. 조직은 남는다. 직위는 계약으로 승계된다.

---

## Account

실제 유저 계정이다. 게임 세계 내부 존재가 아니다.

### Fields

* id
* email
* createdAt

---

## Character

플레이어가 조작하는 세계 내부 인물이다.

### Fields

* id
* accountId
* name
* originRegionId
* currentRegionId
* status
* createdAt

---

## Actor

플레이어 캐릭터와 NPC를 통합하는 추상 주체이다. 계약, 아이템, 조직, 지식, 발견 기록은 Actor를 기준으로 연결한다.

### Types

* PLAYER
* NPC

### Fields

* id
* type
* characterId
* npcId
* createdAt

---

## NPC

세계 내부의 비플레이어 인물이다. NPC는 플레이어와 거의 동일한 시스템을 사용하지만, 사망하면 복귀하지 않는다.

### Fields

* id
* name
* homeRegionId
* currentRegionId
* alive
* createdAt

---

## Contract

세계의 기본 단위이다. 퀘스트, 직위, 신분, 소유권, 위임, 라이선스는 모두 Contract로 표현된다.

### Fields

* id
* kind
* purpose
* issuerActorId
* issuerOrganizationId
* assigneeActorId
* assigneeOrganizationId
* parentContractId
* status
* title
* description
* rewardJson
* conditionsJson
* failureConditionsJson
* delegationAllowed
* maxDelegationDepth
* startsAt
* expiresAt
* completedAt
* failedAt
* createdAt

### Contract Kind

* TASK
* RECURRING
* POSITION
* OWNERSHIP
* CITIZENSHIP
* LICENSE
* INHERITANCE

### Contract Purpose

* HUNT
* SURVEY
* COLLECTION
* ESCORT
* DEFENSE
* TRANSPORT
* RESEARCH
* GOVERNANCE
* SETTLEMENT
* TRADE
* EDUCATION

### Contract Status

* DRAFT
* OPEN
* ASSIGNED
* COMPLETED
* FAILED
* CANCELLED
* EXPIRED

---

## ContractMarketAccessPoint

계약 시장에 접근할 수 있는 세계 내부 객체이다. 계약 발주 권한은 위치가 아니라 Actor의 권한으로 결정된다. 이 객체는 단지 계약 시장 접근 수단이다.

### Examples

* 도시 게시판
* 길드 게시판
* 관청 게시판
* 국경 소초 게시판
* 황궁 집무실
* 상단 사무소

### Fields

* id
* regionId
* organizationId
* type
* name
* createdAt

---

## Item

소유 가능한 모든 경제 자산이다. 지도, 무기, 술식 문서, 계약서, 몬스터 부산물은 모두 Item이 될 수 있다.

### Fields

* id
* type
* templateId
* ownerActorId
* ownerOrganizationId
* currentRegionId
* metadataJson
* createdAt

### Item Types

* RESOURCE
* WEAPON
* MAGIC_TOOL
* MAP
* KNOWLEDGE_DOCUMENT
* MONSTER_MATERIAL
* CONTRACT_DOCUMENT
* EQUIPMENT
* CONSUMABLE

---

## Equipment

장착 가능한 Item이다. 플레이어는 기본적으로 좌우 2개 슬롯을 가진다.

### Fields

* itemId
* equipmentType
* slotType

### Equipment Types

* WEAPON
* MAGIC_TOOL
* SHIELD
* BOW
* FIREARM
* CATALYST

### Slot Types

* LEFT
* RIGHT
* EITHER
* TWO_HAND

---

## Loadout

Actor의 현재 장비 상태이다.

### Fields

* actorId
* leftItemId
* rightItemId
* updatedAt

---

## Discovery

최초 발견 기록이다. Discovery는 역사 자산이며 삭제되거나 변경되지 않는다.

### Fields

* id
* type
* discovererActorId
* regionId
* targetId
* title
* description
* discoveredAt

### Discovery Types

* REGION
* MAP
* SKILL
* COMMAND
* RUIN
* RESOURCE_NODE
* MONSTER_SPECIES
* KNOWLEDGE
* RECIPE
* MOVEMENT_TECHNIQUE

---

## DiscoveryCandidate

발견 가능성이 있는 행동 패턴 또는 조건이다. 행동, 환경, 장비, 지식, 상황의 조합으로 정의된다.

### Fields

* id
* candidateKey
* category
* requiredContextJson
* requiredProgress
* rarity
* createdAt

### Categories

* BEHAVIOR
* ENVIRONMENT
* COMBAT
* KNOWLEDGE
* HYBRID

---

## DiscoveryProgress

Actor가 특정 발견 후보에 대해 얼마나 접근했는지 기록한다.

### Fields

* id
* actorId
* discoveryCandidateId
* progress
* metadataJson
* updatedAt

---

## Knowledge

Discovery가 사회화된 지식 자산이다. Knowledge는 거래, 교육, 연구, 라이선스의 대상이 된다.

### Fields

* id
* discoveryId
* type
* title
* description
* creatorActorId
* ownerActorId
* ownerOrganizationId
* createdAt

### Knowledge Types

* MAGIC_FORMULA
* WEAPON_COMMAND
* MOVEMENT_TECHNIQUE
* MAP_DATA
* RUIN_LOCATION
* RECIPE
* MONSTER_INFO
* RESOURCE_INFO
* ORGANIZATION_METHOD
* SETTLEMENT_METHOD

---

## KnowledgeLineage

지식 계보를 표현한다. 동일한 부모 지식에서 여러 다른 지식이 파생될 수 있다.

### Fields

* id
* name
* description
* rootKnowledgeId
* createdAt

### Examples

* 화염 압축 계열
* 수류 제어 계열
* 고속 기동 계열
* 생체 변이 계열

---

## KnowledgeRelation

지식 간 관계를 표현한다.

### Fields

* id
* parentKnowledgeId
* childKnowledgeId
* lineageId
* relationType
* createdAt

### Relation Types

* COMBINATION
* EVOLUTION
* ENVIRONMENT
* BEHAVIOR
* DERIVATION
* SCHOOL_FOUNDATION

---

## School

계보가 축적되어 형성된 지식 공동체이다. 학파는 교육, 연구, 라이선스, 산업화를 수행할 수 있다.

### Fields

* id
* name
* founderActorId
* founderOrganizationId
* rootLineageId
* ownerOrganizationId
* createdAt

---

## KnowledgeLicense

Knowledge를 사용할 수 있는 권리이다. 지식 자체가 아니라 지식 사용권을 거래한다.

### Fields

* id
* knowledgeId
* ownerActorId
* ownerOrganizationId
* sellerActorId
* sellerOrganizationId
* canUse
* canResell
* canCopy
* canTeach
* canResearch
* royaltyBps
* parentLicenseId
* createdAt

---

## LicenseTransaction

지식 라이선스 거래 기록이다.

### Fields

* id
* licenseId
* buyerActorId
* buyerOrganizationId
* sellerActorId
* sellerOrganizationId
* price
* royaltyPaid
* createdAt

---

## Organization

국가, 상단, 교단, 군단, 길드, 도시 운영체 등 모든 조직이다. 조직은 자산, 계약, 지식, 하위 조직을 보유할 수 있다.

### Fields

* id
* name
* type
* parentOrganizationId
* sovereign
* immutableDoctrine
* createdAt

### Organization Types

* STATE
* COMPANY
* RELIGION
* MILITARY
* CITY
* GUILD
* ACADEMY
* SCHOOL
* ADMINISTRATION

---

## OrganizationMembership

Actor가 조직에 소속되는 관계이다.

### Fields

* id
* actorId
* organizationId
* roleContractId
* joinedAt

---

## OrganizationShare

조직 지분이다. 지분은 수익 배분권, 영향력, 경영권을 제공할 수 있다.

### Fields

* id
* ownerActorId
* ownerOrganizationId
* targetOrganizationId
* shareBps
* votingPowerBps
* createdAt

---

## Region

세계의 공간 단위이다.

### Fields

* id
* name
* type
* parentRegionId
* civilizationId
* dangerLevel
* centrality
* environmentTagsJson

### Region Types

* CITY
* SAFE_ZONE
* FRONTIER
* WILDERNESS
* RUIN
* CENTRAL_WASTE
* BABEL_AREA
* BORDERLAND

---

## Civilization

4대 문명권이다.

### Fields

* id
* name
* quadrant
* environmentCore
* combatCore
* cultureCore

### Presets

#### Northwest

* name: 설산 군사도시
* environmentCore: SNOW_MOUNTAIN
* combatCore: FIREARMS
* cultureCore: MILITARY

#### Northeast

* name: 수정사막 마도도시
* environmentCore: CRYSTAL_DESERT
* combatCore: MAGIC
* cultureCore: ARCANE_RESEARCH

#### Southwest

* name: 밀림 자연도시
* environmentCore: JUNGLE
* combatCore: BOW_POISON_BIO
* cultureCore: NATURALISM

#### Southeast

* name: 초원 폭포도시
* environmentCore: GRASSLAND_WATERFALL
* combatCore: MELEE
* cultureCore: TRADITIONAL_FEUDAL

---

## Settlement

개척지, 전초기지, 마을, 도시를 표현한다.

### Fields

* id
* regionId
* name
* rulerActorId
* rulerOrganizationId
* status
* styleProfileJson
* createdAt

### Status

* CAMP
* OUTPOST
* SETTLEMENT
* VILLAGE
* TOWN
* CITY
* METROPOLIS
* RUINED

---

## Infrastructure

정착지 인프라 성숙도이다.

### Fields

* id
* settlementId
* type
* maturityLevel
* updatedAt

### Infrastructure Types

* SHELTER
* WATER
* FOOD
* DEFENSE
* MARKET
* CRAFT
* GOVERNANCE
* RELIGION
* RESEARCH
* TRANSPORT

---

## MigrationEvent

NPC 유입 또는 이탈 기록이다.

### Fields

* id
* settlementId
* npcType
* amount
* reasonJson
* createdAt

---

## MonsterSpecies

몬스터 종이다. 몬스터는 환경 영향, 문명 영향, 경계 지역 혼합 영향을 받을 수 있다.

### Fields

* id
* name
* environmentInfluenceJson
* civilizationInfluenceJson
* hybridInfluenceJson
* tier
* dropsJson
* createdAt

### Tier

* COMMON
* ELITE
* NAMED
* BEAST
* DISASTER

---

## Monster

몬스터 개체이다.

### Fields

* id
* speciesId
* regionId
* tier
* alive
* spawnedByWorldWill
* spawnedAt

---

## WorldWillEvent

세계의 의지 이벤트이다.

### Fields

* id
* type
* regionId
* severity
* causeJson
* resultJson
* createdAt

### Types

* MONSTER_RESPAWN
* BEAST_SPAWN
* DISASTER
* CORRUPTION_SPREAD
* SETTLEMENT_PRESSURE

---

## DeathEvent

사망 기록이다.

### Fields

* id
* actorId
* killerActorId
* monsterId
* regionId
* lostGold
* reputationDelta
* contractConsequencesJson
* createdAt

---

## ReputationEvent

명성, 신뢰, 영향력에 영향을 주는 사건이다. 수치는 내부 데이터이며 UI에 직접 표시하지 않는다.

### Fields

* id
* actorId
* type
* value
* metadataJson
* createdAt

### Types

* CONTRACT_COMPLETED
* CONTRACT_FAILED
* FIRST_DISCOVERY
* MONSTER_KILLED
* DISASTER_DEFENDED
* SETTLEMENT_FOUNDED
* POSITION_GAINED
* POSITION_LOST
* LICENSE_SOLD
* SCHOOL_FOUNDED

---

## 핵심 관계

### Account and Character

* Account는 여러 Character를 가질 수 있다.
* Character는 하나의 Actor와 연결된다.

### Actor and Contract

* Actor는 Contract를 발주할 수 있다.
* Actor는 Contract를 수주할 수 있다.
* Actor는 Position Contract를 통해 권한을 가진다.

### Contract and Organization

* Organization은 Contract를 발급할 수 있다.
* Organization 내부 역할은 Position Contract로 표현된다.

### Discovery and Knowledge

* Discovery는 개인의 발견 기록이다.
* Knowledge는 Discovery가 사회화된 결과이다.
* 하나의 Discovery는 하나의 Knowledge를 만들 수 있다.

### Knowledge and Lineage

* Knowledge는 여러 KnowledgeRelation을 통해 계보를 형성한다.
* 하나의 부모 Knowledge에서 여러 자식 Knowledge가 파생될 수 있다.

### Knowledge and Economy

* Knowledge는 KnowledgeLicense를 통해 거래된다.
* KnowledgeLicense는 사용권, 재판매권, 복제권, 교육권, 연구권을 포함할 수 있다.

### Organization and Economy

* Organization은 자산, 지식, 지분, 계약을 보유할 수 있다.
* Organization은 다른 Organization의 지분을 보유할 수 있다.

### Settlement and Region

* Region은 Settlement를 가질 수 있다.
* Settlement는 Infrastructure에 의해 성장한다.
* Settlement는 MigrationEvent를 통해 NPC 유입과 이탈을 기록한다.

### Monster and World Will

* Monster는 Region에 생성된다.
* WorldWillEvent는 몬스터 리스폰, 재앙, 침식을 발생시킨다.

---

## MVP 우선순위

### MVP 필수

* Account
* Character
* Actor
* NPC
* Contract
* ContractMarketAccessPoint
* Item
* Equipment
* Loadout
* Region
* Monster
* Discovery
* DiscoveryCandidate
* DiscoveryProgress

### MVP 이후

* Knowledge
* KnowledgeLineage
* KnowledgeRelation
* KnowledgeLicense
* Organization
* Settlement
* Infrastructure
* ReputationEvent

### 장기 구현

* School
* OrganizationShare
* LicenseTransaction
* WorldWillEvent
* Sovereignty
* Position Lifecycle
* Full Settlement Simulation

---

## 설계 원칙

* 모든 핵심 행위자는 Actor로 통합한다.
* 모든 권한은 Contract로 표현한다.
* 모든 발견은 Discovery로 기록한다.
* 모든 사회화된 발견은 Knowledge가 된다.
* 모든 지식은 계보를 가진다.
* 모든 조직은 경제 단위이다.
* 모든 도시는 성장하는 시스템이다.
* 모든 중요한 변화는 Event로 기록한다.
