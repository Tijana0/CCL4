using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Static helper for blending ingredient colours into a final potion colour.
/// Used by MultiIngredientStation when a wrong/improvised combo is brewed,
/// and can also be used for correct recipes to colour the output liquid.
///
/// Math:
///   1. Average the potionColour of all non-colourless ingredients.
///   2. Each colourless ingredient (e.g. Water) dilutes the result toward
///      white by a percentage, simulating watering-down.
///
/// Safe for any ingredient count, including all-water or zero ingredients.
/// </summary>
public static class PotionColourUtility
{
    [Tooltip("How much each water/colourless ingredient dilutes toward white (0-1)")]
    public const float DilutionPerColourless = 0.15f;

    /// <summary>
    /// Blends a list of ingredient ItemData into a single resulting colour.
    /// </summary>
    public static Color BlendIngredients(List<ItemData> ingredients)
    {
        if (ingredients == null || ingredients.Count == 0)
            return Color.white;

        List<Color> colourContributors = new List<Color>();
        int colourlessCount = 0;

        foreach (var item in ingredients)
        {
            if (item == null) continue;

            if (item.isColourless)
                colourlessCount++;
            else
                colourContributors.Add(item.potionColour);
        }

        // Average the colour contributors. If everything was colourless
        // (e.g. brewing pure water), fall back to a neutral pale blue-white.
        Color baseColour;
        if (colourContributors.Count == 0)
        {
            baseColour = new Color(0.85f, 0.9f, 1f); // pale water tint
        }
        else
        {
            float r = 0f, g = 0f, b = 0f;
            foreach (var c in colourContributors)
            {
                r += c.r; g += c.g; b += c.b;
            }
            int n = colourContributors.Count;
            baseColour = new Color(r / n, g / n, b / n);
        }

        // Dilute toward white based on how many colourless ingredients were added.
        // Clamped so it can never overshoot past pure white even with many waters.
        float dilution = Mathf.Clamp01(colourlessCount * DilutionPerColourless);
        Color result = Color.Lerp(baseColour, Color.white, dilution);

        return result;
    }
}