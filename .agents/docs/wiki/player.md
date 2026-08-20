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
  - 아이템 파지(`IsHoldingItem`) 시, 하체 걷기/달리기 애니메이션에 의한 골반(Hips)의 좌우 롤링/요동침이 상체로 전파되지 않도록 `Spine` 및 `Chest` 본의 월드 회전을 플레이어 정면(Yaw)으로 안정화합니다.
  - 빈손 상태에서는 IK와 상체 고정을 완전 비활성화하여 자연스러운 전신 달리기 모션을 유지합니다.
- **허리 50% + 가슴 50% 다중 관절 상하 피치 분배:**
  - 시선 상하 각도(Pitch, 최대 ±80°)를 `Spine`(50%)과 `Chest`(50%)에 균등 분배하여 위/아래를 볼 때 100% 각도가 시원하게 회전하며 자연스러운 인체 척추 곡선을 형성합니다.

