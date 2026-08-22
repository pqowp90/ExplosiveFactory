using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using Object = UnityEngine.Object;

public partial class NetworkPoolManager
{
    [Server]
    public static T Get<T>(T prefab, Transform parent = null) where T : Object
    {
        var obj = PoolManager.Get(prefab, parent);
        var gameObject = GetGameObject(obj);
        NetworkServer.Spawn(gameObject);
        return obj;
    }

    [Server]
    public static T Get<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Object
    {
        var obj = PoolManager.Get(prefab, position, rotation, parent);
        var gameObject = GetGameObject(obj);
        NetworkServer.Spawn(gameObject);
        return obj;
    }

    [Server]
    public static T Get<T>(T prefab, NetworkConnectionToClient connection, Transform parent = null) where T : Object
    {
        var obj = PoolManager.Get(prefab, parent);
        var gameObject = GetGameObject(obj);
        NetworkServer.Spawn(gameObject, connection);
        return obj;
    }

    [Server]
    public static T Get<T>(T prefab, NetworkConnectionToClient connection, Vector3 position, Quaternion rotation,
        Transform parent = null) where T : Object
    {
        var obj = PoolManager.Get(prefab, position, rotation, parent);
        var gameObject = GetGameObject(obj);
        NetworkServer.Spawn(gameObject, connection);
        return obj;
    }

    [Server]
    public static void Release<T>(T obj) where T : Object
    {
        if (obj == null) return;
        var gameObject = GetGameObject(obj);
        if (gameObject == null) return;

        gameObject.SetActive(false);

        if (Instance != null)
        {
            uint id = Instance._releaseIdCounter++;
            Instance._releaseActions[id] = () =>
            {
                NetworkServer.UnSpawn(gameObject);
                PoolManager.Release(obj);
            };
            Instance.RpcRelease(id);
        }
        else
        {
            NetworkServer.UnSpawn(gameObject);
            PoolManager.Release(obj);
        }
    }

    private static GameObject GetGameObject<T>(T prefab) where T : Object
    {
        return prefab as GameObject ?? (prefab as Component)?.gameObject;
    }

    public static GameObject SpawnHandler(Vector3 position, uint assetId)
    {
        var prefab = NetWorkPrefabs.FirstOrDefault(x => x.assetId == assetId);
        if (prefab == null)
        {
            Debug.LogWarning($"[NetworkPoolManager] AssetId {assetId} not found in NetWorkPrefabs.");
            return null;
        }

        var o = PoolManager.Get(prefab.gameObject, position, Quaternion.identity);
        return o;
    }

    public static void UnSpawnHandler(GameObject o)
    {
        if (o != null)
        {
            PoolManager.Release(o);
        }
    }
}

[SingletonLifeTime(LifeTime.Application)]
public partial class NetworkPoolManager : NetworkSingleton<NetworkPoolManager>
{
    private readonly Dictionary<uint, Action> _releaseActions = new();
    private uint _releaseIdCounter = 0;
    private static readonly List<NetworkIdentity> NetWorkPrefabs = new();

    protected override void Awake()
    {
        base.Awake();
        RegisterNetworkPrefabs();
    }

    public static void RegisterNetworkPrefabs()
    {
        NetWorkPrefabs.Clear();
        var allNetPrefabs = Resources.LoadAll<GameObject>("Network");
        foreach (var p in allNetPrefabs)
        {
            if (p != null && p.TryGetComponent<NetworkIdentity>(out var netId))
            {
                NetWorkPrefabs.Add(netId);
                if (!NetworkClient.prefabs.ContainsKey(netId.assetId))
                {
                    NetworkClient.RegisterPrefab(p, SpawnHandler, UnSpawnHandler);
                }
            }
        }
    }

    [ClientRpc]
    private void RpcRelease(uint releaseId)
    {
        if (!_releaseActions.ContainsKey(releaseId)) return;
        _releaseActions[releaseId].Invoke();
        _releaseActions.Remove(releaseId);
    }
}