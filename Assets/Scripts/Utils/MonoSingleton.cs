using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T _instance;

    protected virtual void Awake()
    {
        if (_instance != null) return;

        var instance = this as T;

        SingletonManager.Register(instance);

        _instance = instance;
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
                if (!Application.isPlaying)
                {
                    return null!;
                }

                _instance = new GameObject(typeof(T).ToString()).AddComponent<T>();
            }

            if (_instance != null && _instance.transform.parent != null)
            {
                _instance.transform.SetParent(null);
            }

            return _instance;
        }
    }

    protected virtual void OnDestroy()
    {
#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlaying) return;
#endif
        if(_instance == this)
            _instance = null;
    }
}