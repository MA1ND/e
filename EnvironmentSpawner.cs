using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using Unity.Collections;
using static MergedUtilities; // Füge Verweis auf MergedUtilities hinzu

public class EnvironmentSpawner : MonoBehaviour 
{
    [Header("Prefabs")]
    public GameObject villagePlaceholder;
    public GameObject treePlaceholder;
    public GameObject rockPlaceholder;
    public GameObject monumentPlaceholder;
    public GameObject roadPlaceholder;

    [Header("Spawn-Einstellungen")]
    public int villageCount = 3;
    public int treeCount = 100;
    public int rockCount = 30;
    public int monumentCount = 2;

    private TerrainGenerator terrainGen;

    public void SpawnEnvironment(float[,] heights, float seaLevelNorm)
    {
        terrainGen = GetComponent<TerrainGenerator>();
        if (terrainGen == null) return;

        var villages = SpawnVillages(villageCount, heights, seaLevelNorm);
        SpawnTrees(treeCount, heights, seaLevelNorm);
        SpawnRocks(rockCount, heights, seaLevelNorm);
        SpawnMonuments(monumentCount, heights, seaLevelNorm);
        ConnectVillagesWithRoads(villages);
    }

    private List<Vector3> SpawnVillages(int count, float[,] map, float seaLevelNorm)
    {
        List<Vector3> positions = new List<Vector3>();
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = FindRandomCoastalPoint(map, seaLevelNorm);
            positions.Add(pos);
            InstantiatePlaceholder(villagePlaceholder, pos);
        }
        return positions;
    }

    private void SpawnTrees(int count, float[,] map, float seaLevelNorm)
    {
        NativeArray<Vector3> treePositions = new NativeArray<Vector3>(count, Allocator.TempJob);
        var treeJob = new TreeSpawnJob
        {
            map = map,
            seaLevelNorm = seaLevelNorm,
            treePositions = treePositions,
            terrainWidth = terrainGen.terrainWidth,
            terrainLength = terrainGen.terrainLength,
            terrainHeight = terrainGen.terrainHeight
        };
        JobHandle treeJobHandle = treeJob.Schedule(count, 64);
        treeJobHandle.Complete();

        for (int i = 0; i < count; i++)
        {
            InstantiatePlaceholder(treePlaceholder, treePositions[i]);
        }
        treePositions.Dispose();
    }

    private void SpawnRocks(int count, float[,] map, float seaLevelNorm)
    {
        NativeArray<Vector3> rockPositions = new NativeArray<Vector3>(count, Allocator.TempJob);
        var rockJob = new RockSpawnJob
        {
            map = map,
            seaLevelNorm = seaLevelNorm,
            rockPositions = rockPositions,
            terrainWidth = terrainGen.terrainWidth,
            terrainLength = terrainGen.terrainLength,
            terrainHeight = terrainGen.terrainHeight
        };
        JobHandle rockJobHandle = rockJob.Schedule(count, 64);
        rockJobHandle.Complete();

        for (int i = 0; i < count; i++)
        {
            InstantiatePlaceholder(rockPlaceholder, rockPositions[i]);
        }
        rockPositions.Dispose();
    }

    private void SpawnMonuments(int count, float[,] map, float seaLevelNorm)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = FindRandomPosition(map, seaLevelNorm, 0.3f, 0.2f, 0.8f); // Monumente in mittleren bis hohen Lagen
            InstantiatePlaceholder(monumentPlaceholder, pos);
        }
    }

    private void ConnectVillagesWithRoads(List<Vector3> villagePositions)
    {
        if (villagePositions == null || villagePositions.Count < 2) return;

        Dictionary<Vector3, int> intersectionPoints = new Dictionary<Vector3, int>();
        List<Vector3> roadPoints = new List<Vector3>();

        for (int i = 0; i < villagePositions.Count; i++)
        {
            for (int j = i + 1; j < villagePositions.Count; j++)
            {
                var path = GenerateRoadPath(villagePositions[i], villagePositions[j]);
                roadPoints.AddRange(path);
                
                foreach (var point in path)
                {
                    if (!intersectionPoints.ContainsKey(point))
                        intersectionPoints[point] = 1;
                    else
                        intersectionPoints[point]++;
                }
            }
        }

        PaintRoadsAsSplines(roadPoints);
        CreateIntersections(intersectionPoints);
    }

    private List<Vector3> GenerateRoadPath(Vector3 start, Vector3 end)
    {
        List<Vector3> path = new List<Vector3>();
        // Implementiere Wegfindung hier
        return path;
    }

    private void CreateIntersections(Dictionary<Vector3, int> intersectionPoints)
    {
        foreach (var point in intersectionPoints)
        {
            if (point.Value > 2)
            {
                CreateIntersection(point.Key);
            }
        }
    }

    private void PaintRoadsAsSplines(List<Vector3> roadPoints)
    {
        TerrainData terrainData = terrainGen.GetComponent<Terrain>().terrainData;
        int alphaMapWidth = terrainData.alphamapWidth;
        int alphaMapHeight = terrainData.alphamapHeight;
        float[,,] alphaMaps = terrainData.GetAlphamaps(0, 0, alphaMapWidth, alphaMapHeight);

        // Verwenden Sie eine Spline-Bibliothek, um glatte Kurven zu erzeugen
        Spline spline = new Spline(roadPoints);

        foreach (var point in spline.GetPoints(20)) // Erhöhte Auflösung
        {
            Vector3 terrainPos = terrainGen.GetComponent<Terrain>().transform.InverseTransformPoint(point);
            int mapX = Mathf.RoundToInt((terrainPos.x / terrainGen.terrainWidth) * alphaMapWidth);
            int mapZ = Mathf.RoundToInt((terrainPos.z / terrainGen.terrainLength) * alphaMapHeight);

            for (int x = -5; x <= 5; x++) // Breitere Straßen
            {
                for (int z = -5; z <= 5; z++)
                {
                    int px = Mathf.Clamp(mapX + x, 0, alphaMapWidth - 1);
                    int pz = Mathf.Clamp(mapZ + z, 0, alphaMapHeight - 1);
                    alphaMaps[pz, px, 9] = 1; // Setze die Straßen-Textur (Index 9 für Straßen-Biom)
                }
            }
        }

        terrainData.SetAlphamaps(0, 0, alphaMaps);
    }

    private void CreateIntersection(Vector3 position)
    {
        TerrainData terrainData = terrainGen.GetComponent<Terrain>().terrainData;
        int alphaMapWidth = terrainData.alphamapWidth;
        int alphaMapHeight = terrainData.alphamapHeight;
        float[,,] alphaMaps = terrainData.GetAlphamaps(0, 0, alphaMapWidth, alphaMapHeight);

        Vector3 terrainPos = terrainGen.GetComponent<Terrain>().transform.InverseTransformPoint(position);
        int mapX = Mathf.RoundToInt((terrainPos.x / terrainGen.terrainWidth) * alphaMapWidth);
        int mapZ = Mathf.RoundToInt((terrainPos.z / terrainGen.terrainLength) * alphaMapHeight);

        for (int x = -7; x <= 7; x++) // Größere Kreuzungen
        {
            for (int z = -7; z <= 7; z++)
            {
                int px = Mathf.Clamp(mapX + x, 0, alphaMapWidth - 1);
                int pz = Mathf.Clamp(mapZ + z, 0, alphaMapHeight - 1);
                alphaMaps[pz, px, 9] = 1; // Setze die Straßen-Textur (Index 9 für Straßen-Biom)
            }
        }

        terrainData.SetAlphamaps(0, 0, alphaMaps);
    }

    private void InstantiatePlaceholder(GameObject prefab, Vector3 worldPos)
    {
        if (!prefab || !terrainGen) return;
        float terrainY = terrainGen.GetComponent<Terrain>().SampleHeight(new Vector3(worldPos.x, 0, worldPos.z));
        worldPos.y = terrainY + 0.2f;
        Instantiate(prefab, worldPos, Quaternion.identity, transform);
    }

    private Vector3 FindRandomCoastalPoint(float[,] map, float seaLevelNorm)
    {
        float thrMin = seaLevelNorm - 0.01f;
        float thrMax = seaLevelNorm + 0.01f;
        return FindRandomPosition(map, seaLevelNorm, 0.1f, thrMin, thrMax);
    }

    private Vector3 FindRandomPosition(float[,] map, float seaLevelNorm, float offset, float heightMin = 0f, float heightMax = 1f)
    {
        int w = map.GetLength(0);
        int h = map.GetLength(1);
        for (int i = 0; i < 1000; i++)
        {
            int rx = Random.Range(0, w);
            int ry = Random.Range(0, h);
            float val = map[rx, ry];
            if (val >= heightMin && val <= heightMax)
            {
                float posX = (float)rx / (w - 1) * terrainGen.terrainWidth;
                float posZ = (float)ry / (h - 1) * terrainGen.terrainLength;
                float posY = val * terrainGen.terrainHeight + offset;
                return new Vector3(posX, posY, posZ);
            }
        }
        return Vector3.zero;
    }

    private Vector2Int WorldToCoord(Vector3 w)
    {
        return new Vector2Int(
            Mathf.RoundToInt(w.x / 5f),
            Mathf.RoundToInt(w.z / 5f)
        );
    }

    private Vector3 CoordToWorld(Vector2Int c)
    {
        float x = c.x * 5f;
        float z = c.y * 5f;
        float y = terrainGen.GetComponent<Terrain>().SampleHeight(new Vector3(x, 0, z));
        return new Vector3(x, y, z);
    }

    private IEnumerable<Vector2Int> GetNeighbors(Vector2Int c)
    {
        var offsets = new Vector2Int[]{
            new Vector2Int(1,0), new Vector2Int(-1,0),
            new Vector2Int(0,1), new Vector2Int(0,-1)
        };
        foreach (var o in offsets) yield return c + o;
    }

    private List<Vector3> AdjustPathToTerrain(List<Vector3> rawPoints)
    {
        var result = new List<Vector3>();
        var terr = terrainGen.GetComponent<Terrain>();
        for (int i = 0; i < rawPoints.Count; i++)
        {
            var p = rawPoints[i];
            float y = terr.SampleHeight(new Vector3(p.x, 0, p.z));
            p.y = y + 0.1f;
            result.Add(p);
        }
        return result;
    }

    private float GetTerrainHeight(Vector2Int c)
    {
        return terrainGen.GetComponent<Terrain>().SampleHeight(CoordToWorld(c));
    }

    private void SpawnVegetation()
    {
        // ... Zusätzliche Spawn-Logik ...
    }

    private struct TreeSpawnJob : IJobParallelFor
    {
        [ReadOnly] public float[,] map;
        public float seaLevelNorm;
        public NativeArray<Vector3> treePositions;
        public float terrainWidth;
        public float terrainLength;
        public float terrainHeight;

        public void Execute(int index)
        {
            treePositions[index] = FindRandomPosition(map, seaLevelNorm, 0.05f, 0.3f, 0.7f);
        }

        private Vector3 FindRandomPosition(float[,] map, float seaLevelNorm, float offset, float heightMin, float heightMax)
        {
            int w = map.GetLength(0);
            int h = map.GetLength(1);
            for (int i = 0; i < 1000; i++)
            {
                int rx = UnityEngine.Random.Range(0, w);
                int ry = UnityEngine.Random.Range(0, h);
                float val = map[rx, ry];
                if (val >= heightMin && val <= heightMax)
                {
                    float posX = (float)rx / (w - 1) * terrainWidth;
                    float posZ = (float)ry / (h - 1) * terrainLength;
                    float posY = val * terrainHeight + offset;
                    return new Vector3(posX, posY, posZ);
                }
            }
            return Vector3.zero;
        }
    }

    private struct RockSpawnJob : IJobParallelFor
    {
        [ReadOnly] public float[,] map;
        public float seaLevelNorm;
        public NativeArray<Vector3> rockPositions;
        public float terrainWidth;
        public float terrainLength;
        public float terrainHeight;

        public void Execute(int index)
        {
            rockPositions[index] = FindRandomPosition(map, seaLevelNorm, 0.15f, 0.5f, 1f);
        }

        private Vector3 FindRandomPosition(float[,] map, float seaLevelNorm, float offset, float heightMin, float heightMax)
        {
            int w = map.GetLength(0);
            int h = map.GetLength(1);
            for (int i = 0; i < 1000; i++)
            {
                int rx = UnityEngine.Random.Range(0, w);
                int ry = UnityEngine.Random.Range(0, h);
                float val = map[rx, ry];
                if (val >= heightMin && val <= heightMax)
                {
                    float posX = (float)rx / (w - 1) * terrainWidth;
                    float posZ = (float)ry / (h - 1) * terrainLength;
                    float posY = val * terrainHeight + offset;
                    return new Vector3(posX, posY, posZ);
                }
            }
            return Vector3.zero;
        }
    }

    private float CalculateSlope(float[,] hmap, int x, int y, int w, int h, float sizeX, float sizeZ, float terrainHeight)
    {
        if (x < 0 || x >= w || y < 0 || y >= h)
        {
            throw new System.ArgumentOutOfRangeException("Indices are out of range.");
        }

        float dx = sizeX / (w - 1);
        float dz = sizeZ / (h - 1);

        int x1 = Mathf.Clamp(x - 1, 0, w - 1);
        int x2 = Mathf.Clamp(x + 1, 0, w - 1);
        int y1 = Mathf.Clamp(y - 1, 0, h - 1);
        int y2 = Mathf.Clamp(y + 1, 0, h - 1);

        float hL = hmap[x1, y];
        float hR = hmap[x2, y];
        float hD = hmap[x, y1];
        float hU = hmap[x, y2];

        float dZx = (hR - hL) * terrainHeight;
        float dZy = (hU - hD) * terrainHeight;

        float riseRunX = dZx / (2f * dx);
        float riseRunY = dZy / (2f * dz);

        float slopeRad = Mathf.Atan(Mathf.Sqrt(riseRunX * riseRunX + riseRunY * riseRunY));
        return slopeRad * Mathf.Rad2Deg;
    }

    private class Spline
    {
        private List<Vector3> points;

        public Spline(List<Vector3> controlPoints)
        {
            points = controlPoints;
        }

        public List<Vector3> GetPoints(int resolution)
        {
            List<Vector3> result = new List<Vector3>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                for (int j = 0; j < resolution; j++)
                {
                    float t = j / (float)resolution;
                    result.Add(Vector3.Lerp(points[i], points[i + 1], t));
                }
            }
            return result;
        }
    }

    public void SpawnVegetation(float[,] vegetationMap, float[,] heightMap, float seaLevel)
    {
        // Implementation...
    }
}
