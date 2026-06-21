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
    public CharacterController characterController;
    public LockerHideSystem lockerSystem;

    [Header("Puntos de Patrulla")]
    public List<Transform> waypoints = new List<Transform>();
    public float idleTimeMin = 2f;
    public float idleTimeMax = 5f;

    [Header("Detección")]
    public float detectionRadius = 15f;
    public float visionRange = 20f;
    public float visionAngle = 60f;
    public LayerMask obstacleMask = -1;

    [Header("Persecución")]
    public float runSpeed = 5f;
    public float walkSpeed = 2f;
    public float lostPlayerTime = 3f;

    [Header("Escondite")]
    public float hideWaitTime = 3f;

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

    private bool isWaitingAfterHide = false;
    private float hideWaitTimer = 0f;

    void Start()
    {
        Debug.Log($"?? Inicializando {gameObject.name}...");

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (lockerSystem == null)
        {
            lockerSystem = FindObjectOfType<LockerHideSystem>();
            if (lockerSystem == null)
            {
                Debug.LogWarning("LockerHideSystem: No se encontró LockerHideSystem en la escena");
            }
        }

        if (agent != null)
        {
            agent.speed = walkSpeed;
            agent.autoBraking = false;
            agent.stoppingDistance = 0.5f;
            agent.isStopped = false;
        }

        if (waypoints.Count > 0)
        {
            currentWaypointIndex = Random.Range(0, waypoints.Count);
            currentWaypoint = waypoints[currentWaypointIndex];
            currentState = EnemyState.Walking;
            Debug.Log($"?? Primer waypoint: {currentWaypoint.name}");
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
        DebugDraw();
    }

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
                RaycastHit hit;
                Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
                Vector3 rayDirection = (player.position - rayOrigin).normalized;

                if (Physics.Raycast(rayOrigin, rayDirection, out hit, visionRange, obstacleMask))
                {
                    if (hit.collider.CompareTag("Player"))
                    {
                        isPlayerVisible = true;
                        lastKnownPlayerPosition = player.position;
                    }
                }
            }
        }

        if (!isPlayerVisible && isPlayerInRange && distanceToPlayer < 3f)
        {
            isPlayerVisible = true;
            lastKnownPlayerPosition = player.position;
        }

        if (isPlayerInRange)
        {
            isPlayerVisible = true;
            lastKnownPlayerPosition = player.position;
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

        Vector3 direction = (currentWaypoint.position - transform.position).normalized;
        direction.y = 0;

        float distance = Vector3.Distance(transform.position, currentWaypoint.position);

        if (distance > 0.5f)
        {
            Vector3 moveVector = direction * walkSpeed * Time.deltaTime;
            characterController.Move(moveVector);

            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
            }
        }
        else
        {
            idleTimer = Random.Range(idleTimeMin, idleTimeMax);
            ChangeState(EnemyState.Idle);
        }
    }

    void UpdateRunning()
    {
        if (isPlayerHiding)
        {
            if (showDebugLogs) Debug.Log($"?? Jugador escondido en taquilla, esperando {hideWaitTime}s - {gameObject.name}");

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

            if (player != null)
            {
                Vector3 direction = (player.position - transform.position).normalized;
                direction.y = 0;

                float distance = Vector3.Distance(transform.position, player.position);

                if (distance > 1f)
                {
                    Vector3 moveVector = direction * runSpeed * Time.deltaTime;
                    characterController.Move(moveVector);

                    if (direction.magnitude > 0.1f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
                    }
                }

                if (showDebugLogs && Time.frameCount % 30 == 0)
                {
                    Debug.Log($"?? Persiguiendo (dist: {distance:F1}m)");
                }
            }
            return;
        }

        playerLostTimer += Time.deltaTime;

        if (playerLostTimer >= lostPlayerTime)
        {
            if (showDebugLogs) Debug.Log($"?? Perdí al jugador");
            SelectNewWaypoint();
            ChangeState(EnemyState.Walking);
        }
        else
        {
            Vector3 direction = (lastKnownPlayerPosition - transform.position).normalized;
            direction.y = 0;

            float distance = Vector3.Distance(transform.position, lastKnownPlayerPosition);

            if (distance > 1f)
            {
                Vector3 moveVector = direction * runSpeed * Time.deltaTime;
                characterController.Move(moveVector);

                if (direction.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
                }
            }
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

        string oldState = currentState.ToString();
        currentState = newState;

        switch (currentState)
        {
            case EnemyState.Idle:
                if (!isWaitingAfterHide)
                {
                    idleTimer = Random.Range(idleTimeMin, idleTimeMax);
                }
                if (showDebugLogs) Debug.Log($"?? IDLE por {(isWaitingAfterHide ? hideWaitTimer : idleTimer):F1}s");
                break;

            case EnemyState.Walking:
                if (showDebugLogs) Debug.Log($"?? WALKING a {currentWaypoint?.name}");
                break;

            case EnemyState.Running:
                if (showDebugLogs) Debug.Log($"?? RUNNING al jugador");
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

    // ============================================
    // ?? UPDATE ANIMATIONS - CORREGIDO
    // ============================================
    void UpdateAnimations()
    {
        if (animator == null) return;

        // Obtener la velocidad real del CharacterController
        float speed = 0f;

        if (characterController != null)
        {
            speed = characterController.velocity.magnitude;
        }
        else if (agent != null)
        {
            speed = agent.velocity.magnitude;
        }

        // Calcular la velocidad normalizada según el estado
        float normalizedSpeed = 0f;

        if (currentState == EnemyState.Running)
        {
            normalizedSpeed = Mathf.Clamp01(speed / runSpeed);
        }
        else if (currentState == EnemyState.Walking)
        {
            normalizedSpeed = Mathf.Clamp01(speed / walkSpeed);
        }

        // Si está en Idle, velocidad 0 (a menos que esté en la espera post-escondite)
        if (currentState == EnemyState.Idle)
        {
            normalizedSpeed = 0f;
        }

        // ?? SOLO forzar si está en Running y realmente se está moviendo (evita el bug)
        // Si está en Running y tiene velocidad > 0, la animación se activará sola
        // Si está en Running y no se mueve (bloqueado), la animación de idle se activará naturalmente

        // Aplicar al Animator
        animator.SetFloat("Speed", normalizedSpeed);
        animator.SetBool("IsRunning", currentState == EnemyState.Running);
        animator.SetInteger("State", (int)currentState);
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
    }
}