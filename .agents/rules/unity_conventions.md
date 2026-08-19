# Unity 6 C# Coding & Physics Conventions

## 1. Unity 6 물리 (Physics)
- `Rigidbody`의 선형 속도 지정/조회 시 반드시 **`linearVelocity`**를 사용합니다 (`velocity`는 Unity 6에서 deprecated).
- 각속도는 `angularVelocity`를 사용합니다.

## 2. 직렬화 및 컴포넌트 레퍼런스
- `[SerializeField]` 필드는 인스펙터 상에서 누락(Missing / None)되지 않도록 100% 바인딩을 유지합니다.
- 추측성 시그니처 작성을 금지하며, 상속/오버라이드 전 항상 베이스 코드를 먼저 확인합니다.

## 3. 레이캐스트 및 인터랙션
- 플레이어의 상호작용은 카메라 시선 기준(Raycast)으로 수행합니다.
- 인터랙션 거리 및 레이어마스크를 철저히 검증하여 정확한 조준 반응을 보장합니다.

## 4. 싱글톤 패턴 안전성
- `MonoSingleton.Instance` 접근 시 에디터 미재생 상태나 씬 언로드 시점에 예외(`throw Exception`)를 던지지 않도록 안전한 널 처리를 보장합니다.
