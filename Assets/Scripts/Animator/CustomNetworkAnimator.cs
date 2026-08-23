using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class CustomNetworkAnimator : NetworkBehaviour
{
    [Serializable]
    public struct AnimatorParameter
    {
        public int Id;
        public AnimatorControllerParameterType Type;
        public string Value;
    }

    public Animator Animator;
    private readonly SyncList<AnimatorParameter> _syncParameters = new();
    private readonly Dictionary<int, AnimatorParameter> _parameterDictionary = new();
    private AnimatorControllerParameter[] _parameters;
    private bool _isInitialized = false;
    private float _currentSpeed = 1f;

    public float Speed
    {
        get { return Animator != null ? Animator.speed : _currentSpeed; }
        set
        {
            _currentSpeed = value;
            if (Animator != null) Animator.speed = value;
            if (isServer)
                RpcChangeAnimatorSpeed(value);
            else if (isClient)
                CmdChangeAnimatorSpeed(value);
        }
    }

    [ClientRpc]
    private void RpcChangeAnimatorSpeed(float speed)
    {
        _currentSpeed = speed;
        if (isOwned) return;
        if (Animator != null) Animator.speed = speed;
    }

    [Command(requiresAuthority = false)]
    private void CmdChangeAnimatorSpeed(float speed)
    {
        RpcChangeAnimatorSpeed(speed);
    }

    private void Awake()
    {
        _parameterDictionary.Clear();
        if (Animator == null)
        {
            Animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        }
        if (Animator != null)
        {
            Animator.keepAnimatorStateOnDisable = true;
        }
    }

    /// <summary>
    /// 플레이어 모델링 교체 시 새 Animator를 바인딩하고 기존 파라미터 및 속도 값을 새 Animator에 일괄 세팅합니다.
    /// </summary>
    public void SetAnimator(Animator newAnimator)
    {
        Animator = newAnimator;
        if (Animator == null) return;

        Animator.keepAnimatorStateOnDisable = true;
        Animator.speed = _currentSpeed;

        InitializeParameters();
        ApplyAllParametersToAnimator();
    }

    /// <summary>
    /// 현재 캐시 및 동기화된 모든 파라미터 값을 Animator에 즉시 일괄 적용합니다.
    /// </summary>
    public void ApplyAllParametersToAnimator()
    {
        if (Animator == null) return;

        foreach (var kvp in _parameterDictionary)
        {
            SaveParameterValue(kvp.Value);
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (Animator == null) Animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        if (Animator == null) return;

        InitializeParameters();
        _isInitialized = true;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (Animator == null) Animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        if (Animator == null) return;

        _syncParameters.Callback += OnParametersChanged;
        InitializeParameters();
        _isInitialized = true;
    }

    private void InitializeParameters()
    {
        if (Animator == null || Animator.runtimeAnimatorController == null) return;
        _parameters = Animator.parameters;

        if (isServer && _syncParameters.Count == 0)
        {
            for (int i = 0; i < _parameters.Length; i++)
            {
                var p = new AnimatorParameter
                {
                    Id = _parameters[i].nameHash,
                    Type = _parameters[i].type,
                    Value = GetParameterDefaultValue(_parameters[i])
                };
                _syncParameters.Add(p);
                _parameterDictionary[p.Id] = p;
            }
        }
    }

    private string GetParameterDefaultValue(AnimatorControllerParameter parameter)
    {
        return parameter.type switch
        {
            AnimatorControllerParameterType.Bool => parameter.defaultBool.ToString(),
            AnimatorControllerParameterType.Float => parameter.defaultFloat.ToString(),
            AnimatorControllerParameterType.Int => parameter.defaultInt.ToString(),
            AnimatorControllerParameterType.Trigger => false.ToString(),
            _ => null,
        };
    }

    private AnimatorParameter GetParameter(int id)
    {
        if (_parameterDictionary.TryGetValue(id, out var parameter)) return parameter;
        foreach (var syncParameter in _syncParameters)
        {
            if (syncParameter.Id != id) continue;
            _parameterDictionary[id] = syncParameter;
            return syncParameter;
        }
        return new AnimatorParameter { Id = id };
    }

    private void OnParametersChanged(SyncList<AnimatorParameter>.Operation op, int index, AnimatorParameter oldItem, AnimatorParameter newItem)
    {
        if (isOwned) return;
        SaveParameterValue(newItem);
    }

    [ClientRpc]
    private void RpcSetParameterValue(int id, string value)
    {
        if (isOwned || !gameObject.activeSelf || !_isInitialized) return;
        SetParameterValue(id, value);
    }

    private void SetParameterValue(int id, string value)
    {
        var parameter = GetParameter(id);
        parameter.Value = value;
        SaveParameterValue(parameter);
    }

    private void SaveParameterValue(AnimatorParameter animatorParameter)
    {
        if (Animator == null) return;

        switch (animatorParameter.Type)
        {
            case AnimatorControllerParameterType.Bool:
                if (bool.TryParse(animatorParameter.Value, out var bVal))
                    Animator.SetBool(animatorParameter.Id, bVal);
                break;
            case AnimatorControllerParameterType.Float:
                if (float.TryParse(animatorParameter.Value, out var fVal))
                    Animator.SetFloat(animatorParameter.Id, fVal);
                break;
            case AnimatorControllerParameterType.Int:
                if (int.TryParse(animatorParameter.Value, out var iVal))
                    Animator.SetInteger(animatorParameter.Id, iVal);
                break;
            case AnimatorControllerParameterType.Trigger:
                if (bool.TryParse(animatorParameter.Value, out var tVal) && tVal)
                    Animator.SetTrigger(animatorParameter.Id);
                break;
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdChangeValue(int id, string value)
    {
        RpcSetParameterValue(id, value);
    }

    public void SetBool(int id, bool value)
    {
        if (Animator != null) Animator.SetBool(id, value);
        SetParameterValue(id, value.ToString());
        if (isServer) RpcSetParameterValue(id, value.ToString());
        else CmdChangeValue(id, value.ToString());
    }

    public void SetBool(string name, bool value)
    {
        SetBool(Animator.StringToHash(name), value);
    }

    public void SetFloat(int id, float value)
    {
        if (Animator != null) Animator.SetFloat(id, value);
        SetParameterValue(id, value.ToString());
        if (isServer) RpcSetParameterValue(id, value.ToString());
        else CmdChangeValue(id, value.ToString());
    }

    public void SetFloat(string name, float value)
    {
        SetFloat(Animator.StringToHash(name), value);
    }

    public void SetInteger(int id, int value)
    {
        if (Animator != null) Animator.SetInteger(id, value);
        SetParameterValue(id, value.ToString());
        if (isServer) RpcSetParameterValue(id, value.ToString());
        else CmdChangeValue(id, value.ToString());
    }

    public void SetInteger(string name, int value)
    {
        SetInteger(Animator.StringToHash(name), value);
    }

    public void SetTrigger(int id)
    {
        if (Animator != null) Animator.SetTrigger(id);
        if (isServer) RpcSetTrigger(id);
        else CmdSetTrigger(id);
    }

    public void SetTrigger(string name)
    {
        SetTrigger(Animator.StringToHash(name));
    }

    [ClientRpc]
    private void RpcSetTrigger(int id)
    {
        if (isOwned || Animator == null) return;
        Animator.SetTrigger(id);
    }

    [Command(requiresAuthority = false)]
    private void CmdSetTrigger(int id)
    {
        RpcSetTrigger(id);
    }

    public void ResetTrigger(int id)
    {
        if (Animator != null) Animator.ResetTrigger(id);
        if (isServer) RpcResetTrigger(id);
        else CmdResetTrigger(id);
    }

    public void ResetTrigger(string name)
    {
        ResetTrigger(Animator.StringToHash(name));
    }

    [ClientRpc]
    private void RpcResetTrigger(int id)
    {
        if (isOwned || Animator == null) return;
        Animator.ResetTrigger(id);
    }

    [Command(requiresAuthority = false)]
    private void CmdResetTrigger(int id)
    {
        RpcResetTrigger(id);
    }

    public void SetLookAtPosition(Vector3 position)
    {
        if (Animator != null) Animator.SetLookAtPosition(position);
    }

    public void SetLookAtWeight(float weight, float bodyWeight = 0f, float headWeight = 1f, float eyesWeight = 0f, float clampWeight = 0.5f)
    {
        if (Animator != null) Animator.SetLookAtWeight(weight, bodyWeight, headWeight, eyesWeight, clampWeight);
    }
}
