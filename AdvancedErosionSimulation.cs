using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using System.Threading.Tasks;
using static MergedUtils;

public class River
{
    public Vector3 source { get; private set; }
    public float initialFlow { get; private set; }

    public River(Vector3 sourcePosition)
    {
        source = sourcePosition;
        initialFlow = 1f;
    }

    public void AddPoint(Vector3 point)
    {
        // Implementierung für Flusspunkte
    }
}

public class RiverNetwork
{
    private int width;
    private int height;

    public RiverNetwork(int width, int height)
    {
        this.width = width;
        this.height = height;
    }

    public void AddRiver(River river)
    {
        // Implementierung für Flussnetzwerk
    }
}

public struct PhysicsParams
{
    public float3 position;
    public float3 velocity;
    public float3 acceleration;
}

public class AdvancedErosionSimulation
{
    [System.Serializable]
    private struct ErosionSettings
    {
        public float inertia;             // 0.1f - 0.9f
        public float sedimentCapacity;     // 0.1f - 0.5f
        public float erosionRate;          // 0.1f - 0.5f
        public float depositionRate;       // 0.1f - 0.5f
        public float evaporationRate;      // 0.01f - 0.1f
        public float gravity;              // 9.81f
        public int maxSteps;              // 50-200
        public float waterCapacity;      // Wasserspeicherkapazität des Bodens
        public float soilSoftness;       // Wie leicht der Boden erodiert wird
        public float thermalErosion;     // Stärke der thermischen Erosion
        public float rockHardness;       // Widerstandsfähigkeit von Gestein
        public float vegetationProtection; // Schutz durch Vegetation
    }

    [System.Serializable]
    private struct ParticleSettings
    {
        public int particleCount;        // Anzahl der Erosionspartikel
        public float lifetime;           // Lebensdauer eines Partikels
        public float startSpeed;         // Anfangsgeschwindigkeit
        public float friction;           // Reibung mit dem Untergrund
        public float depositionRate;     // Rate der Sedimentablagerung
        public float evaporationRate;    // Verdunstungsrate
    }

    public struct ErosionParticle 
    {
        public Vector2 position;
        public Vector2 velocity;
        public float water;
        public float sediment;
        public float erosionRadius;
        public float erosionStrength;
        public float3 momentum;           // Verbesserte Physik
        public float temperature;         // Für Frost-Verwitterung
        public float mineralContent;      // Verschiedene Sedimenttypen
    }

    private ComputeShader erosionCompute;
    private readonly ErosionSettings settings = new ErosionSettings
    {
        inertia = 0.7f,
        sedimentCapacity = 0.3f,
        erosionRate = 0.2f,
        depositionRate = 0.2f,
        evaporationRate = 0.02f,
        gravity = 9.81f,
        maxSteps = 150
    };

    private struct ErosionJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> heightMap;
        [ReadOnly] public NativeArray<float> soilHardness;
        [ReadOnly] public NativeArray<float> moistureMap;
        public NativeArray<float> erosionMap;
        public NativeArray<float> sedimentMap;
        public float deltaTime;
        public float erosionStrength;
        public float depositionRate;
        public float evaporationRate;

        public void Execute(int index)
        {
            int width = (int)math.sqrt(heightMap.Length);
            int x = index % width;
            int y = index / width;

            // Berechne Erosionsfaktoren
            float slope = CalculateSlope(x, y, width);
            float hardness = soilHardness[index];
            float moisture = moistureMap[index];

            // Berechne Erosion basierend auf verschiedenen Faktoren
            float erosion = CalculateErosion(slope, hardness, moisture);
            
            // Aktualisiere Height- und Sediment-Maps
            UpdateMaps(index, erosion);
        }

        private float CalculateSlope(int x, int y, int width)
        {
            // Verbesserte Steigungsberechnung mit diagonalen Nachbarn
            float[] neighbors = MergedUtils.GetNeighborHeights(x, y, width);
            return MergedUtils.CalculateSlopeFromNeighbors(neighbors);
        }

        private float CalculateErosion(float slope, float hardness, float moisture)
        {
            // Komplexere Erosionsberechnung
            float baseErosion = slope * erosionStrength;
            float moistureEffect = 1f + moisture * 0.5f;
            float hardnessResistance = 1f - hardness * 0.8f;
            
            return baseErosion * moistureEffect * hardnessResistance;
        }

        private void UpdateMaps(int index, float erosion)
        {
            // Aktualisiere Höhenkarte und Sedimentkarte
            float currentHeight = heightMap[index];
            float sediment = erosion * depositionRate;
            
            erosionMap[index] = erosion;
            sedimentMap[index] = sediment;
        }
    }

    public float[,] SimulateErosion(float[,] heightMap, float[,] moisture, TerrainData terrainData, WeatherSystem weather)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        // Initialisiere Erosions-Daten
        var erosionData = InitializeErosionData(width, height);
        
        // Führe verschiedene Erosionstypen aus
        SimulateHydraulicErosion(erosionData, moisture, weather);
        SimulateThermalErosion(erosionData);
        SimulateWindErosion(erosionData, weather);
        SimulateChemicalErosion(erosionData, moisture);
        
        // Wende Erosion auf Heightmap an
        return ApplyErosionToHeightmap(erosionData, heightMap);
    }

    public struct ErosionData
    {
        public NativeArray<float> heights;
        public NativeArray<float> erosion;
        public NativeArray<float> sediment;
        public NativeArray<float> hardness;
        public NativeArray<float> moisture;
    }

    private ErosionData InitializeErosionData(int width, int height)
    {
        return new ErosionData
        {
            heights = new NativeArray<float>(width * height, Allocator.TempJob),
            erosion = new NativeArray<float>(width * height, Allocator.TempJob),
            sediment = new NativeArray<float>(width * height, Allocator.TempJob),
            hardness = new NativeArray<float>(width * height, Allocator.TempJob),
            moisture = new NativeArray<float>(width * height, Allocator.TempJob)
        };
    }

    private void SimulateHydraulicErosion(ErosionData data, float[,] moisture, WeatherSystem weather)
    {
        var job = new HydraulicErosionJob
        {
            heightMap = data.heights,
            moistureMap = data.moisture,
            erosionMap = data.erosion,
            sedimentMap = data.sediment,
            rainfall = weather.GetCurrentWeatherState().precipitation,
            deltaTime = Time.deltaTime
        };

        job.Schedule(data.heights.Length, 64).Complete();
    }

    private void SimulateThermalErosion(ErosionData data)
    {
        var job = new ThermalErosionJob
        {
            heightMap = data.heights,
            hardnessMap = data.hardness,
            erosionMap = data.erosion,
            temperature = GetAverageTemperature(),
            deltaTime = Time.deltaTime
        };

        job.Schedule(data.heights.Length, 64).Complete();
    }

    private void SimulateWindErosion(ErosionData data, WeatherSystem weather)
    {
        var weatherState = weather.GetCurrentWeatherState();
        var job = new WindErosionJob
        {
            heightMap = data.heights,
            erosionMap = data.erosion,
            windSpeed = weatherState.windSpeed,
            windDirection = 0f, // Standardwert statt windDirection
            deltaTime = Time.deltaTime
        };

        job.Schedule(data.heights.Length, 64).Complete();
    }

    private void SimulateChemicalErosion(ErosionData data, float[,] moisture)
    {
        var job = new ChemicalErosionJob
        {
            heightMap = data.heights,
            hardnessMap = data.hardness,
            moistureMap = data.moisture,
            erosionMap = data.erosion,
            deltaTime = Time.deltaTime
        };

        job.Schedule(data.heights.Length, 64).Complete();
    }

    private float[,] ApplyErosionToHeightmap(ErosionData data, float[,] originalHeightmap)
    {
        int width = originalHeightmap.GetLength(0);
        int height = originalHeightmap.GetLength(1);
        float[,] result = new float[width, height];

        // Kombiniere alle Erosionseffekte
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                float erosion = data.erosion[index];
                float sediment = data.sediment[index];
                
                result[x, y] = originalHeightmap[x, y] - erosion + sediment;
            }
        }

        // Aufräumen
        data.heights.Dispose();
        data.erosion.Dispose();
        data.sediment.Dispose();
        data.hardness.Dispose();
        data.moisture.Dispose();

        return result;
    }

    public float[,] RunAdvancedErosion(float[,] map, int iterationCount)
    {
        if (SystemInfo.supportsComputeShaders && erosionCompute != null)
        {
            return RunGPUErosion(map, iterationCount);
        }
        return RunCPUErosion(map, iterationCount);
    }

    private float[,] RunGPUErosion(float[,] map, int iterationCount)
    {
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        ComputeBuffer heightBuffer = new ComputeBuffer(width * height, sizeof(float));
        ComputeBuffer particleBuffer = new ComputeBuffer(iterationCount, sizeof(float) * 8);

        try
        {
            // GPU Erosionsberechnung
            erosionCompute.SetBuffer(0, "_HeightMap", heightBuffer);
            erosionCompute.SetBuffer(0, "_Particles", particleBuffer);
            erosionCompute.Dispatch(0, iterationCount / 64, 1, 1);
            
            float[,] result = new float[width, height];
            heightBuffer.GetData(result);
            return result;
        }
        finally
        {
            heightBuffer.Release();
            particleBuffer.Release();
        }
    }

    private float[,] RunCPUErosion(float[,] map, int iterationCount)
    {
        int width = map.GetLength(0);
        int height = map.GetLength(1);
        float[,] result = (float[,])map.Clone();

        // Parallel processing für bessere Performance
        Parallel.For(0, iterationCount, i =>
        {
            ErosionParticle particle = InitializeParticle(width, height);
            SimulateParticleWithRadius(ref particle, result, 100);
        });

        return result;
    }

    private ErosionParticle InitializeParticle(int width, int height)
    {
        return new ErosionParticle
        {
            position = new Vector2(Random.Range(0, width), Random.Range(0, height)),
            velocity = Vector2.zero,
            water = 1f,
            sediment = 0f,
            erosionRadius = Random.Range(2f, 4f),
            erosionStrength = Random.Range(0.1f, 0.3f),
            momentum = float3.zero,
            temperature = Random.Range(-10f, 30f),
            mineralContent = Random.Range(0.1f, 1f)
        };
    }

    private void SimulateParticleWithRadius(ref ErosionParticle particle, float[,] heightmap, int steps)
    {
        Vector2Int pos = Vector2Int.FloorToInt(particle.position);
        if (!IsInBounds(pos, heightmap.GetLength(0), heightmap.GetLength(1))) return;

        for (int step = 0; step < steps && particle.water > 0.01f; step++)
        {
            // Verbesserte Physik-Simulation
            UpdateParticlePhysics(ref particle, heightmap);
            
            // Thermische Erosion
            SimulateTemperatureEffects(ref particle, heightmap);
            
            // Chemische Verwitterung
            SimulateChemicalErosion(ref particle, heightmap);
            
            // Sediment-Transport
            UpdateSedimentTransport(ref particle, heightmap);
        }
    }

    private Vector2 CalculateGradient(Vector2Int pos, float[,] heightmap, float radius)
    {
        Vector2 gradient = Vector2.zero;
        int r = Mathf.CeilToInt(radius);

        for (int y = -r; y <= r; y++)
        {
            for (int x = -r; x <= r; x++)
            {
                Vector2Int offset = new Vector2Int(x, y);
                Vector2Int samplePos = pos + offset;
                
                if (!IsInBounds(samplePos, heightmap.GetLength(0), heightmap.GetLength(1)))
                    continue;

                float weight = Mathf.Exp(-(x * x + y * y) / (2f * radius * radius));
                float height = heightmap[samplePos.x, samplePos.y];
                
                gradient += new Vector2(x, y) * height * weight;
            }
        }

        return gradient.normalized;
    }

    private void UpdateParticlePhysics(ref ErosionParticle particle, float[,] heightmap)
    {
        // Verbesserte Physik basierend auf Navier-Stokes
        float3 acceleration = CalculateAcceleration(particle, heightmap);
        particle.momentum += acceleration * Time.deltaTime;
        particle.velocity = new Vector2(particle.momentum.x, particle.momentum.z);
        
        // Reibung und Viskosität
        ApplyViscosity(ref particle);
        
        // Kollisionsbehandlung
        HandleCollisions(ref particle, heightmap);
    }

    private float3 CalculateAcceleration(ErosionParticle particle, float[,] heightmap)
    {
        // Berechne die Beschleunigung basierend auf der Höhenkarte
        Vector2 gradient = CalculateGradient(
            Vector2Int.FloorToInt(particle.position), 
            heightmap, 
            particle.erosionRadius
        );

        return new float3(
            gradient.x * settings.gravity,
            -settings.gravity,
            gradient.y * settings.gravity
        );
    }

    private void ApplyViscosity(ref ErosionParticle particle)
    {
        // Implementiere Viskositätseffekte
        particle.velocity *= (1f - settings.inertia * Time.deltaTime);
    }

    private void HandleCollisions(ref ErosionParticle particle, float[,] heightmap)
    {
        // Implementiere Kollisionsbehandlung
        Vector2Int pos = Vector2Int.FloorToInt(particle.position);
        if (!IsInBounds(pos, heightmap.GetLength(0), heightmap.GetLength(1)))
        {
            particle.velocity = Vector2.zero;
            return;
        }

        float terrainHeight = heightmap[pos.x, pos.y];
        if (particle.position.y < terrainHeight)
        {
            particle.position.y = terrainHeight;
            particle.velocity = Vector2.Reflect(particle.velocity, Vector2.up);
            particle.velocity *= 0.5f; // Energieverlust bei Kollision
        }
    }

    private void SimulateTemperatureEffects(ref ErosionParticle particle, float[,] heightmap)
    {
        // Frost-Verwitterung
        if (particle.temperature < 0 && particle.water > 0.1f)
        {
            Vector2Int pos = Vector2Int.FloorToInt(particle.position);
            float frostDamage = CalculateFrostDamage(particle.temperature, particle.water);
            ApplyFrostErosion(pos, frostDamage, heightmap);
        }
    }

    private void SimulateChemicalErosion(ref ErosionParticle particle, float[,] heightmap)
    {
        // pH-Wert basierte Erosion
        float acidityFactor = CalculateAcidity(particle);
        float dissolvedMinerals = CalculateDissolvedMinerals(acidityFactor, particle.mineralContent);
        
        Vector2Int pos = Vector2Int.FloorToInt(particle.position);
        ApplyChemicalErosion(pos, dissolvedMinerals, heightmap);
    }

    private void UpdateSedimentTransport(ref ErosionParticle particle, float[,] heightmap)
    {
        // Verbesserte Sediment-Kapazität basierend auf Geschwindigkeit
        float capacity = CalculateSedimentCapacity(particle.velocity.magnitude, particle.water);
        
        if (particle.sediment > capacity)
        {
            // Sediment-Ablagerung
            DepositSediment(ref particle, capacity, heightmap);
        }
        else
        {
            // Erosion
            ErodeTerrainAdvanced(ref particle, capacity, heightmap);
        }
    }

    private float CalculateSedimentCapacity(float speed, float waterVolume)
    {
        // Verbesserte Hjulström-Kurve Implementation
        float criticalVelocity = 0.25f; // Minimum velocity for erosion
        if (speed < criticalVelocity) return 0;
        
        return settings.sedimentCapacity * 
               Mathf.Pow(speed, 2) * 
               waterVolume * 
               (1f - Mathf.Exp(-speed / criticalVelocity));
    }

    private void ApplyErosionAndDeposition(ref ErosionParticle particle, float[,] heightmap, Vector2Int pos)
    {
        int r = Mathf.CeilToInt(particle.erosionRadius);
        float totalWeight = 0f;
        
        // Zwei-Pass-System für stabilere Erosion
        for (int y = -r; y <= r; y++)
        {
            for (int x = -r; x <= r; x++)
            {
                Vector2Int offset = new Vector2Int(x, y);
                Vector2Int targetPos = pos + offset;
                
                if (!IsInBounds(targetPos, heightmap.GetLength(0), heightmap.GetLength(1)))
                    continue;

                float weight = Mathf.Exp(-(x * x + y * y) / (2f * particle.erosionRadius * particle.erosionRadius));
                totalWeight += weight;
                
                float erosionAmount = particle.erosionStrength * weight * particle.water;
                float currentHeight = heightmap[targetPos.x, targetPos.y];
                
                // Erosion
                if (particle.sediment < particle.water * 0.1f)
                {
                    float eroded = Mathf.Min(erosionAmount, currentHeight);
                    heightmap[targetPos.x, targetPos.y] -= eroded;
                    particle.sediment += eroded;
                }
                // Ablagerung
                else
                {
                    float deposited = particle.sediment * weight / totalWeight;
                    heightmap[targetPos.x, targetPos.y] += deposited;
                    particle.sediment -= deposited;
                }
            }
        }

        particle.water *= 0.98f; // Verdunstung
    }

    private bool IsInBounds(Vector2Int pos, int width, int height)
    {
        return pos.x >= 0 && pos.x < width - 1 && pos.y >= 0 && pos.y < height - 1;
    }
}