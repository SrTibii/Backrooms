using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class MensajeInteraccion : MonoBehaviour
{
    [Header("Configuración")]
    public float interactionDistance = 3f;

    [Header("UI")]
    public GameObject panelMensaje;
    public TextMeshProUGUI textoMensaje;
    public float tiempoVisible = 2f;

    [Header("Requisitos por Tag del objeto interactuable")]
    public Requisito[] requisitos;

    [Header("Input")]
    public InputActionReference interactAction;

    [System.Serializable]
    public class Requisito
    {
        public string tagObjeto; // Tag del objeto con el que interactúas (ej: "Tabla")
        public string tagNecesario; // Tag que necesitas tener en la mano (ej: "Martillo")
        public string mensaje; // Mensaje a mostrar (ej: "I need a hammer")
        public bool exactMatch = true; // Si es true, el objeto en mano debe tener EXACTAMENTE el tagNecesario
    }

    private InteractionSystem interactionSystem;
    private ManosManager manosManager;
    private float timer = 0f;
    private bool mensajeActivo = false;
    private Coroutine corrutinaCerrar;

    void Start()
    {
        interactionSystem = FindObjectOfType<InteractionSystem>();
        manosManager = FindObjectOfType<ManosManager>();

        if (panelMensaje != null)
        {
            panelMensaje.SetActive(false);
        }
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
        // Si el panel ya está visible, no hacer nada
        if (mensajeActivo) return;

        if (interactionSystem == null) return;
        if (manosManager == null) return;

        // Obtener el objeto que el jugador está mirando
        GameObject target = interactionSystem.GetTargetObject();
        if (target == null) return;

        // Verificar distancia
        float distance = Vector3.Distance(Camera.main.transform.position, target.transform.position);
        if (distance > interactionDistance) return;

        // Buscar si hay un requisito para este tag
        Requisito req = ObtenerRequisito(target.tag);
        if (req == null) return;

        // Verificar si el objeto en mano cumple el requisito
        if (!CumpleRequisito(req))
        {
            MostrarMensaje(req.mensaje);
        }
    }

    private Requisito ObtenerRequisito(string tag)
    {
        foreach (Requisito req in requisitos)
        {
            if (req.tagObjeto == tag)
            {
                return req;
            }
        }
        return null;
    }

    private bool CumpleRequisito(Requisito req)
    {
        // Buscar en ambas manos
        GameObject objetoEnMano = manosManager.objetoEnManoIzquierda;
        if (objetoEnMano == null)
        {
            objetoEnMano = manosManager.objetoEnManoDerecha;
        }

        // Si no hay objeto en mano
        if (objetoEnMano == null)
        {
            // Si el requisito exige tener algo, no cumple
            return false;
        }

        // Verificar el tag del objeto en mano
        if (req.exactMatch)
        {
            return objetoEnMano.CompareTag(req.tagNecesario);
        }
        else
        {
            // Si no es exacto, puedes añadir lógica adicional aquí
            return objetoEnMano.CompareTag(req.tagNecesario);
        }
    }

    private void MostrarMensaje(string mensaje)
    {
        if (panelMensaje == null || textoMensaje == null) return;

        // Cancelar corrutina anterior si existe
        if (corrutinaCerrar != null)
        {
            StopCoroutine(corrutinaCerrar);
        }

        textoMensaje.text = mensaje;
        panelMensaje.SetActive(true);
        mensajeActivo = true;

        // Iniciar corrutina para ocultar el mensaje
        corrutinaCerrar = StartCoroutine(CerrarMensajeConDelay());
    }

    private System.Collections.IEnumerator CerrarMensajeConDelay()
    {
        yield return new WaitForSeconds(tiempoVisible);

        if (panelMensaje != null)
        {
            panelMensaje.SetActive(false);
        }
        mensajeActivo = false;
        corrutinaCerrar = null;
    }

    // ?? Método para cerrar el mensaje manualmente (opcional)
    public void CerrarMensaje()
    {
        if (panelMensaje != null)
        {
            panelMensaje.SetActive(false);
        }
        mensajeActivo = false;
        if (corrutinaCerrar != null)
        {
            StopCoroutine(corrutinaCerrar);
            corrutinaCerrar = null;
        }
    }
}