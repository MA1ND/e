using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Unity.Collections;
using Unity.Jobs;
using static MergedUtils;

[System.Serializable]
public struct BiomeColorSettings
{
    public string biomeName;
    [Range(0f, 1f)] public float minHeight;
    [Range(0f, 1f)] public float maxHeight;
    public Gradient colorGradient;
    public bool autoAdjust;
    public float blendDistance; // Neue Property für weichere Übergänge
    public AnimationCurve heightInfluenceCurve; // Bessere Höhenkontrolle
    public float temperatureRange; // Optimaler Temperaturbereich
    public float moistureRange;    // Optimaler Feuchtigkeitsbereich
    public float adaptability;     // Wie gut sich das Biom an suboptimale Bedingungen anpasst
    public float seasonalVariation; // Wie stark sich das Biom mit Jahreszeiten verändert
    // Weitere Einstellungen je nach Bedarf
}

public class BiomeColorationSystem : MonoBehaviour
{
    [Header("Biome-Einstellungen")]
    public List<BiomeColorSettings> biomeSettings;

    [Header("Optionale Texturen")]
    public Texture2D biomeTexture;
    public bool generateTextureAutomatically = true;

    [Header("Fortgeschrittene Gelände-Faktoren")]
    public float moistureInfluence = 0.5f;
    public float temperatureInfluence = 0.3f;

    [Header("Automatische Anpassungen")]
    public bool autoScaleHeights = true;

    [Header("Performance")]
    [SerializeField] private int chunkSize = 64;
    [SerializeField] private bool useGPUAcceleration = true;

    [Header("Erweiterte Klimasimulation")]
    public float seasonalInfluence = 0.2f;
    public float windInfluence = 0.3f;
    public float rainShadowEffect = 0.5f;
    public float oceanProximityEffect = 0.4f;
    
    [Header("Performance-Optimierung")]
    public int threadCount = 8;
    public bool useJobSystem = true;
    public float lodDistance = 100f;

    [Header("Biome Übergänge")]
    public float biomeBlendDistance = 10f;
    public AnimationCurve blendCurve;
    public bool useVoronoiNoise = true;
    
    [Header("Jahreszeiten")]
    public Wetter wetterSystem;
    [Range(0f, 1f)] public float seasonalColorInfluence = 0.5f;
    public Gradient springColors;
    public Gradient summerColors;
    public Gradient autumnColors;
    public Gradient winterColors;

    private Terrain terrain;
    private TerrainData terrainData;

    private float[,] heightCache;
    private float[,] moistureCache;
    private float[,] temperatureCache;
    private float[,] windCache;
    private float[,] oceanDistanceCache;
    private ConcurrentDictionary<Vector2Int, float[,,]> chunkCache;
    private ComputeShader biomeComputeShader;

    private struct BiomeData
    {
        public float height;
        public float moisture;
        public float temperature;
        public float windExposure;
        public float oceanDistance;
    }

    void Awake()
    {
        chunkCache = new ConcurrentDictionary<Vector2Int, float[,,]>();
        if (useGPUAcceleration)
        {
            biomeComputeShader = Resources.Load<ComputeShader>("BiomeCalculation");
        }
    }

    void Start()
    {
        terrain = GetComponent<Terrain>();
        terrainData = terrain.terrainData;
        ApplyBiomeColors();
    }

    public void ApplyBiomeColors()
    {
        InitializeCaches();
        CalculateEnvironmentalFactors();
        ApplyBiomeWeights();
    }

    private void InitializeCaches()
    {
        int width = terrainData.alphamapWidth;
        int height = terrainData.alphamapHeight;
        
        heightCache = terrainData.GetHeights(0, 0, width, height);
        moistureCache = new float[width, height];
        temperatureCache = new float[width, height];
        windCache = new float[width, height];
        oceanDistanceCache = new float[width, height];
    }

    private float[] CalculateBiomeWeights(float height, float moisture, float temperature, float season)
    {
        float[] weights = new float[biomeSettings.Count];
        float totalWeight = 0f;

        for (int i = 0; i < biomeSettings.Count; i++)
        {
            var biome = biomeSettings[i];
            float baseWeight = CalculateBaseBiomeWeight(biome, height, moisture, temperature);
            float seasonalWeight = CalculateSeasonalWeight(biome, season);
            weights[i] = baseWeight * seasonalWeight;
            totalWeight += weights[i];
        }

        if (totalWeight > 0f)
        {
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] /= totalWeight;
            }
        }

        return weights;
    }

    private void CalculateEnvironmentalFactors()
    {
        var jobs = new NativeArray<JobHandle>(threadCount, Allocator.TempJob);
        int heightPerThread = terrainData.alphamapHeight / threadCount;

        // Konvertiere Arrays zu NativeArrays
        NativeArray<float> heightArray;
        NativeArray<float> moistureArray;
        NativeArray<float> temperatureArray;
        NativeArray<float> windArray;
        NativeArray<float> oceanArray;

        TerrainUtils.ConvertToNativeArray(heightCache, out heightArray);
        TerrainUtils.ConvertToNativeArray(moistureCache, out moistureArray);
        TerrainUtils.ConvertToNativeArray(temperatureCache, out temperatureArray);
        TerrainUtils.ConvertToNativeArray(windCache, out windArray);
        TerrainUtils.ConvertToNativeArray(oceanDistanceCache, out oceanArray);

        try
        {
            for (int i = 0; i < threadCount; i++)
            {
                var job = new TerrainSystem.Jobs.EnvironmentCalculationJob
                {
                    startY = i * heightPerThread,
                    endY = (i + 1) * heightPerThread,
                    width = terrainData.alphamapWidth,
                    heightCache = heightArray,
                    moistureCache = moistureArray,
                    temperatureCache = temperatureArray,
                    windCache = windArray,
                    oceanDistanceCache = oceanArray
                };
                jobs[i] = job.Schedule();
            }
            JobHandle.CompleteAll(jobs);
        }
        finally
        {
            heightArray.Dispose();
            moistureArray.Dispose();
            temperatureArray.Dispose();
            windArray.Dispose();
            oceanArray.Dispose();
            jobs.Dispose();
        }
    }

    private void ApplyBiomeWeights()
    {
        int width = terrainData.alphamapWidth;
        int height = terrainData.alphamapHeight;
        
        // Chunk-basierte Verarbeitung
        int numChunksX = Mathf.CeilToInt((float)width / chunkSize);
        int numChunksY = Mathf.CeilToInt((float)height / chunkSize);

        Parallel.For(0, numChunksX * numChunksY, index =>
        {
            int chunkX = index % numChunksX;
            int chunkY = index / numChunksX;
            ProcessChunk(chunkX, chunkY);
        });
    }

    private void ProcessChunk(int chunkX, int chunkY)
    {
        Vector2Int chunkKey = new Vector2Int(chunkX, chunkY);
        if (chunkCache.TryGetValue(chunkKey, out float[,,] cachedData))
        {
            ApplyCachedChunkData(chunkX, chunkY, cachedData);
            return;
        }

        if (useGPUAcceleration)
        {
            ProcessChunkGPU(chunkX, chunkY);
        }
        else
        {
            ProcessChunkCPU(chunkX, chunkY);
        }
    }

    private void ProcessChunkCPU(int chunkX, int chunkY)
    {
        int startX = chunkX * chunkSize;
        int startY = chunkY * chunkSize;
        int endX = Mathf.Min(startX + chunkSize, terrainData.alphamapWidth);
        int endY = Mathf.Min(startY + chunkSize, terrainData.alphamapHeight);

        float[,,] splatmapData = new float[chunkSize, chunkSize, biomeSettings.Count];

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                ProcessTerrainPoint(x, y, ref splatmapData);
            }
        }

        // Cache und anwenden der Daten
        chunkCache[new Vector2Int(chunkX, chunkY)] = splatmapData;
        // ApplySplatmap(chunkX, chunkY, splatmapData); // Temporär angepasst
    }

    private void ProcessTerrainPoint(int x, int y, ref float[,,] splatmapData)
    {
        float height = heightCache[x, y];
        float moisture = moistureCache[x, y];
        float temperature = temperatureCache[x, y];
        float season = wetterSystem != null ? wetterSystem.GetSeasonProgress() : 0f;

        // Berechne Biom-Gewichte
        float[] weights = CalculateBiomeWeights(height, moisture, temperature, season);
        
        // Wende Übergangseffekte an
        ApplyBiomeTransitions(x, y, ref weights);

        // Speichere die Gewichte
        for (int i = 0; i < weights.Length; i++)
        {
            splatmapData[x % chunkSize, y % chunkSize, i] = weights[i];
        }
    }

    private float CalculateBaseBiomeWeight(BiomeColorSettings biome, float height, float moisture, float temperature)
    {
        // Höheneinfluss
        float heightWeight = biome.heightInfluenceCurve.Evaluate(height);
        
        // Feuchtigkeitseinfluss
        float moistureWeight = CalculateMoistureWeight(moisture, biome.moistureRange, biome.adaptability);
        
        // Temperatureinfluss
        float temperatureWeight = CalculateTemperatureWeight(temperature, biome.temperatureRange, biome.adaptability);
        
        return heightWeight * moistureWeight * temperatureWeight;
    }

    private float CalculateSeasonalWeight(BiomeColorSettings biome, float season)
    {
        if (biome.seasonalVariation <= 0f) return 1f;

        // Berechne jahreszeitliche Anpassung
        float seasonalFactor = Mathf.Sin(season * 2f * Mathf.PI);
        return 1f + seasonalFactor * biome.seasonalVariation;
    }

    private void ApplyBiomeTransitions(int x, int y, ref float[] weights)
    {
        if (!useVoronoiNoise) return;

        // Verwende Voronoi-Noise für natürlichere Übergänge
        float noise = VoronoiNoise(x * 0.1f, y * 0.1f, biomeBlendDistance, 12345);
        
        // Wende die Übergangseffekte an
        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = Mathf.Lerp(weights[i], weights[i] * noise, blendCurve.Evaluate(noise));
        }
    }

    private float CalculateBiomeWeight(BiomeColorSettings biome, float elevation, float moisture, float temperature)
    {
        float heightInfluence = biome.heightInfluenceCurve.Evaluate(elevation);
        float moistureWeight = Mathf.SmoothStep(biome.minHeight, biome.maxHeight, moisture);
        float temperatureWeight = WeatherUtils.CalculateTemperatureWeight(temperature, biome.temperatureRange, biome.adaptability);
        
        float voronoiNoise = NoiseUtils.VoronoiNoise(
            elevation * 100f, 
            moisture * 100f, 
            temperature * 100f,
            12345
        );
        
        return heightInfluence * moistureWeight * temperatureWeight * (1 + voronoiNoise * 0.2f);
    }

    private float CalculateDesertWeight(float elevation, float moisture, float temperature)
    {
        return Mathf.Clamp01((temperature - moisture * 2f) * (1f - elevation * 0.5f));
    }

    private float CalculateGrassWeight(float elevation, float moisture, float temperature)
    {
        return Mathf.Clamp01(moisture * (1f - temperature * 0.5f) * (1f - elevation * 0.7f));
    }

    private float CalculateSnowWeight(float elevation, float temperature)
    {
        return Mathf.Clamp01(elevation * 2f - temperature);
    }

    private float CalculateJungleWeight(float elevation, float moisture, float temperature)
    {
        return Mathf.Clamp01(moisture * temperature * (1f - elevation));
    }

    private float CalculateMoisture(float worldX, float worldY, float height)
    {
        float baseMoisture = MultiOctave(worldX, worldY, 3, 0.5f, 2.0f, 500f, 12345);
        
        // Realistischere Feuchtigkeitsverteilung
        float heightEffect = Mathf.Lerp(1f, 0.3f, height);
        float windDirection = CalculateWindDirection(worldX, worldY);
        float rainShadow = CalculateRainShadowEffect(worldX, worldY, windDirection);
        float oceanMoisture = CalculateOceanMoistureEffect(worldX, worldY);
        
        return (baseMoisture * heightEffect * rainShadow * oceanMoisture) 
               * moistureInfluence;
    }

    private float CalculateTemperature(float worldX, float worldY, float height)
    {
        float baseTemp = MultiOctave(worldX, worldY, 4, 0.5f, 2.0f, 800f, 67890);
        
        // Verbesserte Höhenabhängigkeit
        float heightTemp = Mathf.Lerp(1f, 0f, height * 6.5f); // -6.5°C pro 1000m
        
        // Realistischere Breitengrad-Berechnung
        float latitude = (worldY / terrainData.size.z);
        float latitudeInfluence = CalculateLatitudeTemperature(latitude);
        
        // Jahreszeiten-Einfluss
        float season = CalculateSeasonalEffect(latitude);
        
        // Windeffekte
        float windChill = CalculateWindChill(worldX, worldY);
        
        // Meeres-Einfluss (mildert Temperaturschwankungen)
        float oceanEffect = CalculateOceanProximityEffect(worldX, worldY);
        
        return (baseTemp * heightTemp * latitudeInfluence * season * windChill * oceanEffect) 
               * temperatureInfluence;
    }

    private float CalculateLatitudeTemperature(float latitude)
    {
        // Komplexere Temperaturverteilung basierend auf Breitengrad
        float angle = (latitude - 0.5f) * Mathf.PI;
        return Mathf.Pow(Mathf.Cos(angle), 2) * 
               (1 - Mathf.Abs(latitude - 0.5f) * 0.5f);
    }

    private float CalculateWindChill(float x, float y)
    {
        float windSpeed = MultiOctave(x, y, 2, 0.5f, 2.0f, 600f, 34567);
        float elevation = heightCache[Mathf.FloorToInt(x), Mathf.FloorToInt(y)];
        return Mathf.Lerp(1f, 0.8f, windSpeed * elevation * windInfluence);
    }

    private float CalculateOceanProximityEffect(float x, float y)
    {
        float distToOcean = CalculateDistanceToOcean(x, y);
        return Mathf.Lerp(1f, 0.7f, Mathf.Exp(-distToOcean * oceanProximityEffect));
    }

    private float CalculateRainShadowEffect(float x, float y, float windDir)
    {
        // Berechnet den Regenschatten-Effekt hinter Bergen
        float upwindHeight = SampleHeightInDirection(x, y, windDir);
        float currentHeight = heightCache[Mathf.FloorToInt(x), Mathf.FloorToInt(y)];
        return Mathf.Lerp(1f, 0.5f, Mathf.Max(0, upwindHeight - currentHeight) * rainShadowEffect);
    }

    private void ProcessChunkGPU(int chunkX, int chunkY)
    {
        // GPU-beschleunigte Chunk-Verarbeitung
        // Implementation depends on your compute shader setup
        // ...existing code...
    }
    private void ApplyCachedChunkData(int chunkX, int chunkY, float[,,] cachedData)
    {
        int startX = chunkX * chunkSize;
        int startY = chunkY * chunkSize;
        int endX = Mathf.Min(startX + chunkSize, terrainData.alphamapWidth);
        int endY = Mathf.Min(startY + chunkSize, terrainData.alphamapHeight);

        float[,,] splatmapData = terrainData.GetAlphamaps(startX, startY,
            endX - startX, endY - startY);

        for (int y = 0; y < endY - startY; y++)
        {
            for (int x = 0; x < endX - startX; x++)
            {
                for (int i = 0; i < biomeSettings.Count; i++)
                {
                    splatmapData[y, x, i] = cachedData[y, x, i];
                }
            }
        }

        terrainData.SetAlphamaps(startX, startY, splatmapData);
    }

    private void GenerateBiomeTextureIfNeeded()
    {
        if (!generateTextureAutomatically || biomeSettings.Count == 0) return;
        // ...
    }

    private Color GetSeasonalBiomeColor(BiomeColorSettings biome, float height, float season)
    {
        // Bestimme die Jahreszeit
        if (season < 0.25f) // Frühling
        {
            return Color.Lerp(winterColors.Evaluate(height), springColors.Evaluate(height), season * 4f);
        }
        else if (season < 0.5f) // Sommer
        {
            return Color.Lerp(springColors.Evaluate(height), summerColors.Evaluate(height), (season - 0.25f) * 4f);
        }
        else if (season < 0.75f) // Herbst
        {
            return Color.Lerp(summerColors.Evaluate(height), autumnColors.Evaluate(height), (season - 0.5f) * 4f);
        }
        else // Winter
        {
            return Color.Lerp(autumnColors.Evaluate(height), winterColors.Evaluate(height), (season - 0.75f) * 4f);
        }
    }

    private void HandleWeatherChange(WetterZustand neuerZustand)
    {
        if (Time.time - lastWeatherUpdateTime < WEATHER_UPDATE_INTERVAL) return;
        
        UpdateTerrainForWeather(neuerZustand);
        lastWeatherUpdateTime = Time.time;
    }

    private void UpdateTerrainForWeather(WetterZustand wetter)
    {
        if (wetter.niederschlag > 0.5f)
        {
            var erosionStrength = wetter.niederschlag * weatherErosionMultiplier;
            ApplyRealTimeErosion(erosionStrength);
        }
        
        if (wetter.temperatur < 0 && wetter.niederschlag > 0)
        {
            AccumulateSnow(wetter.niederschlag);
        }
    }

    // Events für Wettersystem
    public delegate void WeatherChangedHandler(WetterZustand neuerZustand);
    public event WeatherChangedHandler OnWeatherChanged;
}