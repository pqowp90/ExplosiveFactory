using System.Collections;
using System.Collections.Generic;

using Mirror;

using UnityEngine;

public class LookAtController : MonoBehaviour
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

    private CustomNetworkAnimator _networkAnimator;
    private Animator _animator;
    private Player _player;
    private Transform _spineTransform;
    private Transform _chestTransform;
    private Transform _neckTransform;
    private Transform _headTransform;

    private float _curRotation = 0f;
    private float _realRotation = 0f;

    private void Start()
    {
        _player = GetComponentInParent<Player>();
        _networkAnimator = GetComponent<CustomNetworkAnimator>();
        if (_networkAnimator != null) _animator = _networkAnimator.Animator;
        if (_animator == null) _animator = GetComponent<Animator>();

        if (_animator != null && _animator.isHuman)
        {
            _spineTransform = _animator.GetBoneTransform(HumanBodyBones.Spine);
            _chestTransform = _animator.GetBoneTransform(HumanBodyBones.Chest);
            _neckTransform = _animator.GetBoneTransform(HumanBodyBones.Neck);
            _headTransform = _animator.GetBoneTransform(HumanBodyBones.Head);
        }

        _curRotation = transform.parent.eulerAngles.y;
    }

    private void Update()
    {
        if (!_player.PlayerAnimation.IsMoving)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(_realRotation, transform.parent.eulerAngles.y)) > 60f)
            {
                _curRotation = transform.parent.eulerAngles.y;
            }
            var deltaAngle = Mathf.DeltaAngle(_realRotation, _curRotation);
            if (Mathf.Abs(deltaAngle) < 3f)
            {
                _player.PlayerAnimation.SetTurn(0);
                _realRotation += deltaAngle * Time.deltaTime * 14f;
            }
            else
            {
                _player.PlayerAnimation.SetTurn(deltaAngle < 0 ? -1 : 1);
                _realRotation += deltaAngle * Time.deltaTime * 7f;
            }
        }
        else
        {
            _curRotation = transform.parent.eulerAngles.y;
            var deltaAngle = Mathf.DeltaAngle(_realRotation, _curRotation);
            _realRotation += deltaAngle * Time.deltaTime * 10f;
        }
        transform.eulerAngles = new Vector3(0, _realRotation, 0);

        // 1인칭 로컬 플레이어일 때만 3인칭 몸체 모델(그림자)을 회전축 뒤로 오프셋
        float bodyBack = (_player != null && _player.isOwned && _player.FirstPersonLegsSettings != null)
            ? _player.FirstPersonLegsSettings.firstPersonBodyBackwardOffset
            : 0f;
        transform.localPosition = Quaternion.Euler(0, _realRotation - transform.parent.eulerAngles.y, 0) * new Vector3(0, 0, -bodyBack);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        // 0번(Base Layer)에서만 IK를 수행하여 레이어 중복 연산 방지
        if (layerIndex != 0) return;

        bool isHolding = _player != null && _player.PlayerAnimation != null && _player.PlayerAnimation.IsHoldingItem;

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
        float cameraYaw = _player != null ? _player.transform.eulerAngles.y : transform.eulerAngles.y;
        if (_player != null && _player.Camera != null)
        {
            cameraYaw = _player.Camera.transform.eulerAngles.y;
        }
        float bodyYaw = _realRotation;
        float deltaYaw = Mathf.DeltaAngle(bodyYaw, cameraYaw);

        // 2. 시선 타겟 / 카메라를 향한 상하 각도 (Pitch) 100% 선형 계산
        float fullPitch = 0f;
        if (_player != null && _player.Camera != null)
        {
            float camPitch = _player.Camera.transform.eulerAngles.x;
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
                Vector3 origin = _chestTransform != null ? _chestTransform.position : transform.position + Vector3.up * 1.4f;
                Vector3 lookDir = objectToLookAt.position - origin;
                float horizontalDist = new Vector2(lookDir.x, lookDir.z).magnitude;
                if (horizontalDist > 0.001f)
                {
                    fullPitch = -Mathf.Atan2(lookDir.y, horizontalDist) * Mathf.Rad2Deg;
                    fullPitch = Mathf.Clamp(fullPitch, -maxPitchAngle, maxPitchAngle);
                }
            }
        }

        bool isHolding = _player != null && _player.PlayerAnimation != null && _player.PlayerAnimation.IsHoldingItem;

        if (isHolding)
        {
            // [아이템 파지 상태] ➔ 다리 고정/턴 각도와 완벽 결합된 상체 50/50 조준
            // 1단계: 허리(Spine)에 좌우 50% + 상하 50% 회전 적용
            if (_spineTransform != null)
            {
                float spinePitch = fullPitch * spinePitchRatio;
                float spineYaw = bodyYaw + deltaYaw * 0.5f;
                Quaternion targetSpineRotation = Quaternion.Euler(0f, spineYaw, 0f) * Quaternion.Euler(spinePitch, 0f, 0f);

                if (spineWorldStabilizeWeight >= 0.99f)
                {
                    _spineTransform.rotation = targetSpineRotation;
                }
                else
                {
                    _spineTransform.rotation = Quaternion.Slerp(_spineTransform.rotation, targetSpineRotation, spineWorldStabilizeWeight);
                }
            }

            // 2단계: 가슴(Chest)에 좌우 100% + 상하 100% 회전 적용 (시선 완벽 일치)
            if (_chestTransform != null)
            {
                float chestPitch = fullPitch * (spinePitchRatio + chestPitchRatio);
                float chestYaw = bodyYaw + deltaYaw;
                Quaternion targetChestRotation = Quaternion.Euler(0f, chestYaw, 0f) * Quaternion.Euler(chestPitch, 0f, 0f);

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
