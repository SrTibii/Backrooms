using UnityEngine;
using UnityEngine.InputSystem;

public class TablaMadera : MonoBehaviour
{
    [Header("Configuración")]
    public float interactionDistance = 3f;

    [Header("Audio")]
    public AudioClip sonidoRomper;
    [Range(0f, 1f)] public float volumenSonido = 0.8f;

    [Header("Input")]
    public InputActionReference interactAction;

    private AudioSource audioSource;
    private bool isBroken = false;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.volume = volumenSonido;

        gameObject.tag = "Tabla";
    }

    private void OnEnable()
    {
        if (interactAction != null)
        {
            // ?? IMPORTANTE: Crear una nueva referencia para cada tabla
            // o usar el método que evita problemas de duplicados
            interactAction.action.performed += OnInteractPerformed;
            interactAction.action.Enable();
            Debug.Log($"? Tabla {gameObject.name} suscrita al Input Action");
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed -= OnInteractPerformed;
            Debug.Log($"? Tabla {gameObject.name} desuscrita del Input Action");
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        // ?? Verificar si ya está rota
        if (isBroken)
        {
            Debug.Log($"? {gameObject.name} ya está rota");
            return;
        }

        Debug.Log($"?? {gameObject.name} ha recibido interacción");

        // ?? RAYCAST DIRECTO
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

        // ?? Verificar si está mirando ESTA tabla
        if (hit.collider.gameObject != gameObject)
        {
            Debug.Log($"?? {gameObject.name}: No es esta tabla (es {hit.collider.gameObject.name})");
            return;
        }

        // ?? Verificar distancia
        float distance = Vector3.Distance(cam.transform.position, transform.position);
        if (distance > interactionDistance)
        {
            Debug.Log($"?? {gameObject.name}: Demasiado lejos ({distance:F1}m)");
            return;
        }

        // ?? Verificar si tiene martillo
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
        ManosManager manosManager = FindObjectOfType<ManosManager>();
        if (manosManager == null)
        {
            Debug.Log("? No hay ManosManager");
            return false;
        }

        GameObject objetoEnMano = manosManager.objetoEnManoIzquierda;
        if (objetoEnMano == null)
        {
            objetoEnMano = manosManager.objetoEnManoDerecha;
        }

        if (objetoEnMano == null)
        {
            Debug.Log("??? No hay objeto en la mano");
            return false;
        }

        bool tieneMartillo = objetoEnMano.CompareTag("Martillo");
        Debug.Log($"?? ¿Tiene martillo? {tieneMartillo} (objeto: {objetoEnMano.name})");
        return tieneMartillo;
    }

    private void RomperTabla()
    {
        if (isBroken) return;
        isBroken = true;

        Debug.Log($"?? ¡Tabla {gameObject.name} rota!");

        if (sonidoRomper != null)
        {
            audioSource.volume = volumenSonido;
            audioSource.PlayOneShot(sonidoRomper);
        }

        float delay = sonidoRomper != null ? sonidoRomper.length : 0.3f;
        Destroy(gameObject, delay);
    }
}