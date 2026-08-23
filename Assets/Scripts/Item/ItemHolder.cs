using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ItemHolder : NetworkBehaviour
{
    [Serializable]
    public class ItemHandyTypeTransform
    {
        public PlayerHandyType PlayerHandyType;
        public Transform HandyTransform;
        public Transform BodyTransform;
    }

    private readonly Dictionary<PlayerHandyType, Transform> _handyTypeTransforms = new();
    private readonly Dictionary<PlayerHandyType, Transform> _bodyTypeTransforms = new();

    [SerializeField]
    private List<ItemHandyTypeTransform> _itemHandyTypeTransforms = new();

    [SerializeField]
    private Transform _itemDropPoint;

    [SerializeField]
    private int _maxHandyItemIndex = 3;

    private Player _player;
    public Player Player => _player ??= GetComponent<Player>();

    private int _currentHandyItemIndex = 0;
    private readonly List<Item?> _holdingItems = new();

    private HandyItemObject _currentHandyItemObject;
    public HandyItemObject CurrentHandyItemObject => _currentHandyItemObject ?? _currentBodyHandyItemObject;

    private HandyItemObject _currentBodyHandyItemObject;
    public HandyItemObject CurrentBodyHandyItemObject => _currentBodyHandyItemObject;

    private void Awake()
    {
        _player = GetComponent<Player>();
        if (_itemDropPoint == null)
            _itemDropPoint = transform;

        GetHandyTransformByHandyType();
        if (_player != null && _player.PlayerBodyTransform != null)
        {
            RebindBodySockets(_player.PlayerBodyTransform);
        }
        EnsureHoldingItemsCapacity();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        _holdingItems.Clear();
        for (int i = 0; i < _maxHandyItemIndex; i++)
        {
            _holdingItems.Add(null);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        EnsureHoldingItemsCapacity();
        UpdateHandyObject();
        CmdRequestHolderState();
    }

    [Command(requiresAuthority = false)]
    private void CmdRequestHolderState(NetworkConnectionToClient? conn = null)
    {
        if (conn != null)
        {
            EnsureHoldingItemsCapacity();
            TargetSyncHolderState(conn, _currentHandyItemIndex, _holdingItems.ToArray());
        }
    }

    [TargetRpc]
    private void TargetSyncHolderState(NetworkConnection target, int currentIndex, Item?[] items)
    {
        _currentHandyItemIndex = currentIndex;
        _holdingItems.Clear();
        if (items != null)
        {
            _holdingItems.AddRange(items);
        }
        EnsureHoldingItemsCapacity();

        UpdateHandyObject();
        OnCurrentSlotChanged?.Invoke(currentIndex);
        OnAllSlotsUpdated?.Invoke();

        if (_player != null && _player.PlayerAnimation != null)
        {
            _player.PlayerAnimation.SetHoldingItem(HoldingItem != null);
        }
    }

    public event Action<int>? OnCurrentSlotChanged;
    public event Action<int, Item?>? OnSlotItemChanged;
    public event Action? OnAllSlotsUpdated;

    public int MaxSlotCount => _maxHandyItemIndex;

    public Item? GetItemAtSlot(int index)
    {
        if (index >= 0 && index < _holdingItems.Count)
            return _holdingItems[index];
        return null;
    }

    private void Update()
    {
        if (!isLocalPlayer || _player == null || _player.InputController == null) return;

        // 슬롯 변경 (마우스 휠 스크롤)
        float scroll = _player.InputController.ScrollValue;
        if (scroll > 0.01f && _currentHandyItemIndex > 0)
        {
            CmdChangeHandyItemIndex(-1);
        }
        else if (scroll < -0.01f && _currentHandyItemIndex < _maxHandyItemIndex - 1)
        {
            CmdChangeHandyItemIndex(1);
        }

        // 아이템 사용 (우클릭 - 게임플레이 모드 또는 스마트폰 UI 뒤로가기)
        if (_player.InputController.IsUseSecondaryTriggered)
        {
            CmdUseItem();
        }
        else if (CursorManager.Instance != null && CursorManager.Instance.CurrentCursor == CursorType.UI)
        {
            if (_player.InputController.IsRawSecondaryClickTriggered)
            {
                CmdUseItem();
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdChangeHandyItemIndex(int delta)
    {
        if (_maxHandyItemIndex <= 0) return;

        int newIndex = Mathf.Clamp(_currentHandyItemIndex + delta, 0, _maxHandyItemIndex - 1);
        if (newIndex == _currentHandyItemIndex) return;

        _currentHandyItemIndex = newIndex;
        RpcSetHandyItemIndex(newIndex);
    }

    [ClientRpc]
    private void RpcSetHandyItemIndex(int newIndex)
    {
        _currentHandyItemIndex = newIndex;
        UpdateHandyObject();
        OnCurrentSlotChanged?.Invoke(newIndex);

        if (_player != null && _player.PlayerAnimation != null)
        {
            _player.PlayerAnimation.SetHoldingItem(HoldingItem != null);
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdUseItem()
    {
        if (HoldingItem != null)
        {
            HoldingItem.UseItem();
        }
    }

    public int CurrentHandyItemIndex
    {
        get => _currentHandyItemIndex;
        set
        {
            if (!NetworkServer.active) return;
            if (value < 0 || value >= _maxHandyItemIndex) return;

            _currentHandyItemIndex = value;
            RpcSetHandyItemIndex(value);
        }
    }

    public Item? HoldingItem
    {
        get
        {
            if (_currentHandyItemIndex >= 0 && _currentHandyItemIndex < _holdingItems.Count)
            {
                return _holdingItems[_currentHandyItemIndex];
            }
            return null;
        }
        set
        {
            if (!NetworkServer.active) return;
            SetSlotItemOnServer(_currentHandyItemIndex, value);
        }
    }

    [Server]
    private void SetSlotItemOnServer(int slotIndex, Item? newItem)
    {
        EnsureHoldingItemsCapacity();
        if (slotIndex < 0 || slotIndex >= _maxHandyItemIndex) return;

        var oldItem = _holdingItems[slotIndex];
        if (oldItem != null && oldItem != newItem)
        {
            Vector3 dropPos = _itemDropPoint != null ? _itemDropPoint.position : transform.position + transform.forward * 0.5f + Vector3.up * 0.5f;
            Quaternion dropRot = _itemDropPoint != null ? _itemDropPoint.rotation : transform.rotation;
            Vector3 dropVel = transform.forward * 2f + Vector3.up * 1f;
            oldItem.DropItem(dropPos, dropRot, dropVel);
        }

        _holdingItems[slotIndex] = newItem;

        if (newItem != null)
        {
            newItem.ItemHolder = this;
            newItem.PickUpItem(this);
        }

        RpcSetSlotItem(slotIndex, newItem);
    }

    [ClientRpc]
    private void RpcSetSlotItem(int slotIndex, Item? newItem)
    {
        EnsureHoldingItemsCapacity();
        if (slotIndex < 0 || slotIndex >= _holdingItems.Count) return;

        _holdingItems[slotIndex] = newItem;
        if (newItem != null)
        {
            newItem.ItemHolder = this;
        }

        if (slotIndex == _currentHandyItemIndex)
        {
            UpdateHandyObject();
            if (_player != null && _player.PlayerAnimation != null)
            {
                _player.PlayerAnimation.SetHoldingItem(newItem != null);
            }
        }

        OnSlotItemChanged?.Invoke(slotIndex, newItem);
        OnAllSlotsUpdated?.Invoke();
    }

    private void EnsureHoldingItemsCapacity()
    {
        while (_holdingItems.Count < _maxHandyItemIndex)
        {
            _holdingItems.Add(null);
        }
    }

    [Command(requiresAuthority = false)]
    public void DropItem()
    {
        if (HoldingItem == null) return;
        SetSlotItemOnServer(_currentHandyItemIndex, null);
    }

    [Command(requiresAuthority = false)]
    public void PickUpItem(Item item)
    {
        if (item == null || item.IsPickedUp) return;
        SetSlotItemOnServer(_currentHandyItemIndex, item);
    }

    private void UpdateHandyObject()
    {
        SetHandyObject(HoldingItem);
    }

    private void SetHandyObject(Item item)
    {
        if (_player != null && _player.PlayerAnimation != null)
        {
            _player.PlayerAnimation.ResetJump();
            _player.PlayerAnimation.ResetHandyAnimation();
        }

        // 기존 쥐고 있던 1인칭 및 3인칭 HandyObject 정리
        if (_currentHandyItemObject != null)
        {
            if (_currentHandyItemObject.Item != null)
            {
                _currentHandyItemObject.Item.OnHandyItemObjectDespawned();
            }
            PoolManager.Release(_currentHandyItemObject);
            _currentHandyItemObject = null;
        }

        if (_currentBodyHandyItemObject != null)
        {
            PoolManager.Release(_currentBodyHandyItemObject);
            _currentBodyHandyItemObject = null;
        }

        if (item == null || item.HandyItemObjectPrefab == null)
        {
            if (_player != null && _player.PlayerAnimation != null)
            {
                _player.PlayerAnimation.SetAnimatorController(null);
            }
            return;
        }

        if (isOwned)
        {
            // 1. 로컬 플레이어: 1인칭 손 소켓에 1인칭용 HandyItemObject 스폰 및 부착
            var handyItemObj = PoolManager.Get(item.HandyItemObjectPrefab);
            if (handyItemObj != null)
            {
                item.HandyItemObject = handyItemObj;
                _currentHandyItemObject = handyItemObj;
                _currentHandyItemObject.Item = item;
                _currentHandyItemObject.OnSpawned(_player, HandyAttachMode.FirstPerson);
                item.OnHandyItemObjectSpawned();

                if (_player != null && _player.PlayerAnimation != null)
                {
                    _player.PlayerAnimation.SetAnimatorController(handyItemObj);
                }

                if (_handyTypeTransforms.TryGetValue(handyItemObj.PlayerHandyType, out var handTransform) && handTransform != null)
                {
                    handyItemObj.AttachToSocket(handTransform, handyItemObj.HandOffset, handyItemObj.HandRotation);
                    SetLayerByParent(handyItemObj.transform);
                }
            }

            // 2. 로컬 플레이어: 3인칭 바디 소켓에도 3인칭용 HandyItemObject 스폰 및 부착 (그림자 전용 ShadowsOnly)
            var bodyItemObj = PoolManager.Get(item.HandyItemObjectPrefab);
            if (bodyItemObj != null)
            {
                _currentBodyHandyItemObject = bodyItemObj;
                _currentBodyHandyItemObject.Item = item;
                _currentBodyHandyItemObject.OnSpawned(_player, HandyAttachMode.ShadowOnly);

                if (_bodyTypeTransforms.TryGetValue(bodyItemObj.PlayerHandyType, out var bodyTransform) && bodyTransform != null)
                {
                    bodyItemObj.AttachToSocket(bodyTransform, bodyItemObj.BodyOffset, bodyItemObj.BodyRotation);
                    SetLayerAndShadow(bodyItemObj.transform, UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly);
                }
            }
        }
        else
        {
            // 원격 플레이어: 3인칭 바디 소켓에만 스폰 및 부착 (일반 렌더링 On)
            var bodyItemObj = PoolManager.Get(item.HandyItemObjectPrefab);
            if (bodyItemObj != null)
            {
                item.HandyItemObject = bodyItemObj;
                _currentBodyHandyItemObject = bodyItemObj;
                _currentBodyHandyItemObject.Item = item;
                _currentBodyHandyItemObject.OnSpawned(_player, HandyAttachMode.ThirdPerson);
                item.OnHandyItemObjectSpawned();

                if (_player != null && _player.PlayerAnimation != null)
                {
                    _player.PlayerAnimation.SetAnimatorController(bodyItemObj);
                }

                if (_bodyTypeTransforms.TryGetValue(bodyItemObj.PlayerHandyType, out var bodyTransform) && bodyTransform != null)
                {
                    bodyItemObj.AttachToSocket(bodyTransform, bodyItemObj.BodyOffset, bodyItemObj.BodyRotation);
                    SetLayerAndShadow(bodyItemObj.transform, UnityEngine.Rendering.ShadowCastingMode.On);
                }
            }
        }
    }

    /// <summary>
    /// 애니메이션 트리거 이벤트를 1인칭 및 3인칭 핸디 오브젝트 모두에 안전하게 전파합니다.
    /// </summary>
    public void TriggerAnimationEvent(int triggerID)
    {
        if (_currentHandyItemObject != null)
        {
            _currentHandyItemObject.OnAnimationTriggerEvent(triggerID);
        }
        if (_currentBodyHandyItemObject != null && _currentBodyHandyItemObject != _currentHandyItemObject)
        {
            _currentBodyHandyItemObject.OnAnimationTriggerEvent(triggerID);
        }
    }

    private void SetLayerByParent(Transform target)
    {
        SetLayerAndShadow(target, UnityEngine.Rendering.ShadowCastingMode.Off);
    }

    private void SetLayerAndShadow(Transform target, UnityEngine.Rendering.ShadowCastingMode shadowMode)
    {
        if (target == null || target.parent == null) return;
        var layer = target.parent.gameObject.layer;
        SetLayerRecursively(target, layer);

        var renderers = target.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r != null)
            {
                r.shadowCastingMode = shadowMode;
                r.allowOcclusionWhenDynamic = false;
                if (r is SkinnedMeshRenderer smr)
                {
                    smr.updateWhenOffscreen = true;
                }
            }
        }
    }

    private void SetLayerRecursively(Transform target, int layer)
    {
        if (target == null) return;
        target.gameObject.layer = layer;
        for (int i = 0; i < target.childCount; i++)
        {
            SetLayerRecursively(target.GetChild(i), layer);
        }
    }

    private void GetHandyTransformByHandyType()
    {
        _handyTypeTransforms.Clear();
        _bodyTypeTransforms.Clear();
        foreach (var itemHandyTypeTransform in _itemHandyTypeTransforms)
        {
            if (itemHandyTypeTransform != null)
            {
                if (itemHandyTypeTransform.HandyTransform != null)
                    _handyTypeTransforms[itemHandyTypeTransform.PlayerHandyType] = itemHandyTypeTransform.HandyTransform;
                if (itemHandyTypeTransform.BodyTransform != null)
                    _bodyTypeTransforms[itemHandyTypeTransform.PlayerHandyType] = itemHandyTypeTransform.BodyTransform;
            }
        }
    }

    /// <summary>
    /// 플레이어 모델링 교체 시 새 3인칭 모델의 오른손/왼손 본을 탐색하여 3인칭 소켓을 동적으로 재바인딩합니다.
    /// </summary>
    public void RebindBodySockets(Transform newBodyTransform)
    {
        if (newBodyTransform == null) return;

        Transform rightHand = null;
        Transform leftHand = null;

        // 1. 모델 루트의 CharacterModelSockets 컴포넌트로부터 직렬화된 소켓 취득 (최우선)
        var modelSockets = newBodyTransform.GetComponentInChildren<CharacterModelSockets>();
        if (modelSockets != null)
        {
            rightHand = modelSockets.RightHandSocket;
            leftHand = modelSockets.LeftHandSocket;
        }

        // 2. 소켓 컴포넌트가 없을 경우 휴머노이드 Animator 본으로 안전 폴백
        if (rightHand == null || leftHand == null)
        {
            var animator = newBodyTransform.GetComponentInChildren<Animator>();
            if (animator != null && animator.isHuman)
            {
                if (rightHand == null) rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (leftHand == null) leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            }
        }

        if (rightHand != null)
        {
            _bodyTypeTransforms[PlayerHandyType.Right] = rightHand;
            SetBodyTransformInList(PlayerHandyType.Right, rightHand);
        }
        if (leftHand != null)
        {
            _bodyTypeTransforms[PlayerHandyType.Left] = leftHand;
            SetBodyTransformInList(PlayerHandyType.Left, leftHand);
        }

        // 현재 들고 있는 3인칭 아이템이 있다면 새 소켓으로 재배치
        if (_currentBodyHandyItemObject != null)
        {
            if (_bodyTypeTransforms.TryGetValue(_currentBodyHandyItemObject.PlayerHandyType, out var bodyTransform) && bodyTransform != null)
            {
                _currentBodyHandyItemObject.AttachToSocket(bodyTransform, _currentBodyHandyItemObject.BodyOffset, _currentBodyHandyItemObject.BodyRotation);
                SetLayerAndShadow(_currentBodyHandyItemObject.transform, isOwned ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On);
            }
        }
    }

    private void SetBodyTransformInList(PlayerHandyType type, Transform targetTransform)
    {
        if (targetTransform == null) return;

        bool found = false;
        for (int i = 0; i < _itemHandyTypeTransforms.Count; i++)
        {
            var item = _itemHandyTypeTransforms[i];
            if (item != null && item.PlayerHandyType == type)
            {
                item.BodyTransform = targetTransform;
                found = true;
                break;
            }
        }

        if (!found)
        {
            _itemHandyTypeTransforms.Add(new ItemHandyTypeTransform
            {
                PlayerHandyType = type,
                BodyTransform = targetTransform
            });
        }
    }
}