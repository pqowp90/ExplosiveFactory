# ExplosiveFactory — Antigravity Assistant Guide

> 이 문서는 Antigravity 에이전트가 본 프로젝트에서 작업할 때 항상 준수해야 하는 최상위 가이드라인입니다.
> 상세 아키텍처 및 SSOT는 [AGENTS.md](file:///x:/Github/ExplosiveFactory/AGENTS.md)를 따릅니다.

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
2. **Unity 6 API:**
   - Rigidbody 속도는 `rb.velocity` 대신 반드시 `rb.linearVelocity`를 사용합니다.
3. **네트워크 프리팹 위치:**
   - `NetworkServer.Spawn`되는 모든 프리팹은 반드시 `Assets/Resources/Network/` 폴더 내에 배치해야 합니다.
4. **씬 내 NetworkIdentity 정적 배치 금지:**
   - 씬 파일에 NetworkIdentity를 가진 오브젝트를 정적으로 하드코딩하지 않고, 서버 씬 전환 시 동적으로 스폰합니다.
5. **조작 키 매핑 준수:**
   - `F`: 줍기/상호작용
   - `G`: 버리기
   - `좌클릭 / 우클릭`: 들고 있는 아이템 기능 사용
   - `마우스 휠`: 인벤토리 슬롯 전환
