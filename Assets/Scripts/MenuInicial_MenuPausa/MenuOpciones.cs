using UnityEngine;
using UnityEngine.UI;

public class MenuOpciones : MonoBehaviour
{
    [Header("Sliders")]
    public Slider sliderSensibilidad;

    [Header("Valores por Defecto")]
    [Tooltip("Sensibilidad por defecto (0.5 = 50%)")]
    public float sensibilidadPorDefecto = 0.5f;

    [Header("Referencias")]
    public FirstPersonController playerController;

    private const string SENSIBILIDAD_KEY = "SensibilidadRaton";

    void Start()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<FirstPersonController>();
        }

        // ============================================
        // CONFIGURAR SLIDER DE SENSIBILIDAD
        // ============================================
        if (sliderSensibilidad != null)
        {
            // Cargar valor guardado o usar el por defecto (0.5)
            float sensibilidadGuardada = PlayerPrefs.GetFloat(SENSIBILIDAD_KEY, sensibilidadPorDefecto);
            sliderSensibilidad.value = sensibilidadGuardada;

            // Añadir listener para cuando cambie el slider
            sliderSensibilidad.onValueChanged.AddListener(OnSensibilidadChanged);

            // Aplicar la sensibilidad guardada
            AplicarSensibilidad(sensibilidadGuardada);
        }
    }

    public void OnSensibilidadChanged(float value)
    {
        // Guardar en PlayerPrefs
        PlayerPrefs.SetFloat(SENSIBILIDAD_KEY, value);
        PlayerPrefs.Save();

        // Aplicar sensibilidad
        AplicarSensibilidad(value);

        Debug.Log($"?? Sensibilidad cambiada a: {value}");
    }

    private void AplicarSensibilidad(float sensibilidad)
    {
        if (playerController != null)
        {
            // ============================================
            // RANGO DE SENSIBILIDAD:
            // - Slider a 0 (0%) ? sensibilidad = 0 (ratón no se mueve)
            // - Slider a 0.5 (50%) ? sensibilidad = 1 (normal)
            // - Slider a 1 (100%) ? sensibilidad = 2 (rápido)
            // ============================================
            float sensibilidadAjustada = sensibilidad * 2f; // 0 a 2
            playerController.SetMouseSensitivity(sensibilidadAjustada);

            Debug.Log($"?? Sensibilidad aplicada: slider={sensibilidad}, ajustada={sensibilidadAjustada}");
        }
    }

    public void RestaurarValoresPorDefecto()
    {
        if (sliderSensibilidad != null)
        {
            sliderSensibilidad.value = sensibilidadPorDefecto;
        }
        Debug.Log("?? Sensibilidad restaurada a por defecto (0.5)");
    }
}
