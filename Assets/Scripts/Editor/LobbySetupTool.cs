#if UNITY_EDITOR
using System.IO;
using ExplosiveFactory.Network;
using ExplosiveFactory.Network.UI;
using Mirror;
using Mirror.FizzySteam;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ExplosiveFactory.Editor
{
    [InitializeOnLoad]
    public static class LobbySetupTool
    {
        private const string PrefabsPath = "Assets/Resources/Network";
        private const string ScenesPath = "Assets/Scenes";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenuScene.unity";
        private const string LobbyScenePath = "Assets/Scenes/LobbyScene.unity";
        private const string GameScenePath = "Assets/Scenes/GameScene.unity";

        static LobbySetupTool()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(MainMenuScenePath))
                {
                    SetupAll();
                }
            };
        }

        [MenuItem("ExplosiveFactory/Setup 3-Scenes & Prefabs (MainMenu, Lobby, Game)", false, 1)]
        public static void SetupAll()
        {
            EnsureDirectories();
            var lobbyPlayerPrefab = CreateLobbyPlayerPrefab();
            var gamePlayerPrefab = CreateGamePlayerPrefab();
            var entryPrefab = CreatePlayerEntryPrefab();

            CreateMainMenuScene(lobbyPlayerPrefab, gamePlayerPrefab);
            CreateLobbyScene(entryPrefab);
            CreateGameScene();

            RegisterBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#00FF00>[LobbySetupTool] Project-Eddy 2D 캐릭터 및 3개 씬 세팅이 완벽히 완료되었습니다!</color>");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(PrefabsPath)) Directory.CreateDirectory(PrefabsPath);
            if (!Directory.Exists(ScenesPath)) Directory.CreateDirectory(ScenesPath);
        }

        private static GameObject CreateLobbyPlayerPrefab()
        {
            string path = $"{PrefabsPath}/LobbyPlayer.prefab";
            var go = new GameObject("LobbyPlayer");
            go.AddComponent<NetworkIdentity>();
            go.AddComponent<LobbyPlayer>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"[LobbySetupTool] LobbyPlayer Prefab created at: {path}");
            return prefab;
        }

        private static GameObject CreateGamePlayerPrefab()
        {
            string path = $"{PrefabsPath}/GamePlayer.prefab";
            GameObject go;

            // Load original BackPack Player prefab if available
            var backPackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/BackPack_Player.prefab");
            if (backPackPrefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(backPackPrefab);
                go.name = "GamePlayer";
            }
            else
            {
                go = new GameObject("GamePlayer");
                var cc = go.AddComponent<CharacterController>();
                cc.center = new Vector3(0, 1f, 0);
                cc.radius = 0.4f;
                cc.height = 2f;
            }

            // Remove non-network legacy scripts if present
            var oldMove = go.GetComponent("PlayerMove");
            if (oldMove != null) Object.DestroyImmediate(oldMove);
            var oldAnim = go.GetComponent("PlayerAnimation");
            if (oldAnim != null) Object.DestroyImmediate(oldAnim);

            // Add Mirror Network Identity & Transform
            if (!go.TryGetComponent<NetworkIdentity>(out _))
                go.AddComponent<NetworkIdentity>();

            if (!go.TryGetComponent<NetworkTransformReliable>(out _))
                go.AddComponent<NetworkTransformReliable>();

            if (!go.TryGetComponent<GamePlayer>(out var gamePlayer))
                gamePlayer = go.AddComponent<GamePlayer>();

            // Head Name Tag TextMeshPro
            var nameTagTransform = go.transform.Find("NameTag");
            TextMeshPro tmp;
            if (nameTagTransform == null)
            {
                var textObj = new GameObject("NameTag", typeof(TextMeshPro));
                textObj.transform.SetParent(go.transform, false);
                textObj.transform.localPosition = new Vector3(0, 2.2f, 0);
                tmp = textObj.GetComponent<TextMeshPro>();
            }
            else
            {
                tmp = nameTagTransform.GetComponent<TextMeshPro>();
            }

            var font = GetKoreanFontAsset();
            if (font != null) tmp.font = font;

            tmp.text = "Player";
            tmp.fontSize = 4;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.yellow;

            var so = new SerializedObject(gamePlayer);
            so.FindProperty("nameText").objectReferenceValue = tmp;
            so.ApplyModifiedProperties();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"[LobbySetupTool] GamePlayer Prefab created with authentic 1st-person BackPack setup at: {path}");
            return prefab;
        }

        private static GameObject CreatePlayerEntryPrefab()
        {
            string path = $"{PrefabsPath}/PlayerEntry.prefab";
            var go = new GameObject("PlayerEntry", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 45);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.2f, 0.85f);

            var textGo = new GameObject("PlayerNameText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(15, 5);
            textRt.offsetMax = new Vector2(-15, -5);

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            var font = GetKoreanFontAsset();
            if (font != null) tmp.font = font;

            tmp.text = "PlayerName [대기 중]";
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"[LobbySetupTool] PlayerEntry Prefab created at: {path}");
            return prefab;
        }

        #region Scene Creations

        private static void CreateMainMenuScene(GameObject lobbyPlayerPrefab, GameObject gamePlayerPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 1. Network Manager Root (DontDestroyOnLoad)
            var netObj = new GameObject("[NetworkManager]");
            netObj.AddComponent<SteamManager>();
            netObj.AddComponent<LobbyService>();
            var transport = netObj.AddComponent<FizzyFacepunch>();
            transport.InitFacepunch = false;

            var netMgr = netObj.AddComponent<CustomNetworkManager>();
            netMgr.transport = transport;
            netMgr.playerPrefab = lobbyPlayerPrefab;
            netMgr.autoCreatePlayer = false;
            netMgr.dontDestroyOnLoad = true;

            // Register distinct prefabs
            var soNet = new SerializedObject(netMgr);
            soNet.FindProperty("lobbyPlayerPrefab").objectReferenceValue = lobbyPlayerPrefab;
            soNet.FindProperty("gamePlayerPrefab").objectReferenceValue = gamePlayerPrefab;

            // spawnPrefabs에 gamePlayerPrefab 등록
            var spawnProp = soNet.FindProperty("spawnPrefabs");
            spawnProp.ClearArray();
            spawnProp.InsertArrayElementAtIndex(0);
            spawnProp.GetArrayElementAtIndex(0).objectReferenceValue = gamePlayerPrefab;

            soNet.ApplyModifiedProperties();

            // 2. Canvas & UI
            var canvasGo = CreateCanvas("MainMenuCanvas");
            var mainMenuUI = canvasGo.AddComponent<MainMenuUI>();

            var bg = CreatePanel(canvasGo.transform, "Background", new Color(0.08f, 0.08f, 0.1f, 1f));
            var title = CreateText(bg.transform, "TitleText", "EXPLOSIVE FACTORY", 54, new Vector2(0, 140), new Vector2(700, 90));
            title.fontStyle = FontStyles.Bold;

            var hostBtn = CreateButton(bg.transform, "HostButton", "방 만들기 (Host)", new Vector2(0, -20), new Vector2(320, 65));
            var quitBtn = CreateButton(bg.transform, "QuitButton", "게임 종료", new Vector2(0, -100), new Vector2(320, 65));

            var verText = CreateText(bg.transform, "VersionText", "v0.0.0.1", 18, new Vector2(0, -350), new Vector2(400, 40));
            verText.color = new Color(1f, 1f, 1f, 0.5f);

            var so = new SerializedObject(mainMenuUI);
            so.FindProperty("hostButton").objectReferenceValue = hostBtn.GetComponent<Button>();
            so.FindProperty("quitButton").objectReferenceValue = quitBtn.GetComponent<Button>();
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("versionText").objectReferenceValue = verText;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            Debug.Log($"[LobbySetupTool] MainMenu Scene created: {MainMenuScenePath}");
        }

        private static void CreateLobbyScene(GameObject entryPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var canvasGo = CreateCanvas("LobbyCanvas");
            var lobbyUI = canvasGo.AddComponent<LobbyUI>();

            var bg = CreatePanel(canvasGo.transform, "LobbyPanel", new Color(0.1f, 0.1f, 0.14f, 1f));
            var lobbyTitle = CreateText(bg.transform, "LobbyTitleText", "로비 대기실 (1/4)", 40, new Vector2(0, 360), new Vector2(600, 70));
            lobbyTitle.fontStyle = FontStyles.Bold;

            // Player List Container
            var listContainer = new GameObject("PlayerListContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listContainer.transform.SetParent(bg.transform, false);
            var listRt = listContainer.GetComponent<RectTransform>();
            listRt.anchoredPosition = new Vector2(0, 100);
            listRt.sizeDelta = new Vector2(480, 320);

            var vlg = listContainer.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 12;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            // Buttons
            var inviteBtn = CreateButton(bg.transform, "InviteButton", "친구 초대", new Vector2(-230, -260), new Vector2(190, 55));
            var readyBtn = CreateButton(bg.transform, "ReadyButton", "준비 완료", new Vector2(0, -260), new Vector2(190, 55));
            var readyBtnText = readyBtn.GetComponentInChildren<TextMeshProUGUI>();

            var startBtn = CreateButton(bg.transform, "StartButton", "게임 시작", new Vector2(0, -260), new Vector2(190, 55));
            startBtn.gameObject.SetActive(false);

            var leaveBtn = CreateButton(bg.transform, "LeaveButton", "로비 나가기", new Vector2(230, -260), new Vector2(190, 55));

            var so = new SerializedObject(lobbyUI);
            so.FindProperty("inviteButton").objectReferenceValue = inviteBtn.GetComponent<Button>();
            so.FindProperty("readyButton").objectReferenceValue = readyBtn.GetComponent<Button>();
            so.FindProperty("readyButtonText").objectReferenceValue = readyBtnText;
            so.FindProperty("startButton").objectReferenceValue = startBtn.GetComponent<Button>();
            so.FindProperty("leaveButton").objectReferenceValue = leaveBtn.GetComponent<Button>();
            so.FindProperty("lobbyTitleText").objectReferenceValue = lobbyTitle;
            so.FindProperty("playerListContainer").objectReferenceValue = listContainer.transform;
            so.FindProperty("playerEntryPrefab").objectReferenceValue = entryPrefab;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, LobbyScenePath);
            Debug.Log($"[LobbySetupTool] Lobby Scene created: {LobbyScenePath}");
        }

        private static void CreateGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 3D Ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(5, 1, 5);

            // 3D Spawn Points
            var spawnRoot = new GameObject("SpawnPoints");
            Vector3[] positions = new[]
            {
                new Vector3(-4f, 0.5f, 0f),
                new Vector3(-1.5f, 0.5f, 0f),
                new Vector3(1.5f, 0.5f, 0f),
                new Vector3(4f, 0.5f, 0f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var spawn = new GameObject($"SpawnPoint_{i + 1}");
                spawn.transform.SetParent(spawnRoot.transform);
                spawn.transform.position = positions[i];
                spawn.AddComponent<NetworkStartPosition>();
            }

            // Ingame Canvas HUD
            var canvasGo = CreateCanvas("IngameHUD");
            var hudText = CreateText(canvasGo.transform, "IngameNotice", "GAME IN PROGRESS", 32, new Vector2(0, 450), new Vector2(500, 60));
            hudText.fontStyle = FontStyles.Bold;

            EditorSceneManager.SaveScene(scene, GameScenePath);
            Debug.Log($"[LobbySetupTool] Game Scene created: {GameScenePath}");
        }

        #endregion

        #region Helpers

        private static GameObject CreateCanvas(string name)
        {
            var canvasGo = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var eventSystemGo = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystemGo == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }

            return canvasGo;
        }

        private static TMP_FontAsset? GetKoreanFontAsset()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/Fonts & Materials/Bold SDF.asset");
            if (font == null)
            {
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/Fonts & Materials/Regular SDF.asset");
            }
            return font;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = panel.GetComponent<Image>();
            img.color = color;
            return panel;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            var font = GetKoreanFontAsset();
            if (font != null) tmp.font = font;

            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return tmp;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.2f, 0.45f, 0.8f, 1f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            var font = GetKoreanFontAsset();
            if (font != null) tmp.font = font;

            tmp.text = label;
            tmp.fontSize = 22;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return go;
        }

        private static void RegisterBuildSettings()
        {
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(LobbyScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[LobbySetupTool] Registered 3 scenes to Build Settings (0: MainMenuScene, 1: LobbyScene, 2: GameScene)");
        }

        #endregion
    }
}
#endif
