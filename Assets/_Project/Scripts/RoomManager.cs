using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Add this alongside RoomConfig on the same GameObject.
/// On Start it reads the RoomConfig and:
///   1. Tells every StationBase in the scene its item whitelist
///   2. Registers the room's recipes with the OrderManager
///   3. Seeds any ItemContainerStation dispensers with their assigned items
/// 
/// This means you never hardcode item lists on individual stations —
/// it all flows from the RoomConfig.
/// </summary>
[RequireComponent(typeof(RoomConfig))]
public class RoomManager : MonoBehaviour
{
    [Header("References (auto-found if left empty)")]
    public OrderManager orderManager;

    private RoomConfig config;

    private void Awake()
    {
        config = GetComponent<RoomConfig>();
    }

    private void Start()
    {
        if (orderManager == null)
            orderManager = FindFirstObjectByType<OrderManager>();

        ConfigureStations();
        RegisterRecipes();

        Debug.Log($"[RoomManager] Room '{config.roomName}' initialised. " +
                  $"{config.availableItems.Count} items, {config.recipes.Count} recipes.");
    }

    /// <summary>
    /// Finds all StationBase instances in the scene and applies the
    /// item whitelist from RoomConfig to each one.
    /// </summary>
    private void ConfigureStations()
    {
        StationBase[] stations = FindObjectsByType<StationBase>(FindObjectsSortMode.None);

        foreach (var station in stations)
        {
            List<ItemData> whitelist = config.GetWhitelistForStation(station.stationType);

            // If no specific config exists for this station type, use all room items
            if (whitelist == null || whitelist.Count == 0)
                whitelist = config.availableItems;

            station.SetAllowedItems(whitelist);
        }

        Debug.Log($"[RoomManager] Configured {stations.Length} stations.");
    }

    /// <summary>
    /// Passes this room's recipe list to the OrderManager so it knows
    /// what orders to spawn.
    /// </summary>
    private void RegisterRecipes()
    {
        if (orderManager == null)
        {
            Debug.LogWarning("[RoomManager] No OrderManager found in scene.");
            return;
        }

        orderManager.SetRoomRecipes(config.recipes);
    }

    /// <summary>
    /// Returns the full list of items available in this room.
    /// Useful for procedural recipe generation later.
    /// </summary>
    public List<ItemData> GetAvailableItems() => config.availableItems;

    /// <summary>
    /// Returns the room's recipe list.
    /// </summary>
    public List<RoomRecipe> GetRecipes() => config.recipes;
}