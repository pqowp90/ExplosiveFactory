using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public interface IInteractable
{
    void OnWatch();
    void OnNotWatch();
    void OnInteract();
}

public class InteractableObject : NetworkBehaviour, IInteractable
{
    [SerializeField] protected GameObject? RendererObject;
    [SerializeField] protected Color outlineColor = new Color(1f, 0.92f, 0.016f, 1f);
    [SerializeField] protected float outlineWidth = 2.5f;

    protected Renderer[] CachedRenderers = Array.Empty<Renderer>();

    protected virtual void Awake()
    {
        if (RendererObject == null) RendererObject = gameObject;
        CacheRenderers();
    }

    public void CacheRenderers()
    {
        if (RendererObject != null)
        {
            CachedRenderers = RendererObject.GetComponentsInChildren<Renderer>(true);
        }
    }

    public virtual void OnInteract()
    {

    }

    public virtual void OnNotWatch()
    {
        OutlineManager.Hide(CachedRenderers);
    }

    public virtual void OnWatch()
    {
        if (CachedRenderers.Length == 0)
        {
            CacheRenderers();
        }
        OutlineManager.Show(CachedRenderers, outlineColor, outlineWidth);
    }

    protected virtual void OnDisable()
    {
        OutlineManager.Hide(CachedRenderers);
    }
}