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

            if (!File.Exists(assetPath))
            {
                Debug.Log($"[FontAssetGenerator] Generating TMP Font Asset for {fontName}...");
                
                // Load the source Font
                Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
                if (sourceFont == null)
                {
                    Debug.LogError($"[FontAssetGenerator] Could not load font at {ttfPath}");
                    continue;
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

        // Style the Credits text in Start_Scene if not already done
        StyleStartSceneCredits();
    }

    private static void StyleStartSceneCredits()
    {
        string startScenePath = "Assets/_Project/Scenes/DevScenes/Start_Scene.unity";
        if (!File.Exists(startScenePath)) return;

        string fontAssetPath = "Assets/_Project/Fonts/Cinzel-Variable TMP.asset";
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
        if (fontAsset == null) return;

        string originalScenePath = EditorSceneManager.GetActiveScene().path;

        // Open start scene
        var scene = EditorSceneManager.OpenScene(startScenePath, OpenSceneMode.Single);
        GameObject creditsGo = GameObject.Find("Credits");
        bool modified = false;

        if (creditsGo != null)
        {
            var tmp = creditsGo.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                // Check if we need to apply our beautiful styling
                if (tmp.font != fontAsset || tmp.fontSize != 15f || tmp.characterSpacing != 6f)
                {
                    tmp.font = fontAsset;
                    tmp.fontSize = 15f;
                    tmp.characterSpacing = 6f; // Elegant wide letter spacing
                    tmp.color = new Color(0.8f, 0.75f, 0.65f, 0.85f); // Soft warm platinum/silver color
                    tmp.fontStyle = FontStyles.Normal; // Clean and classic
                    
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    modified = true;
                    Debug.Log("[FontAssetGenerator] Styled Credits text in Start_Scene successfully!");
                }
            }
        }

        // Restore original scene if we switched
        if (modified && !string.IsNullOrEmpty(originalScenePath) && originalScenePath != startScenePath && File.Exists(originalScenePath))
        {
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }
    }
}
