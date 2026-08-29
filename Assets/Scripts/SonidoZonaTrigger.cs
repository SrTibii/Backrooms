using UnityEngine;

public class SonidoZonaTrigger : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public float volumen = 0.8f;

    [Header("Configuración")]
    public bool reproducirUnaVez = false;
    public float tiempoEspera = 0.5f;

    private bool yaReproducido = false;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.volume = volumen;

            // ?? FORZAR REGISTRO DE TODOS
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ForzarRegistroDeTodos();
            }
        }
    }

    void OnEnable()
    {
        // ============================================
        // ?? CADA VEZ QUE SE ACTIVA EL GAMEOBJECT, REGISTRAR
        // ============================================
        RegistrarEnAudioManager();
    }

    void OnDisable()
    {
        // ============================================
        // ?? DESREGISTRAR AL DESACTIVAR
        // ============================================
        if (audioSource != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.DesregistrarAudioSource(audioSource);
        }
    }

    void OnDestroy()
    {
        // ============================================
        // ?? DESREGISTRAR AL DESTRUIR
        // ============================================
        if (audioSource != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.DesregistrarAudioSource(audioSource);
        }
    }

    // ============================================
    // ?? REGISTRAR EN AUDIOMANAGER CON LOGS
    // ============================================
    private void RegistrarEnAudioManager()
    {
        if (audioSource == null) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.RegistrarAudioSource(audioSource);
            Debug.Log($"?? AudioSource registrado: {gameObject.name} - Clip: {audioSource.clip?.name ?? "NULL"}");
        }
        else
        {
            Debug.LogWarning($"?? AudioManager no disponible en {gameObject.name} - Intentando registrar más tarde...");
            // Intentar registrar después de 0.5 segundos si el AudioManager no está listo
            Invoke("RegistrarEnAudioManager", 0.5f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (reproducirUnaVez && yaReproducido) return;
        if (audioSource == null) return;

        // ============================================
        // ?? FORZAR REGISTRO ANTES DE REPRODUCIR
        // ============================================
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.RegistrarAudioSource(audioSource);
        }

        // ============================================
        // ?? APLICAR VOLUMEN GLOBAL
        // ============================================
        if (AudioManager.Instance != null)
        {
            float volumenGlobal = AudioManager.Instance.GetVolumenGlobal();
            audioSource.volume = volumen * volumenGlobal;
            Debug.Log($"?? Volumen aplicado: {audioSource.volume} (original: {volumen} * global: {volumenGlobal})");
        }
        else
        {
            audioSource.volume = volumen;
        }

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
            yaReproducido = true;
            Debug.Log($"?? Sonido ACTIVADO en {gameObject.name} - Clip: {audioSource.clip?.name ?? "NULL"}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (audioSource == null) return;

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log($"?? Sonido DETENIDO en {gameObject.name}");
        }
    }

    // ============================================
    // ?? MÉTODO PARA ACTUALIZAR EL VOLUMEN (OPCIONAL)
    // ============================================
    public void ActualizarVolumen()
    {
        if (audioSource != null && AudioManager.Instance != null)
        {
            float volumenGlobal = AudioManager.Instance.GetVolumenGlobal();
            audioSource.volume = volumen * volumenGlobal;
        }
    }
}