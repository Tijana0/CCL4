using UnityEngine;

/// <summary>
/// All station types in the game. Add new entries here as new rooms/levels are added.
/// </summary>
public enum StationType
{
    Counter,        // Plain counter — can hold items, allows stacking
    Cauldron,       // Multi-ingredient cooker, has boiling states
    MortarAndPestle,// Grinding station (hold to process)
    CuttingBoard,   // Chopping station (hold to process)
    Sink,           // Washing/resetting station (instant)
    Bin,            // Disposal station (instant, destroys item)
    HandIn,         // Order completion station (instant, scores points)
    ItemContainer   // Spawns a fresh copy of its item on interact
}