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
    public GameObject uiOpciones;

    [Header("Input Actions")]
    public InputActionReference pauseAction;

    [Header("Audio")]
    [Range(0f, 1f)] public float volumenSonidos = 0.8f;

    // ============================================
    // LISTA PARA GUARDAR AUDIOSOURCES DEL JUEGO
    // ============================================
    private List<AudioSource> audioSourcesJuego = new List<AudioSource>();
    private List<bool> estadosAudioSources = new List<bool>();
    private List<bool> estadosLoop = new List<bool>();

    private AudioSource audioSource;
    private bool isPaused = false;

    private FirstPersonController playerController;
    private VHSCameraEffects vhsEffects;
    private EnemyIA enemyIA;
    private LockerHideSystem lockerSystem;

    void Start()
    {
        playerController = FindObjectOfType<FirstPersonController>();
        vhsEffects = FindObjectOfType<VHSCameraEffects>();
        enemyIA = FindObjectOfType<EnemyIA>();
        lockerSystem = FindObjectOfType<LockerHideSystem>();

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

        if (uiIngame != null) uiIngame.SetActive(true);
        if (uiPausa != null) uiPausa.SetActive(false);
        if (uiOpciones != null) uiOpciones.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

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

        if (LockerHideSystem.IsPlayerHidingGlobal)
        {
            Debug.Log("?? No se puede pausar mientras estás escondido en la taquilla (Flag Global)");
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

        if (LockerHideSystem.IsPlayerHidingGlobal)
        {
            Debug.Log("?? No se puede pausar mientras estás escondido en la taquilla (Flag Global)");
            return;
        }

        isPaused = true;

        if (uiIngame != null) uiIngame.SetActive(false);
        if (uiPausa != null) uiPausa.SetActive(true);
        if (uiOpciones != null) uiOpciones.SetActive(false);

        // ============================================
        // OCULTAR INDICADORES AL ABRIR PAUSA
        // ============================================
        BotonConIndicadorPausa.OcultarTodosLosIndicadores();

        Time.timeScale = 0f;

        if (playerController != null)
        {
            playerController.enabled = false;
        }

        if (vhsEffects != null)
        {
            vhsEffects.enabled = false;
        }

        DesactivarAudioSources();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("?? Juego PAUSADO - Todo congelado");
    }

    public void ReanudarJuego()
    {
        if (!isPaused) return;

        isPaused = false;

        if (uiIngame != null)
        {
            uiIngame.SetActive(true);
            Transform crosshair = uiIngame.transform.Find("Crosshair");
            if (crosshair != null) crosshair.gameObject.SetActive(true);
        }
        if (uiPausa != null) uiPausa.SetActive(false);
        if (uiOpciones != null) uiOpciones.SetActive(false);

        // ============================================
        // OCULTAR INDICADORES AL REANUDAR
        // ============================================
        BotonConIndicadorPausa.OcultarTodosLosIndicadores();

        Time.timeScale = 1f;

        if (playerController != null)
        {
            playerController.enabled = true;
        }

        if (vhsEffects != null)
        {
            vhsEffects.enabled = true;
        }

        ReactivarAudioSources();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("?? Juego REANUDADO - Todo descongelado");
    }

    // ============================================
    // MÉTODOS PARA NAVEGAR ENTRE MENÚS
    // ============================================

    public void AbrirOpciones()
    {
        // ============================================
        // REPRODUCIR SONIDO CON AUDIOMANAGER
        // ============================================
        if (AudioManager.Instance != null && AudioManager.Instance.sonidoClick != null)
        {
            AudioManager.Instance.PlayOneShot(audioSource, AudioManager.Instance.sonidoClick, volumenSonidos);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(AudioManager.Instance?.sonidoClick, volumenSonidos);
        }

        BotonConIndicadorPausa.OcultarTodosLosIndicadores();

        if (uiPausa != null) uiPausa.SetActive(false);
        if (uiOpciones != null) uiOpciones.SetActive(true);
    }

    public void CerrarOpciones()
    {
        // ============================================
        // REPRODUCIR SONIDO CON AUDIOMANAGER
        // ============================================
        if (AudioManager.Instance != null && AudioManager.Instance.sonidoClick != null)
        {
            AudioManager.Instance.PlayOneShot(audioSource, AudioManager.Instance.sonidoClick, volumenSonidos);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(AudioManager.Instance?.sonidoClick, volumenSonidos);
        }

        BotonConIndicadorPausa.OcultarTodosLosIndicadores();

        if (uiOpciones != null) uiOpciones.SetActive(false);
        if (uiPausa != null) uiPausa.SetActive(true);
    }

    // ============================================
    // MÉTODOS PARA BOTONES
    // ============================================

    public void BotonReanudar()
    {
        // ============================================
        // REPRODUCIR SONIDO CON AUDIOMANAGER
        // ============================================
        if (AudioManager.Instance != null && AudioManager.Instance.sonidoClick != null)
        {
            AudioManager.Instance.PlayOneShot(audioSource, AudioManager.Instance.sonidoClick, volumenSonidos);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(AudioManager.Instance?.sonidoClick, volumenSonidos);
        }

        StartCoroutine(ReanudarConDelay());
    }

    private IEnumerator ReanudarConDelay()
    {
        yield return new WaitForEndOfFrame();
        ReanudarJuego();
    }

    public void BotonSalir()
    {
        // ============================================
        // REPRODUCIR SONIDO CON AUDIOMANAGER
        // ============================================
        if (AudioManager.Instance != null && AudioManager.Instance.sonidoClick != null)
        {
            AudioManager.Instance.PlayOneShot(audioSource, AudioManager.Instance.sonidoClick, volumenSonidos);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(AudioManager.Instance?.sonidoClick, volumenSonidos);
        }

        Time.timeScale = 1f;
        ReactivarAudioSources();

        if (playerController != null) playerController.enabled = true;
        if (vhsEffects != null) vhsEffects.enabled = true;

        BotonConIndicadorPausa.OcultarTodosLosIndicadores();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("MenuInicial");
    }

    // ============================================
    // MÉTODOS PARA CONTROLAR AUDIOSOURCES
    // ============================================

    private void DesactivarAudioSources()
    {
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        audioSourcesJuego.Clear();
        estadosAudioSources.Clear();
        estadosLoop.Clear();

        foreach (var src in allAudioSources)
        {
            if (src != audioSource)
            {
                audioSourcesJuego.Add(src);
                estadosAudioSources.Add(src.isPlaying);
                estadosLoop.Add(src.loop);

                if (src.isPlaying)
                {
                    src.Stop();
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
                audioSourcesJuego[i].enabled = true;
                audioSourcesJuego[i].loop = estadosLoop[i];

                if (estadosAudioSources[i])
                {
                    audioSourcesJuego[i].Play();
                }
            }
        }

        Debug.Log($"?? {audioSourcesJuego.Count} AudioSources reactivados");
    }

    // ============================================
    // MÉTODOS PARA EVENT TRIGGER (Pointer Enter)
    // ============================================

    public void SonidoHover()
    {
        // ============================================
        // REPRODUCIR SONIDO HOVER CON AUDIOMANAGER
        // ============================================
        if (AudioManager.Instance != null && AudioManager.Instance.sonidoHover != null)
        {
            AudioManager.Instance.PlayOneShot(audioSource, AudioManager.Instance.sonidoHover, volumenSonidos);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(AudioManager.Instance?.sonidoHover, volumenSonidos);
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