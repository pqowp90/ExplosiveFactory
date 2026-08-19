#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;

namespace ExplosiveFactory.Editor.Toolbar
{
    [InitializeOnLoad]
    public static class ToolbarPlayerPrefab
    {
        private const string ElementId = "ExplosiveFactory/PlayerPrefab";
        private const string PrefKey_PlayerPrefabPath = "ExplosiveFactory_PlayerPrefabPath";

        public static string PlayerPrefabPath
        {
            get => EditorPrefs.GetString(PrefKey_PlayerPrefabPath, "Assets/Prefabs/Player.prefab");
            set => EditorPrefs.SetString(PrefKey_PlayerPrefabPath, value);
        }

        [MainToolbarElement(ElementId, defaultDockPosition = MainToolbarDockPosition.Left)]
        public static MainToolbarElement CreatePlayerPrefabButton()
        {
            var content = new MainToolbarContent("👤 Player Prefab", "클릭하여 플레이어 프리팹을 즉시 열고 편집 모드로 진입합니다.");
            return new MainToolbarButton(content, OpenPlayerPrefab);
        }

        private static void OpenPlayerPrefab()
        {
            string path = PlayerPrefabPath;

            // 경로에 파일이 없는 경우 프로젝트 내에서 플레이어 프리팹 자동 탐색
            if (!File.Exists(path))
            {
                string[] guids = AssetDatabase.FindAssets("Player t:Prefab");
                if (guids.Length > 0)
                {
                    path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    PlayerPrefabPath = path;
                }
            }

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[Toolbar] 플레이어 프리팹을 찾을 수 없습니다: {path}\nAssets/Prefabs/Player.prefab을 생성하거나 경로를 확인해주세요.");
                
                // 프리팹 선택창 제공
                string selected = EditorUtility.OpenFilePanel("플레이어 프리팹 선택", Application.dataPath, "prefab");
                if (!string.IsNullOrEmpty(selected))
                {
                    int assetsIndex = selected.IndexOf("Assets", System.StringComparison.OrdinalIgnoreCase);
                    if (assetsIndex != -1)
                    {
                        path = selected.Substring(assetsIndex).Replace('\\', '/');
                        PlayerPrefabPath = path;
                    }
                }
                else
                {
                    return;
                }
            }

            // 프리팹 스테이지 열기
            PrefabStageUtility.OpenPrefab(path);
            Debug.Log($"[Toolbar] Opened Player Prefab: {path}");

            // 애니메이션 창 열기 & 포커스
            try
            {
                var animWindow = EditorWindow.GetWindow<AnimationWindow>();
                if (animWindow != null)
                {
                    animWindow.Show();
                    animWindow.Focus();
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}
#endif
