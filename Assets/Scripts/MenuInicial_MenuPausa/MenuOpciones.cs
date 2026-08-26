using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuOpciones : MonoBehaviour
{
    [Header("Sliders")]
    public Slider sliderSensibilidad;
    public Slider sliderBrillo;
    public Slider sliderFOV;
    public Slider sliderVolumen;

    [Header("Toggles")]
    public Toggle toggleFPS;
    public Toggle toggleSombras;
    public Toggle togglePantallaCompleta;
    public Toggle toggleMotionBlur;
    public Toggle toggleGlitchEffect;

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
    public ControlGlitchEffect controlGlitchEffect;

    private const string SENSIBILIDAD_KEY = "SensibilidadRaton";

    void Start()
    {
        if (playerController == null) playerController = FindObjectOfType<FirstPersonController>();
        if (controlBrillo == null) controlBrillo = FindObjectOfType<ControlBrillo>();
        if (mostrarFPS == null) mostrarFPS = FindObjectOfType<MostrarFPS>();
        if (controlSombras == null) controlSombras = FindObjectOfType<ControlSombras>();
        if (controlFOV == null) controlFOV = FindObjectOfType<ControlFOV>();
        if (controlPantalla == null) controlPantalla = FindObjectOfType<ControlPantalla>();
        if (controlMotionBlur == null) controlMotionBlur = FindObjectOfType<ControlMotionBlur>();
        if (controlGlitchEffect == null) controlGlitchEffect = FindObjectOfType<ControlGlitchEffect>();

        if (sliderSensibilidad != null)
        {
            float sensibilidadGuardada = PlayerPrefs.GetFloat(SENSIBILIDAD_KEY, sensibilidadPorDefecto);
            sliderSensibilidad.value = sensibilidadGuardada;
            sliderSensibilidad.onValueChanged.AddListener(OnSensibilidadChanged);
            AplicarSensibilidad(sensibilidadGuardada);
        }

        if (sliderBrillo != null && controlBrillo != null)
        {
            float brilloGuardado = PlayerPrefs.GetFloat("Brillo", brilloPorDefecto);
            sliderBrillo.value = brilloGuardado;
            sliderBrillo.onValueChanged.AddListener(OnBrilloChanged);
            controlBrillo.SetBrillo(brilloGuardado);
        }

        if (sliderFOV != null && controlFOV != null)
        {
            float fovGuardado = PlayerPrefs.GetFloat("PaniniDistance", fovPorDefecto);
            sliderFOV.value = fovGuardado;
            sliderFOV.onValueChanged.AddListener(OnFOVChanged);
            controlFOV.SetFOV(fovGuardado);
        }

        if (sliderVolumen != null && AudioManager.Instance != null)
        {
            float volumenGuardado = AudioManager.Instance.GetVolumenGlobal();
            sliderVolumen.value = volumenGuardado;
            sliderVolumen.onValueChanged.AddListener(OnVolumenChanged);
        }

        if (toggleFPS != null && mostrarFPS != null)
        {
            bool fpsActivo = PlayerPrefs.GetInt("MostrarFPS", 1) == 1;
            toggleFPS.isOn = fpsActivo;
            toggleFPS.onValueChanged.AddListener(OnFPSChanged);
            mostrarFPS.ToggleMostrarFPS(fpsActivo);
        }

        if (toggleSombras != null && controlSombras != null)
        {
            bool sombrasActivas = PlayerPrefs.GetInt("SombrasActivadas", 1) == 1;
            toggleSombras.isOn = sombrasActivas;
            toggleSombras.onValueChanged.AddListener(OnSombrasChanged);
            controlSombras.SetSombras(sombrasActivas);
        }

        if (togglePantallaCompleta != null && controlPantalla != null)
        {
            bool pantallaCompleta = PlayerPrefs.GetInt("PantallaCompleta", 1) == 1;
            togglePantallaCompleta.isOn = pantallaCompleta;
            togglePantallaCompleta.onValueChanged.AddListener(OnPantallaCompletaChanged);
            controlPantalla.SetPantallaCompleta(pantallaCompleta);
        }

        if (toggleMotionBlur != null && controlMotionBlur != null)
        {
            bool motionBlurActivo = PlayerPrefs.GetInt("MotionBlur", 1) == 1;
            toggleMotionBlur.isOn = motionBlurActivo;
            toggleMotionBlur.onValueChanged.AddListener(OnMotionBlurChanged);
            controlMotionBlur.SetMotionBlur(motionBlurActivo);
        }

        if (toggleGlitchEffect != null && controlGlitchEffect != null)
        {
            bool glitchActivo = PlayerPrefs.GetInt("GlitchEffect", 1) == 1;
            toggleGlitchEffect.isOn = glitchActivo;
            toggleGlitchEffect.onValueChanged.AddListener(OnGlitchEffectChanged);
            controlGlitchEffect.SetGlitchEffect(glitchActivo);
        }

        if (dropdownVSync != null && controlPantalla != null)
        {
            controlPantalla.dropdownVSync = dropdownVSync;
            int vsyncIndex = PlayerPrefs.GetInt("VSyncIndex", 0);
            dropdownVSync.value = vsyncIndex;
        }

        Debug.Log("? MenuOpciones inicializado correctamente");
    }

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

    public void OnBrilloChanged(float value)
    {
        if (controlBrillo != null)
            controlBrillo.SetBrillo(value);
    }

    public void OnFOVChanged(float value)
    {
        if (controlFOV != null)
            controlFOV.SetFOV(value);
    }

    public void OnVolumenChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetVolumenGlobal(value);
        Debug.Log($"?? Volumen cambiado a: {value}");
    }

    public void OnFPSChanged(bool value)
    {
        if (mostrarFPS != null)
            mostrarFPS.ToggleMostrarFPS(value);
    }

    public void OnSombrasChanged(bool value)
    {
        if (controlSombras != null)
            controlSombras.SetSombras(value);
    }

    public void OnPantallaCompletaChanged(bool value)
    {
        if (controlPantalla != null)
            controlPantalla.SetPantallaCompleta(value);
    }

    public void OnMotionBlurChanged(bool value)
    {
        if (controlMotionBlur != null)
            controlMotionBlur.SetMotionBlur(value);
    }

    public void OnGlitchEffectChanged(bool value)
    {
        if (controlGlitchEffect != null)
            controlGlitchEffect.SetGlitchEffect(value);
    }

    public void RestaurarValoresPorDefecto()
    {
        if (sliderSensibilidad != null) sliderSensibilidad.value = sensibilidadPorDefecto;
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
        if (sliderVolumen != null && AudioManager.Instance != null)
        {
            sliderVolumen.value = 0.8f;
            AudioManager.Instance.SetVolumenGlobal(0.8f);
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
        if (togglePantallaCompleta != null && controlPantalla != null)
        {
            togglePantallaCompleta.isOn = true;
            controlPantalla.SetPantallaCompleta(true);
        }
        if (toggleMotionBlur != null && controlMotionBlur != null)
        {
            toggleMotionBlur.isOn = true;
            controlMotionBlur.SetMotionBlur(true);
        }
        if (toggleGlitchEffect != null && controlGlitchEffect != null)
        {
            toggleGlitchEffect.isOn = true;
            controlGlitchEffect.SetGlitchEffect(true);
        }
        if (dropdownVSync != null && controlPantalla != null)
        {
            dropdownVSync.value = 0;
            controlPantalla.RestaurarPorDefecto();
        }

        Debug.Log("?? Todos los valores restaurados a por defecto");
    }
}