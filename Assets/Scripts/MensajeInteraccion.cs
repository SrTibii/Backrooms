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
        public string tagObjeto;
        public string tagNecesario;
        public string mensaje;
        public bool exactMatch = true;
        public bool verificarEstado = false;
        public bool estadoEsperado = true;
    }

    private InteractionSystem interactionSystem;
    private ManosManager manosManager;
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
        if (mensajeActivo) return;
        if (interactionSystem == null) return;
        if (manosManager == null) return;

        GameObject target = interactionSystem.GetTargetObject();
        if (target == null) return;

        // ?? IGNORAR OBJETOS YA USADOS
        if (target.CompareTag("Usado")) return;

        float distance = Vector3.Distance(Camera.main.transform.position, target.transform.position);
        if (distance > interactionDistance) return;

        Requisito req = ObtenerRequisito(target.tag);
        if (req == null) return;

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

    // ?? VERIFICAR ESTADO DEL OBJETO
    private bool VerificarEstadoObjeto(GameObject obj, Requisito req)
    {
        // ?? Buscar el script PuertaGeneradores en este objeto o en sus padres/hijos
        PuertaGeneradores puerta = obj.GetComponent<PuertaGeneradores>();

        // Si no está en el objeto, buscar en el padre
        if (puerta == null && obj.transform.parent != null)
        {
            puerta = obj.transform.parent.GetComponent<PuertaGeneradores>();
        }

        // Si no está en el padre, buscar en cualquier hijo
        if (puerta == null)
        {
            puerta = obj.GetComponentInChildren<PuertaGeneradores>();
        }

        // ?? Si encontramos la puerta, verificar su estado
        if (puerta != null)
        {
            bool isOpen = puerta.IsOpen();
            Debug.Log($"?? Puerta verificada: IsOpen = {isOpen}, Estado esperado = {req.estadoEsperado}");

            // Devolver true si la puerta está en el estado esperado (NO mostrar mensaje)
            return isOpen == req.estadoEsperado;
        }

        // ?? Si no encontramos PuertaGeneradores, buscar otros scripts con estado
        // Puedes añadir más casos aquí

        // Si no se encuentra ningún script con estado, mostrar mensaje normalmente
        return false;
    }

    private bool CumpleRequisito(Requisito req)
    {
        GameObject objetoEnMano = manosManager.objetoEnManoIzquierda;
        if (objetoEnMano == null)
        {
            objetoEnMano = manosManager.objetoEnManoDerecha;
        }

        if (objetoEnMano == null) return false;

        if (req.exactMatch)
        {
            return objetoEnMano.CompareTag(req.tagNecesario);
        }
        else
        {
            return objetoEnMano.CompareTag(req.tagNecesario);
        }
    }

    private void MostrarMensaje(string mensaje)
    {
        if (panelMensaje == null || textoMensaje == null) return;

        if (corrutinaCerrar != null)
        {
            StopCoroutine(corrutinaCerrar);
        }

        textoMensaje.text = mensaje;
        panelMensaje.SetActive(true);
        mensajeActivo = true;

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