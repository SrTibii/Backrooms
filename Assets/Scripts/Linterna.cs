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
        // Buscar el sistema de recogida
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
        // Solo se puede encender/apagar si está en la mano
        if (isInHand)
        {
            bool nuevoEstado = !flashLight.enabled;
            flashLight.enabled = nuevoEstado;

            // ============================================
            // REPRODUCIR SONIDO CON AUDIOMANAGER
            // ============================================
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
    // REPRODUCIR SONIDO DE ENCENDER CON AUDIOMANAGER
    // ============================================
    private void ReproducirSonidoEncender()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("?? AudioManager no disponible");
            return;
        }

        // Usar el sonido de zoomInSound como sonido de encender (o crear uno específico)
        // Si no hay un sonido específico para encender, usamos zoomInSound como placeholder
        AudioClip clip = AudioManager.Instance.zoomInSound;

        if (clip != null)
        {
            AudioManager.Instance.PlayOneShotAtPosition(
                clip,
                transform.position,
                volumenSonidos,
                5f
            );
            Debug.Log($"?? Sonido de encender reproducido");
        }
        else
        {
            Debug.LogWarning("?? No hay sonido de encender disponible en AudioManager");
        }
    }

    // ============================================
    // REPRODUCIR SONIDO DE APAGAR CON AUDIOMANAGER
    // ============================================
    private void ReproducirSonidoApagar()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("?? AudioManager no disponible");
            return;
        }

        // Usar el sonido de zoomOutSound como sonido de apagar (o crear uno específico)
        AudioClip clip = AudioManager.Instance.zoomOutSound;

        if (clip != null)
        {
            AudioManager.Instance.PlayOneShotAtPosition(
                clip,
                transform.position,
                volumenSonidos,
                5f
            );
            Debug.Log($"?? Sonido de apagar reproducido");
        }
        else
        {
            Debug.LogWarning("?? No hay sonido de apagar disponible en AudioManager");
        }
    }

    // Métodos para controlar el estado desde el sistema de recogida
    public void SetInHand(bool inHand)
    {
        isInHand = inHand;
        Debug.Log($"?? Linterna {(inHand ? "en la mano" : "suelta")}");
    }

    public bool IsInHand()
    {
        return isInHand;
    }

    // Método para cambiar el volumen desde fuera
    public void SetVolume(float newVolume)
    {
        volumenSonidos = Mathf.Clamp01(newVolume);
    }
}