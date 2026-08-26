using UnityEngine;
using UnityEngine.InputSystem;

public class Generador : MonoBehaviour
{
    [Header("Configuración")]
    public int generadorID; // 1, 2 o 3
    public bool isActive = false;

    [Header("Luces")]
    public Light luzGenerador; // La luz roja/verde del generador
    public Color colorApagado = Color.red;
    public Color colorEncendido = Color.green;

    [Header("Material (opcional)")]
    public Renderer luzRenderer;
    public Material materialApagado;
    public Material materialEncendido;

    [Header("Volumen")]
    [Range(0f, 1f)] public float volumenSonido = 0.7f;

    // ============================================
    // DISTANCIA DE INTERACCIÓN
    // ============================================
    [Header("Interacción")]
    [Tooltip("Distancia máxima a la que el jugador puede interactuar con el generador")]
    public float interactionDistance = 3f;

    // Input System
    public InputActionReference interactAction;

    // Referencias
    private InteractionSystem interactionSystem;
    private PuertaGeneradores puerta;

    void Start()
    {
        // Buscar referencias
        interactionSystem = FindObjectOfType<InteractionSystem>();

        // Buscar la puerta
        puerta = FindObjectOfType<PuertaGeneradores>();

        // Estado inicial: apagado
        SetState(false);

        Debug.Log($"? Generador {generadorID} inicializado");
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
        // Si ya está activado, no hacer nada
        if (isActive) return;

        // Obtener el objeto que el jugador está mirando
        GameObject target = GetTargetObject();
        if (target == null) return;

        // Verificar que está mirando este generador
        if (target != gameObject) return;

        // ============================================
        // USAR LA VARIABLE interactionDistance
        // ============================================
        float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
        if (distance > interactionDistance)
        {
            Debug.Log($"? Demasiado lejos para interactuar (necesitas {interactionDistance}m, estás a {distance:F1}m)");
            return;
        }

        // ACTIVAR GENERADOR
        ActivarGenerador();
    }

    private GameObject GetTargetObject()
    {
        if (interactionSystem != null)
        {
            return interactionSystem.GetTargetObject();
        }

        // Si no hay InteractionSystem, usar Raycast directo
        Camera cam = Camera.main;
        if (cam == null) return null;

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        // ============================================
        // TAMBIÉN USAR interactionDistance AQUÍ
        // ============================================
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            return hit.collider.gameObject;
        }

        return null;
    }

    private void ActivarGenerador()
    {
        if (isActive) return;

        isActive = true;

        // Cambiar luz a verde
        SetState(true);

        // ============================================
        // REPRODUCIR SONIDO DE ACTIVACIÓN CON AUDIOMANAGER
        // ============================================
        if (AudioManager.Instance != null && AudioManager.Instance.sonidoActivacionGenerador != null)
        {
            AudioManager.Instance.PlayOneShotAtPosition(
                AudioManager.Instance.sonidoActivacionGenerador,
                transform.position,
                volumenSonido,
                10f
            );
            Debug.Log($"?? Sonido de activación reproducido para generador {generadorID}");
        }
        else
        {
            Debug.LogWarning($"?? AudioManager o sonidoActivacionGenerador no disponible para generador {generadorID}");
        }

        Debug.Log($"? Generador {generadorID} ACTIVADO");

        // Notificar a la puerta
        if (puerta != null)
        {
            puerta.GeneradorActivado(generadorID);
        }
    }

    private void SetState(bool active)
    {
        // Cambiar la luz (Light)
        if (luzGenerador != null)
        {
            luzGenerador.color = active ? colorEncendido : colorApagado;
            luzGenerador.enabled = true;
        }

        // Cambiar el material (si se usa)
        if (luzRenderer != null)
        {
            if (active && materialEncendido != null)
                luzRenderer.material = materialEncendido;
            else if (!active && materialApagado != null)
                luzRenderer.material = materialApagado;
        }
    }

    // Método para saber si está activado
    public bool IsActive()
    {
        return isActive;
    }

    // Método para obtener el ID
    public int GetID()
    {
        return generadorID;
    }
}