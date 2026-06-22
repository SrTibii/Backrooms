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
    public Collider playerCollider;
    public Image crosshairImage;

    [Header("Configuración de Taquilla")]
    public GameObject doorObject; // ?? ASIGNAR LA PUERTA EN EL INSPECTOR
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

    // AudioListener de la cámara de la taquilla
    private AudioListener lockerAudioListener;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // Variables internas
    private bool isLookingAtDoor = false;
    private bool isHiding = false;

    // Guardar estado original
    private bool wasPlayerCameraActive;
    private bool wasLockerCameraActive;
    private bool wasPlayerControllerEnabled;
    private bool wasCharacterControllerEnabled;
    private bool wasPlayerColliderEnabled;
    private bool wasCrosshairActive;

    // Variables para la transparencia de la puerta
    private Material doorMaterial;
    private float originalDoorOpacity = 1f;
    private bool isDoorTransparent = false;
    private Renderer doorRenderer;
    private int originalRenderQueue;
    private bool originalIsTransparent;

    // Referencia a los enemigos
    private EnemyIA[] enemies;

    private bool wasLockerAudioListenerActive;

    public bool IsPlayerHiding()
    {
        return isHiding;
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
        // ?? Verificar que la puerta esté asignada
        if (doorObject == null)
        {
            Debug.LogError($"? {gameObject.name}: No se ha asignado la puerta en el Inspector!");
            return;
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

        // Obtener el AudioListener de la cámara de la taquilla
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

        Debug.Log($"? {gameObject.name} inicializado correctamente. Puerta: {doorObject?.name}");
    }

    void Update()
    {
        CheckDoorDetection();
    }

    void CheckDoorDetection()
    {
        if (doorObject == null) return;
        if (interactionSystem == null) return;

        // ?? Obtener el objeto que el jugador está mirando
        GameObject target = interactionSystem.GetTargetObject();

        // ?? Verificar si el objeto mirado es EXACTAMENTE la puerta asignada
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

        // ?? Obtener el renderer de la puerta
        if (doorObject != null)
        {
            doorRenderer = doorObject.GetComponent<Renderer>();
            if (doorRenderer != null)
            {
                doorMaterial = doorRenderer.material;
                originalDoorOpacity = doorMaterial.color.a;
                Debug.Log($"?? Material de puerta guardado. Opacidad original: {originalDoorOpacity}");
            }
            else
            {
                Debug.LogWarning($"?? {gameObject.name}: La puerta no tiene Renderer");
            }
        }

        // Guardar estado actual de las cámaras
        wasPlayerCameraActive = playerCamera.gameObject.activeSelf;
        wasLockerCameraActive = lockerCamera.gameObject.activeSelf;

        // Guardar estado del AudioListener de la taquilla
        if (lockerAudioListener != null)
        {
            wasLockerAudioListenerActive = lockerAudioListener.enabled;
        }

        // Guardar estado del CharacterController
        if (characterController != null)
        {
            wasCharacterControllerEnabled = characterController.enabled;
            characterController.enabled = false;
            Debug.Log("?? CharacterController DESACTIVADO");
        }

        if (playerCollider != null)
        {
            wasPlayerColliderEnabled = playerCollider.enabled;
            playerCollider.enabled = false;
            Debug.Log("?? Collider del player DESACTIVADO");
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

        // ACTIVAR el AudioListener de la taquilla
        if (lockerAudioListener != null)
        {
            lockerAudioListener.enabled = true;
            Debug.Log("?? AudioListener de taquilla ACTIVADO");
        }

        // Desactivar el AudioListener de la cámara del player
        AudioListener playerAudioListener = playerCamera?.GetComponent<AudioListener>();
        if (playerAudioListener != null)
        {
            playerAudioListener.enabled = false;
            Debug.Log("?? AudioListener del player DESACTIVADO");
        }

        MakeDoorTransparentInstant();

        // Reproducir sonido de entrada
        PlayEnterSound();

        // NOTIFICAR A TODOS LOS ENEMIGOS
        NotifyEnemiesPlayerHid();

        Debug.Log($"? {gameObject.name}: Entrando a la taquilla");
    }

    void ExitLocker()
    {
        if (!isHiding) return;

        // DESACTIVAR cámara de la taquilla
        if (lockerCamera != null)
        {
            lockerCamera.gameObject.SetActive(false);
        }

        // DESACTIVAR el AudioListener de la taquilla
        if (lockerAudioListener != null)
        {
            lockerAudioListener.enabled = false;
            Debug.Log("?? AudioListener de taquilla DESACTIVADO");
        }

        // REACTIVAR cámara del player
        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);
        }

        // REACTIVAR el AudioListener del player
        AudioListener playerAudioListener = playerCamera?.GetComponent<AudioListener>();
        if (playerAudioListener != null)
        {
            playerAudioListener.enabled = true;
            Debug.Log("?? AudioListener del player REACTIVADO");
        }

        // ?? RESTAURAR LA PUERTA
        RestoreDoorInstant();

        // REACTIVAR el crosshair
        if (crosshairImage != null)
        {
            crosshairImage.gameObject.SetActive(wasCrosshairActive);
            Debug.Log("? Crosshair REACTIVADO");
        }

        if (playerCollider != null)
        {
            playerCollider.enabled = wasPlayerColliderEnabled;
            Debug.Log("? Collider del player REACTIVADO");
        }

        // REACTIVAR el CharacterController
        if (characterController != null)
        {
            characterController.enabled = wasCharacterControllerEnabled;
            Debug.Log("? CharacterController REACTIVADO");
        }

        // REACTIVAR el controlador del player
        if (playerController != null)
        {
            ResetPlayerState();
            playerController.enabled = true;
            Debug.Log("? Movimiento del player REACTIVADO - Estado reseteado");
        }

        isHiding = false;

        // Reproducir sonido de salida
        PlayExitSound();

        Debug.Log($"? {gameObject.name}: Saliendo de la taquilla");
    }

    void NotifyEnemiesPlayerHid()
    {
        if (enemies == null || enemies.Length == 0)
        {
            enemies = FindObjectsOfType<EnemyIA>();
            if (enemies == null || enemies.Length == 0)
            {
                if (showDebugLogs) Debug.Log("? No hay enemigos en la escena para notificar");
                return;
            }
        }

        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.OnPlayerHid();
                if (showDebugLogs) Debug.Log($"? Notificado a enemigo: {enemy.name}");
            }
        }
    }

    void ResetPlayerState()
    {
        if (playerController == null) return;

        // Asegurarse de que el CharacterController está activo antes de resetear
        if (characterController != null && !characterController.enabled)
        {
            characterController.enabled = true;
            Debug.Log("? CharacterController REACTIVADO durante reset");
        }

        playerController.ResetSprintState();
        playerController.ResetMovementState();
        playerController.enabled = false;

        Debug.Log("? Estado del player reseteado correctamente");
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
        Debug.Log($"?? Sonido de entrada reproducido (volumen: {enterSoundVolume})");
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
        Debug.Log($"?? Sonido de salida reproducido (volumen: {exitSoundVolume})");
    }

    // ============================================
    // ?? TRANSPARENCIA DE PUERTA
    // ============================================

    void MakeDoorTransparentInstant()
    {
        if (doorRenderer == null)
        {
            Debug.LogWarning($"?? {gameObject.name}: No hay renderer de puerta");
            return;
        }

        if (doorMaterial == null)
        {
            Debug.LogWarning($"?? {gameObject.name}: No hay material de puerta");
            return;
        }

        // Guardar estado original del material
        originalRenderQueue = doorMaterial.renderQueue;
        originalIsTransparent = doorMaterial.IsKeywordEnabled("_ALPHABLEND_ON");

        // Hacer transparente
        doorMaterial.SetFloat("_Mode", 3);
        doorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        doorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        doorMaterial.SetInt("_ZWrite", 0);
        doorMaterial.DisableKeyword("_ALPHATEST_ON");
        doorMaterial.EnableKeyword("_ALPHABLEND_ON");
        doorMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        doorMaterial.renderQueue = 3000;

        Color color = doorMaterial.color;
        color.a = doorTransparency;
        doorMaterial.color = color;

        isDoorTransparent = true;
        Debug.Log($"? {gameObject.name}: Puerta transparente");
    }

    void RestoreDoorInstant()
    {
        if (doorMaterial == null || doorRenderer == null)
        {
            Debug.LogWarning($"?? {gameObject.name}: No hay puerta para restaurar");
            return;
        }

        // ?? RESTAURAR COLOR
        Color color = doorMaterial.color;
        color.a = originalDoorOpacity;
        doorMaterial.color = color;

        // ?? RESTAURAR RENDER MODE
        doorMaterial.SetFloat("_Mode", 0);
        doorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        doorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        doorMaterial.SetInt("_ZWrite", 1);
        doorMaterial.DisableKeyword("_ALPHABLEND_ON");
        doorMaterial.EnableKeyword("_ALPHATEST_ON");
        doorMaterial.renderQueue = originalRenderQueue;

        // ?? FORZAR ACTUALIZACIÓN DEL MATERIAL
        doorRenderer.material = doorMaterial;

        doorMaterial = null;
        doorRenderer = null;
        isDoorTransparent = false;

        Debug.Log($"? {gameObject.name}: Puerta restaurada");
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
        return isLookingAtDoor ? doorObject : null;
    }
}