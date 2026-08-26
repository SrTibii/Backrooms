using UnityEngine;
using UnityEngine.InputSystem;

public class Linterna : MonoBehaviour
{
    [Header("Luces")]
    public Light flashLight;

    [Header("Input")]
    public InputActionReference flashLightPressed;

    [Header("Volumen")]
    [Range(0f, 1f)] public float volumenSonidos = 0.7f;

    // Referencia al sistema de recogida
    private RecogerLinterna recogerLinterna;
    private bool isInHand = false;

    void Start()
    {
        recogerLinterna = FindObjectOfType<RecogerLinterna>();
        Debug.Log("? Linterna inicializada");
    }

    private void OnEnable()
    {
        if (flashLightPressed != null)
        {
            flashLightPressed.action.performed += OnFlashLightPressed;
            flashLightPressed.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (flashLightPressed != null)
        {
            flashLightPressed.action.performed -= OnFlashLightPressed;
            flashLightPressed.action.Disable();
        }
    }

    private void OnFlashLightPressed(InputAction.CallbackContext context)
    {
        if (isInHand)
        {
            bool nuevoEstado = !flashLight.enabled;
            flashLight.enabled = nuevoEstado;

            if (nuevoEstado)
            {
                ReproducirSonidoEncender();
                Debug.Log($"?? Linterna ENCENDIDA");
            }
            else
            {
                ReproducirSonidoApagar();
                Debug.Log($"?? Linterna APAGADA");
            }
        }
        else
        {
            Debug.Log("?? No tienes la linterna en la mano");
        }
    }

    // ============================================
    // ?? REPRODUCIR SONIDO DE ENCENDER - USA CLIP ESPECÍFICO
    // ============================================
    private void ReproducirSonidoEncender()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("?? AudioManager no disponible");
            return;
        }

        // ?? USA EL CLIP ESPECÍFICO DE LA LINTERNA
        AudioClip clip = AudioManager.Instance.sonidoEncenderLinterna;

        if (clip != null)
        {
            // Aplicar volumen global
            float volumenFinal = volumenSonidos * AudioManager.Instance.GetVolumenGlobal();
            AudioManager.Instance.PlayOneShotAtPosition(
                clip,
                transform.position,
                volumenFinal,
                5f
            );
            Debug.Log($"?? Sonido de encender linterna reproducido");
        }
        else
        {
            Debug.LogWarning("?? No hay sonido de encender linterna en AudioManager");
        }
    }

    // ============================================
    // ?? REPRODUCIR SONIDO DE APAGAR - USA CLIP ESPECÍFICO
    // ============================================
    private void ReproducirSonidoApagar()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("?? AudioManager no disponible");
            return;
        }

        // ?? USA EL CLIP ESPECÍFICO DE LA LINTERNA
        AudioClip clip = AudioManager.Instance.sonidoApagarLinterna;

        if (clip != null)
        {
            float volumenFinal = volumenSonidos * AudioManager.Instance.GetVolumenGlobal();
            AudioManager.Instance.PlayOneShotAtPosition(
                clip,
                transform.position,
                volumenFinal,
                5f
            );
            Debug.Log($"?? Sonido de apagar linterna reproducido");
        }
        else
        {
            Debug.LogWarning("?? No hay sonido de apagar linterna en AudioManager");
        }
    }

    public void SetInHand(bool inHand)
    {
        isInHand = inHand;
        Debug.Log($"?? Linterna {(inHand ? "en la mano" : "suelta")}");
    }

    public bool IsInHand()
    {
        return isInHand;
    }

    public void SetVolume(float newVolume)
    {
        volumenSonidos = Mathf.Clamp01(newVolume);
    }
}