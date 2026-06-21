using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LockerHideSystem : MonoBehaviour
{
    [Header("Referencias")]
    public InteractionSystem interactionSystem;
    public Camera playerCamera;
    public Camera lockerCamera;
    public FirstPersonController playerController;
    public CharacterController characterController;
    public Image crosshairImage;

    [Header("Configuración")]
    public string doorTag = "Door";
    public float interactionDistance = 3f;

    [Header("Input Actions")]
    public InputActionReference interactAction;

    [Header("Puerta transparente")]
    public float doorTransparency = 0.15f;

    // ============================================
    // AUDIO DE ENTRADA Y SALIDA
    // ============================================
    [Header("Sonidos de entrada/salida")]
    public AudioClip enterSound;
    public AudioClip exitSound;
    [Range(0f, 1f)] public float enterSoundVolume = 0.7f;
    [Range(0f, 1f)] public float exitSoundVolume = 0.7f;

    private AudioSource audioSource;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // Variables internas
    private bool isLookingAtDoor = false;
    private GameObject currentTarget;
    private bool isHiding = false;

    // Guardar estado original
    private bool wasPlayerCameraActive;
    private bool wasLockerCameraActive;
    private bool wasPlayerControllerEnabled;
    private bool wasCharacterControllerEnabled;
    private bool wasCrosshairActive;

    // Variables para la transparencia de la puerta
    private GameObject currentDoorObject;
    private Material doorMaterial;
    private float originalDoorOpacity = 1f;
    private bool isDoorTransparent = false;

    // ============================================
    // Referencia a los enemigos
    // ============================================
    private EnemyIA[] enemies;

    private void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed += OnInteractPerformed;
            interactAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed -= OnInteractPerformed;
            interactAction.action.Disable();
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        HandleInteraction();
    }

    void Start()
    {
        if (interactionSystem == null)
        {
            interactionSystem = FindObjectOfType<InteractionSystem>();
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (lockerCamera == null)
        {
            Debug.LogError("LockerHideSystem: No se ha asignado la cámara de la taquilla");
        }

        if (playerController == null)
        {
            playerController = FindObjectOfType<FirstPersonController>();
            if (playerController == null)
            {
                Debug.LogWarning("LockerHideSystem: No se encontró FirstPersonController");
            }
        }

        if (characterController == null && playerController != null)
        {
            characterController = playerController.GetComponent<CharacterController>();
            if (characterController == null)
            {
                Debug.LogWarning("LockerHideSystem: No se encontró CharacterController en el player");
            }
        }

        // Buscar todos los enemigos en la escena
        enemies = FindObjectsOfType<EnemyIA>();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (crosshairImage == null && interactionSystem != null)
        {
            var crosshairRect = interactionSystem?.GetComponentInChildren<RectTransform>();
            if (crosshairRect != null)
            {
                crosshairImage = crosshairRect.GetComponent<Image>();
            }
        }

        if (lockerCamera != null)
        {
            lockerCamera.gameObject.SetActive(false);
        }

        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);
        }

        if (crosshairImage != null)
        {
            wasCrosshairActive = crosshairImage.gameObject.activeSelf;
        }
    }

    void Update()
    {
        CheckDoorDetection();
    }

    void CheckDoorDetection()
    {
        if (interactionSystem == null) return;

        GameObject target = interactionSystem.GetTargetObject();

        if (target != null && target.CompareTag(doorTag))
        {
            isLookingAtDoor = true;
            currentTarget = target;
        }
        else
        {
            isLookingAtDoor = false;
            currentTarget = null;
        }
    }

    void HandleInteraction()
    {
        if (isHiding)
        {
            ExitLocker();
            return;
        }

        if (isLookingAtDoor)
        {
            EnterLocker();
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.Log("? No estás mirando una puerta");
            }
        }
    }

    void EnterLocker()
    {
        if (isHiding) return;
        if (lockerCamera == null)
        {
            Debug.LogError("LockerHideSystem: No se ha asignado la cámara de la taquilla");
            return;
        }

        isHiding = true;
        currentDoorObject = currentTarget;

        // Guardar estado actual de las cámaras
        wasPlayerCameraActive = playerCamera.gameObject.activeSelf;
        wasLockerCameraActive = lockerCamera.gameObject.activeSelf;

        // Guardar estado del CharacterController
        if (characterController != null)
        {
            wasCharacterControllerEnabled = characterController.enabled;
            characterController.enabled = false;
            Debug.Log("??? CharacterController DESACTIVADO");
        }

        // Guardar y desactivar el controlador del player
        if (playerController != null)
        {
            wasPlayerControllerEnabled = playerController.enabled;
            playerController.enabled = false;
            Debug.Log("?? Movimiento del player DESACTIVADO");
        }

        // DESACTIVAR el crosshair
        if (crosshairImage != null)
        {
            wasCrosshairActive = crosshairImage.gameObject.activeSelf;
            crosshairImage.gameObject.SetActive(false);
            Debug.Log("?? Crosshair DESACTIVADO");
        }

        // DESACTIVAR cámara del player
        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(false);
        }

        // ACTIVAR cámara de la taquilla
        if (lockerCamera != null)
        {
            lockerCamera.gameObject.SetActive(true);
        }

        MakeDoorTransparentInstant();

        // Reproducir sonido de entrada
        PlayEnterSound();

        // NOTIFICAR A TODOS LOS ENEMIGOS QUE EL JUGADOR SE HA ESCONDIDO
        NotifyEnemiesPlayerHid();

        Debug.Log("?? Entrando a la taquilla");
    }

    void ExitLocker()
    {
        if (!isHiding) return;

        // DESACTIVAR cámara de la taquilla
        if (lockerCamera != null)
        {
            lockerCamera.gameObject.SetActive(false);
        }

        // REACTIVAR cámara del player
        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);
        }

        // REACTIVAR el crosshair
        if (crosshairImage != null)
        {
            crosshairImage.gameObject.SetActive(wasCrosshairActive);
            Debug.Log("?? Crosshair REACTIVADO");
        }

        // REACTIVAR el CharacterController
        if (characterController != null)
        {
            characterController.enabled = wasCharacterControllerEnabled;
            Debug.Log("??? CharacterController REACTIVADO");
        }

        // REACTIVAR el controlador del player
        if (playerController != null)
        {
            ResetPlayerState();
            playerController.enabled = true;
            Debug.Log("? Movimiento del player REACTIVADO - Estado reseteado");
        }

        RestoreDoorInstant();

        // Reproducir sonido de salida
        PlayExitSound();

        isHiding = false;
        currentDoorObject = null;

        Debug.Log("?? Saliendo de la taquilla");
    }

    /// <summary>
    /// Notifica a todos los enemigos que el jugador se ha escondido
    /// </summary>
    void NotifyEnemiesPlayerHid()
    {
        if (enemies == null || enemies.Length == 0)
        {
            // Buscar enemigos si no se encontraron antes
            enemies = FindObjectsOfType<EnemyIA>();
            if (enemies == null || enemies.Length == 0)
            {
                if (showDebugLogs) Debug.Log("?? No hay enemigos en la escena para notificar");
                return;
            }
        }

        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.OnPlayerHid();
                if (showDebugLogs) Debug.Log($"?? Notificado a enemigo: {enemy.name}");
            }
        }
    }

    /// <summary>
    /// Resetea completamente el estado del player para evitar movimiento residual
    /// </summary>
    void ResetPlayerState()
    {
        if (playerController == null) return;

        playerController.ResetSprintState();
        playerController.ResetMovementState();
        playerController.enabled = false;

        Debug.Log("?? Estado del player reseteado correctamente");
    }

    // ============================================
    // MÉTODOS PARA REPRODUCIR SONIDOS
    // ============================================

    void PlayEnterSound()
    {
        if (enterSound == null)
        {
            if (showDebugLogs) Debug.LogWarning("LockerHideSystem: No se ha asignado un sonido de entrada");
            return;
        }

        audioSource.volume = enterSoundVolume;
        audioSource.PlayOneShot(enterSound);
        Debug.Log($"?? Sonido de entrada reproducido (volumen: {enterSoundVolume})");
    }

    void PlayExitSound()
    {
        if (exitSound == null)
        {
            if (showDebugLogs) Debug.LogWarning("LockerHideSystem: No se ha asignado un sonido de salida");
            return;
        }

        audioSource.volume = exitSoundVolume;
        audioSource.PlayOneShot(exitSound);
        Debug.Log($"?? Sonido de salida reproducido (volumen: {exitSoundVolume})");
    }

    // ============================================
    // TRANSPARENCIA INSTANTÁNEA DE LA PUERTA
    // ============================================

    void MakeDoorTransparentInstant()
    {
        if (currentDoorObject == null)
        {
            Debug.LogWarning("LockerHideSystem: No hay puerta para hacer transparente");
            return;
        }

        MeshRenderer renderer = currentDoorObject.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            Debug.LogWarning("LockerHideSystem: La puerta no tiene MeshRenderer");
            return;
        }

        doorMaterial = renderer.material;
        originalDoorOpacity = doorMaterial.color.a;

        SetMaterialTransparentInstant(doorMaterial);

        isDoorTransparent = true;
        Debug.Log("?? Puerta transparente INSTANTÁNEAMENTE");
    }

    void SetMaterialTransparentInstant(Material mat)
    {
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        Color color = mat.color;
        color.a = doorTransparency;
        mat.color = color;
    }

    void RestoreDoorInstant()
    {
        if (doorMaterial == null) return;

        Color color = doorMaterial.color;
        color.a = originalDoorOpacity;
        doorMaterial.color = color;

        doorMaterial.SetFloat("_Mode", 0);
        doorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        doorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        doorMaterial.SetInt("_ZWrite", 1);
        doorMaterial.DisableKeyword("_ALPHABLEND_ON");
        doorMaterial.EnableKeyword("_ALPHATEST_ON");
        doorMaterial.renderQueue = -1;

        doorMaterial = null;
        isDoorTransparent = false;

        Debug.Log("?? Puerta restaurada INSTANTÁNEAMENTE");
    }

    // Métodos públicos
    public bool IsHiding()
    {
        return isHiding;
    }

    public bool IsLookingAtDoor()
    {
        return isLookingAtDoor;
    }

    public GameObject GetCurrentDoor()
    {
        return currentTarget;
    }
}