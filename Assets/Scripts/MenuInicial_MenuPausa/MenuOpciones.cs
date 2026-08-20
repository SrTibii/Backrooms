using UnityEngine;
using UnityEngine.UI;

public class MenuOpciones : MonoBehaviour
{
    [Header("Sliders")]
    public Slider sliderSensibilidad;
    public Slider sliderBrillo;
    public Slider sliderFOV;

    [Header("Toggles")]
    public Toggle toggleFPS;
    public Toggle toggleSombras;

    [Header("Valores por Defecto")]
    public float sensibilidadPorDefecto = 0.5f;
    public float brilloPorDefecto = 0f;
    public float fovPorDefecto = 0.5f;

    [Header("Referencias")]
    public FirstPersonController playerController;
    public ControlBrillo controlBrillo;
    public MostrarFPS mostrarFPS;
    public ControlSombras controlSombras;
    public ControlFOV controlFOV;

    private const string SENSIBILIDAD_KEY = "SensibilidadRaton";

    void Start()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<FirstPersonController>();
        }

        if (controlBrillo == null)
        {
            controlBrillo = FindObjectOfType<ControlBrillo>();
        }

        if (mostrarFPS == null)
        {
            mostrarFPS = FindObjectOfType<MostrarFPS>();
        }

        if (controlSombras == null)
        {
            controlSombras = FindObjectOfType<ControlSombras>();
        }

        if (controlFOV == null)
        {
            controlFOV = FindObjectOfType<ControlFOV>();
        }

        // ============================================
        // CONFIGURAR SLIDER DE SENSIBILIDAD
        // ============================================
        if (sliderSensibilidad != null)
        {
            float sensibilidadGuardada = PlayerPrefs.GetFloat(SENSIBILIDAD_KEY, sensibilidadPorDefecto);
            sliderSensibilidad.value = sensibilidadGuardada;
            sliderSensibilidad.onValueChanged.AddListener(OnSensibilidadChanged);
            AplicarSensibilidad(sensibilidadGuardada);
        }

        // ============================================
        // CONFIGURAR SLIDER DE BRILLO
        // ============================================
        if (sliderBrillo != null && controlBrillo != null)
        {
            float brilloGuardado = PlayerPrefs.GetFloat("Brillo", brilloPorDefecto);
            sliderBrillo.value = brilloGuardado;
            sliderBrillo.onValueChanged.AddListener(OnBrilloChanged);
            controlBrillo.SetBrillo(brilloGuardado);
        }

        // ============================================
        // CONFIGURAR SLIDER DE FOV
        // ============================================
        if (sliderFOV != null && controlFOV != null)
        {
            float fovGuardado = PlayerPrefs.GetFloat("PaniniDistance", fovPorDefecto);
            sliderFOV.value = fovGuardado;
            sliderFOV.onValueChanged.AddListener(OnFOVChanged);
            controlFOV.SetFOV(fovGuardado);
        }

        // ============================================
        // CONFIGURAR TOGGLE DE FPS
        // ============================================
        if (toggleFPS != null && mostrarFPS != null)
        {
            bool fpsActivo = PlayerPrefs.GetInt("MostrarFPS", 1) == 1;
            toggleFPS.isOn = fpsActivo;
            toggleFPS.onValueChanged.AddListener(OnFPSChanged);
            mostrarFPS.ToggleMostrarFPS(fpsActivo);
        }

        // ============================================
        // CONFIGURAR TOGGLE DE SOMBRAS
        // ============================================
        if (toggleSombras != null && controlSombras != null)
        {
            bool sombrasActivas = PlayerPrefs.GetInt("SombrasActivadas", 1) == 1;
            toggleSombras.isOn = sombrasActivas;
            toggleSombras.onValueChanged.AddListener(OnSombrasChanged);
            controlSombras.SetSombras(sombrasActivas);
        }
    }

    // ============================================
    // MÉTODO PARA SENSIBILIDAD
    // ============================================

    public void OnSensibilidadChanged(float value)
    {
        PlayerPrefs.SetFloat(SENSIBILIDAD_KEY, value);
        PlayerPrefs.Save();
        AplicarSensibilidad(value);
    }

    private void AplicarSensibilidad(float sensibilidad)
    {
        if (playerController != null)
        {
            float sensibilidadAjustada = sensibilidad * 2f;
            playerController.SetMouseSensitivity(sensibilidadAjustada);
        }
    }

    // ============================================
    // MÉTODO PARA BRILLO
    // ============================================

    public void OnBrilloChanged(float value)
    {
        if (controlBrillo != null)
        {
            controlBrillo.SetBrillo(value);
        }
    }

    // ============================================
    // MÉTODO PARA FOV
    // ============================================

    public void OnFOVChanged(float value)
    {
        if (controlFOV != null)
        {
            controlFOV.SetFOV(value);
        }
    }

    // ============================================
    // MÉTODO PARA FPS
    // ============================================

    public void OnFPSChanged(bool value)
    {
        if (mostrarFPS != null)
        {
            mostrarFPS.ToggleMostrarFPS(value);
        }
    }

    // ============================================
    // MÉTODO PARA SOMBRAS
    // ============================================

    public void OnSombrasChanged(bool value)
    {
        if (controlSombras != null)
        {
            controlSombras.SetSombras(value);
        }
    }

    // ============================================
    // RESTAURAR VALORES POR DEFECTO
    // ============================================

    public void RestaurarValoresPorDefecto()
    {
        if (sliderSensibilidad != null)
        {
            sliderSensibilidad.value = sensibilidadPorDefecto;
        }

        if (sliderBrillo != null && controlBrillo != null)
        {
            sliderBrillo.value = brilloPorDefecto;
            controlBrillo.SetBrillo(brilloPorDefecto);
        }

        if (sliderFOV != null && controlFOV != null)
        {
            sliderFOV.value = fovPorDefecto;
            controlFOV.SetFOV(fovPorDefecto);
        }

        if (toggleFPS != null && mostrarFPS != null)
        {
            toggleFPS.isOn = true;
            mostrarFPS.ToggleMostrarFPS(true);
        }

        if (toggleSombras != null && controlSombras != null)
        {
            toggleSombras.isOn = true;
            controlSombras.SetSombras(true);
        }

        Debug.Log("?? Valores restaurados a por defecto");
    }
}