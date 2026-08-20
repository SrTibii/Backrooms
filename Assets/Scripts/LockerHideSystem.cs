using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LockerHideSystem : MonoBehaviour
{
    // ============================================
    // FLAG GLOBAL ESTÁTICO PARA QUE TODOS PUEDAN VER SI ESTÁ ESCONDIDO
    // ============================================
    public static bool IsPlayerHidingGlobal = false;

    [Header("Referencias")]
    public InteractionSystem interactionSystem;
    public Camera playerCamera;
    public Camera lockerCamera;
    public FirstPersonController playerController;
    public CharacterController characterController;
    public Collider playerCollider;
    public Image crosshairImage;

    [Header("Configuración de Taquilla")]
    public GameObject doorObject; // Puerta original (opaca)
    public GameObject doorTransparentObject; // Puerta transparente (duplicado)
    public float interactionDistance = 3f;

    [Header("Input Actions")]
    public InputActionReference interactAction;

    [Header("Sonidos de entrada/salida")]
    public AudioClip enterSound;
    public AudioClip exitSound;
    [Range(0f, 1f)] public float enterSoundVolume = 0.7f;
    [Range(0f, 1f)] public float exitSoundVolume = 0.7f;

    private AudioSource audioSource;
    private AudioListener lockerAudioListener;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private bool isLookingAtDoor = false;
    private bool isHiding = false;

    private bool wasPlayerCameraActive;
    private bool wasLockerCameraActive;
    private bool wasPlayerControllerEnabled;
    private bool wasCharacterControllerEnabled;
    private bool wasPlayerColliderEnabled;
    private bool wasCrosshairActive;

    private EnemyIA[] enemies;
    private bool wasLockerAudioListenerActive;

    public bool IsPlayerHiding() => isHiding;

    public bool IsHidingOrTransitioning()
    {
        return isHiding || (doorTransparentObject != null && doorTransparentObject.activeSelf);
    }

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
        if (doorObject == null)
        {
            Debug.LogError($"? {gameObject.name}: No se ha asignado la puerta en el Inspector!");
            return;
        }

        if (doorTransparentObject == null)
        {
            Debug.LogWarning($"?? {gameObject.name}: No se ha asignado la puerta transparente.");
        }

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
            Debug.LogError($"? {gameObject.name}: No se ha asignado la cámara de la taquilla");
        }

        if (lockerCamera != null)
        {
            lockerAudioListener = lockerCamera.GetComponent<AudioListener>();
            if (lockerAudioListener == null)
            {
                lockerAudioListener = lockerCamera.gameObject.AddComponent<AudioListener>();
            }
            lockerAudioListener.enabled = false;
        }

        if (playerController == null)
        {
            playerController = FindObjectOfType<FirstPersonController>();
        }

        if (characterController == null && playerController != null)
        {
            characterController = playerController.GetComponent<CharacterController>();
        }

        if (playerCollider == null && playerController != null)
        {
            playerCollider = playerController.GetComponent<Collider>();
        }

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

        if (doorTransparentObject != null)
        {
            doorTransparentObject.SetActive(false);
        }

        // ============================================
        // INICIALIZAR FLAG GLOBAL
        // ============================================
        IsPlayerHidingGlobal = false;

        Debug.Log($"? {gameObject.name} inicializado correctamente. Puerta: {doorObject?.name}");
    }

    void Update()
    {
        CheckDoorDetection();
    }

    void CheckDoorDetection()
    {
        if (doorObject == null || interactionSystem == null) return;

        GameObject target = interactionSystem.GetTargetObject();

        if (target != null && target == doorObject)
        {
            isLookingAtDoor = true;
        }
        else
        {
            isLookingAtDoor = false;
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
                Debug.Log($"? {gameObject.name}: No estás mirando la puerta asignada");
            }
        }
    }

    void EnterLocker()
    {
        if (isHiding) return;
        if (lockerCamera == null)
        {
            Debug.LogError($"? {gameObject.name}: No se ha asignado la cámara de la taquilla");
            return;
        }

        isHiding = true;
        IsPlayerHidingGlobal = true; // ?? ACTIVAR FLAG GLOBAL

        if (doorObject != null)
        {
            doorObject.SetActive(false);
        }

        if (doorTransparentObject != null)
        {
            doorTransparentObject.SetActive(true);
        }

        wasPlayerCameraActive = playerCamera.gameObject.activeSelf;
        wasLockerCameraActive = lockerCamera.gameObject.activeSelf;

        if (lockerAudioListener != null)
        {
            wasLockerAudioListenerActive = lockerAudioListener.enabled;
        }

        if (characterController != null)
        {
            wasCharacterControllerEnabled = characterController.enabled;
            characterController.enabled = false;
            Debug.Log("CharacterController DESACTIVADO");
        }

        if (playerCollider != null)
        {
            wasPlayerColliderEnabled = playerCollider.enabled;
            playerCollider.enabled = false;
            Debug.Log("Collider del player DESACTIVADO");
        }

        if (playerController != null)
        {
            wasPlayerControllerEnabled = playerController.enabled;
            playerController.enabled = false;
            Debug.Log("Movimiento del player DESACTIVADO");
        }

        if (crosshairImage != null)
        {
            wasCrosshairActive = crosshairImage.gameObject.activeSelf;
            crosshairImage.gameObject.SetActive(false);
            Debug.Log("Crosshair DESACTIVADO");
        }

        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(false);
        }

        if (lockerCamera != null)
        {
            lockerCamera.gameObject.SetActive(true);
        }

        if (lockerAudioListener != null)
        {
            lockerAudioListener.enabled = true;
            Debug.Log("AudioListener de taquilla ACTIVADO");
        }

        AudioListener playerAudioListener = playerCamera?.GetComponent<AudioListener>();
        if (playerAudioListener != null)
        {
            playerAudioListener.enabled = false;
            Debug.Log("AudioListener del player DESACTIVADO");
        }

        PlayEnterSound();
        NotifyEnemiesPlayerHid(true);

        Debug.Log($"? {gameObject.name}: Entrando a la taquilla - Flag Global = {IsPlayerHidingGlobal}");
    }

    void ExitLocker()
    {
        if (!isHiding) return;

        if (doorTransparentObject != null)
        {
            doorTransparentObject.SetActive(false);
        }

        if (doorObject != null)
        {
            doorObject.SetActive(true);
        }

        if (lockerCamera != null)
        {
            lockerCamera.gameObject.SetActive(false);
        }

        if (lockerAudioListener != null)
        {
            lockerAudioListener.enabled = false;
            Debug.Log("AudioListener de taquilla DESACTIVADO");
        }

        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);
        }

        AudioListener playerAudioListener = playerCamera?.GetComponent<AudioListener>();
        if (playerAudioListener != null)
        {
            playerAudioListener.enabled = true;
            Debug.Log("AudioListener del player REACTIVADO");
        }

        if (crosshairImage != null)
        {
            crosshairImage.gameObject.SetActive(wasCrosshairActive);
            Debug.Log("Crosshair REACTIVADO");
        }

        if (playerCollider != null)
        {
            playerCollider.enabled = wasPlayerColliderEnabled;
            Debug.Log("Collider del player REACTIVADO");
        }

        if (characterController != null)
        {
            characterController.enabled = wasCharacterControllerEnabled;
            Debug.Log("CharacterController REACTIVADO");
        }

        if (playerController != null)
        {
            ResetPlayerState();
            playerController.enabled = true;
            Debug.Log("Movimiento del player REACTIVADO - Estado reseteado");
        }

        isHiding = false;
        IsPlayerHidingGlobal = false; // ?? DESACTIVAR FLAG GLOBAL

        PlayExitSound();
        NotifyEnemiesPlayerHid(false);

        Debug.Log($"? {gameObject.name}: Saliendo de la taquilla - Flag Global = {IsPlayerHidingGlobal}");
    }

    void NotifyEnemiesPlayerHid(bool isHidden)
    {
        if (enemies == null || enemies.Length == 0)
        {
            enemies = FindObjectsOfType<EnemyIA>();
            if (enemies == null || enemies.Length == 0)
            {
                if (showDebugLogs) Debug.Log("No hay enemigos en la escena para notificar");
                return;
            }
        }

        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.SetPlayerHidden(isHidden);
                if (isHidden)
                {
                    enemy.OnPlayerHid();
                }
                if (showDebugLogs) Debug.Log($"? {(isHidden ? "Ocultando" : "Revelando")} al enemigo: {enemy.name}");
            }
        }
    }

    void ResetPlayerState()
    {
        if (playerController == null) return;

        if (characterController != null && !characterController.enabled)
        {
            characterController.enabled = true;
            Debug.Log("CharacterController REACTIVADO durante reset");
        }

        playerController.ResetSprintState();
        playerController.ResetMovementState();
        playerController.enabled = false;
        Debug.Log("Estado del player reseteado correctamente");
    }

    void PlayEnterSound()
    {
        if (enterSound == null)
        {
            if (showDebugLogs) Debug.LogWarning($"?? {gameObject.name}: No se ha asignado un sonido de entrada");
            return;
        }

        audioSource.volume = enterSoundVolume;
        audioSource.PlayOneShot(enterSound);
        Debug.Log($"Sonido de entrada reproducido (volumen: {enterSoundVolume})");
    }

    void PlayExitSound()
    {
        if (exitSound == null)
        {
            if (showDebugLogs) Debug.LogWarning($"?? {gameObject.name}: No se ha asignado un sonido de salida");
            return;
        }

        audioSource.volume = exitSoundVolume;
        audioSource.PlayOneShot(exitSound);
        Debug.Log($"Sonido de salida reproducido (volumen: {exitSoundVolume})");
    }

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
        return isLookingAtDoor ? doorObject : null;
    }
}