using UnityEngine;

/// <summary>
/// Automatically generates invisible physical walls around the bounds of the attached MeshRenderer.
/// This prevents Rigidbody items from falling off the edge of the ground.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class EdgeBoundaries : MonoBehaviour
{
    public float wallHeight = 10f;
    public float wallThickness = 1f;

    private void Awake()
    {
        GenerateWalls();
    }

    private void GenerateWalls()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null) return;

        Bounds bounds = mr.bounds;
        Vector3 center = bounds.center;
        Vector3 size = bounds.size;

        // Create a parent container for the walls to keep the hierarchy clean
        GameObject wallsContainer = new GameObject("InvisibleWalls");
        wallsContainer.transform.SetParent(this.transform);
        wallsContainer.transform.position = center;

        // Top Wall (Z+)
        CreateWall(wallsContainer, 
            new Vector3(center.x, center.y + (wallHeight / 2f), center.z + (size.z / 2f) + (wallThickness / 2f)), 
            new Vector3(size.x + (wallThickness * 2f), wallHeight, wallThickness), 
            "Wall_Top");

        // Bottom Wall (Z-)
        CreateWall(wallsContainer, 
            new Vector3(center.x, center.y + (wallHeight / 2f), center.z - (size.z / 2f) - (wallThickness / 2f)), 
            new Vector3(size.x + (wallThickness * 2f), wallHeight, wallThickness), 
            "Wall_Bottom");

        // Left Wall (X-)
        CreateWall(wallsContainer, 
            new Vector3(center.x - (size.x / 2f) - (wallThickness / 2f), center.y + (wallHeight / 2f), center.z), 
            new Vector3(wallThickness, wallHeight, size.z), 
            "Wall_Left");

        // Right Wall (X+)
        CreateWall(wallsContainer, 
            new Vector3(center.x + (size.x / 2f) + (wallThickness / 2f), center.y + (wallHeight / 2f), center.z), 
            new Vector3(wallThickness, wallHeight, size.z), 
            "Wall_Right");
    }

    private void CreateWall(GameObject parent, Vector3 position, Vector3 size, string name)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent.transform);
        wall.transform.position = position;
        
        BoxCollider col = wall.AddComponent<BoxCollider>();
        col.size = size;

        // Optionally assign it to a specific layer (e.g., "Default" or a custom "Wall" layer)
        // wall.layer = LayerMask.NameToLayer("Default");
    }
}
