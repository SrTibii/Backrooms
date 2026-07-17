using UnityEngine;
using System.Collections;

/// <summary>
/// Gestiona los glitches de la cámara VHS (fallos de tracking, estática, etc.)
/// Este script va en el Player (mismo que FirstPersonController)
/// </summary>
public class VHSGlitchManager : MonoBehaviour
{
    [Header("Referencias")]
    public Camera vhsCamera; // Arrastrar la Main Camera
    public FirstPersonController playerController; // Arrastrar el Player (o se busca solo)

    [Header("Glitch de Tracking")]
    public bool enableTrackingGlitch = true;
    public float glitchIntervalMin = 3f;           // Tiempo mínimo entre glitches
    public float glitchIntervalMax = 8f;           // Tiempo máximo entre glitches
    public float glitchDurationMin = 0.05f;        // Duración mínima del glitch
    public float glitchDurationMax = 0.15f;        // Duración máxima del glitch
    public float glitchIntensityMin = 0.5f;        // Intensidad mínima del glitch
    public float glitchIntensityMax = 1.5f;        // Intensidad máxima del glitch

    [Header("Efectos de Glitch")]
    public float positionGlitchAmount = 0.3f;      // Desplazamiento de posición durante glitch
    public float rotationGlitchAmount = 2f;        // Rotación durante glitch

    [Header("Audio")]
    public AudioClip glitchSound;                  // Sonido de glitch (opcional)
    public float glitchSoundVolume = 0.3f;

    // Variables internas
    private Vector3 cameraOriginalPosition;
    private Quaternion cameraOriginalRotation;
    private float nextGlitchTime = 0f;
    private bool isGlitching = false;
    private AudioSource glitchAudioSource;

    // ?? NUEVO: Control de pausa para cuando se leen notas
    private bool isPaused = false;

    void Start()
    {
        // Buscar referencias si no se asignaron
        if (playerController == null)
        {
            playerController = GetComponent<FirstPersonController>();
        }

        if (vhsCamera == null && playerController != null)
        {
            vhsCamera = playerController.GetComponentInChildren<Camera>();
        }

        if (vhsCamera != null)
        {
            cameraOriginalPosition = vhsCamera.transform.localPosition;
            cameraOriginalRotation = vhsCamera.transform.localRotation;
        }

        // Crear AudioSource para glitch
        glitchAudioSource = gameObject.AddComponent<AudioSource>();
        glitchAudioSource.loop = false;
        glitchAudioSource.playOnAwake = false;
        glitchAudioSource.spatialBlend = 0f;
        glitchAudioSource.volume = glitchSoundVolume;

        // Programar el primer glitch
        ScheduleNextGlitch();
    }

    void Update()
    {
        // ?? Si está en pausa, NO ejecutar glitches
        if (isPaused) return;

        if (!enableTrackingGlitch) return;
        if (vhsCamera == null) return;

        // Si estamos en zoom, reducir la frecuencia de glitches
        if (playerController != null && playerController.IsZoomed())
        {
            return;
        }

        // Comprobar si toca glitch
        if (Time.time >= nextGlitchTime && !isGlitching)
        {
            StartCoroutine(TriggerGlitch());
        }
    }

    /// <summary>
    /// Dispara un glitch de tracking
    /// </summary>
    IEnumerator TriggerGlitch()
    {
        isGlitching = true;

        // Calcular duración e intensidad
        float duration = Random.Range(glitchDurationMin, glitchDurationMax);
        float intensity = Random.Range(glitchIntensityMin, glitchIntensityMax);

        // Reproducir sonido de glitch
        if (glitchSound != null)
        {
            glitchAudioSource.PlayOneShot(glitchSound);
        }

        // Aplicar glitch en varios frames (para efecto de "tracking perdido")
        int glitchFrames = Mathf.RoundToInt(duration / Time.deltaTime);
        glitchFrames = Mathf.Clamp(glitchFrames, 2, 10);

        for (int i = 0; i < glitchFrames; i++)
        {
            // Desplazamiento aleatorio de posición
            Vector3 posOffset = new Vector3(
                Random.Range(-positionGlitchAmount, positionGlitchAmount) * intensity,
                Random.Range(-positionGlitchAmount * 0.5f, positionGlitchAmount * 0.5f) * intensity,
                0f
            );

            // Rotación aleatoria
            Vector3 rotOffset = new Vector3(
                0f,
                0f,
                Random.Range(-rotationGlitchAmount, rotationGlitchAmount) * intensity
            );

            // Aplicar glitch
            vhsCamera.transform.localPosition = cameraOriginalPosition + posOffset;
            vhsCamera.transform.localEulerAngles = cameraOriginalRotation.eulerAngles + rotOffset;

            // Esperar al siguiente frame
            yield return new WaitForSeconds(Time.deltaTime);
        }

        // Restaurar posición original
        vhsCamera.transform.localPosition = cameraOriginalPosition;
        vhsCamera.transform.localRotation = cameraOriginalRotation;

        isGlitching = false;

        // Programar el siguiente glitch
        ScheduleNextGlitch();
    }

    /// <summary>
    /// Programa el siguiente glitch
    /// </summary>
    void ScheduleNextGlitch()
    {
        nextGlitchTime = Time.time + Random.Range(glitchIntervalMin, glitchIntervalMax);
    }

    /// <summary>
    /// Fuerza un glitch inmediato (para eventos o triggers)
    /// </summary>
    public void ForceGlitch(float intensity = 1f)
    {
        if (!isGlitching)
        {
            StartCoroutine(TriggerGlitch());
        }
    }

    /// <summary>
    /// Reinicia la posición de la cámara (útil para pausas)
    /// </summary>
    public void ResetCamera()
    {
        if (vhsCamera != null)
        {
            vhsCamera.transform.localPosition = cameraOriginalPosition;
            vhsCamera.transform.localRotation = cameraOriginalRotation;
        }
    }

    // ?? NUEVO: Pausar los glitches (cuando se abre una nota)
    public void PausarGlitches()
    {
        isPaused = true;

        // Restaurar cámara a la posición original inmediatamente
        if (vhsCamera != null)
        {
            vhsCamera.transform.localPosition = cameraOriginalPosition;
            vhsCamera.transform.localRotation = cameraOriginalRotation;
        }

        // Detener cualquier glitch en curso
        if (isGlitching)
        {
            StopAllCoroutines();
            isGlitching = false;
        }

        Debug.Log("?? Glitches de VHS pausados");
    }

    // ?? NUEVO: Reanudar los glitches (cuando se cierra la nota)
    public void ReanudarGlitches()
    {
        isPaused = false;

        // Restaurar cámara a la posición original
        if (vhsCamera != null)
        {
            vhsCamera.transform.localPosition = cameraOriginalPosition;
            vhsCamera.transform.localRotation = cameraOriginalRotation;
        }

        // Programar el siguiente glitch
        ScheduleNextGlitch();

        Debug.Log("?? Glitches de VHS reanudados");
    }
}