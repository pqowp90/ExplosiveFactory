using System;
using System.Collections.Generic;
using ExplosiveFactory.Item.Data;
using UnityEngine;

[SingletonLifeTime(LifeTime.Application)]
public class ItemDataManager : MonoSingleton<ItemDataManager>
{
    private static readonly string ItemDataPath = "ItemData";
    private readonly List<ItemData> _itemDataList = new();
    private readonly Dictionary<string, ItemData> _itemDataById = new();
    private readonly Dictionary<int, ItemData> _itemDataByIntId = new();

    public List<ItemData> ItemDataList => _itemDataList;

    protected override void Awake()
    {
        base.Awake();
        LoadItemDataList();
    }

    private void LoadItemDataList()
    {
        _itemDataList.Clear();
        _itemDataById.Clear();
        _itemDataByIntId.Clear();

        var itemDataArray = Resources.LoadAll<ItemData>(ItemDataPath);
        foreach (var data in itemDataArray)
        {
            if (data == null) continue;
            _itemDataList.Add(data);

            if (!string.IsNullOrEmpty(data.id) && !_itemDataById.ContainsKey(data.id))
            {
                _itemDataById.Add(data.id, data);
            }

            if (!_itemDataByIntId.ContainsKey(data.itemID))
            {
                _itemDataByIntId.Add(data.itemID, data);
            }
        }

        _itemDataList.Sort((x, y) => x.itemID.CompareTo(y.itemID));
        Debug.Log($"[ItemDataManager] Loaded {_itemDataList.Count} ItemData assets from Resources/{ItemDataPath}");
    }

    public ItemData? GetItemDataById(string id)
    {
        if (_itemDataById.TryGetValue(id, out var data))
            return data;
        return null;
    }

    public ItemData? GetItemDataByIntId(int itemID)
    {
        if (_itemDataByIntId.TryGetValue(itemID, out var data))
            return data;
        return null;
    }
}