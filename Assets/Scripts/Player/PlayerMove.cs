using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    [Header("Movement Settings (from BackPack)")]
    public float speed = 2.5f;
    [SerializeField]
    private float moveLerp = 10f;
    [SerializeField]
    private float gravity = -13f;  // 중력 가속도
    [SerializeField]
    private float jumpHeight = 1f;
    [SerializeField]
    private float slopeSpeed = 1.1f;
    [SerializeField]
    private float resistance = 0.4f;

    [Header("Mouse Look Sensitivity (from BackPack)")]
    [SerializeField]
    public float sensitivity = 0.277f;

    [Header("References")]
    [SerializeField]
    private Camera myCamera;
    [SerializeField]
    private Transform cameraRoot;

    private CharacterController controller;
    public CharacterController Controller { get { if (!controller) controller = GetComponent<CharacterController>(); return controller; } }
    private float xRotation = 0f;
    private float yRotation = 0f;
    [SerializeField]
    private Vector3 curVelocity;
    public Vector3 addForce = Vector3.zero;
    private PlayerAnimation playerAnimation;
    public bool isGrounded { get { return controller != null && controller.isGrounded; } }
    public bool isRunning;
    private Vector3 hitPointNormal;

    private bool willSlideOnSlope = true;
    private Vector3 downForce;
    private PlayerInput playerInput;
    public InputAction moveAction;
    public InputAction lookAction;
    public InputAction jumpAction;
    public int jumpCount;

    private void Awake()
    {
        if (myCamera == null) myCamera = GetComponentInChildren<Camera>(true);
        if (cameraRoot == null && myCamera != null) cameraRoot = myCamera.transform.parent ?? myCamera.transform;
        
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null && playerInput.actions != null)
        {
            moveAction = playerInput.actions["Move"];
            lookAction = playerInput.actions["Look"];
            jumpAction = playerInput.actions["Jump"];
        }
    }

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        playerAnimation = GetComponent<PlayerAnimation>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // ESC로 마우스 커서 잠금 토글
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool isLocked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = isLocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isLocked;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        RotateMove();
        UpdateSlopeSliding();
        PositiveMove();
    }

    private void RotateMove()
    {
        Vector2 lookInput = Vector2.zero;
        if (lookAction != null && lookAction.enabled)
        {
            lookInput = lookAction.ReadValue<Vector2>();
        }
        else if (Mouse.current != null)
        {
            lookInput = Mouse.current.delta.ReadValue();
        }

        // BackPack 원본 감도 계산 공식
        float mouseX = lookInput.x * sensitivity * 2f;
        float mouseY = lookInput.y * sensitivity * 2f;

        xRotation -= mouseX;
        yRotation -= mouseY;
        yRotation = Mathf.Clamp(yRotation, -89f, 89f);

        if (cameraRoot != null)
        {
            cameraRoot.transform.localRotation = Quaternion.Euler(yRotation, 0f, 0f);
        }
        transform.localRotation = Quaternion.Euler(0f, -xRotation, 0f);
    }

    public void AddForce(Vector3 velocity)
    {
        addForce.x += velocity.x;
        addForce.z += velocity.z;
        curVelocity.y += velocity.y;
    }

    private float minAngle = 90f;
    private float DowmDowm
    {
        get
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopeHit, 0.1f))
            {
                return Vector3.Angle(slopeHit.normal, Vector3.down) / 9f;
            }
            return 0f;
        }
    }

    private bool isSliding;
    private void UpdateSlopeSliding()
    {
        if (isGrounded && controller != null)
        {
            var sphereCastVerticalOffset = controller.height / 2 - controller.radius;
            var castOrigin = transform.position - new Vector3(0f, sphereCastVerticalOffset, 0f) + controller.center;
            float downLength = 0.05f + controller.skinWidth;
            if (Physics.SphereCast(castOrigin, controller.radius - 0.001f, Vector3.down,
                out var hit, downLength, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore))
            {
                var angle = Vector3.Angle(Vector3.up, hit.normal);
                if (angle > controller.slopeLimit)
                {
                    var nomal = hit.normal;
                    var yInverse = 1f - nomal.y;
                    curVelocity.x += yInverse * nomal.x * Time.deltaTime * slopeSpeed * 100f;
                    curVelocity.z += yInverse * nomal.z * Time.deltaTime * slopeSpeed * 100f;
                    isSliding = true;
                }
                else
                {
                    isSliding = false;
                }
            }
        }
    }

    private void PositiveMove()
    {
        if (controller == null) return;

        float moveSpeed = speed;
        var keyboard = Keyboard.current;
        isRunning = keyboard != null && keyboard.leftShiftKey.isPressed;
        if (isRunning)
        {
            moveSpeed *= 1.7f;
        }

        Vector2 moveInput = Vector2.zero;
        if (moveAction != null && moveAction.enabled)
        {
            moveInput = moveAction.ReadValue<Vector2>();
        }

        // Direct Keyboard Fallback
        if (moveInput.sqrMagnitude < 0.01f && keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveInput.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveInput.y -= 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveInput.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveInput.x += 1f;
        }

        if (playerAnimation != null)
        {
            playerAnimation.isRunning = isRunning && (moveInput.magnitude > 0);
            playerAnimation.moveDir = Vector3.right * moveInput.x + Vector3.forward * moveInput.y;
        }

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        move.Normalize();

        float yVelocity = curVelocity.y;
        float airControl = isGrounded ? moveLerp : moveLerp * 0.3f;

        curVelocity.y = 0f;
        curVelocity = Vector3.Lerp(curVelocity, move * moveSpeed, Time.deltaTime * airControl);
        curVelocity.y = yVelocity;
        curVelocity.y += gravity * Time.deltaTime;

        downForce = Vector3.zero;
        if (curVelocity.y < 0)
        {
            downForce = -DowmDowm * Vector3.up;
        }

        controller.Move((curVelocity + addForce + downForce) * Time.deltaTime);

        // Jump
        if (isGrounded && !isSliding)
        {
            bool jumpPressed = (jumpAction != null && jumpAction.ReadValue<float>() > 0) ||
                               (keyboard != null && keyboard.spaceKey.wasPressedThisFrame);

            if (jumpPressed)
            {
                curVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (playerAnimation != null) playerAnimation.GoJump();
            }
            else
            {
                curVelocity.y = -2f;
            }
        }

        // AddForce resistance decay
        if (addForce.x != 0)
        {
            addForce.x = Mathf.MoveTowards(addForce.x, 0f, resistance * 10f * Time.deltaTime);
        }
        if (addForce.z != 0)
        {
            addForce.z = Mathf.MoveTowards(addForce.z, 0f, resistance * 10f * Time.deltaTime);
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        if (body == null || body.isKinematic) return;
        if (hit.moveDirection.y < -0.3f) return;

        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
        body.linearVelocity = pushDir * 4f;
    }
}