using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerHandyType
{
    Left,
    Right,
    Both,
}

/// <summary>
/// 핸디 아이템 오브젝트가 부착되는 시점/역할 분류
/// </summary>
public enum HandyAttachMode
{
    /// <summary>
    /// 로컬 플레이어 1인칭 손: UI 조작, 메인 조명, 1인칭 전용 이펙트/오디오
    /// </summary>
    FirstPerson,

    /// <summary>
    /// 원격 플레이어 3인칭 캐릭터 몸체: 타인 시점 외형 렌더링, 외부용 조명/VFX
    /// </summary>
    ThirdPerson,

    /// <summary>
    /// 로컬 플레이어 3인칭 캐릭터 몸체: 바닥 그림자 전용(ShadowsOnly), 조명/UI/오디오 비활성화
    /// </summary>
    ShadowOnly,
}

[PrefabLabel("HandyObject")]
public class HandyItemObject : MonoBehaviour
{
    [HideInInspector]
    public Item Item;
    public PlayerHandyType PlayerHandyType;
    public Vector3 HandOffset;
    public Vector3 HandRotation;
    public Vector3 BodyOffset;
    public Vector3 BodyRotation;
    public AnimatorOverrideController HandAnimatorOverrideController;
    public AnimatorOverrideController BodyAnimatorOverrideController;

    private Action<Player> _onHandyItemObjectSpawnedEvent;
    private Player _player;
    public Player Player => _player;

    public HandyAttachMode CurrentAttachMode { get; private set; } = HandyAttachMode.FirstPerson;

    public event Action<Player> OnHandyItemObjectSpawnedEvent
    {
        add
        {
            if (_player != null)
                value?.Invoke(_player);
            _onHandyItemObjectSpawnedEvent += value;
        }
        remove
        {
            _onHandyItemObjectSpawnedEvent -= value;
        }
    }

    private Vector3 _initialLocalScale = Vector3.one;

    private void Awake()
    {
        _initialLocalScale = transform.localScale;
        DisableShadowCasting();
    }

    public virtual void OnSpawned(Player player)
    {
        OnSpawned(player, HandyAttachMode.FirstPerson);
    }

    public virtual void OnSpawned(Player player, HandyAttachMode mode)
    {
        _player = player;
        CurrentAttachMode = mode;
        transform.localScale = _initialLocalScale;

        switch (mode)
        {
            case HandyAttachMode.FirstPerson:
                DisableShadowCasting();
                OnSetupFirstPerson(player);
                break;
            case HandyAttachMode.ThirdPerson:
                OnSetupThirdPerson(player);
                break;
            case HandyAttachMode.ShadowOnly:
                SetShadowOnly();
                OnSetupShadowOnly(player);
                break;
        }

        _onHandyItemObjectSpawnedEvent?.Invoke(player);
    }

    /// <summary>
    /// 1인칭 손 소켓에 부착되었을 때의 고유 초기화 로직 (UI 활성화, 메인 조명 등)
    /// </summary>
    protected virtual void OnSetupFirstPerson(Player player) { }

    /// <summary>
    /// 원격 플레이어 3인칭 몸체에 부착되었을 때의 고유 초기화 로직 (외부 렌더링, 외부 조명 등)
    /// </summary>
    protected virtual void OnSetupThirdPerson(Player player) { }

    /// <summary>
    /// 로컬 플레이어 그림자용 3인칭 몸체에 부착되었을 때의 고유 초기화 로직 (조명/UI 끄기 등)
    /// </summary>
    protected virtual void OnSetupShadowOnly(Player player) { }

    public void DisableShadowCasting()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r != null)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.allowOcclusionWhenDynamic = false;
                if (r is SkinnedMeshRenderer smr)
                {
                    smr.updateWhenOffscreen = true;
                }
            }
        }
    }

    public void SetShadowOnly()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r != null)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                r.allowOcclusionWhenDynamic = false;
                if (r is SkinnedMeshRenderer smr)
                {
                    smr.updateWhenOffscreen = true;
                }
            }
        }
    }

    /// <summary>
    /// 대상 부모 소켓에 부착하며, 부모 본의 lossyScale에 영향을 받지 않고 프리팹 원본 월드 크기를 유지하도록 스케일을 보정합니다.
    /// </summary>
    public void AttachToSocket(Transform parentSocket, Vector3 localOffset, Vector3 localRotation)
    {
        if (parentSocket == null) return;

        transform.SetParent(parentSocket, false);
        transform.localPosition = localOffset;
        transform.localRotation = Quaternion.Euler(localRotation);

        // 부모의 lossyScale 역보정을 통해 실제 보이는 크기를 _initialLocalScale로 완벽 고정
        Vector3 parentLossy = parentSocket.lossyScale;
        transform.localScale = new Vector3(
            Mathf.Abs(parentLossy.x) > 0.0001f ? _initialLocalScale.x / parentLossy.x : _initialLocalScale.x,
            Mathf.Abs(parentLossy.y) > 0.0001f ? _initialLocalScale.y / parentLossy.y : _initialLocalScale.y,
            Mathf.Abs(parentLossy.z) > 0.0001f ? _initialLocalScale.z / parentLossy.z : _initialLocalScale.z
        );
    }

    public virtual void OnAnimationTriggerEvent(int triggerID)
    {

    }
}