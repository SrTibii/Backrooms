using UnityEngine;
using UnityEngine.InputSystem;

public class TablaMadera : MonoBehaviour
{
    [Header("Configuración")]
    public float interactionDistance = 3f;

    [Header("Volumen")]
    [Range(0f, 1f)] public float volumenSonido = 0.8f;

    [Header("Input")]
    public InputActionReference interactAction;

    private bool isBroken = false;

    private bool clickDerechoAnterior = false;

    void Start()
    {
        gameObject.tag = "Tabla";
        Debug.Log($"? Tabla {gameObject.name} inicializada");
    }

    private void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed += OnInteractPerformed;
            interactAction.action.Enable();
            Debug.Log($"?? Tabla {gameObject.name} suscrita al Input Action");
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed -= OnInteractPerformed;
            Debug.Log($"?? Tabla {gameObject.name} desuscrita del Input Action");
        }
    }

    void Update()
    {
        // Si Unity no tiene conexión con el Arduino, no hace nada
        if (ArduinoManager.Instance == null) return;

        // Detecta el flanco de subida (cuando pulsas el botón, no mientras lo mantienes)
        if (ArduinoManager.Instance.clickDerecho && !clickDerechoAnterior)
        {
            // Si estamos mirando la tabla, intentamos romperla
            Camera cam = Camera.main;
            if (cam != null)
            {
                Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, interactionDistance))
                {
                    if (hit.collider.gameObject == gameObject)
                    {
                        // ¡Estamos mirando la tabla! Comprobamos si tenemos el martillo
                        if (TieneMartilloEnMano())
                        {
                            RomperTabla();
                        }
                        else
                        {
                            Debug.Log("Necesitas un martillo para romper la tabla");
                        }
                    }
                }
            }
        }

        clickDerechoAnterior = ArduinoManager.Instance.clickDerecho;
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        // Verificar si ya está rota
        if (isBroken)
        {
            Debug.Log($"? {gameObject.name} ya está rota");
            return;
        }

        Debug.Log($"??? {gameObject.name} ha recibido interacción");

        // RAYCAST DIRECTO
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, interactionDistance))
        {
            Debug.Log($"? {gameObject.name}: No hay nada en mira");
            return;
        }

        Debug.Log($"?? {gameObject.name}: Raycast impactó en {hit.collider.gameObject.name}");

        // Verificar si está mirando ESTA tabla
        if (hit.collider.gameObject != gameObject)
        {
            Debug.Log($"?? {gameObject.name}: No es esta tabla (es {hit.collider.gameObject.name})");
            return;
        }

        // Verificar distancia
        float distance = Vector3.Distance(cam.transform.position, transform.position);
        if (distance > interactionDistance)
        {
            Debug.Log($"?? {gameObject.name}: Demasiado lejos ({distance:F1}m)");
            return;
        }

        // Verificar si tiene martillo
        if (!TieneMartilloEnMano())
        {
            Debug.Log($"?? {gameObject.name}: Necesitas martillo");
            return;
        }

        Debug.Log($"? {gameObject.name}: Todas las condiciones cumplidas");
        RomperTabla();
    }

    private bool TieneMartilloEnMano()
    {
        // Buscamos directamente el script del martillo en la escena
        RecogerMartillo scriptMartillo = FindObjectOfType<RecogerMartillo>();

        // Si el script existe y dice que está sosteniendo algo, es que tenemos el martillo
        if (scriptMartillo != null && scriptMartillo.IsHolding())
        {
            return true;
        }

        return false;
    }

    private void RomperTabla()
    {
        if (isBroken) return;
        isBroken = true;

        Debug.Log($"?? ¡Tabla {gameObject.name} rota!");

        // ============================================
        // REPRODUCIR SONIDO DE ROMPER TABLA CON AUDIOMANAGER
        // ============================================
        if (AudioManager.Instance != null && AudioManager.Instance.sonidoRomperTabla != null)
        {
            AudioManager.Instance.PlayOneShotAtPosition(
                AudioManager.Instance.sonidoRomperTabla,
                transform.position,
                volumenSonido,
                12f
            );
            Debug.Log($"?? Sonido de romper tabla reproducido para {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"?? AudioManager o sonidoRomperTabla no disponible para {gameObject.name}");
        }

        // Obtener la duración del clip para el delay
        float delay = 0.3f;
        if (AudioManager.Instance != null && AudioManager.Instance.sonidoRomperTabla != null)
        {
            delay = AudioManager.Instance.sonidoRomperTabla.length;
        }

        Destroy(gameObject, delay);
    }
}