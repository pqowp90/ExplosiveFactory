# 플레이어 시스템 위키 (Player System Architecture)

> 본 문서는 `ExplosiveFactory`의 1인칭 플레이어 캐릭터, 이동/물리 역학, 마우스 회전, 시선 기반 상호작용 및 애니메이션 시스템을 다룹니다.

---

## 1. 플레이어 오브젝트 계층 구조

```
GamePlayer (NetworkIdentity, Player, LocalPlayerSetter)
├── Rigidbody (Unity 6 Physics)
├── CapsuleCollider
├── FirstPersonView (로컬 플레이어만 활성화)
│   ├── Main Camera (FPS 카메라, CameraShocShak, InteractiveRaycast)
│   └── Arms_Root (1인칭 손 모델, SwayNBobScript, HoldSocket)
│       └── HoldSocket (현재 들고 있는 Item 프리팹의 부모)
└── ThirdPersonView (원격 플레이어에게만 렌더링, 그림자 전용)
    └── BodyMesh (3인칭 캐릭터 모델, LookAtController, PlayerAnimation)
```

---

## 2. 이동 및 물리 시스템 (`PlayerMove.cs`)

- **Unity 6 API 준수:**
  - `rb.velocity` 대신 **`rb.linearVelocity`**를 직접 조작하여 가속/감속 및 공중 제어를 수행합니다.
- **지면 검사 (Ground Check):**
  - SphereCast 또는 Raycast를 바닥으로 발사하여 `isGrounded` 상태를 판별합니다.
  - 경사면(Slope) 이동 시 지면 법선 벡터(Normal)를 계산하여 미끄러짐 방지 및 부드러운 오르막 이동을 지원합니다.
- **이동 상태:**
  - 걷기(`WalkSpeed`), 달리기(`SprintSpeed`), 앉기(`CrouchSpeed`), 점프(`JumpForce`).

---

## 3. 회전 및 시선 처리 (`PlayerRotate.cs` & `InteractiveRaycast.cs`)

- **수평 회전 (Yaw):**
  - 마우스 X 입력을 플레이어 몸체 루트 트랜스폼(`transform.rotation`)에 직접 적용하여 회전합니다.
- **수직 회전 (Pitch):**
  - 마우스 Y 입력을 1인칭 카메라 트랜스폼(`cameraTransform.localRotation`)에만 적용하며, 상하 각도를 `-80도 ~ +80도` 범위로 클램핑(Clamp)합니다.
- **시선 기반 상호작용 및 버리기 (`InteractiveRaycast.cs` & `InputController.cs`):**
  - 매 프레임 1인칭 카메라 중심에서 정면으로 `Raycast`를 발사합니다 (`maxDistance: 약 2.5m ~ 3m`).
  - 레이캐스트 히트 대상에서 `IInteractable` / `InteractableObject` / `Item` 컴포넌트를 검출합니다.
  - 감지 시 중앙 조준점(Crosshair) 상태를 인터랙션 가능 모드로 전환하고, `F` 키(`InteractAction`) 입력 시 `Interact()`를 호출합니다.
  - 버리기 입력(`DropAction` 또는 `G` 키) 감지 시 `ItemHolder.DropItem()`을 호출하여 들고 있는 아이템을 안전하게 드롭합니다.

---

## 4. 로컬 vs 리모트 플레이어 분리 (`LocalPlayerSetter.cs`)

- `isLocalPlayer` (또는 `isOwned`)가 `true`인 경우:
  - 1인칭 카메라 및 오디오 리스너 활성화
  - 3인칭 몸체 메시는 `ShadowsOnly` 모드로 전환하여 카메라 가림 방지
  - 입력 처리기(`InputController`) 활성화
- `isLocalPlayer`가 `false`인 원격 플레이어:
  - 1인칭 카메라 및 입력 스크립트 비활성화
  - 3인칭 몸체 메시 완전 활성화
  - Mirror의 `NetworkTransformReliable`을 통해 위치와 회전만 수신하여 동기화

---

## 5. 절차적 상체/척추 시선 제어 (`LookAtController.cs`)

- **아이템 파지 시 상체 일체화 및 골반 흔들림 차단:**
  - 아이템 파지(`IsHoldingItem`) 시, 하체 걷기/달리기 애니메이션에 의한 골반(Hips)의 좌우 롤링/요동침이 상체로 전파되지 않도록 `Spine` 및 `Chest` 본의 월드 회전을 플레이어 정면(Yaw)으로 안정화하고 허리/가슴 50/50으로 단단하게 조준합니다.
- **빈손 상태의 자연스러운 LookAt IK 시선 제어:**
  - 빈손 상태에서는 `LateUpdate`의 중복 각도 가산을 배제하고, Unity의 휴머노이드 `LookAt IK`(`bodyWeight: 0.15`, `headWeight: 0.85`)에 시선 제어를 위임하여 과도한 꺾임 없이 사람처럼 자연스러운 인체 곡선으로 시선을 처리합니다.

---

## 6. 1인칭 전용 다리 시스템 (`FirstPersonLegsController.cs`)

- **3인칭 전신 모델 (`PlayerBodyTransform`):**
  - 로컬 플레이어 시점에서 `ShadowsOnly` 모드로 동작하여 바닥에 완벽한 전신 사람 그림자를 캐스팅.
- **1인칭 전용 다리 (`PlayerLegTransform` / `FirstPersonLegsController`):**
  - 로컬 플레이어 화면에만 렌더링되며 그림자는 비활성화(`ShadowCastingMode.Off`).
  - 팔(`LeftShoulder`, `RightShoulder`, `LeftArm`, `RightArm`) 및 머리/목(`Head`, `Neck`) 본의 스케일을 `(0, 0, 0)`으로 축소하여 시야 가림 원천 제거.
  - `Spine`(허리) 본을 카메라 뒤쪽(`Z: -0.18m`)으로 오프셋하여 고개를 푹 숙여도 가슴/목 뚫림 없이 바지와 다리, 걸어가는 발걸음만 깨끗하게 렌더링.
  - `PlayerAnimation.cs`를 통해 이동/점프/앉기/회전 애니메이션 파라미터 100% 동기화.


