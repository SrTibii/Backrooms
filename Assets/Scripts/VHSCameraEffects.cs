using UnityEngine;

/// <summary>
/// Efectos de cámara VHS: Micro-temblor (saccades) y Gate Weave (bailoteo de cinta)
/// Este script va en la Main Camera (hijo del Player)
/// </summary>
public class VHSCameraEffects : MonoBehaviour
{
    [Header("Referencias")]
    public FirstPersonController playerController; // Arrastrar el Player

    [Header("Micro-Saccades (Temblor de mano)")]
    public bool enableMicroSaccades = true;
    public float saccadeAmplitude = 0.03f;
    public float saccadeSpeed = 30f;
    public float saccadeSmoothness = 8f;

    [Header("Gate Weave (Bailoteo de cinta)")]
    public bool enableGateWeave = true;
    public float weaveAmplitude = 0.02f;
    public float weaveSpeed = 0.8f;
    public float weaveSmoothness = 4f;

    [Header("Intensidad por Estado")]
    public float idleMultiplier = 0.3f;
    public float walkMultiplier = 0.6f;
    public float sprintMultiplier = 1.2f;
    public float crouchMultiplier = 0.4f;

    // Variables internas
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;

    private float saccadeX;
    private float saccadeY;
    private float saccadeZ;

    private float weaveX;
    private float weaveY;

    private float targetSaccadeX;
    private float targetSaccadeY;
    private float targetSaccadeZ;

    private Vector3 targetWeavePosition;

    // ============================================
    // REFERENCIA AL MENÚ DE PAUSA
    // ============================================
    private MenuPausa menuPausa;

    void Start()
    {
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;

        if (playerController == null)
        {
            playerController = GetComponentInParent<FirstPersonController>();
            if (playerController == null)
            {
                Debug.LogWarning("VHSCameraEffects: No se encontró FirstPersonController en el padre.");
            }
        }

        // Buscar el menú de pausa
        menuPausa = FindObjectOfType<MenuPausa>();
        if (menuPausa == null)
        {
            Debug.LogWarning("VHSCameraEffects: No se encontró MenuPausa en la escena.");
        }
    }

    void Update()
    {
        // ============================================
        // SI EL JUEGO ESTÁ PAUSADO, NO EJECUTAR EFECTOS
        // ============================================
        if (menuPausa != null && menuPausa.EstaPausado())
        {
            // Restaurar posición y rotación a la inicial cuando está pausado
            transform.localPosition = initialLocalPosition;
            transform.localRotation = initialLocalRotation;
            return;
        }

        if (playerController == null) return;

        float intensity = CalculateIntensity();

        if (enableMicroSaccades)
        {
            ApplyMicroSaccades(intensity);
        }

        if (enableGateWeave)
        {
            ApplyGateWeave(intensity);
        }
    }

    float CalculateIntensity()
    {
        float intensity = idleMultiplier;

        if (playerController.IsMovingFast())
        {
            intensity = sprintMultiplier;
        }
        else if (playerController.IsCrouching() && playerController.IsMoving())
        {
            intensity = crouchMultiplier;
        }
        else if (playerController.IsMoving())
        {
            intensity = walkMultiplier;
        }

        if (playerController.IsZoomed())
        {
            intensity *= 0.5f;
        }

        return intensity;
    }

    void ApplyMicroSaccades(float intensity)
    {
        float time = Time.time * saccadeSpeed;

        targetSaccadeX = (Mathf.PerlinNoise(time, 0f) - 0.5f) * 2f * saccadeAmplitude * intensity;
        targetSaccadeY = (Mathf.PerlinNoise(0f, time) - 0.5f) * 2f * saccadeAmplitude * intensity;
        targetSaccadeZ = (Mathf.PerlinNoise(time * 0.7f, time * 0.5f) - 0.5f) * 2f * saccadeAmplitude * intensity * 0.5f;

        saccadeX = Mathf.Lerp(saccadeX, targetSaccadeX, Time.deltaTime * saccadeSmoothness);
        saccadeY = Mathf.Lerp(saccadeY, targetSaccadeY, Time.deltaTime * saccadeSmoothness);
        saccadeZ = Mathf.Lerp(saccadeZ, targetSaccadeZ, Time.deltaTime * saccadeSmoothness);

        Vector3 currentRotation = transform.localEulerAngles;
        currentRotation.x += saccadeX;
        currentRotation.y += saccadeY;
        currentRotation.z += saccadeZ;
        transform.localEulerAngles = currentRotation;
    }

    void ApplyGateWeave(float intensity)
    {
        float time = Time.time * weaveSpeed;

        float weaveXTarget = Mathf.Sin(time * 0.7f + 1.2f) * weaveAmplitude * intensity;
        float weaveYTarget = Mathf.Sin(time * 1.1f + 0.5f) * weaveAmplitude * intensity * 0.6f;

        weaveX = Mathf.Lerp(weaveX, weaveXTarget, Time.deltaTime * weaveSmoothness);
        weaveY = Mathf.Lerp(weaveY, weaveYTarget, Time.deltaTime * weaveSmoothness);

        Vector3 currentPos = transform.localPosition;
        currentPos.x += weaveX;
        currentPos.y += weaveY;
        transform.localPosition = currentPos;
    }

    public void ResetEffects()
    {
        transform.localPosition = initialLocalPosition;
        transform.localRotation = initialLocalRotation;
        saccadeX = 0f;
        saccadeY = 0f;
        saccadeZ = 0f;
        weaveX = 0f;
        weaveY = 0f;
    }
}