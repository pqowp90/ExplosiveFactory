using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public enum LifeTime
{
    Scene,
    Network,
    Application
}

[AttributeUsage(AttributeTargets.Class)]
public class SingletonLifeTimeAttribute : Attribute
{
    public LifeTime LifeTime { get; }

    public SingletonLifeTimeAttribute(LifeTime lifeTime)
    {
        LifeTime = lifeTime;
    }

    public SingletonLifeTimeAttribute()
    {
        LifeTime = LifeTime.Scene;
    }
}


public static class SingletonManager
{
    private static readonly Dictionary<LifeTime, Dictionary<Type, MonoBehaviour>> InstanceDictionary = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void LoadSingleton()
    {
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        foreach (LifeTime lifeTime in Enum.GetValues(typeof(LifeTime)))
        {
            InstanceDictionary.Add(lifeTime, new());
        }

        // CustomNetworkManager.OnClientStopEvent += OnClientDisconnected;
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        InstanceDictionary[LifeTime.Scene].Clear();
    }

    private static void OnClientDisconnected()
    {
        if (InstanceDictionary[LifeTime.Network].Count == 0) return;

        foreach (var instance in InstanceDictionary[LifeTime.Network].Values) 
            Object.Destroy(instance.gameObject);

        InstanceDictionary[LifeTime.Network].Clear();
    }

    public static void Register(MonoBehaviour instance)
    {
        var att = Attribute.GetCustomAttribute(instance.GetType(), typeof(SingletonLifeTimeAttribute));

        if (att is not SingletonLifeTimeAttribute singletonLifeTimeAttribute)
        {
            Debug.LogError($"{instance.GetType()} haven't SingletonLifeTimeAttribute");
            return;
        }
        
        if (InstanceDictionary[singletonLifeTimeAttribute.LifeTime].TryGetValue(instance.GetType(), out var oldInstance))
        {
            Debug.LogWarning($"OldSingleton {oldInstance.GetType()} (LifeTime : {singletonLifeTimeAttribute.LifeTime}) Destroyed");
            Object.Destroy(oldInstance.gameObject); 
            
            InstanceDictionary[singletonLifeTimeAttribute.LifeTime].Remove(oldInstance.GetType());
        }

        InstanceDictionary[singletonLifeTimeAttribute.LifeTime].Add(instance.GetType(), instance);
        if (singletonLifeTimeAttribute.LifeTime is not LifeTime.Scene)
            DontDestroyOnLoadAsync(instance, singletonLifeTimeAttribute.LifeTime).Forget();
    }

    private static async UniTask DontDestroyOnLoadAsync(MonoBehaviour instance, LifeTime lifeTime)
    {
        if (lifeTime is LifeTime.Network)
            await UniTask.WaitUntil(() => NetworkClient.active && NetworkClient.isConnected);

        Object.DontDestroyOnLoad(instance.transform.root);
    }
}
