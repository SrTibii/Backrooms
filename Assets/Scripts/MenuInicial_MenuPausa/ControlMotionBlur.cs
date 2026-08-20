using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ControlMotionBlur : MonoBehaviour
{
    [Header("Referencias")]
    public Volume globalVolume; // El Volume global de la escena

    [Header("Configuración")]
    [Tooltip("Si está activado, el Motion Blur está activo")]
    public bool motionBlurPorDefecto = true;

    [Header("Debug")]
    public bool mostrarLogs = true;

    private MotionBlur motionBlur;
    private bool motionBlurActual = true;
    private const string MOTION_BLUR_KEY = "MotionBlur";

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

        // Obtener el componente MotionBlur del Volume
        if (globalVolume.profile != null)
        {
            if (globalVolume.profile.TryGet<MotionBlur>(out motionBlur))
            {
                // Cargar valor guardado o usar el por defecto
                motionBlurActual = PlayerPrefs.GetInt(MOTION_BLUR_KEY, motionBlurPorDefecto ? 1 : 0) == 1;
                AplicarMotionBlur(motionBlurActual);

                if (mostrarLogs)
                {
                    Debug.Log($"?? Motion Blur cargado: {(motionBlurActual ? "ACTIVADO" : "DESACTIVADO")}");
                }
            }
            else
            {
                Debug.LogWarning("?? No se encontró MotionBlur en el Volume profile");
            }
        }
    }

    // ============================================
    // MÉTODO PARA CAMBIAR MOTION BLUR
    // ============================================

    public void SetMotionBlur(bool activar)
    {
        motionBlurActual = activar;
        PlayerPrefs.SetInt(MOTION_BLUR_KEY, activar ? 1 : 0);
        PlayerPrefs.Save();
        AplicarMotionBlur(activar);

        if (mostrarLogs)
        {
            Debug.Log($"?? Motion Blur: {(activar ? "ACTIVADO" : "DESACTIVADO")}");
        }
    }

    public bool EstaActivado()
    {
        return motionBlurActual;
    }

    private void AplicarMotionBlur(bool activar)
    {
        if (motionBlur != null)
        {
            motionBlur.active = activar;
        }
    }

    public void RestaurarPorDefecto()
    {
        SetMotionBlur(motionBlurPorDefecto);
    }
}