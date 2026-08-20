using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ControlFOV : MonoBehaviour
{
    [Header("Referencias")]
    public Volume globalVolume; // El Volume global de la escena

    [Header("Configuración")]
    [Tooltip("Valor mínimo del Panini Projection (0 = sin efecto)")]
    public float valorMinimo = 0f;

    [Tooltip("Valor máximo del Panini Projection (1 = máximo efecto)")]
    public float valorMaximo = 1f;

    [Tooltip("Valor por defecto (0.5 = efecto medio)")]
    public float valorPorDefecto = 0.5f;

    [Header("Debug")]
    public bool mostrarLogs = true;

    private PaniniProjection paniniProjection;
    private float valorActual = 0.5f;
    private const string FOV_KEY = "PaniniDistance";

    void Start()
    {
        // Buscar el Volume global si no está asignado
        if (globalVolume == null)
        {
            globalVolume = FindObjectOfType<Volume>();
            if (globalVolume == null)
            {
                Debug.LogError("?? No se encontró Volume global en la escena");
                return;
            }
        }

        // Obtener el componente PaniniProjection del Volume
        if (globalVolume.profile != null)
        {
            if (globalVolume.profile.TryGet<PaniniProjection>(out paniniProjection))
            {
                // Cargar valor guardado o usar el por defecto
                valorActual = PlayerPrefs.GetFloat(FOV_KEY, valorPorDefecto);
                AplicarFOV(valorActual);

                if (mostrarLogs)
                {
                    Debug.Log($"?? Panini Projection cargado: {valorActual}");
                }
            }
            else
            {
                Debug.LogError("?? No se encontró PaniniProjection en el Volume profile");
            }
        }
    }

    // ============================================
    // MÉTODO PARA CAMBIAR EL FOV
    // ============================================

    public void SetFOV(float valor)
    {
        valorActual = Mathf.Clamp(valor, valorMinimo, valorMaximo);
        PlayerPrefs.SetFloat(FOV_KEY, valorActual);
        PlayerPrefs.Save();
        AplicarFOV(valorActual);

        if (mostrarLogs)
        {
            Debug.Log($"?? FOV cambiado a: {valorActual}");
        }
    }

    public float GetFOV()
    {
        return valorActual;
    }

    private void AplicarFOV(float valor)
    {
        if (paniniProjection != null)
        {
            paniniProjection.distance.value = valor;
        }
    }

    public void RestaurarPorDefecto()
    {
        SetFOV(valorPorDefecto);
    }

    // ============================================
    // MÉTODO PARA SABER SI ESTÁ ACTIVO
    // ============================================

    public bool EstaActivo()
    {
        return valorActual > 0.01f;
    }
}