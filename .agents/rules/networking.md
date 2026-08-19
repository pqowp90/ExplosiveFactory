# Mirror Networking Rules & Conventions

## 1. 프리팹 관리 및 스폰
- 동적으로 생성되는 모든 네트워크 프리팹은 **`Assets/Resources/Network/`** 아래에 위치해야 합니다.
- `CustomNetworkManager`가 시작 시 `Resources/Network` 내의 모든 프리팹을 `spawnPrefabs`에 자동 등록합니다.
- 네트워크 스폰은 반드시 서버 권한(`NetworkServer.Spawn`)으로 수행합니다.

## 2. 컴포넌트 및 동적 추가 주의사항
- 런타임에 동적으로 `AddComponent`되는 스크립트는 `NetworkBehaviour` 대신 **`MonoBehaviour`**를 사용합니다.
- 로컬 플레이어 판단 시 루트 `Player`의 `player.isLocalPlayer` 또는 `player.isOwned`를 검사합니다.

## 3. 네트워크 동기화 컴포넌트 GUID
- `NetworkIdentity`: `9b91ecbcc199f4492b9a91e820070131`
- `NetworkTransformReliable`: `8ff3ba0becae47b8b9381191598957c8`

## 4. 정적 씬 오브젝트 금지
- 씬 파일 내에 `NetworkIdentity`가 포함된 오브젝트를 직접 배치하지 않습니다.
- 씬 전환 시 `OnServerSceneChanged` 또는 `OnServerReady`에서 동적으로 스폰합니다.
