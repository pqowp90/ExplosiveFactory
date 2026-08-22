using UnityEngine;

/// <summary>
/// 플레이어 3인칭 바디 및 1인칭 다리에 적용되는 경사면/지형 대응 발 IK(Foot IK) 제어기.
/// - AAA 게임 표준: Animator의 LeftFootIK / RightFootIK 커브(0.0~1.0)를 읽어와 발의 접지(Stance)와 스윙(Swing) 페이즈를 정밀 제어합니다.
/// - 발걸음 템포에 맞춘 민첩하고 즉각적인 반응성(Responsive & Snappy)을 제공합니다.
/// - Unity Humanoid OnAnimatorIK를 통해 양발 각각 독립적인 지면 레이캐스트를 발사하고 발바닥을 경사면 법선에 밀착시키며 골반 높이를 보정합니다.
/// </summary>
[DisallowMultipleComponent]
public class FootIKController : PlayerComponent
{
    [Header("Foot IK General Settings")]
    [Tooltip("발 IK 활성화 여부")]
    public bool enableFootIK = true;

    [Tooltip("경사면 및 지면 감지 레이어마스크")]
    public LayerMask groundLayers = ~((1 << 2) | (1 << 3) | (1 << 5) | (1 << 6) | (1 << 11)); // Ignore Raycast(2), Player(3), UI(5), Arm(6), ItemLayer(11) 제외

    [Header("Animation Curve Settings (AAA Standard)")]
    [Tooltip("애니메이션 클립의 Foot IK 커브 사용 여부")]
    public bool useAnimationCurves = true;

    [Tooltip("왼발 애니메이터 커브 파라미터 이름")]
    public string leftFootCurveName = "LeftFootIK";

    [Tooltip("오른발 애니메이터 커브 파라미터 이름")]
    public string rightFootCurveName = "RightFootIK";

    [Header("Raycast Settings")]
    [Tooltip("캐릭터 바닥 기준 레이캐스트 발사 시작 높이 (단위: m, 기본 0.55m)")]
    public float raycastStartHeight = 0.55f;

    [Tooltip("레이캐스트 하향 탐색 총 길이 (단위: m, 기본 1.2m)")]
    public float raycastLength = 1.2f;

    [Tooltip("신발 밑창/발바닥 높이 오프셋 (단위: m, 기본 0.08m)")]
    public float footBottomOffset = 0.08f;

    [Tooltip("발바닥 중앙 감지를 위한 전방 오프셋 (단위: m, 기본 0.05m)")]
    public float footForwardOffset = 0.05f;

    [Header("Slope & Rotation Settings")]
    [Tooltip("경사면에 맞춰 발 회전 적응 여부")]
    public bool enableFootRotation = true;

    [Tooltip("최대 반영 경사각 (도)")]
    [Range(0f, 80f)]
    public float maxSlopeAngle = 60f;

    [Header("Pelvis & Center Raycast Settings")]
    [Tooltip("플레이어 중심 지면 감지를 통한 몸체(Pelvis) 높이 보정 활성화")]
    public bool enablePelvisCorrection = true;

    [Tooltip("경사면 각도에 따른 몸체 추가 하강 거리 (단위: m, 기본 0.12m)")]
    public float slopePelvisDrop = 0.12f;

    [Tooltip("골반(Pelvis) 높이 보간 속도 (기본 15)")]
    public float pelvisCorrectionSpeed = 15f;

    [Tooltip("골반 최대 하향 보정 거리 (단위: m)")]
    public float maxPelvisDrop = 0.7f;

    [Tooltip("골반 최대 상향 보정 거리 (단위: m)")]
    public float maxPelvisRise = 0.5f;

    [Header("Smoothing & Response Settings (Responsive)")]
    [Tooltip("발 위치 및 회전 기본 보간 속도 (민첩하고 즉각적인 반응: 20~25)")]
    public float footPlacementSpeed = 22f;

    [Tooltip("달리기/이동 시 발 IK 반응 속도 증폭 배율 (기본 1.8x)")]
    public float runningSpeedMultiplier = 1.8f;

    [Tooltip("이동 방향 전방 레이캐스트 예측 거리 (단위: m, 기본 0.12m)")]
    public float forwardPredictionDistance = 0.12f;

    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int RunHash = Animator.StringToHash("Run");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");

    private Animator _animator;
    private Transform _hipsTransform;
    private int _leftCurveHash;
    private int _rightCurveHash;

    private float _leftFootOffsetY = 0f;
    private float _rightFootOffsetY = 0f;
    private Vector3 _leftFootPosition;
    private Vector3 _rightFootPosition;
    private Quaternion _leftFootRotation = Quaternion.identity;
    private Quaternion _rightFootRotation = Quaternion.identity;

    private float _leftFootWeight = 0f;
    private float _rightFootWeight = 0f;

    private float _currentPelvisOffset = 0f;
    private bool _isInitialized = false;

    // 디버그 기즈모용
    private Vector3 _debugCenterRayStart;
    private Vector3 _debugCenterHitPoint;
    private Vector3 _debugLeftRayStart;
    private Vector3 _debugRightRayStart;
    private Vector3 _debugLeftHitPoint;
    private Vector3 _debugRightHitPoint;

    private readonly RaycastHit[] _raycastHits = new RaycastHit[8];

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _leftCurveHash = Animator.StringToHash(leftFootCurveName);
        _rightCurveHash = Animator.StringToHash(rightFootCurveName);
    }

    private void Start()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        if (_animator != null && _animator.isHuman)
        {
            _hipsTransform = _animator.GetBoneTransform(HumanBodyBones.Hips);
        }

        _leftCurveHash = Animator.StringToHash(leftFootCurveName);
        _rightCurveHash = Animator.StringToHash(rightFootCurveName);

        if (groundLayers.value == 0)
        {
            groundLayers = ~((1 << 2) | (1 << 3) | (1 << 5) | (1 << 6) | (1 << 11));
        }

        // 플레이어 본체 및 자식에 사용된 모든 레이어를 groundLayers에서 강제 제외
        if (Player != null)
        {
            groundLayers &= ~(1 << Player.gameObject.layer);
            var allCols = Player.GetComponentsInChildren<Collider>(true);
            foreach (var col in allCols)
            {
                groundLayers &= ~(1 << col.gameObject.layer);
            }
        }

        _isInitialized = _animator != null && _animator.isHuman;
    }

    private float GetBaseGroundY()
    {
        if (Player != null && Player.PlayerBodyTransform != null)
        {
            return Player.PlayerBodyTransform.position.y;
        }
        return transform.position.y;
    }

    private float GetSafeRaycastStartY(float baseGroundY)
    {
        return baseGroundY + raycastStartHeight;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        // Base Layer(0번 레이어)에서만 IK 연산 수행
        if (layerIndex != 0 || !enableFootIK) return;

        if (!_isInitialized)
        {
            Initialize();
            if (!_isInitialized) return;
        }

        // 지면에 닿아 있는지 확인:
        // 네트워크 동기화된 Animator 파라미터(Grounded)를 우선 확인하여 원격 3인칭 플레이어에서도 정확히 접지 판정
        bool isGrounded = true;
        if (_animator != null)
        {
            try
            {
                isGrounded = _animator.GetBool(GroundedHash);
            }
            catch
            {
                isGrounded = PlayerMove != null ? PlayerMove.isGrounded : true;
            }
        }
        else if (PlayerMove != null)
        {
            isGrounded = PlayerMove.isGrounded;
        }

        // 공중에 떠 있거나 점프 중일 때는 IK 가중치와 오프셋을 0으로 부드럽게 복귀
        if (!isGrounded)
        {
            _currentPelvisOffset = Mathf.Lerp(_currentPelvisOffset, 0f, Time.deltaTime * pelvisCorrectionSpeed);
            _leftFootOffsetY = Mathf.Lerp(_leftFootOffsetY, 0f, Time.deltaTime * footPlacementSpeed);
            _rightFootOffsetY = Mathf.Lerp(_rightFootOffsetY, 0f, Time.deltaTime * footPlacementSpeed);
            _leftFootWeight = Mathf.Lerp(_leftFootWeight, 0f, Time.deltaTime * footPlacementSpeed);
            _rightFootWeight = Mathf.Lerp(_rightFootWeight, 0f, Time.deltaTime * footPlacementSpeed);

            if (Mathf.Abs(_currentPelvisOffset) > 0.001f)
            {
                _animator.bodyPosition += Vector3.up * _currentPelvisOffset;
            }

            Vector3 animLeft = _animator.GetIKPosition(AvatarIKGoal.LeftFoot);
            Vector3 animRight = _animator.GetIKPosition(AvatarIKGoal.RightFoot);
            ApplyIKGoal(AvatarIKGoal.LeftFoot, animLeft + Vector3.up * _leftFootOffsetY, _leftFootRotation, _leftFootWeight);
            ApplyIKGoal(AvatarIKGoal.RightFoot, animRight + Vector3.up * _rightFootOffsetY, _rightFootRotation, _rightFootWeight);
            return;
        }

        // 이동/달리기 상태에 따른 동적 반응 속도 계산
        float dynamicSpeed = footPlacementSpeed;
        if (_animator != null)
        {
            try
            {
                float runVal = _animator.GetFloat(RunHash);
                float moveX = _animator.GetFloat(MoveXHash);
                float moveY = _animator.GetFloat(MoveYHash);
                float moveSqr = (moveX * moveX) + (moveY * moveY);

                if (runVal > 0.5f)
                {
                    dynamicSpeed *= runningSpeedMultiplier;
                }
                else if (moveSqr > 0.01f)
                {
                    dynamicSpeed *= Mathf.Lerp(1.0f, runningSpeedMultiplier, 0.5f);
                }
            }
            catch
            {
                if (PlayerMove != null)
                {
                    if (PlayerMove.IsRunning)
                    {
                        dynamicSpeed *= runningSpeedMultiplier;
                    }
                    else if (PlayerMove.MoveValue.sqrMagnitude > 0.01f)
                    {
                        dynamicSpeed *= Mathf.Lerp(1.0f, runningSpeedMultiplier, 0.5f);
                    }
                }
            }
        }
        else if (PlayerMove != null)
        {
            if (PlayerMove.IsRunning)
            {
                dynamicSpeed *= runningSpeedMultiplier;
            }
            else if (PlayerMove.MoveValue.sqrMagnitude > 0.01f)
            {
                dynamicSpeed *= Mathf.Lerp(1.0f, runningSpeedMultiplier, 0.5f);
            }
        }

        float baseGroundY = GetBaseGroundY();
        float safeStartY = GetSafeRaycastStartY(baseGroundY);

        // 1. 캐릭터 골반(Hips 본) 기준 레이캐스트로 몸체(Pelvis / Body) 높이 안정적 계산 (앉기 시에도 지면 상단 안전 발사)
        if (enablePelvisCorrection)
        {
            Vector3 centerPos = _hipsTransform != null 
                ? _hipsTransform.position 
                : (Player != null ? Player.transform.position : transform.position);

            Vector3 centerRayStart = new Vector3(centerPos.x, safeStartY, centerPos.z);
            _debugCenterRayStart = centerRayStart;

            int centerHits = Physics.RaycastNonAlloc(centerRayStart, Vector3.down, _raycastHits, raycastLength, groundLayers, QueryTriggerInteraction.Ignore);
            float closestDist = float.MaxValue;
            RaycastHit centerBestHit = default;
            bool centerHitSuccess = false;

            for (int i = 0; i < centerHits; i++)
            {
                RaycastHit hit = _raycastHits[i];
                if (hit.collider == null) continue;
                if (Player != null && (hit.collider.transform.IsChildOf(Player.transform) || hit.collider.transform == Player.transform))
                    continue;

                if (hit.distance < closestDist)
                {
                    closestDist = hit.distance;
                    centerBestHit = hit;
                    centerHitSuccess = true;
                }
            }

            float targetPelvisDelta = 0f;
            if (centerHitSuccess)
            {
                _debugCenterHitPoint = centerBestHit.point;
                float groundDelta = centerBestHit.point.y - baseGroundY;

                // 경사각(Slope Angle)에 따른 자연스러운 무릎 굽힘 및 무게중심 하강
                float slopeDrop = 0f;
                if (slopePelvisDrop > 0f && maxSlopeAngle > 0f)
                {
                    float slopeAngle = Vector3.Angle(Vector3.up, centerBestHit.normal);
                    float slopeFactor = Mathf.Clamp01(slopeAngle / maxSlopeAngle);
                    slopeDrop = slopeFactor * slopePelvisDrop;
                }

                targetPelvisDelta = Mathf.Clamp(groundDelta - slopeDrop, -maxPelvisDrop, maxPelvisRise);
            }
            else
            {
                _debugCenterHitPoint = centerRayStart + Vector3.down * raycastLength;
                targetPelvisDelta = 0f;
            }

            _currentPelvisOffset = Mathf.Lerp(_currentPelvisOffset, targetPelvisDelta, Time.deltaTime * pelvisCorrectionSpeed);

            if (Mathf.Abs(_currentPelvisOffset) > 0.001f)
            {
                _animator.bodyPosition += Vector3.up * _currentPelvisOffset;
            }
        }

        // 2. 왼발/오른발 애니메이션 커브 가중치 조회 (회전 및 이동 스윙 제어)
        float leftCurveWeight = 1f;
        float rightCurveWeight = 1f;

        if (useAnimationCurves && _animator != null)
        {
            try
            {
                leftCurveWeight = _animator.GetFloat(_leftCurveHash);
                rightCurveWeight = _animator.GetFloat(_rightCurveHash);
            }
            catch
            {
                leftCurveWeight = 1f;
                rightCurveWeight = 1f;
            }
        }

        // 3. 왼발/오른발 독립 레이캐스트 및 Y축 오프셋/회전 계산
        CalculateFootIK(
            AvatarIKGoal.LeftFoot,
            leftCurveWeight,
            safeStartY,
            ref _leftFootOffsetY,
            ref _leftFootRotation,
            ref _leftFootWeight,
            ref _leftFootPosition,
            ref _debugLeftRayStart,
            ref _debugLeftHitPoint,
            dynamicSpeed
        );

        CalculateFootIK(
            AvatarIKGoal.RightFoot,
            rightCurveWeight,
            safeStartY,
            ref _rightFootOffsetY,
            ref _rightFootRotation,
            ref _rightFootWeight,
            ref _rightFootPosition,
            ref _debugRightRayStart,
            ref _debugRightHitPoint,
            dynamicSpeed
        );

        // 4. IK 위치 및 회전 최종 적용
        ApplyIKGoal(AvatarIKGoal.LeftFoot, _leftFootPosition, _leftFootRotation, _leftFootWeight);
        ApplyIKGoal(AvatarIKGoal.RightFoot, _rightFootPosition, _rightFootRotation, _rightFootWeight);
    }

    private void CalculateFootIK(
        AvatarIKGoal footGoal,
        float curveWeight,
        float safeStartY,
        ref float currentOffsetY,
        ref Quaternion currentRot,
        ref float currentWeight,
        ref Vector3 finalFootPos,
        ref Vector3 debugRayStart,
        ref Vector3 debugHitPoint,
        float dynamicSpeed)
    {
        Vector3 animFootPos = _animator.GetIKPosition(footGoal);
        Quaternion animFootRot = _animator.GetIKRotation(footGoal);

        // 이동 중일 때 이동 방향 전방 예측 오프셋 계산
        Vector3 moveLead = Vector3.zero;
        if (PlayerMove != null)
        {
            CharacterController cc = PlayerMove.GetComponent<CharacterController>();
            Vector3 vel = cc != null ? cc.velocity : Vector3.zero;
            vel.y = 0f;
            if (vel.sqrMagnitude > 0.01f)
            {
                moveLead = vel.normalized * forwardPredictionDistance;
            }
        }

        // 양발 위치 기준 + 발바닥 중앙 오프셋 + 이동 전방 예측 반영
        Vector3 footCenter = animFootPos + (transform.forward * footForwardOffset) + moveLead;

        // 앉거나 서 있거나 항상 지면 상단 안전 지대에서 수직 하향 발사
        Vector3 rayStart = new Vector3(footCenter.x, safeStartY, footCenter.z);
        debugRayStart = rayStart;

        float targetOffsetY = 0f;
        Quaternion targetRot = animFootRot;
        float targetWeight = 0f;
        bool hitSuccess = false;

        // 플레이어 자신의 콜라이더를 제외하고 가장 가까운 지면 충돌체 탐색
        int hitCount = Physics.RaycastNonAlloc(rayStart, Vector3.down, _raycastHits, raycastLength, groundLayers, QueryTriggerInteraction.Ignore);
        float closestDist = float.MaxValue;
        RaycastHit bestHit = default;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _raycastHits[i];
            if (hit.collider == null) continue;

            // 플레이어 자신의 콜라이더 무시
            if (Player != null && (hit.collider.transform.IsChildOf(Player.transform) || hit.collider.transform == Player.transform))
            {
                continue;
            }

            if (hit.distance < closestDist)
            {
                closestDist = hit.distance;
                bestHit = hit;
                hitSuccess = true;
            }
        }

        if (hitSuccess)
        {
            debugHitPoint = bestHit.point;
            float groundY = bestHit.point.y + footBottomOffset;

            // 접지 시 필요한 순수 Y축 보정 오프셋
            float rawOffsetY = groundY - animFootPos.y;

            // 애니메이션 커브 가중치 반영 (회전 및 스윙 시 발이 자연스럽게 떨어짐)
            targetOffsetY = Mathf.Lerp(0f, rawOffsetY, curveWeight);
            targetWeight = curveWeight;

            if (enableFootRotation)
            {
                float slopeAngle = Vector3.Angle(Vector3.up, bestHit.normal);
                if (slopeAngle <= maxSlopeAngle)
                {
                    Quaternion surfaceRotation = Quaternion.FromToRotation(Vector3.up, bestHit.normal);
                    targetRot = surfaceRotation * animFootRot;
                }
            }
        }
        else
        {
            debugHitPoint = rayStart + Vector3.down * raycastLength;
            targetWeight = 0f;
            targetOffsetY = 0f;
        }

        // Y축 높이 오프셋, 회전, 가중치만 기민하게 보간
        currentWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * dynamicSpeed);
        currentOffsetY = Mathf.Lerp(currentOffsetY, targetOffsetY, Time.deltaTime * dynamicSpeed);
        currentRot = Quaternion.Slerp(currentRot == Quaternion.identity ? animFootRot : currentRot, targetRot, Time.deltaTime * dynamicSpeed);

        // X, Z는 애니메이션 원본을 100% 즉시 추종하고 Y축만 오프셋을 적용 (수평 지연/처짐 100% 제거)
        finalFootPos = new Vector3(animFootPos.x, animFootPos.y + currentOffsetY, animFootPos.z);
    }

    private void ApplyIKGoal(AvatarIKGoal footGoal, Vector3 position, Quaternion rotation, float weight)
    {
        _animator.SetIKPositionWeight(footGoal, weight);
        _animator.SetIKPosition(footGoal, position);

        _animator.SetIKRotationWeight(footGoal, weight);
        _animator.SetIKRotation(footGoal, rotation);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_animator == null || !_animator.isHuman) return;

        // 중앙 레이캐스트 라인 (몸체 높이 제어용 - 청록색)
        if (enablePelvisCorrection)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(_debugCenterRayStart, _debugCenterHitPoint);
            Gizmos.DrawWireSphere(_debugCenterHitPoint, 0.04f);
        }

        // 양발 레이캐스트 라인 (노란색)
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(_debugLeftRayStart, _debugLeftHitPoint);
        Gizmos.DrawLine(_debugRightRayStart, _debugRightHitPoint);

        // 발 타겟 위치 (초록색)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_leftFootPosition, 0.06f);
        Gizmos.DrawWireSphere(_rightFootPosition, 0.06f);
    }
#endif
}
