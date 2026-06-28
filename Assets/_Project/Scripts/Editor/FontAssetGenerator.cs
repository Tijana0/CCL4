using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;
using UnityEngine.TextCore.LowLevel;

public class FontAssetGenerator
{
    [InitializeOnLoadMethod]
    public static void CheckAndGenerateFonts()
    {
        string fontsDir = "Assets/_Project/Fonts";
        if (!Directory.Exists(fontsDir)) return;

        string[] ttfFiles = Directory.GetFiles(fontsDir, "*.ttf");
        bool generatedAny = false;

        foreach (string ttfPath in ttfFiles)
        {
            string fontName = Path.GetFileNameWithoutExtension(ttfPath);
            string assetPath = Path.Combine(fontsDir, fontName + " TMP.asset");

            bool needsGeneration = !File.Exists(assetPath);
            if (!needsGeneration)
            {
                // Verify if the existing asset is corrupted (missing atlas textures or material)
                TMP_FontAsset existingAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
                if (existingAsset == null || 
                    existingAsset.atlasTextures == null || 
                    existingAsset.atlasTextures.Length == 0 || 
                    existingAsset.atlasTextures[0] == null ||
                    existingAsset.material == null)
                {
                    Debug.Log($"[FontAssetGenerator] Detected corrupted font asset at {assetPath}. Regenerating...");
                    needsGeneration = true;
                }
            }

            if (needsGeneration)
            {
                Debug.Log($"[FontAssetGenerator] Generating TMP Font Asset for {fontName}...");
                
                // Load the source Font
                Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
                if (sourceFont == null)
                {
                    Debug.LogError($"[FontAssetGenerator] Could not load font at {ttfPath}");
                    continue;
                }

                // Delete old asset if it exists to prevent conflicts
                if (File.Exists(assetPath))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }

                // Create the font asset with Dynamic population so all characters are supported
                TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                    sourceFont, 
                    90, // samplingPointSize
                    9,  // atlasPadding
                    GlyphRenderMode.SDFAA, 
                    1024, // atlasWidth
                    1024, // atlasHeight
                    AtlasPopulationMode.Dynamic
                );

                if (fontAsset != null)
                {
                    AssetDatabase.CreateAsset(fontAsset, assetPath);

                    // Add the texture and material as sub-assets so they are saved inside the same file
                    if (fontAsset.atlasTextures != null)
                    {
                        for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                        {
                            if (fontAsset.atlasTextures[i] != null)
                            {
                                fontAsset.atlasTextures[i].name = fontName + " Atlas";
                                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[i], fontAsset);
                            }
                        }
                    }
                    if (fontAsset.material != null)
                    {
                        fontAsset.material.name = fontName + " Material";
                        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                    }

                    generatedAny = true;
                    Debug.Log($"[FontAssetGenerator] Successfully created: {assetPath}");
                }
                else
                {
                    Debug.LogError($"[FontAssetGenerator] Failed to create TMP_FontAsset for {fontName}");
                }
            }
        }

        if (generatedAny)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
