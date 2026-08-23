# 폴더 및 스크립트 레이아웃 (Layout & Script Reference)

> 본 문서는 `ExplosiveFactory` 프로젝트의 모든 폴더 및 C# 스크립트의 위치와 역할을 1:1로 정리한 색인입니다.
> 새로운 스크립트를 추가하거나 기존 스크립트를 수정할 때 본 문서를 참조하고 업데이트합니다.

---

## 1. Assets/ 최상위 폴더 구조

| 폴더 | 설명 |
|---|---|
| `00_Asset/` | 3D 모델, 텍스처, 사운드, 외부 임포트 에셋 (Flashlight, Phone, FizzyFacepunch 등) |
| `Mirror/` | Mirror 네트워킹 핵심 라이브러리 및 트랜스포트 |
| `Prefabs/` | 일반 프리팹 (PlayerEntry 등 UI/비네트워크 프리팹) |
| `Resources/` | 런타임 동적 로드 에셋 (`Resources.Load`) |
| `Resources/ItemData/` | `ItemData` 기반 아이템 ScriptableObject 데이터 에셋 |
| `Resources/Network/` | **[필수]** `NetworkServer.Spawn`되는 모든 네트워크 동기화 프리팹 |
| `Scenes/` | 메인 씬 (`MainMenuScene`, `LobbyScene`, `GameScene`) |
| `Scripts/` | 게임 로직 C# 소스 코드 (`ExplosiveFactory.Scripts.asmdef`) |

---

## 2. Assets/Scripts/ 세부 스크립트 명세

### 🎮 Player (플레이어 시스템)

| 스크립트 경로 | 클래스명 | 역할 및 핵심 기능 |
|---|---|---|
| `Player/Player.cs` | `Player` | 플레이어 엔티티의 최상위 루트 클래스. `NetworkIdentity`를 보유하며, 이동/회전/입력/아이템/애니메이션 컴포넌트들을 총괄 관리. |
| `Player/PlayerComponent.cs` | `PlayerComponent` | 플레이어 하위 모든 서브시스템 컴포넌트의 베이스 클래스. 부모 `Player` 엔티티 자동 지연 캐싱 및 하위 컴포넌트(`PlayerMove`, `PlayerAnimation`, `IsOwned` 등) 즉시 접근 프로퍼티 제공. |
| `Player/LocalPlayerSetter.cs` | `LocalPlayerSetter` | 로컬 플레이어 여부(`isLocalPlayer`)에 따라 1인칭 카메라, 가상 리스너, 렌더러 레이어(1인칭/3인칭 분리)를 활성화/비활성화. |
| `Player/Input/InputController.cs` | `InputController` | Unity 신규/레거시 입력 이벤트를 감지하여 이동/시선/상호작용 키 입력을 이벤트 및 프로퍼티로 브로드캐스팅. |
| `Player/Interact/InteractiveRaycast.cs` | `InteractiveRaycast` | 카메라 정면 시선(Raycast)을 발사하여 상호작용 가능한 대상(`InteractableObject`, `Item`, `ItemVendingMachine` 등)을 검출하고 UI 및 상호작용 트리거. |
| `Player/Interact/InteractableObject.cs` | `InteractableObject` | 상호작용 가능한 오브젝트의 기반 컴포넌트. 시선 감지(`OnWatch` / `OnNotWatch`) 시 `OutlineManager`에 렌더러를 등록/해제하여 아웃라인 렌더링을 처리하고 `F` 키 상호작용 진입점 제공. |
| `Player/Move/PlayerMove.cs` | `PlayerMove` | Rigidbody 물리 기반 1인칭 걷기/달리기/점프/앉기 이동 로직. Unity 6의 `rb.linearVelocity`를 사용하여 지면 검사 및 경사면 이동 처리. |
| `Player/Move/PlayerRotate.cs` | `PlayerRotate` | 마우스 입력을 받아 수평(좌우 몸체 회전) 및 수직(상하 카메라 피치 회전 및 클램핑) 회전을 동기화. |
| `Player/Move/CameraShocShak.cs` | `CameraShocShak` | 플레이어 피격, 폭발, 착지 시 카메라 쉐이크 연출 처리. |
| `Player/Move/SwayNBobScript.cs` | `SwayNBobScript` | 1인칭 시점 무기/손의 이동 밥(Bobbing) 및 마우스 회전 스웨이(Sway) 물리적 흔들림 효과 연출. |
| `Player/PlayerAnimation/PlayerAnimation.cs` | `PlayerAnimation` | 1인칭/3인칭 애니메이터 제어 및 이동 상태(속도, 점프, 앉기) 파라미터 갱신. |
| `Player/PlayerAnimation/IMovementAnimation.cs` | `IMovementAnimation` | 이동 애니메이션 상태 갱신을 위한 공용 인터페이스. |
| `Player/PlayerAnimation/LookAtController.cs` | `LookAtController` | 부모(Player 루트)에 위치하여 3인칭 모델의 머리/상체가 카메라 시선 방향을 자연스럽게 바라보도록 하는 절차적 IK/LookAt 제어 및 모델 스왑 리바인딩 지원. |
| `Player/PlayerAnimation/FootIKController.cs` | `FootIKController` | 부모(Player 루트) 및 1인칭 다리에 적용되는 경사면/지형 대응 Foot IK 제어기. 양발 Raycast를 통한 지면 법선(Normal) 회전 정렬, 골반(Pelvis) 높이 자동 보정 및 모델 스왑 리바인딩 제공. |
| `Player/PlayerAnimation/AnimatorIKForwarder.cs` | `AnimatorIKForwarder` | 자식 모델 오브젝트에서 유니티 `OnAnimatorIK` 이벤트를 수신하여 부모의 `LookAtController` 및 `FootIKController`로 중계하는 가벼운 프록시 컴포넌트. |
| `Player/PlayerAnimation/FirstPersonLegsController.cs` | `FirstPersonLegsController` | 1인칭 전용 다리(Legs) 제어 컴포넌트. 자식 Animator 및 본 자동 탐색, 팔/머리/목/어깨/손 본 (0,0,0) 강제 축소, 상체(Spine/Chest) 및 다리 루트 카메라 후방 오프셋/Pitch 비례 보정으로 시야 간섭 없는 완벽한 하체 렌더링 지원. |
| `Player/PlayerAnimation/FirstPersonLegsSetup.cs` | `FirstPersonLegsSetup` | 1인칭 다리 모델 인스턴스화, 불필요 컴포넌트 정리 및 1인칭/3인칭 Foot IK/Forwarder 자동 연결을 전담하는 셋업 컴포넌트. |
| `Player/PlayerAnimation/FirstPersonLegsSettings.cs` | `FirstPersonLegsSettings` | 1인칭 다리 및 상체 오프셋(서 있을 때/앉았을 때) 세팅 데이터 클래스. |
| `Player/Skin/PlayerSkinData.cs` | `PlayerSkinData` | 플레이어 3인칭 캐릭터 모델링 스킨 ScriptableObject 메타데이터 (이름, 설명, 아이콘, 3D 모델 프리팹). |
| `Player/Skin/PlayerSkinDatabase.cs` | `PlayerSkinDatabase` | 등록된 전체 스킨 목록을 관리하는 ScriptableObject 데이터베이스. Resources/SkinData 경로에서 자동 로드. |
| `Player/Skin/PlayerSkinController.cs` | `PlayerSkinController` | 플레이어 캐릭터 모델 교체 및 네트워크 동기화 전담 컴포넌트 (`NetworkBehaviour`). SyncVar/Command/ClientRpc를 통해 모든 접속자에게 실시간 모델 교체 동기화. |
| `UI/ModelSelectUI.cs` | `ModelSelectUI` | 핸드폰 UI 내 캐릭터 모델(스킨) 선택 화면 제어 컴포넌트. 스킨 목록 표시 및 버튼 클릭 시 `PlayerSkinController.CmdChangeSkin` 호출. |

---

### 📦 Item System (아이템 파이프라인)

| 스크립트 경로 | 클래스명 | 역할 및 핵심 기능 |
|---|---|---|
| `Item/Item.cs` | `Item` | 네트워크 상에 존재하는 모든 아이템의 기본 베이스 클래스 (`NetworkBehaviour`). 바닥 상태(`Grounded`), 파지 상태(`Held`), 투척 상태(`Thrown`) 라이프사이클 관리. |
| `Item/ItemHolder.cs` | `ItemHolder` | 플레이어가 보유한 3개 인벤토리 슬롯 및 손에 쥔 아이템을 관리. 마우스 휠 슬롯 전환, 우클릭 아이템 사용, `F` 줍기, `G` 버리기 처리. |
| `Item/ItemDataManager.cs` | `ItemDataManager` | `Resources/ItemData/` 경로의 `ItemData` 에셋을 로드하고 관리하는 싱글톤 (`MonoSingleton<ItemDataManager>`). ID 기반 아이템 메타데이터 조회 지원. |
| `Item/ItemVendingMachine.cs` | `ItemVendingMachine` | 인게임 아이템 구매/뽑기 자판기 오브젝트. 서버에서 인터랙션 요청을 검증하고 `NetworkPoolManager`를 통해 아이템 배출. |
| `Item/ItemEventBehaviour.cs` | `ItemEventBehaviour` | 아이템의 주움, 버림, 사용 이벤트에 반응하는 베이스 컴포넌트. |
| `Item/HandyItemObject.cs` | `HandyItemObject` | 손에 쥐었을 때 특정 기능을 발동하는 기능성 핸디 아이템의 기본 클래스. |
| `Item/HandyItem/FlashLightHandyItemObject.cs` | `FlashLightHandyItemObject` | 손전등 아이템 구현체. 마우스 우클릭 시 손 애니메이션 및 `OnAnimationTriggerEvent(0)`를 통해 스팟라이트 On/Off 토글. |
| `Item/ToggleHoldItem/ToggleHoldItem.cs` | `ToggleHoldItem` | 들고 있을 때 모드 전환이나 상태 토글이 가능한 아이템 베이스. |
| `Item/ToggleHoldItem/PhoneItem/PhoneItem.cs` | `PhoneItem` | 스마트폰 아이템 구현체. 마우스 우클릭 시 화면 확대/UI 인터랙션 모드 지원. |
| `Item/NomalItem/NomalItem.cs` | `NomalItem` | 별도 기능 없이 운반/판매/투척 목적의 일반 화물/자원 아이템. |
| `Item/Data/ItemData.cs` | `ItemData` | 아이템 메타데이터(이름, 무게, 가격, 손 부착 오프셋, 애니메이터 오버라이드 등)를 정의하는 ScriptableObject. |

---

### 🌐 Network & Steam (네트워크 및 멀티플레이어)

| 스크립트 경로 | 클래스명 | 역할 및 핵심 기능 |
|---|---|---|
| `Network/CustomNetworkManager.cs` | `CustomNetworkManager` | Mirror `NetworkManager` 확장. `Resources/Network/` 프리팹 자동 등록, 씬 전환, 플레이어 접속/퇴장 제어. |
| `Network/GamePlayer.cs` | `GamePlayer` | 인게임 씬에서 스폰되는 플레이어의 네트워크 엔티티. 플레이어 이름, 스킨, 준비 상태 동기화. |
| `Network/LobbyPlayer.cs` | `LobbyPlayer` | 대기방(로비) 씬에서 사용되는 플레이어 엔티티. 준비 상태, 방장 권한 관리. |
| `Network/LobbyService.cs` | `LobbyService` | Steam Matchmaking API를 래핑하여 Steam 로비 생성, 검색, 참가, 데이터 동기화 수행. |
| `Network/SteamManager.cs` | `SteamManager` | Steamworks.NET 초기화, 콜백 디스패치 및 라이프사이클 관리 싱글톤. |
| `Network/SteamFriendsManager.cs` | `SteamFriendsManager` | 스팀 친구 목록 조회, 상태 확인, 인게임 초대 UI 연동. |
| `Network/FriendObject.cs` | `FriendObject` | 친구 초대 UI 목록의 각 개별 친구 엔트리 UI 프리팹 제어. |
| `Network/UI/MainMenuUI.cs` | `MainMenuUI` | 메인 메뉴 화면 UI (방 만들기, 방 찾기, 설정, 나가기). |
| `Network/UI/LobbyUI.cs` | `LobbyUI` | 로비 화면 UI (참가자 명단, 준비/시작 버튼, 친구 초대 팝업). |

---

### 🖥️ UI (사용자 인터페이스)

| 스크립트 경로 | 클래스명 | 역할 및 핵심 기능 |
|---|---|---|
| `UI/InventoryUI.cs` | `InventoryUI` | 인게임 하단 인벤토리 슬롯 HUD 관리자. 로컬 플레이어의 `ItemHolder`와 바인딩하여 활성 슬롯 하이라이트 및 아이템 상태 실시간 갱신. |
| `UI/InventorySlotUI.cs` | `InventorySlotUI` | 인벤토리 개별 슬롯 UI. 슬롯 번호(`[1]`, `[2]`, `[3]`), 아이템 이름, 테두리 하이라이트 렌더링. |
| `UI/HandyPhoneUI.cs` | `HandyPhoneUI` | 스마트폰 아이템의 인게임 스크린 UI 화면 제어. |
| `UI/PhoneTimeUI.cs` | `PhoneTimeUI` | 스마트폰 상단 시간 및 배터리 표시기 UI. |
| `UI/MarketUI.cs` | `MarketUI` | 자판기/상점 인터랙션 시 열리는 아이템 구매 창 UI. |
| `UI/MarketItemUI.cs` | `MarketItemUI` | 상점 창 내 개별 판매 아이템 슬롯 UI. |
| `UI/InviteUI.cs` | `InviteUI` | 스팀 친구 초대 모달 팝업 UI. |

---

### ⚙️ Manager & Utils (매니저 및 유틸리티)

| 스크립트 경로 | 클래스명 | 역할 및 핵심 기능 |
|---|---|---|
| `Manager/GameManager.cs` | `GameManager` | 인게임 전체 상태 머신(게임 진행, 라운드, 타이머)을 관리하는 최상위 매니저. |
| `Manager/CursorManager.cs` | `CursorManager` | 상황에 따른 마우스 커서 잠금(`Locked`) / 해제(`None`) 상태 중앙 관리. |
| `Manager/SoundManager.cs` | `SoundManager` | BGM 및 2D/3D SFX 사운드 재생, 볼륨 믹싱 관리. |
| `Camera/CameraShake.cs` | `CameraShake` | 전역 카메라 흔들림 연출 유틸리티. |
| `Animator/CustomNetworkAnimator.cs` | `CustomNetworkAnimator` | Mirror 기본 NetworkAnimator의 오버라이드 및 안정화 버전. |
| `Animator/AnimationTriggerEventHolder.cs` | `AnimationTriggerEventHolder` | 애니메이션 이벤트 클립에서 C# 이벤트를 전달받아 디스패치하는 홀더. |
| `RenderFeature/OutlineRenderFeature.cs` | `OutlineRenderFeature` | Unity 6 URP 렌더러 피처. `OutlineManager`에 등록된 타겟 렌더러들을 마스크 버퍼에 실루엣으로 렌더링하고 카메라 뷰에 아웃라인을 합성. |
| `Utils/OutlineManager.cs` | `OutlineManager` | 현재 시선에 들어온 오브젝트의 렌더러 목록, 색상, 두께를 관리하는 전역 아웃라인 관리자. |
| `Utils/MonoSingleton.cs` | `MonoSingleton<T>` | 씬 종속적이지 않은 안전한 `MonoBehaviour` 기반 제네릭 싱글톤. |
| `Utils/NetworkSingleton.cs` | `NetworkSingleton<T>` | `NetworkBehaviour` 기반 제네릭 네트워크 싱글톤. |
| `Utils/NetworkUuid.cs` | `NetworkUuid` | 네트워크 오브젝트의 고유 식별자 생성 및 매핑 유틸. |
| `Utils/LazyResource.cs` | `LazyResource<T>` | 리소스 지연 로드 및 캐싱 래퍼. |
| `Utils/LazyAddressable.cs` | `LazyAddressable<T>` | Addressables 에셋 지연 로드 및 폴백 지원 유틸. |
| `Utils/PoolManager.cs` | `PoolManager` | 일반 오브젝트 풀링 관리자. |
| `Utils/NetworkPoolManager.cs` | `NetworkPoolManager` | 네트워크 동기화 오브젝트 풀링 관리자. |

---

### 🛠️ Editor Tools (에디터 전용 툴)

| 스크립트 경로 | 클래스명 | 역할 |
|---|---|---|
| `Editor/NetworkPrefabPostprocessor.cs` | `NetworkPrefabPostprocessor` | `Resources/Network/`에 프리팹 추가 시 자동으로 네트워크 매니저 등록 처리. |
| `Editor/LobbySetupTool.cs` | `LobbySetupTool` | 로비 씬 자동 구성 및 바인딩 헬퍼 에디터 윈도우. |
| `Editor/FootIKCurveGenerator.cs` | `FootIKCurveGenerator` | 애니메이션 클립을 분석하여 `LeftFootIK` / `RightFootIK` Float 커브를 100% 자동 생성/주입하는 에디터 유틸리티. |
| `Editor/Toolbar/*` | `Toolbar*` | Unity 상단 툴바에 빠른 씬 전환, 플레이어 프리팹 선택 버튼을 추가하는 편의 툴. |
