#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ExplosiveFactory.Editor
{
    [InitializeOnLoad]
    public static class PlayModeSelectionCleaner
    {
        static PlayModeSelectionCleaner()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredPlayMode)
            {
                // 플레이 모드 진입 시 인스펙터의 파괴 대기 객체 역참조 예외 방지
                if (Selection.objects != null && Selection.objects.Length > 0)
                {
                    for (int i = 0; i < Selection.objects.Length; i++)
                    {
                        if (Selection.objects[i] == null)
                        {
                            Selection.objects = System.Array.Empty<Object>();
                            break;
                        }
                    }
                }
            }
        }
    }
}
#endif
