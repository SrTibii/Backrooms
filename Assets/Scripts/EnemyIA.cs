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

    // AudioSources
    private AudioSource ambientAudioSource;
    private AudioSource chaseAudioSource;

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

    private enum EnemyState { Idle, Walking, Running }
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

    // ?? Variables para persecución suave
    private float pathUpdateTimer = 0f;
    private float pathUpdateRate = 0.3f; // Actualizar ruta cada 0.3s
    private Vector3 lastTargetPosition;

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
            lastTargetPosition = transform.position;

            // ?? CONFIGURACIÓN OPTIMIZADA
            agent.acceleration = 12f;        // Aceleración rápida
            agent.angularSpeed = 360f;       // Rotación rápida
            agent.autoRepath = true;         // ?? REACTIVADO (necesario para paredes)
            agent.avoidancePriority = 50;
            agent.radius = 0.4f;             // Radio más pequeño para mejor navegación
            agent.height = 2f;
        }

        // ============================================
        // ?? CONFIGURAR AUDIOSOURCES
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

        // ============================================
        // ?? INICIALIZAR SONIDOS
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

        DetectPlayer();

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
        }

        UpdateAnimations();
        UpdateAudio();
        DebugDraw();
    }

    // ============================================
    // ?? DETECTAR JUGADOR
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

        if (distanceToPlayer <= visionRange)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToPlayer);

            if (angle <= visionAngle * 0.5f)
            {
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

        if (!isPlayerVisible && distanceToPlayer < 3f)
        {
            isPlayerVisible = true;
            lastKnownPlayerPosition = player.position;
            lastDirection = (player.position - transform.position).normalized;
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
    // ?? UPDATE RUNNING (VERSIÓN OPTIMIZADA)
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

                // ?? PERSECUCIÓN OPTIMIZADA - NavMesh con actualización controlada
                ChasePlayerOptimized();
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
    // ?? PERSECUCIÓN OPTIMIZADA (SIN RODEOS)
    // ============================================
    void ChasePlayerOptimized()
    {
        Vector3 targetPosition = player.position;

        // ?? 1. Verificar si el jugador se movió significativamente
        float distanceToTarget = Vector3.Distance(targetPosition, lastTargetPosition);

        // ?? 2. Actualizar ruta solo si:
        // - Ha pasado suficiente tiempo (pathUpdateRate)
        // - El jugador se movió más de 0.5m
        // - El agente está cerca del destino (para evitar rodeos)
        bool shouldUpdatePath = false;

        pathUpdateTimer += Time.deltaTime;

        if (pathUpdateTimer >= pathUpdateRate)
        {
            shouldUpdatePath = true;
        }

        if (distanceToTarget > 0.5f)
        {
            shouldUpdatePath = true;
        }

        if (agent.remainingDistance < 1f && distanceToTarget > 0.3f)
        {
            shouldUpdatePath = true; // Forzar actualización si está cerca del destino
        }

        // ?? 3. Actualizar destino si es necesario
        if (shouldUpdatePath)
        {
            pathUpdateTimer = 0f;
            lastTargetPosition = targetPosition;

            // ?? Calcular el punto en NavMesh más cercano al jugador
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPosition, out hit, 10f, agent.areaMask))
            {
                Vector3 navMeshTarget = hit.position;

                // ?? Solo actualizar si el destino cambió lo suficiente
                if (Vector3.Distance(navMeshTarget, agent.destination) > 0.3f)
                {
                    agent.SetDestination(navMeshTarget);

                    if (showDebugLogs && Time.frameCount % 60 == 0)
                        Debug.Log($"?? Actualizando ruta a: {navMeshTarget}");
                }
            }
        }

        // ?? 4. Si el agente está muy cerca del jugador (< 2m), usar movimiento directo
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer < 2f && isPlayerVisible)
        {
            // Movimiento directo para acercarse
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            directionToPlayer.y = 0;

            // Rotación suave hacia el jugador
            if (directionToPlayer.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
            }

            // Si está muy cerca y el NavMesh no llega, forzar movimiento
            if (distanceToPlayer < 1.5f && agent.remainingDistance > 0.8f)
            {
                // Pequeño empuje hacia el jugador
                agent.velocity = directionToPlayer * runSpeed * 0.5f;
            }
        }

        // ?? 5. Si el agente está atascado en una pared, forzar recálculo
        if (agent.remainingDistance > 3f && agent.velocity.magnitude < 0.1f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 1.5f)
            {
                if (showDebugLogs) Debug.Log($"?? Forzando recálculo de ruta...");

                // Forzar actualización de ruta
                pathUpdateTimer = pathUpdateRate;
                lastTargetPosition = Vector3.zero;
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    /// <summary>
    /// Detecta si el agente está atascado
    /// </summary>
    void CheckIfStuck()
    {
        if (agent == null) return;

        if (agent.hasPath && agent.remainingDistance > agent.stoppingDistance)
        {
            if (Vector3.Distance(transform.position, lastPosition) < 0.05f)
            {
                stuckTimer += Time.deltaTime;

                if (stuckTimer >= stuckThreshold)
                {
                    if (showDebugLogs) Debug.Log($"?? Agente atascado! Buscando nueva ruta...");

                    // Forzar recálculo de ruta
                    pathUpdateTimer = pathUpdateRate;
                    lastTargetPosition = Vector3.zero;

                    if (player != null && agent.isOnNavMesh)
                    {
                        NavMeshHit hit;
                        if (NavMesh.SamplePosition(player.position, out hit, 15f, agent.areaMask))
                        {
                            agent.SetDestination(hit.position);
                            if (showDebugLogs) Debug.Log($"?? Nueva ruta: {hit.position}");
                        }
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
                    agent.autoRepath = true; // Reactivar para navegación

                    // Inicializar persecución
                    pathUpdateTimer = 0f;
                    lastTargetPosition = Vector3.zero;

                    if (player != null && agent.isOnNavMesh)
                    {
                        NavMeshHit hit;
                        if (NavMesh.SamplePosition(player.position, out hit, 10f, agent.areaMask))
                        {
                            agent.SetDestination(hit.position);
                            lastTargetPosition = player.position;
                        }
                    }
                }
                if (showDebugLogs) Debug.Log($"?? RUNNING al jugador");

                StartChaseSound();
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

        if (currentState == EnemyState.Idle)
        {
            normalizedSpeed = 0f;
        }

        animator.SetFloat("Speed", normalizedSpeed);
        animator.SetBool("IsRunning", currentState == EnemyState.Running);
        animator.SetInteger("State", (int)currentState);
    }

    // ============================================
    // ?? SISTEMA DE SONIDOS
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
    // ?? ACTUALIZAR AUDIO
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