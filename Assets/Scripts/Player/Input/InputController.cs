using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
    private PlayerInput playerInput;
    public InputAction MoveAction;
    public InputAction LookAction;
    public InputAction JumpAction;
    public InputAction CrouchAction;
    public InputAction SprintAction;
    public InputAction MouseLeftClickAction;
    public InputAction MouseRightClickAction;
    public InputAction MouseScrollAction;
    public InputAction InteractAction;
    public InputAction DropAction;

    public bool IsInitialized = false;

    /// <summary>
    /// UI 커서가 열려있지 않고 게임플레이 입력을 받을 수 있는 상태인지 검사
    /// </summary>
    public bool CanProcessGameplayInput => CursorManager.Instance == null || CursorManager.Instance.CurrentCursor == CursorType.Player;

    public Vector2 MoveValue
    {
        get
        {
            if (!CanProcessGameplayInput) return Vector2.zero;
            return MoveAction != null ? MoveAction.ReadValue<Vector2>() : Vector2.zero;
        }
    }

    public Vector2 LookValue
    {
        get
        {
            if (!CanProcessGameplayInput) return Vector2.zero;
            return LookAction != null ? LookAction.ReadValue<Vector2>() : (Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero);
        }
    }

    public bool IsJumping => CanProcessGameplayInput && JumpAction != null && JumpAction.ReadValue<float>() > 0;
    public bool IsCrouching => CanProcessGameplayInput && CrouchAction != null && CrouchAction.ReadValue<float>() > 0;
    public bool IsSprinting => CanProcessGameplayInput && (SprintAction != null ? SprintAction.ReadValue<float>() > 0 : (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed));

    public bool IsJumpTriggered
    {
        get
        {
            if (!CanProcessGameplayInput) return false;
            return JumpAction != null && JumpAction.triggered;
        }
    }

    public bool IsCrouchTriggered
    {
        get
        {
            if (!CanProcessGameplayInput) return false;
            return CrouchAction != null && CrouchAction.triggered;
        }
    }

    public bool IsSprintPressed
    {
        get
        {
            if (!CanProcessGameplayInput) return false;
            return SprintAction != null && SprintAction.IsPressed();
        }
    }

    /// <summary>
    /// F 키 / 상호작용 트리거 여부
    /// </summary>
    public bool IsInteractTriggered
    {
        get
        {
            if (!CanProcessGameplayInput) return false;
            if (InteractAction != null && InteractAction.triggered) return true;
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) return true;
            return false;
        }
    }

    /// <summary>
    /// G 키 / 아이템 버리기 트리거 여부
    /// </summary>
    public bool IsDropTriggered
    {
        get
        {
            if (!CanProcessGameplayInput) return false;
            if (DropAction != null && DropAction.triggered) return true;
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame) return true;
            return false;
        }
    }

    /// <summary>
    /// 실제 마우스 좌클릭 (Primary) 트리거 여부 (게임플레이 상태일 때만)
    /// </summary>
    public bool IsUseTriggered => IsLeftClickTriggered;
    public bool IsLeftClickTriggered
    {
        get
        {
            if (!CanProcessGameplayInput) return false;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (MouseLeftClickAction != null && MouseLeftClickAction.triggered) return true;
            return false;
        }
    }

    /// <summary>
    /// 실제 마우스 우클릭 (Secondary / 아이템 사용) 트리거 여부 (게임플레이 상태일 때만)
    /// </summary>
    public bool IsUseSecondaryTriggered => IsRightClickTriggered;
    public bool IsRightClickTriggered
    {
        get
        {
            if (!CanProcessGameplayInput) return false;
            return IsRawRightClickTriggered;
        }
    }

    /// <summary>
    /// 실제 마우스 우클릭 트리거 여부 (UI 모드 여부 무관, 스마트폰 뒤로가기/닫기용)
    /// </summary>
    public bool IsRawSecondaryClickTriggered => IsRawRightClickTriggered;
    public bool IsRawRightClickTriggered
    {
        get
        {
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) return true;
            if (MouseRightClickAction != null && MouseRightClickAction.triggered) return true;
            return false;
        }
    }

    /// <summary>
    /// 마우스 휠 스크롤 델타 값
    /// </summary>
    public float ScrollValue
    {
        get
        {
            if (!CanProcessGameplayInput) return 0f;
            if (MouseScrollAction != null && MouseScrollAction.enabled)
            {
                var val = MouseScrollAction.ReadValueAsObject();
                if (val is float f && Mathf.Abs(f) > 0.01f) return f;
                if (val is Vector2 v && Mathf.Abs(v.y) > 0.01f) return v.y;
            }
            if (Mouse.current != null)
            {
                return Mouse.current.scroll.ReadValue().y;
            }
            return 0f;
        }
    }

    public void Initialize()
    {
        IsInitialized = true;
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null && playerInput.actions != null)
        {
            MoveAction = playerInput.actions.FindAction("Move");
            LookAction = playerInput.actions.FindAction("Look");
            JumpAction = playerInput.actions.FindAction("Jump");
            CrouchAction = playerInput.actions.FindAction("Crouch");
            SprintAction = playerInput.actions.FindAction("Sprint") ?? playerInput.actions.FindAction("Run");
            MouseLeftClickAction = playerInput.actions.FindAction("MouseLeftClick") ?? playerInput.actions.FindAction("MouseClick") ?? playerInput.actions.FindAction("Fire");
            MouseRightClickAction = playerInput.actions.FindAction("MouseRightClick");
            MouseScrollAction = playerInput.actions.FindAction("MouseScroll") ?? playerInput.actions.FindAction("Scroll");
            InteractAction = playerInput.actions.FindAction("Interact");
            DropAction = playerInput.actions.FindAction("Drop");

            MoveAction?.Enable();
            LookAction?.Enable();
            JumpAction?.Enable();
            CrouchAction?.Enable();
            SprintAction?.Enable();
            MouseLeftClickAction?.Enable();
            MouseRightClickAction?.Enable();
            MouseScrollAction?.Enable();
            InteractAction?.Enable();
            DropAction?.Enable();
        }
    }

    private void Awake()
    {
        if (IsInitialized) return;
        Initialize();
    }
}
