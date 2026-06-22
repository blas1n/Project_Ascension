# Database Schema Draft

## 목적

이 문서는 Project_Ascension의 초기 데이터베이스 스키마 초안이다. 최종 DB 설계가 아니라, EF Core 엔티티와 마이그레이션을 작성하기 위한 기준 문서이다.

---

## 설계 원칙

* 모든 주요 주체는 Actor로 통합한다.
* 플레이어와 NPC는 동일한 도메인 규칙을 사용한다.
* 계약은 사회 시스템의 기본 단위이다.
* 발견과 지식은 분리한다.
* 지식은 계보와 라이선스를 가진다.
* 모든 중요한 변화는 이벤트로 기록한다.

---

## Core

### accounts

실제 유저 계정이다.

#### Columns

* id
* email
* created_at

### characters

플레이어가 조작하는 세계 내부 인물이다.

#### Columns

* id
* account_id
* name
* origin_region_id
* current_region_id
* status
* created_at

### actors

플레이어 캐릭터와 NPC를 통합하는 주체이다.

#### Columns

* id
* type
* character_id
* npc_id
* created_at

#### Enum: actor_type

* PLAYER
* NPC

### npcs

세계 내부의 비플레이어 인물이다.

#### Columns

* id
* name
* home_region_id
* current_region_id
* alive
* created_at

---

## Contracts

### contracts

세계의 기본 계약 테이블이다.

#### Columns

* id
* kind
* purpose
* issuer_actor_id
* issuer_organization_id
* assignee_actor_id
* assignee_organization_id
* parent_contract_id
* status
* title
* description
* reward_json
* conditions_json
* failure_conditions_json
* delegation_allowed
* max_delegation_depth
* starts_at
* expires_at
* completed_at
* failed_at
* created_at

#### Enum: contract_kind

* TASK
* RECURRING
* POSITION
* OWNERSHIP
* CITIZENSHIP
* LICENSE
* INHERITANCE

#### Enum: contract_purpose

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

#### Enum: contract_status

* DRAFT
* OPEN
* ASSIGNED
* COMPLETED
* FAILED
* CANCELLED
* EXPIRED

### contract_market_access_points

계약 시장에 접근 가능한 세계 내부 객체이다.

#### Columns

* id
* region_id
* organization_id
* type
* name
* created_at

#### Enum: access_point_type

* CITY_BOARD
* GUILD_BOARD
* OFFICE
* OUTPOST_BOARD
* PALACE_OFFICE
* COMPANY_OFFICE
* ADMINISTRATION_OFFICE

---

## Items and Equipment

### items

소유 가능한 모든 경제 자산이다.

#### Columns

* id
* type
* template_id
* owner_actor_id
* owner_organization_id
* current_region_id
* metadata_json
* created_at

#### Enum: item_type

* RESOURCE
* WEAPON
* MAGIC_TOOL
* MAP
* KNOWLEDGE_DOCUMENT
* MONSTER_MATERIAL
* CONTRACT_DOCUMENT
* EQUIPMENT
* CONSUMABLE

### equipment

장착 가능한 아이템이다.

#### Columns

* item_id
* equipment_type
* slot_type

#### Enum: equipment_type

* WEAPON
* MAGIC_TOOL
* SHIELD
* BOW
* FIREARM
* CATALYST

#### Enum: equipment_slot_type

* LEFT
* RIGHT
* EITHER
* TWO_HAND

### loadouts

Actor의 현재 양손 장비 상태이다.

#### Columns

* actor_id
* left_item_id
* right_item_id
* updated_at

---

## Discovery

### discoveries

최초 발견 기록이다.

#### Columns

* id
* type
* discoverer_actor_id
* region_id
* target_id
* title
* description
* discovered_at

#### Enum: discovery_type

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

### discovery_candidates

발견 가능성이 있는 행동 패턴 또는 조건이다.

#### Columns

* id
* candidate_key
* category
* required_context_json
* required_progress
* rarity
* created_at

#### Enum: discovery_candidate_category

* BEHAVIOR
* ENVIRONMENT
* COMBAT
* KNOWLEDGE
* HYBRID

#### Enum: rarity

* COMMON
* UNCOMMON
* RARE
* EPIC
* LEGENDARY
* CIVILIZATION

### discovery_progress

Actor별 발견 후보 진행도이다.

#### Columns

* id
* actor_id
* discovery_candidate_id
* progress
* metadata_json
* updated_at

---

## Knowledge

### knowledge

사회화된 지식 자산이다.

#### Columns

* id
* discovery_id
* type
* title
* description
* creator_actor_id
* owner_actor_id
* owner_organization_id
* created_at

#### Enum: knowledge_type

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

### knowledge_lineages

지식 계보이다.

#### Columns

* id
* name
* description
* root_knowledge_id
* created_at

### knowledge_relations

지식 간 관계이다.

#### Columns

* id
* parent_knowledge_id
* child_knowledge_id
* lineage_id
* relation_type
* created_at

#### Enum: knowledge_relation_type

* COMBINATION
* EVOLUTION
* ENVIRONMENT
* BEHAVIOR
* DERIVATION
* SCHOOL_FOUNDATION

### schools

학파이다.

#### Columns

* id
* name
* founder_actor_id
* founder_organization_id
* root_lineage_id
* owner_organization_id
* created_at

### knowledge_licenses

지식 사용 권리이다.

#### Columns

* id
* knowledge_id
* owner_actor_id
* owner_organization_id
* seller_actor_id
* seller_organization_id
* can_use
* can_resell
* can_copy
* can_teach
* can_research
* royalty_bps
* parent_license_id
* created_at

### license_transactions

지식 라이선스 거래 기록이다.

#### Columns

* id
* license_id
* buyer_actor_id
* buyer_organization_id
* seller_actor_id
* seller_organization_id
* price
* royalty_paid
* created_at

---

## Organizations

### organizations

국가, 상단, 교단, 길드, 도시 운영체 등 모든 조직이다.

#### Columns

* id
* name
* type
* parent_organization_id
* sovereign
* immutable_doctrine
* created_at

#### Enum: organization_type

* STATE
* COMPANY
* RELIGION
* MILITARY
* CITY
* GUILD
* ACADEMY
* SCHOOL
* ADMINISTRATION

### organization_memberships

Actor와 조직의 소속 관계이다.

#### Columns

* id
* actor_id
* organization_id
* role_contract_id
* joined_at

### organization_shares

조직 지분이다.

#### Columns

* id
* owner_actor_id
* owner_organization_id
* target_organization_id
* share_bps
* voting_power_bps
* created_at

---

## World

### civilizations

4대 문명권이다.

#### Columns

* id
* name
* quadrant
* environment_core
* combat_core
* culture_core

### regions

세계의 공간 단위이다.

#### Columns

* id
* name
* type
* parent_region_id
* civilization_id
* danger_level
* centrality
* environment_tags_json

#### Enum: region_type

* CITY
* SAFE_ZONE
* FRONTIER
* WILDERNESS
* RUIN
* CENTRAL_WASTE
* BABEL_AREA
* BORDERLAND

---

## Settlements

### settlements

개척지, 전초기지, 마을, 도시이다.

#### Columns

* id
* region_id
* name
* ruler_actor_id
* ruler_organization_id
* status
* style_profile_json
* created_at

#### Enum: settlement_status

* CAMP
* OUTPOST
* SETTLEMENT
* VILLAGE
* TOWN
* CITY
* METROPOLIS
* RUINED

### infrastructure

정착지 인프라 성숙도이다.

#### Columns

* id
* settlement_id
* type
* maturity_level
* updated_at

#### Enum: infrastructure_type

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

### migration_events

NPC 유입 또는 이탈 기록이다.

#### Columns

* id
* settlement_id
* npc_type
* amount
* reason_json
* created_at

---

## Monsters

### monster_species

몬스터 종이다.

#### Columns

* id
* name
* environment_influence_json
* civilization_influence_json
* hybrid_influence_json
* tier
* drops_json
* created_at

#### Enum: monster_tier

* COMMON
* ELITE
* NAMED
* BEAST
* DISASTER

### monsters

몬스터 개체이다.

#### Columns

* id
* species_id
* region_id
* tier
* alive
* spawned_by_world_will
* spawned_at

---

## Events

### world_will_events

세계의 의지 이벤트이다.

#### Columns

* id
* type
* region_id
* severity
* cause_json
* result_json
* created_at

#### Enum: world_will_event_type

* MONSTER_RESPAWN
* BEAST_SPAWN
* DISASTER
* CORRUPTION_SPREAD
* SETTLEMENT_PRESSURE

### death_events

사망 기록이다.

#### Columns

* id
* actor_id
* killer_actor_id
* monster_id
* region_id
* lost_gold
* reputation_delta
* contract_consequences_json
* created_at

### reputation_events

명성, 신뢰, 영향력에 영향을 주는 사건이다.

#### Columns

* id
* actor_id
* type
* value
* metadata_json
* created_at

#### Enum: reputation_event_type

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

## 주요 인덱스

### contracts

* status
* kind
* purpose
* issuer_actor_id
* issuer_organization_id
* assignee_actor_id
* assignee_organization_id
* parent_contract_id

### discovery_progress

* actor_id
* discovery_candidate_id

### knowledge_relations

* parent_knowledge_id
* child_knowledge_id
* lineage_id

### knowledge_licenses

* knowledge_id
* owner_actor_id
* owner_organization_id

### regions

* civilization_id
* type
* danger_level
* centrality

### monsters

* region_id
* species_id
* alive

---

## MVP 스키마 범위

### MVP 포함

* accounts
* characters
* actors
* npcs
* contracts
* contract_market_access_points
* items
* equipment
* loadouts
* civilizations
* regions
* monster_species
* monsters
* discoveries
* discovery_candidates
* discovery_progress

### MVP 이후

* knowledge
* knowledge_lineages
* knowledge_relations
* knowledge_licenses
* license_transactions
* organizations
* organization_memberships
* settlements
* infrastructure
* migration_events
* reputation_events

### 장기 구현

* schools
* organization_shares
* world_will_events
* death_events 상세 처리
