using System;
using System.Reflection;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering; // Hinzugefügt, um ReflectionProbeRefreshMode zu finden
using System.Collections.Generic;
using Unity.Mathematics;
using MathRandom = Unity.Mathematics.Random; // Alias für Unity.Mathematics.Random

// ============================================================================
// = Hier beginnt die große All-in-One-Klasse MergedUtilities (Beispielname). =
// ============================================================================
public static class MergedUtilities
{
    // -------------------------------------------------------------
    // 1) WeatherSystem, WeatherState, WetterZustand (Stubs)
    // -------------------------------------------------------------
    public class WeatherSystem
    {
        public WeatherState GetCurrentWeatherState() => new WeatherState();
    }

    public class WeatherState
    {
        public float precipitation = 0f;
        public float windSpeed = 0f;
        public Vector3 windDirection = Vector3.forward;
    }

    public class WetterZustand
    {
        public float bewoelkung = 0f;
        public float niederschlag = 0f;
        public float feuchtigkeit = 0f;
    }

    // -------------------------------------------------------------
    // 2) ReflectionUtils
    // -------------------------------------------------------------
    public static class ReflectionUtils
    {
        /// <summary>
        /// Beispiel einer Reflection-basierten Methode, um ein privates Feld aus einem Objekt auszulesen.
        /// </summary>
        public static T GetPrivateField<T>(object instance, string fieldName)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                throw new MissingFieldException($"Field '{fieldName}' not found on {instance.GetType()}");

            return (T)field.GetValue(instance);
        }

        /// <summary>
        /// Beispiel einer Reflection-basierten Methode, um ein privates Feld in einem Objekt zu setzen.
        /// </summary>
        public static void SetPrivateField<T>(object instance, string fieldName, T value)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                throw new MissingFieldException($"Field '{fieldName}' not found on {instance.GetType()}");

            field.SetValue(instance, value);
        }

        /// <summary>
        /// Dummy-Beispiel, das zeigt, wie du "using static ReflectionUtils;" verwenden könntest.
        /// </summary>
        public static void SomeReflectionHelperMethod()
        {
            Debug.Log("ReflectionUtils.SomeReflectionHelperMethod() aufgerufen.");
        }
    }

    // -------------------------------------------------------------
    // 3) HDReflectionProbeWrapper (für AdvancedLightingSystem u.ä.)
    // -------------------------------------------------------------
    public class HDReflectionProbeWrapper
    {
        private ReflectionProbe probe;

        public HDReflectionProbeWrapper(ReflectionProbe baseProbe)
        {
            probe = baseProbe;
        }

        /// <summary>
        /// Konfiguriert "HDR"-spezifische Einstellungen an der ReflectionProbe.
        /// Funktioniert auch dann, wenn kein echtes HDRP installiert ist.
        /// </summary>
        public void ConfigureHDSettings(float hdrIntensity)
        {
            if (probe)
            {
                probe.intensity = hdrIntensity;
                probe.hdr = true;
                probe.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
            }
        }
    }

    // -------------------------------------------------------------
    // 4) Mehrere Utility-Methoden (Noise, Erosion, Biome, etc.)
    // -------------------------------------------------------------

    // Beispiel: Alias für einen HDReflectionProbe
    public static object HDReflectionProbe => null; // Implementierung oder Proxy

    public static void ConfigureProbe(object probe)
    {
        // Implementiere oder leite an eine zentrale Methode weiter
    }

    public static Vector3[] GetOptimalProbePositions()
    {
        // Dummy-Implementierung
        return new Vector3[0];
    }

    public static float GetSunAzimuth()
    {
        return 0f;
    }

    public static void SimulateHydraulicErosion(ref AdvancedErosionSimulation.ErosionData data, float[,] moisture, WeatherSystem weather) { }
    public static void SimulateThermalErosion(ref AdvancedErosionSimulation.ErosionData data, float avgTemp) { }
    public static void SimulateWindErosion(ref AdvancedErosionSimulation.ErosionData data, WeatherSystem weather) { }
    public static void SimulateChemicalErosion(ref AdvancedErosionSimulation.ErosionData data, float[,] moisture) { }
    public static float GetAverageTemperature() { return 20f; }

    public static void SimulateParticle(ref AdvancedErosionSimulation.ErosionParticle particle, float[,] heightmap, int steps) { }

    public static Vector2 CalculateGradient(float[,] heightmap, Vector2Int pos, float radius)
    {
        // Implementiere interne Logik (ggf. getNeighborHeights und CalculateSlopeFromNeighbors)
        return Vector2.zero;
    }

    public static float CalculateSedimentCapacity(float speed, float waterVolume)
    {
        return 0f;
    }

    public static void DepositSediment(ref AdvancedErosionSimulation.ErosionParticle particle, float capacity, float[,] heightmap) { }
    public static void ErodeTerrainAdvanced(ref AdvancedErosionSimulation.ErosionParticle particle, float capacity, float[,] heightmap) { }

    public static void ApplyHeightmap(TerrainData terrainData, float[,] heights)
    {
        terrainData.SetHeights(0, 0, heights);
    }

    public static float[,] GenerateHeightmapCPU(int resolution, float width, float length)
    {
        // Dummy-Implementierung
        return new float[resolution, resolution];
    }

    public static float[] CreateMoistureMap(int resolution)
    {
        // Dummy-Implementierung
        return new float[resolution * resolution];
    }

    public static void InitializeComputeBuffers(ref ComputeBuffer heightmapBuffer, ref NativeArray<float> heightmapNative, int resolution)
    {
        heightmapBuffer = new ComputeBuffer(resolution * resolution, sizeof(float));
        heightmapNative = new NativeArray<float>(resolution * resolution, Allocator.Persistent);
    }

    public static float[,] NativeArrayToArray(NativeArray<float> nativeArray, int resolution)
    {
        float[,] result = new float[resolution, resolution];
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                result[x, y] = nativeArray[y * resolution + x];
            }
        }
        return result;
    }

    public static Vector2 GetWeatherWindDirection(WeatherSystem weatherSystem)
    {
        // Leite z. B. weatherSystem.windDirection oder einen Default zurück
        return Vector2.right;
    }

    public static void ApplyRealTimeErosion(TerrainData terrainData, float erosionStrength) { }
    public static void AccumulateSnow(TerrainData terrainData, float precipitation) { }

    // Biome-Hilfsfunktionen
    public static void ApplySplatmap() { }
    public static float CalculateTemperatureWeight(float value) { return value; }
    public static void WeatherUtils() { }
    public static void NoiseUtils() { }
    public static Vector2 CalculateWindDirection() { return Vector2.right; }
    public static float CalculateOceanMoistureEffect() { return 1f; }
    public static float CalculateSeasonalEffect() { return 1f; }

    private static float lastWeatherUpdateTime;
    public static float GetLastWeatherUpdateTime() { return lastWeatherUpdateTime; }
    public static void SetLastWeatherUpdateTime(float time) { lastWeatherUpdateTime = time; }
    public static float GetWeatherUpdateInterval() { return 1f; }

    public static void UpdateBiomeForWeather(WeatherState weather) { }

    // ===========================
    //  MergedWetterZustand-Struktur
    // ===========================
    public struct MergedWetterZustand
    {
        public float temperatur;
        public float niederschlag;

        public MergedWetterZustand(float temperatur, float niederschlag)
        {
            this.temperatur = temperatur;
            this.niederschlag = niederschlag;
        }
    }

    // ===========================
    //   Geologie-Methoden
    // ===========================
    public static float CalculateTectonicInfluence(float x, float y)
    {
        // Implementiere die Logik zur Berechnung des tektonischen Einflusses
        return Mathf.PerlinNoise(x * 0.01f, y * 0.01f);
    }

    public static float CalculateGeologicalAge(float x, float y)
    {
        // Implementiere die Logik zur Berechnung des geologischen Alters
        return Mathf.PerlinNoise(x * 0.005f, y * 0.005f);
    }

    public static float GetGeologicalAgeModifier(float age)
    {
        // Implementiere die Logik zur Berechnung des geologischen Altersmodifikators
        return Mathf.Lerp(0.5f, 1.5f, age);
    }

    public static void ApplyVolcanicFeatures() { }
    public static void ApplyGeologicalErosion() { }

    public static Vector2[] GenerateFaultLines()
    {
        // Implementiere die Logik zur Erzeugung von Verwerfungslinien
        return new Vector2[0];
    }

    public static void ApplyFaultDisplacement(Vector2 fault) { }

    public static Vector3[] FindVolcanicHotspots(float[,] heights)
    {
        // Implementiere die Logik zur Identifizierung von Vulkan-Hotspots
        return new Vector3[0];
    }

    public static void ApplyVolcanicFormation(ref float[,] heights, Vector3 hotspot) { }

    public static Vector3 CalculateOptimalRiverSource(List<Vector3> existingSources)
    {
        // Implementiere die Logik zur Berechnung der optimalen Flussquelle
        return Vector3.zero;
    }

    public static Vector3 CalculateRiverFlowDirection(Vector3 pos, float flow)
    {
        // Implementiere die Logik zur Berechnung des Flussflussrichtung
        return Vector3.zero;
    }

    public static float UpdateRiverFlow(float flow, Vector3 currPos, Vector3 nextPos)
    {
        // Implementiere die Logik zur Aktualisierung des Flussflusses
        return flow * 0.95f;
    }

    /* ===========================
     * WetterUtils-Methoden
     * ===========================
     */
    public static float BerechneTemperaturGewichtung(float aktuell, float optimal, float anpassungsfaehigkeit)
    {
        // Einfacher Gaussian-ähnlicher Abfall
        float differenz = Mathf.Abs(aktuell - optimal);
        return Mathf.Exp(-differenz * (1f - anpassungsfaehigkeit));
    }

    public static float BerechneWindrichtung(float x, float y)
    {
        // Erzeuge eine einfache PerlinNoise-basierte Windrichtung [0..360]
        return Mathf.PerlinNoise(x * 0.01f, y * 0.01f) * 360f;
    }

    public static float BerechneSaisonEffekt(float breitengrad)
    {
        // Einfaches jahreszeitliches Modell über Realzeit in Tagen
        float saisonWinkel = Time.time * Mathf.PI * 2f / (365f * 24f * 3600f);
        return Mathf.Cos(saisonWinkel) * Mathf.Cos(breitengrad * Mathf.Deg2Rad);
    }

    public static float BerechneWindkuehlung(float temperatur, float windgeschwindigkeit)
    {
        // Ein sehr einfaches Windchill-Modell
        return temperatur - (windgeschwindigkeit * 0.5f);
    }

    public static bool IstSchneefall(MergedWetterZustand wetter)
    {
        // Schnee fällt, wenn T < 2°C und Niederschlag vorhanden
        return wetter.temperatur < 2f && wetter.niederschlag > 0f;
    }

    /* ===========================
     * RiverUtils-Methoden
     * ===========================
     */
    public static Vector3 CalculateOptimalRiverSource(float[,] heightmap, float minHeight)
    {
        // Suche den höchsten Punkt > minHeight
        int w = heightmap.GetLength(0);
        int h = heightmap.GetLength(1);
        float bestVal = minHeight;
        Vector2Int bestPos = new Vector2Int(-1, -1);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float val = heightmap[x, y];
                if (val > bestVal)
                {
                    bestVal = val;
                    bestPos = new Vector2Int(x, y);
                }
            }
        }
        if (bestPos.x < 0) return Vector3.zero;
        return new Vector3(bestPos.x, bestVal, bestPos.y);
    }

    public static void ApplyRiverErosion(float[,] heightmap, List<Vector3> riverPoints, float erosionStrength)
    {
        // Verringere entlang der RiverPoints die Höhe, um einen Flussgraben zu simulieren
        int w = heightmap.GetLength(0);
        int h = heightmap.GetLength(1);
        foreach (var rp in riverPoints)
        {
            int rx = Mathf.RoundToInt(rp.x);
            int ry = Mathf.RoundToInt(rp.z);
            if (rx < 0 || ry < 0 || rx >= w || ry >= h) continue;
            heightmap[rx, ry] -= erosionStrength * 0.5f;

            // Verbreiterung
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = rx + dx;
                    int ny = ry + dy;
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                    heightmap[nx, ny] -= erosionStrength * 0.2f;
                }
            }
        }
    }

    public static void GenerateRiverDeltas(float[,] heightmap, List<Vector3> riverEndPoints)
    {
        // Erhöhe im Mündungsbereich die Sedimente
        int w = heightmap.GetLength(0);
        int h = heightmap.GetLength(1);
        foreach (var endPt in riverEndPoints)
        {
            int ex = Mathf.RoundToInt(endPt.x);
            int ey = Mathf.RoundToInt(endPt.z);
            if (ex < 0 || ey < 0 || ex >= w || ey >= h) continue;
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    int nx = ex + dx;
                    int ny = ey + dy;
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                    heightmap[nx, ny] += 0.02f;
                }
            }
        }
    }

    // ===========================
    // BiomeUtils-Methoden
    // ===========================
    public static float CalculateMoistureWeight(float current, float optimal, float adaptability)
    {
        float diff = Mathf.Abs(current - optimal);
        return Mathf.Exp(-diff * (1f - adaptability));
    }

    // Beispiel, wenn BiomeColorSettings existiert:
    public static float GetTemperatureWeight(float temperature, BiomeColorSettings biome)
    {
        float diff = Mathf.Abs(temperature - biome.temperatureRange);
        return Mathf.Exp(-diff * (1f - biome.adaptability));
    }

    public static float CalculateDistanceToOcean(float x, float y)
    {
        return Mathf.PerlinNoise(x * 0.01f, y * 0.01f) * 100f;
    }

    public static float SampleHeightInDirection(float x, float y, float direction)
    {
        float dx = Mathf.Cos(direction * Mathf.Deg2Rad);
        float dy = Mathf.Sin(direction * Mathf.Deg2Rad);
        return Mathf.PerlinNoise((x + dx * 10f) * 0.01f, (y + dy * 10f) * 0.01f);
    }

    // --------------------------------------------
    // Beispiel-Struktur, wie es im Code referenziert wurde.
    // --------------------------------------------
    public struct BiomeColorSettings
    {
        public string biomeName;
        [Range(0f, 1f)] public float minHeight;
        [Range(0f, 1f)] public float maxHeight;
        public Gradient colorGradient;
        public bool autoAdjust;
        public float blendDistance;
        public AnimationCurve heightInfluenceCurve;
        public float temperatureRange;
        public float moistureRange;
        public float adaptability;
        public float seasonalVariation;
    }

    // -------------------------------------------------------------
    // 5) ErosionParameters
    // -------------------------------------------------------------
    public struct ErosionParameters
    {
        public float rainfall;
        public float temperature;
        public float windSpeed;
        public Vector2 windDirection;
    }

    // -------------------------------------------------------------
    // 6) Beispiel-Methoden für Erosion / Frost / Chemie
    // -------------------------------------------------------------
    public static float CalculateFrostDamage(float temperature, float waterContent)
    {
        if (temperature >= 0f) return 0f;
        return Mathf.Abs(temperature) * waterContent * 0.1f;
    }

    public static float CalculateAcidity(float rainIntensity, float pollution)
    {
        return Mathf.Lerp(7f, 4f, rainIntensity * pollution);
    }

    public static float CalculateDissolvedMinerals(float acidity, float mineralContent)
    {
        float phDiff = Mathf.Abs(7f - acidity);
        return phDiff * mineralContent * 0.05f;
    }

    public static void ApplyFrostErosion(Vector2Int pos, float damage, float[,] heightmap)
    {
        if (IsInBounds(pos, heightmap.GetLength(0), heightmap.GetLength(1)))
        {
            heightmap[pos.x, pos.y] -= damage;
        }
    }

    public static bool IsInBounds(Vector2Int pos, int width, int height)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }

    // -------------------------------------------------------------
    // 7) NoiseUtils (GPU-Compute, Perlin, RidgedNoise, etc.)
    // -------------------------------------------------------------
    private static ComputeShader noiseComputeShader;

    static MergedUtilities()
    {
        // Optionaler Versuch, einen ComputeShader "NoiseComputation.compute" aus Resources zu laden
        noiseComputeShader = Resources.Load<ComputeShader>("NoiseComputation");
    }

    public static float MultiOctave(float x, float y, int octaves, float persistence, float lacunarity, float scale, int offset)
    {
        if (scale <= 1e-5f) scale = 0.0001f;

        float sum = 0f;
        float amplitude = 1f;
        float freq = 1f;
        float maxAmp = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float sx = (x + offset * 0.1f + i * 13.13f) / scale * freq;
            float sy = (y + offset * 0.1f + i * 17.17f) / scale * freq;
            float val = Mathf.PerlinNoise(sx, sy);

            sum += val * amplitude;
            maxAmp += amplitude;
            amplitude *= persistence;
            freq *= lacunarity;
        }
        return sum / maxAmp;
    }

    public static float RidgedNoise(float x, float y, int octaves, float persistence, float lacunarity, float scale, int offset, float gain)
    {
        if (scale <= 1e-5f) scale = 0.0001f;

        float sum = 0f;
        float amplitude = 1f;
        float freq = 1f;
        float weight = 1f;
        float maxAmp = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float sx = (x + offset * 0.1f + i * 13.13f) / scale * freq;
            float sy = (y + offset * 0.1f + i * 17.17f) / scale * freq;
            float val = Mathf.PerlinNoise(sx, sy);

            // Ridged
            float rid = 1f - Mathf.Abs(val * 2f - 1f);
            rid *= rid;
            rid *= weight;

            sum += rid * amplitude;
            maxAmp += amplitude;

            weight = Mathf.Clamp01(rid * gain);
            amplitude *= persistence;
            freq *= lacunarity;
        }
        return sum / maxAmp;
    }

    public static float VoronoiNoise(float x, float y, float scale, int seed)
    {
        float scaledX = x / scale;
        float scaledY = y / scale;

        int xi = Mathf.FloorToInt(scaledX);
        int yi = Mathf.FloorToInt(scaledY);
        float minDist = float.MaxValue;

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                Vector2 neighbor = new Vector2(xi + i, yi + j);
                Vector2 point = neighbor + new Vector2(Hash(neighbor.x, neighbor.y, seed),
                                                       Hash(neighbor.y, neighbor.x, seed));
                float dist = Vector2.Distance(new Vector2(scaledX, scaledY), point);
                minDist = Mathf.Min(minDist, dist);
            }
        }
        return minDist;
    }

    private static float Hash(float x, float y, int seed)
    {
        return Mathf.Abs(Mathf.Sin(Vector2.Dot(new Vector2(x, y), new Vector2(12.9898f, 78.233f)) + seed) * 43758.5453f) % 1;
    }

    public static void GenerateNoiseMapGPU(float[,] map, int width, int height, float scale, int octaves, float persistence, float lacunarity, int seed)
    {
        if (noiseComputeShader == null)
        {
            Debug.LogError("Noise compute shader not found!");
            return;
        }
        ComputeBuffer mapBuffer = new ComputeBuffer(width * height, sizeof(float));
        mapBuffer.SetData(map);

        noiseComputeShader.SetBuffer(0, "Result", mapBuffer);
        noiseComputeShader.SetInt("Width", width);
        noiseComputeShader.SetInt("Height", height);
        noiseComputeShader.SetFloat("Scale", scale);
        noiseComputeShader.SetInt("Octaves", octaves);
        noiseComputeShader.SetFloat("Persistence", persistence);
        noiseComputeShader.SetFloat("Lacunarity", lacunarity);
        noiseComputeShader.SetInt("Seed", seed);

        int threadGroupsX = Mathf.CeilToInt(width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(height / 8.0f);
        noiseComputeShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        mapBuffer.GetData(map);
        mapBuffer.Release();
    }

    // -------------------------------------------------------------
    // 8) RandomExtensions
    // -------------------------------------------------------------
    public static float Range(this MathRandom rnd, float min, float max)
    {
        return min + (max - min) * rnd.NextFloat();
    }

    public static int Range(this MathRandom rnd, int min, int max)
    {
        return rnd.NextInt(min, max);
    }

    public static MathRandom ToMathematicsRandom(this System.Random random)
    {
        return new MathRandom((uint)random.Next());
    }

    public static MathRandom CreateMathematicsRandom(uint seed)
    {
        return new MathRandom(seed);
    }

    public static void ConfigureReflectionProbe(ReflectionProbe probe)
    {
        probe.intensity = 1f;
        probe.blendDistance = 1f;
        probe.boxProjection = true;
        probe.importance = 1;
        probe.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
    }


    // -------------------------------------------------------------
    // 9) Beispiel: ErosionJobs (Hydraulic, Thermal, Wind, Chemical)
    // -------------------------------------------------------------
    // Diese werden z. B. in AdvancedErosionSimulation.cs aufgerufen
    // und können hier integriert bleiben.
    // -------------------------------------------------------------
    public static class ErosionJobs
    {
        using Unity.Jobs; // Muss hier möglich sein (lokale using-Direktive)
        using Unity.Burst;

        [BurstCompile]
        public struct HydraulicErosionJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> heightMap;
            [ReadOnly] public NativeArray<float> moistureMap;

            public NativeArray<float> erosionMap;
            public NativeArray<float> sedimentMap;

            public float rainfall;
            public float deltaTime;

            public void Execute(int index)
            {
                float mo = moistureMap[index];
                float baseErosion = rainfall * mo * 0.1f * deltaTime;

                erosionMap[index] += baseErosion;
                sedimentMap[index] += baseErosion * 0.5f;
            }
        }

        [BurstCompile]
        public struct ThermalErosionJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> heightMap;
            [ReadOnly] public NativeArray<float> hardnessMap;
            public NativeArray<float> erosionMap;
            public float temperature;
            public float deltaTime;

            public void Execute(int index)
            {
                float hardness = hardnessMap[index];
                float thermalFactor = temperature * (1f - hardness) * 0.01f * deltaTime;
                erosionMap[index] += thermalFactor;
            }
        }

        [BurstCompile]
        public struct WindErosionJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> heightMap;
            public NativeArray<float> erosionMap;
            public float windSpeed;
            public float windDirection;
            public float deltaTime;

            public void Execute(int index)
            {
                float windFactor = windSpeed * 0.02f * deltaTime;
                erosionMap[index] += windFactor;
            }
        }

        [BurstCompile]
        public struct ChemicalErosionJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> heightMap;
            [ReadOnly] public NativeArray<float> hardnessMap;
            [ReadOnly] public NativeArray<float> moistureMap;
            public NativeArray<float> erosionMap;
            public float deltaTime;

            public void Execute(int index)
            {
                float m = moistureMap[index];
                float h = hardnessMap[index];
                float chemicalFactor = m * (1f - h) * 0.05f * deltaTime;
                erosionMap[index] += chemicalFactor;
            }
        }
    }
}