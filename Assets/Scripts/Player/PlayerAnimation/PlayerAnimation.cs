using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class PlayerAnimation : NetworkBehaviour, IMovementAnimation
{
    private static readonly int RayDistHash = Animator.StringToHash("RayDist");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int EquipHash = Animator.StringToHash("Equip");
    private static readonly int UnequalHash = Animator.StringToHash("Unequip");
    private static readonly int UseHash = Animator.StringToHash("Use");
    private static readonly int Use2Hash = Animator.StringToHash("Use2");
    private static readonly int HoldableItemHash = Animator.StringToHash("HoldableItem");
    private static readonly int TurnHash = Animator.StringToHash("Turn");
    private static readonly int TurnTriggerHash = Animator.StringToHash("TurnTrigger");
    private static readonly int HoldItemHash = Animator.StringToHash("HoldItem");


    private CustomNetworkAnimator _bodyCustomNetworkAnimator;
    private Animator _handAnimator;
    private AnimationTriggerEventHolder _animationTriggerEventHolder;
    private Player _player;
    public SwayNBobScript SwayNBobScript;

    private void Awake()
    {
        _player = GetComponent<Player>();
        if (_player != null && _player.PlayerBodyTransform != null)
            _bodyCustomNetworkAnimator = _player.PlayerBodyTransform.GetComponentInChildren<CustomNetworkAnimator>();
        if (_player != null && _player.PlayerHandTransform != null)
        {
            _handAnimator = _player.PlayerHandTransform.GetComponent<Animator>();
            if (_handAnimator != null)
            {
                _animationTriggerEventHolder = _handAnimator.GetComponent<AnimationTriggerEventHolder>();
                if (_animationTriggerEventHolder != null)
                {
                    _animationTriggerEventHolder.SetOnAnimationTriggerEvent(CmdTriggerEvent);
                }
            }
        }
        SwayNBobScript = GetComponentInChildren<SwayNBobScript>();
    }
    [Command(requiresAuthority = false)]
    private void CmdTriggerEvent(int triggerID)
    {
        RpcTriggerEvent(triggerID);
    }
    [ClientRpc]
    private void RpcTriggerEvent(int triggerID)
    {
        if (_player != null && _player.ItemHolder != null)
        {
            if (_player.ItemHolder.CurrentHandyItemObject != null)
            {
                _player.ItemHolder.CurrentHandyItemObject.OnAnimationTriggerEvent(triggerID);
            }
            else if (_player.ItemHolder.HoldingItem != null && _player.ItemHolder.HoldingItem.HandyItemObject != null)
            {
                _player.ItemHolder.HoldingItem.HandyItemObject.OnAnimationTriggerEvent(triggerID);
            }
        }
    }

    public void SetRayDist(float rayDist)
    {
        if (_bodyCustomNetworkAnimator != null) _bodyCustomNetworkAnimator.SetFloat(RayDistHash, rayDist);
        if (_handAnimator != null) _handAnimator.SetFloat(RayDistHash, rayDist);
    }
    public void SetItem(object handyItemObject)
    {

    }
    private void Update()
    {
        _crouch = Mathf.Lerp(_crouch, _isCrouching ? 1f : 0f, Time.deltaTime * 9f);
        if (_bodyCustomNetworkAnimator != null && _bodyCustomNetworkAnimator.Animator != null)
            _bodyCustomNetworkAnimator.Animator.SetFloat(IMovementAnimation.Crouch, _crouch);
        if (_handAnimator != null)
            _handAnimator.SetFloat(IMovementAnimation.Crouch, _crouch);

        if (_isCrouching)
        {
            _HandRun = 0;
            _run = 0;
        }
        else
        {
            _HandRun = Mathf.Lerp(_HandRun, _isRunning ? 1f : 0f, Time.deltaTime * 6f * (_isRunning ? 1f : 3f)) * _moveValue.magnitude;
        }

        if (_bodyCustomNetworkAnimator != null && _bodyCustomNetworkAnimator.Animator != null)
            _bodyCustomNetworkAnimator.Animator.SetFloat(IMovementAnimation.Run, _HandRun);
        if (_handAnimator != null)
            _handAnimator.SetFloat(IMovementAnimation.Run, _HandRun);

        if (_handAnimator != null)
        {
            _handAnimator.SetLayerWeight(1, _isHoldingItem ? 1f : 0f);
            _handAnimator.SetBool(HoldItemHash, _isHoldingItem);
        }
        if (_bodyCustomNetworkAnimator != null && _bodyCustomNetworkAnimator.Animator != null)
        {
            _bodyCustomNetworkAnimator.Animator.SetLayerWeight(1, _isHoldingItem ? 1f : 0f);
            _currentTurn = Mathf.Lerp(_currentTurn, _turn, Time.deltaTime * 20f);
            _bodyCustomNetworkAnimator.Animator.SetFloat(TurnHash, _currentTurn);
        }
    }
    private float _crouch = 0;
    private float _HandRun = 0;
    private float _run = 0;
    // ------------------------------------------------- //
    [SyncVar]
    private bool _isCrouching = false;
    public void SetCrouch(bool crouch)
    {
        if (_isCrouching == crouch) return;
        CmdSetCrouch(crouch);
    }
    [Command]
    private void CmdSetCrouch(bool crouch)
    {
        _isCrouching = crouch;
    }
    // ------------------------------------------------- //
    [SyncVar]
    private bool _isRunning = false;
    public void SetRun(bool run)
    {
        if (_isRunning == run) return;
        CmdSetRun(run);
    }
    [Command]
    private void CmdSetRun(bool run)
    {
        _isRunning = run;
    }
    // ------------------------------------------------- //
    public void SetGrounded(bool grounded)
    {
        _bodyCustomNetworkAnimator.SetBool(IMovementAnimation.Grounded, grounded);
        _handAnimator.SetBool(IMovementAnimation.Grounded, grounded);
    }

    public void SetJump()
    {
        _bodyCustomNetworkAnimator.SetTrigger(IMovementAnimation.Jump);
        _handAnimator.SetTrigger(IMovementAnimation.Jump);
    }
    public void ResetJump()
    {
        _bodyCustomNetworkAnimator.ResetTrigger(IMovementAnimation.Jump);
        _handAnimator.ResetTrigger(IMovementAnimation.Jump);
    }
    private List<Vector2> _latestMoves = new List<Vector2>();
    private Vector2 _moveValue;
    private void FixedUpdate()
    {
        if (!isOwned)
        {
            _moveValue = new Vector2(_bodyCustomNetworkAnimator.Animator.GetFloat(IMovementAnimation.MoveX),
                _bodyCustomNetworkAnimator.Animator.GetFloat(IMovementAnimation.MoveY));
        }
        if (_latestMoves.Count > 10)
            _latestMoves.RemoveAt(0);
        _latestMoves.Add(_moveValue);
        Vector2 move = Vector2.zero;
        foreach (var latestMove in _latestMoves)
        {
            move += latestMove;
        }
        move /= _latestMoves.Count;
        _moveValue = move;
    }
    public bool IsMoving => _moveValue.magnitude > 0.1f;
    public void SetMove(Vector3 move)
    {
        _bodyCustomNetworkAnimator.SetBool(IsMovingHash, Mathf.Abs(move.x) + Mathf.Abs(move.y) > 0.1f);
        _bodyCustomNetworkAnimator.SetFloat(IMovementAnimation.MoveX, move.x);
        _bodyCustomNetworkAnimator.SetFloat(IMovementAnimation.MoveY, move.y);
        _bodyCustomNetworkAnimator.SetFloat(IMovementAnimation.MoveZ, move.z);
        _handAnimator.SetBool(IsMovingHash, Mathf.Abs(move.x) + Mathf.Abs(move.y) > 0.1f);
        _handAnimator.SetFloat(IMovementAnimation.MoveX, move.x);
        _handAnimator.SetFloat(IMovementAnimation.MoveY, move.y);
        _handAnimator.SetFloat(IMovementAnimation.MoveZ, move.z);

        _moveValue = new Vector2(move.x, move.y);
    }


    public void SetSpeed(float speed)
    {
        _bodyCustomNetworkAnimator.SetFloat(IMovementAnimation.Speed, speed);
        _handAnimator.SetFloat(IMovementAnimation.Speed, speed);
    }

    public void SetSprint(bool sprint)
    {
        _bodyCustomNetworkAnimator.SetBool(IMovementAnimation.Sprint, sprint);
        _handAnimator.SetBool(IMovementAnimation.Sprint, sprint);
    }
    [SyncVar]
    private bool _isHoldingItem = false;
    public void SetHoldingItem(bool holdingItem)
    {
        _isHoldingItem = holdingItem;
    }
    public void SetAnimatorController(HandyItemObject handyItemObject)
    {
        if (handyItemObject == null)
        {
            SetAnimatorController(null, null);
            return;
        }
        SetAnimatorController(handyItemObject.HandAnimatorOverrideController, handyItemObject.BodyAnimatorOverrideController);
    }

    public void SetAnimatorController(RuntimeAnimatorController handController = null, RuntimeAnimatorController bodyController = null)
    {
        if (isOwned && handController != null && _handAnimator != null)
        {
            _handAnimator.runtimeAnimatorController = handController;
        }
        else if (bodyController != null && _bodyCustomNetworkAnimator != null)
        {
            _bodyCustomNetworkAnimator.Animator.runtimeAnimatorController = bodyController;
        }
        if (_handAnimator != null) _handAnimator.SetTrigger(EquipHash);
        if (_bodyCustomNetworkAnimator != null && _bodyCustomNetworkAnimator.Animator != null)
        {
            _bodyCustomNetworkAnimator.Animator.SetTrigger(EquipHash);
        }
    }
    public void SetTurn(int v)
    {
        _turn = v;
    }

    public void UseItem(int index = 0)
    {
        Debug.Log(index);
        _handAnimator.ResetTrigger(UseHash);
        _handAnimator.ResetTrigger(Use2Hash);
        switch (index)
        {
            case 0:
                _handAnimator.SetTrigger(UseHash);
                break;
            case 1:
                _handAnimator.SetTrigger(Use2Hash);
                break;
        }
    }
    public void SetHoldableItem(bool holdable)
    {
        _handAnimator.SetBool(HoldableItemHash, holdable);
    }

    public void ResetHandyAnimation()
    {
        _handAnimator.ResetTrigger(UseHash);
        _handAnimator.ResetTrigger(Use2Hash);
        _handAnimator.SetBool(HoldableItemHash, false);
    }

    private int _turn = 0;
    private float _currentTurn = 0f;
}