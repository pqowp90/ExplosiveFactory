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
    private Animator _legAnimator;
    private AnimationTriggerEventHolder _animationTriggerEventHolder;
    private Player _player;
    private RuntimeAnimatorController _defaultHandController;
    private RuntimeAnimatorController _defaultBodyController;
    public SwayNBobScript SwayNBobScript;

    private void Awake()
    {
        _player = GetComponent<Player>();
        if (_player != null && _player.PlayerBodyTransform != null)
        {
            _bodyCustomNetworkAnimator = _player.PlayerBodyTransform.GetComponentInChildren<CustomNetworkAnimator>();
            if (_bodyCustomNetworkAnimator != null && _bodyCustomNetworkAnimator.Animator != null)
            {
                _defaultBodyController = _bodyCustomNetworkAnimator.Animator.runtimeAnimatorController;
            }
        }
        if (_player != null && _player.PlayerLegTransform != null)
        {
            _legAnimator = _player.PlayerLegTransform.GetComponentInChildren<Animator>();
            if (_legAnimator != null)
            {
                _legAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }
        if (_player != null && _player.PlayerHandTransform != null)
        {
            _handAnimator = _player.PlayerHandTransform.GetComponent<Animator>();
            if (_handAnimator != null)
            {
                _defaultHandController = _handAnimator.runtimeAnimatorController;
                _handAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                _animationTriggerEventHolder = _handAnimator.GetComponent<AnimationTriggerEventHolder>();
                if (_animationTriggerEventHolder != null)
                {
                    _animationTriggerEventHolder.SetOnAnimationTriggerEvent(CmdTriggerEvent);
                }
            }

            // 1인칭 손 메시 렌더러가 카메라 뷰 프러스텀 밖으로 나가도 애니메이션 및 렌더링이 중단되지 않도록 보장
            var skinnedRenderers = _player.PlayerHandTransform.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in skinnedRenderers)
            {
                smr.updateWhenOffscreen = true;
            }
        }
        SwayNBobScript = GetComponentInChildren<SwayNBobScript>();
    }

    private void Start()
    {
        if (_legAnimator == null && _player != null && _player.PlayerLegTransform != null)
        {
            _legAnimator = _player.PlayerLegTransform.GetComponentInChildren<Animator>();
            if (_legAnimator != null)
            {
                _legAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }

        if (_handAnimator != null)
        {
            _handAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
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
        if (_legAnimator != null) _legAnimator.SetFloat(RayDistHash, rayDist);
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
        if (_legAnimator != null)
            _legAnimator.SetFloat(IMovementAnimation.Crouch, _crouch);

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
        if (_legAnimator != null)
            _legAnimator.SetFloat(IMovementAnimation.Run, _HandRun);

        if (isOwned && _handAnimator != null)
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
        if (_legAnimator != null)
        {
            _legAnimator.SetFloat(TurnHash, _currentTurn);
        }
    }
    private float _crouch = 0;
    private float _HandRun = 0;
    private float _run = 0;
    // ------------------------------------------------- //
    private bool _isCrouching = false;
    public void SetCrouch(bool crouch)
    {
        if (_isCrouching == crouch) return;
        _isCrouching = crouch;
        CmdSetCrouch(crouch);
    }
    [Command(requiresAuthority = false)]
    private void CmdSetCrouch(bool crouch)
    {
        _isCrouching = crouch;
        RpcSetCrouch(crouch);
    }
    [ClientRpc]
    private void RpcSetCrouch(bool crouch)
    {
        if (isOwned) return;
        _isCrouching = crouch;
    }
    // ------------------------------------------------- //
    private bool _isRunning = false;
    public void SetRun(bool run)
    {
        if (_isRunning == run) return;
        _isRunning = run;
        CmdSetRun(run);
    }
    [Command(requiresAuthority = false)]
    private void CmdSetRun(bool run)
    {
        _isRunning = run;
        RpcSetRun(run);
    }
    [ClientRpc]
    private void RpcSetRun(bool run)
    {
        if (isOwned) return;
        _isRunning = run;
    }
    // ------------------------------------------------- //
    public void SetGrounded(bool grounded)
    {
        if (_bodyCustomNetworkAnimator != null) _bodyCustomNetworkAnimator.SetBool(IMovementAnimation.Grounded, grounded);
        if (_handAnimator != null) _handAnimator.SetBool(IMovementAnimation.Grounded, grounded);
        if (_legAnimator != null) _legAnimator.SetBool(IMovementAnimation.Grounded, grounded);
    }

    public void SetJump()
    {
        if (_bodyCustomNetworkAnimator != null) _bodyCustomNetworkAnimator.SetTrigger(IMovementAnimation.Jump);
        if (_handAnimator != null) _handAnimator.SetTrigger(IMovementAnimation.Jump);
        if (_legAnimator != null) _legAnimator.SetTrigger(IMovementAnimation.Jump);
    }
    public void ResetJump()
    {
        if (_bodyCustomNetworkAnimator != null) _bodyCustomNetworkAnimator.ResetTrigger(IMovementAnimation.Jump);
        if (_handAnimator != null) _handAnimator.ResetTrigger(IMovementAnimation.Jump);
        if (_legAnimator != null) _legAnimator.ResetTrigger(IMovementAnimation.Jump);
    }
    private List<Vector2> _latestMoves = new List<Vector2>();
    private Vector2 _moveValue;
    private void FixedUpdate()
    {
        if (!isOwned)
        {
            if (_bodyCustomNetworkAnimator != null && _bodyCustomNetworkAnimator.Animator != null)
            {
                _moveValue = new Vector2(_bodyCustomNetworkAnimator.Animator.GetFloat(IMovementAnimation.MoveX),
                    _bodyCustomNetworkAnimator.Animator.GetFloat(IMovementAnimation.MoveY));
            }
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
        if (_bodyCustomNetworkAnimator != null)
        {
            _bodyCustomNetworkAnimator.SetBool(IsMovingHash, Mathf.Abs(move.x) + Mathf.Abs(move.y) > 0.1f);
            _bodyCustomNetworkAnimator.SetFloat(IMovementAnimation.MoveX, move.x);
            _bodyCustomNetworkAnimator.SetFloat(IMovementAnimation.MoveY, move.y);
            _bodyCustomNetworkAnimator.SetFloat(IMovementAnimation.MoveZ, move.z);
        }
        if (_handAnimator != null)
        {
            _handAnimator.SetBool(IsMovingHash, Mathf.Abs(move.x) + Mathf.Abs(move.y) > 0.1f);
            _handAnimator.SetFloat(IMovementAnimation.MoveX, move.x);
            _handAnimator.SetFloat(IMovementAnimation.MoveY, move.y);
            _handAnimator.SetFloat(IMovementAnimation.MoveZ, move.z);
        }
        if (_legAnimator != null)
        {
            _legAnimator.SetBool(IsMovingHash, Mathf.Abs(move.x) + Mathf.Abs(move.y) > 0.1f);
            _legAnimator.SetFloat(IMovementAnimation.MoveX, move.x);
            _legAnimator.SetFloat(IMovementAnimation.MoveY, move.y);
            _legAnimator.SetFloat(IMovementAnimation.MoveZ, move.z);
        }

        _moveValue = new Vector2(move.x, move.y);
    }


    public void SetSpeed(float speed)
    {
        if (_bodyCustomNetworkAnimator != null) _bodyCustomNetworkAnimator.SetFloat(IMovementAnimation.Speed, speed);
        if (_handAnimator != null) _handAnimator.SetFloat(IMovementAnimation.Speed, speed);
        if (_legAnimator != null) _legAnimator.SetFloat(IMovementAnimation.Speed, speed);
    }

    public void SetSprint(bool sprint)
    {
        if (_bodyCustomNetworkAnimator != null) _bodyCustomNetworkAnimator.SetBool(IMovementAnimation.Sprint, sprint);
        if (_handAnimator != null) _handAnimator.SetBool(IMovementAnimation.Sprint, sprint);
        if (_legAnimator != null) _legAnimator.SetBool(IMovementAnimation.Sprint, sprint);
    }
    private bool _isHoldingItem = false;
    public bool IsHoldingItem => _isHoldingItem;
    public void SetHoldingItem(bool holdingItem)
    {
        _isHoldingItem = holdingItem;
        if (NetworkServer.active)
        {
            RpcSetHoldingItem(holdingItem);
        }
        else
        {
            CmdSetHoldingItem(holdingItem);
        }
    }
    [Command(requiresAuthority = false)]
    private void CmdSetHoldingItem(bool holdingItem)
    {
        _isHoldingItem = holdingItem;
        RpcSetHoldingItem(holdingItem);
    }
    [ClientRpc]
    private void RpcSetHoldingItem(bool holdingItem)
    {
        if (isOwned) return;
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
        if (isOwned && _handAnimator != null)
        {
            var targetHandController = handController != null ? handController : _defaultHandController;
            if (targetHandController != null && _handAnimator.runtimeAnimatorController != targetHandController)
            {
                _handAnimator.runtimeAnimatorController = targetHandController;
            }
            if (handController != null)
            {
                _handAnimator.SetTrigger(EquipHash);
            }
            else
            {
                _handAnimator.SetTrigger(UnequalHash);
            }
        }

        if (_bodyCustomNetworkAnimator != null && _bodyCustomNetworkAnimator.Animator != null)
        {
            var targetBodyController = bodyController != null ? bodyController : _defaultBodyController;
            if (targetBodyController != null && _bodyCustomNetworkAnimator.Animator.runtimeAnimatorController != targetBodyController)
            {
                _bodyCustomNetworkAnimator.Animator.runtimeAnimatorController = targetBodyController;
            }
            if (bodyController != null)
            {
                _bodyCustomNetworkAnimator.Animator.SetTrigger(EquipHash);
            }
            else
            {
                _bodyCustomNetworkAnimator.Animator.SetTrigger(UnequalHash);
            }
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