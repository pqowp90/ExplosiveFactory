# 네트워크 및 멀티플레이어 위키 (Network & Steam Architecture)

> 본 문서는 `ExplosiveFactory`의 Mirror 네트워킹, Steamworks P2P 로비 매칭, 프리팹 자동 등록 메커니즘 및 씬 전환 스폰 파이프라인을 다룹니다.

---

## 1. 네트워크 계층 구조

```
[Steam Matchmaking / Steamworks.NET] (LobbyService / SteamManager)
                         ↓ P2P Connection
[FizzyFacepunch / KCP Transport] (Mirror Transport Layer)
                         ↓ Packet Relay
[CustomNetworkManager] (Mirror NetworkManager)
   ├── spawnPrefabs 자동 로드 (Assets/Resources/Network/)
   ├── LobbyPlayer 관리 (LobbyScene)
   └── GamePlayer 관리 (GameScene)
```

---

## 2. 핵심 규칙 및 원칙

### A. 프리팹 스폰 위치 규칙
- 네트워크 상에서 `NetworkServer.Spawn`을 호출할 모든 프리팹은 반드시 **`Assets/Resources/Network/`** 디렉토리에 보관되어야 합니다.
- `CustomNetworkManager.EnsurePrefabsLoaded()` 및 에디터 포스트프로세서(`NetworkPrefabPostprocessor.cs`)가 이 디렉토리를 스캔하여 Mirror의 `spawnPrefabs`에 자동 등록합니다.

### C. NetworkPoolManager를 통한 전면적 풀링 스폰 (SSOT)
- 네트워크 상에서 생성/파괴되는 모든 엔티티(아이템, 자판기, 투사체 등)는 `Instantiate`/`Destroy` 대신 **`NetworkPoolManager.Get(...)` 및 `NetworkPoolManager.Release(...)`를 전적으로 사용**합니다.
- 클라이언트는 Mirror의 `SpawnHandler`/`UnSpawnHandler`를 통해 로컬 `PoolManager`와 연동되어 GC 부하 없이 오브젝트를 재사용합니다.

---

## 3. 로비 및 씬 전환 흐름

1. **메인 메뉴 (`MainMenuScene`):**
   - 호스트가 방 만들기 클릭 → `LobbyService`가 Steam P2P 로비 생성 → `CustomNetworkManager.StartHost()` 호출.
   - 클라이언트가 초대 수락 또는 로비 목록 클릭 → `CustomNetworkManager.StartClient()`로 접속.
2. **로비 대기실 (`LobbyScene`):**
   - 플레이어 입장 시 `LobbyPlayer` 프리팹 스폰.
   - 각 플레이어는 준비(`Ready`) 토글 및 방장의 게임 시작(`StartGame`) 트리거.
3. **인게임 (`GameScene`):**
   - 서버가 `ServerChangeScene("GameScene")` 호출.
   - 씬 로드 완료 시 `LobbyPlayer`를 `GamePlayer` 프리팹으로 교체 스폰.
   - 서버가 게임 내 필수 인터랙션 시설(`ItemVendingMachine` 등)을 `NetworkPoolManager.Get`을 통해 동적 스폰.
