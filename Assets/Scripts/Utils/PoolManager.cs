using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public interface IPoolable
{
    public void OnSpawned();
    public void OnDespawned();
}

public static class PoolManager
{
    private static readonly Dictionary<GameObject, Pool> Pools = new();

    [RuntimeInitializeOnLoadMethod]
    private static void Init()
    {
        Application.quitting += () => OnExiting?.Invoke();
        SceneManager.sceneUnloaded += scene => OnSceneUnloaded?.Invoke(scene);
    }

    private static event Action OnExiting;
    private static event Action<Scene> OnSceneUnloaded;

    /// <summary>
    ///     오브젝트를 풀에서 가져옵니다.
    /// </summary>
    /// <param name="prefab">프리팹</param>
    /// <param name="parent">부모</param>
    /// <typeparam name="T">컴포넌트 타입</typeparam>
    /// <returns>풀링된 오브젝트</returns>
    public static T Get<T>(T prefab, Transform parent = null) where T : Object
    {
        if (prefab == null) throw new ArgumentNullException(nameof(prefab));

        var pool = Pool<T>.Instance;
        return pool.Get(prefab, parent);
    }

    /// <summary>
    ///     오브젝트를 풀에서 가져옵니다.
    /// </summary>
    /// <param name="prefab">프리팹</param>
    /// <param name="position">위치</param>
    /// <param name="rotation">회전</param>
    /// <param name="parent">부모</param>
    /// <typeparam name="T">컴포넌트 타입</typeparam>
    /// <returns>풀링된 오브젝트</returns>
    public static T Get<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Object
    {
        if (prefab == null) throw new ArgumentNullException(nameof(prefab));

        var pool = Pool<T>.Instance;
        var obj = pool.Get(prefab, parent);

        var gameObject = GetGameObject(obj);
        gameObject.transform.SetPositionAndRotation(position, rotation);

        return obj;
    }

    /// <summary>
    ///     오브젝트를 풀에 반환합니다.
    /// </summary>
    /// <param name="obj">반환할 오브젝트</param>
    /// <param name="f"></param>
    /// <typeparam name="T">컴포넌트 타입</typeparam>
    public static void Release<T>(T obj) where T : Object
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));

        var gameObject = GetGameObject(obj);

        if (!Pools.TryGetValue(gameObject, out var pool))
        {
            Pool<GameObject>.Instance.Release(gameObject);
            return;
        }

        pool.Release(gameObject);
    }

    /// <summary>
    ///     오브젝트를 풀에 반환합니다.
    /// </summary>
    /// <param name="obj">반환할 오브젝트</param>
    /// <param name="delay">지연시간</param>
    /// <typeparam name="T">컴포넌트 타입</typeparam>
    public static void Release<T>(T obj, float delay) where T : Object
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));

        ReleaseAsync(obj, delay).Forget();
        return;

        static async UniTaskVoid ReleaseAsync(T obj, float delay)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
            Release(obj);
        }
    }

    /// <summary>
    ///     오브젝트를 풀에 미리 생성합니다.
    /// </summary>
    /// <param name="prefab">프리팹</param>
    /// <param name="count">생성할 개수</param>
    /// <typeparam name="T">컴포넌트 타입</typeparam>
    public static void Preload<T>(T prefab, int count) where T : Object
    {
        if (prefab == null) throw new ArgumentNullException(nameof(prefab));

        var pool = Pool<T>.Instance;
        for (var i = 0; i < count; i++)
        {
            var obj = pool.Get(prefab);
            pool.Release(obj);
        }
    }

    /// <summary>
    ///     수동으로 모든 풀을 비웁니다.
    /// </summary>
    public static void Clear()
    {
        foreach (var pool in Pools.Values) pool.ClearPool();

        Pools.Clear();
    }

    private static GameObject GetGameObject<T>(T prefab) where T : Object
    {
        return prefab as GameObject ?? (prefab as Component)?.gameObject;
    }

    private abstract class Pool
    {
        public abstract void Release(GameObject gameObject);
        public abstract void ClearPoolInScene(Scene scene);
        public abstract void ClearPool();
    }

    private class Pool<T> : Pool where T : Object
    {
        private readonly Dictionary<GameObject, T> _components = new();
        private readonly Dictionary<GameObject, IPoolable[]> _poolables = new();
        private readonly Dictionary<GameObject, T> _prefabs = new();
        private readonly Dictionary<T, List<T>> _lists = new();
        private readonly HashSet<GameObject> _pooledObjects = new();

        static Pool()
        {
            OnExiting += Instance.ClearPool;
            OnSceneUnloaded += Instance.ClearPoolInScene;
        }

        public static Pool<T> Instance { get; } = new();

        public T Get(T prefab, Transform parent = null)
        {
            if (!_lists.TryGetValue(prefab, out var stack))
            {
                stack = new List<T>();
                _lists.Add(prefab, stack);
            }

            T obj;
            GameObject gameObject;

            if (stack.Count > 0)
            {
                obj = stack[^1];
                stack.RemoveAt(stack.Count - 1);
                gameObject = GetGameObject(obj);
            }
            else
            {
                obj = Object.Instantiate(prefab, parent);
                gameObject = GetGameObject(obj);
                AddToManagedObject(prefab, gameObject, obj);
            }

            gameObject.transform.SetParent(parent, false);
            gameObject.hideFlags = HideFlags.None;
            gameObject.transform.SetAsLastSibling();

            if (GetGameObject(prefab).activeSelf)
                gameObject.SetActive(true);

            foreach (var poolable in _poolables[gameObject]) poolable.OnSpawned();

            _pooledObjects.Remove(gameObject);

            return obj;
        }

        private void AddToManagedObject(T prefab, GameObject gameObject, T obj)
        {
            _prefabs.Add(gameObject, prefab);
            _components.Add(gameObject, obj);
            _poolables.Add(gameObject, gameObject.GetComponentsInChildren<IPoolable>(true));
            _pooledObjects.Add(gameObject);
            Pools.Add(gameObject, this);

            gameObject.name = $"[Pooled #{Pools.Count.ToString()}] {prefab.name}";
        }

        public void Release(T obj)
        {
            Release(GetGameObject(obj));
        }

        public override void Release(GameObject gameObject)
        {
            if (_prefabs.TryGetValue(gameObject, out var prefab) == false)
            {
                gameObject.GetComponentsInChildren<IPoolable>(true).ToList()
                    .ForEach(poolable => poolable.OnDespawned());

                Debug.LogWarning($"[PoolManager] {gameObject.name} is not pooled object.");
                Object.Destroy(gameObject);
                return;
            }

            if (_pooledObjects.Contains(gameObject))
            {
                Debug.LogError($"[PoolManager] {gameObject.name} is already released.");
                return;
            }

            if (_poolables.TryGetValue(gameObject, out var poolables))
                foreach (var poolable in poolables)
                    poolable.OnDespawned();

            if (gameObject != null) gameObject.SetActive(false);
            gameObject.hideFlags = HideFlags.HideInHierarchy;
            _lists[prefab].Add(_components[gameObject]);
            _pooledObjects.Add(gameObject);
        }

        public override void ClearPoolInScene(Scene scene)
        {
            var liveObjects = new List<T>();

            foreach (var obj in _lists.Values.SelectMany(stack => stack))
            {
                if (obj == null) continue;
                var gameObject = GetGameObject(obj);
                if (gameObject.scene != scene)
                {
                    liveObjects.Add(obj);
                    continue;
                }

                Object.Destroy(gameObject);
            }

            _lists.Clear();
            _prefabs.Clear();
            _poolables.Clear();
            _components.Clear();

            foreach (var prefab in liveObjects.Select(Object.Instantiate))
            {
                AddToManagedObject(prefab, GetGameObject(prefab), prefab);
            }
        }

        public override void ClearPool()
        {
            foreach (var obj in _lists.Values.SelectMany(stack => stack))
            {
                if (obj == null) continue;
                var gameObject = GetGameObject(obj);

                Object.Destroy(gameObject);
            }

            _lists.Clear();
            _prefabs.Clear();
            _poolables.Clear();
            _components.Clear();
        }
    }
}