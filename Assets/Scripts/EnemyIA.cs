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
    public float proximityDetectionRange = 10f;
    public LayerMask obstacleMask = -1;

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

    // ============================================
    // ?? JUMPSCARE
    // ============================================
    [Header("Jumpscare")]
    public float jumpscareDistance = 2.5f;
    public AudioClip jumpscareSound;
    [Range(0f, 1f)] public float jumpscareVolume = 1f;
    public float jumpscareDuration = 3f;

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

    // Variables para control de ruta
    private float stuckTimer = 0f;
    private float stuckThreshold = 2f;
    private Vector3 lastPosition;

    // Variables para movimiento directo (PERSECUCIÓN OPTIMIZADA)
    private float directMovementTimer = 0f;
    private float directMovementUpdateRate = 0.5f;
    private Vector3 lastDestination;

    // Para detectar si el jugador está quieto
    private Vector3 lastPlayerPosition;
    private float playerIdleTimer = 0f;
    private float playerIdleThreshold = 0.5f;

    // ?? Variables para Jumpscare
    private bool isJumpscareActive = false;
    private FirstPersonController playerController;
    private CharacterController playerCharacterController;
    private Collider playerCollider;
    private LockerHideSystem lockerSystemRef;
    private InteractionSystem interactionSystemRef;
    private Canvas crosshairCanvas;
    private bool wasPlayerControllerEnabled;
    private bool wasCharacterControllerEnabled;
    private bool wasPlayerColliderEnabled;
    private bool wasCrosshairActive;
    private bool wasLockerSystemEnabled;

    void Start()
    {
        Debug.Log($"?? Inicializando {gameObject.name}...");

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
            agent.autoBraking = false;
            agent.stoppingDistance = 0.5f;
            agent.isStopped = false;
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.enabled = true;
            lastPosition = transform.position;
            lastDestination = transform.position;
            lastPlayerPosition = player != null ? player.position : transform.position;
        }

        // ?? Obtener referencias para Jumpscare
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

        // ============================================
        // CONFIGURAR AUDIOSOURCES
        // ============================================
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

        // ?? AudioSource para Jumpscare
        jumpscareAudioSource = gameObject.AddComponent<AudioSource>();
        jumpscareAudioSource.loop = false;
        jumpscareAudioSource.playOnAwake = false;
        jumpscareAudioSource.spatialBlend = 0f;
        jumpscareAudioSource.volume = jumpscareVolume;
        jumpscareAudioSource.priority = 0;

        // ============================================
        // INICIALIZAR SONIDOS
        // ============================================
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

        // ?? Si el jumpscare está activo, no hacer nada más
        if (isJumpscareActive) return;

        DetectPlayer();

        // Detectar si el jugador está quieto
        CheckPlayerIdle();

        // ?? Verificar si el enemigo está lo suficientemente cerca para jumpscare
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
                // No hacer nada, la corrutina maneja todo
                break;
        }

        UpdateAnimations();
        UpdateAudio();
        DebugDraw();
    }

    // ============================================
    // ?? CHECK JUMBSCARE
    // ============================================
    void CheckJumpscare()
    {
        if (isJumpscareActive) return;
        if (player == null) return;
        if (isPlayerHiding) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // ?? Si está lo suficientemente cerca y el jugador no está escondido
        if (distanceToPlayer <= jumpscareDistance && !isPlayerHiding)
        {
            // ?? Verificar que hay línea de visión
            if (HasLineOfSightToPlayer())
            {
                StartJumpscare();
            }
        }
    }

    // ============================================
    // ?? INICIAR JUMBSCARE
    // ============================================
    void StartJumpscare()
    {
        if (isJumpscareActive) return;
        isJumpscareActive = true;

        Debug.Log($"?? JUMBSCARE ACTIVADO! {gameObject.name} atrapó al jugador");

        // ?? Cambiar estado
        currentState = EnemyState.Jumpscare;

        // ?? Detener el enemigo
        StopAgentImmediately();

        // ?? Detener todos los sonidos
        StopAllCoroutines();
        if (ambientAudioSource.isPlaying) ambientAudioSource.Stop();
        if (chaseAudioSource.isPlaying) chaseAudioSource.Stop();
        isChasePlaying = false;
        isAmbientPlaying = false;

        // ?? Reproducir sonido de jumpscare
        if (jumpscareSound != null)
        {
            jumpscareAudioSource.volume = jumpscareVolume;
            jumpscareAudioSource.PlayOneShot(jumpscareSound);
            Debug.Log($"?? Sonido de jumpscare reproducido");
        }

        // ?? Desactivar controles del jugador
        DisablePlayerControls();

        // ?? Rotar la cámara del jugador hacia el enemigo
        RotatePlayerToEnemy();

        // ?? Iniciar corrutina para esperar y reiniciar
        StartCoroutine(JumpscareSequence());
    }

    // ============================================
    // ?? DESACTIVAR CONTROLES DEL JUGADOR
    // ============================================
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

        // ?? Desactivar crosshair
        if (crosshairCanvas != null)
        {
            wasCrosshairActive = crosshairCanvas.gameObject.activeSelf;
            crosshairCanvas.gameObject.SetActive(false);
            Debug.Log("?? Crosshair DESACTIVADO");
        }

        // ?? Desactivar el sistema de interacción
        if (interactionSystemRef != null)
        {
            interactionSystemRef.enabled = false;
            Debug.Log("?? InteractionSystem DESACTIVADO");
        }
    }

    // ============================================
    // ?? REACTIVAR CONTROLES DEL JUGADOR
    // ============================================
    void EnablePlayerControls()
    {
        if (playerController != null)
        {
            playerController.enabled = wasPlayerControllerEnabled;
            Debug.Log("? PlayerController REACTIVADO");
        }

        if (playerCharacterController != null)
        {
            playerCharacterController.enabled = wasCharacterControllerEnabled;
            Debug.Log("? CharacterController REACTIVADO");
        }

        if (playerCollider != null)
        {
            playerCollider.enabled = wasPlayerColliderEnabled;
            Debug.Log("? PlayerCollider REACTIVADO");
        }

        if (lockerSystemRef != null)
        {
            lockerSystemRef.enabled = wasLockerSystemEnabled;
            Debug.Log("? LockerHideSystem REACTIVADO");
        }

        if (crosshairCanvas != null)
        {
            crosshairCanvas.gameObject.SetActive(wasCrosshairActive);
            Debug.Log("? Crosshair REACTIVADO");
        }

        if (interactionSystemRef != null)
        {
            interactionSystemRef.enabled = true;
            Debug.Log("? InteractionSystem REACTIVADO");
        }
    }

    // ============================================
    // ?? ROTAR JUGADOR HACIA EL ENEMIGO
    // ============================================
    void RotatePlayerToEnemy()
    {
        if (player == null) return;

        Vector3 directionToEnemy = (transform.position - player.position).normalized;
        directionToEnemy.y = 0;

        if (directionToEnemy.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToEnemy);
            player.rotation = targetRotation;
            Debug.Log($"?? Jugador rotado hacia el enemigo");
        }
    }

    // ============================================
    // ?? CORRUTINA DE JUMBSCARE
    // ============================================
    IEnumerator JumpscareSequence()
    {
        // Esperar la duración del jumpscare
        yield return new WaitForSeconds(jumpscareDuration);

        Debug.Log($"?? Reiniciando escena después de {jumpscareDuration} segundos...");

        // Reactivar controles antes de reiniciar
        EnablePlayerControls();

        // ?? Reiniciar la escena
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    // ============================================
    // DETECTAR JUGADOR (CON MÚLTIPLES RAYCASTS)
    // ============================================
    void DetectPlayer()
    {
        isPlayerHiding = false;
        if (lockerSystem != null)
        {
            isPlayerHiding = lockerSystem.IsHiding();
        }

        if (isPlayerHiding)
        {
            isPlayerVisible = false;
            isPlayerInRange = false;
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        isPlayerInRange = distanceToPlayer <= detectionRadius;
        isPlayerVisible = false;

        // Si el jugador está MUY CERCA, DETECTARLO SIEMPRE
        if (distanceToPlayer < proximityDetectionRange)
        {
            isPlayerVisible = true;
            lastKnownPlayerPosition = player.position;
            lastDirection = (player.position - transform.position).normalized;
            return;
        }

        // Detección normal por línea de visión con MÚLTIPLES RAYCASTS
        if (distanceToPlayer <= visionRange)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToPlayer);

            if (angle <= visionAngle * 0.5f)
            {
                // MÚLTIPLES ALTURAS para los raycasts
                float[] heights = { 0.2f, 0.5f, 1.0f, 1.5f, 2.0f, 2.5f };
                Vector3 rayOrigin = transform.position;

                foreach (float height in heights)
                {
                    Vector3 origin = rayOrigin + Vector3.up * height;
                    Vector3 direction = (player.position - origin).normalized;
                    float maxDistance = Vector3.Distance(origin, player.position) + 0.5f;

                    RaycastHit hit;
                    if (Physics.Raycast(origin, direction, out hit, maxDistance, obstacleMask))
                    {
                        if (hit.collider.CompareTag("Player"))
                        {
                            isPlayerVisible = true;
                            lastKnownPlayerPosition = player.position;
                            lastDirection = (player.position - transform.position).normalized;
                            break;
                        }
                    }
                }

                if (!isPlayerVisible)
                {
                    Vector3 center = transform.position + Vector3.up * 1.2f;
                    Vector3 direction = (player.position - center).normalized;
                    float distance = Vector3.Distance(center, player.position);

                    RaycastHit hit;
                    if (Physics.SphereCast(center, 0.8f, direction, out hit, distance, obstacleMask))
                    {
                        if (hit.collider.CompareTag("Player"))
                        {
                            isPlayerVisible = true;
                            lastKnownPlayerPosition = player.position;
                            lastDirection = (player.position - transform.position).normalized;
                        }
                    }
                }
            }
        }
    }

    // ============================================
    // DETECTAR SI EL JUGADOR ESTÁ QUIETO
    // ============================================
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

                if (isPlayerHiding)
                {
                    SelectNewWaypoint();
                    ChangeState(EnemyState.Walking);
                }
                else if (isPlayerVisible && !isPlayerHiding)
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

        if (isPlayerVisible && !isPlayerHiding)
        {
            ChangeState(EnemyState.Running);
            return;
        }

        idleTimer -= Time.deltaTime;

        if (idleTimer <= 0)
        {
            if (isPlayerVisible && !isPlayerHiding)
            {
                ChangeState(EnemyState.Running);
                return;
            }

            SelectNewWaypoint();
            ChangeState(EnemyState.Walking);
        }
    }

    // ============================================
    // UPDATE WALKING (PATRULLA FUNCIONAL - NO TOCAR)
    // ============================================
    void UpdateWalking()
    {
        if (isWaitingAfterHide) return;

        if (isPlayerVisible && !isPlayerHiding)
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
    // UPDATE RUNNING (PERSECUCIÓN OPTIMIZADA)
    // ============================================
    void UpdateRunning()
    {
        if (isPlayerHiding)
        {
            if (showDebugLogs) Debug.Log($"?? Jugador escondido en taquilla, esperando {hideWaitTime}s");

            lastKnownHidingPosition = player.position;
            isWaitingAfterHide = true;
            hideWaitTimer = hideWaitTime;
            playerLostTimer = 0f;
            isPlayerVisible = false;
            isPlayerInRange = false;

            // FORZAR DETENCIÓN INMEDIATA DEL AGENTE
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

                // Movimiento directo optimizado (sin rodeos)
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

    // ============================================
    // FORZAR DETENCIÓN INMEDIATA DEL AGENTE
    // ============================================
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

    // ============================================
    // MOVIMIENTO DIRECTO OPTIMIZADO (SIN RODEOS)
    // ============================================
    void UpdateDirectMovement()
    {
        Vector3 targetPosition = player.position;

        bool isPlayerIdle = playerIdleTimer >= playerIdleThreshold;

        directMovementTimer += Time.deltaTime;

        bool shouldUpdate = directMovementTimer >= directMovementUpdateRate || isPlayerIdle;

        if (shouldUpdate)
        {
            directMovementTimer = 0f;

            if (isPlayerVisible && HasLineOfSightToPlayer())
            {
                Vector3 directionToPlayer = (targetPosition - transform.position).normalized;
                directionToPlayer.y = 0;

                Vector3 destination = transform.position + directionToPlayer * 10f;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(destination, out hit, 15f, agent.areaMask))
                {
                    Vector3 navMeshTarget = hit.position;

                    if (Vector3.Distance(navMeshTarget, destination) < 3f)
                    {
                        agent.SetDestination(navMeshTarget);
                        lastDestination = navMeshTarget;
                        if (showDebugLogs && Time.frameCount % 30 == 0)
                            Debug.Log($"?? Línea recta a: {navMeshTarget}");
                        return;
                    }
                }
            }

            NavMeshHit hit2;
            if (NavMesh.SamplePosition(targetPosition, out hit2, 10f, agent.areaMask))
            {
                Vector3 navMeshTarget = hit2.position;

                if (Vector3.Distance(navMeshTarget, agent.destination) > 0.5f)
                {
                    agent.SetDestination(navMeshTarget);
                    lastDestination = navMeshTarget;
                    if (showDebugLogs && Time.frameCount % 60 == 0)
                        Debug.Log($"?? NavMesh a: {navMeshTarget}");
                }
            }
        }

        if (agent.remainingDistance < 0.5f && isPlayerVisible)
        {
            directMovementTimer = directMovementUpdateRate;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer < 2f && isPlayerVisible)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            directionToPlayer.y = 0;

            if (directionToPlayer.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
            }
        }
    }

    bool HasLineOfSightToPlayer()
    {
        if (player == null) return false;

        float[] heights = { 0.2f, 0.5f, 1.0f, 1.5f, 2.0f, 2.5f };

        foreach (float height in heights)
        {
            Vector3 origin = transform.position + Vector3.up * height;
            Vector3 direction = (player.position - origin).normalized;
            float distance = Vector3.Distance(origin, player.position);

            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, distance, obstacleMask))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    return true;
                }
            }
        }

        Vector3 center = transform.position + Vector3.up * 1.2f;
        Vector3 dir = (player.position - center).normalized;
        float dist = Vector3.Distance(center, player.position);

        RaycastHit sphereHit;
        if (Physics.SphereCast(center, 0.8f, dir, out sphereHit, dist, obstacleMask))
        {
            if (sphereHit.collider.CompareTag("Player"))
            {
                return true;
            }
        }

        return false;
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

        isPlayerVisible = false;
        isPlayerInRange = false;
        isPlayerHiding = true;
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
                // No hacer nada, la corrutina maneja todo
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

    // ============================================
    // SISTEMA DE SONIDOS
    // ============================================

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

    // ============================================
    // ACTUALIZAR AUDIO
    // ============================================
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
    }
}