using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

    Vector2 walkInput;
    Vector2 lookInput;

    void Awake()
    {
        if (mover == null) mover = GetComponentInParent<PlayerMove>();
    }

    void Update()
    {
        GetInput();

        Sway();
        SwayRotation();
        BobOffset();
        BobRotation();

        CompositePositionRotation();
    }

    void GetInput()
    {
        walkInput = Vector2.zero;
        lookInput = Vector2.zero;

        // Try mover InputActions first
        if (mover != null && mover.moveAction != null && mover.lookAction != null)
        {
            walkInput = mover.moveAction.ReadValue<Vector2>();
            lookInput = mover.lookAction.ReadValue<Vector2>() * 0.05f;
        }
        else
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) walkInput.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) walkInput.y -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) walkInput.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) walkInput.x += 1f;
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                lookInput = mouse.delta.ReadValue() * 0.05f;
            }
        }

        walkInput = walkInput.normalized;
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
        bool isRunning = mover != null && mover.isRunning;

        speedCurve += Time.deltaTime * (isGrounded ? walkInput.magnitude * bobExaggeration * (isRunning ? 1.5f : 1f) : 1f);

        bobPosition.x = (curveCos * bobLimit.x * (isGrounded ? 1 : 0)) - (walkInput.x * travelLimit.x);
        bobPosition.y = (curveSin * bobLimit.y) - (walkInput.y * travelLimit.y);
        bobPosition.z = -(walkInput.y * travelLimit.z);
    }

    void BobRotation()
    {
        bool isGrounded = mover != null && mover.isGrounded;
        bobEulerRotation.x = (walkInput != Vector2.zero ? multiplier.x * (Mathf.Sin(2 * speedCurve)) : multiplier.x * (Mathf.Sin(2 * speedCurve) / 2));
        bobEulerRotation.y = (walkInput != Vector2.zero ? multiplier.y * curveCos : 0);
        bobEulerRotation.z = (walkInput != Vector2.zero ? multiplier.z * curveCos * walkInput.x : 0);
    }
}