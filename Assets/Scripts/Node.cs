using UnityEngine;

public class Node
{
    public int gridX;
    public int gridY;
    public Vector3 worldPosition;
    public bool walkable;
    public float weight = 1f;

    public float gCost;
    public float hCost;
    public Node parent;

    public Node(int x, int y, Vector3 worldPos)
    {
        gridX = x;
        gridY = y;
        worldPosition = worldPos;
    }

    public float fCost { get { return gCost + hCost; } }
}
