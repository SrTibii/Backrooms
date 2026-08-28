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

    [Header("Paneles")]
    public GameObject panelInicio;
    public GameObject panelCreditos;

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

        // Asegurar que el panel de inicio está visible y el de créditos no
        if (panelInicio != null) panelInicio.SetActive(true);
        if (panelCreditos != null) panelCreditos.SetActive(false);
    }

    // ============================================
    // MÉTODOS PARA EVENT TRIGGER (Pointer Enter)
    // ============================================

    public void SonidoHover()
    {
        if (audioSource == null || isLoading) return;

        if (AudioManager.Instance != null && AudioManager.Instance.sonidoHover != null)
        {
            AudioManager.Instance.PlayOneShot(audioSource, AudioManager.Instance.sonidoHover, volumenSonidos);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(AudioManager.Instance?.sonidoHover, volumenSonidos);
        }
    }

    // ============================================
    // MÉTODOS PARA NAVEGAR ENTRE PANELES
    // ============================================

    public void AbrirCreditos()
    {
        if (isLoading) return;

        // ?? OCULTAR TODOS LOS INDICADORES ANTES DE CAMBIAR DE PANEL
        OcultarTodosLosIndicadores();

        if (panelInicio != null) panelInicio.SetActive(false);
        if (panelCreditos != null) panelCreditos.SetActive(true);

        // Reproducir sonido de click
        if (AudioManager.Instance != null && AudioManager.Instance.sonidoClick != null)
        {
            AudioManager.Instance.PlayOneShot(audioSource, AudioManager.Instance.sonidoClick, volumenSonidos);
        }

        Debug.Log("?? Abriendo panel de créditos");
    }

    public void CerrarCreditos()
    {
        if (isLoading) return;

        // ?? OCULTAR TODOS LOS INDICADORES ANTES DE CAMBIAR DE PANEL
        OcultarTodosLosIndicadores();

        if (panelCreditos != null) panelCreditos.SetActive(false);
        if (panelInicio != null) panelInicio.SetActive(true);

        // Reproducir sonido de click
        if (AudioManager.Instance != null && AudioManager.Instance.sonidoClick != null)
        {
            AudioManager.Instance.PlayOneShot(audioSource, AudioManager.Instance.sonidoClick, volumenSonidos);
        }

        Debug.Log("?? Volviendo al panel de inicio");
    }

    // ============================================
    // ?? MÉTODO PARA OCULTAR TODOS LOS INDICADORES
    // ============================================

    private void OcultarTodosLosIndicadores()
    {
        // Buscar todos los BotonConIndicador en la escena
        BotonConIndicador[] botones = FindObjectsOfType<BotonConIndicador>(true);

        foreach (var boton in botones)
        {
            if (boton != null)
            {
                // Forzar la ocultación del indicador
                boton.ForzarOcultar();
            }
        }

        Debug.Log($"?? {botones.Length} indicadores ocultados");
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

        if (AudioManager.Instance != null && AudioManager.Instance.sonidoClick != null)
        {
            AudioManager.Instance.PlayOneShot(audioSource, AudioManager.Instance.sonidoClick, volumenSonidos);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(AudioManager.Instance?.sonidoClick, volumenSonidos);
        }

        yield return new WaitForSeconds(delayAntesDeCargar);
        SceneManager.LoadScene(nombreEscena);
    }

    private IEnumerator SalirConDelay()
    {
        isLoading = true;

        if (AudioManager.Instance != null && AudioManager.Instance.sonidoClick != null)
        {
            AudioManager.Instance.PlayOneShot(audioSource, AudioManager.Instance.sonidoClick, volumenSonidos);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(AudioManager.Instance?.sonidoClick, volumenSonidos);
        }

        yield return new WaitForSeconds(delayAntesDeCargar);

        Debug.Log("Saliendo del juego...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}