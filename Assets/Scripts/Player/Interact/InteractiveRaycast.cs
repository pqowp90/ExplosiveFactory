using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractiveRaycast : MonoBehaviour
{
    [SerializeField] private LayerMask itemLayer = ~0;
    [SerializeField] private float interactRange = 3.5f;
    [SerializeField] private TextMeshProUGUI? interactText;
    private Player? _player;

    private void Awake()
    {
        _player = GetComponentInParent<Player>();
    }

    private void Update()
    {
        if (_player == null || !_player.isLocalPlayer) return;
        Interact();
    }

    private IInteractable? _currentInteractable;

    private void Interact()
    {
        if (_player == null) return;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, interactRange, itemLayer))
        {
            if (hit.collider != null)
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (_currentInteractable != interactable)
                {
                    if (_currentInteractable != null)
                    {
                        _currentInteractable.OnNotWatch();
                    }
                }

                _currentInteractable = interactable;
                if (_currentInteractable != null)
                {
                    _currentInteractable.OnWatch();

                    bool interactTriggered = _player.InputController != null && _player.InputController.InteractAction != null && _player.InputController.InteractAction.triggered;
                    if (!interactTriggered && Keyboard.current != null)
                    {
                        interactTriggered = Keyboard.current.fKey.wasPressedThisFrame;
                    }

                    if (interactTriggered)
                    {
                        if (_currentInteractable is Item item)
                        {
                            if (_player.ItemHolder != null)
                            {
                                _player.ItemHolder.PickUpItem(item);
                            }
                        }
                        _currentInteractable.OnInteract();
                    }
                }
            }
        }
        else
        {
            if (_currentInteractable != null)
            {
                _currentInteractable.OnNotWatch();
                _currentInteractable = null;
            }
        }

        bool dropTriggered = Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame;
        if (dropTriggered && _player.ItemHolder != null)
        {
            _player.ItemHolder.DropItem();
        }
    }
}
