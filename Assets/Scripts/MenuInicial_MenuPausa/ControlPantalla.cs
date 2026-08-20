using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ControlPantalla : MonoBehaviour
{
    [Header("Configuración")]
    public bool pantallaCompletaPorDefecto = true;

    [Header("Tamaño de Ventana (cuando no está en Fullscreen)")]
    public int anchoVentana = 1280;
    public int altoVentana = 720;

    [Header("VSync - Selector de Frecuencia")]
    public int vsyncIndexPorDefecto = 0;

    [Header("Referencias")]
    public TMP_Dropdown dropdownVSync;

    [Header("Debug")]
    public bool mostrarLogs = true;

    private int[] frecuenciasVSync = {
        0, 30, 60, 75, 90, 100, 120, 144, 165, 180, 200, 240, 280, 300, 360, 390, 480, 500
    };

    private const string PANTALLA_KEY = "PantallaCompleta";
    private const string VSYNC_INDEX_KEY = "VSyncIndex";

    private bool pantallaCompletaActual = true;
    private int vsyncIndexActual = 0;

    void Start()
    {
        ConfigurarDropdownVSync();

        // ============================================
        // CARGAR PANTALLA COMPLETA
        // ============================================
        pantallaCompletaActual = PlayerPrefs.GetInt(PANTALLA_KEY, pantallaCompletaPorDefecto ? 1 : 0) == 1;
        AplicarPantallaCompleta(pantallaCompletaActual);

        // ============================================
        // CARGAR VSYNC
        // ============================================
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

            // ============================================
            // FORZAR SOLO LA FRECUENCIA, NO EL MONITOR
            // ============================================
            Screen.SetResolution(Screen.width, Screen.height, Screen.fullScreen, freq);

            if (mostrarLogs)
            {
                Debug.Log($"?? VSync ACTIVADO - FPS limitados a {freq} Hz");
            }
        }
    }

    // ============================================
    // MÉTODO PARA PANTALLA COMPLETA (CORREGIDO)
    // ============================================

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
        // ============================================
        // OBTENER LA RESOLUCIÓN DEL MONITOR PRINCIPAL
        // ============================================
        int ancho = Screen.currentResolution.width;
        int alto = Screen.currentResolution.height;
        int refresco = Screen.currentResolution.refreshRate;

        if (activar)
        {
            // ============================================
            // PANTALLA COMPLETA - Usar resolución nativa
            // ============================================
            Screen.fullScreen = true;
            // Forzar resolución nativa en el monitor principal
            Screen.SetResolution(ancho, alto, true, refresco);
        }
        else
        {
            // ============================================
            // MODO VENTANA - Tamaño configurable
            // ============================================
            Screen.fullScreen = false;
            // Usar el tamaño configurado en modo ventana
            Screen.SetResolution(anchoVentana, altoVentana, false, refresco);
        }

        if (mostrarLogs)
        {
            Debug.Log($"?? Modo ventana: {(activar ? $"Fullscreen ({ancho}x{alto}@{refresco}Hz)" : $"Windowed ({anchoVentana}x{altoVentana})")}");
        }
    }

    // ============================================
    // MÉTODO PARA CAMBIAR EL TAMAÑO DE LA VENTANA
    // ============================================

    public void SetTamañoVentana(int ancho, int alto)
    {
        anchoVentana = ancho;
        altoVentana = alto;
        PlayerPrefs.SetInt("AnchoVentana", ancho);
        PlayerPrefs.SetInt("AltoVentana", alto);
        PlayerPrefs.Save();

        if (!pantallaCompletaActual)
        {
            int refresco = Screen.currentResolution.refreshRate;
            Screen.SetResolution(ancho, alto, false, refresco);
        }

        if (mostrarLogs)
        {
            Debug.Log($"?? Tamaño de ventana cambiado a: {ancho}x{alto}");
        }
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