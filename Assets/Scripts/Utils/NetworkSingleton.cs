using Mirror;
using UnityEngine;

public class NetworkSingleton<T> : NetworkBehaviour where T : NetworkSingleton<T>
{
    private static T _instance;

    protected virtual void Awake()
    {
        _instance = this as T;

        SingletonManager.Register(_instance);
    }

    
    public static bool IsExist() => _instance != null;

    public static T Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = FindObjectOfType<T>();

            if (_instance == null)
            {
                Debug.LogError($"{typeof(T)} is not exist.");
                return null;
            }
            
            _instance.transform.SetParent(null);

            return _instance;
        }
    }


    protected virtual void OnDestroy()
    {
#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlaying) return;
#endif
        _instance = null;
    }
}