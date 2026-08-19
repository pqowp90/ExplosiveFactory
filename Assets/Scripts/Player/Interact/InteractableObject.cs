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
    private static int _noneOutline;
    private static int _outline;
    [SerializeField] protected GameObject RendererObject;

    protected virtual void Awake()
    {
        _noneOutline = LayerMask.NameToLayer("ItemLayer");
        _outline = LayerMask.NameToLayer("Outline");
        if (RendererObject == null) RendererObject = gameObject;
    }
    public virtual void OnInteract()
    {

    }

    public virtual void OnNotWatch()
    {
        RendererObject.layer = _noneOutline;
    }

    public virtual void OnWatch()
    {
        RendererObject.layer = _outline;
    }
}