using System.Collections.Generic;
using UnityEngine;

public class RoadBuilder : MonoBehaviour
{
    public HeightmapGenerator heightmap;
    public float roadRadius = 1f;
    public float roadTargetHeightOffset = -0.1f;
    public float bridgeSlopeThreshold = 4f;
    public GameObject bridgeSegmentPrefab;
    public Transform roadsParent;

    private List<GameObject> spawnedBridges = new List<GameObject>();

    public void BuildRoadAlongPath(List<Node> path)
    {
        if (heightmap == null) { Debug.LogError("Assign HeightmapGenerator in RoadBuilder."); return; }
        if (roadsParent == null)
        {
            GameObject p = new GameObject("RoadsParent");
            roadsParent = p.transform;
        }

        for (int i = 0; i < path.Count; i++)
        {
            Node n = path[i];
            float targetHeight = n.worldPosition.y + roadTargetHeightOffset;
            heightmap.SetHeightInRadius(n.worldPosition, roadRadius, targetHeight, false);

            if (i < path.Count - 1)
            {
                Node next = path[i + 1];
                float dh = Mathf.Abs(next.worldPosition.y - n.worldPosition.y);
                if (dh > bridgeSlopeThreshold && bridgeSegmentPrefab != null)
                {
                    Vector3 mid = (n.worldPosition + next.worldPosition) * 0.5f;
                    Quaternion rot = Quaternion.LookRotation((next.worldPosition - n.worldPosition).normalized);
                    GameObject b = Instantiate(bridgeSegmentPrefab, mid, rot, roadsParent);

                    float len = Vector3.Distance(n.worldPosition, next.worldPosition);
                    b.transform.localScale = new Vector3(b.transform.localScale.x, b.transform.localScale.y, len);
                    spawnedBridges.Add(b);
                }
            }
        }
        heightmap.RebuildMesh();
    }

    public void ClearRoads()
    {
        if (roadsParent != null)
        {
            foreach (Transform t in roadsParent) Destroy(t.gameObject);
            Destroy(roadsParent.gameObject);
            roadsParent = null;
        }
        foreach (var g in spawnedBridges) Destroy(g);
        spawnedBridges.Clear();
    }
}