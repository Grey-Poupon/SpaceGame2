using UnityEngine;

public class SegmentedCircle : MonoBehaviour
{
    public int segmentCount = 12;  // Number of segments
    public float radius = 5f;      // Radius of the circle
    public Material outlineMaterial;  // Material for the black outline
    public Material segmentMaterial;  // Material for the red segments
    public float gapAngle = 2f;  // Angle of the gap between segments (in degrees)

    private GameObject[] segments;

    void Start()
    {
        CreateSegments();
    }

    void CreateSegments()
    {
        segments = new GameObject[segmentCount];
        float angleStep = (360f / segmentCount) - gapAngle; // Adjust angle to include gaps

        for (int i = 0; i < segmentCount; i++)
        {
            segments[i] = CreateSegment(i, angleStep);
        }
    }

    GameObject CreateSegment(int index, float angleStep)
    {
        GameObject segment = new GameObject("Segment " + index);
        segment.transform.parent = transform;

        // Create the full-sized outer mesh (outline)
        // CreatePieMesh(segment, angleStep, radius, outlineMaterial, "Outline", 0);

        // Create the smaller inner part (scaled down to create a gap)
        CreatePieMesh(segment, angleStep, radius, segmentMaterial, "Inner", gapAngle / 2); // Shift for gap

        return segment;
    }

    void CreatePieMesh(GameObject parent, float angleStep, float meshRadius, Material material, string nameSuffix, float angleShift)
    {
        GameObject meshObj = new GameObject(parent.name + " " + nameSuffix);
        meshObj.transform.parent = parent.transform;

        MeshFilter meshFilter = meshObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = meshObj.AddComponent<MeshRenderer>();
        meshRenderer.material = material;

        Mesh mesh = new Mesh();
        meshFilter.mesh = mesh;

        // Create a pie-shaped mesh (triangle fan)
        Vector3[] vertices = new Vector3[3];
        vertices[0] = Vector3.zero;  // Center of the circle

        // Calculate the two points that define the edges of the segment, with a small angle shift for the gap
        float angle1 = Mathf.Deg2Rad * (parent.transform.GetSiblingIndex() * (angleStep + gapAngle) + angleShift);
        float angle2 = Mathf.Deg2Rad * ((parent.transform.GetSiblingIndex() + 1) * (angleStep + gapAngle) - angleShift);
        vertices[1] = new Vector3(Mathf.Cos(angle1), Mathf.Sin(angle1), 0) * meshRadius;
        vertices[2] = new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2), 0) * meshRadius;

        // Assign the vertices to the mesh
        mesh.vertices = vertices;

        // Define the triangles
        int[] triangles = new int[] { 0, 1, 2 };
        mesh.triangles = triangles;

        // Recalculate normals and bounds for proper rendering
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    public void RemoveColorFromSegment(int index)
    {
        if (index >= 0 && index < segmentCount)
        {
            MeshRenderer meshRenderer = segments[index].transform.Find(segments[index].name + " Inner").GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.material.color = Color.clear;  // Remove color
            }
        }
    }
}
