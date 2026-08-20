# 기술 스택 요약 (Tech Stack & Environment)

> 본 문서는 `ExplosiveFactory`의 개발 환경, 엔진 버전, 주요 라이브러리 및 API 사용 컨벤션을 정의합니다.

---

## 1. 기본 환경

- **Engine:** Unity 6 (6000.3.16f1)
- **Scripting Backend:** .NET 8 / C# 9.0+
- **Assembly Definition:** `Assets/Scripts/ExplosiveFactory.Scripts.asmdef`
- **Render Pipeline:** Universal Render Pipeline (URP)

---

## 2. 핵심 라이브러리 및 패키지

| 영역 | 라이브러리 / 패키지 | 용도 및 주의사항 |
|---|---|---|
| **Networking** | Mirror Networking (kcp / FizzyFacepunch) | 멀티플레이어 동기화, `NetworkIdentity`, `NetworkTransformReliable`, `SyncVar`, `Command`, `ClientRpc` |
| **Platform** | Steamworks.NET / Facepunch.Steamworks | Steam 로비, P2P 매칭, 친구 초대 및 상태 조회 |
| **Physics** | Unity 3D Rigidbody Physics | **Unity 6 API 준수:** `rb.velocity` 대신 반드시 **`rb.linearVelocity`** 및 `rb.angularVelocity` 사용 |
| **Animation** | Unity Animator + OverrideController | 1인칭 손/아이템 애니메이션 오버라이드 및 3인칭 절차적 LookAt 제어 |
| **Tweening** | DOTween, LitMotion | UI 애니메이션, 카메라 쉐이크, 부드러운 전환 연출 |
| **Resource Loading** | `Resources.Load` + `LazyResource<T>` / `LazyAddressable<T>` | 네트워크 프리팹 및 아이템 데이터 동적 로딩 |

---

## 3. 코드 컨벤션 및 네트워킹 핵심 규칙

1. **Unity 6 물리 속도 접근:**
   - ❌ `rb.velocity = moveDir * speed;`
   - ⭕ `rb.linearVelocity = moveDir * speed;`

2. **네트워크 프리팹 보관 위치:**
   - ❌ `Assets/Prefabs/` 또는 씬에 정적 배치된 NetworkIdentity
   - ⭕ **`Assets/Resources/Network/`** (자동 등록 및 `NetworkServer.Spawn` 보장)

3. **로컬 플레이어 검사 안전성:**
   - 동적으로 `AddComponent`되는 스크립트는 `MonoBehaviour`를 사용하고, 부모 `Player`의 `player.isLocalPlayer` / `player.isOwned`를 검사합니다.
