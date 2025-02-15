using UnityEngine;
using static MergedUtilities; // Füge Verweis auf MergedUtilities hinzu

public class ShallowWaterSimulation
{
    public int Resolution { get; private set; }
    public float CellSize { get; private set; }
    public float[,] waterHeight;
    public Vector2[,] waterVelocity;
    
    public ShallowWaterSimulation(int resolution, float cellSize)
    {
        Resolution = resolution;
        CellSize = cellSize;
        waterHeight = new float[resolution, resolution];
        waterVelocity = new Vector2[resolution, resolution];
    }

    public void Step(float deltaTime)
    {
        // Implementiere die Shallow Water Equations
        for (int i = 1; i < Resolution - 1; i++)
        {
            for (int j = 1; i < Resolution - 1; j++)
            {
                // Berechne Wasserfluss
                UpdateWaterFlow(i, j, deltaTime);
            }
        }
    }

    private void UpdateWaterFlow(int x, int y, float deltaTime)
    {
        // Implementiere Wasserfluss-Berechnung
        float gravity = 9.81f;
        Vector2 gradient = CalculateWaterGradient(x, y);
        waterVelocity[x, y] += gradient * gravity * deltaTime;
        waterHeight[x, y] += (waterVelocity[x, y].magnitude * deltaTime) / CellSize;
    }

    private Vector2 CalculateWaterGradient(int x, int y)
    {
        // Verwende MergedUtilities Methoden hier
        return CalculateRiverFlowDirection(waterHeight, new Vector2Int(x, y));
    }

    public float[,] GetWaterMap()
    {
        return waterHeight;
    }

    public float[,] GetTerrainHeights2D()
    {
        // Implementierung der Terrain-Höhen-Konvertierung
        float[,] heights = new float[Resolution, Resolution];
        // Konvertierungslogik hier
        return heights;
    }

    public static ShallowWaterSimulation operator +(ShallowWaterSimulation a, ShallowWaterSimulation b)
    {
        int resolution = a.Resolution;
        var result = new ShallowWaterSimulation(resolution, a.CellSize);

        for (int i = 0; i < resolution; i++)
        {
            for (int j = 0; j < resolution; j++)
            {
                result.waterHeight[i, j] = a.waterHeight[i, j] + b.waterHeight[i, j];
                result.waterVelocity[i, j] = a.waterVelocity[i, j] + b.waterVelocity[i, j];
            }
        }

        return result;
    }

    public static ShallowWaterSimulation operator *(ShallowWaterSimulation sim, float factor)
    {
        var result = new ShallowWaterSimulation(sim.Resolution, sim.CellSize);
        
        for (int i = 0; i < sim.Resolution; i++)
        {
            for (int j = 0; j < sim.Resolution; j++)
            {
                result.waterHeight[i, j] = sim.waterHeight[i, j] * factor;
                result.waterVelocity[i, j] = sim.waterVelocity[i, j] * factor;
            }
        }
        
        return result;
    }

    public bool IsActive => waterHeight != null && waterVelocity != null;

    public static bool operator !(ShallowWaterSimulation sim)
    {
        return !sim.IsActive;
    }
}
