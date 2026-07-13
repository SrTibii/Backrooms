using UnityEngine;
using UnityEngine.InputSystem;

public class PuertaBloqueada : MonoBehaviour
{
    [Header("Configuración")]
    public float interactionDistance = 3f;

    [Header("Tags y Sonidos")]
    public SonidoPorTag[] sonidosPorTag;

    [Header("Sonido por defecto")]
    public AudioClip sonidoPorDefecto;
    [Range(0f, 1f)] public float volumenPorDefecto = 0.7f;

    [Header("Input")]
    public InputActionReference interactAction;

    // Referencias
    private AudioSource audioSource; // AudioSource en este GameObject (para el sonido global)
    private InteractionSystem interactionSystem;

    [System.Serializable]
    public class SonidoPorTag
    {
        public string tag;
        public AudioClip sonido;
        [Range(0f, 1f)] public float volumen = 0.7f;
    }

    void Start()
    {
        // Configurar AudioSource (se queda en este GameObject)
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // ?? Sonido 3D (se escucha en la posición de la fuente)
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = 0.5f;
        audioSource.maxDistance = 10f;

        interactionSystem = FindObjectOfType<InteractionSystem>();
        if (interactionSystem == null)
        {
            Debug.LogError("? No se encontró InteractionSystem en la escena");
        }

        Debug.Log("?? Sistema de puerta bloqueada inicializado");
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
        GameObject target = GetTargetObject();
        if (target == null) return;

        string tag = target.tag;

        // Verificar si el tag está configurado
        bool esPuerta = false;
        SonidoPorTag sonidoConfigurado = null;
        foreach (SonidoPorTag item in sonidosPorTag)
        {
            if (item.tag == tag)
            {
                esPuerta = true;
                sonidoConfigurado = item;
                break;
            }
        }

        if (!esPuerta) return;

        // Verificar distancia
        float distance = Vector3.Distance(Camera.main.transform.position, target.transform.position);
        if (distance > interactionDistance) return;

        // ?? Verificar si la puerta está abierta
        bool estaAbierta = false;

        PuertaGeneradores puertaGeneradores = target.GetComponent<PuertaGeneradores>();
        if (puertaGeneradores != null)
        {
            estaAbierta = puertaGeneradores.IsOpen();
        }

        PanelColores panelColores = target.GetComponent<PanelColores>();
        if (panelColores != null)
        {
            estaAbierta = panelColores.IsOpen();
        }

        // ?? Si la puerta NO está abierta, reproducir sonido EN LA PUERTA
        if (!estaAbierta)
        {
            ReproducirSonidoEnPuerta(target, sonidoConfigurado);
            Debug.Log($"?? Puerta bloqueada: {target.name} (tag: {tag})");
        }
        else
        {
            Debug.Log($"? Puerta ya abierta: {target.name}");
        }
    }

    // ?? NUEVO: Reproducir sonido en la posición de la puerta
    private void ReproducirSonidoEnPuerta(GameObject puerta, SonidoPorTag sonidoConfigurado)
    {
        // Obtener el clip y el volumen
        AudioClip clip = sonidoConfigurado != null ? sonidoConfigurado.sonido : sonidoPorDefecto;
        float volumen = sonidoConfigurado != null ? sonidoConfigurado.volumen : volumenPorDefecto;

        if (clip == null)
        {
            Debug.LogWarning($"?? No hay sonido asignado para la puerta: {puerta.name}");
            return;
        }

        // ?? Crear un GameObject temporal en la posición de la puerta para reproducir el sonido
        GameObject sonidoGO = new GameObject($"SonidoPuerta_{puerta.name}");
        sonidoGO.transform.position = puerta.transform.position;

        // Añadir AudioSource
        AudioSource src = sonidoGO.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volumen;
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = 0.5f;
        src.maxDistance = 10f;
        src.Play();

        // Destruir el GameObject cuando termine el sonido
        Destroy(sonidoGO, clip.length + 0.1f);

        Debug.Log($"?? Sonido reproducido en puerta: {puerta.name} (clip: {clip.name})");
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

    public bool EstaPuertaAbierta(GameObject puerta)
    {
        if (puerta == null) return false;

        PuertaGeneradores puertaGeneradores = puerta.GetComponent<PuertaGeneradores>();
        if (puertaGeneradores != null)
        {
            return puertaGeneradores.IsOpen();
        }

        PanelColores panelColores = puerta.GetComponent<PanelColores>();
        if (panelColores != null)
        {
            return panelColores.IsOpen();
        }

        return false;
    }
}