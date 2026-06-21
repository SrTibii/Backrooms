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

    [Header("Debug")]
    public bool showDebugLogs = true;

    private enum EnemyState { Idle, Walking, Running }
    private EnemyState currentState = EnemyState.Idle;

    private Transform currentWaypoint;
    private int currentWaypointIndex = 0;
    private float idleTimer = 0f;

    private bool isPlayerVisible = false;
    private bool isPlayerInRange = false;
    private float playerLostTimer = 0f;

    private Vector3 lastKnownPlayerPosition;
    private bool isPlayerHiding = false; // ?? NUEVO: Saber si el jugador está escondido

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
        // ?? Comprobar si el jugador está escondido
        isPlayerHiding = false;
        LockerHideSystem lockerSystem = player.GetComponent<LockerHideSystem>();
        if (lockerSystem == null)
            lockerSystem = player.GetComponentInChildren<LockerHideSystem>();

        if (lockerSystem != null)
            isPlayerHiding = lockerSystem.IsHiding();

        // ?? Si el jugador está escondido, NO se puede detectar
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
        // ?? Si el jugador está visible y NO está escondido, correr
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
        // ?? Si el jugador está visible y NO está escondido, correr
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
        // ?? Si el jugador está escondido, perderlo inmediatamente
        if (isPlayerHiding)
        {
            if (showDebugLogs) Debug.Log($"?? Jugador escondido en taquilla, volviendo a patrullar");
            playerLostTimer = lostPlayerTime; // Forzar pérdida inmediata
            isPlayerVisible = false;
            isPlayerInRange = false;
            SelectNewWaypoint();
            ChangeState(EnemyState.Walking);
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

        // Si no ve al jugador
        playerLostTimer += Time.deltaTime;

        if (playerLostTimer >= lostPlayerTime)
        {
            if (showDebugLogs) Debug.Log($"?? Perdí al jugador");
            SelectNewWaypoint();
            ChangeState(EnemyState.Walking);
        }
        else
        {
            // Ir a la última posición conocida
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

    // ============================================
    // ?? OnPlayerHid - Mejorado
    // ============================================
    public void OnPlayerHid()
    {
        if (showDebugLogs) Debug.Log($"?? El jugador se ha escondido en una taquilla");

        // Si está corriendo, volver a patrullar inmediatamente
        if (currentState == EnemyState.Running)
        {
            isPlayerVisible = false;
            isPlayerInRange = false;
            playerLostTimer = 0f;
            SelectNewWaypoint();
            ChangeState(EnemyState.Walking);
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
                idleTimer = Random.Range(idleTimeMin, idleTimeMax);
                if (showDebugLogs) Debug.Log($"?? IDLE por {idleTimer:F1}s");
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

    void UpdateAnimations()
    {
        if (animator == null) return;

        float speed = 0f;
        if (currentState == EnemyState.Running)
        {
            speed = runSpeed;
        }
        else if (currentState == EnemyState.Walking)
        {
            speed = walkSpeed;
        }

        animator.SetFloat("Speed", speed);
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