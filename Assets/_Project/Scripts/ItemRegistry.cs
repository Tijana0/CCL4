using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The single global list of all "official" items in the game.
/// Create via: Right Click > Create > HarryPotter > ItemRegistry
/// 
/// This is your master list. Only items in here are considered
/// active in the game. Use the ItemRegistryBuilder (Tools menu)
/// to scan the project and add discovered items.
/// </summary>
[CreateAssetMenu(fileName = "ItemRegistry", menuName = "HarryPotter/ItemRegistry")]
public class ItemRegistry : ScriptableObject
{
    public static ItemRegistry Instance { get; private set; }

    [Header("All official game items")]
    public List<ItemData> items = new List<ItemData>();

    private Dictionary<string, ItemData> _lookup;

    private void OnEnable()
    {
        Instance = this;
        BuildLookup();
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<string, ItemData>();
        foreach (var item in items)
        {
            if (item != null && !_lookup.ContainsKey(item.itemName))
                _lookup[item.itemName] = item;
        }
    }

    /// <summary>Get an item by name. Returns null if not found.</summary>
    public ItemData Get(string itemName)
    {
        if (_lookup == null) BuildLookup();
        _lookup.TryGetValue(itemName, out var result);
        return result;
    }

    /// <summary>Returns true if the item is registered.</summary>
    public bool Contains(ItemData item) => items.Contains(item);
}