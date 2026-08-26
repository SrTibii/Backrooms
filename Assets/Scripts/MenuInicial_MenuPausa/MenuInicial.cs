using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuInicial : MonoBehaviour
{
    [Header("Audio")]
    [Range(0f, 1f)] public float volumenSonidos = 0.8f;

    [Header("Delay")]
    [Tooltip("Tiempo de espera antes de cargar la escena después del click")]
    public float delayAntesDeCargar = 0.3f;

    private AudioSource audioSource;
    private bool isLoading = false;

    void Start()
    {
        // ============================================
        // CREAR AUDIOSOURCE Y REGISTRAR EN AUDIOMANAGER
        // ============================================
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = volumenSonidos;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.RegistrarAudioSource(audioSource);
        }
    }

    // ============================================
    // MÉTODOS PARA EVENT TRIGGER (Pointer Enter)
    // ============================================

    public void SonidoHover()
    {
        if (audioSource == null || isLoading) return;

        // ============================================
        // REPRODUCIR SONIDO HOVER CON AUDIOMANAGER
        // ============================================
        if (AudioManager.Instance != null && AudioManager.Instance.sonidoHover != null)
        {
            AudioManager.Instance.PlayOneShot(audioSource, AudioManager.Instance.sonidoHover, volumenSonidos);
        }
        else if (audioSource != null)
        {
            // Fallback por si no hay AudioManager
            audioSource.PlayOneShot(AudioManager.Instance?.sonidoHover, volumenSonidos);
        }
    }

    // ============================================
    // MÉTODOS PARA BOTONES (OnClick)
    // ============================================

    public void Jugar()
    {
        if (isLoading) return;
        StartCoroutine(CargarConDelay("VHSPlay"));
    }

    public void IrAlMenuInicial()
    {
        if (isLoading) return;
        StartCoroutine(CargarConDelay("MenuInicial"));
    }

    public void Salir()
    {
        if (isLoading) return;
        StartCoroutine(SalirConDelay());
    }

    // ============================================
    // CORRUTINAS CON DELAY
    // ============================================

    private IEnumerator CargarConDelay(string nombreEscena)
    {
        isLoading = true;

        // ============================================
        // REPRODUCIR SONIDO CLICK CON AUDIOMANAGER
        // ============================================
        if (AudioManager.Instance != null && AudioManager.Instance.sonidoClick != null)
        {
            AudioManager.Instance.PlayOneShot(audioSource, AudioManager.Instance.sonidoClick, volumenSonidos);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(AudioManager.Instance?.sonidoClick, volumenSonidos);
        }

        // Esperar el delay para que se escuche el sonido
        yield return new WaitForSeconds(delayAntesDeCargar);

        // Cargar la escena
        SceneManager.LoadScene(nombreEscena);
    }

    private IEnumerator SalirConDelay()
    {
        isLoading = true;

        // ============================================
        // REPRODUCIR SONIDO CLICK CON AUDIOMANAGER
        // ============================================
        if (AudioManager.Instance != null && AudioManager.Instance.sonidoClick != null)
        {
            AudioManager.Instance.PlayOneShot(audioSource, AudioManager.Instance.sonidoClick, volumenSonidos);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(AudioManager.Instance?.sonidoClick, volumenSonidos);
        }

        // Esperar el delay para que se escuche el sonido
        yield return new WaitForSeconds(delayAntesDeCargar);

        // Salir del juego
        Debug.Log("Saliendo del juego...");
        Application.Quit();

        // En el editor, detener la reproducción
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}