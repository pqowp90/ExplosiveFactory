using System.Collections;
using System.Collections.Generic;

using Mirror;

using UnityEngine;

public class LookAtController : PlayerComponent
{
    public Transform objectToLookAt;
    [Tooltip("머리 LookAt IK 회전 가중치 (0 = 애니메이션의 목/머리 모션 100% 반영)")]
    public float headWeight = 0f;

    [Header("Pitch Distribution")]
    [Tooltip("허리(Spine) 상하 회전 분배 비율 (기본 0.5 = 50%)")]
    [Range(0f, 1f)]
    public float spinePitchRatio = 0.5f;

    [Tooltip("가슴(Chest) 상하 회전 분배 비율 (기본 0.5 = 50%)")]
    [Range(0f, 1f)]
    public float chestPitchRatio = 0.5f;

    [Tooltip("상하 최대 회전 각도 제한")]
    public float maxPitchAngle = 80f;

    [Tooltip("월드 정면 상체 안정화 가중치 (1.0 = 골반 흔들림 100% 무시하고 월드 정면 고정)")]
    [Range(0f, 1f)]
    public float spineWorldStabilizeWeight = 1.0f;

    [Header("Target Model Transform")]
    [Tooltip("3인칭 바디 모델 트랜스폼 (미지정 시 Player.PlayerBodyTransform 또는 transform 자동 사용)")]
    public Transform targetBodyTransform;

    private Transform TargetTransform => targetBodyTransform != null ? targetBodyTransform : (PlayerBodyTransform != null ? PlayerBodyTransform : transform);
    private Transform RootTransform => Player != null ? Player.transform : (transform.parent != null ? transform.parent : transform);

    private CustomNetworkAnimator _networkAnimator;
    private Animator _animator;
    private Transform _spineTransform;
    private Transform _chestTransform;
    private Transform _neckTransform;
    private Transform _headTransform;

    private Quaternion _spineBindOffset = Quaternion.identity;
    private Quaternion _chestBindOffset = Quaternion.identity;

    private float _curRotation = 0f;
    private float _realRotation = 0f;

    private void Start()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        _networkAnimator = GetComponent<CustomNetworkAnimator>() ?? GetComponentInParent<CustomNetworkAnimator>();
        if (_networkAnimator != null) _animator = _networkAnimator.Animator;
        if (_animator == null) _animator = TargetTransform.GetComponentInChildren<Animator>();

        BindBones();
        _curRotation = RootTransform.eulerAngles.y;
    }

    /// <summary>
    /// 플레이어 모델링 교체 시 타겟 트랜스폼 및 본 레퍼런스를 즉시 재바인딩합니다.
    /// </summary>
    public void RebindModel(Transform newBodyTransform, Animator newAnimator = null)
    {
        targetBodyTransform = newBodyTransform;
        _animator = newAnimator != null ? newAnimator : TargetTransform.GetComponentInChildren<Animator>();
        BindBones();
    }

    private void BindBones()
    {
        if (_animator != null && _animator.isHuman)
        {
            _spineTransform = _animator.GetBoneTransform(HumanBodyBones.Spine);
            _chestTransform = _animator.GetBoneTransform(HumanBodyBones.Chest);
            _neckTransform = _animator.GetBoneTransform(HumanBodyBones.Neck);
            _headTransform = _animator.GetBoneTransform(HumanBodyBones.Head);

            if (_spineTransform != null)
            {
                _spineBindOffset = Quaternion.Inverse(TargetTransform.rotation) * _spineTransform.rotation;
            }
            if (_chestTransform != null)
            {
                _chestBindOffset = Quaternion.Inverse(TargetTransform.rotation) * _chestTransform.rotation;
            }
        }
    }

    private void Update()
    {
        float rootYaw = RootTransform.eulerAngles.y;
        if (PlayerAnimation != null && !PlayerAnimation.IsMoving)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(_realRotation, rootYaw)) > 60f)
            {
                _curRotation = rootYaw;
            }
            var deltaAngle = Mathf.DeltaAngle(_realRotation, _curRotation);
            if (Mathf.Abs(deltaAngle) < 3f)
            {
                PlayerAnimation.SetTurn(0);
                _realRotation += deltaAngle * Time.deltaTime * 14f;
            }
            else
            {
                PlayerAnimation.SetTurn(deltaAngle < 0 ? -1 : 1);
                _realRotation += deltaAngle * Time.deltaTime * 7f;
            }
        }
        else
        {
            _curRotation = rootYaw;
            var deltaAngle = Mathf.DeltaAngle(_realRotation, _curRotation);
            _realRotation += deltaAngle * Time.deltaTime * 10f;
        }

        TargetTransform.eulerAngles = new Vector3(0, _realRotation, 0);

        // 1인칭 로컬 플레이어일 때만 3인칭 몸체 모델(그림자)을 회전축 뒤로 오프셋
        float bodyBack = (IsOwned && LegsSetup != null && LegsSetup.Settings != null)
            ? LegsSetup.Settings.firstPersonBodyBackwardOffset
            : 0f;
        TargetTransform.localPosition = Quaternion.Euler(0, _realRotation - rootYaw, 0) * new Vector3(0, 0, -bodyBack);
    }

    /// <summary>
    /// AnimatorIKForwarder 등 외부 프록시에서 중계 호출하는 IK 콜백.
    /// </summary>
    public void OnForwardedAnimatorIK(int layerIndex)
    {
        ProcessAnimatorIK(layerIndex);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        ProcessAnimatorIK(layerIndex);
    }

    private void ProcessAnimatorIK(int layerIndex)
    {
        // 0번(Base Layer)에서만 IK를 수행하여 레이어 중복 연산 방지
        if (layerIndex != 0) return;

        bool isHolding = PlayerAnimation != null && PlayerAnimation.IsHoldingItem;

        if (_networkAnimator == null)
        {
            _networkAnimator = GetComponent<CustomNetworkAnimator>() ?? GetComponentInParent<CustomNetworkAnimator>();
        }

        if (_networkAnimator != null && objectToLookAt != null)
        {
            if (isHolding)
            {
                if (headWeight > 0f)
                {
                    _networkAnimator.SetLookAtPosition(objectToLookAt.position);
                    _networkAnimator.SetLookAtWeight(1f, 0f, headWeight, 0f, 0.5f);
                }
                else
                {
                    _networkAnimator.SetLookAtWeight(0f, 0f, 0f, 0f, 0f);
                }
            }
            else
            {
                // 빈손 상태: 유니티 LookAt IK에 자연스러운 전신 시선 제어 위임 (몸통 15%, 머리/목 85%)
                _networkAnimator.SetLookAtPosition(objectToLookAt.position);
                _networkAnimator.SetLookAtWeight(1f, 0.15f, 0.85f, 0.5f, 0.5f);
            }
        }
    }

    private void LateUpdate()
    {
        // 1. 카메라 시선 Yaw와 몸체(다리) 실제 Yaw 계산
        float cameraYaw = Player != null ? Player.transform.eulerAngles.y : transform.eulerAngles.y;
        if (Camera != null)
        {
            cameraYaw = Camera.transform.eulerAngles.y;
        }
        float bodyYaw = _realRotation;
        float deltaYaw = Mathf.DeltaAngle(bodyYaw, cameraYaw);

        // 2. 시선 타겟 / 카메라를 향한 상하 각도 (Pitch) 100% 선형 계산
        float fullPitch = 0f;
        if (Camera != null)
        {
            float camPitch = Camera.transform.eulerAngles.x;
            if (camPitch > 180f) camPitch -= 360f;
            fullPitch = Mathf.Clamp(camPitch, -maxPitchAngle, maxPitchAngle);
        }
        else if (objectToLookAt != null)
        {
            float targetPitch = objectToLookAt.eulerAngles.x;
            if (targetPitch > 180f) targetPitch -= 360f;
            if (Mathf.Abs(targetPitch) > 0.01f)
            {
                fullPitch = Mathf.Clamp(targetPitch, -maxPitchAngle, maxPitchAngle);
            }
            else
            {
                // 가슴/눈 높이(Y: ~1.4m) 기준으로 상대 벡터 계산하여 발바닥 Atan2 왜곡 방지
                Vector3 origin = _chestTransform != null ? _chestTransform.position : TargetTransform.position + Vector3.up * 1.4f;
                Vector3 lookDir = objectToLookAt.position - origin;
                float horizontalDist = new Vector2(lookDir.x, lookDir.z).magnitude;
                if (horizontalDist > 0.001f)
                {
                    fullPitch = -Mathf.Atan2(lookDir.y, horizontalDist) * Mathf.Rad2Deg;
                    fullPitch = Mathf.Clamp(fullPitch, -maxPitchAngle, maxPitchAngle);
                }
            }
        }

        bool isHolding = PlayerAnimation != null && PlayerAnimation.IsHoldingItem;

        if (isHolding)
        {
            // [아이템 파지 상태] ➔ 모델 고유 바인드 포즈 오프셋을 결합한 월드 절대 안정화 (달릴 때 상체 흔들림 완벽 제거 + 본 뒤틀림 원천 차단)
            // 1단계: 허리(Spine)에 좌우 50% + 상하 50% 절대 회전 적용
            if (_spineTransform != null)
            {
                float spinePitch = fullPitch * spinePitchRatio;
                float spineYaw = bodyYaw + deltaYaw * 0.5f;
                Quaternion targetSpineRotation = Quaternion.Euler(0f, spineYaw, 0f) * Quaternion.Euler(spinePitch, 0f, 0f) * _spineBindOffset;

                if (spineWorldStabilizeWeight >= 0.99f)
                {
                    _spineTransform.rotation = targetSpineRotation;
                }
                else
                {
                    _spineTransform.rotation = Quaternion.Slerp(_spineTransform.rotation, targetSpineRotation, spineWorldStabilizeWeight);
                }
            }

            // 2단계: 가슴(Chest)에 좌우 100% + 상하 100% 절대 회전 적용 (시선 완벽 일치 및 월드 고정)
            if (_chestTransform != null)
            {
                float chestPitch = fullPitch * (spinePitchRatio + chestPitchRatio);
                float chestYaw = bodyYaw + deltaYaw;
                Quaternion targetChestRotation = Quaternion.Euler(0f, chestYaw, 0f) * Quaternion.Euler(chestPitch, 0f, 0f) * _chestBindOffset;

                if (spineWorldStabilizeWeight >= 0.99f)
                {
                    _chestTransform.rotation = targetChestRotation;
                }
                else
                {
                    _chestTransform.rotation = Quaternion.Slerp(_chestTransform.rotation, targetChestRotation, spineWorldStabilizeWeight);
                }
            }
        }
    }
}
