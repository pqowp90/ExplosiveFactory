using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRotate : NetworkBehaviour
{
    private PlayerInput _playerInput;
    public InputAction lookAction;
    private float xRotation = 0f;
    private float yRotation = 0f;

    [SerializeField]
    private Transform cameraRoot;

    [SerializeField]
    private float sensitivity = 0.2f;
    private Player _player;

    private void Awake()
    {
        _player = GetComponent<Player>();
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput != null && _playerInput.actions != null)
        {
            lookAction = _playerInput.actions.FindAction("Look");
            lookAction?.Enable();
        }

        if (cameraRoot == null)
        {
            var cam = GetComponentInChildren<Camera>(true);
            if (cam != null) cameraRoot = cam.transform.parent ?? cam.transform;
            else cameraRoot = transform;
        }

        if (sensitivity <= 0f)
        {
            sensitivity = 0.2f;
        }

        xRotation = transform.eulerAngles.y;
        if (cameraRoot != null)
        {
            float pitch = cameraRoot.localEulerAngles.x;
            if (pitch > 180f) pitch -= 360f;
            yRotation = pitch;
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (isOwned && CursorManager.Instance != null)
        {
            CursorManager.Instance.SetCursor(CursorType.Player, this);
        }
    }

    private void Start()
    {
        if (isOwned && CursorManager.Instance != null)
        {
            CursorManager.Instance.SetCursor(CursorType.Player, this);
        }
    }

    private void OnDestroy()
    {
        if (isOwned && CursorManager.Instance != null)
        {
            CursorManager.Instance.UnsetCursorFromSource(this);
        }
    }

    private void Update()
    {
        if (!isOwned) return;

        // UI 커서 상태일 때는 마우스 시야 회전을 수행하지 않음
        if (CursorManager.Instance != null && CursorManager.Instance.CurrentCursor == CursorType.UI)
        {
            return;
        }

        RotateMove();
    }

    private void RotateMove()
    {
        Vector2 lookInput = Vector2.zero;
        if (_player != null && _player.InputController != null)
        {
            lookInput = _player.InputController.LookValue;
        }
        else if (lookAction != null && lookAction.enabled)
        {
            lookInput = lookAction.ReadValue<Vector2>();
        }
        else if (Mouse.current != null)
        {
            lookInput = Mouse.current.delta.ReadValue();
        }

        if (lookInput.sqrMagnitude < 0.0001f) return;

        float mouseX = lookInput.x * sensitivity;
        float mouseY = lookInput.y * sensitivity;

        xRotation += mouseX;
        yRotation -= mouseY;

        yRotation = Mathf.Clamp(yRotation, -89f, 89f);

        if (cameraRoot != null)
        {
            cameraRoot.localRotation = Quaternion.Euler(yRotation, 0f, 0f);
        }
        transform.rotation = Quaternion.Euler(0f, xRotation, 0f);
    }
}
