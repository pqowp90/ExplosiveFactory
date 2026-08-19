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
        NetworkServer.Spawn(GetGameObject(obj));
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
        NetworkServer.Spawn(GetGameObject(obj), connection);
        return obj;
    }

    [Server]
    public static T Get<T>(T prefab, NetworkConnectionToClient connection, Vector3 position, Quaternion rotation,
        Transform parent = null) where T : Object
    {
        var obj = PoolManager.Get(prefab, position, rotation, parent);
        NetworkServer.Spawn(GetGameObject(obj), connection);
        return obj;
    }

    [Server]
    public static void Release<T>(T obj) where T : Object
    {
        GetGameObject(obj).SetActive(false);
        Instance._releaseActions.Add(Instance._releaseIdCounter, () =>
        {
            NetworkServer.UnSpawn(GetGameObject(obj));
            PoolManager.Release(obj);
        });
        Instance.RpcRelease(Instance._releaseIdCounter);
        Instance._releaseIdCounter++;
    }

    private static GameObject GetGameObject<T>(T prefab) where T : Object
    {
        return prefab as GameObject ?? (prefab as Component)?.gameObject;
    }

    private static GameObject SpawnHandler(Vector3 position, uint assetId)
    {
        var prefab = NetWorkPrefabs.FirstOrDefault(x => x.assetId == assetId);
        if (prefab == null) throw new NullReferenceException($"AssetId {assetId} not found");

        var o = PoolManager.Get(prefab.gameObject, position, Quaternion.identity);
        return o;
    }

    private static void UnSpawnHandler(GameObject o)
    {
        PoolManager.Release(o);
    }
}

[SingletonLifeTime(LifeTime.Network)]
public partial class NetworkPoolManager : NetworkSingleton<NetworkPoolManager>
{
    private readonly Dictionary<uint, Action> _releaseActions = new();
    private uint _releaseIdCounter = 0;
    private static readonly List<NetworkIdentity> NetWorkPrefabs = new();

    protected override void Awake()
    {
        base.Awake();

        NetWorkPrefabs.Clear();
        NetWorkPrefabs.AddRange(Resources.LoadAll<NetworkIdentity>("Network"));
        NetWorkPrefabs.ForEach(x => NetworkClient.RegisterPrefab(x.gameObject, SpawnHandler, UnSpawnHandler));
    }

    [ClientRpc]
    private void RpcRelease(uint releaseId)
    {
        if (!_releaseActions.ContainsKey(releaseId)) return;
        _releaseActions[releaseId].Invoke();
        _releaseActions.Remove(releaseId);
    }

#if UNITY_EDITOR
    private static void FindNetIdCommand(int netId)
    {
        var obj = NetworkIdentity.GetSceneIdentity((uint)netId);
        if (obj == null)
        {
            return;
        }

        UnityEditor.EditorGUIUtility.PingObject(obj.gameObject);

    }
#endif
}