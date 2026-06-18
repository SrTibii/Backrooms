using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonController : MonoBehaviour
{
    // ============================================
    // 1. MOVIMIENTO Y CONFIGURACIÓN BÁSICA
    // ============================================
    [Header("Movimiento")]
    public float walkSpeed = 3f;          // Velocidad al andar
    public float sprintSpeed = 6f;        // Velocidad al correr
    public float acceleration = 8f;       // Qué tan rápido aceleras
    public float deceleration = 10f;      // Qué tan rápido frenas
    public float maxVelocity = 5f;        // Velocidad máxima (tope)

    // ============================================
    // 2. STAMINA (RESISTENCIA)
    // ============================================
    [Header("Stamina")]
    public float maxStamina = 100f;               // Stamina máxima
    public float staminaDrainRate = 20f;          // Gasto por segundo al correr
    public float staminaRegenRate = 10f;          // Recuperación por segundo
    public float staminaRegenDelay = 2f;          // Espera antes de recuperar
    public float staminaRequiredToSprint = 100f;  // Stamina mínima para correr

    // ============================================
    // 3. CONTROL DE CÁMARA (RATÓN)
    // ============================================
    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;   // Sensibilidad del ratón
    public float minYAngle = -80f;        // Límite inferior de mirada
    public float maxYAngle = 80f;         // Límite superior de mirada

    // ============================================
    // 4. HEAD BOB (BALANCEO DE CÁMARA)
    // ============================================
    [Header("Head Bob")]
    public bool enableHeadBob = true;             // Activar/desactivar balanceo
    public float bobSpeed = 1.4f;                 // Velocidad del balanceo
    public float bobAmount = 0.05f;               // Amplitud del balanceo
    public float bobSmoothness = 0.5f;            // Suavidad de la transición

    [Header("Head Bob - Sprint")]
    public float sprintBobSpeedMultiplier = 1.8f;   // Velocidad al correr (más rápido)
    public float sprintBobAmountMultiplier = 2.0f;  // Amplitud al correr (más movimiento)

    [Header("Head Bob Horizontal - Sprint")]
    public bool enableHorizontalBob = true;          // Balanceo lateral al correr
    public float horizontalBobAmount = 0.03f;        // Amplitud del movimiento lateral
    public float horizontalBobSpeedMultiplier = 1.5f; // Velocidad del movimiento lateral

    // ============================================
    // 5. INCLINACIÓN DE CÁMARA (TILT)
    // ============================================
    [Header("Cámara Inclinación")]
    public bool enableTilt = true;          // Activar inclinación lateral
    public float tiltAmount = 2f;           // Intensidad de la inclinación
    public float tiltSmoothness = 4f;       // Suavidad de la inclinación

    [Header("Cámara Inclinación - Sprint")]
    public float sprintTiltMultiplier = 1.8f; // Inclinación más pronunciada al correr

    // ============================================
    // 6. INERCIA (MOVIMIENTO SUAVE)
    // ============================================
    [Header("Efecto de Inercia")]
    public bool enableInertia = true; // Activar/desactivar inercia

    // ============================================
    // 7. CAMBIO DE FOV (AL CORRER)
    // ============================================
    [Header("FOV Change")]
    public bool enableFOVChange = true;   // Activar cambio de FOV al correr
    public float normalFOV = 60f;         // FOV normal
    public float sprintFOV = 70f;         // FOV al correr (sensación de velocidad)
    public float fovSmoothness = 5f;      // Suavidad del cambio de FOV

    // ============================================
    // 8. INPUT ACTIONS (INPUT SYSTEM)
    // ============================================
    [Header("Input Actions")]
    public InputActionReference moveAction;   // Movimiento WASD
    public InputActionReference lookAction;   // Mirada con ratón
    public InputActionReference sprintAction; // Sprint (Shift)
    public InputActionReference zoomAction;   // Zoom (tecla elegida)
    public InputActionReference crouchAction; // Agacharse (Ctrl)

    // ============================================
    // 9. ZOOM VHS
    // ============================================
    [Header("Zoom VHS")]
    public float zoomFOV = 30f;                     // FOV al hacer zoom
    public float zoomTransitionSpeed = 5f;          // Velocidad de transición
    public bool enableZoomVignette = true;          // Activar viñeta al zoom
    public float zoomVignetteIntensity = 0.8f;      // Intensidad de la viñeta

    [Header("Zoom Audio")]
    public AudioClip zoomInSound;   // Sonido al activar zoom
    public AudioClip zoomOutSound;  // Sonido al desactivar zoom
    public float zoomSoundVolume = 0.5f;

    [Header("Focus Effect")]
    public bool enableFocusEffect = true;        // Activar/desactivar efecto de enfoque
    public float focusTransitionSpeed = 3f;      // Velocidad de transición del enfoque
    public float focusBlurAmount = 15f;          // Intensidad del desenfoque (en píxeles)

    // ============================================
    // 10. AUDIO DE PASOS
    // ============================================
    [Header("Footstep Audio")]
    public AudioClip[] footstepSounds;              // Sonidos de pasos normales
    public float footstepVolume = 0.4f;             // Volumen de pasos
    public float footstepPitchMin = 0.85f;          // Tono mínimo
    public float footstepPitchMax = 1.15f;          // Tono máximo
    public float footstepInterval = 0.5f;           // Intervalo entre pasos
    public bool enableFootsteps = true;             // Activar pasos

    [Header("Sprint Footstep Audio")]
    public AudioClip[] sprintFootstepSounds;        // Sonidos de pasos al correr
    public float sprintFootstepInterval = 0.35f;    // Intervalo más rápido al correr

    // ============================================
    // 11. AUDIO DE RESPIRACIÓN
    // ============================================
    [Header("Breathing Audio")]
    public AudioClip breathingClip;                 // Loop de respiración
    public float breathingVolumeWalk = 0.15f;       // Volumen al andar
    public float breathingVolumeSprint = 0.5f;      // Volumen al correr
    public float breathingPitchWalk = 0.9f;         // Pitch al andar (lento)
    public float breathingPitchSprint = 1.4f;       // Pitch al correr (rápido)
    public float breathingTransitionSpeed = 2f;     // Velocidad de transición

    // ============================================
    // 11.5. STAMINA AUDIO
    // ============================================
    [Header("Stamina Audio")]
    public AudioClip staminaDepletedSound;      // Sonido cuando te quedas sin stamina
    public AudioClip staminaRecoveredSound;     // Sonido cuando recuperas toda la stamina
    public float staminaSoundVolume = 0.5f;     // Volumen de estos sonidos

    // ============================================
    // 12. CROUCH - AGACHARSE
    // ============================================
    [Header("Crouch Settings")]
    public bool enableCrouch = true;                    // Activar/desactivar agacharse
    public float crouchHeight = 1f;                     // Altura al agacharse
    public float standingHeight = 1.8f;                 // Altura normal
    public float crouchSpeed = 2f;                      // Velocidad al agacharse
    public float crouchTransitionSpeed = 8f;            // Velocidad de transición entre estados
    public float crouchCameraOffset = 0.6f;             // Cuánto baja la cámara al agacharse

    [Header("Crouch - Ceiling Detection")]
    public bool enableCeilingDetection = true;           // Activar/desactivar detección de techo
    public float ceilingCheckRadius = 0.2f;              // Radio del esfera de detección
    public float ceilingCheckDistance = 0.3f;            // Distancia extra para detectar techo
    public LayerMask ceilingLayerMask = -1;              // Layers a detectar (-1 = todos)

    [Header("Crouch Audio")]
    public AudioClip crouchSound;                       // Sonido al agacharse
    public AudioClip standSound;                        // Sonido al levantarse
    public float crouchSoundVolume = 0.3f;              // Volumen de los sonidos de agacharse

    // ============================================
    // 12.5. CAMERA SWAY - BALANCEO DE CÁMARA
    // ============================================
    [Header("Camera Sway (Balanceo de cámara)")]
    public bool enableSway = true;                      // Activar/desactivar balanceo
    public float swayAmount = 2.5f;                     // Intensidad del balanceo
    public float swaySpeed = 4f;                        // Velocidad del balanceo
    public float swaySmoothness = 6f;                   // Suavidad del movimiento
    public float swayMaxAngle = 8f;                     // Ángulo máximo de inclinación
    public float swayReturnSpeed = 4f;                  // Velocidad de retorno a la posición neutra

    [Header("Sway - Sprint")]
    public float swaySpeedMultiplier = 1.8f;            // Velocidad al correr
    public float swayAmountMultiplier = 2.0f;           // Intensidad al correr

    [Header("Sway - Crouch")]
    public float swayAmountCrouchMultiplier = 0.5f;     // Intensidad al agacharse
    public float swaySpeedCrouchMultiplier = 0.7f;      // Velocidad al agacharse

    // ============================================
    // 13. VARIABLES PRIVADAS (COMPONENTES)
    // ============================================
    private CharacterController controller;          // Controlador de movimiento
    private Transform cameraTransform;               // Transform de la cámara
    private Vector3 cameraInitialPosition;           // Posición inicial de la cámara
    private Vector3 cameraHorizontalOffset;          // Offset para head bob horizontal

    private Vector3 currentVelocity;                 // Velocidad actual
    public bool isMoving;                            // ¿Está moviéndose?
    private bool isSprinting;                        // ¿Está corriendo?
    private bool isSprintPressed;                    // ¿Tecla de sprint pulsada?

    private float xRotation;                         // Rotación vertical de la cámara
    private float yRotation;                         // Rotación horizontal del jugador

    private float bobTimer;                          // Timer para head bob
    private float horizontalBobTimer;                // Timer para head bob horizontal
    private float currentTilt;                       // Inclinación actual

    // Input System
    private Vector2 moveInput;
    private Vector2 lookInput;

    // Audio
    private float footstepTimer;
    private AudioSource footstepAudioSource;
    private AudioSource breathingAudioSource;
    private AudioSource zoomAudioSource;
    private AudioSource crouchAudioSource;
    private AudioSource staminaAudioSource;          // AudioSource para sonidos de stamina
    private float currentBreathingVolume;
    private float currentBreathingPitch;

    // Stamina
    private float currentStamina;
    private float staminaRegenTimer;
    private bool isExhausted;
    private bool hasPlayedDepletedSound = false;     // Evita que el sonido se repita

    // FOV
    private Camera playerCamera;
    private float targetFOV;

    // Zoom VHS
    private bool isZoomed;
    private float originalFOV;

    // Crouch
    private bool isCrouching = false;
    private bool isCrouchPressed = false;
    private float currentHeight;
    private Vector3 crouchCameraTarget;

    // Ceiling detection
    private bool isBlockedByCeiling = false;

    // Camera Sway
    private float swayCurrentAngle = 0f;
    private float swayTargetAngle = 0f;
    private float swayTimer = 0f;
    private Vector3 swayTargetPosition = Vector3.zero;
    private Vector3 swayCurrentPosition = Vector3.zero;

    // ============================================
    // 14. ONENABLE / ONDISABLE
    // ============================================
    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.action.performed += OnMovePerformed;
            moveAction.action.canceled += OnMoveCanceled;
            moveAction.action.Enable();
        }

        if (lookAction != null)
        {
            lookAction.action.performed += OnLookPerformed;
            lookAction.action.canceled += OnLookCanceled;
            lookAction.action.Enable();
        }

        if (sprintAction != null)
        {
            sprintAction.action.performed += OnSprintPerformed;
            sprintAction.action.canceled += OnSprintCanceled;
            sprintAction.action.Enable();
        }

        if (zoomAction != null)
        {
            zoomAction.action.performed += OnZoomPerformed;
            zoomAction.action.Enable();
        }

        if (crouchAction != null)
        {
            crouchAction.action.performed += OnCrouchPerformed;
            crouchAction.action.canceled += OnCrouchCanceled;
            crouchAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.action.performed -= OnMovePerformed;
            moveAction.action.canceled -= OnMoveCanceled;
            moveAction.action.Disable();
        }

        if (lookAction != null)
        {
            lookAction.action.performed -= OnLookPerformed;
            lookAction.action.canceled -= OnLookCanceled;
            lookAction.action.Disable();
        }

        if (sprintAction != null)
        {
            sprintAction.action.performed -= OnSprintPerformed;
            sprintAction.action.canceled -= OnSprintCanceled;
            sprintAction.action.Disable();
        }

        if (zoomAction != null)
        {
            zoomAction.action.performed -= OnZoomPerformed;
            zoomAction.action.Disable();
        }

        if (crouchAction != null)
        {
            crouchAction.action.performed -= OnCrouchPerformed;
            crouchAction.action.canceled -= OnCrouchCanceled;
            crouchAction.action.Disable();
        }
    }

    // ============================================
    // 15. CALLBACKS DE INPUT SYSTEM
    // ============================================
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }

    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    private void OnLookCanceled(InputAction.CallbackContext context)
    {
        lookInput = Vector2.zero;
    }

    private void OnSprintPerformed(InputAction.CallbackContext context)
    {
        isSprintPressed = true;
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        isSprintPressed = false;
    }

    private void OnZoomPerformed(InputAction.CallbackContext context)
    {
        ToggleZoom();
    }

    private void OnCrouchPerformed(InputAction.CallbackContext context)
    {
        isCrouchPressed = true;
        ToggleCrouch();
    }

    private void OnCrouchCanceled(InputAction.CallbackContext context)
    {
        isCrouchPressed = false;
    }

    // ============================================
    // 16. START - INICIALIZACIÓN
    // ============================================
    void Start()
    {
        // --- Character Controller ---
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.3f;
        }
        currentHeight = standingHeight;
        controller.height = standingHeight;

        // --- Cámara ---
        cameraTransform = GetComponentInChildren<Camera>().transform;
        if (cameraTransform == null)
        {
            GameObject cam = new GameObject("Main Camera");
            cam.transform.SetParent(transform);
            cam.transform.localPosition = new Vector3(0, 1.6f, 0);
            cam.AddComponent<Camera>();
            cameraTransform = cam.transform;
        }

        cameraInitialPosition = cameraTransform.localPosition;
        crouchCameraTarget = new Vector3(0, cameraInitialPosition.y - crouchCameraOffset, 0);

        // --- Guardar FOV original ---
        playerCamera = cameraTransform.GetComponent<Camera>();
        if (playerCamera != null)
        {
            normalFOV = playerCamera.fieldOfView;
            targetFOV = normalFOV;
            originalFOV = normalFOV;
        }

        // --- Bloquear cursor ---
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // --- AudioSource para pasos ---
        footstepAudioSource = gameObject.AddComponent<AudioSource>();
        footstepAudioSource.loop = false;
        footstepAudioSource.playOnAwake = false;
        footstepAudioSource.spatialBlend = 0f;
        footstepAudioSource.volume = footstepVolume;

        // --- AudioSource para respiración ---
        breathingAudioSource = gameObject.AddComponent<AudioSource>();
        breathingAudioSource.loop = true;
        breathingAudioSource.playOnAwake = false;
        breathingAudioSource.spatialBlend = 0f;
        breathingAudioSource.clip = breathingClip;

        currentBreathingVolume = breathingVolumeWalk;
        currentBreathingPitch = breathingPitchWalk;
        breathingAudioSource.volume = currentBreathingVolume;
        breathingAudioSource.pitch = currentBreathingPitch;

        if (breathingClip != null)
        {
            breathingAudioSource.Play();
        }

        // --- AudioSource para zoom ---
        zoomAudioSource = gameObject.AddComponent<AudioSource>();
        zoomAudioSource.loop = false;
        zoomAudioSource.playOnAwake = false;
        zoomAudioSource.spatialBlend = 0f;
        zoomAudioSource.volume = zoomSoundVolume;

        // --- AudioSource para crouch ---
        crouchAudioSource = gameObject.AddComponent<AudioSource>();
        crouchAudioSource.loop = false;
        crouchAudioSource.playOnAwake = false;
        crouchAudioSource.spatialBlend = 0f;
        crouchAudioSource.volume = crouchSoundVolume;

        // --- AudioSource para sonidos de stamina ---
        staminaAudioSource = gameObject.AddComponent<AudioSource>();
        staminaAudioSource.loop = false;
        staminaAudioSource.playOnAwake = false;
        staminaAudioSource.spatialBlend = 0f;
        staminaAudioSource.volume = staminaSoundVolume;

        // --- Stamina ---
        currentStamina = maxStamina;
        isExhausted = false;
        staminaRegenTimer = 0f;
    }

    // ============================================
    // 17. UPDATE - BUCLE PRINCIPAL
    // ============================================
    void Update()
    {
        HandleStamina();
        HandleCrouch();
        HandleMovement();
        HandleMouseLook();
        HandleSway();

        if (enableHeadBob)
            HandleHeadBob();

        if (enableTilt)
            HandleTilt();

        if (enableFootsteps)
            HandleFootsteps();

        if (enableFOVChange)
            HandleFOV();

        HandleBreathing();

        if (enableFocusEffect)
            HandleFocusEffect();
    }

    // ============================================
    // 18. STAMINA - RESISTENCIA
    // ============================================
    void HandleStamina()
    {
        bool canRun = isSprintPressed && isMoving && currentStamina > 0 && !isExhausted && !isCrouching;

        if (canRun)
        {
            // ? CORRIENDO - Gastar stamina
            currentStamina -= staminaDrainRate * Time.deltaTime;
            staminaRegenTimer = 0f;
            hasPlayedDepletedSound = false;

            if (currentStamina <= 0)
            {
                currentStamina = 0;
                isExhausted = true;
                isSprinting = false;
                staminaRegenTimer = 0f;

                // Reproducir sonido de STAMINA AGOTADA
                if (staminaDepletedSound != null && !hasPlayedDepletedSound)
                {
                    staminaAudioSource.PlayOneShot(staminaDepletedSound);
                    hasPlayedDepletedSound = true;
                }

                Debug.Log("?? ¡Agotado! Esperando para recuperar...");
            }
            else
            {
                isSprinting = true;
            }
        }
        else
        {
            isSprinting = false;

            if (isExhausted)
            {
                // ? AGOTADO - Esperar delay antes de recuperar
                staminaRegenTimer += Time.deltaTime;

                if (staminaRegenTimer >= staminaRegenDelay)
                {
                    currentStamina += staminaRegenRate * Time.deltaTime;

                    if (currentStamina >= maxStamina)
                    {
                        currentStamina = maxStamina;
                        isExhausted = false;
                        staminaRegenTimer = 0f;
                        hasPlayedDepletedSound = false;

                        // Reproducir sonido de STAMINA RECUPERADA
                        if (staminaRecoveredSound != null)
                        {
                            staminaAudioSource.PlayOneShot(staminaRecoveredSound);
                        }

                        Debug.Log("? ¡Recuperado! Puedes correr de nuevo.");
                    }
                }
            }
            else
            {
                // ?? RECUPERACIÓN NORMAL (sin estar agotado)
                if (!isSprintPressed || !isMoving)
                {
                    staminaRegenTimer += Time.deltaTime;

                    if (staminaRegenTimer >= staminaRegenDelay)
                    {
                        float previousStamina = currentStamina;
                        currentStamina += staminaRegenRate * Time.deltaTime;
                        currentStamina = Mathf.Min(currentStamina, maxStamina);

                        // Si hemos recuperado toda la stamina (y no estábamos agotados)
                        if (currentStamina >= maxStamina && previousStamina < maxStamina)
                        {
                            if (staminaRecoveredSound != null)
                            {
                                staminaAudioSource.PlayOneShot(staminaRecoveredSound);
                            }
                        }
                    }
                }
                else
                {
                    staminaRegenTimer = 0f;
                }
            }
        }
    }

    // ============================================
    // 19. MOVIMIENTO
    // ============================================
    void HandleMovement()
    {
        float horizontal = moveInput.x;
        float vertical = moveInput.y;

        Vector3 inputDirection = (transform.right * horizontal + transform.forward * vertical).normalized;

        float currentSpeed = walkSpeed;

        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (isSprinting && isMoving && currentStamina > 0 && !isExhausted)
        {
            currentSpeed = sprintSpeed;
        }

        float speed = inputDirection.magnitude > 0 ? currentSpeed : 0f;
        Vector3 targetVelocity = inputDirection * speed;

        if (enableInertia)
        {
            float smoothTime = inputDirection.magnitude > 0 ? 1f / acceleration : 1f / deceleration;
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.deltaTime * (1f / smoothTime));

            if (currentVelocity.magnitude > maxVelocity)
                currentVelocity = currentVelocity.normalized * maxVelocity;
        }
        else
        {
            currentVelocity = targetVelocity;
        }

        controller.Move(currentVelocity * Time.deltaTime);
        isMoving = inputDirection.magnitude > 0.1f;
    }

    // ============================================
    // 20. MIRADA CON RATÓN
    // ============================================
    void HandleMouseLook()
    {
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minYAngle, maxYAngle);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        yRotation += mouseX;
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    // ============================================
    // 21. HEAD BOB - BALANCEO DE CÁMARA
    // ============================================
    void HandleHeadBob()
    {
        bool isRunning = isSprinting && isMoving && currentStamina > 0 && !isExhausted && !isCrouching;

        float currentBobSpeed = bobSpeed;
        float currentBobAmount = bobAmount;

        if (isRunning)
        {
            currentBobSpeed = bobSpeed * sprintBobSpeedMultiplier;
            currentBobAmount = bobAmount * sprintBobAmountMultiplier;
        }
        else if (isCrouching && isMoving)
        {
            currentBobSpeed = bobSpeed * 0.6f;
            currentBobAmount = bobAmount * 0.5f;
        }

        if (isMoving)
        {
            bobTimer += Time.deltaTime * currentBobSpeed;

            float bobX = Mathf.Sin(bobTimer) * currentBobAmount * 0.5f;
            float bobY = Mathf.Sin(bobTimer * 2f) * currentBobAmount;

            Vector3 targetPosition = cameraInitialPosition + new Vector3(bobX, bobY, 0f);

            if (enableHorizontalBob && isRunning)
            {
                horizontalBobTimer += Time.deltaTime * currentBobSpeed * horizontalBobSpeedMultiplier;

                float horizontalBobX = Mathf.Sin(horizontalBobTimer * 0.7f + 1f) * horizontalBobAmount;
                targetPosition.x += horizontalBobX;

                float horizontalBobY = Mathf.Sin(horizontalBobTimer * 1.1f + 2.5f) * horizontalBobAmount * 0.3f;
                targetPosition.y += horizontalBobY;

                cameraHorizontalOffset = new Vector3(horizontalBobX, horizontalBobY, 0f);
            }
            else
            {
                cameraHorizontalOffset = Vector3.Lerp(cameraHorizontalOffset, Vector3.zero, Time.deltaTime * 5f);
                targetPosition += cameraHorizontalOffset;
            }

            // Combinar con SWAY
            if (enableSway && isMoving)
            {
                targetPosition += swayCurrentPosition;
            }

            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, targetPosition, Time.deltaTime * bobSmoothness * 10f);
        }
        else
        {
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, cameraInitialPosition, Time.deltaTime * 5f);
            bobTimer = 0f;
            horizontalBobTimer = 0f;
            cameraHorizontalOffset = Vector3.zero;
        }
    }

    // ============================================
    // 22. INCLINACIÓN DE CÁMARA (TILT)
    // ============================================
    void HandleTilt()
    {
        float targetTilt = 0f;
        float currentTiltAmount = tiltAmount;

        if (isSprinting && isMoving && currentStamina > 0 && !isExhausted && !isCrouching)
        {
            currentTiltAmount = tiltAmount * sprintTiltMultiplier;
        }
        else if (isCrouching && isMoving)
        {
            currentTiltAmount = tiltAmount * 0.5f;
        }

        if (isMoving)
        {
            float horizontalInput = moveInput.x;
            targetTilt = -horizontalInput * currentTiltAmount;
        }

        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSmoothness);

        Vector3 currentRotation = cameraTransform.localEulerAngles;
        currentRotation.z = currentTilt;
        cameraTransform.localEulerAngles = currentRotation;
    }

    // ============================================
    // 23. SONIDOS DE PASOS
    // ============================================
    void HandleFootsteps()
    {
        if (isMoving && footstepSounds != null && footstepSounds.Length > 0)
        {
            footstepTimer += Time.deltaTime;

            AudioClip[] currentSounds = footstepSounds;
            float currentInterval = footstepInterval;

            if (isCrouching)
            {
                currentInterval = footstepInterval * 1.2f;
            }
            else if (isSprinting && isMoving && currentStamina > 0 && !isExhausted)
            {
                if (sprintFootstepSounds != null && sprintFootstepSounds.Length > 0)
                {
                    currentSounds = sprintFootstepSounds;
                }
                currentInterval = sprintFootstepInterval;
            }

            float randomVariation = Random.Range(0.85f, 1.15f);
            currentInterval *= randomVariation;

            if (footstepTimer >= currentInterval)
            {
                footstepTimer = 0f;
                PlayFootstepSound(currentSounds);
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    void PlayFootstepSound(AudioClip[] sounds)
    {
        if (sounds == null || sounds.Length == 0) return;

        AudioClip clip = sounds[Random.Range(0, sounds.Length)];
        float pitch = Random.Range(footstepPitchMin, footstepPitchMax);
        float volume = footstepVolume * Random.Range(0.9f, 1.1f);

        if (isCrouching)
        {
            volume *= 0.6f;
        }
        else if (isSprinting && isMoving && currentStamina > 0 && !isExhausted)
        {
            volume *= 1.2f;
        }

        footstepAudioSource.pitch = pitch;
        footstepAudioSource.volume = Mathf.Clamp01(volume);
        footstepAudioSource.PlayOneShot(clip);
    }

    // ============================================
    // 24. SONIDO DE RESPIRACIÓN
    // ============================================

    void HandleBreathing()
    {
        if (breathingClip == null) return;

        // Determinar estado
        bool isRunning = isSprinting && isMoving && currentStamina > 0 && !isExhausted && !isCrouching;
        bool isWalking = isMoving && !isRunning;

        float targetVolume = breathingVolumeWalk;
        float targetPitch = breathingPitchWalk;

        if (isRunning)
        {
            // Corriendo: respiración agitada
            targetVolume = breathingVolumeSprint;
            targetPitch = breathingPitchSprint;
        }
        else if (isExhausted)
        {
            // ?? NUEVO: Agotado - respiración entrecortada y más fuerte
            targetVolume = Mathf.Lerp(breathingVolumeWalk, breathingVolumeSprint, 0.8f);
            targetPitch = Mathf.Lerp(breathingPitchWalk, breathingPitchSprint, 0.7f);

            // Añadir un pequeño "jadeo" o variación
            float jadeo = Mathf.Sin(Time.time * 1.5f) * 0.1f + 0.9f;
            targetPitch *= jadeo;
        }
        else if (isCrouching && isMoving)
        {
            // Agachado: respiración contenida
            targetVolume = breathingVolumeWalk * 0.8f;
            targetPitch = breathingPitchWalk * 0.9f;
        }
        else if (isWalking)
        {
            // Andando: respiración normal
            targetVolume = breathingVolumeWalk;
            targetPitch = breathingPitchWalk;
        }
        else
        {
            // Quieto: respiración muy suave
            targetVolume = breathingVolumeWalk * 0.5f;
            targetPitch = breathingPitchWalk * 0.9f;
        }

        // Transición suave
        currentBreathingVolume = Mathf.Lerp(currentBreathingVolume, targetVolume, Time.deltaTime * breathingTransitionSpeed);
        currentBreathingPitch = Mathf.Lerp(currentBreathingPitch, targetPitch, Time.deltaTime * breathingTransitionSpeed);

        breathingAudioSource.volume = currentBreathingVolume;
        breathingAudioSource.pitch = currentBreathingPitch;

        if (!breathingAudioSource.isPlaying && breathingClip != null)
        {
            breathingAudioSource.Play();
        }
    }

    // ============================================
    // 25. FOV - CAMBIO DE CAMPO DE VISIÓN (CON ZOOM)
    // ============================================
    void HandleFOV()
    {
        if (playerCamera == null) return;

        float baseFOV = normalFOV;

        if (isSprinting && isMoving && currentStamina > 0 && !isExhausted && !isCrouching)
        {
            baseFOV = sprintFOV;
        }
        else if (isCrouching)
        {
            baseFOV = normalFOV * 0.95f;
        }

        if (!isZoomed)
        {
            originalFOV = baseFOV;
            targetFOV = baseFOV;
        }
        else
        {
            targetFOV = zoomFOV;
        }

        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * zoomTransitionSpeed);
    }

    // ============================================
    // 26. ZOOM VHS - TOGGLE Y EFECTOS
    // ============================================
    public void ToggleZoom()
    {
        isZoomed = !isZoomed;

        if (isZoomed)
        {
            if (originalFOV == 0f)
            {
                originalFOV = playerCamera.fieldOfView;
            }

            if (zoomInSound != null)
            {
                zoomAudioSource.PlayOneShot(zoomInSound);
            }

            Debug.Log("?? Zoom IN - Efecto VHS activado");
        }
        else
        {
            if (zoomOutSound != null)
            {
                zoomAudioSource.PlayOneShot(zoomOutSound);
            }

            Debug.Log("?? Zoom OUT - Efecto VHS desactivado");
        }
    }

    // ============================================
    // 27. EFECTO DE ENFOQUE
    // ============================================
    void HandleFocusEffect()
    {
        if (isZoomed)
        {
            float focusOffset = Mathf.Sin(Time.time * 2f) * 0.05f;
        }
    }

    // ============================================
    // 27.5. CAMERA SWAY - BALANCEO DE CÁMARA
    // ============================================
    void HandleSway()
    {
        if (!enableSway) return;

        bool isMovingFast = isSprinting && isMoving && currentStamina > 0 && !isExhausted && !isCrouching;
        bool isMovingSlow = isMoving && !isMovingFast;

        float currentSpeedMultiplier = 1f;
        float currentAmountMultiplier = 1f;

        if (isMovingFast)
        {
            currentSpeedMultiplier = swaySpeedMultiplier;
            currentAmountMultiplier = swayAmountMultiplier;
        }
        else if (isCrouching && isMoving)
        {
            currentSpeedMultiplier = swaySpeedCrouchMultiplier;
            currentAmountMultiplier = swayAmountCrouchMultiplier;
        }
        else if (!isMoving)
        {
            swayCurrentAngle = Mathf.Lerp(swayCurrentAngle, 0f, Time.deltaTime * swayReturnSpeed);
            swayCurrentPosition = Vector3.Lerp(swayCurrentPosition, Vector3.zero, Time.deltaTime * swayReturnSpeed);
            return;
        }

        if (isMoving)
        {
            swayTimer += Time.deltaTime * swaySpeed * currentSpeedMultiplier;

            float swayAngle = Mathf.Sin(swayTimer) * swayAmount * currentAmountMultiplier;
            swayAngle = Mathf.Clamp(swayAngle, -swayMaxAngle, swayMaxAngle);

            float swayX = Mathf.Sin(swayTimer * 0.7f + 1.2f) * (swayAmount * 0.3f) * currentAmountMultiplier;
            float swayY = Mathf.Sin(swayTimer * 1.1f + 0.5f) * (swayAmount * 0.1f) * currentAmountMultiplier;

            swayTargetAngle = swayAngle;
            swayTargetPosition = new Vector3(swayX, swayY, 0f);
        }

        swayCurrentAngle = Mathf.Lerp(swayCurrentAngle, swayTargetAngle, Time.deltaTime * swaySmoothness);
        swayCurrentPosition = Vector3.Lerp(swayCurrentPosition, swayTargetPosition, Time.deltaTime * swaySmoothness);

        Vector3 currentRotation = cameraTransform.localEulerAngles;
        currentRotation.z = swayCurrentAngle;
        cameraTransform.localEulerAngles = currentRotation;
    }

    // ============================================
    // 28. CROUCH - AGACHARSE CON DETECCIÓN DE TECHO
    // ============================================

    bool CheckCeilingBlock()
    {
        if (!enableCeilingDetection) return false;

        Vector3 checkPosition = transform.position + Vector3.up * standingHeight;
        float radius = ceilingCheckRadius;
        float checkDistance = standingHeight - crouchHeight + ceilingCheckDistance;

        RaycastHit hit;
        if (Physics.SphereCast(
            checkPosition - Vector3.up * 0.1f,
            radius,
            Vector3.up,
            out hit,
            checkDistance,
            ceilingLayerMask
        ))
        {
            return true;
        }

        Debug.DrawRay(
            transform.position + Vector3.up * (crouchHeight + 0.1f),
            Vector3.up * (standingHeight - crouchHeight + ceilingCheckDistance),
            Color.red
        );

        if (Physics.Raycast(
            transform.position + Vector3.up * (crouchHeight + 0.1f),
            Vector3.up,
            out hit,
            standingHeight - crouchHeight + ceilingCheckDistance,
            ceilingLayerMask
        ))
        {
            return true;
        }

        return false;
    }

    void HandleCrouch()
    {
        if (!enableCrouch) return;

        if (isCrouching && isSprintPressed && isMoving)
        {
            ToggleCrouch();
            return;
        }

        float targetHeight = isCrouching ? crouchHeight : standingHeight;

        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        controller.height = currentHeight;

        Vector3 targetCameraPos = isCrouching ? crouchCameraTarget : cameraInitialPosition;
        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, targetCameraPos, Time.deltaTime * crouchTransitionSpeed);

        Vector3 controllerCenter = controller.center;
        controllerCenter.y = currentHeight / 2f;
        controller.center = controllerCenter;
    }

    public void ToggleCrouch()
    {
        if (!enableCrouch) return;
        if (isZoomed) return;

        if (isCrouching)
        {
            isBlockedByCeiling = CheckCeilingBlock();

            if (isBlockedByCeiling)
            {
                Debug.Log("?? ¡Bloqueado por techo! No puedes levantarte.");
                return;
            }
        }

        isCrouching = !isCrouching;
        isBlockedByCeiling = false;

        if (isCrouching)
        {
            if (crouchSound != null)
            {
                crouchAudioSource.PlayOneShot(crouchSound);
            }
            Debug.Log("?? Agachado");
        }
        else
        {
            if (standSound != null)
            {
                crouchAudioSource.PlayOneShot(standSound);
            }
            Debug.Log("?? De pie");
        }
    }

    // ============================================
    // 29. MÉTODOS PÚBLICOS (PARA ACCESO EXTERNO)
    // ============================================
    public void ToggleCursor(bool visible)
    {
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = visible;
    }

    public float GetCurrentStamina()
    {
        return currentStamina / maxStamina;
    }

    public bool IsSprinting()
    {
        return isSprinting && isMoving && currentStamina > 0 && !isExhausted && !isCrouching;
    }

    public bool IsExhausted()
    {
        return isExhausted;
    }

    public bool IsZoomed()
    {
        return isZoomed;
    }

    public float GetZoomFOV()
    {
        return zoomFOV;
    }

    public bool IsFocusActive()
    {
        return isZoomed && enableFocusEffect;
    }

    public bool IsCrouching()
    {
        return isCrouching;
    }

    public float GetCrouchProgress()
    {
        return Mathf.InverseLerp(crouchHeight, standingHeight, controller.height);
    }

    public bool IsBlockedByCeiling()
    {
        return isBlockedByCeiling;
    }

    public bool IsMoving()
    {
        return isMoving;
    }

    public bool IsMovingFast()
    {
        return isSprinting && isMoving && currentStamina > 0 && !isExhausted && !isCrouching;
    }

    public bool IsMovingSlow()
    {
        return isMoving && !IsMovingFast();
    }
}