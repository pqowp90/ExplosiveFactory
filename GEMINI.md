# ExplosiveFactory — Antigravity Assistant Guide

> 이 문서는 Antigravity 에이전트가 본 프로젝트에서 작업할 때 항상 준수해야 하는 최상위 가이드라인입니다.
> 상세 아키텍처 및 SSOT는 [AGENTS.md](file:///x:/Github/ExplosiveFactory/AGENTS.md)를 따르며, 세부 스크립트 색인은 [.agents/docs/layout.md](file:///x:/Github/ExplosiveFactory/.agents/docs/layout.md) 및 [.agents/docs/wiki/index.md](file:///x:/Github/ExplosiveFactory/.agents/docs/wiki/index.md)를 참조합니다.

---

## 1. 프로젝트 요약
- **프로젝트명:** ExplosiveFactory
- **엔진:** Unity 6 (6000.3.16f1)
- **네트워킹:** Mirror Networking (kcp / FizzyFacepunch Steam)
- **주요 시스템:** FPS 기반 물리 인터랙션, 멀티플레이어 아이템/자판기 시스템

---

## 2. 필수 준수 규칙 (Core Rules)

1. **단일 진실 공급원(SSOT):**
   - 모든 기능 구현 및 수정 시 [AGENTS.md](file:///x:/Github/ExplosiveFactory/AGENTS.md)의 아키텍처를 최우선으로 따릅니다.
2. **사전 계획 수립 및 사용자 승인 (Plan First & User Approval):**
   - 어떤 작업이든 코드를 작성하거나 변경하기 전에 **반드시 구체적인 구현 계획을 수립하고 사용자에게 설계와 방향을 설명/질문하여 승인을 받은 뒤 진행**합니다.
   - 모호한 사항이나 여러 구현 대안이 있을 때는 추측으로 진행하지 않고 사용자에게 직접 묻습니다.
3. **Unity 6 API:**
   - Rigidbody 속도는 `rb.velocity` 대신 반드시 `rb.linearVelocity`를 사용합니다.
4. **네트워크 프리팹 위치:**
   - `NetworkServer.Spawn`되는 모든 프리팹은 반드시 `Assets/Resources/Network/` 폴더 내에 배치해야 합니다.
5. **씬 내 NetworkIdentity 정적 배치 금지:**
   - 씬 파일에 NetworkIdentity를 가진 오브젝트를 정적으로 하드코딩하지 않고, 서버 씬 전환 시 동적으로 스폰합니다.
6. **조작 키 매핑 준수:**
   - `F`: 줍기/상호작용
   - `G`: 버리기
   - `좌클릭 / 우클릭`: 들고 있는 아이템 기능 사용
   - `마우스 휠`: 인벤토리 슬롯 전환
7. **문서 및 Wiki 동기화 (Living Documentation):**
   - 스크립트 추가/수정 또는 시스템 변경 시 반드시 [layout.md](file:///x:/Github/ExplosiveFactory/.agents/docs/layout.md) 및 [wiki/](file:///x:/Github/ExplosiveFactory/.agents/docs/wiki/) 문서를 함께 업데이트해야 합니다.
8. **🚨 철저한 사전 검증 및 안일한 코딩 금지:**
   - UI 모드와 인게임 입력 교차 상태를 사전에 완벽히 추적하여 조작 먹통 방지
   - Input System 컨트롤 타입(`float`, `Vector2`) 안전 처리로 런타임 예외 원천 차단
   - 아이템별 좌클릭(기능)과 우클릭(UI/보조)의 명확한 역할 분리 준수
   - 실시간 상호작용 시 SyncVar 지양 및 명시적 Command/ClientRpc 즉시 동기화 파이프라인 적용
9. **사용자 수동 작업 전가 절대 금지 (100% 자동 완료 원칙):**
   - 인스펙터 드래그앤드롭, 수동 컴포넌트 연결 등을 사용자에게 시키지 않고, 에이전트가 코드/런타임 자동화/프리팹 직접 수정으로 100% 완결할 것.
10. **Git 커밋은 반드시 사용자의 명시적 지시 시에만 수행:**
    - 에이전트가 임의로 커밋하지 않으며, 사용자가 "커밋해", "커밋해줘"라고 요청했을 때만 수행할 것. (커밋 메시지는 반드시 100% 한국어로 작성)
11. **🚨 사용자의 명시적 지시 없는 임의 롤백/복원(git restore/checkout/reset) 절대 금지:**
    - 오류나 오작동이 발생했을 때 에이전트가 당황하여 임의로 코드를 롤백하거나 되돌리지 않습니다. 사용자가 "되돌려", "원복해", "롤백해"라고 명시적으로 지시하지 않는 한, 반드시 문제를 분석하고 올바른 수정안을 제시하여 해결합니다.
