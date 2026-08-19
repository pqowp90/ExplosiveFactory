# Item Pipeline Rules & Architecture

## 1. 아이템 데이터 (ItemData ScriptableObject)
- **저장 경로:** `Assets/Resources/ItemData/ItemData_{ItemName}.asset`
- **필수 속성:**
  - 무게(`weight`), 줍기/던지기 가능 여부(`canPickup`, `canThrow`)
  - 던지기 힘 배율(`throwForceMultiplier`)
  - 1인칭 손 애니메이터 오버라이드(`handAnimatorOverride`)
  - 손 부착 트랜스폼 오프셋(`holdPositionOffset`, `holdRotationOffset`)

## 2. 아이템 엔티티 (Item.cs)
- **상태 머신:** `Grounded` (바닥), `Held` (손에 쥠), `Thrown` (던져짐)
- 손에 쥐어질 때(`Held`): `NetworkTransform` 동기화를 끄고 손 트랜스폼의 자식으로 고정.
- 바닥에 떨어지거나 던져질 때(`Grounded` / `Thrown`): `NetworkTransform` 동기화를 다시 켜고 물리 속도 부여.

## 3. 아이템 프리팹 (Item Prefab)
- **저장 경로:** `Assets/Resources/Network/Item_{ItemName}.prefab`
- **필수 컴포넌트:**
  - `NetworkIdentity`
  - `NetworkTransformReliable`
  - `Rigidbody` (Collision detection: Continuous 권장)
  - `Collider` (상호작용 및 물리 충돌용)
  - 구체 아이템 클래스 (`{ItemName}Item : Item`)
