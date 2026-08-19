---
name: create-item
description: ExplosiveFactory 프로젝트에서 새로운 인터랙션 아이템을 완벽하게 생성하고 파이프라인에 등록하는 표준 워크플로우
---

# 새 아이템 생성 가이드 (Create Item Workflow)

이 스킬은 `ExplosiveFactory` 프로젝트에 새로운 아이템을 추가할 때 필요한 전체 파이프라인을 안내합니다.

## 단계별 생성 절차

### 1단계: 구현 스크립트 작성
- 경로: `Assets/Scripts/Item/Implement/{ItemName}Item.cs`
- `Item` 기본 클래스를 상속받아 생성합니다.
- 마우스 좌클릭(`OnUsePrimary`), 우클릭(`OnUseSecondary`) 등의 기능 오버라이드를 구현합니다.

### 2단계: ItemData ScriptableObject 생성
- 경로: `Assets/Resources/ItemData/ItemData_{ItemName}.asset`
- 무게, 줍기/던지기 가능 여부, 손 위치/회전 오프셋 설정.

### 3단계: 네트워크 프리팹 생성
- 경로: `Assets/Resources/Network/Item_{ItemName}.prefab`
- 컴포넌트 구성:
  - `NetworkIdentity`
  - `NetworkTransformReliable`
  - `Rigidbody` (Interpolate: Interpolate, Collision Detection: Continuous 권장)
  - `Collider` (상호작용용)
  - `{ItemName}Item` (구현 스크립트 연결)

### 4단계: 자판기/스폰 매니저 등록 확인
- 필요 시 `ItemVendingMachine`이나 `ItemManager`에 등록하여 테스트를 진행합니다.
