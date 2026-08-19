#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ExplosiveFactory.Editor.Toolbar
{
    [InitializeOnLoad]
    public static class ToolbarSceneSwitcher
    {
        private const string ElementId = "ExplosiveFactory/SceneSwitcher";
        private const string ScenesFolderPath = "Assets/Scenes";

        static ToolbarSceneSwitcher()
        {
            EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
        }

        private static void OnActiveSceneChanged(Scene previous, Scene current)
        {
            MainToolbar.Refresh(ElementId);
        }

        [MainToolbarElement(ElementId, defaultDockPosition = MainToolbarDockPosition.Left)]
        public static MainToolbarElement CreateSceneDropdown()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(currentSceneName)) currentSceneName = "Untitled";

            var content = new MainToolbarContent($"🎬 {currentSceneName}", "클릭하여 프로젝트 내 씬으로 바로 전환합니다.");
            return new MainToolbarDropdown(content, ShowSceneMenu);
        }

        private static void ShowSceneMenu(Rect rect)
        {
            var menu = new GenericMenu();
            string fullPath = Path.Combine(Application.dataPath, "Scenes");
            
            if (Directory.Exists(fullPath))
            {
                PopulateSceneMenu(fullPath, menu);
            }
            else
            {
                // Assets 전체에서 씬 탐색
                string[] guids = AssetDatabase.FindAssets("t:SceneAsset");
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    string sceneName = Path.GetFileNameWithoutExtension(assetPath);
                    menu.AddItem(new GUIContent(sceneName), false, () => SwitchScene(assetPath));
                }
            }

            menu.DropDown(rect);
        }

        private static void PopulateSceneMenu(string dirPath, GenericMenu menu, string subMenuPath = "")
        {
            string[] entries = { };
            try
            {
                entries = Directory.GetFileSystemEntries(dirPath);
            }
            catch
            {
                return;
            }

            foreach (var entry in entries)
            {
                string extension = Path.GetExtension(entry);
                if (extension.Equals(".meta", System.StringComparison.OrdinalIgnoreCase)) continue;

                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(entry);

                if (extension.Equals(".unity", System.StringComparison.OrdinalIgnoreCase))
                {
                    int assetsIndex = entry.IndexOf("Assets", System.StringComparison.OrdinalIgnoreCase);
                    if (assetsIndex == -1) continue;

                    string assetPath = entry.Substring(assetsIndex).Replace('\\', '/');
                    string currentActivePath = SceneManager.GetActiveScene().path.Replace('\\', '/');
                    bool isActive = string.Equals(assetPath, currentActivePath, System.StringComparison.OrdinalIgnoreCase);

                    menu.AddItem(new GUIContent($"{subMenuPath}{fileNameWithoutExt}"), isActive, () =>
                    {
                        SwitchScene(assetPath);
                    });
                }
                else if (Directory.Exists(entry))
                {
                    PopulateSceneMenu(entry, menu, $"{subMenuPath}{fileNameWithoutExt}/");
                }
            }
        }

        private static void SwitchScene(string assetPath)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(assetPath);
            }
        }
    }
}
#endif
