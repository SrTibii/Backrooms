using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;

public class VHSDeathTransition : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Nombre de la escena a la que ir después del video")]
    public string escenaDestino = "MenuInicial";

    [Tooltip("Tiempo extra de espera después del video (por si quieres delay)")]
    public float tiempoExtra = 0.5f;

    [Header("Referencias")]
    public VideoPlayer videoPlayer;

    [Header("Debug")]
    public bool mostrarLogs = true;

    private bool videoTerminado = false;
    private bool transicionCompletada = false;

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

        if (videoPlayer == null)
        {
            Debug.LogError("?? No se encontró VideoPlayer en la escena");
            return;
        }

        // Configurar eventos
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.isLooping = false;

        // Reproducir el video
        videoPlayer.Play();

        if (mostrarLogs)
        {
            Debug.Log($"?? Reproduciendo video de muerte VHS. Destino: {escenaDestino}");
        }
    }

    void Update()
    {
        // Si el video ha terminado y no hemos completado la transición
        if (videoTerminado && !transicionCompletada)
        {
            transicionCompletada = true;
            StartCoroutine(CargarEscenaConDelay());
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        videoTerminado = true;
        if (mostrarLogs)
        {
            Debug.Log("?? Video de muerte VHS terminado");
        }
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError($"?? Error en VideoPlayer: {message}");
        // Si hay error, cargar escena igualmente
        videoTerminado = true;
    }

    IEnumerator CargarEscenaConDelay()
    {
        if (mostrarLogs)
        {
            Debug.Log($"?? Esperando {tiempoExtra}s antes de cargar {escenaDestino}");
        }

        yield return new WaitForSeconds(tiempoExtra);

        if (mostrarLogs)
        {
            Debug.Log($"?? Cargando escena: {escenaDestino}");
        }

        // ============================================
        // RESTAURAR CURSOR (opcional)
        // ============================================
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(escenaDestino);
    }

    // ============================================
    // MÉTODO PARA SALTAR LA TRANSICIÓN (OPCIONAL)
    // ============================================

    public void SaltarTransicion()
    {
        if (!transicionCompletada)
        {
            if (mostrarLogs)
            {
                Debug.Log("?? Transición VHS saltada");
            }
            transicionCompletada = true;
            StopAllCoroutines();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            SceneManager.LoadScene(escenaDestino);
        }
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.errorReceived -= OnVideoError;
        }
    }
}