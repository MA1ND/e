using UnityEngine;
using UnityEngine.Rendering;
using static ReflectionUtils;

[ExecuteInEditMode]
public class AdvancedLightingSystem : MonoBehaviour
{
    [Header("HDR Lighting")]
    public bool useHDR = true;
    [Range(0f, 10f)] public float hdrIntensity = 1.5f;
    public AnimationCurve hdrAdaptationCurve;

    [Header("Time Settings")]
    public bool useRealTime = true;
    public float dayNightCycleDuration = 24f;
    [Range(-90f, 90f)] public float latitude = 50f;
    [Range(0f, 24f)] public float currentTime = 12f;

    [Header("Advanced Lighting")]
    public Light mainLight;
    public Light moonLight;
    [ColorUsage(true, true)] public Color morningColor = new Color(1f, 0.8f, 0.6f, 1f);
    [ColorUsage(true, true)] public Color dayColor = Color.white;
    [ColorUsage(true, true)] public Color eveningColor = new Color(1f, 0.6f, 0.3f, 1f);
    [ColorUsage(true, true)] public Color nightColor = new Color(0.2f, 0.2f, 0.3f, 1f);

    [Header("Volumetric Lighting")]
    public bool useVolumetricLighting = true;
    [Range(0f, 1f)] public float volumetricIntensity = 0.5f;
    public Material volumetricMaterial;

    [Header("Weather Influence")]
    public Wetter wetterSystem;
    [Range(0f, 1f)] public float weatherInfluence = 0.5f;

    [Header("Advanced Sky Settings")]
    public Material skyboxMaterial;
    [ColorUsage(true, true)] public Color zenithColor = new Color(0.3f, 0.5f, 1f, 1f);
    [ColorUsage(true, true)] public Color horizonColor = new Color(0.7f, 0.7f, 1f, 1f);
    public float rayleighScattering = 1.5f;
    public float mieScattering = 0.5f;

    [Header("Cloud System")]
    public Material volumetricCloudMaterial;
    public float cloudHeight = 1000f;
    public float cloudThickness = 500f;
    public float cloudCoverage = 0.5f;
    public float cloudSpeed = 10f;
    public Vector2 cloudDirection = Vector2.one;

    [Header("Advanced Light Bouncing")]
    [Range(0f, 1f)] public float indirectLightIntensity = 0.3f;
    public bool useScreenSpaceReflections = true;
    public LayerMask reflectionLayers;
    public int reflectionBounces = 2;

    private LightingCache lightingCache;
    private float lastUpdateTime;
    private const float UPDATE_INTERVAL = 0.1f;
    private LightProbeGroup lightProbeGroup;
    private ReflectionProbe[] reflectionProbes;
    private Material atmosphereMaterial;
    private WetterZustand aktuellerWetterZustand;
    private float lastWeatherUpdateTime;
    private const float WEATHER_UPDATE_INTERVAL = 0.5f;

    private struct LightingCache
    {
        public Color skyColor;
        public Color equatorColor;
        public Color groundColor;
        public float intensity;
        public Quaternion sunRotation;
    }

    private void Start()
    {
        InitializeSystem();
    }

    private void InitializeSystem()
    {
        if (mainLight == null) mainLight = GetComponent<Light>();
        
        if (useHDR)
        {
            ConfigureHDRSettings();
        }

        if (useVolumetricLighting)
        {
            SetupVolumetricLighting();
        }

        InitializeLightingCache();
        SetupAtmosphere();
        SetupLightProbes();
        SetupReflectionProbes();
    }

    private void ConfigureHDRSettings()
    {
        var camera = Camera.main;
        if (camera != null)
        {
            camera.allowHDR = true;
            var probe = gameObject.AddComponent<ReflectionProbe>();
            var hdProbe = new HDReflectionProbeWrapper(probe);
            hdProbe.ConfigureHDSettings(hdrIntensity);
        }
    }

    private void SetupAtmosphere()
    {
        if (skyboxMaterial != null)
        {
            atmosphereMaterial = new Material(skyboxMaterial);
            atmosphereMaterial.EnableKeyword("_ATMOSPHERE");
            RenderSettings.skybox = atmosphereMaterial;
        }
    }

    private void SetupLightProbes()
    {
        lightProbeGroup = GetComponent<LightProbeGroup>();
        if (lightProbeGroup == null)
        {
            lightProbeGroup = gameObject.AddComponent<LightProbeGroup>();
            GenerateLightProbePositions();
        }
    }

    private void GenerateLightProbePositions()
    {
        // Intelligente Platzierung von Light Probes basierend auf der Szenengeometrie
        Vector3[] probePositions = CalculateOptimalProbePositions();
        lightProbeGroup.probePositions = probePositions;
    }

    private void SetupReflectionProbes()
    {
        reflectionProbes = GetComponentsInChildren<ReflectionProbe>();
        foreach (var probe in reflectionProbes)
        {
            ConfigureReflectionProbe(probe);
        }
    }

    private void SetupVolumetricLighting()
    {
        if (volumetricMaterial != null)
        {
            volumetricMaterial.EnableKeyword("_VOLUMETRIC_LIGHTING");
            volumetricMaterial.SetFloat("_VolumetricIntensity", volumetricIntensity);
        }
    }

    private void InitializeLightingCache()
    {
        lightingCache = new LightingCache
        {
            skyColor = RenderSettings.ambientSkyColor,
            equatorColor = RenderSettings.ambientEquatorColor,
            groundColor = RenderSettings.ambientGroundColor,
            intensity = mainLight != null ? mainLight.intensity : 1f,
            sunRotation = mainLight != null ? mainLight.transform.rotation : Quaternion.identity
        };
    }

    private void Update()
    {
        if (Time.time - lastUpdateTime < UPDATE_INTERVAL) return;

        UpdateLightingSystem();
        lastUpdateTime = Time.time;
    }

    private void UpdateLightingSystem()
    {
        float dayProgress = CalculateDayProgress();
        UpdateSunPosition(dayProgress);
        UpdateLightColors(dayProgress);
        
        if (useVolumetricLighting)
        {
            UpdateVolumetricEffects(dayProgress);
        }

        if (wetterSystem != null && Time.time - lastWeatherUpdateTime > WEATHER_UPDATE_INTERVAL)
        {
            aktuellerWetterZustand = wetterSystem.GetCurrentWeatherState();
            ApplyWeatherEffects();
            lastWeatherUpdateTime = Time.time;
        }

        UpdateAtmosphericScattering(dayProgress);
        UpdateCloudSystem();
        UpdateIndirectLighting();
    }

    private float CalculateDayProgress()
    {
        if (useRealTime)
        {
            System.DateTime now = System.DateTime.Now;
            return (float)(now.Hour * 3600 + now.Minute * 60 + now.Second) / 86400f;
        }
        
        currentTime = (currentTime + Time.deltaTime / dayNightCycleDuration) % 24f;
        return currentTime / 24f;
    }

    private void UpdateSunPosition(float dayProgress)
    {
        float sunAngle = CalculateSunAngle(dayProgress, latitude);
        float sunAzimuth = CalculateSunAzimuth(dayProgress, latitude);
        
        Quaternion targetRotation = Quaternion.Euler(sunAngle, sunAzimuth, 0f);
        mainLight.transform.rotation = targetRotation;

        if (moonLight != null)
        {
            moonLight.transform.rotation = Quaternion.Euler(sunAngle + 180f, sunAzimuth, 0f);
        }
    }

    private float CalculateSunAngle(float dayProgress, float lat)
    {
        float declination = -23.45f * Mathf.Cos((dayProgress + 10f) / 365f * 2f * Mathf.PI);
        float latRad = lat * Mathf.Deg2Rad;
        float decRad = declination * Mathf.Deg2Rad;
        float timeAngle = (dayProgress * 360f - 180f) * Mathf.Deg2Rad;
        
        float elevation = Mathf.Asin(
            Mathf.Sin(latRad) * Mathf.Sin(decRad) +
            Mathf.Cos(latRad) * Mathf.Cos(decRad) * Mathf.Cos(timeAngle)
        );
        
        return 90f - elevation * Mathf.Rad2Deg;
    }

    private void UpdateLightColors(float dayProgress)
    {
        Color targetColor;
        float intensity;

        if (dayProgress < 0.25f) // Sunrise
        {
            float t = dayProgress * 4f;
            targetColor = Color.Lerp(nightColor, morningColor, t);
            intensity = Mathf.Lerp(0.1f, 1f, t);
        }
        else if (dayProgress < 0.75f) // Day
        {
            float noonProgress = 1f - Mathf.Abs(dayProgress - 0.5f) * 4f;
            targetColor = Color.Lerp(morningColor, dayColor, noonProgress);
            intensity = 1f;
        }
        else if (dayProgress < 0.85f) // Sunset
        {
            float t = (dayProgress - 0.75f) * 10f;
            targetColor = Color.Lerp(dayColor, eveningColor, t);
            intensity = Mathf.Lerp(1f, 0.5f, t);
        }
        else // Night
        {
            float t = (dayProgress - 0.85f) * 6.67f;
            targetColor = Color.Lerp(eveningColor, nightColor, t);
            intensity = Mathf.Lerp(0.5f, 0.1f, t);
        }

        if (useHDR)
        {
            targetColor *= hdrIntensity;
            intensity = hdrAdaptationCurve.Evaluate(intensity);
        }

        mainLight.color = targetColor;
        mainLight.intensity = intensity;
    }

    private void ApplyWeatherEffects()
    {
        if (wetterSystem == null) return;

        // Anpassung an neue WetterZustand Struktur
        float cloudCover = wetterSystem.aktuellerZustand.bewoelkung;
        float precipitation = wetterSystem.aktuellerZustand.niederschlag;

        // Erweiterte Lichtdämpfung
        float baseAttenuation = 1f - (cloudCover * 0.5f + precipitation * 0.3f);
        float scatteringAttenuation = Mathf.Lerp(1f, 0.7f, aktuellerWetterZustand.feuchtigkeit);
        float finalAttenuation = baseAttenuation * scatteringAttenuation * weatherInfluence;

        // Wende Dämpfung auf verschiedene Lichtquellen an
        ApplyWeatherAttenuation(finalAttenuation);
        
        // Aktualisiere volumetrische Effekte
        if (useVolumetricLighting && volumetricMaterial != null)
        {
            UpdateVolumetricWeatherEffects();
        }
    }

    private void ApplyWeatherAttenuation(float attenuation)
    {
        if (mainLight != null)
        {
            mainLight.intensity *= attenuation;
            
            // Farbtemperatur-Anpassung bei Bewölkung
            float colorTemp = Mathf.Lerp(6500f, 5500f, aktuellerWetterZustand.bewoelkung);
            mainLight.colorTemperature = colorTemp;
        }

        if (moonLight != null)
        {
            moonLight.intensity *= attenuation * 0.5f; // Mond wird stärker gedämpft
        }
    }

    private void UpdateVolumetricWeatherEffects()
    {
        volumetricMaterial.SetFloat("_Density", 
            Mathf.Lerp(0.1f, 0.3f, aktuellerWetterZustand.feuchtigkeit));
        
        volumetricMaterial.SetFloat("_ScatteringAnisotropy", 
            Mathf.Lerp(0.1f, 0.7f, aktuellerWetterZustand.niederschlag));
        
        volumetricMaterial.SetColor("_ScatteringCoefficients", 
            CalculateScatteringCoefficients(aktuellerWetterZustand));
    }

    private Color CalculateScatteringCoefficients(WetterZustand wetter)
    {
        // Berechne wellenlängenabhängige Streuung basierend auf Luftfeuchtigkeit
        float feuchtigkeit = wetter.feuchtigkeit;
        return new Color(
            0.5f + feuchtigkeit * 0.2f,  // Mehr Streuung im roten Bereich
            0.5f + feuchtigkeit * 0.1f,  // Mittlere Streuung im grünen Bereich
            0.5f,                    // Basis-Streuung im blauen Bereich
            1f
        );
    }

    private void UpdateVolumetricEffects(float dayProgress)
    {
        if (volumetricMaterial == null) return;

        float sunHeight = Mathf.Sin(dayProgress * Mathf.PI * 2f);
        float scatteringIntensity = Mathf.Max(0.2f, sunHeight) * volumetricIntensity;

        volumetricMaterial.SetFloat("_ScatteringIntensity", scatteringIntensity);
        volumetricMaterial.SetVector("_SunDirection", -mainLight.transform.forward);
    }

    private void UpdateAtmosphericScattering(float dayProgress)
    {
        if (atmosphereMaterial == null) return;

        float sunHeight = Mathf.Sin(dayProgress * Mathf.PI * 2f);
        Vector3 sunDirection = mainLight.transform.forward;

        atmosphereMaterial.SetVector("_SunDirection", sunDirection);
        atmosphereMaterial.SetFloat("_SunHeight", sunHeight);
        atmosphereMaterial.SetFloat("_RayleighScattering", rayleighScattering);
        atmosphereMaterial.SetFloat("_MieScattering", mieScattering);
        
        // Anpassung der atmosphärischen Dichte basierend auf Wetter
        if (aktuellerWetterZustand != null)
        {
            float atmosphericDensity = 1f + aktuellerWetterZustand.feuchtigkeit * 0.5f;
            atmosphereMaterial.SetFloat("_AtmosphericDensity", atmosphericDensity);
        }
    }

    private void UpdateCloudSystem()
    {
        if (volumetricCloudMaterial == null) return;

        // Wolkenbewegung
        Vector2 cloudOffset = Time.time * cloudSpeed * cloudDirection.normalized;
        volumetricCloudMaterial.SetVector("_CloudOffset", cloudOffset);

        // Wettereinfluss auf Wolken
        if (aktuellerWetterZustand != null)
        {
            float coverage = Mathf.Lerp(cloudCoverage, 1f, aktuellerWetterZustand.bewoelkung);
            float density = Mathf.Lerp(1f, 2f, aktuellerWetterZustand.feuchtigkeit);
            
            volumetricCloudMaterial.SetFloat("_CloudCoverage", coverage);
            volumetricCloudMaterial.SetFloat("_CloudDensity", density);
        }
    }

    private void UpdateIndirectLighting()
    {
        if (!useScreenSpaceReflections) return;

        // Update reflection probes basierend auf Tageszeit und Wetter
        foreach (var probe in reflectionProbes)
        {
            if (probe.refreshMode == ReflectionProbeRefreshMode.EveryFrame)
            {
                UpdateProbeSettings(probe);
            }
        }

        // Aktualisiere Light Probes
        DynamicGI.UpdateEnvironment();
    }

    private void UpdateProbeSettings(ReflectionProbe probe)
    {
        if (aktuellerWetterZustand != null)
        {
            // Reduziere Reflexionsintensität bei schlechtem Wetter
            float intensity = Mathf.Lerp(1f, 0.3f, 
                aktuellerWetterZustand.bewoelkung + aktuellerWetterZustand.niederschlag);
            probe.intensity = intensity * hdrIntensity;

            // Passe Blending-Distanz an Sichtweite an
            float blendDistance = Mathf.Lerp(1f, 0.3f, aktuellerWetterZustand.niederschlag);
            probe.blendDistance = blendDistance;
        }
    }

    private void UpdateAmbientLighting()
    {
        RenderSettings.ambientSkyColor = mainLight.color * mainLight.intensity;
        RenderSettings.ambientEquatorColor = mainLight.color * mainLight.intensity * 0.7f;
        RenderSettings.ambientGroundColor = mainLight.color * mainLight.intensity * 0.3f;
    }
}