using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuInicial : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip sonidoClick;
    public AudioClip sonidoHover;
    [Range(0f, 1f)] public float volumenSonidos = 0.8f;

    [Header("Delay")]
    [Tooltip("Tiempo de espera antes de cargar la escena después del click")]
    public float delayAntesDeCargar = 0.3f;

    private AudioSource audioSource;
    private bool isLoading = false;

    void Start()
    {
        // Crear AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = volumenSonidos;
    }

    // ============================================
    // MÉTODOS PARA EVENT TRIGGER (Pointer Enter)
    // ============================================

    public void SonidoHover()
    {
        if (audioSource != null && sonidoHover != null && !isLoading)
        {
            audioSource.PlayOneShot(sonidoHover, volumenSonidos);
        }
    }

    // ============================================
    // MÉTODOS PARA BOTONES (OnClick)
    // ============================================

    public void Jugar()
    {
        if (isLoading) return;
        StartCoroutine(CargarConDelay("VHSPlay")); //Lvl0_Backrooms
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

        // Reproducir sonido de click
        if (audioSource != null && sonidoClick != null)
        {
            audioSource.PlayOneShot(sonidoClick, volumenSonidos);
        }

        // Esperar el delay para que se escuche el sonido
        yield return new WaitForSeconds(delayAntesDeCargar);

        // Cargar la escena
        SceneManager.LoadScene(nombreEscena);
    }

    private IEnumerator SalirConDelay()
    {
        isLoading = true;

        // Reproducir sonido de click
        if (audioSource != null && sonidoClick != null)
        {
            audioSource.PlayOneShot(sonidoClick, volumenSonidos);
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