using UnityEngine;
using UnityEngine.InputSystem;

public class DrawerInteraction : MonoBehaviour
{
    [Header("Referencias")]
    public InteractionSystem interactionSystem;
    public InputActionReference interactAction;

    [Header("Configuración")]
    public float interactionDistance = 3f;
    public float openDistance = 0.5f;
    public float speed = 2f;
    public bool startOpen = false;

    [Header("Dirección de apertura (Local del objeto)")]
    public Vector3 localOpenDirection = new Vector3(0, 1, 0);

    [Header("Tags para interacción")]
    public string drawerTag = "Drawer";

    [Header("Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;
    [Range(0f, 1f)] public float soundVolume = 0.7f;

    private bool isOpen = false;
    private bool isAnimating = false;
    private Vector3 closedPosition;
    private Vector3 openPosition;
    private AudioSource audioSource;

    void Start()
    {
        if (interactionSystem == null)
            interactionSystem = FindObjectOfType<InteractionSystem>();

        if (!string.IsNullOrEmpty(drawerTag))
            gameObject.tag = drawerTag;

        // Guardar posición de cierre
        closedPosition = transform.localPosition;

        // ?? OBTENER LA ESCALA DEL PADRE
        Vector3 parentScale = transform.parent != null ? transform.parent.localScale : Vector3.one;

        // ?? CALCULAR EL FACTOR DE ESCALA PARA CADA EJE
        // El cajón se mueve en el eje Y local (según tu configuración)
        // Pero necesitamos saber en qué dirección se mueve realmente
        Vector3 worldDirection = transform.TransformDirection(localOpenDirection.normalized);

        // ?? Convertir la dirección a local del padre, PERO dividiendo por la escala
        Vector3 localDirectionInParent = transform.parent.InverseTransformDirection(worldDirection);

        // ?? IMPORTANTE: Dividir por la escala del padre para compensar
        // Si el padre tiene escala 220, dividimos por 220 para que el movimiento sea correcto
        Vector3 compensatedDirection = new Vector3(
            localDirectionInParent.x / parentScale.x,
            localDirectionInParent.y / parentScale.y,
            localDirectionInParent.z / parentScale.z
        );

        // ?? Calcular posición de apertura con la dirección compensada
        openPosition = closedPosition + compensatedDirection * openDistance;

        Debug.Log($"?? {gameObject.name}:");
        Debug.Log($"  - closedPosition (local): {closedPosition}");
        Debug.Log($"  - openPosition (local): {openPosition}");
        Debug.Log($"  - openDistance: {openDistance}");
        Debug.Log($"  - parentScale: {parentScale}");
        Debug.Log($"  - localDirectionInParent (sin compensar): {localDirectionInParent}");
        Debug.Log($"  - compensatedDirection (compensado): {compensatedDirection}");

        // ?? Verificar la distancia REAL en mundo
        Vector3 worldClosed = transform.parent.TransformPoint(closedPosition);
        Vector3 worldOpen = transform.parent.TransformPoint(openPosition);
        float realDistance = Vector3.Distance(worldClosed, worldOpen);
        Debug.Log($"  - Distancia REAL en mundo: {realDistance} (debería ser {openDistance})");

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        isOpen = startOpen;
        if (isOpen)
            transform.localPosition = openPosition;
        else
            transform.localPosition = closedPosition;
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

    void Update()
    {
        Vector3 targetPosition = isOpen ? openPosition : closedPosition;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * speed);
    }

    void HandleInteraction()
    {
        if (interactionSystem == null) return;

        GameObject target = interactionSystem.GetTargetObject();
        if (target == null) return;

        if (target != gameObject && !target.transform.IsChildOf(transform))
            return;

        float distance = interactionSystem.GetTargetDistance();
        if (distance < 0 || distance > interactionDistance)
            return;

        if (isAnimating) return;

        ToggleDrawer();
    }

    public void ToggleDrawer()
    {
        isOpen = !isOpen;
        isAnimating = true;

        PlaySound(isOpen ? openSound : closeSound);
        Invoke(nameof(ResetAnimation), 0.5f);

        Debug.Log($"?? Cajón {(isOpen ? "abierto" : "cerrado")}");
    }

    void ResetAnimation() => isAnimating = false;

    void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        audioSource.volume = soundVolume;
        audioSource.PlayOneShot(clip);
    }

    public bool IsOpen() => isOpen;
    public bool IsAnimating() => isAnimating;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);

        if (Application.isPlaying && transform.parent != null)
        {
            Gizmos.color = Color.green;
            Vector3 startWorld = transform.parent.TransformPoint(closedPosition);
            Vector3 endWorld = transform.parent.TransformPoint(openPosition);
            Gizmos.DrawLine(startWorld, endWorld);
            Gizmos.DrawWireSphere(endWorld, 0.05f);
        }
    }
}