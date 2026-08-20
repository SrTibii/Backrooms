using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ControlPantalla : MonoBehaviour
{
    [Header("Configuración")]
    public bool pantallaCompletaPorDefecto = true;

    [Header("VSync - Selector de Frecuencia")]
    public int vsyncIndexPorDefecto = 0;

    [Header("Referencias")]
    public TMP_Dropdown dropdownVSync;

    [Header("Debug")]
    public bool mostrarLogs = true;

    // ============================================
    // FRECUENCIAS DISPONIBLES (ACTUALIZADO)
    // ============================================
    private int[] frecuenciasVSync = {
        0,      // VSync OFF (Sin límite)
        30,     // Muy bajo (PCs muy lentos)
        60,     // Estándar
        75,     // Básico
        90,     // Móviles / Tablets
        100,    // Gaming básico
        120,    // Consolas (PS5/Xbox)
        144,    // Gaming popular
        165,    // Gaming medio
        180,    // Gaming medio-alto
        200,    // Gaming alto
        240,    // Gaming competitivo
        280,    // Gaming competitivo alto
        300,    // Gaming competitivo alto
        360,    // Gaming profesional
        390,    // Gaming profesional
        480,    // Gaming premium
        500     // Gaming premium
    };

    private const string PANTALLA_KEY = "PantallaCompleta";
    private const string VSYNC_INDEX_KEY = "VSyncIndex";

    private bool pantallaCompletaActual = true;
    private int vsyncIndexActual = 0;

    void Start()
    {
        ConfigurarDropdownVSync();

        pantallaCompletaActual = PlayerPrefs.GetInt(PANTALLA_KEY, pantallaCompletaPorDefecto ? 1 : 0) == 1;
        AplicarPantallaCompleta(pantallaCompletaActual);

        vsyncIndexActual = PlayerPrefs.GetInt(VSYNC_INDEX_KEY, vsyncIndexPorDefecto);
        vsyncIndexActual = Mathf.Clamp(vsyncIndexActual, 0, frecuenciasVSync.Length - 1);

        if (dropdownVSync != null)
        {
            dropdownVSync.value = vsyncIndexActual;
        }

        AplicarVSync(vsyncIndexActual);

        if (mostrarLogs)
        {
            Debug.Log($"?? Pantalla completa: {(pantallaCompletaActual ? "ACTIVADA" : "DESACTIVADA")}");
            Debug.Log($"?? VSync seleccionado: {ObtenerNombreFrecuencia(vsyncIndexActual)}");
        }
    }

    private void ConfigurarDropdownVSync()
    {
        if (dropdownVSync == null) return;

        dropdownVSync.ClearOptions();

        List<string> opciones = new List<string>();

        foreach (int freq in frecuenciasVSync)
        {
            opciones.Add(ObtenerNombreFrecuencia(freq));
        }

        dropdownVSync.AddOptions(opciones);
        dropdownVSync.onValueChanged.AddListener(OnVSyncDropdownChanged);
    }

    private string ObtenerNombreFrecuencia(int freq)
    {
        if (freq == 0) return "VSync OFF";
        return $"VSync {freq} Hz";
    }

    public void OnVSyncDropdownChanged(int index)
    {
        vsyncIndexActual = Mathf.Clamp(index, 0, frecuenciasVSync.Length - 1);
        PlayerPrefs.SetInt(VSYNC_INDEX_KEY, vsyncIndexActual);
        PlayerPrefs.Save();
        AplicarVSync(vsyncIndexActual);

        if (mostrarLogs)
        {
            Debug.Log($"?? VSync cambiado a: {ObtenerNombreFrecuencia(vsyncIndexActual)}");
        }
    }

    private void AplicarVSync(int index)
    {
        int freq = frecuenciasVSync[index];

        if (freq == 0)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 300;

            if (mostrarLogs)
            {
                Debug.Log($"?? VSync DESACTIVADO - FPS limitados a 300");
            }
        }
        else
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = freq;

            Screen.SetResolution(Screen.width, Screen.height, Screen.fullScreen, freq);

            if (mostrarLogs)
            {
                Debug.Log($"?? VSync ACTIVADO - FPS limitados a {freq} Hz");
            }
        }
    }

    public void SetPantallaCompleta(bool activar)
    {
        pantallaCompletaActual = activar;
        PlayerPrefs.SetInt(PANTALLA_KEY, activar ? 1 : 0);
        PlayerPrefs.Save();
        AplicarPantallaCompleta(activar);

        if (mostrarLogs)
        {
            Debug.Log($"?? Pantalla completa: {(activar ? "ACTIVADA" : "DESACTIVADA")}");
        }
    }

    public bool EstaPantallaCompleta()
    {
        return pantallaCompletaActual;
    }

    private void AplicarPantallaCompleta(bool activar)
    {
        Screen.fullScreen = activar;
    }

    public int GetFrecuenciaActual()
    {
        return frecuenciasVSync[vsyncIndexActual];
    }

    public string GetNombreFrecuenciaActual()
    {
        return ObtenerNombreFrecuencia(vsyncIndexActual);
    }

    public void RestaurarPorDefecto()
    {
        SetPantallaCompleta(pantallaCompletaPorDefecto);

        if (dropdownVSync != null)
        {
            dropdownVSync.value = vsyncIndexPorDefecto;
        }

        OnVSyncDropdownChanged(vsyncIndexPorDefecto);
    }
}