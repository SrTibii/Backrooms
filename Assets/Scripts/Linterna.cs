using UnityEngine;
using UnityEngine.InputSystem;

public class Linterna : MonoBehaviour
{
    [Header("Luces")]
    public Light flashLight;

    [Header("Input")]
    public InputActionReference flashLightPressed;

    [Header("Sonidos")]
    public AudioClip sonidoEncender;
    public AudioClip sonidoApagar;
    [Range(0f, 1f)] public float volumenSonidos = 0.7f;

    // AudioSource para reproducir sonidos
    private AudioSource audioSource;

    // Referencia al sistema de recogida
    private RecogerLinterna recogerLinterna;
    private bool isInHand = false;

    void Start()
    {
        // Buscar el sistema de recogida
        recogerLinterna = FindObjectOfType<RecogerLinterna>();

        // ?? Crear AudioSource si no existe
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configurar AudioSource
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // Sonido 3D
        audioSource.volume = volumenSonidos;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 10f;
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

            // ?? Reproducir sonido según el estado
            if (nuevoEstado)
            {
                ReproducirSonido(sonidoEncender);
                Debug.Log($"?? Linterna ENCENDIDA");
            }
            else
            {
                ReproducirSonido(sonidoApagar);
                Debug.Log($"?? Linterna APAGADA");
            }
        }
        else
        {
            Debug.Log("?? No tienes la linterna en la mano");
        }
    }

    // ?? Método para reproducir sonidos
    private void ReproducirSonido(AudioClip clip)
    {
        if (clip == null) return;

        audioSource.volume = volumenSonidos;
        audioSource.PlayOneShot(clip);
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

    // ?? Método para cambiar el volumen desde fuera
    public void SetVolume(float newVolume)
    {
        volumenSonidos = Mathf.Clamp01(newVolume);
        audioSource.volume = volumenSonidos;
    }
}