using UnityEngine;
using System;
using System.Threading.Tasks;
using Unity.Jobs;
using Unity.Collections;
using static MergedUtilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Terrain))]
public class TerrainGenerator : MonoBehaviour
{
    public event Action<TerrainData> OnGenerationFinished;

    // =========================
    // 1) Globale Einstellungen
    // =========================
    [Header("World Settings")]
    [Tooltip("Ändert die gesamte Welt auf reproduzierbare Weise.")]
    public int worldSeed = 12345;

    [Range(0f, 1f)]
    [Tooltip("Prozentualer Anteil der Fläche, der unter Wasser liegen soll (0..1).")]
    public float oceanCoverage = 0.3f;

    [Range(0f, 1f)]
    [Tooltip("Wie stark sich Kontinente als klar abgegrenzte Inseln formen (0 = weich, 1 = hart).")]
    public float continentSeparation = 0.6f;

    [Header("World Temperature")]
    [Tooltip("Globale Basistemperatur auf Meereshöhe, in Grad Celsius.")]
    public float globalTemperature = 25f;

    [Tooltip("Temperaturabfall pro 100 Höhenmeter.")]
    public float temperatureFalloffPer100m = 6f;

    // =========================
    // 2) Terrain Dimensionen
    // =========================
    [Header("Terrain Dimensions")]
    public int heightmapResolution = 513;
    public float terrainWidth = 1000f;
    public float terrainLength = 1000f;
    public float terrainHeight = 200f;

    // =========================
    // 3) Berge / Hügel / Canyons
    // =========================
    [Header("Mountains / Hills")]
    [Range(0f, 1f)]
    [Tooltip("Höhe der Berge (max. Beitrag zur Geländehöhe).")]
    public float mountainHeight = 0.4f;

    [Tooltip("Wie zerklüftet (rugged) die Berge sind (Gain bei Ridged Noise).")]
    public float mountainRuggedness = 2f;

    [Range(0f, 1f)]
    [Tooltip("Höhe der Hügel (Detail Contribution).")]
    public float hillHeight = 0.3f;

    [Header("Canyons")]
    [Range(0f, 1f)]
    [Tooltip("Tiefe der Schluchten (z.B. 0.2).")]
    public float canyonDepth = 0.2f;

    // =========================
    // 4) Biome
    // =========================
    [Header("Biome Settings")]
    [Tooltip("Skalierung des Biome-Noises (steuert die Größe der Biome).")]
    public float biomeScale = 200f;

    [Tooltip("Feuchtigkeits-Noise-Größe (je größer, desto sanftere Übergänge).")]
    public float humidityScale = 500f;

    [Range(0f, 1f)]
    [Tooltip("Ab welcher Feuchtigkeit das Gebiet als Dschungel zählt.")]
    public float humidityThresholdJungle = 0.6f;

    [Tooltip("Ob der Breitengrad/Position auf der Y-Achse ins Klima einfließen soll.")]
    public bool useLatitudeForClimate = true;

    [Range(0f, 1f), Tooltip("Legt fest, wie häufig das Desert-Biom auftritt (0=nie, 1=max.)")]
    public float desertFrequency = 1f;
    [Tooltip("Trockenheitsfaktor: Je höher, desto karger das Desert-Biom wirkt.")]
    public float desertDryness = 1.0f;

    [Range(0f, 1f), Tooltip("Legt fest, wie häufig das Canyon-Biom auftritt.")]
    public float canyonFrequency = 1f;
    [Tooltip("Zusätzliche Erosionsstärke für das Canyon-Biom.")]
    public float canyonErosionFactor = 1.0f;

    [Range(0f, 1f), Tooltip("Legt fest, wie häufig das Grass-Biom auftritt.")]
    public float grassFrequency = 1f;
    [Tooltip("Wie dicht das Gras erscheinen soll.")]
    public float grassDensity = 1.0f;

    [Range(0f, 1f), Tooltip("Legt fest, wie häufig das Rock-Biom auftritt.")]
    public float rockFrequency = 1f;
    [Tooltip("Gibt an, wie rau und felsig das Rock-Biom erscheint.")]
    public float rockRoughness = 1.0f;

    [Range(0f, 1f), Tooltip("Legt fest, wie häufig das Snow-Biom auftritt.")]
    public float snowFrequency = 1f;
    [Tooltip("Beeinflusst, wie dick oder ausgeprägt der Schnee sein soll.")]
    public float snowCoverage = 1.0f;

    [Range(0f, 1f), Tooltip("Legt fest, wie häufig das Jungle-Biom auftritt.")]
    public float jungleFrequency = 1f;
    [Tooltip("Je höher dieser Wert, desto dichter und feuchter das Jungle-Biom.")]
    public float jungleHumidityFactor = 1.0f;

    [Range(0f, 1f), Tooltip("Legt fest, wie häufig das Arctic-Biom auftritt.")]
    public float arcticFrequency = 1f;
    [Tooltip("Zusätzliche Kältestärke, je höher desto frostiger das Arctic-Biom.")]
    public float arcticColdness = 1.0f;

    [Range(0f, 1f), Tooltip("Legt fest, wie häufig das Beach-Biom auftritt.")]
    public float beachFrequency = 1f;
    [Tooltip("Beeinflusst, wie weich bzw. sandig das Beach-Biom erscheint.")]
    public float beachSandSoftness = 1.0f;

    [Range(0f, 1f), Tooltip("Legt fest, wie häufig das Unterwasser-Biom auftritt.")]
    public float underwaterFrequency = 1f;
    [Tooltip("Beeinflusst die Tiefe oder Unterwasser-Ausprägung.")]
    public float underwaterDepthFactor = 1.0f;

    // =========================
    // 5) Erosion (optional)
    // =========================
    [Header("Erosion (simple)")]
    public int erosionIterations = 20;
    public float erosionStrength = 0.02f;
    public float erosionSedimentFactor = 0.5f;

    // =========================
    // 6) Terrain Layers (Texturen)
    // =========================
    [Header("Terrain Layers")]
    public TerrainLayer desertLayer;
    public TerrainLayer canyonLayer;
    public TerrainLayer grassLayer;
    public TerrainLayer rockLayer;
    public TerrainLayer snowLayer;
    public TerrainLayer jungleLayer;
    public TerrainLayer arcticLayer;
    public TerrainLayer beachLayer;
    public TerrainLayer underwaterLayer;
    public TerrainLayer roadLayer; // Hinzugefügt

    [Header("River Settings")]
    [Tooltip("Anzahl der Flüsse, die generiert werden sollen.")]
    public int riverCount = 5;

    [Tooltip("Maximale Länge eines Flusses in Einheiten.")]
    public float maxRiverLength = 500f;

    [Tooltip("Wie stark sich der Fluss in die Landschaft eingräbt.")]
    public float riverDepth = 0.05f;

    [Tooltip("Wie stark sich der Fluss in die Landschaft windet.")]
    public float riverCurvature = 0.5f;

    [Header("Generation Trigger")]
    [Tooltip("Drücken Sie diesen Button, um das Terrain neu zu generieren.")]
    public bool regenerateTerrain = false;

    [Header("GPU Acceleration")]
    public bool useGPUGeneration = true;
    public ComputeShader terrainComputeShader;
    
    [Header("Advanced Geological Features")]
    [Range(0f, 1f)]
    public float tectonicActivityScale = 0.5f;
    public float faultLineFrequency = 0.3f;
    public float volcanicActivityScale = 0.4f;
    
    [Header("Advanced River System")]
    public bool useAdvancedRiverSystem = true;
    public int riverSourceCount = 10;
    public float minRiverFlow = 0.1f;
    public float erosionStrengthRivers = 0.5f;
    public AnimationCurve riverWidthGradient;
    
    [Header("Terrain Detail Enhancement")]
    public bool generateMicroDetail = true;
    public float microDetailScale = 0.1f;
    public int microDetailOctaves = 3;

    [Header("Geological Generation")]
    public bool useGeologicalSimulation = true;
    public float tectonicStrength = 1f;
    public float erosionIntensity = 1f;
    [Range(0, 1)] public float riverErosionStrength = 0.5f;
    
    [Header("Advanced Vegetation")]
    public bool useVegetationSystem = true;
    public float vegetationDensity = 1f;
    public AnimationCurve vegetationHeightCurve;
    public float treeLine = 0.8f;
    
    [Header("Climate Integration")]
    public WeatherSystem.WeatherSystem weatherSystem;
    public float weatherErosionMultiplier = 1f;
    public bool useRealTimeWeatherErosion = true;

    // =========================
    // Private Felder
    // =========================
    private Terrain terrain;
    private TerrainData terrainData;
    private float[,] finalHeights;

    // Automatisch berechneter Meeresspiegel (normalisierter Wert)
    private float seaLevelNormalized = 0f;

    // Zufallszahlengenerator + Offsets
    private System.Random rand;
    private int offsetContinent;
    private int offsetDetail;
    private int offsetMountain;
    private int offsetCanyon;
    private int offsetBiome;
    private int offsetHumidity;
    private int offsetTemperature;

    // Falls wir eine Debug-Ebene anzeigen, merken wir uns das Plane-Objekt hier:
    private GameObject biomeDebugPlane;

    private ComputeBuffer heightmapBuffer;
    private ComputeBuffer biomeBuffer;
    private NativeArray<float> heightmapNative;
    private AdvancedErosionSimulation erosionSimulation;
    private float lastWeatherUpdateTime;
    private const float WEATHER_UPDATE_INTERVAL = 1f;

    // ----------------------------------------------------------
    //  Terrain-Erzeugungsmethoden (manuell)
    // ----------------------------------------------------------

    // Manuelles Generieren (aus dem Inspector-Button / Editor-Script / Play-Mode)
    public void RegenerateTerrain()
    {
        if (!InitializeTerrainReferences())
            return;

        GenerateTerrainCore();
        OnGenerationFinished?.Invoke(terrainData);
    }

    private bool InitializeTerrainReferences()
    {
        if (terrain == null)
            terrain = GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogWarning("Kein Terrain-Component vorhanden!");
            return false;
        }

        if (terrainData == null)
            terrainData = terrain.terrainData;
        if (terrainData == null)
        {
            Debug.LogWarning("Kein TerrainData vorhanden!");
            return false;
        }

        terrainData.heightmapResolution = heightmapResolution;
        terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainLength);

        // Welt-Seed -> Offsets
        rand = new System.Random(worldSeed);
        offsetContinent = rand.Next(int.MinValue, int.MaxValue);
        offsetDetail = rand.Next(int.MinValue, int.MaxValue);
        offsetMountain = rand.Next(int.MinValue, int.MaxValue);
        offsetCanyon = rand.Next(int.MinValue, int.MaxValue);
        offsetBiome = rand.Next(int.MinValue, int.MaxValue);
        offsetHumidity = rand.Next(int.MinValue, int.MaxValue);
        offsetTemperature = rand.Next(int.MinValue, int.MaxValue);

        return true;
    }

    private void GenerateTerrainCore()
    {
        float[,] baseHeights = GenerateBaseHeightmap();
        
        if (useGeologicalSimulation)
        {
            baseHeights = ApplyGeologicalProcesses(baseHeights);
        }

        float[,] erodedHeights = ApplyErosion(baseHeights);
        finalHeights = erodedHeights;

        ApplyHeightmap(finalHeights);
        GenerateTerrainDetails();
    }

    private float[,] GenerateBaseHeightmap()
    {
        if (useGPUGeneration && SystemInfo.supportsComputeShaders)
        {
            return GenerateHeightmapGPU();
        }
        return GenerateHeightmapCPU();
    }

    private float[,] GenerateHeightmapGPU()
    {
        int numThreadGroups = Mathf.CeilToInt(heightmapResolution / 8f);
        terrainComputeShader.Dispatch(0, numThreadGroups, numThreadGroups, 1);
        
        float[,] result = new float[heightmapResolution, heightmapResolution];
        heightmapBuffer.GetData(heightmapNative);
        
        Parallel.For(0, heightmapResolution, y =>
        {
            for (int x = 0; x < heightmapResolution; x++)
            {
                result[x, y] = heightmapNative[y * heightmapResolution + x];
            }
        });
        
        return result;
    }

    private float[,] ApplyGeologicalProcesses(float[,] heights)
    {
        float[,] result = (float[,])heights.Clone();
        
        // Tektonische Simulation
        SimulateTectonicProcesses(ref result);
        
        // Vulkanische Aktivität
        SimulateVolcanicActivity(ref result);
        
        // Sedimentablagerung
        // SimulateSedimentation(ref result); // Anpassung fehlt
        
        return result;
    }

    private void SimulateTectonicProcesses(ref float[,] heights)
    {
        var job = new TectonicSimulationJob
        {
            heights = heights,
            tectonicStrength = this.tectonicStrength,
            faultLines = GenerateFaultLines(),
            resolution = heightmapResolution
        };
        
        job.Schedule().Complete();
    }

    private void SimulateVolcanicActivity(ref float[,] heights)
    {
        // Identifiziere potenzielle Vulkanregionen
        var volcanicHotspots = FindVolcanicHotspots(heights);
        
        foreach (var hotspot in volcanicHotspots)
        {
            ApplyVolcanicFormation(ref heights, hotspot);
        }
    }

    private float[,] ApplyErosion(float[,] heights)
    {
        // Hole aktuelle Wetterbedingungen
        var weather = weatherSystem?.GetCurrentWeatherState() ?? new WeatherState();
        
        // Erstelle Erosionsparameter basierend auf Wetter
        var erosionParams = new ErosionParameters
        {
            rainfall = weather.precipitation * weatherErosionMultiplier,
            temperature = weather.temperature,
            windSpeed = weather.windSpeed,
            windDirection = weatherSystem?.windDirection ?? Vector2.right
        };

        // Führe verschiedene Erosionstypen aus
        return erosionSimulation.SimulateErosion(
            heights, 
            CreateMoistureMap(), 
            terrainData, 
            weatherSystem
        );
    }

    private void GenerateTerrainDetails()
    {
        if (!useVegetationSystem) return;

        // Erstelle Vegetationskarte basierend auf Höhe, Steigung und Biomen
        var vegetationMap = GenerateVegetationMap();
        
        // Platziere Vegetation
        var spawner = GetComponent<EnvironmentSpawner>();
        if (spawner != null)
        {
            spawner.SpawnVegetation(vegetationMap, finalHeights, seaLevelNormalized);
        }
    }

    private float[,] GenerateVegetationMap()
    {
        int width = heightmapResolution;
        int height = heightmapResolution;
        float[,] vegetationMap = new float[width, height];

        Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
            {
                float heightValue = finalHeights[x, y];
                float slope = CalculateSlope(finalHeights, x, y, width, height, terrainWidth, terrainLength, terrainHeight);
                
                // Grundvegetation basierend auf Höhe
                float vegetation = vegetationHeightCurve.Evaluate(heightValue) * vegetationDensity;
                
                // Reduziere Vegetation auf steilen Hängen
                vegetation *= Mathf.Lerp(1f, 0f, slope / 45f);
                
                // Berücksichtige Baumgrenze
                if (heightValue > treeLine)
                {
                    vegetation *= 1f - ((heightValue - treeLine) / (1f - treeLine));
                }
                
                vegetationMap[x, y] = vegetation;
            }
        });

        return vegetationMap;
    }

    private void HandleWeatherChange(WeatherState newState)
    {
        if (Time.time - lastWeatherUpdateTime < WEATHER_UPDATE_INTERVAL) return;
        
        // Aktualisiere Terrain basierend auf Wetterbedingungen
        UpdateTerrainForWeather(newState);
        lastWeatherUpdateTime = Time.time;
    }

    private void UpdateTerrainForWeather(WeatherState weather)
    {
        // Echtzeit-Erosion basierend auf Wetter
        if (weather.precipitation > 0.5f)
        {
            var erosionStrength = weather.precipitation * weatherErosionMultiplier;
            ApplyRealTimeErosion(erosionStrength);
        }
        
        // Schneeakkumulation
        if (weather.temperature < 0 && weather.precipitation > 0)
        {
            AccumulateSnow(weather.precipitation);
        }
    }

    private void OnDestroy()
    {
        if (heightmapBuffer != null) heightmapBuffer.Release();
        if (heightmapNative.IsCreated) heightmapNative.Dispose();
        
        if (weatherSystem != null && useRealTimeWeatherErosion)
        {
            // weatherSystem.OnWeatherChanged -= HandleWeatherChange; // Event existiert nicht
        }
    }

    private void GenerateTerrainGPU()
    {
        InitializeComputeBuffers();
        
        terrainComputeShader.SetBuffer(0, "_HeightmapBuffer", heightmapBuffer);
        terrainComputeShader.SetBuffer(0, "_BiomeBuffer", biomeBuffer);
        
        // Dispatch compute shader in tiles for better performance
        int tileSize = 8;
        int numTilesX = Mathf.CeilToInt(heightmapResolution / (float)tileSize);
        int numTilesY = Mathf.CeilToInt(heightmapResolution / (float)tileSize);
        
        terrainComputeShader.Dispatch(0, numTilesX, numTilesY, 1);
        
        // Read back results
        heightmapBuffer.GetData(heightmapNative);
        ApplyHeightmapFromNative(heightmapNative);
    }

    private void ApplyGeologicalFeatures()
    {
        ApplyTectonicFeatures();
        ApplyVolcanicFeatures();
        ApplyGeologicalErosion();
    }

    private void ApplyTectonicFeatures()
    {
        // Implementiere tektonische Platten und Verwerfungen
        var faultLines = GenerateFaultLines();
        foreach (var fault in faultLines)
        {
            ApplyFaultDisplacement(fault);
        }
    }

    private void GenerateAdvancedRiverSystem()
    {
        var riverNetwork = new RiverNetwork(heightmapResolution, heightmapResolution);
        
        // Finde geeignete Quellen für Flüsse
        var sources = FindRiverSources();
        
        foreach (var source in sources)
        {
            var river = new River(source);
            SimulateRiverFlow(river);
            riverNetwork.AddRiver(river);
        }
        
        // Erosion entlang der Flüsse
        ApplyRiverErosion(riverNetwork);
        
        // Deltagebiete an Flussmündungen
        GenerateRiverDeltas(riverNetwork);
    }

    private Vector3[] FindRiverSources()
    {
        List<Vector3> sources = new List<Vector3>();
        
        // Verbesserte Quellenfindung basierend auf:
        // - Höhe
        // - Niederschlag
        // - Geologie
        // - Existierende Wasserquellen
        
        for (int i = 0; i < riverSourceCount; i++)
        {
            var source = CalculateOptimalRiverSource(sources);
            if (source != Vector3.zero)
            {
                sources.Add(source);
            }
        }
        
        return sources.ToArray();
    }

    private void SimulateRiverFlow(River river)
    {
        float flow = river.initialFlow;
        Vector3 pos = river.source;
        
        while (flow > minRiverFlow)
        {
            // Verbesserte Flusssimulation mit:
            // - Realistischer Wasserphysik
            // - Sedimenttransport
            // - Mäanderbildung
            // - Zusammenflüsse
            
            var nextPos = CalculateRiverFlowDirection(pos, flow);
            river.AddPoint(nextPos);
            
            flow = UpdateRiverFlow(flow, pos, nextPos);
            pos = nextPos;
        }
    }

    private float[,] GenerateMultiLayerHeights()
    {
        float[,] heights = new float[heightmapResolution, heightmapResolution];
        
        for (int x = 0; x < heightmapResolution; x++)
        {
            for (int y = 0; y < heightmapResolution; y++)
            {
                float nx = (float)x / (heightmapResolution - 1);
                float ny = (float)y / (heightmapResolution - 1);
                float worldX = nx * terrainWidth;
                float worldY = ny * terrainLength;

                // Kontinente (große Formen)
                float continents = MultiOctave(worldX, worldY, 3, 0.5f, 2.0f, 1200f, offsetContinent);
                
                // Gebirgsketten
                float mountains = RidgedNoise(worldX, worldY, 6, 0.6f, 2.2f, 400f, offsetMountain, 2.5f);
                
                // Hügel mittlerer Größe
                float hills = MultiOctave(worldX, worldY, 4, 0.5f, 2.0f, 200f, offsetDetail);
                
                // Kleine Details
                float details = MultiOctave(worldX, worldY, 3, 0.5f, 2.0f, 50f, offsetDetail);

                // Kombiniere die Schichten mit unterschiedlichen Gewichtungen
                float baseHeight = continents * 0.6f + mountains * 0.25f + hills * 0.1f + details * 0.05f;
                
                // Zusätzliche Variation durch Temperatur und Feuchtigkeit
                float climate = MultiOctave(worldX, worldY, 2, 0.5f, 2.0f, 800f, offsetTemperature);
                float humidity = MultiOctave(worldX, worldY, 2, 0.5f, 2.0f, 600f, offsetHumidity);
                
                // Modifiziere Höhe basierend auf Klima
                baseHeight *= 1f + (climate - 0.5f) * 0.2f;
                baseHeight *= 1f + (humidity - 0.5f) * 0.1f;

                // Zusätzliche geologische Faktoren
                float tectonicInfluence = CalculateTectonicInfluence(worldX, worldY);
                float geologicalAge = CalculateGeologicalAge(worldX, worldY);
                
                // Modifiziere Höhe basierend auf geologischen Faktoren
                baseHeight *= 1f + tectonicInfluence * tectonicActivityScale;
                baseHeight *= GetGeologicalAgeModifier(geologicalAge);
                
                if (generateMicroDetail)
                {
                    float microDetail = GenerateMicroDetail(worldX, worldY);
                    baseHeight += microDetail * microDetailScale;
                }

                heights[x, y] = Mathf.Clamp01(baseHeight);
            }
        }
        
        return heights;
    }

    private float GenerateMicroDetail(float x, float y)
    {
        float detail = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        
        for (int i = 0; i < microDetailOctaves; i++)
        {
            detail += NoiseUtils.RidgedNoise(
                x * frequency,
                y * frequency,
                2,
                0.5f,
                2.0f,
                50f,
                worldSeed + i,
                2f
            ) * amplitude;
            
            amplitude *= 0.5f;
            frequency *= 2f;
        }
        
        return detail;
    }

    // ----------------------------------------------------------
    //  Kern-Noise für die Heightmap
    // ----------------------------------------------------------
    private float[,] GenerateRawHeights()
    {
        float[,] heights = new float[heightmapResolution, heightmapResolution];

        for (int x = 0; x < heightmapResolution; x++)
        {
            for (int y = 0; y < heightmapResolution; y++)
            {
                float nx = (float)x / (heightmapResolution - 1);
                float ny = (float)y / (heightmapResolution - 1);

                float worldX = nx * terrainWidth;
                float worldY = ny * terrainLength;

                float contVal = MultiOctave(worldX, worldY, 2, 0.5f, 2.0f, 1200f, offsetContinent);
                contVal = Mathf.Pow(contVal, 1f - continentSeparation);

                float detailVal = MultiOctave(worldX, worldY, 3, 0.5f, 2.0f, 250f, offsetDetail) * hillHeight;
                float baseVal = contVal + detailVal;

                float mountVal = RidgedNoise(worldX, worldY, 5, 0.5f, 2.2f, 150f, offsetMountain, mountainRuggedness + 1.5f) * (mountainHeight + 0.3f);
                float mountainMask = Mathf.Clamp01(Mathf.InverseLerp(0.5f, 1f, contVal));
                baseVal += mountVal * mountainMask;

                float canyonVal = MultiOctave(worldX, worldY, 2, 0.5f, 2.0f, 200f, offsetCanyon);
                float canyonMask = Mathf.Clamp01(Mathf.InverseLerp(0f, 0.5f, baseVal));
                baseVal -= canyonVal * (canyonDepth + 0.1f) * canyonMask;

                float caveNoise = RidgedNoise(worldX, worldY, 2, 0.6f, 2.0f, 100f, offsetDetail, 2f);
                if (caveNoise > 0.8f)
                    baseVal -= 0.15f;

                baseVal = Mathf.Clamp01(baseVal);
                heights[x, y] = baseVal;
            }
        }

        return heights;
    }

    private float ComputeSeaLevelForCoverage(float[,] map, float coverage)
    {
        int w = map.GetLength(0);
        int h = map.GetLength(1);
        int total = w * h;

        float[] allHeights = new float[total];
        int idx = 0;
        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                allHeights[idx++] = map[i, j];
            }
        }

        System.Array.Sort(allHeights);
        int targetIndex = Mathf.Clamp(
            Mathf.RoundToInt(coverage * (total - 1)),
            0, total - 1
        );

        return allHeights[targetIndex];
    }

    // ----------------------------------------------------------
    //  Erosion
    // ----------------------------------------------------------
    private float[,] SimulateErosion(float[,] map, int iterationCount, float strength, float sedimentFactor)
    {
        int w = map.GetLength(0);
        int h = map.GetLength(1);
        float[,] newMap = (float[,])map.Clone();

        Vector2Int[] neighbors = {
            new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
            new Vector2Int(-1,  0),                     new Vector2Int(1,  0),
            new Vector2Int(-1,  1), new Vector2Int(0,  1), new Vector2Int(1,  1)
        };

        for (int iter = 0; iter < iterationCount; iter++)
        {
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    float currentH = newMap[x, y];
                    float sumDelta = 0f;
                    float mat = strength;

                    foreach (var n in neighbors)
                    {
                        int nx = x + n.x;
                        int ny = y + n.y;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;

                        float neighH = newMap[nx, ny];
                        float delta = currentH - neighH;
                        if (delta > 0f) sumDelta += delta;
                    }

                    if (sumDelta <= 0f) continue;

                    foreach (var n in neighbors)
                    {
                        int nx = x + n.x;
                        int ny = y + n.y;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;

                        float neighH = newMap[nx, ny];
                        float delta = currentH - neighH;
                        if (delta > 0f)
                        {
                            float proportion = delta / sumDelta;
                            float sediment = mat * proportion * sedimentFactor;
                            newMap[x, y] -= sediment;
                            newMap[nx, ny] += sediment;
                        }
                    }
                }
            }
        }
        return newMap;
    }

    // ----------------------------------------------------------
    //  Optional: Objekte, Höhlen etc.
    // ----------------------------------------------------------
    private void PlaceWorldObjects()
    {
        // Hier nur Aufruf zur neuen Logik
        var spawner = GetComponent<EnvironmentSpawner>();
        if (spawner != null)
        {
            spawner.SpawnEnvironment(finalHeights, seaLevelNormalized);
        }
    }

    private void GenerateRivers(float[,] heights)
    {
        // Implementiere die Logik zur Erzeugung von Flüssen
    }

    public float[,] GetTerrainHeights2D()
    {
        return finalHeights;
    }

    void Update()
    {
        if (regenerateTerrain)
        {
            regenerateTerrain = false;
            RegenerateTerrain();
        }
    }

    private void Start()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        if (!InitializeTerrainReferences()) return;
        
        erosionSimulation = new AdvancedErosionSimulation();
        
        if (useGPUGeneration)
        {
            InitializeComputeResources();
        }

        // Verbinde mit Weather System für Echtzeit-Erosion
        if (weatherSystem != null && useRealTimeWeatherErosion)
        {
            weatherSystem.OnWeatherChanged += HandleWeatherChange;
        }
    }

    private void InitializeComputeResources()
    {
        heightmapBuffer = new ComputeBuffer(heightmapResolution * heightmapResolution, sizeof(float));
        heightmapNative = new NativeArray<float>(heightmapResolution * heightmapResolution, Allocator.Persistent);
        
        terrainComputeShader.SetBuffer(0, "_HeightmapBuffer", heightmapBuffer);
        SetupComputeShaderParameters();
    }

    private void SetupComputeShaderParameters()
    {
        terrainComputeShader.SetInt("_Resolution", heightmapResolution);
        terrainComputeShader.SetFloat("_TerrainHeight", terrainHeight);
        terrainComputeShader.SetFloat("_MountainScale", mountainHeight);
        terrainComputeShader.SetFloat("_HillScale", hillHeight);
        // ... weitere Parameter ...
    }

    private void ApplyHeightmapFromNative(NativeArray<float> heightArray)
    {
        float[,] heightmap = new float[heightmapResolution, heightmapResolution];
        for (int y = 0; y < heightmapResolution; y++)
        {
            for (int x = 0; x < heightmapResolution; x++)
            {
                heightmap[x, y] = heightArray[y * heightmapResolution + x];
            }
        }
        
        terrainData.SetHeights(0, 0, heightmap);
    }

    // Events für WeatherSystem
    public delegate void WeatherChangedHandler(WeatherState newState);
    public event WeatherChangedHandler OnWeatherChanged;
}

#if UNITY_EDITOR
[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TerrainGenerator script = (TerrainGenerator)target;
        if (GUILayout.Button("Regenerate Terrain"))
        {
            script.RegenerateTerrain();
        }
    }
}
#endif