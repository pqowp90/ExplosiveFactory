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
    public InputAction MouseClickAction;
    public InputAction MouseLeftClickAction;
    public InputAction MouseScrollAction;
    public InputAction InteractAction;

    public bool IsInitialized = false;

    public Vector2 MoveValue => MoveAction != null ? MoveAction.ReadValue<Vector2>() : Vector2.zero;
    public Vector2 LookValue => LookAction != null ? LookAction.ReadValue<Vector2>() : (Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero);
    public bool IsJumping => JumpAction != null && JumpAction.ReadValue<float>() > 0;
    public bool IsCrouching => CrouchAction != null && CrouchAction.ReadValue<float>() > 0;
    public bool IsSprinting => SprintAction != null ? SprintAction.ReadValue<float>() > 0 : (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed);

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
            MouseClickAction = playerInput.actions.FindAction("MouseClick");
            MouseLeftClickAction = playerInput.actions.FindAction("MouseLeftClick");
            MouseScrollAction = playerInput.actions.FindAction("MouseScroll");
            InteractAction = playerInput.actions.FindAction("Interact");
        }
    }

    private void Awake()
    {
        if (IsInitialized) return;
        Initialize();
    }
}
