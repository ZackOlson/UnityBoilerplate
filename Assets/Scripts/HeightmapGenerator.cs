using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class HeightmapGenerator : MonoBehaviour
{
    [Header("Heightmap Settings")]
    public int width = 128;
    public int length = 128;
    public float cellSize = 1f;
    public float heightScale = 10f;
    public float noiseScale = 0.08f;
    public int seed = 42;

    public float[,] heights;
    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;

    void Start()
    {
        GenerateHeightmap();
        BuildMesh();
    }

    [ContextMenu("Generate Heightmap")]
    public void GenerateHeightmap()
    {
        Random.InitState(seed);
        heights = new float[width, length];
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                float nx = (x + seed) * noiseScale;
                float nz = (z + seed) * noiseScale;
                float h = Mathf.PerlinNoise(nx, nz);
                heights[x, z] = h * heightScale;
            }
        }
    }

    public void BuildMesh()
    {
        mesh = new Mesh();
        mesh.indexFormat = verticesNeeded() > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;

        vertices = new Vector3[width * length];
        Vector2[] uvs = new Vector2[vertices.Length];

        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = z * width + x;
                vertices[i] = new Vector3(x * cellSize, heights[x, z], z * cellSize);
                uvs[i] = new Vector2((float)x / width, (float)z / length);
            }
        }

        List<int> tris = new List<int>();
        for (int z = 0; z < length - 1; z++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                int i = z * width + x;
                // triangle 1
                tris.Add(i);
                tris.Add(i + width);
                tris.Add(i + width + 1);
                // triangle 2
                tris.Add(i);
                tris.Add(i + width + 1);
                tris.Add(i + 1);
            }
        }
        triangles = tris.ToArray();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    private int verticesNeeded()
    {
        return width * length;
    }

    public float SampleHeight(Vector3 worldPos)
    {
        if (heights == null)
        {
            Debug.LogWarning("HeightmapGenerator.SampleHeight() called before heightmap generated.");
            return float.MinValue;
        }

        // Convert world position to local space (bottom-left corner is transform.position)
        Vector3 local = worldPos - transform.position;

        float fx = local.x / cellSize;
        float fz = local.z / cellSize;

        // Ensure we're within terrain bounds
        if (fx < 0 || fz < 0 || fx >= width - 1 || fz >= length - 1)
            return float.MinValue;

        int x0 = Mathf.FloorToInt(fx);
        int z0 = Mathf.FloorToInt(fz);
        int x1 = x0 + 1;
        int z1 = z0 + 1;

        float sx = fx - x0;
        float sz = fz - z0;

        float h00 = heights[x0, z0];
        float h10 = heights[x1, z0];
        float h01 = heights[x0, z1];
        float h11 = heights[x1, z1];

        float h0 = Mathf.Lerp(h00, h10, sx);
        float h1 = Mathf.Lerp(h01, h11, sx);
        float finalHeight = Mathf.Lerp(h0, h1, sz);

        // Return height in world space
        return finalHeight + transform.position.y;
    }

    public void SetHeightAtCell(int gx, int gz, float height, bool rebuild = false)
    {
        if (gx < 0 || gz < 0 || gx >= width || gz >= length) return;
        heights[gx, gz] = height;
        if (rebuild) BuildMesh();
    }

    public void SetHeightInRadius(Vector3 worldPos, float radius, float targetHeight, bool rebuild = false)
    {
        Vector3 local = worldPos - transform.position;
        int minX = Mathf.Max(0, Mathf.FloorToInt((local.x - radius) / cellSize));
        int maxX = Mathf.Min(width - 1, Mathf.CeilToInt((local.x + radius) / cellSize));
        int minZ = Mathf.Max(0, Mathf.FloorToInt((local.z - radius) / cellSize));
        int maxZ = Mathf.Min(length - 1, Mathf.CeilToInt((local.z + radius) / cellSize));
        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                Vector3 cellWorld = new Vector3(x * cellSize, 0, z * cellSize) + transform.position;
                if (Vector3.Distance(cellWorld, worldPos) <= radius)
                    heights[x, z] = Mathf.Lerp(heights[x, z], targetHeight, 1f);
            }
        }
        if (rebuild) BuildMesh();
    }

    public void RebuildMesh()
    {
        BuildMesh();
    }

    public Vector3 GetWorldSize()
    {
        return new Vector3((width - 1) * cellSize, heightScale, (length - 1) * cellSize);
    }
}