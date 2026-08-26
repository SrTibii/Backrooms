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
    private List<float> volumenesOriginales = new List<float>();

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

        Debug.Log("? Menú de Pausa inicializado");
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
            Debug.Log("?? No se puede pausar mientras estás escondido en la taquilla");
            return;
        }

        if (!isPaused)
        {
            PausarJuego();
        }
    }

    public void PausarJuego()
    {
        if (isPaused) return;

        if (enemyIA != null && enemyIA.EstaEnJumpscare()) return;

        if (LockerHideSystem.IsPlayerHidingGlobal)
        {
            Debug.Log("?? No se puede pausar mientras estás escondido en la taquilla");
            return;
        }

        isPaused = true;

        if (uiIngame != null) uiIngame.SetActive(false);
        if (uiPausa != null) uiPausa.SetActive(true);
        if (uiOpciones != null) uiOpciones.SetActive(false);

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

    public void AbrirOpciones()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.sonidoClick != null)
        {
            AudioManager.Instance.PlayOneShot(audioSource, AudioManager.Instance.sonidoClick, volumenSonidos);
        }

        BotonConIndicadorPausa.OcultarTodosLosIndicadores();

        if (uiPausa != null) uiPausa.SetActive(false);
        if (uiOpciones != null) uiOpciones.SetActive(true);
    }

    public void CerrarOpciones()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.sonidoClick != null)
        {
            AudioManager.Instance.PlayOneShot(audioSource, AudioManager.Instance.sonidoClick, volumenSonidos);
        }

        BotonConIndicadorPausa.OcultarTodosLosIndicadores();

        if (uiOpciones != null) uiOpciones.SetActive(false);
        if (uiPausa != null) uiPausa.SetActive(true);
    }

    public void BotonReanudar()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.sonidoClick != null)
        {
            AudioManager.Instance.PlayOneShot(audioSource, AudioManager.Instance.sonidoClick, volumenSonidos);
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
        if (AudioManager.Instance != null && AudioManager.Instance.sonidoClick != null)
        {
            AudioManager.Instance.PlayOneShot(audioSource, AudioManager.Instance.sonidoClick, volumenSonidos);
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
    // ?? MODIFICADO: Guardar volumen original
    // ============================================
    private void DesactivarAudioSources()
    {
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        audioSourcesJuego.Clear();
        estadosAudioSources.Clear();
        estadosLoop.Clear();
        volumenesOriginales.Clear();

        foreach (var src in allAudioSources)
        {
            if (src != audioSource)
            {
                audioSourcesJuego.Add(src);
                estadosAudioSources.Add(src.isPlaying);
                estadosLoop.Add(src.loop);
                volumenesOriginales.Add(src.volume);

                if (src.isPlaying)
                {
                    src.Stop();
                }
                src.enabled = false;
            }
        }

        Debug.Log($"?? {audioSourcesJuego.Count} AudioSources desactivados");
    }

    // ============================================
    // ?? MODIFICADO: Respetar el volumen global al reanudar
    // ============================================
    private void ReactivarAudioSources()
    {
        float volumenGlobal = AudioManager.Instance != null ? AudioManager.Instance.GetVolumenGlobal() : 1f;
        bool volumenCero = volumenGlobal <= 0.001f;

        for (int i = 0; i < audioSourcesJuego.Count; i++)
        {
            if (audioSourcesJuego[i] != null)
            {
                audioSourcesJuego[i].enabled = true;
                audioSourcesJuego[i].loop = estadosLoop[i];

                if (volumenCero)
                {
                    audioSourcesJuego[i].volume = 0f;
                    if (audioSourcesJuego[i].isPlaying)
                        audioSourcesJuego[i].Stop();
                }
                else if (i < volumenesOriginales.Count)
                {
                    float volOriginal = volumenesOriginales[i];
                    audioSourcesJuego[i].volume = volOriginal * volumenGlobal;

                    if (estadosAudioSources[i] && audioSourcesJuego[i].clip != null)
                    {
                        audioSourcesJuego[i].Play();
                    }
                }
            }
        }

        Debug.Log($"?? {audioSourcesJuego.Count} AudioSources reactivados (volumen global: {volumenGlobal})");
    }

    public void SonidoHover()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.sonidoHover != null)
        {
            AudioManager.Instance.PlayOneShot(audioSource, AudioManager.Instance.sonidoHover, volumenSonidos);
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