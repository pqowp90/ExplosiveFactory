using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public interface IInteractable
{
    void OnWatch();
    void OnNotWatch();
    void OnInteract();
}

public class InteractableObject : NetworkBehaviour, IInteractable
{
    private static int _noneOutline = -1;
    private static int _outline = -1;
    [SerializeField] protected GameObject RendererObject;

    protected virtual void Awake()
    {
        int itemLayer = LayerMask.NameToLayer("ItemLayer");
        int outlineLayer = LayerMask.NameToLayer("Outline");
        _noneOutline = itemLayer >= 0 ? itemLayer : 0;
        _outline = outlineLayer >= 0 ? outlineLayer : _noneOutline;
        if (RendererObject == null) RendererObject = gameObject;
    }
    public virtual void OnInteract()
    {

    }

    public virtual void OnNotWatch()
    {
        if (_noneOutline >= 0)
            RendererObject.layer = _noneOutline;
    }

    public virtual void OnWatch()
    {
        if (_outline >= 0)
            RendererObject.layer = _outline;
    }
}