using System;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
public class ItemVendingMachine : InteractableObject
{
    [Header("Dispenser Config")]
    [Tooltip("\uc790\ud310\uae30\uc5d0\uc11c \ub4f1\uc744 \uc218 \uc788\ub294 \uc544\uc774\ud15c \ud504\ub9ac\ud32c \ubaa9\ub85d")]
    [SerializeField] private List<GameObject> availableItems = new();
    [SerializeField] private Transform? spawnPoint;
    [SerializeField] private Vector3 spawnPopVelocity = new(0f, 2.5f, 1.5f);
    [SerializeField] private float cooldown = 0.5f;

    [Header("UI & Visuals")]
    [SerializeField] private TextMeshPro? promptWorldText;
    [SerializeField] private string promptMessage = "[F] \uc544\uc774\ud15c \ub4f1\uae30";
    [SerializeField] private AudioSource? audioSource;
    [SerializeField] private AudioClip? dispenseSound;

    private float _lastDispenseTime = -99f;
    private int _currentItemIndex = 0;

    protected override void Awake()
    {
        base.Awake();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (spawnPoint == null) spawnPoint = transform.Find("SpawnPoint") ?? transform;
    }

    private void Start()
    {
        if (availableItems.Count == 0)
        {
            var flashPrefab = Resources.Load<GameObject>("Network/Item_Flashlight");
            var phonePrefab = Resources.Load<GameObject>("Network/Item_Phone");
            if (flashPrefab != null) availableItems.Add(flashPrefab);
            if (phonePrefab != null) availableItems.Add(phonePrefab);
        }

        UpdatePromptText();

        // \uc2dc\uc791\uc2dc \ud504\ub86c\ud504\ud2b8 \uc228\uae40 (\uad00\ub828 \uc74c\uc2dc\uc5d0\ub9cc \ud45c\uc2dc)
        if (promptWorldText != null)
            promptWorldText.gameObject.SetActive(false);
    }

    private void UpdatePromptText()
    {
        if (promptWorldText != null)
        {
            if (availableItems.Count > 0 && availableItems[_currentItemIndex] != null)
            {
                promptWorldText.text = $"{promptMessage}\n<size=70%>({availableItems[_currentItemIndex].name})</size>";
            }
            else
            {
                promptWorldText.text = promptMessage;
            }
        }
    }

    private void Update()
    {
        if (promptWorldText != null && promptWorldText.gameObject.activeSelf && Camera.main != null)
        {
            promptWorldText.transform.rotation = Camera.main.transform.rotation;
        }
    }

    public override void OnWatch()
    {
        base.OnWatch();
        if (promptWorldText != null)
            promptWorldText.gameObject.SetActive(true);
    }

    public override void OnNotWatch()
    {
        base.OnNotWatch();
        if (promptWorldText != null)
            promptWorldText.gameObject.SetActive(false);
    }

    public override void OnInteract()
    {
        base.OnInteract();

        if (Time.time < _lastDispenseTime + cooldown) return;
        _lastDispenseTime = Time.time;

        CmdDispenseItem(_currentItemIndex);

        if (availableItems.Count > 1)
        {
            _currentItemIndex = (_currentItemIndex + 1) % availableItems.Count;
            UpdatePromptText();
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdDispenseItem(int itemIndex)
    {
        if (availableItems.Count == 0)
        {
            Debug.LogWarning("[ItemVendingMachine] availableItems is empty on server! Check prefab serialization.");
            return;
        }
        if (itemIndex < 0 || itemIndex >= availableItems.Count) itemIndex = 0;

        var prefab = availableItems[itemIndex];
        if (prefab == null)
        {
            Debug.LogWarning($"[ItemVendingMachine] prefab at index {itemIndex} is null on server!");
            return;
        }

        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position + transform.forward * 0.8f + Vector3.up * 0.5f;
        Quaternion spawnRot = Quaternion.identity;

        var obj = Instantiate(prefab, spawnPos, spawnRot);
        NetworkServer.Spawn(obj);

        var rb = obj.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = transform.TransformDirection(spawnPopVelocity);
            rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 4f;
        }

        RpcOnItemDispensed();
        Debug.Log($"[ItemVendingMachine] Dispensed network item: {prefab.name} at {spawnPos}");
    }

    [ClientRpc]
    private void RpcOnItemDispensed()
    {
        if (audioSource != null && dispenseSound != null)
        {
            audioSource.PlayOneShot(dispenseSound);
        }
    }
}