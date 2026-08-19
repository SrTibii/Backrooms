using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class EnemyIA : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public NavMeshAgent agent;
    public Animator animator;
    public LockerHideSystem lockerSystem;

    [Header("Puntos de Patrulla")]
    public List<Transform> waypoints = new List<Transform>();
    public float idleTimeMin = 2f;
    public float idleTimeMax = 5f;

    [Header("Detección")]
    public float detectionRadius = 15f;
    public float visionRange = 25f;
    public float visionAngle = 120f;
    public float proximityDetectionRange = 2.5f;
    public LayerMask obstacleMask = -1;

    [Header("Raycast Heights (Múltiples alturas)")]
    public float[] raycastHeights = { 0.2f, 0.5f, 1.0f, 1.5f, 2.0f, 2.5f };
    public float sphereCastRadius = 0.8f;
    public float sphereCastHeight = 1.2f;

    [Header("Raycast Offset")]
    public float raycastForwardOffset = 0.5f;

    [Header("Debug Raycast")]
    public bool showRaycastDebug = true;
    public Color raycastColor = Color.green;
    public Color hitColor = Color.red;

    [Header("Persecución")]
    public float runSpeed = 5f;
    public float walkSpeed = 2f;
    public float lostPlayerTime = 5f;

    [Header("Escondite")]
    public float hideWaitTime = 3f;

    [Header("Sonidos del Enemigo")]
    public AudioClip[] ambientSounds;
    public AudioClip[] chaseSounds;

    [Header("Volumen")]
    [Range(0f, 1f)] public float ambientVolume = 0.8f;
    [Range(0f, 1f)] public float chaseVolume = 0.9f;

    [Header("Intervalo Ambiente")]
    public float minPauseBetweenAmbient = 2f;
    public float maxPauseBetweenAmbient = 5f;

    [Header("Audio Avanzado")]
    public float audioMinDistance = 1f;
    public float audioMaxDistance = 40f;
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
    public float chaseFadeTime = 0.5f;

    [Header("Jumpscare")]
    public float jumpscareDistance = 2.5f;
    public AudioClip jumpscareSound;
    [Range(0f, 1f)] public float jumpscareVolume = 1f;
    public float jumpscareDuration = 3f;
    public Camera jumpscareCamera;
    public UnityEngine.Rendering.Volume globalVolume;
    public UnityEngine.Rendering.Volume jumpscareVolumeOverride;

    // AudioSources
    private AudioSource ambientAudioSource;
    private AudioSource chaseAudioSource;
    private AudioSource jumpscareAudioSource;

    // Control de sonidos
    private float ambientTimer = 0f;
    private AudioClip currentAmbientClip;
    private AudioClip currentChaseClip;
    private bool isChasePlaying = false;
    private Coroutine currentChaseFadeCoroutine;
    private bool isAmbientPlaying = false;
    private bool isAmbientStopped = false;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private enum EnemyState { Idle, Walking, Running, Jumpscare }
    private EnemyState currentState = EnemyState.Idle;

    private Transform currentWaypoint;
    private int currentWaypointIndex = 0;
    private float idleTimer = 0f;

    private bool isPlayerVisible = false;
    private bool isPlayerInRange = false;
    private bool isPlayerHiding = false;
    private float playerLostTimer = 0f;

    private Vector3 lastKnownPlayerPosition;
    private Vector3 lastKnownHidingPosition;
    private Vector3 lastDirection;

    private bool isWaitingAfterHide = false;
    private float hideWaitTimer = 0f;

    private float stuckTimer = 0f;
    private float stuckThreshold = 2f;
    private Vector3 lastPosition;

    private float directMovementTimer = 0f;
    private float directMovementUpdateRate = 0.1f;
    private Vector3 lastDestination;

    private Vector3 lastPlayerPosition;
    private float playerIdleTimer = 0f;
    private float playerIdleThreshold = 0.3f;

    private bool isJumpscareActive = false;
    private FirstPersonController playerController;
    private CharacterController playerCharacterController;
    private Collider playerCollider;
    private LockerHideSystem lockerSystemRef;
    private InteractionSystem interactionSystemRef;
    private Canvas crosshairCanvas;
    private UnityEngine.Rendering.Universal.ColorAdjustments globalColorAdjustments;
    private float originalHueShift;
    private bool wasPlayerControllerEnabled;
    private bool wasCharacterControllerEnabled;
    private bool wasPlayerColliderEnabled;
    private bool wasCrosshairActive;
    private bool wasLockerSystemEnabled;

    private bool wasPlayerCameraActive;
    private bool wasJumpscareCameraActive;
    private Camera playerCamera;

    private bool detectionDisabled = false;
    private bool playerHidden = false;

    // ============================================
    // ?? CONTROL DE INVISIBILIDAD POR ZONA
    // ============================================
    private bool isPlayerInvisible = false;

    private int playerLayerMask = -1;
    private int enemyLayer = -1;

    void Start()
    {
        Debug.Log($"?? Inicializando {gameObject.name}...");

        enemyLayer = gameObject.layer;
        int playerLayer = LayerMask.NameToLayer("Player");

        DebugComponents();

        if (playerLayer == -1)
        {
            Debug.LogWarning("?? No existe la capa 'Player', usando 'Default' como fallback");
            playerLayer = LayerMask.NameToLayer("Default");
        }

        playerLayerMask = 1 << playerLayer;

        DebugLayerInfo();

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (lockerSystem == null)
        {
            lockerSystem = FindObjectOfType<LockerHideSystem>();
        }

        if (agent != null)
        {
            agent.speed = walkSpeed;
            agent.autoBraking = true;
            agent.stoppingDistance = 0.5f;
            agent.isStopped = false;
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.enabled = true;
            lastPosition = transform.position;
            lastDestination = transform.position;
            lastPlayerPosition = player != null ? player.position : transform.position;
            agent.acceleration = 20f;
            agent.angularSpeed = 360f;
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (player != null)
        {
            playerController = player.GetComponent<FirstPersonController>();
            playerCharacterController = player.GetComponent<CharacterController>();
            playerCollider = player.GetComponent<Collider>();
        }

        lockerSystemRef = FindObjectOfType<LockerHideSystem>();
        interactionSystemRef = FindObjectOfType<InteractionSystem>();

        if (interactionSystemRef != null)
        {
            var crosshairRect = interactionSystemRef?.GetComponentInChildren<RectTransform>();
            if (crosshairRect != null)
            {
                crosshairCanvas = crosshairRect.GetComponentInParent<Canvas>();
            }
        }

        if (globalVolume != null && globalVolume.profile != null)
        {
            if (globalVolume.profile.TryGet<UnityEngine.Rendering.Universal.ColorAdjustments>(out globalColorAdjustments))
            {
                originalHueShift = globalColorAdjustments.hueShift.value;
                Debug.Log($"? ColorAdjustments global encontrado. HueShift original: {originalHueShift}");
            }
        }

        if (jumpscareVolumeOverride != null)
        {
            jumpscareVolumeOverride.enabled = false;
            Debug.Log("?? Jumpscare Volume desactivado al inicio");
        }

        ambientAudioSource = gameObject.AddComponent<AudioSource>();
        ambientAudioSource.loop = false;
        ambientAudioSource.playOnAwake = false;
        ambientAudioSource.spatialBlend = 1f;
        ambientAudioSource.rolloffMode = rolloffMode;
        ambientAudioSource.minDistance = audioMinDistance;
        ambientAudioSource.maxDistance = audioMaxDistance;
        ambientAudioSource.volume = ambientVolume;
        ambientAudioSource.priority = 128;

        chaseAudioSource = gameObject.AddComponent<AudioSource>();
        chaseAudioSource.loop = true;
        chaseAudioSource.playOnAwake = false;
        chaseAudioSource.spatialBlend = 1f;
        chaseAudioSource.rolloffMode = rolloffMode;
        chaseAudioSource.minDistance = audioMinDistance;
        chaseAudioSource.maxDistance = audioMaxDistance;
        chaseAudioSource.volume = 0f;
        chaseAudioSource.priority = 100;

        jumpscareAudioSource = gameObject.AddComponent<AudioSource>();
        jumpscareAudioSource.loop = false;
        jumpscareAudioSource.playOnAwake = false;
        jumpscareAudioSource.spatialBlend = 0f;
        jumpscareAudioSource.volume = jumpscareVolume;
        jumpscareAudioSource.priority = 0;

        if (jumpscareCamera == null)
        {
            Debug.LogWarning($"?? {gameObject.name}: No se ha asignado JumpscareCamera. Se usará cámara por defecto.");
        }
        else
        {
            jumpscareCamera.gameObject.SetActive(false);
            AudioListener jumpscareListener = jumpscareCamera.GetComponent<AudioListener>();
            if (jumpscareListener != null)
            {
                jumpscareListener.enabled = false;
            }
        }

        PlayRandomAmbientSound();

        if (chaseSounds != null && chaseSounds.Length > 0)
        {
            currentChaseClip = chaseSounds[Random.Range(0, chaseSounds.Length)];
            chaseAudioSource.clip = currentChaseClip;
            chaseAudioSource.volume = 0f;
            chaseAudioSource.Play();
            isChasePlaying = true;
        }

        if (waypoints.Count > 0)
        {
            currentWaypointIndex = Random.Range(0, waypoints.Count);
            currentWaypoint = waypoints[currentWaypointIndex];
            currentState = EnemyState.Walking;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(currentWaypoint.position);
                Debug.Log($"?? Yendo a primer waypoint: {currentWaypoint.name}");
            }
            else
            {
                Debug.LogError($"? {gameObject.name} NO está en NavMesh");
                currentState = EnemyState.Idle;
                idleTimer = Random.Range(idleTimeMin, idleTimeMax);
            }
        }
        else
        {
            idleTimer = Random.Range(idleTimeMin, idleTimeMax);
            currentState = EnemyState.Idle;
            Debug.Log($"?? Idle inicial: {idleTimer:F1}s");
        }
    }

    void Update()
    {
        if (player == null) return;

        UpdateHidingStatus();

        if (isJumpscareActive) return;

        DetectPlayer();
        CheckPlayerIdle();
        CheckJumpscare();

        switch (currentState)
        {
            case EnemyState.Idle:
                UpdateIdle();
                break;
            case EnemyState.Walking:
                UpdateWalking();
                break;
            case EnemyState.Running:
                UpdateRunning();
                break;
            case EnemyState.Jumpscare:
                break;
        }

        UpdateAnimations();
        UpdateAudio();
        DebugDraw();
    }

    // ============================================
    // ?? MÉTODOS PARA CONTROL DE INVISIBILIDAD
    // ============================================
    public void SetPlayerHidden(bool hidden)
    {
        isPlayerInvisible = hidden;
        detectionDisabled = hidden;

        if (showDebugLogs)
            Debug.Log($"{(hidden ? "??" : "??")} Jugador {(hidden ? "INVISIBLE" : "VISIBLE")} para {gameObject.name}");
    }

    public bool IsPlayerHidden()
    {
        return isPlayerInvisible;
    }

    void UpdateHidingStatus()
    {
        if (detectionDisabled) return;

        if (lockerSystem != null)
        {
            bool wasHiding = isPlayerHiding;
            isPlayerHiding = lockerSystem.IsHiding();

            if (showDebugLogs && wasHiding != isPlayerHiding)
            {
                Debug.Log($"?? Estado de escondido cambiado: {wasHiding} -> {isPlayerHiding}");
            }
        }
    }

    void CheckJumpscare()
    {
        if (isJumpscareActive) return;
        if (player == null) return;
        if (isPlayerHiding) return;
        if (detectionDisabled) return;
        if (isPlayerInvisible) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= jumpscareDistance && !isPlayerHiding)
        {
            if (HasLineOfSightToPlayer())
            {
                StartJumpscare();
            }
        }
    }

    void StartJumpscare()
    {
        if (isJumpscareActive) return;
        isJumpscareActive = true;

        LeerNota leerNota = FindObjectOfType<LeerNota>();
        if (leerNota != null)
        {
            leerNota.CerrarNotaForzado();
            Debug.Log("?? Nota forzada a cerrar por jumpscare");
        }

        Debug.Log($"?? JUMBSCARE ACTIVADO! {gameObject.name} atrapó al jugador");

        currentState = EnemyState.Jumpscare;

        if (animator != null)
        {
            animator.SetTrigger("Jumpscare");
            Debug.Log("?? Animación de Jumpscare activada");
        }

        StopAgentImmediately();

        StopAllCoroutines();
        if (ambientAudioSource.isPlaying) ambientAudioSource.Stop();
        if (chaseAudioSource.isPlaying) chaseAudioSource.Stop();
        isChasePlaying = false;
        isAmbientPlaying = false;

        if (jumpscareSound != null)
        {
            jumpscareAudioSource.volume = jumpscareVolume;
            jumpscareAudioSource.PlayOneShot(jumpscareSound);
            Debug.Log($"?? Sonido de jumpscare reproducido");
        }

        ActivateJumpscareCamera();
        DisablePlayerControls();
        StartCoroutine(JumpscareSequence());
    }

    void ActivateJumpscareCamera()
    {
        if (jumpscareCamera == null)
        {
            Debug.LogWarning("?? No se ha asignado JumpscareCamera. Usando posición de la cámara principal.");

            if (playerCamera != null)
            {
                Vector3 targetPos = transform.position + Vector3.up * 1.5f + transform.forward * 1f;
                playerCamera.transform.position = targetPos;

                Vector3 directionToEnemy = (transform.position - playerCamera.transform.position).normalized;
                if (directionToEnemy.magnitude > 0.1f)
                {
                    playerCamera.transform.rotation = Quaternion.LookRotation(directionToEnemy);
                }
            }
            return;
        }

        if (jumpscareCamera == null)
        {
            Debug.LogWarning("?? jumpscareCamera ha sido destruido");
            return;
        }

        if (playerCamera != null)
        {
            wasPlayerCameraActive = playerCamera.gameObject.activeSelf;
            playerCamera.gameObject.SetActive(false);
            Debug.Log("?? Cámara del player DESACTIVADA");
        }

        wasJumpscareCameraActive = jumpscareCamera.gameObject.activeSelf;
        jumpscareCamera.gameObject.SetActive(true);

        if (jumpscareVolumeOverride != null && jumpscareCamera != null)
        {
            try
            {
                var existingVolume = jumpscareCamera.GetComponent<UnityEngine.Rendering.Volume>();
                if (existingVolume != null)
                {
                    existingVolume.profile = jumpscareVolumeOverride.profile;
                }
                else
                {
                    var newVolume = jumpscareCamera.gameObject.AddComponent<UnityEngine.Rendering.Volume>();
                    newVolume.profile = jumpscareVolumeOverride.profile;
                }

                var volumeComponent = jumpscareCamera.GetComponent<UnityEngine.Rendering.Volume>();
                if (volumeComponent != null)
                {
                    volumeComponent.enabled = true;
                }

                if (jumpscareVolumeOverride != null)
                {
                    jumpscareVolumeOverride.enabled = true;
                    Debug.Log("?? Jumpscare Volume ACTIVADO");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"?? Error al asignar Volume: {e.Message}");
            }
        }

        AudioListener jumpscareListener = jumpscareCamera.GetComponent<AudioListener>();
        if (jumpscareListener == null)
        {
            jumpscareListener = jumpscareCamera.gameObject.AddComponent<AudioListener>();
        }
        if (jumpscareListener != null)
        {
            jumpscareListener.enabled = true;
        }

        if (playerCamera != null)
        {
            AudioListener playerListener = playerCamera.GetComponent<AudioListener>();
            if (playerListener != null)
            {
                playerListener.enabled = false;
            }
        }

        Debug.Log($"?? Cámara de Jumpscare ACTIVADA: {jumpscareCamera.name}");
    }

    void DeactivateJumpscareCamera()
    {
        if (jumpscareCamera == null)
        {
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(true);
            }
            return;
        }

        if (jumpscareVolumeOverride != null)
        {
            try
            {
                jumpscareVolumeOverride.enabled = false;
                Debug.Log("?? Jumpscare Volume DESACTIVADO");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"?? Error al desactivar Volume: {e.Message}");
            }
        }

        if (jumpscareCamera != null)
        {
            try
            {
                var volume = jumpscareCamera.GetComponent<UnityEngine.Rendering.Volume>();
                if (volume != null)
                {
                    volume.enabled = false;
                }
                Debug.Log("?? Volume de jumpscare limpiado");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"?? Error al limpiar Volume: {e.Message}");
            }
        }

        if (jumpscareCamera != null)
        {
            jumpscareCamera.gameObject.SetActive(false);
        }

        if (jumpscareCamera != null)
        {
            AudioListener jumpscareListener = jumpscareCamera.GetComponent<AudioListener>();
            if (jumpscareListener != null)
            {
                jumpscareListener.enabled = false;
            }
        }

        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);

            AudioListener playerListener = playerCamera.GetComponent<AudioListener>();
            if (playerListener != null)
            {
                playerListener.enabled = true;
            }

            Debug.Log("? Cámara del player REACTIVADA");
        }

        Debug.Log($"?? Cámara de Jumpscare DESACTIVADA");
    }

    void DisablePlayerControls()
    {
        if (playerController != null)
        {
            wasPlayerControllerEnabled = playerController.enabled;
            playerController.enabled = false;
            Debug.Log("?? PlayerController DESACTIVADO");
        }

        if (playerCharacterController != null)
        {
            wasCharacterControllerEnabled = playerCharacterController.enabled;
            playerCharacterController.enabled = false;
            Debug.Log("?? CharacterController DESACTIVADO");
        }

        if (playerCollider != null)
        {
            wasPlayerColliderEnabled = playerCollider.enabled;
            playerCollider.enabled = false;
            Debug.Log("?? PlayerCollider DESACTIVADO");
        }

        if (lockerSystemRef != null)
        {
            wasLockerSystemEnabled = lockerSystemRef.enabled;
            lockerSystemRef.enabled = false;
            Debug.Log("?? LockerHideSystem DESACTIVADO");
        }

        if (crosshairCanvas != null)
        {
            wasCrosshairActive = crosshairCanvas.gameObject.activeSelf;
            crosshairCanvas.gameObject.SetActive(false);
            Debug.Log("?? Crosshair DESACTIVADO");
        }

        if (interactionSystemRef != null)
        {
            interactionSystemRef.enabled = false;
            Debug.Log("?? InteractionSystem DESACTIVADO");
        }
    }

    IEnumerator JumpscareSequence()
    {
        yield return new WaitForSeconds(jumpscareDuration);

        Debug.Log($"?? Reiniciando escena después de {jumpscareDuration} segundos...");

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    // ============================================
    // ?? DETECTAR JUGADOR - CON INVISIBILIDAD
    // ============================================
    void DetectPlayer()
    {
        // SI EL JUGADOR ESTÁ EN ZONA INVISIBLE, NO DETECTAR
        if (detectionDisabled || playerHidden || isPlayerInvisible)
        {
            isPlayerVisible = false;
            isPlayerInRange = false;

            // Si estábamos persiguiendo y el jugador se volvió invisible, perderlo
            if (currentState == EnemyState.Running)
            {
                playerLostTimer = lostPlayerTime; // Forzar pérdida inmediata
            }
            return;
        }

        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        isPlayerInRange = distanceToPlayer <= detectionRadius;
        isPlayerVisible = false;

        // PROXIMIDAD
        if (distanceToPlayer < proximityDetectionRange)
        {
            isPlayerVisible = true;
            lastKnownPlayerPosition = player.position;
            lastDirection = (player.position - transform.position).normalized;
            Debug.Log($"?? DETECTADO por PROXIMIDAD: {distanceToPlayer:F1}m");
            return;
        }

        // FUERA DE RANGO
        if (distanceToPlayer > visionRange) return;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        // FUERA DE ÁNGULO
        if (angle > visionAngle * 0.5f)
        {
            if (showDebugLogs && Time.frameCount % 30 == 0)
                Debug.Log($"? Jugador fuera del ángulo: {angle:F1}°");
            return;
        }

        // ============================================
        // SOLO USAR ALTURAS ALTAS (1.5, 2.0, 2.5)
        // ============================================
        float[] validHeights = { 1.5f, 2.0f, 2.5f };
        bool foundPlayer = false;

        foreach (float height in validHeights)
        {
            Vector3 start = transform.position + Vector3.up * height + transform.forward * raycastForwardOffset;
            Vector3 end = player.position + Vector3.up * 0.5f;

            if (showRaycastDebug)
            {
                Debug.DrawLine(start, end, Color.yellow, 0.5f);
            }

            RaycastHit hit;

            // VERIFICAR OBSTÁCULO
            if (Physics.Linecast(start, end, out hit, obstacleMask))
            {
                if (!hit.collider.CompareTag("Player"))
                {
                    if (showRaycastDebug)
                    {
                        Debug.DrawLine(start, hit.point, Color.red, 0.5f);
                    }
                    continue;
                }
            }

            // VERIFICAR JUGADOR
            if (Physics.Linecast(start, end, out RaycastHit playerHit, playerLayerMask))
            {
                if (playerHit.collider.CompareTag("Player"))
                {
                    isPlayerVisible = true;
                    lastKnownPlayerPosition = player.position;
                    lastDirection = directionToPlayer;
                    foundPlayer = true;
                    Debug.Log($"? JUGADOR DETECTADO desde altura {height}m");
                    break;
                }
            }
        }

        if (!foundPlayer && showDebugLogs && Time.frameCount % 30 == 0)
        {
            Debug.Log("? No se detectó al jugador");
        }
    }

    // ============================================
    // ?? HAS LINE OF SIGHT - CON INVISIBILIDAD
    // ============================================
    bool HasLineOfSightToPlayer()
    {
        if (player == null) return false;
        if (isPlayerInvisible) return false;

        float[] validHeights = { 1.5f, 2.0f, 2.5f };

        foreach (float height in validHeights)
        {
            Vector3 start = transform.position + Vector3.up * height + transform.forward * raycastForwardOffset;
            Vector3 end = player.position + Vector3.up * 0.5f;

            if (Physics.Linecast(start, end, out RaycastHit hit, obstacleMask))
            {
                if (!hit.collider.CompareTag("Player"))
                {
                    continue;
                }
            }

            if (Physics.Linecast(start, end, out RaycastHit playerHit, playerLayerMask))
            {
                if (playerHit.collider.CompareTag("Player"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    void CheckPlayerIdle()
    {
        if (player == null) return;

        float playerMovement = Vector3.Distance(player.position, lastPlayerPosition);

        if (playerMovement < 0.1f)
        {
            playerIdleTimer += Time.deltaTime;
        }
        else
        {
            playerIdleTimer = 0f;
            lastPlayerPosition = player.position;
        }
    }

    void UpdateIdle()
    {
        if (isWaitingAfterHide)
        {
            hideWaitTimer -= Time.deltaTime;

            if (hideWaitTimer <= 0)
            {
                isWaitingAfterHide = false;
                ReactivateAmbientSound();

                if (lockerSystem != null && !detectionDisabled)
                {
                    isPlayerHiding = lockerSystem.IsHiding();
                }

                if (isPlayerHiding || detectionDisabled)
                {
                    if (showDebugLogs) Debug.Log($"?? El jugador sigue escondido, volviendo a patrullar");
                    SelectNewWaypoint();
                    ChangeState(EnemyState.Walking);
                    return;
                }

                if (isPlayerVisible && !isPlayerHiding && !detectionDisabled)
                {
                    ChangeState(EnemyState.Running);
                }
                else
                {
                    SelectNewWaypoint();
                    ChangeState(EnemyState.Walking);
                }
            }
            return;
        }

        if (isPlayerVisible && !isPlayerHiding && !detectionDisabled)
        {
            ChangeState(EnemyState.Running);
            return;
        }

        idleTimer -= Time.deltaTime;

        if (idleTimer <= 0)
        {
            if (isPlayerVisible && !isPlayerHiding && !detectionDisabled)
            {
                ChangeState(EnemyState.Running);
                return;
            }

            SelectNewWaypoint();
            ChangeState(EnemyState.Walking);
        }
    }

    void UpdateWalking()
    {
        if (isWaitingAfterHide) return;

        if (isPlayerVisible && !isPlayerHiding && !detectionDisabled && !isPlayerInvisible)
        {
            ChangeState(EnemyState.Running);
            return;
        }

        if (currentWaypoint == null)
        {
            SelectNewWaypoint();
            return;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                idleTimer = Random.Range(idleTimeMin, idleTimeMax);
                ChangeState(EnemyState.Idle);
            }
        }
    }

    // ============================================
    // ?? UPDATE RUNNING - CON INVISIBILIDAD
    // ============================================
    void UpdateRunning()
    {
        // SI EL JUGADOR ES INVISIBLE, PERDERLO INMEDIATAMENTE
        if (isPlayerInvisible)
        {
            if (showDebugLogs) Debug.Log($"?? Jugador invisible - Perdiendo persecución");
            playerLostTimer = lostPlayerTime;
            isPlayerVisible = false;
            isPlayerInRange = false;
            ReactivateAmbientSound();
            SelectNewWaypoint();
            ChangeState(EnemyState.Walking);
            return;
        }

        if (lockerSystem != null && !detectionDisabled)
        {
            isPlayerHiding = lockerSystem.IsHiding();
        }

        if (detectionDisabled || isPlayerHiding)
        {
            if (showDebugLogs) Debug.Log($"?? Jugador escondido, esperando {hideWaitTime}s");

            lastKnownHidingPosition = player.position;
            isWaitingAfterHide = true;
            hideWaitTimer = hideWaitTime;
            playerLostTimer = 0f;
            isPlayerVisible = false;
            isPlayerInRange = false;

            StopAgentImmediately();
            ChangeState(EnemyState.Idle);

            Vector3 directionToHide = (lastKnownHidingPosition - transform.position).normalized;
            directionToHide.y = 0;
            if (directionToHide.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToHide);
                transform.rotation = targetRotation;
            }

            return;
        }

        if (isPlayerVisible || isPlayerInRange)
        {
            playerLostTimer = 0f;
            lastKnownPlayerPosition = player.position;
            lastDirection = (player.position - transform.position).normalized;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.speed = runSpeed;
                agent.isStopped = false;
                UpdateDirectMovement();
            }

            return;
        }

        playerLostTimer += Time.deltaTime;

        if (playerLostTimer < lostPlayerTime)
        {
            if (showDebugLogs && Time.frameCount % 30 == 0)
            {
                Debug.Log($"?? Buscando... {playerLostTimer:F1}s/{lostPlayerTime}s");
            }

            if (agent != null && agent.isOnNavMesh)
            {
                agent.speed = runSpeed;
                agent.isStopped = false;

                if (!agent.hasPath || agent.remainingDistance < 0.5f)
                {
                    Vector3 newDestination = transform.position + lastDirection * 5f;

                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(newDestination, out hit, 10f, agent.areaMask))
                    {
                        agent.SetDestination(hit.position);
                        if (showDebugLogs) Debug.Log($"?? Nuevo destino en dirección: {hit.position}");
                    }
                    else
                    {
                        agent.SetDestination(lastKnownPlayerPosition);
                    }
                }
            }
        }
        else
        {
            if (showDebugLogs) Debug.Log($"?? Perdí al jugador después de {lostPlayerTime}s");

            ReactivateAmbientSound();
            SelectNewWaypoint();
            ChangeState(EnemyState.Walking);
        }

        CheckIfStuck();
    }

    void UpdateDirectMovement()
    {
        if (agent == null || player == null) return;

        if (isPlayerVisible)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            directionToPlayer.y = 0;

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer < 1.5f)
            {
                agent.velocity = directionToPlayer * runSpeed;
                agent.ResetPath();

                if (directionToPlayer.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
                }
                return;
            }

            NavMeshHit hit;
            if (NavMesh.SamplePosition(player.position, out hit, 10f, agent.areaMask))
            {
                Vector3 target = hit.position;

                if (Vector3.Distance(target, lastDestination) > 0.5f)
                {
                    agent.SetDestination(target);
                    lastDestination = target;

                    if (showDebugLogs && Time.frameCount % 60 == 0)
                        Debug.Log($"?? Actualizando ruta a: {target}");
                }
            }

            if (agent.remainingDistance > 1f && agent.velocity.magnitude < 0.5f)
            {
                agent.velocity = directionToPlayer * runSpeed * 0.5f;
            }

            Vector3 desiredDirection = (player.position - transform.position).normalized;
            desiredDirection.y = 0;
            if (desiredDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(desiredDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
            }
        }
        else
        {
            if (!agent.hasPath || agent.remainingDistance < 0.5f)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(lastKnownPlayerPosition, out hit, 10f, agent.areaMask))
                {
                    agent.SetDestination(hit.position);
                    lastDestination = hit.position;
                }
            }
        }

        if (agent.remainingDistance > 2f && agent.velocity.magnitude < 0.1f)
        {
            directMovementTimer += Time.deltaTime;
            if (directMovementTimer > 1.5f)
            {
                directMovementTimer = 0f;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(player.position, out hit, 10f, agent.areaMask))
                {
                    agent.SetDestination(hit.position);
                    lastDestination = hit.position;
                    if (showDebugLogs) Debug.Log($"?? Forzando recálculo de ruta");
                }
            }
        }
        else
        {
            directMovementTimer = 0f;
        }
    }

    void StopAgentImmediately()
    {
        if (agent == null) return;

        agent.velocity = Vector3.zero;
        agent.isStopped = true;
        agent.ResetPath();
        agent.updatePosition = true;
        agent.updateRotation = true;

        if (showDebugLogs) Debug.Log($"?? Agente detenido inmediatamente");
    }

    bool CanReachDestination(Vector3 destination)
    {
        if (agent == null || !agent.isOnNavMesh) return false;

        NavMeshPath path = new NavMeshPath();
        if (agent.CalculatePath(destination, path))
        {
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                return true;
            }
        }
        return false;
    }

    Vector3 FindClosestNavMeshPosition(Vector3 position)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 10f, agent.areaMask))
        {
            return hit.position;
        }
        return Vector3.zero;
    }

    void CheckIfStuck()
    {
        if (agent == null) return;

        if (agent.hasPath && agent.remainingDistance > agent.stoppingDistance)
        {
            if (Vector3.Distance(transform.position, lastPosition) < 0.1f)
            {
                stuckTimer += Time.deltaTime;

                if (stuckTimer >= stuckThreshold)
                {
                    if (showDebugLogs) Debug.Log($"?? Agente atascado! Buscando nueva ruta...");

                    Vector3 randomDirection = Random.insideUnitSphere * 5f;
                    randomDirection += transform.position;
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(randomDirection, out hit, 5f, agent.areaMask))
                    {
                        agent.SetDestination(hit.position);
                        if (showDebugLogs) Debug.Log($"?? Nueva ruta de escape: {hit.position}");
                    }

                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
                lastPosition = transform.position;
            }
        }
        else
        {
            stuckTimer = 0f;
            lastPosition = transform.position;
        }
    }

    public void OnPlayerHid()
    {
        if (showDebugLogs) Debug.Log($"?? El jugador se ha escondido en una taquilla - {gameObject.name}");

        isPlayerHiding = true;
        isPlayerVisible = false;
        isPlayerInRange = false;
        playerLostTimer = 0f;

        if (player != null)
        {
            lastKnownHidingPosition = player.position;
        }

        isWaitingAfterHide = true;
        hideWaitTimer = hideWaitTime;

        StopAgentImmediately();
        ChangeState(EnemyState.Idle);

        if (lastKnownHidingPosition != Vector3.zero)
        {
            Vector3 directionToHide = (lastKnownHidingPosition - transform.position).normalized;
            directionToHide.y = 0;
            if (directionToHide.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToHide);
                transform.rotation = targetRotation;
            }
        }
    }

    void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        switch (currentState)
        {
            case EnemyState.Idle:
                if (!isWaitingAfterHide)
                {
                    idleTimer = Random.Range(idleTimeMin, idleTimeMax);
                }
                if (agent != null)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }
                if (showDebugLogs) Debug.Log($"?? IDLE por {(isWaitingAfterHide ? hideWaitTimer : idleTimer):F1}s");

                StopChaseSound();
                break;

            case EnemyState.Walking:
                if (agent != null)
                {
                    agent.speed = walkSpeed;
                    agent.isStopped = false;
                    if (currentWaypoint != null && agent.isOnNavMesh)
                    {
                        agent.SetDestination(currentWaypoint.position);
                    }
                }
                if (showDebugLogs) Debug.Log($"?? WALKING a {currentWaypoint?.name}");

                StopChaseSound();
                break;

            case EnemyState.Running:
                if (agent != null)
                {
                    agent.speed = runSpeed;
                    agent.isStopped = false;
                    if (player != null && agent.isOnNavMesh)
                    {
                        if (CanReachDestination(player.position))
                        {
                            agent.SetDestination(player.position);
                            lastDestination = player.position;
                        }
                        else
                        {
                            Vector3 closestPoint = FindClosestNavMeshPosition(player.position);
                            if (closestPoint != Vector3.zero)
                            {
                                agent.SetDestination(closestPoint);
                                lastDestination = closestPoint;
                            }
                        }
                    }
                }
                if (showDebugLogs) Debug.Log($"?? RUNNING al jugador");

                StartChaseSound();
                break;

            case EnemyState.Jumpscare:
                break;
        }
    }

    void SelectNewWaypoint()
    {
        if (waypoints.Count == 0) return;

        int newIndex = currentWaypointIndex;
        int attempts = 0;
        while (newIndex == currentWaypointIndex && waypoints.Count > 1 && attempts < 50)
        {
            newIndex = Random.Range(0, waypoints.Count);
            attempts++;
        }

        currentWaypointIndex = newIndex;
        currentWaypoint = waypoints[currentWaypointIndex];

        if (showDebugLogs)
            Debug.Log($"?? Nuevo waypoint: {currentWaypoint.name}");
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        float speed = agent != null ? agent.velocity.magnitude : 0f;
        float normalizedSpeed = 0f;

        if (currentState == EnemyState.Running)
        {
            normalizedSpeed = Mathf.Clamp01(speed / runSpeed);
        }
        else if (currentState == EnemyState.Walking)
        {
            normalizedSpeed = Mathf.Clamp01(speed / walkSpeed);
        }

        if (currentState == EnemyState.Idle || currentState == EnemyState.Jumpscare)
        {
            normalizedSpeed = 0f;
        }

        animator.SetFloat("Speed", normalizedSpeed);
        animator.SetBool("IsRunning", currentState == EnemyState.Running);
        animator.SetInteger("State", (int)currentState);
    }

    void PlayRandomAmbientSound()
    {
        if (ambientSounds == null || ambientSounds.Length == 0)
        {
            if (showDebugLogs) Debug.LogWarning("?? No hay sonidos ambientales asignados");
            return;
        }

        AudioClip newClip = ambientSounds[Random.Range(0, ambientSounds.Length)];
        int attempts = 0;
        while (newClip == currentAmbientClip && ambientSounds.Length > 1 && attempts < 20)
        {
            newClip = ambientSounds[Random.Range(0, ambientSounds.Length)];
            attempts++;
        }

        currentAmbientClip = newClip;

        ambientAudioSource.clip = currentAmbientClip;
        ambientAudioSource.loop = false;
        ambientAudioSource.volume = ambientVolume;
        ambientAudioSource.Play();
        isAmbientPlaying = true;
        isAmbientStopped = false;

        float clipDuration = currentAmbientClip.length;
        float pauseBetweenSounds = Random.Range(minPauseBetweenAmbient, maxPauseBetweenAmbient);
        ambientTimer = clipDuration + pauseBetweenSounds;

        if (showDebugLogs)
            Debug.Log($"?? Ambiente: {currentAmbientClip.name} | Duración: {clipDuration:F1}s | Pausa: {pauseBetweenSounds:F1}s | Total: {ambientTimer:F1}s");
    }

    void ReactivateAmbientSound()
    {
        if (showDebugLogs) Debug.Log($"?? Reactivando sonidos ambientales...");

        if (isAmbientStopped || !ambientAudioSource.isPlaying)
        {
            isAmbientStopped = false;
            isAmbientPlaying = false;
            ambientTimer = 0f;
            PlayRandomAmbientSound();
        }
    }

    void StartChaseSound()
    {
        if (chaseSounds == null || chaseSounds.Length == 0)
        {
            if (showDebugLogs) Debug.LogWarning("?? No hay sonidos de persecución asignados");
            return;
        }

        if (isChasePlaying && chaseAudioSource.isPlaying && chaseAudioSource.volume > 0.5f)
        {
            return;
        }

        StopAmbientSoundImmediate();

        AudioClip newClip = chaseSounds[Random.Range(0, chaseSounds.Length)];
        int attempts = 0;
        while (newClip == currentChaseClip && chaseSounds.Length > 1 && attempts < 20)
        {
            newClip = chaseSounds[Random.Range(0, chaseSounds.Length)];
            attempts++;
        }

        currentChaseClip = newClip;

        chaseAudioSource.clip = currentChaseClip;
        chaseAudioSource.loop = true;

        if (!chaseAudioSource.isPlaying)
        {
            chaseAudioSource.Play();
            isChasePlaying = true;
        }

        if (currentChaseFadeCoroutine != null)
            StopCoroutine(currentChaseFadeCoroutine);
        currentChaseFadeCoroutine = StartCoroutine(FadeChaseVolume(chaseVolume, chaseFadeTime));

        if (showDebugLogs)
            Debug.Log($"?? Iniciando persecución: {currentChaseClip.name} (en bucle)");
    }

    void StopChaseSound()
    {
        if (currentChaseFadeCoroutine != null)
            StopCoroutine(currentChaseFadeCoroutine);
        currentChaseFadeCoroutine = StartCoroutine(FadeChaseVolume(0f, chaseFadeTime));
    }

    void StopAmbientSoundImmediate()
    {
        if (ambientAudioSource.isPlaying)
        {
            ambientAudioSource.Stop();
        }
        isAmbientPlaying = false;
        isAmbientStopped = true;
        if (showDebugLogs) Debug.Log($"?? Ambiente detenido inmediatamente");
    }

    IEnumerator FadeChaseVolume(float targetVolume, float duration)
    {
        float startVolume = chaseAudioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            chaseAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        chaseAudioSource.volume = targetVolume;

        if (targetVolume == 0f)
        {
            if (chaseAudioSource.isPlaying)
            {
                chaseAudioSource.Pause();
            }
            isChasePlaying = false;

            if (!isWaitingAfterHide && !isPlayerHiding)
            {
                ReactivateAmbientSound();
            }
        }
        else
        {
            if (!chaseAudioSource.isPlaying)
            {
                chaseAudioSource.Play();
                isChasePlaying = true;
            }
        }
    }

    void UpdateAudio()
    {
        if (currentState != EnemyState.Running)
        {
            if (!ambientAudioSource.isPlaying && isAmbientPlaying)
            {
                isAmbientPlaying = false;
            }

            if (!isAmbientPlaying && !isAmbientStopped)
            {
                ambientTimer -= Time.deltaTime;

                if (ambientTimer <= 0f)
                {
                    PlayRandomAmbientSound();
                }
            }

            if (isAmbientStopped && currentState != EnemyState.Running)
            {
                ReactivateAmbientSound();
            }
        }
        else
        {
            if (ambientAudioSource.isPlaying)
            {
                ambientAudioSource.Stop();
                isAmbientPlaying = false;
                isAmbientStopped = true;
            }
        }

        if (currentState == EnemyState.Running)
        {
            if (!isChasePlaying || !chaseAudioSource.isPlaying)
            {
                StartChaseSound();
            }
        }
    }

    void DebugDraw()
    {
        if (!showDebugLogs) return;

        DrawWireCircle(transform.position, detectionRadius, Color.yellow);
        DrawWireCircle(transform.position, visionRange, Color.blue);

        Vector3 forward = transform.forward;
        Vector3 left = Quaternion.Euler(0, -visionAngle * 0.5f, 0) * forward;
        Vector3 right = Quaternion.Euler(0, visionAngle * 0.5f, 0) * forward;

        Debug.DrawRay(transform.position + Vector3.up * 0.5f, left * visionRange, Color.green);
        Debug.DrawRay(transform.position + Vector3.up * 0.5f, right * visionRange, Color.green);

        if (isPlayerVisible && player != null)
        {
            Debug.DrawLine(transform.position + Vector3.up * 0.5f, player.position, Color.red);
        }

        if (currentWaypoint != null)
        {
            Debug.DrawLine(transform.position, currentWaypoint.position, Color.cyan);
        }

        if (currentState == EnemyState.Running && !isPlayerVisible && playerLostTimer < lostPlayerTime)
        {
            Debug.DrawRay(transform.position + Vector3.up * 0.5f, lastDirection * 5f, Color.magenta);
        }
    }

    void DrawWireCircle(Vector3 center, float radius, Color color)
    {
        int segments = 36;
        float angleStep = 360f / segments;

        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Debug.DrawLine(prevPoint, newPoint, color);
            prevPoint = newPoint;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, jumpscareDistance);

        Gizmos.color = Color.cyan;
        foreach (var wp in waypoints)
        {
            if (wp != null)
                Gizmos.DrawSphere(wp.position, 0.3f);
        }

        if (currentWaypoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(currentWaypoint.position, 0.5f);
        }

        Gizmos.color = Color.green;
        Vector3 forward = transform.forward;
        Vector3 left = Quaternion.Euler(0, -visionAngle * 0.5f, 0) * forward;
        Vector3 right = Quaternion.Euler(0, visionAngle * 0.5f, 0) * forward;

        Gizmos.DrawLine(transform.position + Vector3.up * 0.5f,
                        transform.position + Vector3.up * 0.5f + left * visionRange);
        Gizmos.DrawLine(transform.position + Vector3.up * 0.5f,
                        transform.position + Vector3.up * 0.5f + right * visionRange);

        if (jumpscareCamera != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(jumpscareCamera.transform.position, 0.3f);
            Gizmos.DrawLine(transform.position, jumpscareCamera.transform.position);

            Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
            Gizmos.DrawFrustum(
                jumpscareCamera.transform.position,
                jumpscareCamera.fieldOfView,
                2f,
                0.1f,
                jumpscareCamera.aspect
            );
        }
    }

    private void OnDestroy()
    {
        jumpscareVolumeOverride = null;
        globalVolume = null;
        jumpscareCamera = null;
        playerCamera = null;
    }

    void DebugLayerInfo()
    {
        if (player == null) return;

        int playerLayer = LayerMask.NameToLayer("Player");

        Debug.Log($"=== INFORMACIÓN DE CAPAS ===");
        Debug.Log($"Enemigo: {gameObject.name} | Layer: {gameObject.layer} ({LayerMask.LayerToName(gameObject.layer)})");
        Debug.Log($"Jugador: {player.name} | Layer: {player.gameObject.layer} ({LayerMask.LayerToName(player.gameObject.layer)})");
        Debug.Log($"Tag del jugador: {player.tag}");
        Debug.Log($"Capa 'Player' existe: {(playerLayer != -1 ? "SÍ" : "NO")}");
        if (playerLayer != -1)
            Debug.Log($"LayerMask usado: {1 << playerLayer}");
        Debug.Log($"=============================");
    }

    void DebugComponents()
    {
        Debug.Log($"=== COMPONENTES DE {gameObject.name} ===");

        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            Debug.Log($"? Collider: {collider.GetType().Name} | Enabled: {collider.enabled} | IsTrigger: {collider.isTrigger}");
        }
        else
        {
            Debug.LogError($"? {gameObject.name} NO tiene Collider!");
        }

        if (agent != null)
        {
            Debug.Log($"? NavMeshAgent: {agent.GetType().Name} | Enabled: {agent.enabled} | OnNavMesh: {agent.isOnNavMesh}");
        }

        if (player != null)
        {
            Collider playerCol = player.GetComponent<Collider>();
            if (playerCol != null)
            {
                Debug.Log($"? Player Collider: {playerCol.GetType().Name} | Enabled: {playerCol.enabled} | IsTrigger: {playerCol.isTrigger}");
            }
            else
            {
                Debug.LogError($"? Player NO tiene Collider!");
            }
        }
        Debug.Log($"=============================");
    }

    // ============================================
    // ?? MÉTODO PARA SABER SI ESTÁ EN JUMPSCARE
    // ============================================
    public bool EstaEnJumpscare()
    {
        return isJumpscareActive;
    }
}