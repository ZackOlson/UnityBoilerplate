using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Settings")]
    public int gridWidth = 64;
    public int gridHeight = 64;
    public float cellSize = 1f;
    public Vector3 origin = Vector3.zero;

    [Header("Terrain Reference")]
    public HeightmapGenerator heightmapGenerator;

    [Header("Walkability Settings")]
    public float maxWalkableSlope = 45f;
    public float maxWalkableHeight = 20f;
    public LayerMask obstacleLayer;

    public Node[,] grid;

    private void Awake()
    {
        Instance = this;
    }

    IEnumerator Start()
    {
        if (heightmapGenerator == null)
        {
            Debug.LogError("GridManager: HeightmapGenerator not assigned!");
            yield break;
        }

        // Wait until the heightmap is initialized
        while (heightmapGenerator.heights == null)
        {
            Debug.Log("GridManager: waiting for heightmap...");
            yield return null;
        }

        gridWidth = heightmapGenerator.width;
        gridHeight = heightmapGenerator.length;
        cellSize = heightmapGenerator.cellSize;

        origin = heightmapGenerator.transform.position;
        InitializeGrid();
    }

    public void InitializeGrid()
    {
        if (heightmapGenerator == null)
        {
            Debug.LogError("GridManager.InitializeGrid(): HeightmapGenerator not assigned!");
            return;
        }

        origin = heightmapGenerator.transform.position;

        grid = new Node[gridWidth, gridHeight];

        int walkableCount = 0;
        int unwalkableCount = 0;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 worldPos = origin + new Vector3(x * cellSize, 0, y * cellSize);

                float h = heightmapGenerator.SampleHeight(worldPos);
                if (float.IsNaN(h) || h == float.MinValue)
                {
                    h = 0f;
                }

                worldPos.y = h;

                Node node = new Node(x, y, worldPos)
                {
                    walkable = true,
                    weight = 1f
                };

                float dh = 0f;
                Vector3 right = worldPos + new Vector3(cellSize, 0, 0);
                Vector3 forward = worldPos + new Vector3(0, 0, cellSize);
                float hr = heightmapGenerator.SampleHeight(right);
                float hf = heightmapGenerator.SampleHeight(forward);

                if (hr != float.MinValue) dh = Mathf.Abs(hr - h);
                if (hf != float.MinValue) dh = Mathf.Max(dh, Mathf.Abs(hf - h));

                float slopeDeg = Mathf.Atan2(dh, cellSize) * Mathf.Rad2Deg;
                if (slopeDeg > maxWalkableSlope)
                    node.walkable = false;

                // Check for obstcles
                Collider[] cols = Physics.OverlapBox(
                    worldPos + Vector3.up * 0.5f,
                    Vector3.one * cellSize * 0.45f,
                    Quaternion.identity,
                    obstacleLayer
                );
                if (cols.Length > 0)
                    node.walkable = false;

                if (node.walkable) walkableCount++;
                else unwalkableCount++;

                grid[x, y] = node;
            }
        }

        Debug.Log($" Grid initialized at {origin} ({gridWidth}x{gridHeight}) " +
                  $"Walkable: {walkableCount}, Unwalkable: {unwalkableCount}");
    }

    public Node NodeFromWorldPosition(Vector3 worldPos)
    {
        if (grid == null)
        {
            Debug.LogWarning("GridManager.NodeFromWorldPosition: grid not initialized yet!");
            return null;
        }

        Vector3 local = worldPos - origin;

        int x = Mathf.FloorToInt(local.x / cellSize);
        int y = Mathf.FloorToInt(local.z / cellSize);

        if (x < 0 || y < 0 || x >= gridWidth || y >= gridHeight)
        {
            print("out of bounds");
            return null;

        }
            

        return grid[x, y];
    }

    public IEnumerable<Node> GetNeighbors(Node node)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = node.gridX + dx;
                int ny = node.gridY + dy;

                if (nx >= 0 && ny >= 0 && nx < gridWidth && ny < gridHeight)
                    yield return grid[nx, ny];
            }
        }
    }

    // Draw walkable/unwalkable nodes
    private void OnDrawGizmosSelected()
    {
        if (grid == null) return;


        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Node n = grid[x, y];
                if (n == null) continue;

                Gizmos.color = n.walkable ? Color.green : Color.red;
                Gizmos.DrawWireCube(n.worldPosition + Vector3.up * 0.05f, Vector3.one * (cellSize * 0.9f));
            }
        }

        // Draw grid area
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            origin + new Vector3(gridWidth * cellSize / 2f, 0, gridHeight * cellSize / 2f),
            new Vector3(gridWidth * cellSize, 0, gridHeight * cellSize)
        );
    }
}