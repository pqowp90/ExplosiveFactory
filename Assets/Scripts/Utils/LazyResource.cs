using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ExplosiveFactory.Utils
{
    public interface ILazyResource
    {
        string Path { get; }
    }

    [Serializable]
    public class LazyResource<T> : ILazyResource where T : Object
    {
        [SerializeField] private string _path = "";
        private T? _resource;

        public T Value => _resource != null ? _resource : LoadResource(_path);
        public string Path => _path;

        public LazyResource(string path)
        {
            _path = path;
        }

        public static implicit operator LazyResource<T>(string path)
        {
            return new LazyResource<T>(path);
        }

        public static implicit operator string(LazyResource<T> lazyResource)
        {
            return lazyResource._path;
        }

        public static implicit operator T?(LazyResource<T>? lazyResource)
        {
            return lazyResource?.Value;
        }

        public static bool operator ==(LazyResource<T>? lazyResource, T? other)
        {
            if (lazyResource is null) return other is null;
            return lazyResource.Value == other;
        }

        public static bool operator !=(LazyResource<T>? lazyResource, T? other)
        {
            return !(lazyResource == other);
        }

        public override bool Equals(object? obj)
        {
            return obj is LazyResource<T> lazyResource && Equals(lazyResource.Value, Value);
        }

        public override int GetHashCode()
        {
            return Value != null ? Value.GetHashCode() : 0;
        }

        private T LoadResource(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("[LazyResource] Load path is empty.");
                return null!;
            }

            if (path.StartsWith("Assets/Resources/"))
            {
                path = path["Assets/Resources/".Length..];
            }

            if (path.Split('/').Last().Contains('.'))
            {
                path = path.Split('/').Last().Split('.')[0];
            }

            var resource = Resources.Load<T>(path);

            if (!resource)
            {
                Debug.LogError($"[LazyResource] Resource not found at path: {path}");
                return null!;
            }

            _resource = resource;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            return resource;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            UnloadResource();
        }

        public void UnloadResource()
        {
            if (_resource == null) return;
            if (_resource is not (GameObject or Component))
            {
                Resources.UnloadAsset(_resource);
            }
            _resource = null;
        }

        public override string ToString()
        {
            return _path;
        }
    }
}
