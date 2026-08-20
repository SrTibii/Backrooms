using UnityEngine;

public class ControlGlitchEffect : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject glitchEffectObject; // El GameObject que contiene el VideoPlayer

    [Header("Configuración")]
    [Tooltip("Si está activado, el efecto Glitch está activo")]
    public bool glitchPorDefecto = true;

    [Header("Debug")]
    public bool mostrarLogs = true;

    private bool glitchActual = true;
    private const string GLITCH_KEY = "GlitchEffect";

    void Start()
    {
        // Buscar el GameObject si no está asignado
        if (glitchEffectObject == null)
        {
            // Buscar por nombre o tag
            glitchEffectObject = GameObject.Find("GlitchEffect");
            if (glitchEffectObject == null)
            {
                // Buscar por tag si tiene
                GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
                foreach (var obj in allObjects)
                {
                    if (obj.CompareTag("GlitchEffect"))
                    {
                        glitchEffectObject = obj;
                        break;
                    }
                }
            }

            if (glitchEffectObject == null)
            {
                Debug.LogError("?? No se encontró el GameObject GlitchEffect");
                return;
            }
        }

        // Cargar estado guardado
        glitchActual = PlayerPrefs.GetInt(GLITCH_KEY, glitchPorDefecto ? 1 : 0) == 1;
        AplicarGlitch(glitchActual);

        if (mostrarLogs)
        {
            Debug.Log($"?? Glitch Effect cargado: {(glitchActual ? "ACTIVADO" : "DESACTIVADO")}");
        }
    }

    // ============================================
    // MÉTODO PARA CAMBIAR GLITCH EFFECT
    // ============================================

    public void SetGlitchEffect(bool activar)
    {
        glitchActual = activar;
        PlayerPrefs.SetInt(GLITCH_KEY, activar ? 1 : 0);
        PlayerPrefs.Save();
        AplicarGlitch(activar);

        if (mostrarLogs)
        {
            Debug.Log($"?? Glitch Effect: {(activar ? "ACTIVADO" : "DESACTIVADO")}");
        }
    }

    public bool EstaActivado()
    {
        return glitchActual;
    }

    private void AplicarGlitch(bool activar)
    {
        if (glitchEffectObject != null)
        {
            glitchEffectObject.SetActive(activar);
        }
    }

    public void RestaurarPorDefecto()
    {
        SetGlitchEffect(glitchPorDefecto);
    }
}