using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 1인칭 시점 전용 다리(First-Person Legs) 제어 컴포넌트.
/// - 팔과 머리, 목, 어깨, 손 본 전체를 (0,0,0)으로 축소하여 1인칭 시야 간섭을 원천 차단합니다.
/// - 상체(Spine, Chest) 및 몸체를 카메라 뒤쪽(-Z)으로 밀어내어 꼿꼿한 형태를 방지하고,
///   고개를 아래로 숙였을 때 가슴/등이 카메라를 뚫지 않으면서 다리와 발끝이 자연스럽게 시야 하단에 들어오도록 합니다.
/// </summary>
public class FirstPersonLegsController : PlayerComponent
{
    [Header("Slanted Torso Offsets (상체 비스듬히 뒤로 빼기 - 회전 없음)")]
    [Tooltip("허리 하단(Spine)을 카메라 시선 뒤쪽으로 밀어주는 거리 (단위: m, 기본 0.15m)")]
    public float spineBackwardOffset = 0.15f;

    [Tooltip("허리(Spine)를 위쪽(+Y)으로 이동시키는 거리 (단위: m, 기본 0.27m)")]
    public float spineUpwardOffset = 0.27f;

    [Tooltip("가슴 중간(Chest)을 추가로 뒤로 밀어주는 거리 (단위: m, 기본 0.15m)")]
    public float chestBackwardOffset = 0.15f;

    [Tooltip("가슴(Chest)을 위쪽(+Y)으로 이동시키는 거리 (단위: m, 기본 0.15m)")]
    public float chestUpwardOffset = 0.15f;

    [Tooltip("가슴 상단(UpperChest)을 추가로 뒤로 밀어주는 거리 (단위: m, 기본 -0.16m)")]
    public float upperChestBackwardOffset = -0.16f;

    private Animator _animator;
    private Transform _cameraTransform;
    private Transform _thirdPersonModelTransform;

    private Transform _spineTransform;
    private Transform _chestTransform;
    private Transform _upperChestTransform;
    private Transform _hipsTransform;

    private readonly List<Transform> _hiddenBones = new List<Transform>();
    private bool _isInitialized = false;
    private float _crouchRatio = 0f;

    [Header("Settings Data")]
    public FirstPersonLegsSettings settings;

    private FirstPersonLegsSettings Settings => settings ?? (LegsSetup != null ? LegsSetup.Settings : null);

    public float GetEffectiveSpineBackwardOffset(float crouchRatio)
    {
        if (Settings == null) return spineBackwardOffset;
        return Mathf.Lerp(Settings.spineBackwardOffset, Settings.crouchSpineBackwardOffset, crouchRatio);
    }

    public float GetEffectiveSpineUpwardOffset(float crouchRatio)
    {
        if (Settings == null) return spineUpwardOffset;
        return Mathf.Lerp(Settings.spineUpwardOffset, Settings.crouchSpineUpwardOffset, crouchRatio);
    }

    public float GetEffectiveChestBackwardOffset(float crouchRatio)
    {
        if (Settings == null) return chestBackwardOffset;
        return Mathf.Lerp(Settings.chestBackwardOffset, Settings.crouchChestBackwardOffset, crouchRatio);
    }

    public float GetEffectiveChestUpwardOffset(float crouchRatio)
    {
        if (Settings == null) return chestUpwardOffset;
        return Mathf.Lerp(Settings.chestUpwardOffset, Settings.crouchChestUpwardOffset, crouchRatio);
    }

    public float GetEffectiveUpperChestBackwardOffset(float crouchRatio)
    {
        if (Settings == null) return upperChestBackwardOffset;
        return Mathf.Lerp(Settings.upperChestBackwardOffset, Settings.crouchUpperChestBackwardOffset, crouchRatio);
    }

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        InitializeBones();
    }

    private void OnEnable()
    {
        if (!_isInitialized)
        {
            InitializeBones();
        }
    }

    private void InitializeBones()
    {
        if (Camera != null)
        {
            _cameraTransform = Camera.transform;
        }

        if (PlayerBodyTransform != null)
        {
            var lookAt = PlayerBodyTransform.GetComponentInChildren<LookAtController>();
            if (lookAt != null)
            {
                _thirdPersonModelTransform = lookAt.transform;
            }
            else
            {
                _thirdPersonModelTransform = PlayerBodyTransform;
            }
        }

        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }

        _hiddenBones.Clear();

        if (_animator != null && _animator.isHuman)
        {
            // Humanoid 본 획득
            _spineTransform = _animator.GetBoneTransform(HumanBodyBones.Spine);
            _chestTransform = _animator.GetBoneTransform(HumanBodyBones.Chest);
            _upperChestTransform = _animator.GetBoneTransform(HumanBodyBones.UpperChest);
            _hipsTransform = _animator.GetBoneTransform(HumanBodyBones.Hips);

            AddHiddenBone(_animator.GetBoneTransform(HumanBodyBones.Head));
            AddHiddenBone(_animator.GetBoneTransform(HumanBodyBones.Neck));
            AddHiddenBone(_animator.GetBoneTransform(HumanBodyBones.Jaw));
            AddHiddenBone(_animator.GetBoneTransform(HumanBodyBones.LeftShoulder));
            AddHiddenBone(_animator.GetBoneTransform(HumanBodyBones.RightShoulder));
            AddHiddenBone(_animator.GetBoneTransform(HumanBodyBones.LeftUpperArm));
            AddHiddenBone(_animator.GetBoneTransform(HumanBodyBones.RightUpperArm));
            AddHiddenBone(_animator.GetBoneTransform(HumanBodyBones.LeftLowerArm));
            AddHiddenBone(_animator.GetBoneTransform(HumanBodyBones.RightLowerArm));
            AddHiddenBone(_animator.GetBoneTransform(HumanBodyBones.LeftHand));
            AddHiddenBone(_animator.GetBoneTransform(HumanBodyBones.RightHand));
        }

        // 이름 기반 본 탐색(Mixamo rig 등 fallback 및 안전 장치)
        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
        {
            string lower = t.name.ToLower();
            if (lower.Contains("armature") || lower.Contains("root")) continue;

            if (lower.Contains("head") || lower.Contains("neck") || 
                lower.Contains("shoulder") || (lower.Contains("arm") && !lower.Contains("armature")) || 
                lower.Contains("forearm") || lower.Contains("hand"))
            {
                // 다리(Leg), 발(Foot, Toe), 골반/허리 제외
                if (!lower.Contains("leg") && !lower.Contains("foot") && !lower.Contains("toe") && 
                    !lower.Contains("hips") && !lower.Contains("pelvis") && !lower.Contains("spine") && !lower.Contains("chest"))
                {
                    AddHiddenBone(t);
                }
            }

            if (_spineTransform == null && (lower.Contains("spine") || lower == "spine"))
                _spineTransform = t;
            if (_chestTransform == null && (lower.Contains("chest") || lower.Contains("spine1")))
                _chestTransform = t;
            if (_upperChestTransform == null && lower.Contains("spine2"))
                _upperChestTransform = t;
            if (_hipsTransform == null && (lower.Contains("hips") || lower.Contains("pelvis")))
                _hipsTransform = t;
        }

        // 다리의 모든 SkinnedMeshRenderer가 화면 밖에서도 컬링되지 않도록 보장
        var skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var smr in skinnedMeshRenderers)
        {
            smr.updateWhenOffscreen = true;
        }

        _isInitialized = true;
    }

    private void AddHiddenBone(Transform bone)
    {
        if (bone == null || _hiddenBones.Contains(bone)) return;

        // 루트/컨테이너 본(Armature, Root), 골반/허리(Hips, Pelvis, Spine), 다리/발(Leg, Foot, Toe) 보호
        if (bone == transform || (_animator != null && bone == _animator.transform)) return;
        if (_hipsTransform != null && bone == _hipsTransform) return;
        if (_spineTransform != null && bone == _spineTransform) return;

        string lower = bone.name.ToLower();
        if (lower.Contains("armature") || lower.Contains("root") ||
            lower.Contains("hips") || lower.Contains("pelvis") ||
            lower.Contains("spine") || lower.Contains("chest") ||
            lower.Contains("leg") || lower.Contains("foot") || lower.Contains("toe") || 
            lower.Contains("thigh") || lower.Contains("calf"))
        {
            return;
        }

        _hiddenBones.Add(bone);
    }

    private void LateUpdate()
    {
        // 로컬 플레이어 확인 (로컬 플레이어가 아닌 경우 1인칭 다리 비활성화)
        if (Player != null && !IsOwned)
        {
            gameObject.SetActive(false);
            return;
        }

        if (!_isInitialized)
        {
            InitializeBones();
        }

        if (_cameraTransform == null && Camera != null)
        {
            _cameraTransform = Camera.transform;
        }

        if (_thirdPersonModelTransform == null && PlayerBodyTransform != null)
        {
            var lookAt = PlayerBodyTransform.GetComponentInChildren<LookAtController>();
            _thirdPersonModelTransform = lookAt != null ? lookAt.transform : PlayerBodyTransform;
        }

        // 1. 머리, 목, 어깨, 팔, 손 전체 본 스케일을 (0, 0, 0)으로 매 프레임 강제 축소
        for (int i = 0; i < _hiddenBones.Count; i++)
        {
            Transform bone = _hiddenBones[i];
            if (bone != null)
            {
                bone.localScale = Vector3.zero;
            }
        }

        // 2. 카메라 피치 각도(Pitch)에 따라 고개를 숙일 때의 비율 계산
        float pitchRatio = 0f;
        if (_cameraTransform != null)
        {
            float pitch = _cameraTransform.localEulerAngles.x;
            if (pitch > 180f) pitch -= 360f;
            // pitch > 0 은 아래를 내려다보는 각도 (-89 ~ +89 범위 중 0 ~ 80 정규화)
            pitchRatio = Mathf.Clamp01(pitch / 80f);
        }

        // 3. 다리 루트 회전: 3인칭 몸체 모델과 100% 동기화 (전체 각도 왜곡 없음)
        if (_thirdPersonModelTransform != null)
        {
            transform.rotation = _thirdPersonModelTransform.rotation;
        }
        else if (PlayerBodyTransform != null)
        {
            transform.rotation = PlayerBodyTransform.rotation;
        }

        // 카메라의 수평 시선 전방/후방 벡터 계산 (몸체 회전과 무관하게 항상 카메라 시선 기준 유지)
        Vector3 camForward = Vector3.forward;
        if (_cameraTransform != null)
        {
            camForward = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
            if (camForward.sqrMagnitude < 0.001f)
            {
                camForward = Player != null ? Player.transform.forward : transform.forward;
            }
        }
        else if (Player != null)
        {
            camForward = Player.transform.forward;
        }
        Vector3 camBackward = -camForward;

        Vector3 basePos = Player != null ? Player.transform.position : transform.position;
        if (PlayerBodyTransform != null)
        {
            // 앉기(Crouch) 등으로 인해 3인칭 몸체 높이가 보정되는 월드 Y 좌표를 그대로 동기화하여 파묻힘 방지
            basePos.y = PlayerBodyTransform.position.y;
        }

        if (_cameraTransform != null)
        {
            // 카메라의 수평(X, Z) 위치를 추종하여 시선 중심축 정렬
            basePos.x = _cameraTransform.position.x;
            basePos.z = _cameraTransform.position.z;
        }

        // 다리 루트 위치: 3인칭 몸체 지면 높이(Y) 및 모델 회전 기준 후방 오프셋(firstPersonBodyBackwardOffset) 적용
        float bodyBack = Settings != null ? Settings.firstPersonBodyBackwardOffset : 0.12f;
        transform.position = basePos - (transform.forward * bodyBack);

        // 4. 앉기(Crouch) 진행 비율 부드럽게 보간 (0: 서있음 ~ 1: 앉음)
        bool isCrouching = PlayerMove != null && PlayerMove.IsCrouching;
        _crouchRatio = Mathf.Lerp(_crouchRatio, isCrouching ? 1f : 0f, Time.deltaTime * 9f);

        float currentSpineBackward = GetEffectiveSpineBackwardOffset(_crouchRatio);
        float currentSpineUpward = GetEffectiveSpineUpwardOffset(_crouchRatio);
        float currentChestBackward = GetEffectiveChestBackwardOffset(_crouchRatio);
        float currentChestUpward = GetEffectiveChestUpwardOffset(_crouchRatio);
        float currentUpperChestBackward = GetEffectiveUpperChestBackwardOffset(_crouchRatio);

        // 5. 상체 본 체인(Spine -> Chest -> UpperChest)을 위로 갈수록 비스듬히 점진적으로 더 뒤로 빼기 (회전 없이 위치만 점진적 오프셋)
        float dynamicSpineBackward = currentSpineBackward + (pitchRatio * 0.12f);
        float dynamicChestBackward = dynamicSpineBackward + currentChestBackward + (pitchRatio * 0.08f);
        float dynamicUpperChestBackward = dynamicChestBackward + currentUpperChestBackward + (pitchRatio * 0.06f);

        if (_spineTransform != null && (dynamicSpineBackward > 0.001f || Mathf.Abs(currentSpineUpward) > 0.001f))
        {
            _spineTransform.position += (camBackward * dynamicSpineBackward) + (Vector3.up * currentSpineUpward);
        }

        if (_chestTransform != null && (dynamicChestBackward > 0.001f || Mathf.Abs(currentChestUpward) > 0.001f))
        {
            _chestTransform.position += (camBackward * dynamicChestBackward) + (Vector3.up * currentChestUpward);
        }

        if (_upperChestTransform != null && dynamicUpperChestBackward > 0.001f)
        {
            _upperChestTransform.position += (camBackward * dynamicUpperChestBackward) + (Vector3.up * currentChestUpward);
        }
    }
}


