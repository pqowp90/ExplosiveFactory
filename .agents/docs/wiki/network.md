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

### A. 프리팹 스폰 위치 및 풀링 등록 규칙
- 네트워크 상에서 `NetworkServer.Spawn`을 호출할 모든 프리팹은 반드시 **`Assets/Resources/Network/`** 디렉토리에 보관되어야 합니다.
- `NetworkPoolManager.RegisterNetworkPrefabs()`가 이 디렉토리의 모든 `NetworkIdentity` 프리팹을 스캔하여 Mirror의 `SpawnHandler`/`UnSpawnHandler`를 통해 로컬 `PoolManager`와 연동 등록합니다.

### B. NetworkPoolManager를 통한 전면적 풀링 스폰 (SSOT)
- 네트워크 상에서 생성/파괴되는 모든 엔티티(플레이어, 아이템, 자판기, 투사체 등)는 `NetworkPoolManager`를 통해 풀링 스폰 및 동기화됩니다.
- 클라이언트는 Mirror의 `SpawnHandler`/`UnSpawnHandler`를 통해 로컬 `PoolManager`와 연동되어 GC 부하 없이 오브젝트를 재사용합니다.

---

## 3. 로비 및 씬 전환 흐름

1. **메인 메뉴 (`MainMenuScene`):**
   - **호스트:** '방 만들기' 클릭 → `LobbyService.CreateLobbyAsync`로 Steam P2P 로비 생성 → `CustomNetworkManager.StartHost()` 호출 후 `LobbyScene`으로 이동.
   - **클라이언트 참가 경로:**
     - **스팀 초대 / 친구 참가:** Steam 오버레이 또는 알림에서 수락 시 `SteamFriends.OnGameLobbyJoinRequested` ➔ `CustomNetworkManager.singleton.JoinLobby(lobbyId)`가 트리거되어 Steam 로비 입장 및 P2P 호스트 주소(`hostSteamId`)로 `StartClient()` 즉시 연결.
     - **로비 ID 직접 참가:** 클립보드에 복사된 로비 ID 또는 입력 필드로 `CustomNetworkManager.singleton.JoinLobbyByIdString` 호출.
     - **호스트 Steam ID 폴백:** 메타데이터(`HostSteamId`)가 동기화 지연 시 `lobby.Owner.Id`를 즉시 안전한 폴백으로 참조.
2. **로비 대기실 (`LobbyScene`):**
   - 플레이어 입장 시 `LobbyPlayer` 프리팹 스폰 및 `CustomNetworkManager.CachePlayerInfo`로 연결 ID별 메타데이터(이름, SteamId) 캐싱.
   - 각 플레이어는 준비(`Ready`) 토글, 방장은 모든 참가자 준비 완료 시 게임 시작(`StartGame`) 트리거.
   - **'초대하기' 버튼 클릭 시:**
     - 인게임 **Steam 친구 목록 팝업 모달(`SteamFriendsManager`)**이 화면에 오픈되어 각 친구의 프로필 아바타/닉네임 옆 **[초대]** 버튼으로 원클릭 초대 발송.
     - 동시에 팝업 상단의 **[📋 로비 ID 복사]** 버튼을 통해 디스코드/카톡용 코드로도 언제든 복사 및 전달 가능.
3. **인게임 (`GameScene`):**
   - 서버가 `ServerChangeScene("GameScene")` 호출.
   - 각 클라이언트 씬 로드 완료(`OnServerReady`) 시, 서버의 캐시된 플레이어 정보를 기반으로 `GamePlayer` 프리팹을 스폰하고 `ReplacePlayerForConnection`을 통해 완벽하게 바인딩.
   - 서버가 게임 내 필수 인터랙션 시설(`ItemVendingMachine` 등)을 `NetworkPoolManager.Get`을 통해 동적 스폰.

