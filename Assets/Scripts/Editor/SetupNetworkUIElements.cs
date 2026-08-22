#if UNITY_EDITOR
using System.IO;
using ExplosiveFactory.Network;
using ExplosiveFactory.Network.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ExplosiveFactory.Editor
{
    public static class SetupNetworkUIElements
    {
        [MenuItem("ExplosiveFactory/Setup Network UI into Scenes", false, 100)]
        public static void SetupAll()
        {
            SetupFriendItemPrefab();
            SetupMainMenuScene();
            SetupLobbyScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#00FF88>[SetupNetworkUIElements] All Scenes and Prefabs successfully updated!</color>");
        }

        public static GameObject SetupFriendItemPrefab()
        {
            string prefabPath = "Assets/Prefabs/FriendItem.prefab";
            if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");

            // Create GameObject hierarchy
            var rootObj = new GameObject("FriendItem");
            var rootRect = rootObj.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(580, 64);

            var bgImg = rootObj.AddComponent<Image>();
            bgImg.color = new Color(0.16f, 0.18f, 0.24f, 0.95f);

            var hlg = rootObj.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(12, 12, 8, 8);
            hlg.spacing = 15;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // 1. Avatar (RawImage)
            var avatarObj = new GameObject("Avatar");
            avatarObj.transform.SetParent(rootObj.transform, false);
            var avRect = avatarObj.AddComponent<RectTransform>();
            avRect.sizeDelta = new Vector2(48, 48);
            var rawImg = avatarObj.AddComponent<RawImage>();
            rawImg.color = Color.white;

            // 2. Name Text
            var nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(rootObj.transform, false);
            var nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(360, 48);
            var nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = "Friend Name";
            nameText.fontSize = 20;
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.color = Color.white;
            nameText.overflowMode = TextOverflowModes.Ellipsis;

            // 3. Invite Button
            var inviteBtnObj = new GameObject("InviteButton");
            inviteBtnObj.transform.SetParent(rootObj.transform, false);
            var invRect = inviteBtnObj.AddComponent<RectTransform>();
            invRect.sizeDelta = new Vector2(100, 42);

            var invImg = inviteBtnObj.AddComponent<Image>();
            invImg.color = new Color(0.2f, 0.5f, 0.9f, 1f);
            var btn = inviteBtnObj.AddComponent<Button>();

            var btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(inviteBtnObj.transform, false);
            var btnTextRect = btnTextObj.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.sizeDelta = Vector2.zero;
            var btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnText.text = "초대";
            btnText.fontSize = 18;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.color = Color.white;

            var fo = rootObj.AddComponent<FriendObject>();

            // Serialized link
            var so = new SerializedObject(fo);
            so.FindProperty("inviteBtn").objectReferenceValue = btn;
            so.FindProperty("inviteBtnText").objectReferenceValue = btnText;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(rootObj, prefabPath);
            Object.DestroyImmediate(rootObj);
            Debug.Log($"[SetupNetworkUIElements] Created {prefabPath}");
            return prefab;
        }

        public static void SetupMainMenuScene()
        {
            string scenePath = "Assets/Scenes/MainMenuScene.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var mainMenuUI = Object.FindFirstObjectByType<MainMenuUI>();
            if (mainMenuUI == null)
            {
                Debug.LogError("[SetupNetworkUIElements] MainMenuUI not found in MainMenuScene!");
                return;
            }

            var so = new SerializedObject(mainMenuUI);
            var hostBtnProp = so.FindProperty("hostButton");
            var joinBtnProp = so.FindProperty("joinButton");
            var quitBtnProp = so.FindProperty("quitButton");
            var statusTextProp = so.FindProperty("statusText");
            var versionTextProp = so.FindProperty("versionText");

            var hostBtn = hostBtnProp.objectReferenceValue as Button;
            if (hostBtn != null)
            {
                var parent = hostBtn.transform.parent;
                var hostRect = hostBtn.GetComponent<RectTransform>();
                hostRect.anchoredPosition = new Vector2(0, 30);

                // Join Button
                var existingJoin = parent.Find("JoinButton");
                GameObject joinObj = existingJoin != null ? existingJoin.gameObject : null;
                if (joinObj == null)
                {
                    joinObj = Object.Instantiate(hostBtn.gameObject, parent);
                    joinObj.name = "JoinButton";
                }

                var joinRect = joinObj.GetComponent<RectTransform>();
                joinRect.anchoredPosition = new Vector2(0, -45);
                var joinImg = joinObj.GetComponent<Image>();
                if (joinImg != null) joinImg.color = new Color(0.18f, 0.55f, 0.45f, 1f);

                var joinText = joinObj.GetComponentInChildren<TextMeshProUGUI>();
                if (joinText != null) joinText.text = "방 참가";

                var joinBtn = joinObj.GetComponent<Button>();
                joinBtnProp.objectReferenceValue = joinBtn;

                // Quit Button
                var quitBtn = quitBtnProp.objectReferenceValue as Button;
                if (quitBtn != null)
                {
                    quitBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -120);
                }

                // Status Text
                var versionText = versionTextProp.objectReferenceValue as TextMeshProUGUI;
                if (versionText != null)
                {
                    var existingStatus = parent.Find("SteamStatusText");
                    GameObject statusObj = existingStatus != null ? existingStatus.gameObject : null;
                    if (statusObj == null)
                    {
                        statusObj = Object.Instantiate(versionText.gameObject, parent);
                        statusObj.name = "SteamStatusText";
                    }

                    var statusRect = statusObj.GetComponent<RectTransform>();
                    statusRect.anchoredPosition = new Vector2(0, -200);
                    statusRect.sizeDelta = new Vector2(600, 40);

                    var st = statusObj.GetComponent<TextMeshProUGUI>();
                    st.fontSize = 20;
                    st.alignment = TextAlignmentOptions.Center;
                    statusTextProp.objectReferenceValue = st;
                }

                // --- Join Modal Popup Panel ---
                var canvas = mainMenuUI.GetComponent<Canvas>() ?? mainMenuUI.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    var existingPopup = canvas.transform.Find("JoinPopup");
                    GameObject popupObj = existingPopup != null ? existingPopup.gameObject : null;

                    if (popupObj == null)
                    {
                        popupObj = new GameObject("JoinPopup");
                        popupObj.transform.SetParent(canvas.transform, false);
                        var popRect = popupObj.AddComponent<RectTransform>();
                        popRect.anchorMin = Vector2.zero;
                        popRect.anchorMax = Vector2.one;
                        popRect.sizeDelta = Vector2.zero;

                        var bg = popupObj.AddComponent<Image>();
                        bg.color = new Color(0, 0, 0, 0.65f);

                        // Window Panel
                        var winObj = new GameObject("WindowPanel");
                        winObj.transform.SetParent(popupObj.transform, false);
                        var winRect = winObj.AddComponent<RectTransform>();
                        winRect.anchorMin = new Vector2(0.5f, 0.5f);
                        winRect.anchorMax = new Vector2(0.5f, 0.5f);
                        winRect.sizeDelta = new Vector2(560, 320);

                        var winImg = winObj.AddComponent<Image>();
                        winImg.color = new Color(0.12f, 0.14f, 0.18f, 0.98f);

                        // Header Panel
                        var headerObj = new GameObject("Header");
                        headerObj.transform.SetParent(winObj.transform, false);
                        var headRect = headerObj.AddComponent<RectTransform>();
                        headRect.anchorMin = new Vector2(0, 1);
                        headRect.anchorMax = new Vector2(1, 1);
                        headRect.anchoredPosition = new Vector2(0, -30);
                        headRect.sizeDelta = new Vector2(0, 60);

                        var headImg = headerObj.AddComponent<Image>();
                        headImg.color = new Color(0.18f, 0.22f, 0.28f, 1f);

                        // Title
                        var titleObj = new GameObject("TitleText");
                        titleObj.transform.SetParent(headerObj.transform, false);
                        var titleRect = titleObj.AddComponent<RectTransform>();
                        titleRect.anchorMin = new Vector2(0, 0);
                        titleRect.anchorMax = new Vector2(0.8f, 1);
                        titleRect.anchoredPosition = new Vector2(20, 0);
                        var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
                        titleTxt.text = "방 참가";
                        titleTxt.fontSize = 22;
                        titleTxt.fontStyle = FontStyles.Bold;
                        titleTxt.alignment = TextAlignmentOptions.MidlineLeft;
                        titleTxt.color = Color.white;

                        // Close Button (X)
                        var closeBtnObj = new GameObject("CloseButton");
                        closeBtnObj.transform.SetParent(headerObj.transform, false);
                        var closeRect = closeBtnObj.AddComponent<RectTransform>();
                        closeRect.anchorMin = new Vector2(1, 0.5f);
                        closeRect.anchorMax = new Vector2(1, 0.5f);
                        closeRect.anchoredPosition = new Vector2(-25, 0);
                        closeRect.sizeDelta = new Vector2(36, 36);

                        var closeImg = closeBtnObj.AddComponent<Image>();
                        closeImg.color = new Color(0.8f, 0.25f, 0.25f, 1f);
                        closeBtnObj.AddComponent<Button>();

                        var closeTxtObj = new GameObject("Text");
                        closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
                        var closeTxtRect = closeTxtObj.AddComponent<RectTransform>();
                        closeTxtRect.anchorMin = Vector2.zero;
                        closeTxtRect.anchorMax = Vector2.one;
                        closeTxtRect.sizeDelta = Vector2.zero;
                        var closeTxt = closeTxtObj.AddComponent<TextMeshProUGUI>();
                        closeTxt.text = "X";
                        closeTxt.fontSize = 18;
                        closeTxt.alignment = TextAlignmentOptions.Center;
                        closeTxt.color = Color.white;

                        // Input Field Row Container
                        var inputRow = new GameObject("InputRow");
                        inputRow.transform.SetParent(winObj.transform, false);
                        var rowRect = inputRow.AddComponent<RectTransform>();
                        rowRect.anchoredPosition = new Vector2(0, 30);
                        rowRect.sizeDelta = new Vector2(500, 52);

                        // Input Field
                        var inputObj = new GameObject("LobbyIdInputField");
                        inputObj.transform.SetParent(inputRow.transform, false);
                        var inputRect = inputObj.AddComponent<RectTransform>();
                        inputRect.anchorMin = new Vector2(0, 0.5f);
                        inputRect.anchorMax = new Vector2(0, 0.5f);
                        inputRect.anchoredPosition = new Vector2(185, 0);
                        inputRect.sizeDelta = new Vector2(370, 50);

                        var inputBg = inputObj.AddComponent<Image>();
                        inputBg.color = new Color(0.08f, 0.09f, 0.12f, 1f);

                        var tmpInput = inputObj.AddComponent<TMP_InputField>();

                        // Text Component
                        var textObj = new GameObject("Text");
                        textObj.transform.SetParent(inputObj.transform, false);
                        var tRect = textObj.AddComponent<RectTransform>();
                        tRect.anchorMin = Vector2.zero;
                        tRect.anchorMax = Vector2.one;
                        tRect.offsetMin = new Vector2(10, 0);
                        tRect.offsetMax = new Vector2(-10, 0);
                        var tmpText = textObj.AddComponent<TextMeshProUGUI>();
                        tmpText.fontSize = 20;
                        tmpText.alignment = TextAlignmentOptions.MidlineLeft;
                        tmpText.color = Color.white;

                        // Placeholder Component
                        var phObj = new GameObject("Placeholder");
                        phObj.transform.SetParent(inputObj.transform, false);
                        var phRect = phObj.AddComponent<RectTransform>();
                        phRect.anchorMin = Vector2.zero;
                        phRect.anchorMax = Vector2.one;
                        phRect.offsetMin = new Vector2(10, 0);
                        phRect.offsetMax = new Vector2(-10, 0);
                        var phText = phObj.AddComponent<TextMeshProUGUI>();
                        phText.text = "로비 ID 입력...";
                        phText.fontSize = 18;
                        phText.fontStyle = FontStyles.Italic;
                        phText.alignment = TextAlignmentOptions.MidlineLeft;
                        phText.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);

                        tmpInput.textComponent = tmpText;
                        tmpInput.placeholder = phText;

                        // Paste Button
                        var pasteBtnObj = new GameObject("PasteButton");
                        pasteBtnObj.transform.SetParent(inputRow.transform, false);
                        var pasteRect = pasteBtnObj.AddComponent<RectTransform>();
                        pasteRect.anchorMin = new Vector2(1, 0.5f);
                        pasteRect.anchorMax = new Vector2(1, 0.5f);
                        pasteRect.anchoredPosition = new Vector2(-55, 0);
                        pasteRect.sizeDelta = new Vector2(110, 50);

                        var pasteImg = pasteBtnObj.AddComponent<Image>();
                        pasteImg.color = new Color(0.18f, 0.55f, 0.45f, 1f);
                        pasteBtnObj.AddComponent<Button>();

                        var pasteTxtObj = new GameObject("Text");
                        pasteTxtObj.transform.SetParent(pasteBtnObj.transform, false);
                        var pasteTxtRect = pasteTxtObj.AddComponent<RectTransform>();
                        pasteTxtRect.anchorMin = Vector2.zero;
                        pasteTxtRect.anchorMax = Vector2.one;
                        pasteTxtRect.sizeDelta = Vector2.zero;
                        var pasteTxt = pasteTxtObj.AddComponent<TextMeshProUGUI>();
                        pasteTxt.text = "붙여넣기";
                        pasteTxt.fontSize = 16;
                        pasteTxt.alignment = TextAlignmentOptions.Center;
                        pasteTxt.color = Color.white;

                        // Bottom Button Row
                        var btnRow = new GameObject("ButtonRow");
                        btnRow.transform.SetParent(winObj.transform, false);
                        var bRowRect = btnRow.AddComponent<RectTransform>();
                        bRowRect.anchoredPosition = new Vector2(0, -65);
                        bRowRect.sizeDelta = new Vector2(500, 55);

                        // Confirm Join Button
                        var confirmObj = new GameObject("ConfirmJoinButton");
                        confirmObj.transform.SetParent(btnRow.transform, false);
                        var confRect = confirmObj.AddComponent<RectTransform>();
                        confRect.anchoredPosition = new Vector2(-80, 0);
                        confRect.sizeDelta = new Vector2(200, 50);

                        var confImg = confirmObj.AddComponent<Image>();
                        confImg.color = new Color(0.2f, 0.5f, 0.9f, 1f);
                        confirmObj.AddComponent<Button>();

                        var confTxtObj = new GameObject("Text");
                        confTxtObj.transform.SetParent(confirmObj.transform, false);
                        var confTxtRect = confTxtObj.AddComponent<RectTransform>();
                        confTxtRect.anchorMin = Vector2.zero;
                        confTxtRect.anchorMax = Vector2.one;
                        confTxtRect.sizeDelta = Vector2.zero;
                        var confTxt = confTxtObj.AddComponent<TextMeshProUGUI>();
                        confTxt.text = "입장하기";
                        confTxt.fontSize = 20;
                        confTxt.alignment = TextAlignmentOptions.Center;
                        confTxt.color = Color.white;

                        // Cancel Button
                        var cancelObj = new GameObject("CancelButton");
                        cancelObj.transform.SetParent(btnRow.transform, false);
                        var cancRect = cancelObj.AddComponent<RectTransform>();
                        cancRect.anchoredPosition = new Vector2(130, 0);
                        cancRect.sizeDelta = new Vector2(140, 50);

                        var cancImg = cancelObj.AddComponent<Image>();
                        cancImg.color = new Color(0.35f, 0.38f, 0.45f, 1f);
                        cancelObj.AddComponent<Button>();

                        var cancTxtObj = new GameObject("Text");
                        cancTxtObj.transform.SetParent(cancelObj.transform, false);
                        var cancTxtRect = cancTxtObj.AddComponent<RectTransform>();
                        cancTxtRect.anchorMin = Vector2.zero;
                        cancTxtRect.anchorMax = Vector2.one;
                        cancTxtRect.sizeDelta = Vector2.zero;
                        var cancTxt = cancTxtObj.AddComponent<TextMeshProUGUI>();
                        cancTxt.text = "취소";
                        cancTxt.fontSize = 18;
                        cancTxt.alignment = TextAlignmentOptions.Center;
                        cancTxt.color = Color.white;
                    }

                    popupObj.SetActive(false);

                    // Serialize bindings
                    so.FindProperty("joinPopupPanel").objectReferenceValue = popupObj;
                    so.FindProperty("lobbyIdInputField").objectReferenceValue = popupObj.transform.Find("WindowPanel/InputRow/LobbyIdInputField")?.GetComponent<TMP_InputField>();
                    so.FindProperty("pasteButton").objectReferenceValue = popupObj.transform.Find("WindowPanel/InputRow/PasteButton")?.GetComponent<Button>();
                    so.FindProperty("confirmJoinButton").objectReferenceValue = popupObj.transform.Find("WindowPanel/ButtonRow/ConfirmJoinButton")?.GetComponent<Button>();
                    so.FindProperty("cancelButton").objectReferenceValue = popupObj.transform.Find("WindowPanel/ButtonRow/CancelButton")?.GetComponent<Button>();
                    so.FindProperty("closeJoinPopupButton").objectReferenceValue = popupObj.transform.Find("WindowPanel/Header/CloseButton")?.GetComponent<Button>();
                }
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SetupNetworkUIElements] Updated MainMenuScene.unity with Join Modal Popup");
        }

        public static void SetupLobbyScene()
        {
            string scenePath = "Assets/Scenes/LobbyScene.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var lobbyUI = Object.FindFirstObjectByType<LobbyUI>();
            if (lobbyUI == null)
            {
                Debug.LogError("[SetupNetworkUIElements] LobbyUI not found in LobbyScene!");
                return;
            }

            var canvas = lobbyUI.GetComponent<Canvas>() ?? lobbyUI.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            var friendItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FriendItem.prefab");

            // Find or Create FriendsPopup Panel in Canvas
            var existingPopup = canvas.transform.Find("FriendsPopup");
            GameObject popupObj = existingPopup != null ? existingPopup.gameObject : null;

            if (popupObj == null)
            {
                // 1. Root Modal Overlay
                popupObj = new GameObject("FriendsPopup");
                popupObj.transform.SetParent(canvas.transform, false);
                var rootRect = popupObj.AddComponent<RectTransform>();
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.sizeDelta = Vector2.zero;

                var bgImg = popupObj.AddComponent<Image>();
                bgImg.color = new Color(0, 0, 0, 0.65f);

                // 2. Window Panel
                var winObj = new GameObject("WindowPanel");
                winObj.transform.SetParent(popupObj.transform, false);
                var winRect = winObj.AddComponent<RectTransform>();
                winRect.anchorMin = new Vector2(0.5f, 0.5f);
                winRect.anchorMax = new Vector2(0.5f, 0.5f);
                winRect.sizeDelta = new Vector2(650, 680);

                var winImg = winObj.AddComponent<Image>();
                winImg.color = new Color(0.12f, 0.14f, 0.18f, 0.98f);

                // 3. Header Panel
                var headerObj = new GameObject("Header");
                headerObj.transform.SetParent(winObj.transform, false);
                var headRect = headerObj.AddComponent<RectTransform>();
                headRect.anchorMin = new Vector2(0, 1);
                headRect.anchorMax = new Vector2(1, 1);
                headRect.anchoredPosition = new Vector2(0, -35);
                headRect.sizeDelta = new Vector2(0, 70);

                var headImg = headerObj.AddComponent<Image>();
                headImg.color = new Color(0.18f, 0.22f, 0.28f, 1f);

                // Title
                var titleObj = new GameObject("TitleText");
                titleObj.transform.SetParent(headerObj.transform, false);
                var titleRect = titleObj.AddComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0, 0);
                titleRect.anchorMax = new Vector2(0.6f, 1);
                titleRect.anchoredPosition = new Vector2(25, 0);
                var titleText = titleObj.AddComponent<TextMeshProUGUI>();
                titleText.text = "친구 초대";
                titleText.fontSize = 24;
                titleText.fontStyle = FontStyles.Bold;
                titleText.alignment = TextAlignmentOptions.MidlineLeft;
                titleText.color = Color.white;

                // Copy Button
                var copyBtnObj = new GameObject("CopyCodeButton");
                copyBtnObj.transform.SetParent(headerObj.transform, false);
                var copyRect = copyBtnObj.AddComponent<RectTransform>();
                copyRect.anchorMin = new Vector2(0.62f, 0.5f);
                copyRect.anchorMax = new Vector2(0.62f, 0.5f);
                copyRect.anchoredPosition = new Vector2(60, 0);
                copyRect.sizeDelta = new Vector2(150, 42);

                var copyImg = copyBtnObj.AddComponent<Image>();
                copyImg.color = new Color(0.2f, 0.6f, 0.5f, 1f);
                var copyBtn = copyBtnObj.AddComponent<Button>();

                var copyBtnTextObj = new GameObject("Text");
                copyBtnTextObj.transform.SetParent(copyBtnObj.transform, false);
                var copyTextRect = copyBtnTextObj.AddComponent<RectTransform>();
                copyTextRect.anchorMin = Vector2.zero;
                copyTextRect.anchorMax = Vector2.one;
                copyTextRect.sizeDelta = Vector2.zero;
                var copyText = copyBtnTextObj.AddComponent<TextMeshProUGUI>();
                copyText.text = "로비 ID 복사";
                copyText.fontSize = 16;
                copyText.alignment = TextAlignmentOptions.Center;
                copyText.color = Color.white;

                // Close Button
                var closeBtnObj = new GameObject("CloseButton");
                closeBtnObj.transform.SetParent(headerObj.transform, false);
                var closeRect = closeBtnObj.AddComponent<RectTransform>();
                closeRect.anchorMin = new Vector2(1, 0.5f);
                closeRect.anchorMax = new Vector2(1, 0.5f);
                closeRect.anchoredPosition = new Vector2(-30, 0);
                closeRect.sizeDelta = new Vector2(42, 42);

                var closeImg = closeBtnObj.AddComponent<Image>();
                closeImg.color = new Color(0.8f, 0.25f, 0.25f, 1f);
                var closeBtn = closeBtnObj.AddComponent<Button>();

                var closeTextObj = new GameObject("Text");
                closeTextObj.transform.SetParent(closeBtnObj.transform, false);
                var closeTextRect = closeTextObj.AddComponent<RectTransform>();
                closeTextRect.anchorMin = Vector2.zero;
                closeTextRect.anchorMax = Vector2.one;
                closeTextRect.sizeDelta = Vector2.zero;
                var closeText = closeTextObj.AddComponent<TextMeshProUGUI>();
                closeText.text = "X";
                closeText.fontSize = 20;
                closeText.alignment = TextAlignmentOptions.Center;
                closeText.color = Color.white;

                // 4. Scroll View
                var scrollObj = new GameObject("ScrollView");
                scrollObj.transform.SetParent(winObj.transform, false);
                var scrollRect = scrollObj.AddComponent<RectTransform>();
                scrollRect.anchorMin = new Vector2(0, 0);
                scrollRect.anchorMax = new Vector2(1, 1);
                scrollRect.offsetMin = new Vector2(20, 20);
                scrollRect.offsetMax = new Vector2(-20, -80);

                var scrollRectComp = scrollObj.AddComponent<ScrollRect>();
                scrollRectComp.horizontal = false;
                scrollRectComp.vertical = true;
                scrollRectComp.movementType = ScrollRect.MovementType.Clamped;
                scrollRectComp.scrollSensitivity = 30f;

                // Viewport
                var viewObj = new GameObject("Viewport");
                viewObj.transform.SetParent(scrollObj.transform, false);
                var viewRect = viewObj.AddComponent<RectTransform>();
                viewRect.anchorMin = Vector2.zero;
                viewRect.anchorMax = Vector2.one;
                viewRect.sizeDelta = Vector2.zero;
                viewObj.AddComponent<RectMask2D>();
                scrollRectComp.viewport = viewRect;

                // Content
                var contentObj = new GameObject("Content");
                contentObj.transform.SetParent(viewObj.transform, false);
                var contentRect = contentObj.AddComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0, 1);
                contentRect.anchorMax = new Vector2(1, 1);
                contentRect.pivot = new Vector2(0.5f, 1);
                contentRect.sizeDelta = new Vector2(0, 0);

                var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 8;
                vlg.padding = new RectOffset(5, 5, 10, 10);
                vlg.childControlHeight = false;
                vlg.childControlWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.childForceExpandWidth = true;

                var csf = contentObj.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                scrollRectComp.content = contentRect;
            }

            popupObj.SetActive(false);

            // Bind LobbyUI serialized properties
            var so = new SerializedObject(lobbyUI);
            so.FindProperty("friendsPopupPanel").objectReferenceValue = popupObj;
            so.FindProperty("friendsContent").objectReferenceValue = popupObj.transform.Find("WindowPanel/ScrollView/Viewport/Content");
            so.FindProperty("friendItemPrefab").objectReferenceValue = friendItemPrefab;
            so.FindProperty("closeFriendsPopupButton").objectReferenceValue = popupObj.transform.Find("WindowPanel/Header/CloseButton")?.GetComponent<Button>();
            so.FindProperty("copyLobbyIdButton").objectReferenceValue = popupObj.transform.Find("WindowPanel/Header/CopyCodeButton")?.GetComponent<Button>();
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SetupNetworkUIElements] Updated LobbyScene.unity");
        }
    }
}
#endif
