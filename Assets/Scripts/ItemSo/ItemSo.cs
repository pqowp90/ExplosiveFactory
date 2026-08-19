using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemSo : ScriptableObject
{
    public int itemID;
    public string itemName;
    public int itemPrice;
}
