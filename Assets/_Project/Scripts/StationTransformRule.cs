using UnityEngine;

/// <summary>
/// Defines what a specific item becomes when processed at a station.
/// This is set directly on the station in the Inspector — NOT on the item's ItemData.
/// 
/// Example on a Cutting Board station:
///   Input: Herb  →  Output: ChoppedHerb  (processing time: 2s)
///   Input: Mushroom → Output: SlicedMushroom (processing time: 2.5s)
///
/// Example on a Sink station:
///   Input: DirtyBowl → Output: CleanBowl (instant)
///   Input: FilledCauldron → Output: EmptyCauldron (instant)
/// </summary>
[System.Serializable]
public class StationTransformRule
{
    [Tooltip("The item that goes IN to this station")]
    public ItemData inputItem;
    [Tooltip("What it becomes after processing")]
    public ItemData outputItem;
    [Tooltip("How long processing takes. 0 = instant (used by Sink).")]
    public float processingTime = 2f;
}