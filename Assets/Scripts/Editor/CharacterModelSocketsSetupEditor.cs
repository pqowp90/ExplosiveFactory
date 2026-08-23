#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CharacterModelSocketsSetupEditor
{
    static CharacterModelSocketsSetupEditor()
    {
        EditorApplication.delayCall += SetupAllSkinPrefabs;
    }

    [MenuItem("Tools/ExplosiveFactory/Setup Character Model Sockets")]
    public static void SetupAllSkinPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Skin" });
        foreach (var guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var root = scope.prefabContentsRoot;
                var sockets = root.GetComponent<CharacterModelSockets>();
                if (sockets == null)
                {
                    sockets = root.AddComponent<CharacterModelSockets>();
                }

                var animator = root.GetComponentInChildren<Animator>();
                if (animator != null && animator.isHuman)
                {
                    var serializedObject = new SerializedObject(sockets);
                    var rightProp = serializedObject.FindProperty("_rightHandSocket");
                    var leftProp = serializedObject.FindProperty("_leftHandSocket");

                    var rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                    if (rightHand != null)
                    {
                        var socket = rightHand.Find("ItemSocket_Right");
                        if (socket == null)
                        {
                            var newSocket = new GameObject("ItemSocket_Right");
                            newSocket.transform.SetParent(rightHand, false);
                            socket = newSocket.transform;
                        }
                        CharacterModelSockets.NormalizeSocket(socket);
                        rightProp.objectReferenceValue = socket;
                    }

                    var leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                    if (leftHand != null)
                    {
                        var socket = leftHand.Find("ItemSocket_Left");
                        if (socket == null)
                        {
                            var newSocket = new GameObject("ItemSocket_Left");
                            newSocket.transform.SetParent(leftHand, false);
                            socket = newSocket.transform;
                        }
                        CharacterModelSockets.NormalizeSocket(socket);
                        leftProp.objectReferenceValue = socket;
                    }

                    serializedObject.ApplyModifiedProperties();
                    Debug.Log($"[CharacterModelSocketsSetupEditor] Sockets configured for: {path}");
                }
            }
        }
        AssetDatabase.SaveAssets();
    }
}
#endif
