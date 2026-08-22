# ExplosiveFactory — Agent Guidelines & Project Architecture

> **이 파일은 모든 코딩 에이전트(Antigravity, Claude Code 등)가 공유하는 단일 진실 공급원(Single Source of Truth, SSOT)입니다.**
> 프로젝트 내 모든 작업(아이템 추가, 네트워크 기능 구현, 씬/프리팹 수정) 시 이 가이드라인을 최우선으로 준수합니다.

---

## ⚠️ 규칙 0 — 작업 전 문서 확인 및 사전 계획 승인 (Plan First & User Approval)

1. **관련 문서 및 Wiki 필수 확인:**
   - 📌 **[폴더 및 스크립트 색인](.agents/docs/layout.md)** — 전체 스크립트 60여 개 역할 및 위치 1:1 매핑
   - 📚 **[시스템 위키 인덱스](.agents/docs/wiki/index.md)** — [아이템](.agents/docs/wiki/item.md) · [플레이어](.agents/docs/wiki/player.md) · [네트워크](.agents/docs/wiki/network.md) 시스템 심층 가이드
   - ⚙️ **[기술 스택 및 API 규칙](.agents/docs/stack.md)** — Unity 6, Mirror, Rigidbody linearVelocity 등
   - 🎯 **[아이템 생성 스킬](.agents/skills/create-item/SKILL.md)** — 신규 아이템 생성 시 표준 파이프라인

2. **사전 구현 계획 수립 및 사용자 확인 (필수):**
   - 코드 수정, 기능 구현, 아키텍처 변경 전 **반드시 구현 계획(`implementation_plan.md`)을 수립하고 사용자에게 설계 의도/방식을 설명한 후 승인(Feedback)을 받아 착수**합니다.
   - 구현 방식에 여러 대안이 있거나 모호한 점이 있을 때는 임의로 추측하여 코딩하지 않고 **사용자에게 질문하여 결정**합니다.
   - Mirror `SyncList` 콜백 순서, `asmdef` 패키지 참조, Unity 6 직렬화 등 사이드 이펙트를 사전에 검토합니다.

   - **🚨 씬(Scene) 수정 전 동적 백업 생성 및 피드백 복구 파이프라인 (Dynamic Scene Backup & Fix from Backup):** `.unity` 씬 파일을 수정할 때는 수정 착수 직전 **반드시 원본 씬을 백업 폴더(`.agents/backups/scenes/`)에 동적으로 복사본(`{SceneName}.unity.bak`)으로 자동 저장(클립보드 보관)**한 후 작업을 진행합니다. 수정 후 사용자가 "구조가 꼬였다", "기존에 작업해둔 레이아웃/오브젝트가 사라졌다" 등 문제를 제기하면, **동적으로 떠둔 백업 파일의 구조(Hierarchy, 컴포넌트, 부모-자식 관계, VerticalLayoutGroup 등)를 직접 열어보고 비교 분석하여 사용자의 기존 작업 내용을 완벽하게 보존하면서 올바르게 고칩니다.**
   - **🚨 행동 전 계획 수립 및 사용자 승인 필수 (Plan First & User Approval):** 어떤 코드나 씬을 수정하든 행동하기 전에 **반드시 구체적인 계획을 먼저 설명하고, 사용자의 명시적 수락/승인을 받은 후에만 실행**합니다.
   - **🚨 사용자의 명시적 지시 없는 임의 롤백/복원(git restore/checkout/reset) 절대 금지:** 오류나 오작동이 발생했을 때 에이전트가 당황하여 임의로 코드를 롤백하거나 되돌리지 않습니다. 사용자가 "되돌려", "원복해", "롤백해"라고 명시적으로 지시하지 않는 한, 반드시 문제를 분석하고 올바른 수정안을 제시하여 해결합니다.
   - **🚨 동적 UI 코드 생성 절대 금지 (프리팹 및 씬 정적 배치 원칙):** C# 코드(`new GameObject()`, `AddComponent<Image>()`, `AddComponent<Button>()` 등)로 런타임에 UI 레이아웃, 모달, 팝업을 동적으로 생성하는 행위를 절대 금지합니다. 사용자가 유니티 에디터(Scene View 및 Inspector)에서 시각적으로 확인하고 색상, 폰트, 크기, 마진, 스프라이트 등 **스타일을 자유롭게 직접 수정/커스텀할 수 있도록, 모든 UI는 반드시 Unity Prefab(`Assets/Prefabs/`) 또는 씬(Scene) 파일 내에 정적으로 배치 및 직렬화(`[SerializeField]`)**해야 합니다.
   - **사용자에게 수동 작업 전가 절대 금지 (100% 자동화 원칙):** 인스펙터 드래그 앤 드롭, 수동 컴포넌트 연결 등 작업을 사용자에게 미루지 말고, 에이전트가 프리팹/씬 파일 직접 수정 또는 에디터 스크립트를 통해 100% 끝까지 완료합니다.
   - **🚨 UI 텍스트 이모티콘 및 특수문자 사용 절대 금지 (TMP 폰트 깨짐 방지):** TextMeshPro SDF 폰트 아틀라스에는 유니코드 이모지(🎮, 📋 등) 글리프가 없으므로 런타임에 네모 박스(□)로 깨집니다. 씬, 프리팹, 스크립트의 모든 UI 텍스트에는 이모티콘/특수문자/괄호 영문 병기를 절대 쓰지 않고 100% 순수 한글/영문 텍스트만 사용합니다.
   - **🚨 간결하고 핵심 위주의 답변 (상투적 멘트 절대 금지):** 장황한 인사, 기계적인 확인/승인 유도 멘트, 불필요한 미사여구를 일체 배제하고 오직 필요한 핵심 사실과 작업 결과만 최대한 간결하고 쓸모 있게 전달합니다.
   - **Git 커밋은 반드시 사용자의 명시적 지시 시에만 수행:** 에이전트가 작업 완료 시 임의로 커밋하지 않으며, 사용자가 "커밋해", "커밋해줘"라고 직접 요청했을 때만 커밋을 수행합니다. (모든 Git 커밋 메시지는 100% 한국어로 작성)
   - **상태 및 모드 교차 검증:** UI 모드(`CursorType.UI`)와 인게임 입력이 교차하는 모든 상태 전이를 사전에 추적하여 입력 먹통 및 오작동을 원천 차단합니다.
   - **타입 안정성 보장:** Unity Input System 컨트롤 타입(`float`, `Vector2` 등)을 추측하지 않고 안전하게 처리하여 런타임 예외를 방지합니다.
   - **조작 분리 준수:** 아이템별 좌클릭(Primary: 도구 기능)과 우클릭(Secondary: UI/조준/모드 토글) 역할을 명확히 분리하여 구현합니다.
   - **실시간 RPC 우선 (SyncVar 지양):** 실시간 게임플레이 상호작용은 틱 지연 및 순서 꼬임을 방지하기 위해 명시적인 `[Command] ➔ [ClientRpc]` 즉시 동기화 파이프라인을 사용합니다. (스폰 초기 메타데이터에만 SyncVar 한정)

---

## 1. 프로젝트 개요 및 환경 (Tech Stack)

- **Engine:** Unity 6 (6000.3.16f1)
- **Physics:** 3D Rigidbody (Linear Velocity는 `rb.linearVelocity` 사용)
- **Networking:** Mirror Networking (kcp / FizzyFacepunch Steam Transport)
- **Platform:** Steamworks.NET / Facepunch.Steamworks
- **Animation / Tweening:** Unity Animator (1인칭 / 3인칭 오버라이드 지원), DOTween, LitMotion
- **Resources:** `Resources.Load` 및 `LazyAddressable` (Addressables 조건부 폴백 지원)

---

## 2. 핵심 아키텍처 & 디렉토리 구조

```
Assets/
├── 00_Asset/              # 3D 모델, 텍스처, 사운드 등 원본 에셋 (Flashlight, Phone 등)
├── Mirror/                # Mirror 네트워킹 핵심 라이브러리
├── Prefabs/               # 일반 프리팹 (PlayerEntry 등)
├── Resources/
│   ├── ItemData/          # 아이템 ScriptableObject 데이터 (ItemData_Flashlight, ItemData_Phone 등)
│   └── Network/           # [중요] 모든 NetworkIdentity 스폰 가능 프리팹 보관 디렉토리
│       ├── Item_Flashlight.prefab
│       ├── Item_Phone.prefab
│       ├── ItemVendingMachine.prefab
│       ├── GamePlayer.prefab
│       └── LobbyPlayer.prefab
├── Scenes/                # 메인 씬 (MainMenuScene, LobbyScene, GameScene)
└── Scripts/
    ├── Item/
    │   ├── Base/          # Item.cs, ItemHolder.cs, ItemManager.cs, HandyItemObject.cs
    │   ├── Data/          # ItemData.cs (ScriptableObject)
    │   ├── Implement/     # FlashlightItem.cs, PhoneItem.cs 등 구체적 아이템 구현
    │   └── ItemVendingMachine.cs  # 아이템 자판기
    ├── Network/           # CustomNetworkManager.cs, SteamLobby.cs 등
    ├── Player/            # Player.cs, PlayerMove.cs, PlayerRotate.cs, PlayerItemInteraction.cs
    └── Utils/             # MonoSingleton.cs, NetworkSingleton.cs, NetworkUuid.cs, LazyResource.cs
```

---

## 3. 네트워크(Mirror) 프로그래밍 규칙

1. **프리팹 보관 위치:**
   - 네트워크 상에서 `NetworkServer.Spawn`으로 동적 생성되는 모든 프리팹은 반드시 **`Assets/Resources/Network/`** 폴더 아래에 위치해야 합니다.
   - `CustomNetworkManager.EnsurePrefabsLoaded()`가 `Resources/Network`의 모든 프리팹을 자동으로 `spawnPrefabs`에 등록하고 `NetworkClient.RegisterPrefab`을 수행합니다.

2. **씬 오브젝트와 동적 스폰:**
   - 씬 파일에 `NetworkIdentity`를 가진 정적 오브젝트를 하드코딩하지 않습니다 (씬 ID 충돌 및 미싱 방지).
   - 공용 시설(예: 자판기 `ItemVendingMachine`)은 씬 전환 시 서버(`OnServerSceneChanged` 또는 `OnServerReady`)에서 네트워크 프리팹을 동적으로 스폰(`NetworkServer.Spawn`)합니다.

3. **소유권 및 LocalPlayer 검사 안전성:**
   - 런타임에 동적으로 `AddComponent`되는 컴포넌트는 `NetworkBehaviour` 대신 **`MonoBehaviour`**를 사용하고, 부모/루트 `Player`의 `player.isLocalPlayer` / `player.isOwned`를 검사합니다.
   - `MonoSingleton.Instance` 호출 시 에디터 비재생 상태에서 불필요한 예외(`throw Exception`)를 던지지 않고 안전한 폴백을 수행합니다.

4. **네트워크 동기화 컴포넌트 GUID:**
   - Mirror `NetworkIdentity`: `guid: 9b91ecbcc199f4492b9a91e820070131`
   - Mirror `NetworkTransformReliable`: `guid: 8ff3ba0becae47b8b9381191598957c8`

---

## 4. 아이템 시스템 구조 (Item Pipeline)

### A. 아이템 데이터 (`ItemData.cs`)
- `ScriptableObject` 기반으로 무게, 줍기/던지기 가능 여부, 던지기 힘 배율, 1인칭 손 애니메이터 오버라이드(`handAnimatorOverride`), 손 부착 오프셋(`holdPositionOffset`, `holdRotationOffset`)을 정의합니다.
- 저장 위치: `Assets/Resources/ItemData/ItemData_{ItemName}.asset`

### B. 아이템 엔티티 (`Item.cs`)
- 바닥에 놓여 있을 때(`Grounded`), 손에 쥐어졌을 때(`Held`), 던져졌을 때(`Thrown`)의 상태 머신을 관리합니다.
- 손에 쥐어질 때 `NetworkTransform` 동기화를 자동으로 끄고 손 트랜스폼의 자식으로 고정하며, 떨어지거나 던져질 때 다시 켜고 물리 속도를 부여합니다.

### C. 플레이어 아이템 홀더 (`ItemHolder.cs` & `PlayerItemInteraction.cs`)
- **조작 매핑 (Team_Secret_Boar 정통 키배치):**
  - **`F` 키**: 시선에 있는 아이템 줍기(`TryPickup`) / 자판기 상호작용
  - **`G` 키**: 들고 있는 아이템 바닥에 버리기(`TryDrop`)
  - **`마우스 좌클릭 / 우클릭`**: 들고 있는 아이템 기능 사용(`TryUseHoldingItem`) - 손전등 라이트 토글, 폰 화면 등
  - **`마우스 휠`**: 인벤토리 슬롯 전환
- 1인칭 카메라와 손 소켓에 맞춰 아이템을 안정적으로 정렬하고 들고 있는 아이템에 맞는 손 애니메이션 오버라이드를 자동 교체합니다.

### D. 아이템 스폰 관리 (`ItemManager.cs` & `ItemVendingMachine.cs`)
- 서버 전용 `ItemManager.SpawnItem(prefab, position, rotation, data)`를 통해 네트워크 풀링 및 동기화 스폰을 수행합니다.

---

## 5. 코딩 및 에셋 작업 원칙

1. **문서 및 Wiki 실시간 동기화 (Living Documentation - 필수):**
   - 스크립트를 추가, 삭제, 수정하거나 새로운 시스템/기능을 구현한 경우 **반드시 관련된 MD 문서도 함께 업데이트**합니다:
     - 새 스크립트 추가/역할 변경 시: **[`.agents/docs/layout.md`](.agents/docs/layout.md)** 업데이트
     - 시스템 구조, 파이프라인, 상태 머신 변경 시: **[`.agents/docs/wiki/`](.agents/docs/wiki/)** 내 해당 위키 문서 업데이트
     - 기술 스택/API 정책 변경 시: **[`.agents/docs/stack.md`](.agents/docs/stack.md)** 업데이트
   - 문서 업데이트를 누락하는 것은 코드 작성 미완료와 동일하게 취급합니다.
2. **추측성 시그니처 작성 금지:**
   - 기존 클래스를 상속하거나 메서드를 오버라이드할 때 반드시 `view_file`로 원본 코드를 확인하고 작성합니다.
3. **Unity 6 API 준수:**
   - Rigidbody의 속도 설정은 `linearVelocity`를 사용합니다.
4. **직렬화 필드 100% 매핑:**
   - 생성하거나 수정한 프리팹의 `SerializeField` 레퍼런스(MeshFilter, MeshRenderer, Collider, Rigidbody, AudioSource 등)가 인스펙터 상에서 `Missing`이나 `None`이 되지 않도록 에셋 GUID 및 SubMesh ID를 정확하게 연결합니다.
5. **시선 기반 Raycast 상호작용:**
   - 모든 오브젝트 상호작용은 카메라 시선 레이캐스트(Raycast)를 기본으로 하여 FPS 조준에 맞게 정밀하게 반응하도록 구성합니다.
