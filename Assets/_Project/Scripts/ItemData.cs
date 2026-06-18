using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines what an item is, what it looks like, and what can be done to it.
/// Create via: Right Click > Create > HarryPotter > ItemData
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "HarryPotter/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemName;
    public Sprite icon;
    public GameObject prefab; // The physical prefab spawned in the world

    [Header("Processing Rules")]
    // What this item becomes at each station type. Leave empty if not processable there.
    public List<ProcessingRule> processingRules = new List<ProcessingRule>();

    [Header("Combination Rules")]
    // What this item can combine with (on counters or in cauldron)
    public List<CombineRule> combineRules = new List<CombineRule>();

    [Header("Station Restrictions")]
    // Which station types this item is ALLOWED to be placed on.
    // Leave empty to allow placement on any station (counters, etc.)
    // Items that require processing first should restrict their valid station types.
    public List<StationType> allowedStationTypes = new List<StationType>();
    // If true, this item CANNOT be placed in the cauldron without being processed first
    public bool requiresProcessingBeforeCauldron = false;
    [Tooltip("If false, this item cannot be thrown in the Bin. Use for permanent scene items like buckets or teapots.")]
    public bool isDisposable = true;

    /// <summary>
    /// Returns the output item for a given station type, or null if not processable there.
    /// </summary>
    public ItemData GetProcessingResult(StationType stationType)
    {
        foreach (var rule in processingRules)
        {
            if (rule.stationType == stationType)
                return rule.outputItem;
        }
        return null;
    }

    /// <summary>
    /// Returns the combined item when this item meets another, or null if no rule exists.
    /// </summary>
    public ItemData GetCombineResult(ItemData other)
    {
        foreach (var rule in combineRules)
        {
            if (rule.otherItem == other)
                return rule.outputItem;
        }
        // Also check the other item's rules in case it's defined there
        foreach (var rule in other.combineRules)
        {
            if (rule.otherItem == this)
                return rule.outputItem;
        }
        return null;
    }

    /// <summary>
    /// Whether this item can be placed on a given station type.
    /// </summary>
    public bool CanBePlacedOn(StationType stationType)
    {
        if (allowedStationTypes == null || allowedStationTypes.Count == 0)
            return true; // No restrictions — can go anywhere
        return allowedStationTypes.Contains(stationType);
    }
}

[System.Serializable]
public class ProcessingRule
{
    public StationType stationType;
    public ItemData outputItem;
    [Tooltip("How long in seconds to process. 0 = instant.")]
    public float processingTime = 2f;
}

[System.Serializable]
public class CombineRule
{
    public ItemData otherItem;
    public ItemData outputItem;
}