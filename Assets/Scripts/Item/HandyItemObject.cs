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

    private void Awake()
    {
        DisableShadowCasting();
    }

    public virtual void OnSpawned(Player player)
    {
        _player = player;
        DisableShadowCasting();
        _onHandyItemObjectSpawnedEvent?.Invoke(player);
    }

    public void DisableShadowCasting()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r != null)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }

    public virtual void OnAnimationTriggerEvent(int triggerID)
    {

    }
}