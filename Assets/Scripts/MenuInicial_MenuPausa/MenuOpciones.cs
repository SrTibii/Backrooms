using TMPro;
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
    public Toggle togglePantallaCompleta;
    public Toggle toggleMotionBlur;
    public Toggle toggleGlitchEffect; // ?? NUEVO

    [Header("Dropdowns")]
    public TMP_Dropdown dropdownVSync;

    [Header("Valores por Defecto")]
    public float sensibilidadPorDefecto = 0.5f;
    public float brilloPorDefecto = 0.3f;
    public float fovPorDefecto = 0.5f;

    [Header("Referencias")]
    public FirstPersonController playerController;
    public ControlBrillo controlBrillo;
    public MostrarFPS mostrarFPS;
    public ControlSombras controlSombras;
    public ControlFOV controlFOV;
    public ControlPantalla controlPantalla;
    public ControlMotionBlur controlMotionBlur;
    public ControlGlitchEffect controlGlitchEffect; // ?? NUEVO

    private const string SENSIBILIDAD_KEY = "SensibilidadRaton";

    void Start()
    {
        // ============================================
        // OBTENER REFERENCIAS
        // ============================================

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

        if (controlPantalla == null)
        {
            controlPantalla = FindObjectOfType<ControlPantalla>();
        }

        if (controlMotionBlur == null)
        {
            controlMotionBlur = FindObjectOfType<ControlMotionBlur>();
        }

        if (controlGlitchEffect == null)
        {
            controlGlitchEffect = FindObjectOfType<ControlGlitchEffect>();
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

        // ============================================
        // CONFIGURAR TOGGLE DE PANTALLA COMPLETA
        // ============================================

        if (togglePantallaCompleta != null && controlPantalla != null)
        {
            bool pantallaCompleta = PlayerPrefs.GetInt("PantallaCompleta", 1) == 1;
            togglePantallaCompleta.isOn = pantallaCompleta;
            togglePantallaCompleta.onValueChanged.AddListener(OnPantallaCompletaChanged);
            controlPantalla.SetPantallaCompleta(pantallaCompleta);
        }

        // ============================================
        // CONFIGURAR TOGGLE DE MOTION BLUR
        // ============================================

        if (toggleMotionBlur != null && controlMotionBlur != null)
        {
            bool motionBlurActivo = PlayerPrefs.GetInt("MotionBlur", 1) == 1;
            toggleMotionBlur.isOn = motionBlurActivo;
            toggleMotionBlur.onValueChanged.AddListener(OnMotionBlurChanged);
            controlMotionBlur.SetMotionBlur(motionBlurActivo);
        }

        // ============================================
        // CONFIGURAR TOGGLE DE GLITCH EFFECT
        // ============================================

        if (toggleGlitchEffect != null && controlGlitchEffect != null)
        {
            bool glitchActivo = PlayerPrefs.GetInt("GlitchEffect", 1) == 1;
            toggleGlitchEffect.isOn = glitchActivo;
            toggleGlitchEffect.onValueChanged.AddListener(OnGlitchEffectChanged);
            controlGlitchEffect.SetGlitchEffect(glitchActivo);
        }

        // ============================================
        // CONFIGURAR DROPDOWN DE VSYNC
        // ============================================

        if (dropdownVSync != null && controlPantalla != null)
        {
            controlPantalla.dropdownVSync = dropdownVSync;
            int vsyncIndex = PlayerPrefs.GetInt("VSyncIndex", 0);
            dropdownVSync.value = vsyncIndex;
        }

        Debug.Log("?? MenuOpciones inicializado correctamente");
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
    // MÉTODO PARA PANTALLA COMPLETA
    // ============================================

    public void OnPantallaCompletaChanged(bool value)
    {
        if (controlPantalla != null)
        {
            controlPantalla.SetPantallaCompleta(value);
        }
    }

    // ============================================
    // MÉTODO PARA MOTION BLUR
    // ============================================

    public void OnMotionBlurChanged(bool value)
    {
        if (controlMotionBlur != null)
        {
            controlMotionBlur.SetMotionBlur(value);
        }
    }

    // ============================================
    // MÉTODO PARA GLITCH EFFECT
    // ============================================

    public void OnGlitchEffectChanged(bool value)
    {
        if (controlGlitchEffect != null)
        {
            controlGlitchEffect.SetGlitchEffect(value);
        }
    }

    // ============================================
    // RESTAURAR VALORES POR DEFECTO
    // ============================================

    public void RestaurarValoresPorDefecto()
    {
        // Restaurar Sensibilidad
        if (sliderSensibilidad != null)
        {
            sliderSensibilidad.value = sensibilidadPorDefecto;
        }

        // Restaurar Brillo
        if (sliderBrillo != null && controlBrillo != null)
        {
            sliderBrillo.value = brilloPorDefecto;
            controlBrillo.SetBrillo(brilloPorDefecto);
        }

        // Restaurar FOV
        if (sliderFOV != null && controlFOV != null)
        {
            sliderFOV.value = fovPorDefecto;
            controlFOV.SetFOV(fovPorDefecto);
        }

        // Restaurar FPS
        if (toggleFPS != null && mostrarFPS != null)
        {
            toggleFPS.isOn = true;
            mostrarFPS.ToggleMostrarFPS(true);
        }

        // Restaurar Sombras
        if (toggleSombras != null && controlSombras != null)
        {
            toggleSombras.isOn = true;
            controlSombras.SetSombras(true);
        }

        // Restaurar Pantalla Completa
        if (togglePantallaCompleta != null && controlPantalla != null)
        {
            togglePantallaCompleta.isOn = true;
            controlPantalla.SetPantallaCompleta(true);
        }

        // Restaurar Motion Blur
        if (toggleMotionBlur != null && controlMotionBlur != null)
        {
            toggleMotionBlur.isOn = true;
            controlMotionBlur.SetMotionBlur(true);
        }

        // Restaurar Glitch Effect
        if (toggleGlitchEffect != null && controlGlitchEffect != null)
        {
            toggleGlitchEffect.isOn = true;
            controlGlitchEffect.SetGlitchEffect(true);
        }

        // Restaurar VSync
        if (dropdownVSync != null && controlPantalla != null)
        {
            dropdownVSync.value = 0;
            controlPantalla.RestaurarPorDefecto();
        }

        Debug.Log("?? Todos los valores restaurados a por defecto");
    }
}