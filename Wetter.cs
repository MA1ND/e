using UnityEngine;
using System.Collections.Generic;

public class Wetter : MonoBehaviour 
{
    [System.Serializable]
    public struct WetterZustand 
    {
        public float temperatur;
        public float niederschlag;
        public float bewoelkung; 
        public float windgeschwindigkeit;
        public float feuchtigkeit;
    }

    public WetterZustand aktuellerZustand;

    // Event für Wetteränderungen
    public event System.Action<WetterZustand> OnWeatherChanged;

    public float BerechneTemperatur(float basisTemp, float hoehe, float breitengrad, float tageszeit)
    {
        float hoehenEffekt = Mathf.Lerp(1f, 0f, hoehe * 0.001f);
        float breitengradEffekt = Mathf.Cos(breitengrad * Mathf.Deg2Rad);
        float zeitEffekt = Mathf.Sin(tageszeit * Mathf.PI * 2f - Mathf.PI/2) * 0.5f + 0.5f;
        
        return basisTemp * hoehenEffekt * breitengradEffekt * zeitEffekt;
    }

    public float BerechneFeuchtigkeit(float basisFeuchtigkeit)
    {
        return Mathf.Clamp01(basisFeuchtigkeit * 
            (1 - aktuellerZustand.windgeschwindigkeit * 0.1f));
    }

    public float BerechneNiederschlag()
    {
        return Mathf.Clamp01(
            (aktuellerZustand.feuchtigkeit - 0.6f) * 2f * 
            (aktuellerZustand.bewoelkung + 0.2f)
        );
    }

    // Getter für aktuellen Zustand
    public WetterZustand GetCurrentWeatherState()
    {
        return aktuellerZustand;
    }

    // Jahreszeiten-Fortschritt (0..1)
    public float GetSeasonProgress()
    {
        return (Mathf.Sin(Time.time * 0.0001f) + 1f) * 0.5f;
    }
}