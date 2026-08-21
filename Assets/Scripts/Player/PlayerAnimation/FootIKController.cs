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
    [Tooltip("캐릭터 루트 기준 레이캐스트 발사 시작 높이 (단위: m, 기본 0.6m)")]
    public float raycastStartHeight = 0.6f;

    [Tooltip("레이캐스트 하향 탐색 총 길이 (단위: m, 기본 1.4m)")]
    public float raycastLength = 1.4f;

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

    [Header("Smoothing & Pelvis Settings (Responsive)")]
    [Tooltip("발 위치 및 회전 보간 속도 (민첩하고 즉각적인 반응: 18~20)")]
    public float footPlacementSpeed = 18f;

    [Tooltip("골반(Pelvis) 높이 보간 속도 (자연스러운 체중 이동: 12~15)")]
    public float pelvisCorrectionSpeed = 14f;

    [Tooltip("골반 최대 하향 보정 거리 (단위: m)")]
    public float maxPelvisDrop = 0.7f;

    [Tooltip("골반 최대 상향 보정 거리 (단위: m)")]
    public float maxPelvisRise = 0.3f;

    private Animator _animator;
    private int _leftCurveHash;
    private int _rightCurveHash;

    private Vector3 _leftFootPosition;
    private Vector3 _rightFootPosition;
    private Quaternion _leftFootRotation = Quaternion.identity;
    private Quaternion _rightFootRotation = Quaternion.identity;

    private float _leftFootWeight = 0f;
    private float _rightFootWeight = 0f;

    private float _currentPelvisOffset = 0f;
    private bool _isInitialized = false;

    // 디버그 기즈모용
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

    private void OnAnimatorIK(int layerIndex)
    {
        // Base Layer(0번 레이어)에서만 IK 연산 수행
        if (layerIndex != 0 || !enableFootIK) return;

        if (!_isInitialized)
        {
            Initialize();
            if (!_isInitialized) return;
        }

        // 지면에 닿아 있는지 확인 (PlayerMove.isGrounded)
        bool isGrounded = PlayerMove != null ? PlayerMove.isGrounded : true;

        // 공중에 떠 있거나 점프 중일 때는 골반 오프셋과 IK 가중치를 0으로 부드럽게 복귀
        if (!isGrounded)
        {
            _currentPelvisOffset = Mathf.Lerp(_currentPelvisOffset, 0f, Time.deltaTime * pelvisCorrectionSpeed);
            _leftFootWeight = Mathf.Lerp(_leftFootWeight, 0f, Time.deltaTime * footPlacementSpeed);
            _rightFootWeight = Mathf.Lerp(_rightFootWeight, 0f, Time.deltaTime * footPlacementSpeed);

            if (Mathf.Abs(_currentPelvisOffset) > 0.001f)
            {
                _animator.bodyPosition += Vector3.up * _currentPelvisOffset;
            }

            ApplyIKGoal(AvatarIKGoal.LeftFoot, _leftFootPosition, _leftFootRotation, _leftFootWeight);
            ApplyIKGoal(AvatarIKGoal.RightFoot, _rightFootPosition, _rightFootRotation, _rightFootWeight);
            return;
        }

        // 1. 왼발/오른발 애니메이션 커브 가중치 조회 (AAA 표준)
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

        // 2. 왼발/오른발 독립 레이캐스트 및 위치/회전 계산
        float leftGroundDelta = 0f;
        float rightGroundDelta = 0f;
        bool leftHit = false;
        bool rightHit = false;

        CalculateFootIK(
            AvatarIKGoal.LeftFoot,
            leftCurveWeight,
            ref _leftFootPosition,
            ref _leftFootRotation,
            ref _leftFootWeight,
            ref _debugLeftRayStart,
            ref _debugLeftHitPoint,
            out leftGroundDelta,
            out leftHit
        );

        CalculateFootIK(
            AvatarIKGoal.RightFoot,
            rightCurveWeight,
            ref _rightFootPosition,
            ref _rightFootRotation,
            ref _rightFootWeight,
            ref _debugRightRayStart,
            ref _debugRightHitPoint,
            out rightGroundDelta,
            out rightHit
        );

        // 3. 골반(Hips / Pelvis / Body) 높이 보정 (지형 단차에만 반응)
        if (leftHit || rightHit)
        {
            float targetPelvisDelta = 0f;
            if (leftHit && rightHit)
            {
                targetPelvisDelta = Mathf.Min(leftGroundDelta, rightGroundDelta);
            }
            else if (leftHit)
            {
                targetPelvisDelta = leftGroundDelta;
            }
            else
            {
                targetPelvisDelta = rightGroundDelta;
            }

            targetPelvisDelta = Mathf.Clamp(targetPelvisDelta, -maxPelvisDrop, maxPelvisRise);
            _currentPelvisOffset = Mathf.Lerp(_currentPelvisOffset, targetPelvisDelta, Time.deltaTime * pelvisCorrectionSpeed);
        }
        else
        {
            _currentPelvisOffset = Mathf.Lerp(_currentPelvisOffset, 0f, Time.deltaTime * pelvisCorrectionSpeed);
        }

        if (Mathf.Abs(_currentPelvisOffset) > 0.001f)
        {
            _animator.bodyPosition += Vector3.up * _currentPelvisOffset;
        }

        // 4. IK 위치 및 회전 최종 적용
        ApplyIKGoal(AvatarIKGoal.LeftFoot, _leftFootPosition, _leftFootRotation, _leftFootWeight);
        ApplyIKGoal(AvatarIKGoal.RightFoot, _rightFootPosition, _rightFootRotation, _rightFootWeight);
    }

    private void CalculateFootIK(
        AvatarIKGoal footGoal,
        float curveWeight,
        ref Vector3 currentPos,
        ref Quaternion currentRot,
        ref float currentWeight,
        ref Vector3 debugRayStart,
        ref Vector3 debugHitPoint,
        out float groundDeltaFromRoot,
        out bool hitSuccess)
    {
        Vector3 animFootPos = _animator.GetIKPosition(footGoal);
        Quaternion animFootRot = _animator.GetIKRotation(footGoal);

        // 양발 위치 기준 + 발바닥 중앙 오프셋 반영
        Vector3 footCenter = animFootPos + (transform.forward * footForwardOffset);

        // 캐릭터 루트 높이 기준 상단 안전 지대에서 수직 하향 발사
        float rootY = Player != null ? Player.transform.position.y : transform.position.y;
        Vector3 rayStart = new Vector3(footCenter.x, rootY + raycastStartHeight, footCenter.z);
        debugRayStart = rayStart;

        Vector3 targetPos = animFootPos;
        Quaternion targetRot = animFootRot;
        groundDeltaFromRoot = 0f;
        hitSuccess = false;
        float targetWeight = 0f;

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

            // 캐릭터 루트 바닥(rootY) 대비 순수 지형 단차 계산
            groundDeltaFromRoot = bestHit.point.y - rootY;

            // AAA 커브 가중치 반영: 커브가 0에 가까우면(스윙 중) 발 원래 높이 유지, 1에 가까우면(접지 시) 지면 밀착
            targetPos = Vector3.Lerp(animFootPos, new Vector3(animFootPos.x, groundY, animFootPos.z), curveWeight);

            // 발이 지면보다 위로 들려 있는 거리 검사 (절차적 보조)
            float footLiftDistance = animFootPos.y - groundY;
            if (footLiftDistance > 0.05f)
            {
                float proceduralWeight = Mathf.Clamp01(1f - (footLiftDistance / 0.2f));
                targetWeight = Mathf.Min(curveWeight, proceduralWeight);
            }
            else
            {
                targetWeight = curveWeight;
            }

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
        }

        // 민첩하고 즉각적인 반응 보간
        currentWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * footPlacementSpeed);
        currentPos = Vector3.Lerp(currentPos == Vector3.zero ? animFootPos : currentPos, targetPos, Time.deltaTime * footPlacementSpeed);
        currentRot = Quaternion.Slerp(currentRot == Quaternion.identity ? animFootRot : currentRot, targetRot, Time.deltaTime * footPlacementSpeed);
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

        // 레이캐스트 라인
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(_debugLeftRayStart, _debugLeftHitPoint);
        Gizmos.DrawLine(_debugRightRayStart, _debugRightHitPoint);

        // 발 타겟 위치
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_leftFootPosition, 0.06f);
        Gizmos.DrawWireSphere(_rightFootPosition, 0.06f);
    }
#endif
}
