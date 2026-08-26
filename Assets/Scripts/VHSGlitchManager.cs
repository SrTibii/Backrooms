using UnityEngine;
using System.Collections;

public class VHSGlitchManager : MonoBehaviour
{
    [Header("Referencias")]
    public Camera vhsCamera;
    public FirstPersonController playerController;

    [Header("Glitch de Tracking")]
    public bool enableTrackingGlitch = true;
    public float glitchIntervalMin = 3f;
    public float glitchIntervalMax = 8f;
    public float glitchDurationMin = 0.05f;
    public float glitchDurationMax = 0.15f;
    public float glitchIntensityMin = 0.5f;
    public float glitchIntensityMax = 1.5f;

    [Header("Efectos de Glitch")]
    public float positionGlitchAmount = 0.3f;
    public float rotationGlitchAmount = 2f;

    [Header("Volumen del Glitch")]
    [Range(0f, 1f)]
    public float glitchSoundVolume = 0.8f; // ?? SLIDER DE VOLUMEN

    private Vector3 cameraOriginalPosition;
    private Quaternion cameraOriginalRotation;
    private float nextGlitchTime = 0f;
    private bool isGlitching = false;
    private bool isPaused = false;

    void Start()
    {
        if (playerController == null)
            playerController = GetComponent<FirstPersonController>();

        if (vhsCamera == null && playerController != null)
            vhsCamera = playerController.GetComponentInChildren<Camera>();

        if (vhsCamera != null)
        {
            cameraOriginalPosition = vhsCamera.transform.localPosition;
            cameraOriginalRotation = vhsCamera.transform.localRotation;
        }

        ScheduleNextGlitch();
    }

    void Update()
    {
        if (isPaused) return;
        if (!enableTrackingGlitch) return;
        if (vhsCamera == null) return;

        if (playerController != null && playerController.IsZoomed()) return;

        if (Time.time >= nextGlitchTime && !isGlitching)
        {
            StartCoroutine(TriggerGlitch());
        }
    }

    IEnumerator TriggerGlitch()
    {
        isGlitching = true;

        float duration = Random.Range(glitchDurationMin, glitchDurationMax);
        float intensity = Random.Range(glitchIntensityMin, glitchIntensityMax);

        // ============================================
        // ?? REPRODUCIR SONIDO DE GLITCH CON AUDIOMANAGER
        // ============================================
        if (AudioManager.Instance != null && AudioManager.Instance.glitchSound != null)
        {
            AudioManager.Instance.PlayOneShotAtPosition(
                AudioManager.Instance.glitchSound,
                transform.position,
                glitchSoundVolume, // ?? AHORA USA EL SLIDER
                8f
            );
            Debug.Log($"?? Sonido de glitch reproducido (volumen: {glitchSoundVolume})");
        }
        else
        {
            Debug.LogWarning("?? AudioManager o glitchSound no disponible");
        }

        int glitchFrames = Mathf.RoundToInt(duration / Time.deltaTime);
        glitchFrames = Mathf.Clamp(glitchFrames, 2, 10);

        for (int i = 0; i < glitchFrames; i++)
        {
            Vector3 posOffset = new Vector3(
                Random.Range(-positionGlitchAmount, positionGlitchAmount) * intensity,
                Random.Range(-positionGlitchAmount * 0.5f, positionGlitchAmount * 0.5f) * intensity,
                0f
            );

            Vector3 rotOffset = new Vector3(
                0f,
                0f,
                Random.Range(-rotationGlitchAmount, rotationGlitchAmount) * intensity
            );

            vhsCamera.transform.localPosition = cameraOriginalPosition + posOffset;
            vhsCamera.transform.localEulerAngles = cameraOriginalRotation.eulerAngles + rotOffset;

            yield return new WaitForSeconds(Time.deltaTime);
        }

        vhsCamera.transform.localPosition = cameraOriginalPosition;
        vhsCamera.transform.localRotation = cameraOriginalRotation;

        isGlitching = false;
        ScheduleNextGlitch();
    }

    void ScheduleNextGlitch()
    {
        nextGlitchTime = Time.time + Random.Range(glitchIntervalMin, glitchIntervalMax);
    }

    public void ForceGlitch(float intensity = 1f)
    {
        if (!isGlitching)
        {
            StartCoroutine(TriggerGlitch());
        }
    }

    public void ResetCamera()
    {
        if (vhsCamera != null)
        {
            vhsCamera.transform.localPosition = cameraOriginalPosition;
            vhsCamera.transform.localRotation = cameraOriginalRotation;
        }
    }

    public void PausarGlitches()
    {
        isPaused = true;
        if (vhsCamera != null)
        {
            vhsCamera.transform.localPosition = cameraOriginalPosition;
            vhsCamera.transform.localRotation = cameraOriginalRotation;
        }
        if (isGlitching)
        {
            StopAllCoroutines();
            isGlitching = false;
        }
    }

    public void ReanudarGlitches()
    {
        isPaused = false;
        if (vhsCamera != null)
        {
            vhsCamera.transform.localPosition = cameraOriginalPosition;
            vhsCamera.transform.localRotation = cameraOriginalRotation;
        }
        ScheduleNextGlitch();
    }
}