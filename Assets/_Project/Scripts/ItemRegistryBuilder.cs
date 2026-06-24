using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor window: Tools > Item Registry Builder
/// Scans the project for ALL ItemData assets and lets you
/// approve which ones get added to the global ItemRegistry.
/// </summary>
public class ItemRegistryBuilder : EditorWindow
{
    private ItemRegistry registry;
    private List<ItemData> discovered = new List<ItemData>();
    private Vector2 scroll;

    [MenuItem("Tools/Item Registry Builder")]
    public static void Open()
    {
        GetWindow<ItemRegistryBuilder>("Item Registry Builder");
    }

    private void OnEnable()
    {
        LoadRegistry();
        ScanProject();
    }

    private void LoadRegistry()
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemRegistry");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            registry = AssetDatabase.LoadAssetAtPath<ItemRegistry>(path);
        }
        else
        {
            registry = null;
        }
    }

    private void ScanProject()
    {
        discovered.Clear();
        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null) discovered.Add(item);
        }
        discovered = discovered.OrderBy(i => i.itemName).ToList();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Item Registry Builder", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        // Registry slot
        registry = (ItemRegistry)EditorGUILayout.ObjectField("Target Registry", registry, typeof(ItemRegistry), false);

        if (registry == null)
        {
            EditorGUILayout.HelpBox("No ItemRegistry found. Create one via Right Click > Create > HarryPotter > ItemRegistry, then assign it above.", MessageType.Warning);
            if (GUILayout.Button("Create ItemRegistry asset"))
                CreateRegistryAsset();
            return;
        }

        EditorGUILayout.Space(8);

        // Toolbar
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Rescan project")) ScanProject();
        if (GUILayout.Button("Add all discovered"))
        {
            foreach (var item in discovered)
                AddToRegistry(item);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField($"Discovered ItemData assets ({discovered.Count})", EditorStyles.miniBoldLabel);
        EditorGUILayout.Space(4);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (var item in discovered)
        {
            bool inRegistry = registry.Contains(item);

            EditorGUILayout.BeginHorizontal("box");

            // Status dot
            GUI.color = inRegistry ? Color.green : Color.gray;
            EditorGUILayout.LabelField(inRegistry ? "●" : "○", GUILayout.Width(16));
            GUI.color = Color.white;

            EditorGUILayout.LabelField(item.itemName, GUILayout.MinWidth(120));
            EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(item), EditorStyles.miniLabel);

            if (inRegistry)
            {
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    RemoveFromRegistry(item);
            }
            else
            {
                if (GUILayout.Button("Add", GUILayout.Width(60)))
                    AddToRegistry(item);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField($"Registry contains {registry.items.Count} official items.", EditorStyles.miniLabel);
    }

    private void AddToRegistry(ItemData item)
    {
        if (!registry.items.Contains(item))
        {
            registry.items.Add(item);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
        }
    }

    private void RemoveFromRegistry(ItemData item)
    {
        registry.items.Remove(item);
        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();
    }

    private void CreateRegistryAsset()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Items"))
            AssetDatabase.CreateFolder("Assets", "Items");

        ItemRegistry newRegistry = ScriptableObject.CreateInstance<ItemRegistry>();
        AssetDatabase.CreateAsset(newRegistry, "Assets/Items/ItemRegistry.asset");
        AssetDatabase.SaveAssets();
        registry = newRegistry;
        Debug.Log("[ItemRegistryBuilder] Created ItemRegistry at Assets/Items/ItemRegistry.asset");
    }
}