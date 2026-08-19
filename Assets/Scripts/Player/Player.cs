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
        ItemHolder = GetComponent<ItemHolder>();
        PlayerMove = GetComponent<PlayerMove>();
        PlayerRotate = GetComponent<PlayerRotate>();
        PlayerInput = GetComponent<PlayerInput>();
        PlayerAnimation = GetComponent<PlayerAnimation>();
        InputController = GetComponent<InputController>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
    }
}
