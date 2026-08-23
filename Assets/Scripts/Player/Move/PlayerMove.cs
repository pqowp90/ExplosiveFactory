using UnityEditor;
using UnityEngine;
using System;
using UnityEngine.InputSystem;
using Mirror;
using System.Security.Cryptography;

public class PlayerMove : NetworkBehaviour
{
    public float speed = 2.5f;
    [SerializeField]
    private float runSpeedRatio = 1.7f;
    [SerializeField]
    private float crouchSpeedRatio = 0.7f;
    [SerializeField]
    private float moveLerp = 5f;
    [SerializeField]
    private float gravity = -9.81f;  // 중력 가속도
    [SerializeField]
    private float jumpHeight = 1f;

    [SerializeField]
    private Transform neckTransform;

    private Vector3 _cameraOffset;
    private CharacterController controller;

    private Vector3 curVelocity;
    private Vector3 addForce = Vector3.zero;
    public bool isGrounded { get { return controller.isGrounded; } private set { } }
    // private Vector3 hitPointNormal;
    [SerializeField]
    private float resistance = 0.9f;
    [SerializeField]
    private float slopeSpeed = 1.1f;
    private bool isRunning;
    public bool IsRunning { get { return isRunning; } set { isRunning = value; _playerAnimator.SetRun(value); } }
    private bool isCrouching = false;
    public bool IsCrouching { get { return isCrouching; } set { isCrouching = value; _playerAnimator.SetCrouch(value); CmdSetCrouch(value); } }
    [Command]
    private void CmdSetCrouch(bool crouch)
    {
        RpcSetCrouch(crouch);
    }
    [ClientRpc]
    private void RpcSetCrouch(bool crouch)
    {
        if (isOwned) return;
        isCrouching = crouch;
    }

    public Vector2 MoveValue;

    //private bool willSlideOnSlope = true;
    private Vector3 downForce;

    private PlayerAnimation _playerAnimator;
    private void Awake()
    {
        // Cursor.visible = false;
        // Cursor.lockState = CursorLockMode.Locked;

        _player = GetComponent<Player>();
        controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            _originHeight = controller.height;
            _originCenterY = controller.center.y;
        }

        if (neckTransform == null)
        {
            var cam = GetComponentInChildren<Camera>(true);
            if (cam != null) neckTransform = cam.transform.parent ?? cam.transform;
            else neckTransform = transform;
        }
        _cameraOffset = neckTransform != null ? neckTransform.localPosition : Vector3.up * 1.6f;
        _lerpedCrouchHeight = CROUCH_RATIO;
        _crouchHeight = CROUCH_RATIO;
        _playerAnimator = GetComponent<PlayerAnimation>();
    }

    private void Start()
    {
        // 컴포넌트를 초기화합니다.
        //Cursor.lockState = CursorLockMode.Locked;
        // 마우스 커서를 잠금 상태로 설정하여 화면을 클릭해도 마우스가 움직이지 않도록 합니다.

    }



    public void AddForce(Vector3 velocity)
    {
        addForce.x += velocity.x;
        addForce.z += velocity.z;
        curVelocity.y += velocity.y;
    }

    // private bool IsSliding{
    //     get{
    //         bool sliding = minAngle > controller.slopeLimit;
    //         minAngle = 90f;
    //         return sliding;
    //     }
    // }
    //private float minAngle = 90f;
    private static readonly float MAX_RAY_DIST = 0.1f;
    private float DownDown
    {
        get
        {
            var origin = transform.position + Vector3.up * (_originHeight - controller.height);
            Debug.DrawRay(origin, Vector3.down, Color.blue);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit slopeHit, MAX_RAY_DIST))
            {
                return Vector3.Angle(slopeHit.normal, Vector3.down) / 9f;
            }
            else
            {
                return 0f;
            }
        }
    }
    private static readonly float MAX_ANIM_RAY_DIST = 0.3f;
    private void SetRayDist()
    {
        if (isGrounded) return;
        var origin = transform.position + Vector3.up * (_originHeight - controller.height);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit slopeHit, MAX_ANIM_RAY_DIST))
        {
            _playerAnimator.SetRayDist(slopeHit.distance / MAX_ANIM_RAY_DIST);
        }
    }
    private Action OnNextDrawGizmos;

    private void OnDrawGizmos()
    {
        OnNextDrawGizmos?.Invoke();
        OnNextDrawGizmos = null;
    }
    private bool isSliding;
    private void UpdateSlopeSliding()
    {
        if (isGrounded)
        {
            var sphereCastVerticalOffset = controller.height / 2 - controller.radius;
            var castOrigin = transform.position - new Vector3(0f, sphereCastVerticalOffset, 0f) + controller.center;
            float downLength = 0.05f + controller.skinWidth;
            if (Physics.SphereCast(castOrigin, controller.radius - 0.001f, Vector3.down,
                out var hit, downLength, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore))
            {
                var collider = hit.collider;
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
    private float _lerpedInputMagnitude;
    private float _moveSpeed;
    public bool IsMoving { get { return MoveValue.magnitude > 0.1f; } }
    private void PositiveMove()
    {
        // float moveX = Input.GetAxisRaw("Horizontal");
        // float moveZ = Input.GetAxisRaw("Vertical");
        _moveInput = _player != null && _player.InputController != null 
            ? _player.InputController.MoveValue 
            : Vector2.zero;
        float lerpSpeed;
        if (Mathf.Abs(_lerpedInput.magnitude - _moveInput.normalized.magnitude) > 0.5f)
            lerpSpeed = 9f;
        else
            lerpSpeed = 19f;

        _lerpedInput = Vector2.Lerp(_lerpedInput, _moveInput.normalized, Time.deltaTime * lerpSpeed);
        if (_moveInput == Vector2.zero && _lerpedInput.sqrMagnitude < 0.0001f)
        {
            _lerpedInput = Vector2.zero;
        }

        MoveValue = _lerpedInput;
        //playerAnimation.isRunning = isRunning && (moveInput.magnitude > 0);

        Vector3 move = transform.right * _moveInput.x + transform.forward * _moveInput.y;
        move.Normalize();
        //playerAnimation.moveDir = Vector3.right * moveInput.x + Vector3.forward * moveInput.y;
        // 캐릭터의 이동 방향을 계산합니다.
        float yVelocity = curVelocity.y;
        float airControl = moveLerp;
        _playerAnimator.SetGrounded(isGrounded);
        if (_player != null && _player.InputController != null)
        {
            IsRunning = _player.InputController.IsSprinting;
        }
        else if (Keyboard.current != null)
        {
            IsRunning = Keyboard.current.leftShiftKey.isPressed;
        }
        if (!isGrounded)
        {
            airControl *= 0.3f;
        }
        else
        {
            _moveSpeed = speed;
            if (IsCrouching)
                _moveSpeed *= crouchSpeedRatio;
            else if (IsRunning)
                _moveSpeed *= runSpeedRatio;
        }
        curVelocity = Vector3.Lerp(curVelocity, move * _moveSpeed, Time.deltaTime * airControl);
        curVelocity.y = yVelocity;
        // 중력을 적용합니다.
        curVelocity.y += gravity * Time.deltaTime;

        downForce = Vector3.zero;
        // if(willSlideOnSlope && IsSliding && controller.isGrounded){
        //     curVelocity += new Vector3(hitPointNormal.x, -hitPointNormal.y, hitPointNormal.z) * slopeSpeed;
        // }else 
        if (curVelocity.y < 0)
        {
            downForce = -DownDown * Vector3.up;
        }

        float zVelocity = isGrounded ? 0f : curVelocity.y;
        _playerAnimator.SetMove(new Vector3(_lerpedInput.x, _lerpedInput.y, zVelocity));

        controller.Move((curVelocity + addForce + downForce) * Time.deltaTime);

        if (isGrounded && !isSliding)
        {
            // 캐릭터가 땅에 있을 때만 점프 가능하도록 처리합니다.
            if (_player != null && _player.InputController != null && _player.InputController.IsJumping)
            {
                curVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _playerAnimator.SetJump();
                // 점프 높이에 따라 점프 속도를 계산합니다.
                //playerAnimation.GoJump();
            }
            else
            {
                // 캐릭터가 땅에 닿아 있을 때만 y 속도를 초기화합니다.
                curVelocity.y = -2f;
            }
        }


    }

    private void CrouchUpdate()
    {

        if (IsCrouching)
            _crouchHeight = _originHeight * CROUCH_RATIO;
        else
            _crouchHeight = _originHeight;
        _lerpedCrouchHeight = Mathf.Lerp(_lerpedCrouchHeight, _crouchHeight, Time.deltaTime * 9f);
        controller.height = _lerpedCrouchHeight;
        neckTransform.localPosition = _cameraOffset + Vector3.down * (_originHeight - controller.height) / 2f;
        _player.PlayerBodyTransform.localPosition = Vector3.up * (_originHeight - controller.height) / 2f;
        //controller.center = new Vector3(0, controller.height / 2f, 0);
        //controller.center = new Vector3(0, _originCenterY + (_originHeight - controller.height) / 2f, 0);
    }


    private bool IsCanStandUp()
    {
        var headPosition = transform.position + Vector3.up * controller.height;
        Debug.DrawRay(headPosition, Vector3.up * (_originHeight - controller.height), Color.red);
        if (Physics.Raycast(headPosition, Vector3.up, _originHeight - controller.height))
        {
            return false;
        }
        return true;
    }

    private const float CROUCH_RATIO = 0.6f;
    private float _crouchHeight;
    private float _lerpedCrouchHeight;
    private float _originHeight;
    private float _originCenterY;
    private Vector2 _lerpedInput;
    private Vector2 _moveInput;
    private Player _player;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {

        // Debug.DrawRay(hit.point, Vector3.down*2f, Color.blue);
        // if(controller.isGrounded && Physics.Raycast(hit.point, Vector3.down, out RaycastHit slopeHit, 2f)){
        //     hitPointNormal = slopeHit.normal;
        //     if(minAngle > Vector3.Angle(hitPointNormal, Vector3.up))
        //     {
        //         minAngle = Vector3.Angle(hitPointNormal, Vector3.up);
        //     }
        // }

        // 충돌된 물체의 릿지드 바디를 가져옴
        Rigidbody body = hit.collider.attachedRigidbody;

        // 만약에 충돌된 물체에 콜라이더가 없거나, isKinematic이 켜저있으면 리턴
        if (body == null || body.isKinematic) return;

        if (hit.moveDirection.y < -0.3f)
        {
            return;
        }

        // pushDir이라는 벡터값에 새로운 백터값 저장. 부딪힌 물체의 x의 방향과 y의 방향을 저장
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
        // 부딪힌 물체의 릿지드바디의 velocity에 위에 저장한 백터 값과 힘을 곱해줌
        body.linearVelocity = pushDir * 4f;
    }

    private void Update()
    {
        CrouchUpdate();
        if (!isOwned) return;
        SetRayDist();
        UpdateSlopeSliding();
        PositiveMove();
        if (_player != null && _player.InputController != null)
        {
            IsCrouching = _player.InputController.IsCrouching && IsCanStandUp();
        }
    }
    private void FixedUpdate()
    {
        if (addForce.x != 0)
        {
            addForce.x += resistance * ((addForce.x > 0) ? -1f : 1f);
            if (Mathf.Abs(addForce.x) < 0.5) addForce.x = 0;
        }
        if (addForce.z != 0)
        {
            addForce.z += resistance * ((addForce.z > 0) ? -1f : 1f);
            if (Mathf.Abs(addForce.z) < 0.5) addForce.z = 0;
        }
    }
}