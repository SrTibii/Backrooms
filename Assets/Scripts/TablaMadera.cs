using UnityEngine;
using UnityEngine.InputSystem;

public class TablaMadera : MonoBehaviour
{
    [Header("Configuración")]
    public float interactionDistance = 3f;
    public string martilloTag = "Martillo";

    [Header("Audio")]
    public AudioClip sonidoRomper;
    [Range(0f, 1f)] public float volumenSonido = 0.8f;

    [Header("Efectos Visuales (Opcional)")]
    //public GameObject prefabFragmentos; // Prefab con partículas o fragmentos
    public float tiempoDestruccion = 0.5f; // Tiempo antes de destruir la tabla

    [Header("Input")]
    public InputActionReference interactAction;

    // Referencias
    private AudioSource audioSource;
    private InteractionSystem interactionSystem;

    void Start()
    {
        // Configurar AudioSource en la tabla
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.volume = volumenSonido;

        interactionSystem = FindObjectOfType<InteractionSystem>();
        if (interactionSystem == null)
        {
            Debug.LogError("? No se encontró InteractionSystem en la escena");
        }

        // Asegurar que la tabla tiene el tag correcto
        gameObject.tag = "Tabla";
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
        // Obtener el objeto que el jugador está mirando
        GameObject target = GetTargetObject();
        if (target == null) return;

        // Verificar que está mirando ESTA tabla
        if (target != gameObject) return;

        // Verificar distancia
        float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
        if (distance > interactionDistance) return;

        // ?? Verificar si el jugador tiene el martillo en la mano
        if (!TieneMartilloEnMano())
        {
            Debug.Log("?? Necesitas un martillo para romper esta tabla");
            return;
        }

        // ?? ROMPER LA TABLA
        RomperTabla();
    }

    private GameObject GetTargetObject()
    {
        if (interactionSystem != null)
        {
            return interactionSystem.GetTargetObject();
        }

        Camera cam = Camera.main;
        if (cam == null) return null;

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            return hit.collider.gameObject;
        }

        return null;
    }

    private bool TieneMartilloEnMano()
    {
        // Buscar el script RecogerMartillo
        RecogerMartillo recogerMartillo = FindObjectOfType<RecogerMartillo>();
        if (recogerMartillo == null) return false;

        // Verificar si tiene el martillo en la mano
        return recogerMartillo.IsHolding();
    }

    private void RomperTabla()
    {
        Debug.Log("?? ¡Tabla rota!");

        // ?? Reproducir sonido
        if (sonidoRomper != null)
        {
            audioSource.volume = volumenSonido;
            audioSource.PlayOneShot(sonidoRomper);
        }

        // ?? Efectos visuales (si hay prefab de fragmentos)
        //if (prefabFragmentos != null)
        //{
        //    Instantiate(prefabFragmentos, transform.position, transform.rotation);
        //}

        // ?? Ocultar la tabla inmediatamente o con delay
        // Opción 1: Desaparece inmediatamente
        // gameObject.SetActive(false);

        // Opción 2: Desaparece después del sonido (recomendado)
        Destroy(gameObject, sonidoRomper != null ? sonidoRomper.length : 0.3f);
    }

    // ?? Método para saber si la tabla está rota (opcional)
    public bool EstaRota()
    {
        return !gameObject.activeSelf;
    }
}