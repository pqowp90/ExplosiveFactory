#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;

namespace ExplosiveFactory.Editor.Toolbar
{
    public class ToolbarToggle
    {
        private readonly string _key;
        private bool _isOn;

        public ToolbarToggle(string label, bool defaultValue = false)
        {
            Label = label;
            _key = "ExplosiveFactory_" + label.Replace(" ", "");
            _isOn = EditorPrefs.GetBool(_key, defaultValue);
        }

        public string Label { get; }

        public bool IsOn
        {
            get => _isOn;
            set
            {
                if (_isOn == value) return;

                _isOn = value;
                EditorPrefs.SetBool(_key, _isOn);
                OnValueChanged?.Invoke(_isOn);
            }
        }

        public event Action<bool>? OnValueChanged;
    }

    [InitializeOnLoad]
    public static class ToolbarStartInMainMenu
    {
        private const string ElementId = "ExplosiveFactory/StartInMainMenu";
        private const string PrefKey_StartScenePath = "ExplosiveFactory_StartScenePath";

        // 기본 시작 씬 경로 (메인메뉴 씬)
        public static string StartScenePath
        {
            get => EditorPrefs.GetString(PrefKey_StartScenePath, "Assets/Scenes/MainMenuScene.unity");
            set => EditorPrefs.SetString(PrefKey_StartScenePath, value);
        }

        private static readonly ToolbarToggle StartInMainMenuToggle = new("Start In Main Menu", false);

        static ToolbarStartInMainMenu()
        {
            // MainMenuScene이 존재하면 시작 씬 경로를 MainMenuScene으로 보정
            if (File.Exists("Assets/Scenes/MainMenuScene.unity"))
            {
                StartScenePath = "Assets/Scenes/MainMenuScene.unity";
            }

            StartInMainMenuToggle.OnValueChanged += ApplyPlayModeStartScene;
            ApplyPlayModeStartScene(StartInMainMenuToggle.IsOn);
        }

        [MainToolbarElement(ElementId, defaultDockPosition = MainToolbarDockPosition.Left)]
        public static IEnumerable<MainToolbarElement> CreateToggles()
        {
            yield return new MainToolbarToggle(
                new MainToolbarContent(StartInMainMenuToggle.Label, "켜두면 플레이 모드 실행 시 항상 메인메뉴 씬(MainMenuScene)에서 시작합니다. (끄면 현재 열려있는 씬에서 시작)"),
                StartInMainMenuToggle.IsOn,
                value =>
                {
                    StartInMainMenuToggle.IsOn = value;
                    MainToolbar.Refresh(ElementId);
                });
        }

        [MenuItem("ExplosiveFactory/Toolbar/Set Current Scene as Start Scene", false, 20)]
        public static void SetCurrentSceneAsStartScene()
        {
            var currentScene = EditorSceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(currentScene.path))
            {
                SetStartScene(currentScene.path);
                Debug.Log($"<color=#00FF00>[Toolbar] 시작 씬이 변경되었습니다: {currentScene.path}</color>");
            }
        }

        public static void SetStartScene(string scenePath)
        {
            StartScenePath = scenePath;
            if (StartInMainMenuToggle.IsOn)
            {
                ApplyPlayModeStartScene(true);
            }
        }

        private static void ApplyPlayModeStartScene(bool isEnabled)
        {
            if (isEnabled)
            {
                string path = StartScenePath;
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                
                // MainMenuScene 탐색
                if (sceneAsset == null)
                {
                    var mainMenuGuids = AssetDatabase.FindAssets("MainMenuScene t:SceneAsset");
                    if (mainMenuGuids.Length > 0)
                    {
                        path = AssetDatabase.GUIDToAssetPath(mainMenuGuids[0]);
                        sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                        StartScenePath = path;
                    }
                }

                EditorSceneManager.playModeStartScene = sceneAsset;
                if (sceneAsset != null)
                {
                    Debug.Log($"[Toolbar] PlayMode Start Scene set to: {path}");
                }
            }
            else
            {
                EditorSceneManager.playModeStartScene = null;
                Debug.Log("[Toolbar] PlayMode Start Scene disabled (현재 작업 중인 씬에서 플레이 시작)");
            }
        }
    }
}
#endif
