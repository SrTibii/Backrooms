using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using System.Collections;

public class VHSIntroScene : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Tiempo en segundos que dura la intro VHS")]
    public float duracionIntro = 5f;

    [Tooltip("Nombre de la escena a la que ir después de la intro")]
    public string nombreEscenaJuego = "Lvl0_Backrooms";

    [Tooltip("Si es true, espera a que el VideoPlayer termine de reproducir")]
    public bool esperarVideoPlayer = true;

    [Header("Input Actions")]
    public InputActionReference skipAction;

    [Header("Referencias")]
    public VideoPlayer videoPlayer;
    public RenderTexture renderTexture; // Arrastra el Render Texture aquí

    [Header("UI")]
    public GameObject rawImageObject; // La RawImage que muestra el video

    [Header("Debug")]
    public bool mostrarLogs = true;

    private bool introTerminada = false;
    private bool videoTerminado = false;
    private bool puedeSaltar = false;

    void Start()
    {
        // ============================================
        // OCULTAR CURSOR
        // ============================================
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Buscar VideoPlayer si no está asignado
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                videoPlayer = FindObjectOfType<VideoPlayer>();
            }
        }

        // Buscar Render Texture si no está asignado
        if (renderTexture == null && videoPlayer != null)
        {
            renderTexture = videoPlayer.targetTexture as RenderTexture;
        }

        // Configurar eventos del VideoPlayer
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.errorReceived += OnVideoError;
            if (mostrarLogs)
            {
                Debug.Log($"?? VideoPlayer encontrado: {videoPlayer.name}");
            }
        }

        // Iniciar la intro
        StartCoroutine(IntroRutina());

        if (mostrarLogs)
        {
            Debug.Log($"?? Intro VHS iniciada. Duración: {duracionIntro}s. Escena destino: {nombreEscenaJuego}");
        }
    }

    private void OnEnable()
    {
        if (skipAction != null)
        {
            skipAction.action.performed += OnSkipPerformed;
            skipAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (skipAction != null)
        {
            skipAction.action.performed -= OnSkipPerformed;
            skipAction.action.Disable();
        }
    }

    private void OnSkipPerformed(InputAction.CallbackContext context)
    {
        if (puedeSaltar && !introTerminada)
        {
            SaltarIntro();
        }
    }

    IEnumerator IntroRutina()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Play();
            if (mostrarLogs)
            {
                Debug.Log("?? Reproduciendo video...");
            }

            yield return new WaitForSeconds(0.3f);
            puedeSaltar = true;

            if (esperarVideoPlayer)
            {
                while (!videoTerminado)
                {
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        if (esperarVideoPlayer && videoPlayer != null && videoPlayer.isPlaying)
        {
            while (videoPlayer.isPlaying)
            {
                yield return null;
            }
        }

        float tiempoInicio = Time.time;
        while (Time.time - tiempoInicio < duracionIntro)
        {
            if (introTerminada) break;
            yield return null;
        }

        if (!introTerminada)
        {
            TerminarIntro();
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        videoTerminado = true;
        if (mostrarLogs)
        {
            Debug.Log("?? Video finalizado.");
        }

        if (duracionIntro <= 0f)
        {
            TerminarIntro();
        }
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError($"?? Error en VideoPlayer: {message}");
        TerminarIntro();
    }

    // ============================================
    // MÉTODO PARA TERMINAR LA INTRO (CON LIMPIEZA)
    // ============================================

    public void TerminarIntro()
    {
        if (introTerminada) return;

        introTerminada = true;
        puedeSaltar = false;

        if (mostrarLogs)
        {
            Debug.Log($"?? Intro terminada. Cargando escena: {nombreEscenaJuego}");
        }

        // ============================================
        // LIMPIAR VIDEOPLAYER, RENDER TEXTURE Y UI
        // ============================================
        LimpiarTodo();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SceneManager.LoadScene(nombreEscenaJuego);
    }

    // ============================================
    // MÉTODO PARA LIMPIAR TODO
    // ============================================

    private void LimpiarTodo()
    {
        // 1. Limpiar el Render Texture
        if (renderTexture != null)
        {
            renderTexture.Release(); // Liberar memoria
            if (mostrarLogs)
            {
                Debug.Log("?? Render Texture liberado");
            }
        }

        // 2. Ocultar la RawImage
        if (rawImageObject != null)
        {
            rawImageObject.SetActive(false);
            if (mostrarLogs)
            {
                Debug.Log("?? RawImage ocultada");
            }
        }

        // 3. Limpiar el VideoPlayer
        if (videoPlayer != null)
        {
            if (videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }

            // Limpiar la textura objetivo
            videoPlayer.targetTexture = null;

            videoPlayer.gameObject.SetActive(false);
            Destroy(videoPlayer);

            if (mostrarLogs)
            {
                Debug.Log("?? VideoPlayer limpiado");
            }
        }
    }

    public void SaltarIntro()
    {
        if (introTerminada) return;

        if (mostrarLogs)
        {
            Debug.Log("?? Intro saltada por el jugador");
        }

        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        TerminarIntro();
    }

    public void SetEscenaDestino(string nombreEscena)
    {
        nombreEscenaJuego = nombreEscena;
    }

    public void SetDuracion(float duracion)
    {
        duracionIntro = duracion;
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.errorReceived -= OnVideoError;
        }

        // Liberar Render Texture al destruir
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
    }
}