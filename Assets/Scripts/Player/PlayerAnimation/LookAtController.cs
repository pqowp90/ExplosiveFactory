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
    }

    private void OnAnimatorIK(int layerIndex)
    {
        // 0번(Base Layer)에서만 IK를 수행하여 레이어 중복 연산 방지
        if (layerIndex != 0) return;

        bool isHolding = _player != null && _player.PlayerAnimation != null && _player.PlayerAnimation.IsHoldingItem;

        if (_networkAnimator != null)
        {
            if (isHolding && objectToLookAt != null && headWeight > 0f)
            {
                _networkAnimator.SetLookAtPosition(objectToLookAt.position);
                _networkAnimator.SetLookAtWeight(1f, 0f, headWeight, 0f, 0.5f);
            }
            else
            {
                _networkAnimator.SetLookAtWeight(0f, 0f, 0f, 0f, 0f);
            }
        }
    }

    private void LateUpdate()
    {
        // 빈손 상태일 때는 상체 월드 고정을 건너뛰어 자연스러운 전신 달리기/걷기 애니메이션 유지
        bool isHolding = _player != null && _player.PlayerAnimation != null && _player.PlayerAnimation.IsHoldingItem;
        if (!isHolding) return;

        // 1. 플레이어 몸체(루트)의 수평 기준 각도 (Yaw)
        float bodyYaw = _player != null ? _player.transform.eulerAngles.y : transform.parent != null ? transform.parent.eulerAngles.y : transform.eulerAngles.y;

        // 2. 시선 타겟을 향한 상하 각도 (Pitch) 100% 계산
        float fullPitch = 0f;
        if (objectToLookAt != null)
        {
            Vector3 lookDir = objectToLookAt.position - transform.position;
            float horizontalDist = new Vector2(lookDir.x, lookDir.z).magnitude;
            if (horizontalDist > 0.001f)
            {
                // 위를 보면 음수각(-), 아래를 보면 양수각(+)
                fullPitch = -Mathf.Atan2(lookDir.y, horizontalDist) * Mathf.Rad2Deg;
                fullPitch = Mathf.Clamp(fullPitch, -maxPitchAngle, maxPitchAngle);
            }
        }

        // 3. 1단계: 허리(Spine)에 50% 회전 적용 (골반 롤링 차단 + 하부 척추 굽힘)
        if (_spineTransform != null)
        {
            float spinePitch = fullPitch * spinePitchRatio;
            Quaternion targetSpineRotation = Quaternion.Euler(spinePitch, bodyYaw, 0f);

            if (spineWorldStabilizeWeight >= 0.99f)
            {
                _spineTransform.rotation = targetSpineRotation;
            }
            else
            {
                _spineTransform.rotation = Quaternion.Slerp(_spineTransform.rotation, targetSpineRotation, spineWorldStabilizeWeight);
            }
        }

        // 4. 2단계: 가슴(Chest)에 나머지 50% 추가 적용 (총 100% 시선 각도 도달, 양팔/목/머리 최종 일치)
        if (_chestTransform != null)
        {
            float chestPitch = fullPitch * (spinePitchRatio + chestPitchRatio);
            Quaternion targetChestRotation = Quaternion.Euler(chestPitch, bodyYaw, 0f);

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
