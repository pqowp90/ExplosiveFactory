#if UNITY_EDITOR
using System.IO;
using Mirror;
using UnityEditor;
using UnityEngine;

namespace ExplosiveFactory.Editor
{
    public class NetworkPrefabPostprocessor : AssetPostprocessor
    {
        private const string NetworkFolder = "Assets/Resources/Network";

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool hasNetworkChanges = false;

            foreach (var path in importedAssets)
            {
                if (path.StartsWith(NetworkFolder) && path.EndsWith(".prefab"))
                {
                    hasNetworkChanges = true;
                    break;
                }
            }

            if (!hasNetworkChanges)
            {
                foreach (var path in deletedAssets)
                {
                    if (path.StartsWith(NetworkFolder) && path.EndsWith(".prefab"))
                    {
                        hasNetworkChanges = true;
                        break;
                    }
                }
            }

            if (hasNetworkChanges)
            {
                EditorApplication.delayCall += RefreshNetworkPrefabs;
            }
        }

        [MenuItem("ExplosiveFactory/Refresh Network Prefabs Registry", false, 20)]
        public static void RefreshNetworkPrefabs()
        {
            if (!Directory.Exists(NetworkFolder)) return;

            var guids = AssetDatabase.FindAssets("t:GameObject", new[] { NetworkFolder });
            int registeredCount = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null && go.TryGetComponent<NetworkIdentity>(out _))
                {
                    registeredCount++;
                }
            }

            Debug.Log($"<color=#00FF88>[NetworkPrefabRegistry] Resources/Network 폴더에서 {registeredCount}개의 네트워크 프리팹을 성공적으로 감지 및 등록했습니다.</color>");
        }
    }
}
#endif
