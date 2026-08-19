using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class PrefabLabelPostProcessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (var importedAsset in importedAssets)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(importedAsset);
            if (go == null) continue;

            OnProcessPrefab(go);
        }
    }

    private static void OnProcessPrefab(GameObject go)
    {
        var labels = Array.Empty<string>();
        var components = go.GetComponentsInChildren<Component>(true);

        foreach (var component in components)
        {
            if (component == null) continue;

            var type = component.GetType();
            if (type.GetCustomAttributes(typeof(PrefabLabelAttribute), true) is not PrefabLabelAttribute[] attribute)
                continue;

            foreach (var prefabLabelAttribute in attribute)
            {
                var label = prefabLabelAttribute.Label;

                if (labels.Contains(label)) continue;

                labels = labels.Append(label).ToArray();
            }
        }

        AssetDatabase.SetLabels(go, labels);
    }
}