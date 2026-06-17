using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Add this component to an empty GameObject in any scene.
/// It defines everything that room needs: which items are available,
/// which recipes can be ordered, and per-station item whitelists.
/// 
/// RoomManager reads this on Start and configures the scene automatically.
/// 
/// Setup:
/// 1. Create an empty GameObject, name it "RoomConfig"
/// 2. Add this component
/// 3. Fill in availableItems, recipes, stationConfigs in the Inspector
/// </summary>
public class RoomConfig : MonoBehaviour
{
    [Header("Room Identity")]
    public string roomName = "New Room";

    [Header("Items available in this room")]
    [Tooltip("All ItemData that can appear in this room (dispensed or pre-placed)")]
    public List<ItemData> availableItems = new List<ItemData>();

    [Header("Recipes (what orders can appear)")]
    public bool useProceduralGeneration = true;
    public List<RoomRecipe> recipes = new List<RoomRecipe>();

    [Header("Station Configs")]
    [Tooltip("Defines which items each station type accepts in this room")]
    public List<StationItemConfig> stationConfigs = new List<StationItemConfig>();

    /// <summary>
    /// Returns the whitelist for a given station type in this room.
    /// Empty list means the station accepts anything from availableItems.
    /// </summary>
    public List<ItemData> GetWhitelistForStation(StationType type)
    {
        foreach (var config in stationConfigs)
        {
            if (config.stationType == type)
                return config.allowedItems;
        }
        return new List<ItemData>(); // no restriction
    }
}

/// <summary>
/// A recipe that can appear as an order in this room.
/// </summary>
[System.Serializable]
public class RoomRecipe
{
    public string recipeName;
    [Tooltip("The final item the player must hand in to complete this order")]
    public ItemData requiredOutput;
    public int scoreValue = 10;
    [Tooltip("How many seconds the player has to complete this order before it expires")]
    public float timeLimit = 60f;
}

/// <summary>
/// Defines which items a specific station type accepts in this room.
/// </summary>
[System.Serializable]
public class StationItemConfig
{
    public StationType stationType;
    [Tooltip("Leave empty to allow all availableItems from the room")]
    public List<ItemData> allowedItems = new List<ItemData>();
}