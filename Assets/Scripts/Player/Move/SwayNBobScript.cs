using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class SwayNBobScript : MonoBehaviour
{
    public PlayerMove mover;

    [Header("Sway")]
    public float step = 0.01f;
    public float maxStepDistance = 0.06f;
    Vector3 swayPos;

    [Header("Sway Rotation")]
    public float rotationStep = 4f;
    public float maxRotationStep = 5f;
    Vector3 swayEulerRot;

    public float smooth = 10f;
    float smoothRot = 12f;

    [Header("Bobbing")]
    public float speedCurve;
    float curveSin { get => Mathf.Sin(speedCurve); }
    float curveCos { get => Mathf.Cos(speedCurve); }

    public Vector3 travelLimit = Vector3.one * 0.025f;
    public Vector3 bobLimit = Vector3.one * 0.01f;
    Vector3 bobPosition;

    public float bobExaggeration = 1f;

    [Header("Bob Rotation")]
    public Vector3 multiplier;
    Vector3 bobEulerRotation;

    public bool FixSwayNBob = false;
    private float _fixRatio = 0f;

    private Vector2 walkInput;
    private Vector2 lookInput;
    private Player _player;

    void Awake()
    {
        if (mover == null) mover = GetComponentInParent<PlayerMove>();
        _player = GetComponentInParent<Player>();
    }

    void Update()
    {
        if (mover != null && !mover.isOwned) return;
        GetInput();
        if (!FixSwayNBob)
        {
            Sway();
            SwayRotation();
            BobOffset();
            BobRotation();
        }

        CompositePositionRotation();
    }

    public void FixSwayNBobbing(bool isFix)
    {
        FixSwayNBob = isFix;
        DOTween.To(() => _fixRatio, x => _fixRatio = x, isFix ? 1f : 0f, 0.5f).OnUpdate(() =>
        {
            swayPos = Vector3.Lerp(swayPos, Vector3.zero, _fixRatio);
            swayEulerRot = Vector3.Lerp(swayEulerRot, Vector3.zero, _fixRatio);
            bobPosition = Vector3.Lerp(bobPosition, Vector3.zero, _fixRatio);
            bobEulerRotation = Vector3.Lerp(bobEulerRotation, Vector3.zero, _fixRatio);
        });
    }

    void GetInput()
    {
        if (mover != null)
        {
            walkInput = mover.MoveValue;
        }

        Vector2 lookDelta = Vector2.zero;
        if (_player != null && _player.InputController != null)
        {
            lookDelta = _player.InputController.LookValue * 0.05f;
        }
        else if (Mouse.current != null)
        {
            lookDelta = Mouse.current.delta.ReadValue() * 0.05f;
        }

        lookInput.x = lookDelta.x;
        lookInput.y = -lookDelta.y;
    }

    void Sway()
    {
        Vector3 invertLook = lookInput * -step;
        invertLook.x = Mathf.Clamp(invertLook.x, -maxStepDistance, maxStepDistance);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxStepDistance, maxStepDistance);

        swayPos = invertLook;
    }

    void SwayRotation()
    {
        Vector2 invertLook = lookInput * -rotationStep;
        invertLook.x = Mathf.Clamp(invertLook.x, -maxRotationStep, maxRotationStep);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxRotationStep, maxRotationStep);
        swayEulerRot = new Vector3(invertLook.y, invertLook.x, invertLook.x);
    }

    void CompositePositionRotation()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, swayPos + bobPosition, Time.deltaTime * smooth);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(swayEulerRot) * Quaternion.Euler(bobEulerRotation), Time.deltaTime * smoothRot);
    }

    void BobOffset()
    {
        bool isGrounded = mover != null && mover.isGrounded;
        bool isRunning = mover != null && mover.IsRunning;

        speedCurve += Time.deltaTime * (isGrounded ? walkInput.magnitude * bobExaggeration * (isRunning ? 1.5f : 1f) : 1f);

        bobPosition.x = (curveCos * bobLimit.x * (isGrounded ? 1 : 0)) - (walkInput.x * travelLimit.x);
        bobPosition.y = (curveSin * bobLimit.y) - (walkInput.y * travelLimit.y);
        bobPosition.z = -(walkInput.y * travelLimit.z);
    }

    void BobRotation()
    {
        bobEulerRotation.x = walkInput != Vector2.zero ? multiplier.x * Mathf.Sin(2 * speedCurve) : multiplier.x * (Mathf.Sin(2 * speedCurve) / 2);
        bobEulerRotation.y = walkInput != Vector2.zero ? multiplier.y * curveCos : 0;
        bobEulerRotation.z = walkInput != Vector2.zero ? multiplier.z * curveCos * walkInput.x : 0;
    }
}