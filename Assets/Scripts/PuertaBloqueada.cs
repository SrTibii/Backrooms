using UnityEngine;
using UnityEngine.InputSystem;

public class PuertaBloqueada : MonoBehaviour
{
    [Header("Configuración")]
    public float interactionDistance = 3f;

    [Header("Volumen")]
    [Range(0f, 1f)] public float volumenPorDefecto = 0.7f;

    [Header("Input")]
    public InputActionReference interactAction;

    // Referencias
    private InteractionSystem interactionSystem;

    void Start()
    {
        interactionSystem = FindObjectOfType<InteractionSystem>();
        if (interactionSystem == null)
        {
            Debug.LogError("? No se encontró InteractionSystem en la escena");
        }

        Debug.Log("? Sistema de puerta bloqueada inicializado");
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

        // ============================================
        // ?? MODIFICADO: AÑADIDO "PuertaGenerador" (singular)
        // ============================================
        bool esPuertaBloqueada = tag == "PuertaBloqueada" ||
                                 tag == "PuertaMadera" ||
                                 tag == "PuertaMetal" ||
                                 tag == "PuertaGenerador" ||    // ? Singular (como en tu juego)
                                 tag == "PuertaGeneradores" ||  // ? Plural (por si acaso)
                                 tag == "PuertaColoresTV";

        if (!esPuertaBloqueada) return;

        // Verificar distancia
        float distance = Vector3.Distance(Camera.main.transform.position, target.transform.position);
        if (distance > interactionDistance) return;

        // Verificar si la puerta está abierta
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

        // Si la puerta NO está abierta, reproducir sonido
        if (!estaAbierta)
        {
            ReproducirSonidoEnPuerta(target, tag);
            Debug.Log($"?? Puerta bloqueada: {target.name} (tag: {tag})");
        }
        else
        {
            Debug.Log($"? Puerta ya abierta: {target.name}");
        }
    }

    // ============================================
    // REPRODUCIR SONIDO EN LA POSICIÓN DE LA PUERTA
    // ============================================
    private void ReproducirSonidoEnPuerta(GameObject puerta, string tag)
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("?? AudioManager no disponible");
            return;
        }

        AudioClip clip = null;
        float volumen = volumenPorDefecto;

        // Determinar qué clip reproducir según el tag
        switch (tag)
        {
            case "PuertaMadera":
                if (AudioManager.Instance.sonidosPuertaBloqueada != null && AudioManager.Instance.sonidosPuertaBloqueada.Length > 0)
                {
                    clip = AudioManager.Instance.sonidosPuertaBloqueada[0];
                }
                break;

            case "PuertaMetal":
                if (AudioManager.Instance.sonidosPuertaBloqueada != null && AudioManager.Instance.sonidosPuertaBloqueada.Length > 1)
                {
                    clip = AudioManager.Instance.sonidosPuertaBloqueada[1];
                }
                else if (AudioManager.Instance.sonidosPuertaBloqueada != null && AudioManager.Instance.sonidosPuertaBloqueada.Length > 0)
                {
                    clip = AudioManager.Instance.sonidosPuertaBloqueada[0];
                }
                break;

            case "PuertaGenerador":
            case "PuertaGeneradores":
                if (AudioManager.Instance.sonidosPuertaBloqueada != null && AudioManager.Instance.sonidosPuertaBloqueada.Length > 2)
                {
                    clip = AudioManager.Instance.sonidosPuertaBloqueada[2];
                }
                else if (AudioManager.Instance.sonidosPuertaBloqueada != null && AudioManager.Instance.sonidosPuertaBloqueada.Length > 0)
                {
                    clip = AudioManager.Instance.sonidosPuertaBloqueada[0];
                }
                break;

            case "PuertaColoresTV":
                if (AudioManager.Instance.sonidosPuertaBloqueada != null && AudioManager.Instance.sonidosPuertaBloqueada.Length > 3)
                {
                    clip = AudioManager.Instance.sonidosPuertaBloqueada[3];
                }
                else if (AudioManager.Instance.sonidosPuertaBloqueada != null && AudioManager.Instance.sonidosPuertaBloqueada.Length > 0)
                {
                    clip = AudioManager.Instance.sonidosPuertaBloqueada[0];
                }
                break;

            default:
                if (AudioManager.Instance.sonidosPuertaBloqueada != null && AudioManager.Instance.sonidosPuertaBloqueada.Length > 0)
                {
                    clip = AudioManager.Instance.sonidosPuertaBloqueada[0];
                }
                break;
        }

        if (clip == null)
        {
            Debug.LogWarning($"?? No hay sonido asignado para el tag '{tag}' en AudioManager");
            return;
        }

        AudioManager.Instance.PlayOneShotAtPosition(
            clip,
            puerta.transform.position,
            volumen,
            10f
        );

        Debug.Log($"?? Sonido reproducido en puerta: {puerta.name} (tag: {tag}, clip: {clip.name})");
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