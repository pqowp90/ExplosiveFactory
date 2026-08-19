using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

#if ENABLE_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif

namespace ExplosiveFactory.Utils
{
    public interface ILazyAddressable
    {
        string Path { get; }
    }

    [Serializable]
    public class LazyAddressable<T> : ILazyAddressable where T : Object
    {
        [SerializeField] private string _path = "";

        public LazyAddressable(string path)
        {
            _path = path;
        }

        public T Value => Resource != null ? Resource : LoadResource(_path);

        public T? Resource { get; private set; }
        public string Path => _path;

        public static implicit operator LazyAddressable<T>(string path)
        {
            return new LazyAddressable<T>(path);
        }

        public static implicit operator string(LazyAddressable<T> lazyAddressable)
        {
            return lazyAddressable._path;
        }

        public static implicit operator T?(LazyAddressable<T>? lazyAddressable)
        {
            return lazyAddressable != null ? lazyAddressable.Value : null;
        }

        public override bool Equals(object? obj)
        {
            return obj is LazyAddressable<T> lazyAddressable && lazyAddressable._path == _path;
        }

        public override int GetHashCode()
        {
            return _path != null ? _path.GetHashCode() : 0;
        }

        public T LoadResource(string loadPath)
        {
            if (string.IsNullOrEmpty(loadPath))
            {
                Debug.LogWarning("[LazyAddressable] Load path is null or empty.");
                return null!;
            }

#if ENABLE_ADDRESSABLES
            try
            {
                var handle = Addressables.LoadAssetAsync<T>(loadPath);
                var resource = handle.WaitForCompletion();
                Resource = resource;
                SceneManager.sceneUnloaded += OnSceneUnloaded;
                return resource;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LazyAddressable] Failed to load Addressable at '{loadPath}': {ex.Message}");
                return null!;
            }
#else
            // Addressables 미활성화 시 Resources 폴백
            var res = Resources.Load<T>(loadPath);
            Resource = res;
            return res;
#endif
        }

        private void OnSceneUnloaded(Scene scene)
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            UnloadResource();
        }

        public void UnloadResource()
        {
            if (Resource == null) return;
#if ENABLE_ADDRESSABLES
            Addressables.Release(Resource);
#else
            if (Resource is not (GameObject or Component))
            {
                Resources.UnloadAsset(Resource);
            }
#endif
            Resource = null;
        }

        public override string ToString()
        {
            return _path;
        }
    }
}
