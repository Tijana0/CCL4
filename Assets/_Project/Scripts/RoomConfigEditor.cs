using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Custom Inspector for RoomConfig.
/// Adds "Fill All" buttons to each StationItemConfig so you can
/// populate the allowed items list from availableItems in one click,
/// then manually remove any exceptions.
/// 
/// Place this in Assets/Editor/
/// </summary>
[CustomEditor(typeof(RoomConfig))]
public class RoomConfigEditor : Editor
{
    // Track foldout states
    private bool showAvailableItems = true;
    private bool showRecipes = true;
    private bool showStationConfigs = true;
    private List<bool> stationFoldouts = new List<bool>();

    public override void OnInspectorGUI()
    {
        RoomConfig config = (RoomConfig)target;
        serializedObject.Update();

        // ── Room Name ────────────────────────────────────────────────────
        EditorGUILayout.Space(4);
        config.roomName = EditorGUILayout.TextField("Room Name", config.roomName);
        EditorGUILayout.Space(8);

        // ── Available Items ──────────────────────────────────────────────
        showAvailableItems = EditorGUILayout.Foldout(showAvailableItems, $"Available Items ({config.availableItems.Count})", true, EditorStyles.foldoutHeader);
        if (showAvailableItems)
        {
            EditorGUI.indentLevel++;
            if (config.availableItems == null)
                config.availableItems = new List<ItemData>();

            for (int i = 0; i < config.availableItems.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                config.availableItems[i] = (ItemData)EditorGUILayout.ObjectField(config.availableItems[i], typeof(ItemData), false);
                if (GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    config.availableItems.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15);
            if (GUILayout.Button("+ Add Item"))
                config.availableItems.Add(null);
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(8);

        // ── Recipes ──────────────────────────────────────────────────────
        showRecipes = EditorGUILayout.Foldout(showRecipes, $"Recipes ({config.recipes.Count})", true, EditorStyles.foldoutHeader);
        if (showRecipes)
        {
            EditorGUI.indentLevel++;
            if (config.recipes == null)
                config.recipes = new List<RoomRecipe>();

            for (int i = 0; i < config.recipes.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Recipe {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    config.recipes.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                config.recipes[i].recipeName     = EditorGUILayout.TextField("Name", config.recipes[i].recipeName);
                config.recipes[i].requiredOutput  = (ItemData)EditorGUILayout.ObjectField("Required Output", config.recipes[i].requiredOutput, typeof(ItemData), false);
                config.recipes[i].scoreValue      = EditorGUILayout.IntField("Score Value", config.recipes[i].scoreValue);
                config.recipes[i].timeLimit       = EditorGUILayout.FloatField("Time Limit (s)", config.recipes[i].timeLimit);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            if (GUILayout.Button("+ Add Recipe"))
                config.recipes.Add(new RoomRecipe());

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(8);

        // ── Station Configs ──────────────────────────────────────────────
        showStationConfigs = EditorGUILayout.Foldout(showStationConfigs, $"Station Configs ({config.stationConfigs.Count})", true, EditorStyles.foldoutHeader);
        if (showStationConfigs)
        {
            EditorGUI.indentLevel++;
            if (config.stationConfigs == null)
                config.stationConfigs = new List<StationItemConfig>();

            // Sync foldout list size
            while (stationFoldouts.Count < config.stationConfigs.Count)
                stationFoldouts.Add(true);

            for (int i = 0; i < config.stationConfigs.Count; i++)
            {
                StationItemConfig sc = config.stationConfigs[i];
                if (sc.allowedItems == null) sc.allowedItems = new List<ItemData>();

                EditorGUILayout.BeginVertical("box");

                // Header row
                EditorGUILayout.BeginHorizontal();
                stationFoldouts[i] = EditorGUILayout.Foldout(stationFoldouts[i], $"{sc.stationType}  ({sc.allowedItems.Count} items)", true);
                if (GUILayout.Button("Remove Station", GUILayout.Width(110)))
                {
                    config.stationConfigs.RemoveAt(i);
                    stationFoldouts.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                if (stationFoldouts[i])
                {
                    sc.stationType = (StationType)EditorGUILayout.EnumPopup("Station Type", sc.stationType);

                    EditorGUILayout.Space(4);

                    // ── Fill All / Clear All buttons ─────────────────────
                    EditorGUILayout.BeginHorizontal();

                    GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
                    if (GUILayout.Button("Fill All from Available Items"))
                    {
                        sc.allowedItems.Clear();
                        foreach (var item in config.availableItems)
                        {
                            if (item != null && !sc.allowedItems.Contains(item))
                                sc.allowedItems.Add(item);
                        }
                    }

                    GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
                    if (GUILayout.Button("Clear All"))
                        sc.allowedItems.Clear();

                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(4);

                    // ── Allowed items list ───────────────────────────────
                    if (sc.allowedItems.Count == 0)
                    {
                        EditorGUILayout.HelpBox("No items — station accepts nothing. Use Fill All or add items manually.", MessageType.Info);
                    }
                    else
                    {
                        for (int j = 0; j < sc.allowedItems.Count; j++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            sc.allowedItems[j] = (ItemData)EditorGUILayout.ObjectField(sc.allowedItems[j], typeof(ItemData), false);
                            if (GUILayout.Button("✕", GUILayout.Width(24)))
                            {
                                sc.allowedItems.RemoveAt(j);
                                break;
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }

                    EditorGUILayout.Space(2);

                    if (GUILayout.Button("+ Add Item Manually"))
                        sc.allowedItems.Add(null);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            if (GUILayout.Button("+ Add Station Config"))
            {
                config.stationConfigs.Add(new StationItemConfig());
                stationFoldouts.Add(true);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(8);

        if (GUI.changed)
            EditorUtility.SetDirty(config);

        serializedObject.ApplyModifiedProperties();
    }
}