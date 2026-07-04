using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Tools > Potion Room Setup
/// Creates all prefabs, ItemData assets, and an ItemRegistry in one click.
/// Also wires all processing/combine rules and adds items to the registry.
///
/// Run this once to bootstrap everything. After that, use the
/// Item Registry Builder (Tools menu) to manage the global item list.
/// </summary>
public class PotionRoomSetup
{
    [MenuItem("Tools/Potion Room Setup - Create All Prefabs and Items")]
    public static void CreateAll()
    {
        EnsureFolders();

        // --- Prefabs ---
        GameObject redBallPrefab      = CreateBallPrefab("RedBall",            Color.red);
        GameObject blueBallPrefab     = CreateBallPrefab("BlueBall",           Color.blue);
        GameObject blueredBallPrefab  = CreateBallPrefab("BlueredBall",        new Color(0.6f, 0.1f, 0.8f));
        GameObject dragonPrefab       = CreateItemPrefab("DragonScales",       new Color(0.2f, 0.6f, 0.2f), PrimitiveType.Capsule,  new Vector3(0.3f, 0.15f, 0.3f));
        GameObject groundPrefab       = CreateItemPrefab("GroundDragonScales", new Color(0.5f, 0.8f, 0.3f), PrimitiveType.Cylinder, new Vector3(0.35f, 0.05f, 0.35f));
        GameObject waterPrefab        = CreateItemPrefab("WaterBowl",          new Color(0.2f, 0.5f, 1.0f), PrimitiveType.Cylinder, new Vector3(0.4f,  0.15f, 0.4f));
        GameObject potionPrefab       = CreateItemPrefab("RedPotion",          new Color(1.0f, 0.1f, 0.1f), PrimitiveType.Capsule,  new Vector3(0.2f,  0.3f,  0.2f));

        // --- ItemData ---
        ItemData redBallData     = CreateItemData("RedBall",            redBallPrefab);
        ItemData blueBallData    = CreateItemData("BlueBall",           blueBallPrefab);
        ItemData blueredData     = CreateItemData("BlueredBall",        blueredBallPrefab);
        ItemData dragonData      = CreateItemData("DragonScales",       dragonPrefab);
        ItemData groundData      = CreateItemData("GroundDragonScales", groundPrefab);
        ItemData waterData       = CreateItemData("WaterBowl",          waterPrefab);
        ItemData potionData      = CreateItemData("RedPotion",          potionPrefab);

        // --- Rules ---

        // Red + Blue = BlueredBall (counter combine)
        AddCombineRule(redBallData, blueBallData, blueredData);

        // Dragon Scales: must be ground first, only goes on mortar/counter/bin
        dragonData.requiresProcessingBeforeCauldron = true;
        dragonData.allowedStationTypes = new List<StationType>
            { StationType.MortarAndPestle, StationType.Counter, StationType.Bin };
        AddProcessingRule(dragonData, StationType.MortarAndPestle, groundData, 3f);

        // Ground Scales + Water = Red Potion
        AddCombineRule(groundData, waterData, potionData);

        // Water bowl station restrictions
        waterData.allowedStationTypes = new List<StationType>
            { StationType.Counter, StationType.Cauldron, StationType.Bin, StationType.HandIn };

        // Save ItemData
        foreach (var d in new[] { redBallData, blueBallData, blueredData, dragonData, groundData, waterData, potionData })
            EditorUtility.SetDirty(d);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // --- Assign ItemData back to prefab WorldItem components ---
        AssignItemDataToPrefab(redBallPrefab,     redBallData);
        AssignItemDataToPrefab(blueBallPrefab,    blueBallData);
        AssignItemDataToPrefab(blueredBallPrefab, blueredData);
        AssignItemDataToPrefab(dragonPrefab,      dragonData);
        AssignItemDataToPrefab(groundPrefab,      groundData);
        AssignItemDataToPrefab(waterPrefab,       waterData);
        AssignItemDataToPrefab(potionPrefab,      potionData);

        // --- Create or update ItemRegistry ---
        ItemRegistry registry = GetOrCreateRegistry();
        var allItems = new[] { redBallData, blueBallData, blueredData, dragonData, groundData, waterData, potionData };
        foreach (var item in allItems)
        {
            if (!registry.items.Contains(item))
                registry.items.Add(item);
        }
        EditorUtility.SetDirty(registry);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[PotionRoomSetup] Done!");
        Debug.Log("Prefabs → Assets/Prefabs/Items/");
        Debug.Log("ItemData → Assets/Items/");
        Debug.Log("ItemRegistry → Assets/Items/ItemRegistry.asset");
        Debug.Log("Next: Open your scene, add an empty GameObject, add RoomConfig + RoomManager components, and configure your room.");

        EditorGUIUtility.PingObject(registry);
    }

    // -----------------------------------------------------------------------

    private static GameObject CreateBallPrefab(string itemName, Color color)
        => CreateItemPrefab(itemName, color, PrimitiveType.Sphere, Vector3.one * 0.5f);

    private static GameObject CreateItemPrefab(string itemName, Color color, PrimitiveType shape, Vector3 scale)
    {
        string assetPath = $"Assets/Prefabs/Items/{itemName}.prefab";
        string filePath  = Path.Combine(Application.dataPath, $"Prefabs/Items/{itemName}.prefab");

        if (File.Exists(filePath))
        {
            Debug.Log($"[PotionRoomSetup] {itemName} prefab already exists — skipping.");
            return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        }

        GameObject go = GameObject.CreatePrimitive(shape);
        go.name = itemName;
        go.transform.localScale = scale;
        ApplyColor(go, color);

        if (go.GetComponent<Rigidbody>() == null) go.AddComponent<Rigidbody>();
        if (go.GetComponent<PickupObject>() == null) go.AddComponent<PickupObject>();
        if (go.GetComponent<WorldItem>() == null) go.AddComponent<WorldItem>();

        bool success;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, assetPath, out success);
        if (!success) Debug.LogError($"[PotionRoomSetup] Failed to save prefab: {assetPath}");
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static ItemData CreateItemData(string itemName, GameObject prefab)
    {
        string path = $"Assets/Items/{itemName}.asset";
        ItemData existing = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (existing != null)
        {
            existing.prefab   = prefab;
            existing.itemName = itemName;
            return existing;
        }

        ItemData data = ScriptableObject.CreateInstance<ItemData>();
        data.itemName         = itemName;
        data.prefab           = prefab;
        data.processingRules  = new List<ProcessingRule>();
        data.combineRules     = new List<CombineRule>();
        data.allowedStationTypes = new List<StationType>();
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    private static void AddCombineRule(ItemData source, ItemData other, ItemData output)
    {
        foreach (var r in source.combineRules)
            if (r.otherItem == other) return;
        source.combineRules.Add(new CombineRule { otherItem = other, outputItem = output });
    }

    private static void AddProcessingRule(ItemData source, StationType station, ItemData output, float time)
    {
        foreach (var r in source.processingRules)
            if (r.stationType == station) return;
        source.processingRules.Add(new ProcessingRule { stationType = station, outputItem = output, processingTime = time });
    }

    private static void AssignItemDataToPrefab(GameObject prefab, ItemData data)
    {
        if (prefab == null || data == null) return;
        string path = AssetDatabase.GetAssetPath(prefab);
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        WorldItem wi = root.GetComponent<WorldItem>();
        if (wi != null)
        {
            wi.itemData = data;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static ItemRegistry GetOrCreateRegistry()
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemRegistry");
        if (guids.Length > 0)
            return AssetDatabase.LoadAssetAtPath<ItemRegistry>(AssetDatabase.GUIDToAssetPath(guids[0]));

        ItemRegistry registry = ScriptableObject.CreateInstance<ItemRegistry>();
        registry.items = new List<ItemData>();
        AssetDatabase.CreateAsset(registry, "Assets/Items/ItemRegistry.asset");
        return registry;
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Items"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Items");
        if (!AssetDatabase.IsValidFolder("Assets/Items"))
            AssetDatabase.CreateFolder("Assets", "Items");
    }

    private static void ApplyColor(GameObject go, Color color)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r == null) return;
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
            mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        r.sharedMaterial = mat;
    }
}