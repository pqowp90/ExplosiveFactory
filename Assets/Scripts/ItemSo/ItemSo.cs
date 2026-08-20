using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemSo : ScriptableObject
{
    [Header("기본 정보")]
    public string id = "Item_Default";
    public int itemID = 0;
    public string itemName = "새 아이템";
    public int itemPrice = 100;

    [TextArea(2, 4)]
    public string description = "";
    public Sprite? icon;

    [Header("프리팹 및 물리")]
    public GameObject? prefab;
    [Range(0.1f, 50f)]
    public float weight = 1.0f;
    public bool isCanPickup = true;
    public bool isCanThrow = true;
    public float throwSpeedMultiplier = 1.0f;
    public float collisionRadius = 0.5f;

    [Header("1인칭 파지 및 애니메이션")]
    public AnimatorOverrideController? handAnimatorOverride;
    public Vector3 holdPositionOffset = Vector3.zero;
    public Vector3 holdRotationOffset = Vector3.zero;

    [Header("소멸/디스폰")]
    public bool isVanishable = false;
    public float vanishTime = 30.0f;
}
