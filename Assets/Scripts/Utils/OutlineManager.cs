using System.Collections.Generic;
using UnityEngine;

public class OutlineManager : MonoBehaviour
{
    private static OutlineManager _instance;
    public static OutlineManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<OutlineManager>();
                if (_instance == null && Application.isPlaying)
                {
                    var go = new GameObject("OutlineManager");
                    _instance = go.AddComponent<OutlineManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("Outline Settings")]
    [SerializeField] private Color defaultOutlineColor = new Color(1f, 0.92f, 0.016f, 1f); // 밝은 노랑/골드
    [SerializeField] private float defaultOutlineWidth = 2.5f;

    private readonly HashSet<Renderer> _activeRenderers = new();
    public IReadOnlyCollection<Renderer> ActiveRenderers => _activeRenderers;

    public Color CurrentOutlineColor { get; private set; }
    public float CurrentOutlineWidth { get; private set; }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            CurrentOutlineColor = defaultOutlineColor;
            CurrentOutlineWidth = defaultOutlineWidth;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 지정된 렌더러들에 아웃라인을 활성화합니다.
    /// </summary>
    public static void Show(Renderer[]? renderers, Color? customColor = null, float? customWidth = null)
    {
        if (renderers == null || renderers.Length == 0) return;
        Instance.ShowInternal(renderers, customColor, customWidth);
    }

    /// <summary>
    /// 지정된 렌더러 하나에 아웃라인을 활성화합니다.
    /// </summary>
    public static void Show(Renderer? renderer, Color? customColor = null, float? customWidth = null)
    {
        if (renderer == null) return;
        Instance.ShowInternal(new[] { renderer }, customColor, customWidth);
    }

    /// <summary>
    /// 지정된 렌더러들의 아웃라인을 비활성화합니다.
    /// </summary>
    public static void Hide(Renderer[]? renderers)
    {
        if (renderers == null || renderers.Length == 0) return;
        Instance.HideInternal(renderers);
    }

    /// <summary>
    /// 지정된 렌더러 하나의 아웃라인을 비활성화합니다.
    /// </summary>
    public static void Hide(Renderer? renderer)
    {
        if (renderer == null) return;
        Instance.HideInternal(new[] { renderer });
    }

    /// <summary>
    /// 모든 활성 아웃라인을 제거합니다.
    /// </summary>
    public static void ClearAll()
    {
        if (_instance != null)
        {
            _instance._activeRenderers.Clear();
        }
    }

    private void ShowInternal(Renderer[] renderers, Color? customColor, float? customWidth)
    {
        CurrentOutlineColor = customColor ?? defaultOutlineColor;
        CurrentOutlineWidth = customWidth ?? defaultOutlineWidth;

        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r != null && r.enabled && r.gameObject.activeInHierarchy)
            {
                _activeRenderers.Add(r);
            }
        }
    }

    private void HideInternal(Renderer[] renderers)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r != null)
            {
                _activeRenderers.Remove(r);
            }
        }
    }

    private void LateUpdate()
    {
        // 파괴되었거나 비활성화된 렌더러 자동 정리
        if (_activeRenderers.Count > 0)
        {
            _activeRenderers.RemoveWhere(r => r == null || !r.enabled || !r.gameObject.activeInHierarchy);
        }
    }
}
