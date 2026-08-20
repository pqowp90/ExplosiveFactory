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
    public Transform PlayerLegTransform;
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
