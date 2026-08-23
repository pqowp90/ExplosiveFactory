# 플레이어 시스템 위키 (Player System Architecture)

> 본 문서는 `ExplosiveFactory`의 1인칭 플레이어 캐릭터, 이동/물리 역학, 마우스 회전, 시선 기반 상호작용 및 애니메이션 시스템을 다룹니다.

---

## 1. 플레이어 오브젝트 계층 구조

```
GamePlayer (NetworkIdentity, Player, LocalPlayerSetter, FirstPersonLegsSetup, PlayerAnimation)
├── Rigidbody (Unity 6 Physics)
├── CapsuleCollider
├── Body (PlayerBodyTransform - CustomNetworkAnimator, LookAtController, FootIKController)
│   └── 3D 캐릭터 모델 (교체 대상 인스턴스, Animator, AnimatorIKForwarder)
├── FirstPersonView (로컬 플레이어만 활성화)
│   ├── Main Camera (FPS 카메라, CameraShocShak, InteractiveRaycast)
│   └── Arms_Root (1인칭 손 모델, SwayNBobScript, HoldSocket)
│       └── HoldSocket (현재 들고 있는 Item 프리팹의 부모)
└── FirstPersonLegs (로컬 플레이어 전용 동적 생성 다리)
    └── LegsMesh (FirstPersonLegsController, FootIKController)
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
  - 로컬 플레이어 화면에만 활성화 및 렌더링되며, 리모트 플레이어는 3인칭 몸체만 렌더링.
  - **트랜스폼 분리 기반 회전 피봇 고정 & 1인칭 전용 몸체 후방 오프셋:** 부모(`Body`)의 회전축을 원점(0,0)에 완전 고정하여 60도 턴/회전 버티기 시 공전(궤도 회전) 및 흔들림을 원천 차단하고, 1인칭 로컬 플레이어 시점에서만 `LookAtController`의 로컬 Z 오프셋(`firstPersonBodyBackwardOffset`)과 1인칭 다리를 뒤로 빼주어 자연스러운 시야 및 그림자 정렬 완성.
  - **서 있을 때 / 앉았을 때(Standing & Crouching) 개별 오프셋 및 실시간 보간:** 서 있을 때(`spineBackwardOffset` 등)와 앉았을 때(`crouchSpineBackwardOffset` 등)의 상체 각 부위별 오프셋을 프리팹 인스펙터에서 완전히 독립적으로 세팅할 수 있으며, 앉거나 일어설 때 상태 전이(`crouchRatio`)에 따라 부드럽게 실시간 Lerp 보간.
  - **앉기(Crouch) 시 지면 높이(Y) 동기화:** 앉을 때 CharacterController 높이 축소로 인한 루트 하강을 보정하기 위해, `basePos.y`를 `PlayerBodyTransform.position.y`와 실시간 동기화하여 앉을 때도 다리와 발이 바닥에 파묻히거나 공중에 뜨지 않고 완벽한 지면 접지 유지.
  - **프리팹 인스펙터 직렬화 설정(`FirstPersonLegsSettings`):** 불필요한 루트 위치/높이/커스텀 오프셋을 제거하고, 상체 각 부위별(Spine, Chest, UpperChest) 후방 및 상하 오프셋 설정만 깔끔하게 노출하여 프리팹 에셋에서 직관적으로 관리.
  - **무회전 점진적 상체 슬랜트(Slanted) 후방 오프셋:** 본의 억지 회전(Tilt)을 0으로 배제하고 다리와 골반(Hips)은 제자리에 고정하며, 척추 체인(`Spine: 0.15m, Up: 0.27m` ➔ `Chest: +0.15m, Up: 0.15m` ➔ `UpperChest: -0.16m`)을 점진적으로 뒤로 빼주어, 관절 왜곡 없이 상체만 비스듬하게 뒤로 쑥 물러난 사선형 1인칭 시야를 구현.
  - **카메라 시선 수평 후방(`camBackward`) 절대 기준 오프셋:** 좌우 이동(스트레이프)이나 회전으로 인해 몸체가 옆으로 돌아가더라도, 다리 루트 및 `Spine`/`Chest` 본의 후방 오프셋을 몸체 로컬이 아닌 **'카메라의 수평 시선 후방'**으로 계산하여 적용함으로써 좌우 이동 시 몸이 옆으로 삐져나가는 현상을 완벽 차단.
  - **카메라 피치(Pitch) 가변 보정:** 고개를 아래로 숙일수록(`Pitch > 0`) 상체를 카메라 뒤쪽으로 추가 당김(`pitchBackwardMultiplier: 0.18m`)으로써 고개를 최대로 숙여도 가슴/등이 카메라 시야를 뚫지 않고 다리와 발걸음만 깨끗하게 표시.
  - `PlayerAnimation.cs`를 통해 이동/점프/앉기/회전/앉기 애니메이션 파라미터 100% 동기화.

---

## 7. 경사면 및 지형 대응 Foot IK 시스템 (`FootIKController.cs`)

- **Unity Humanoid Foot IK & OnAnimatorIK:**
  - `Player.controller`의 Base Layer(`IK Pass: 1`)를 활용하여 매 프레임 `OnAnimatorIK`에서 양 발(`AvatarIKGoal.LeftFoot`, `AvatarIKGoal.RightFoot`)의 지면 접지 및 회전, 골반 높이를 절차적으로 보정합니다.
- **Y축 높이 오프셋 독립 보간 (X/Z 수평 지연 100% 제거):**
  - 발의 수평 위치(X, Z)는 애니메이션 원본을 실시간 100% 즉시 추종하고, 보정이 필요한 Y축 높이 오프셋(`_leftFootOffsetY`, `_rightFootOffsetY`)과 회전만 기민하게 보간함으로써 고속 달리기 시 발이 뒤로 밀리거나 처지는 지연(Lag)을 원천 차단합니다.
- **달리기/이동 적응형 동적 반응 속도 (Speed-Adaptive Responsiveness):**
  - 보행/달리기 상태(`PlayerMove.IsRunning` 또는 이동 속도)에 따라 보간 속도를 동적으로 증폭(`runningSpeedMultiplier: 1.8x`)하고, 이동 방향 전방 예측(`forwardPredictionDistance`)을 통해 고속 질주 중에도 스텝 템포에 맞춰 즉시 지면에 착 달라붙도록 반응성을 극대화합니다.
- **지면 법선(Normal) 기반 발 회전 정렬:**
  - 양 발의 위치에서 `Physics.Raycast`를 하향 발사하여 충돌 지면의 법선 벡터(`hit.normal`)를 검출하고, 경사각에 맞춰 발바닥을 자연스럽게 기울여 밀착(`Quaternion.FromToRotation(Vector3.up, hit.normal)`)시킵니다.
- **골반(Hips 본) 기준 레이캐스트 기반 몸체/골반 높이 제어 (Hips-Raycast Pelvis Control):**
  - 캐릭터 모델의 실제 골반 본(`HumanBodyBones.Hips`) 위치(X, Z)에서 수직 하향으로 발사되는 단일 레이캐스트(청록색 기즈모)를 통해 지면 단차를 측정하고 몸체(`animator.bodyPosition`) 높이를 안정적으로 보정합니다.
  - **경사면 적응형 골반 하강 (Slope Pelvis Drop):** 경사면의 각도(`slopeAngle`)에 비례하여 몸체를 부드럽게 추가 하강(`slopePelvisDrop: 0.12m`)시킴으로써, 경사로에서 다리가 굳지 않고 자연스럽게 무릎을 굽혀 무게중심을 낮추는 인체 역학적 자세를 완성합니다.
  - 레이캐스트 발사 시작 높이를 실제 바닥 기준선(`baseGroundY + 0.55m`)으로 통일하여, 서 있을 때 과도하게 높이 뜨지 않고 앉았을 때도 동일한 무릎 높이에서 안정적으로 지면을 감지합니다.
- **양발 독립 접지 및 경사면 회전 (Independent Feet IK):**
  - 양 발은 중앙 지면에 맞춘 몸체를 바탕으로 각자의 바닥에 착 달라붙고 경사면 법선(Normal) 각도에 맞게 회전합니다.
- **AAA 표준 애니메이션 커브 연동 (`LeftFootIK`, `RightFootIK`):**
  - 애니메이션 클립마다 발이 땅을 딛는 구간(1.0)과 공중에 뜨는 스윙 구간(0.0)을 Float 커브로 정밀 제어하여, 제자리 회전(Turn)이나 달리기 시 발을 들어 올리는 모션을 온전히 보존하면서 착지 순간에만 경사면 IK가 찰싹 밀착되도록 합니다.
  - 커브가 누락된 클립은 높이 기반 스마트 절차적 Fallback을 통해 정상 작동합니다.
- **스윙 페이즈(Foot Swing) 및 체공 보호:**
  - 보행/달리기 중 발이 위로 들리는 타이밍에는 원래 애니메이션 높이를 우선 적용하여 발이 땅에 질질 끌리지 않도록 보호하며, 점프나 체공(`!isGrounded`) 시에는 IK 가중치를 0으로 부드럽게 감쇄합니다.
- **3인칭 및 1인칭 완벽 공유:**
  - 3인칭 몸체(`PlayerBodyTransform`)와 1인칭 다리(`FirstPersonLegs`) 모두에 `FootIKController`가 자동 바인딩되어 동작합니다.

---

## 8. PlayerComponent 기반 아키텍처 (`PlayerComponent.cs`)

- **지연 캐싱 (Lazy Caching):**
  - 모든 플레이어 하위 컴포넌트(`FootIKController`, `FirstPersonLegsController`, `FirstPersonLegsSetup`, `LookAtController` 등)가 `PlayerComponent`를 상속받아, 런타임에 부모 `Player` 엔티티를 자동으로 지연 캐싱합니다.
- **손쉬운 서브시스템 상호 참조:**
  - `PlayerMove`, `PlayerRotate`, `PlayerAnimation`, `ItemHolder`, `InputController`, `LegsSetup`, `Camera`, `IsOwned` 등의 편의 프로퍼티를 즉시 제공하여 `GetComponent` 보일러플레이트와 NullReference 예외를 원천 방지합니다.

---

## 9. 3D 캐릭터 모델 교체 및 스킨 시스템 (`PlayerSkinController.cs`)

- **ScriptableObject 기반 스킨 데이터 관리 (`PlayerSkinData.cs` & `PlayerSkinDatabase.cs`):**
  - 캐릭터 3D 모델 프리팹, 이름, 설명, UI 아이콘을 ScriptableObject 에셋으로 관리합니다 (`Resources/SkinData/`).
- **네트워크 멀티플레이어 실시간 동기화:**
  - `PlayerSkinController` (`NetworkBehaviour`)가 `SyncVar`, `[Command]`, `[ClientRpc]`를 통해 모델 변경을 모든 접속자에게 실시간 브로드캐스팅합니다.
  - 중도 접속자도 `SyncVar hook`을 통해 다른 플레이어들의 현재 스킨을 즉시 인스턴스화하여 렌더링합니다.
- **자동 리바인딩 파이프라인:**
  - 모델 교체 시 `Body` 오브젝트 하위의 기존 모델을 제거하고 새 프리팹을 스폰한 뒤, `Player.SetPlayerBody(newModelTransform)`를 호출합니다.
  - `CustomNetworkAnimator`가 새 Animator에 기존 동기화 파라미터를 일괄 주입하고, `LookAtController` 및 `FootIKController`가 새 본(Bones)을 자동 바인딩합니다.
- **핸드폰 UI 연동 (`ModelSelectUI.cs`):**
  - 스마트폰 인게임 UI(`CellPhone.prefab`)의 '외형 변경' 앱을 통해 실시간으로 스킨을 선택하고 교체할 수 있습니다.

