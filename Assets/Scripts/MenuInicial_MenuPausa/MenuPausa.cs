using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class MenuPausa : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject uiIngame;
    public GameObject uiPausa;

    [Header("Input Actions")]
    public InputActionReference pauseAction;

    [Header("Audio")]
    public AudioClip sonidoClick;
    public AudioClip sonidoHover;
    [Range(0f, 1f)] public float volumenSonidos = 0.8f;

    // ============================================
    // LISTA PARA GUARDAR AUDIOSOURCES DEL JUEGO
    // ============================================
    private List<AudioSource> audioSourcesJuego = new List<AudioSource>();
    private List<bool> estadosAudioSources = new List<bool>();
    private List<bool> estadosLoop = new List<bool>();

    private AudioSource audioSource;
    private bool isPaused = false;

    // ============================================
    // REFERENCIAS PARA CONGELAR LA CÁMARA
    // ============================================
    private FirstPersonController playerController;
    private VHSCameraEffects vhsEffects;

    // ============================================
    // REFERENCIA AL ENEMIGO PARA SABER SI HAY JUMPSCARE
    // ============================================
    private EnemyIA enemyIA;

    void Start()
    {
        // Buscar referencias
        playerController = FindObjectOfType<FirstPersonController>();
        vhsEffects = FindObjectOfType<VHSCameraEffects>();
        enemyIA = FindObjectOfType<EnemyIA>();

        // Crear AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = volumenSonidos;

        // Asegurar UI correcta
        if (uiIngame != null) uiIngame.SetActive(true);
        if (uiPausa != null) uiPausa.SetActive(false);

        // Asegurar que el juego no está pausado al inicio
        Time.timeScale = 1f;
        isPaused = false;

        // Asegurar cursor bloqueado
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("?? Cursor bloqueado al inicio");

        Debug.Log("?? Menú de Pausa inicializado");
    }

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed += OnPausePerformed;
            pauseAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePerformed;
            pauseAction.action.Disable();
        }
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (enemyIA != null && enemyIA.EstaEnJumpscare())
        {
            Debug.Log("?? No se puede pausar durante un jumpscare");
            return;
        }

        if (!isPaused)
        {
            PausarJuego();
        }
    }

    // ============================================
    // MÉTODOS DE PAUSA
    // ============================================

    public void PausarJuego()
    {
        if (isPaused) return;
        if (enemyIA != null && enemyIA.EstaEnJumpscare()) return;

        isPaused = true;

        // Ocultar UI del juego, mostrar pausa
        if (uiIngame != null) uiIngame.SetActive(false);
        if (uiPausa != null) uiPausa.SetActive(true);

        // Congelar el juego
        Time.timeScale = 0f;

        // Desactivar scripts de movimiento y cámara
        if (playerController != null)
        {
            playerController.enabled = false;
            Debug.Log("?? FirstPersonController desactivado");
        }

        if (vhsEffects != null)
        {
            vhsEffects.enabled = false;
            Debug.Log("?? VHSCameraEffects desactivado");
        }

        // ============================================
        // DESACTIVAR TODOS LOS AUDIOSOURCES DEL JUEGO
        // ============================================
        DesactivarAudioSources();

        // Desbloquear cursor para el menú
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("?? Cursor desbloqueado para pausa");

        Debug.Log("?? Juego PAUSADO - Todo congelado");
    }

    public void ReanudarJuego()
    {
        if (!isPaused) return;

        isPaused = false;

        // Mostrar UI del juego, ocultar pausa
        if (uiIngame != null) uiIngame.SetActive(true);
        if (uiPausa != null) uiPausa.SetActive(false);

        // Reanudar el juego
        Time.timeScale = 1f;

        // Reactivar scripts de movimiento y cámara
        if (playerController != null)
        {
            playerController.enabled = true;
            Debug.Log("?? FirstPersonController reactivado");
        }

        if (vhsEffects != null)
        {
            vhsEffects.enabled = true;
            Debug.Log("?? VHSCameraEffects reactivado");
        }

        // ============================================
        // REACTIVAR TODOS LOS AUDIOSOURCES DEL JUEGO
        // ============================================
        ReactivarAudioSources();

        // Bloquear cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("?? Cursor bloqueado al reanudar");

        Debug.Log("?? Juego REANUDADO - Todo descongelado");
    }

    // ============================================
    // MÉTODOS PARA CONTROLAR AUDIOSOURCES
    // ============================================

    private void DesactivarAudioSources()
    {
        // Buscar TODOS los AudioSources
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        audioSourcesJuego.Clear();
        estadosAudioSources.Clear();
        estadosLoop.Clear();

        foreach (var src in allAudioSources)
        {
            // Excluir el AudioSource del menú de pausa
            if (src != audioSource)
            {
                audioSourcesJuego.Add(src);
                estadosAudioSources.Add(src.isPlaying);
                estadosLoop.Add(src.loop);

                // DESACTIVAR el AudioSource completamente
                if (src.isPlaying)
                {
                    src.Stop(); // Para detener bucles
                }
                src.enabled = false;
            }
        }

        Debug.Log($"?? {audioSourcesJuego.Count} AudioSources desactivados");
    }

    private void ReactivarAudioSources()
    {
        for (int i = 0; i < audioSourcesJuego.Count; i++)
        {
            if (audioSourcesJuego[i] != null)
            {
                // Reactivar el AudioSource
                audioSourcesJuego[i].enabled = true;
                audioSourcesJuego[i].loop = estadosLoop[i];

                // Reanudar solo si estaba sonando antes
                if (estadosAudioSources[i])
                {
                    audioSourcesJuego[i].Play();
                }
            }
        }

        Debug.Log($"?? {audioSourcesJuego.Count} AudioSources reactivados");
    }

    // ============================================
    // MÉTODOS PARA BOTONES
    // ============================================

    public void BotonReanudar()
    {
        ReproducirSonido(sonidoClick);
        StartCoroutine(ReanudarConDelay());
    }

    private IEnumerator ReanudarConDelay()
    {
        yield return new WaitForEndOfFrame();
        ReanudarJuego();
    }

    public void BotonSalir()
    {
        ReproducirSonido(sonidoClick);

        Time.timeScale = 1f;
        ReactivarAudioSources();

        if (playerController != null) playerController.enabled = true;
        if (vhsEffects != null) vhsEffects.enabled = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("MenuInicial");
    }

    public void BotonOpciones()
    {
        ReproducirSonido(sonidoClick);
        Debug.Log("?? Abrir menú de opciones (pendiente de implementar)");
    }

    public void SonidoHover()
    {
        if (audioSource != null && sonidoHover != null)
        {
            audioSource.PlayOneShot(sonidoHover, volumenSonidos);
        }
    }

    private void ReproducirSonido(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, volumenSonidos);
        }
    }

    public bool EstaPausado()
    {
        return isPaused;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;

        if (playerController != null) playerController.enabled = true;
        if (vhsEffects != null) vhsEffects.enabled = true;

        ReactivarAudioSources();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}