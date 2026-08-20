using UnityEngine;
using UnityEngine.UI;

public class MenuOpciones : MonoBehaviour
{
    [Header("Sliders")]
    public Slider sliderSensibilidad;
    public Slider sliderBrillo;

    [Header("Valores por Defecto")]
    public float sensibilidadPorDefecto = 0.5f;
    public float brilloPorDefecto = 0f;

    [Header("Referencias")]
    public FirstPersonController playerController;
    public ControlBrillo controlBrillo;

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
            if (controlBrillo == null)
            {
                Debug.LogError("?? No se encontró ControlBrillo en la escena");
            }
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

        Debug.Log("?? Valores restaurados a por defecto");
    }
}