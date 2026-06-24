using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks a teacup that can be filled with tea, and holds the mapping from a tea
/// ingredient to the resulting filled-teacup item.
///
/// NOTE: This is currently a data-holder placeholder. The filled-teacup model is
/// not in the project yet, so no swap/fill logic runs here — the mappings are kept
/// on the prefab so they're ready to wire up once the filled model exists.
/// </summary>
public class TeacupFillTarget : MonoBehaviour
{
    [System.Serializable]
    public class FillMapping
    {
        [Tooltip("Tea ingredient that fills this teacup.")]
        public ItemData teaItem;
        [Tooltip("Filled-teacup item this becomes once filled.")]
        public ItemData resultTeacupItem;
    }

    [Tooltip("Whether this teacup is currently filled.")]
    public bool isFilled = false;

    [Tooltip("Tea -> filled-teacup mappings.")]
    public List<FillMapping> mappings = new List<FillMapping>();
}
