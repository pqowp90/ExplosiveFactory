using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 1인칭 시점 전용 다리(First-Person Legs) 제어 컴포넌트.
/// 팔과 머리 본을 (0,0,0)으로 축소하여 시야 가림을 없애고,
/// 허리/상체를 카메라 뒤쪽으로 살짝 이동하여 고개 숙였을 때의 가슴 뚫림을 원천 차단합니다.
/// </summary>
public class FirstPersonLegsController : MonoBehaviour
{
    [Header("Offsets & Tuning")]
    [Tooltip("상체(허리)를 카메라 뒤쪽으로 밀어주는 거리 (단위: m, 기본 0.18m)")]
    public float backwardOffset = 0.18f;

    [Tooltip("1인칭 다리 전체의 추가 위치 오프셋")]
    public Vector3 legsPositionOffset = Vector3.zero;

    private Animator _animator;
    private Player _player;

    private Transform _headTransform;
    private Transform _neckTransform;
    private Transform _leftShoulder;
    private Transform _rightShoulder;
    private Transform _leftUpperArm;
    private Transform _rightUpperArm;
    private Transform _spineTransform;

    private void Awake()
    {
        _player = GetComponentInParent<Player>();
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (_animator != null && _animator.isHuman)
        {
            _headTransform = _animator.GetBoneTransform(HumanBodyBones.Head);
            _neckTransform = _animator.GetBoneTransform(HumanBodyBones.Neck);
            _leftShoulder = _animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            _rightShoulder = _animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            _leftUpperArm = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            _rightUpperArm = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            _spineTransform = _animator.GetBoneTransform(HumanBodyBones.Spine);
        }
    }

    private void LateUpdate()
    {
        // 1. 팔(Arms)과 머리(Head) 본을 Scale (0,0,0)으로 축소하여 1인칭 시야 간섭 원천 제거
        if (_headTransform != null) _headTransform.localScale = Vector3.zero;
        if (_neckTransform != null) _neckTransform.localScale = Vector3.zero;
        if (_leftShoulder != null) _leftShoulder.localScale = Vector3.zero;
        if (_rightShoulder != null) _rightShoulder.localScale = Vector3.zero;
        if (_leftUpperArm != null) _leftUpperArm.localScale = Vector3.zero;
        if (_rightUpperArm != null) _rightUpperArm.localScale = Vector3.zero;

        // 2. 허리/상체(Spine)를 로컬 뒤쪽(-Z)으로 살짝 밀어서 고개를 푹 숙여도 가슴/목이 카메라를 뚫지 않게 처리
        if (_spineTransform != null && backwardOffset > 0.001f)
        {
            _spineTransform.localPosition -= new Vector3(0f, 0f, backwardOffset);
        }

        // 3. 3인칭 몸체와 회전 동기화
        if (_player != null && _player.PlayerBodyTransform != null)
        {
            transform.rotation = _player.PlayerBodyTransform.rotation;
        }
    }
}
