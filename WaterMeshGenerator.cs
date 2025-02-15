using UnityEngine;
using System.Collections.Generic;
using static MergedUtilities; // Füge Verweis auf MergedUtilities hinzu

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaterMeshGenerator : MonoBehaviour
{
    [Header("References")]
    public ShallowWaterSimulation waterSim;

    [Header("Mesh Settings")]
    public float waterfallThreshold = 2f;
    public float wallThickness = 0.1f;

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uv;

    void Start()
    {
        if (!waterSim)
        {
            Debug.LogError("Bitte die ShallowWaterSimulation-Referenz zuweisen!");
            return;
        }

        MeshFilter mf = GetComponent<MeshFilter>();
        mesh = new Mesh();
        mesh.name = "WaterMesh";
        mf.mesh = mesh;

        BuildSurfaceMesh();
    }

    void Update()
    {
        if (!waterSim) return;
        UpdateSurfaceMesh();
    }

    void BuildSurfaceMesh()
    {
        int res = waterSim.Resolution;
        vertices = new Vector3[res * res];
        uv = new Vector2[res * res];
        triangles = new int[(res - 1) * (res - 1) * 6];

        float cellSize = waterSim.CellSize;
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                int idx = y * res + x;
                float wx = x * cellSize;
                float wz = y * cellSize;
                vertices[idx] = new Vector3(wx, 0, wz);
                uv[idx] = new Vector2((float)x / (res - 1), (float)y / (res - 1));
            }
        }

        int t = 0;
        for (int y = 0; y < res - 1; y++)
        {
            for (int x = 0; x < res - 1; x++)
            {
                int i00 = y * res + x;
                int i01 = y * res + x + 1;
                int i10 = (y + 1) * res + x;
                int i11 = (y + 1) * res + (x + 1);

                triangles[t++] = i00;
                triangles[t++] = i10;
                triangles[t++] = i01;

                triangles[t++] = i01;
                triangles[t++] = i10;
                triangles[t++] = i11;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    void UpdateSurfaceMesh()
    {
        int res = waterSim.Resolution;
        var waterMap = waterSim.GetWaterMap();
        var terrMap = waterSim.GetTerrainHeights2D();
        if (waterMap == null || terrMap == null) return; // Zusätzliche Überprüfung hinzugefügt
        float cellSize = waterSim.CellSize;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                int idx = y * res + x;
                float hTerr = terrMap[x, y];
                float hWater = waterMap[x, y];
                float px = x * cellSize;
                float pz = y * cellSize;
                float py = hTerr + hWater;
                vertices[idx] = new Vector3(px, py, pz);
            }
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }

    void GenerateWaterfallMesh()
    {
        int res = waterSim.Resolution;
        var waterMap = waterSim.GetWaterMap();
        var terrMap = waterSim.GetTerrainHeights2D();
        float cellSize = waterSim.CellSize;

        List<Vector3> waterfallVertices = new List<Vector3>();
        List<int> waterfallTriangles = new List<int>();

        Vector2Int[] neighbors = {
            new Vector2Int(1,0), new Vector2Int(-1,0),
            new Vector2Int(0,1), new Vector2Int(0,-1),
        };

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float myH = terrMap[x, y] + waterMap[x, y];
                // Verwende MergedUtilities Methoden hier
                Vector2 flowDir = CalculateRiverFlowDirection(waterMap, new Vector2Int(x, y));
                foreach (var nb in neighbors)
                {
                    int nx = x + nb.x;
                    int ny = y + nb.y;
                    if (nx < 0 || nx >= res || ny < 0 || ny >= res) continue;

                    float neighH = terrMap[nx, ny] + waterMap[nx, ny];
                    float diff = myH - neighH;
                    if (diff > waterfallThreshold)
                    {
                        Vector3 p0 = new Vector3(x * cellSize, myH, y * cellSize);
                        Vector3 p1 = new Vector3(nx * cellSize, neighH, ny * cellSize);
                        Vector3 p2 = new Vector3(p0.x, p0.y - wallThickness, p0.z);
                        Vector3 p3 = new Vector3(p1.x, p1.y - wallThickness, p1.z);

                        int baseIndex = waterfallVertices.Count;
                        waterfallVertices.Add(p0);
                        waterfallVertices.Add(p1);
                        waterfallVertices.Add(p2);
                        waterfallVertices.Add(p3);

                        waterfallTriangles.Add(baseIndex);
                        waterfallTriangles.Add(baseIndex + 1);
                        waterfallTriangles.Add(baseIndex + 2);

                        waterfallTriangles.Add(baseIndex + 1);
                        waterfallTriangles.Add(baseIndex + 3);
                        waterfallTriangles.Add(baseIndex + 2);
                    }
                }
            }
        }

        Mesh waterfallMesh = new Mesh();
        waterfallMesh.vertices = waterfallVertices.ToArray();
        waterfallMesh.triangles = waterfallTriangles.ToArray();
        waterfallMesh.RecalculateNormals();

        // Assign the waterfall mesh to a separate MeshFilter or combine with the existing mesh
    }

    void OnDrawGizmos()
    {
        // Nur zur optionalen Visualisierung von Wasserfall-Kanten
        if (waterSim == null || vertices == null) return; // Überprüfung hinzugefügt

        int res = waterSim.Resolution;
        var waterMap = waterSim.GetWaterMap();
        var terrMap = waterSim.GetTerrainHeights2D();
        if (waterMap == null || terrMap == null) return; // Zusätzliche Überprüfung hinzugefügt

        float thresh = waterfallThreshold;

        Gizmos.color = Color.cyan;
        Vector2Int[] neighbors = {
            new Vector2Int(1,0), new Vector2Int(-1,0),
            new Vector2Int(0,1), new Vector2Int(0,-1),
        };

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float myH = terrMap[x, y] + waterMap[x, y];
                foreach (var nb in neighbors)
                {
                    int nx = x + nb.x;
                    int ny = y + nb.y;
                    if (nx < 0 || nx >= res || ny < 0 || ny >= res) continue;

                    float neighH = terrMap[nx, ny] + waterMap[nx, ny];
                    float diff = myH - neighH;
                    if (diff > thresh)
                    {
                        int idx0 = y * res + x;
                        int idx1 = ny * res + nx;
                        Vector3 p0 = vertices[idx0];
                        Vector3 p1 = vertices[idx1];
                        Gizmos.DrawLine(p0, new Vector3(p0.x, p1.y, p0.z));
                        Gizmos.DrawLine(new Vector3(p0.x, p1.y, p0.z), p1);
                    }
                }
            }
        }
    }
}
