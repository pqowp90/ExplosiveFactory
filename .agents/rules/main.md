# ExplosiveFactory Rules & Guidelines

> 항상 루트 디렉토리의 [`AGENTS.md`](file:///x:/Github/ExplosiveFactory/AGENTS.md)를 최우선 단일 진실 공급원(SSOT)으로 참조합니다.

## 핵심 요약
1. **Network Prefabs:** 모든 네트워크 스폰 프리팹은 반드시 `Assets/Resources/Network/`에 위치해야 합니다.
2. **Item System:** `ItemData` (ScriptableObject) + `Item` (Base) + `ItemHolder` (Hand/Inventory) 구조를 준수합니다.
3. **Mirror Networking:** 씬 파일에 정적 NetworkIdentity를 넣지 않고, 서버가 동적 `NetworkServer.Spawn`을 수행합니다.
4. **Input & Interaction:** 1인칭 카메라 시선 Raycast 기반 상호작용 (F: 사용/자판기, E: 줍기, 좌클릭: 투척, G: 놓기).
5. **Unity 6 API:** Rigidbody 속도는 `linearVelocity`를 사용합니다.
