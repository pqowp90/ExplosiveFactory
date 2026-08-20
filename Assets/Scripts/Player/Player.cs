using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : NetworkBehaviour, IPoolable
{
    [HideInInspector]
    public ItemHolder ItemHolder;
    [HideInInspector]
    public PlayerMove PlayerMove;
    [HideInInspector]
    public PlayerRotate PlayerRotate;
    [HideInInspector]
    public PlayerAnimation PlayerAnimation;
    [HideInInspector]
    public PlayerInput PlayerInput;
    public Transform PlayerBodyTransform;
    public Transform PlayerHandTransform;
    [HideInInspector]
    public Transform PlayerLegTransform;

    [Header("First Person Legs Settings (프리팹 설정)")]
    public FirstPersonLegsSettings FirstPersonLegsSettings = new FirstPersonLegsSettings();

    private InputController _inputController;
    public Camera Camera;
    public Camera HandCamera;

    public InputController InputController
    {
        get
        {
            if (_inputController == null)
                _inputController = GetComponent<InputController>();
            if (_inputController == null)
            {
                Debug.LogError("InputController is null, Returning null");
                return null!;
            }
            if (!_inputController.IsInitialized)
                _inputController.Initialize();
            return _inputController;
        }
        private set
        {
            _inputController = value;
        }
    }

    public void SetupFirstPersonLegs()
    {
        if (PlayerLegTransform == null && PlayerBodyTransform != null)
        {
            // 3인칭 몸체를 복제하여 1인칭 전용 다리 생성
            GameObject legsObj = Instantiate(PlayerBodyTransform.gameObject, transform);
            legsObj.name = "FirstPersonLegs";

            // 1인칭 다리에 불필요한 3인칭 컴포넌트 제거
            var lookAt = legsObj.GetComponentInChildren<LookAtController>();
            if (lookAt != null) Destroy(lookAt);

            var netAnimator = legsObj.GetComponentInChildren<CustomNetworkAnimator>();
            if (netAnimator != null) Destroy(netAnimator);

            // 1인칭 다리 제어기 추가
            if (legsObj.GetComponent<FirstPersonLegsController>() == null)
            {
                legsObj.AddComponent<FirstPersonLegsController>();
            }

            PlayerLegTransform = legsObj.transform;
        }
    }

    public void OnDespawned()
    {
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.UnsetCursorFromSource(this);
        }
    }

    public void OnSpawned()
    {
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.SetCursor(CursorType.Player, this);
        }
    }

    private void Awake()
    {
        SetupFirstPersonLegs();
        ItemHolder = GetComponent<ItemHolder>();
        PlayerMove = GetComponent<PlayerMove>();
        PlayerRotate = GetComponent<PlayerRotate>();
        PlayerAnimation = GetComponent<PlayerAnimation>();
        PlayerInput = GetComponent<PlayerInput>();
        InputController = GetComponent<InputController>();
        if (Camera == null || HandCamera == null)
        {
            var cams = GetComponentsInChildren<Camera>(true);
            foreach (var c in cams)
            {
                if (c.gameObject.name.ToLower().Contains("hand"))
                    HandCamera = c;
                else if (Camera == null)
                    Camera = c;
            }
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
    }
}

[System.Serializable]
public class FirstPersonLegsSettings
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
}

