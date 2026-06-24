using UnityEngine;

/// <summary>
/// All station types in the game. Add new entries here as new rooms/stations are added.
/// The enum value itself doesn't drive gameplay logic (that comes from the script
/// attached to the station — ProcessingStation, MultiIngredientStation, etc.) except
/// for Bin, Sink, and HandIn, which a few scripts check for specifically.
/// </summary>
public enum StationType
{
    Counter,         // Plain counter — can hold items, allows stacking/combining
    Cauldron,        // Multi-ingredient cooker, has boiling states (Potions room)
    MortarAndPestle, // Grinding station (hold to process)
    CuttingBoard,    // Chopping station (hold to process)
    Sink,            // Washing/resetting station (instant)
    Bin,             // Disposal station (instant, destroys item)
    HandIn,          // Order completion station (instant, scores points)
    ItemContainer,   // Spawns/transforms an item on interact (dispenser)

    // Divination room
    Brazier,         // Herb burning station (hold to process)
    TeapotFire,       // Where the Teapot sits to brew tea (multi-ingredient)
    CrystalBall,      // Crystal + Book -> Vision (multi-ingredient)
    ProphecyTable     // Card + Book -> Prophecy Card (multi-ingredient)
}