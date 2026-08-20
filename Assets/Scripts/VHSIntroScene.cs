using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
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

    [Header("Referencias")]
    public VideoPlayer videoPlayer;

    [Header("Debug")]
    public bool mostrarLogs = true;

    private bool introTerminada = false;
    private bool videoTerminado = false;

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

    // ============================================
    // CORRUTINA PRINCIPAL
    // ============================================

    IEnumerator IntroRutina()
    {
        // Reproducir el video si existe
        if (videoPlayer != null)
        {
            videoPlayer.Play();
            if (mostrarLogs)
            {
                Debug.Log("?? Reproduciendo video...");
            }

            if (esperarVideoPlayer)
            {
                // Esperar a que el video termine
                while (!videoTerminado)
                {
                    yield return null;
                }
            }
            else
            {
                // Esperar un pequeño delay antes de continuar
                yield return new WaitForSeconds(0.5f);
            }
        }

        // Esperar el tiempo restante (si el video fue más corto)
        if (esperarVideoPlayer && videoPlayer != null && videoPlayer.isPlaying)
        {
            // Si el video sigue sonando, esperamos a que termine
            while (videoPlayer.isPlaying)
            {
                yield return null;
            }
        }

        // Esperar el tiempo total de la intro (por si el video es más corto)
        float tiempoInicio = Time.time;
        while (Time.time - tiempoInicio < duracionIntro)
        {
            // Si la intro ya fue marcada como terminada, salir
            if (introTerminada) break;
            yield return null;
        }

        // Si no se ha terminado antes, forzar el fin
        if (!introTerminada)
        {
            TerminarIntro();
        }
    }

    // ============================================
    // EVENTOS DEL VIDEOPLAYER
    // ============================================

    private void OnVideoFinished(VideoPlayer vp)
    {
        videoTerminado = true;
        if (mostrarLogs)
        {
            Debug.Log("?? Video finalizado.");
        }

        // Si no estamos esperando el tiempo total, terminar la intro
        if (duracionIntro <= 0f)
        {
            TerminarIntro();
        }
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError($"?? Error en VideoPlayer: {message}");
        // Si hay error, terminar la intro igualmente
        TerminarIntro();
    }

    // ============================================
    // MÉTODO PARA TERMINAR LA INTRO
    // ============================================

    public void TerminarIntro()
    {
        if (introTerminada) return;

        introTerminada = true;
        if (mostrarLogs)
        {
            Debug.Log($"?? Intro terminada. Cargando escena: {nombreEscenaJuego}");
        }

        // ============================================
        // RESTAURAR CURSOR (opcional, se bloqueará en el juego)
        // ============================================
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Cargar la escena del juego
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    // ============================================
    // MÉTODO PARA SALTAR LA INTRO (OPCIONAL)
    // ============================================

    public void SaltarIntro()
    {
        if (mostrarLogs)
        {
            Debug.Log("?? Intro saltada por el jugador");
        }
        TerminarIntro();
    }

    // ============================================
    // MÉTODO PARA CAMBIAR LA ESCENA DESTINO (OPCIONAL)
    // ============================================

    public void SetEscenaDestino(string nombreEscena)
    {
        nombreEscenaJuego = nombreEscena;
    }

    public void SetDuracion(float duracion)
    {
        duracionIntro = duracion;
    }

    // ============================================
    // LIMPIEZA
    // ============================================

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.errorReceived -= OnVideoError;
        }
    }
}