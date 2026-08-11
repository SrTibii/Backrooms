using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PuertaFinal : MonoBehaviour
{
    [Header("Configuración")]
    public float interactionDistance = 3f;

    [Header("Candados")]
    public Candado[] candados;

    [Header("Tags de las llaves")]
    public string tagLlaveRoja = "LlaveRojo";
    public string tagLlaveAzul = "LlaveAzul";
    public string tagLlaveVerde = "LlaveVerde";

    [Header("Input")]
    public InputActionReference interactAction;

    [Header("Audio")]
    public AudioClip sonidoCandadoAbierto;
    public AudioClip sonidoLlaveIncorrecta;
    public AudioClip sonidoPuertaAbierta;
    [Range(0f, 1f)] public float volumenSonidos = 0.7f;

    private int candadosAbiertos = 0;
    private bool puertaAbierta = false;
    private AudioSource audioSource;
    private InteractionSystem interactionSystem;

    [System.Serializable]
    public class Candado
    {
        public string nombre;
        public GameObject candadoGO;
        public bool isOpen = false;
    }

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.volume = volumenSonidos;

        interactionSystem = FindObjectOfType<InteractionSystem>();
        if (interactionSystem == null)
        {
            Debug.LogError("? No se encontró InteractionSystem en la escena");
        }

        Debug.Log("?? Puerta final inicializada");
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
        if (puertaAbierta) return;

        GameObject target = GetTargetObject();
        if (target == null) return;

        // Verificar si es un candado
        if (target.CompareTag("CandadoRojo") || target.CompareTag("CandadoAzul") || target.CompareTag("CandadoVerde"))
        {
            float distance = Vector3.Distance(Camera.main.transform.position, target.transform.position);
            if (distance > interactionDistance) return;

            Candado candado = ObtenerCandadoPorGameObject(target);
            if (candado != null)
            {
                IntentarAbrirCandado(candado);
            }
            return;
        }

        if (target == gameObject)
        {
            float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
            if (distance > interactionDistance) return;
            IntentarAbrirPuerta();
        }
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

    private Candado ObtenerCandadoPorGameObject(GameObject go)
    {
        foreach (Candado candado in candados)
        {
            if (candado.candadoGO == go)
            {
                return candado;
            }
        }
        return null;
    }

    private string ObtenerTagLlavePorColor(string color)
    {
        switch (color.ToLower())
        {
            case "rojo": return tagLlaveRoja;
            case "azul": return tagLlaveAzul;
            case "verde": return tagLlaveVerde;
            default: return "";
        }
    }

    private bool TieneLlaveEnMano(string tagLlave)
    {
        RecogerObjeto recoger = FindObjectOfType<RecogerObjeto>();
        if (recoger == null) return false;

        GameObject objetoEnMano = recoger.GetHeldObject();
        if (objetoEnMano == null) return false;

        return objetoEnMano.CompareTag(tagLlave);
    }

    private void EliminarLlaveDeLaMano()
    {
        RecogerObjeto recoger = FindObjectOfType<RecogerObjeto>();
        if (recoger == null) return;

        GameObject llave = recoger.GetHeldObject();
        if (llave != null)
        {
            Destroy(llave);
            recoger.ForceDrop();
            Debug.Log("??? Llave consumida");
        }
    }

    private void IntentarAbrirCandado(Candado candado)
    {
        if (candado.isOpen) return;

        string tagLlave = ObtenerTagLlavePorColor(candado.nombre);
        if (string.IsNullOrEmpty(tagLlave)) return;

        if (TieneLlaveEnMano(tagLlave))
        {
            if (sonidoCandadoAbierto != null)
            {
                audioSource.volume = volumenSonidos;
                audioSource.PlayOneShot(sonidoCandadoAbierto);
                Debug.Log($"?? Sonido de candado abierto reproducido para {candado.nombre}");
            }

            candado.isOpen = true;
            candadosAbiertos++;

            if (candado.candadoGO != null)
            {
                StartCoroutine(OcultarCandadoConDelay(candado.candadoGO, 0.3f));
            }

            EliminarLlaveDeLaMano();

            Debug.Log($"?? Candado {candado.nombre} abierto! ({candadosAbiertos}/3)");

            if (candadosAbiertos >= candados.Length)
            {
                Debug.Log("?? ¡Todos los candados abiertos! Ahora puedes abrir la puerta.");
            }
        }
        else
        {
            if (sonidoLlaveIncorrecta != null)
            {
                audioSource.volume = volumenSonidos;
                audioSource.PlayOneShot(sonidoLlaveIncorrecta);
            }
            Debug.Log($"? Necesitas la llave {candado.nombre} para abrir este candado");
        }
    }

    private IEnumerator OcultarCandadoConDelay(GameObject candadoGO, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (candadoGO != null)
        {
            candadoGO.SetActive(false);
            Debug.Log($"?? Candado ocultado después de {delay} segundos");
        }
    }

    private void IntentarAbrirPuerta()
    {
        if (candadosAbiertos >= candados.Length)
        {
            puertaAbierta = true;

            // ?? CAMBIAR EL TAG DE LA PUERTA A "Usado"
            gameObject.tag = "Usado";
            Debug.Log("??? Tag de la puerta final cambiado a 'Usado'");

            if (sonidoPuertaAbierta != null)
            {
                audioSource.volume = volumenSonidos;
                audioSource.PlayOneShot(sonidoPuertaAbierta);
            }

            Debug.Log("?? ¡PUERTA FINAL ABIERTA! ¡HAS ESCAPADO!");
            Debug.Log("?? ¡HAS GANADO EL JUEGO!");
        }
        else
        {
            int faltan = candados.Length - candadosAbiertos;
            Debug.Log($"?? Puerta bloqueada. Faltan {faltan} candado(s) por abrir.");
        }
    }

    public bool EstaAbierta()
    {
        return puertaAbierta;
    }

    public int CandadosFaltantes()
    {
        return candados.Length - candadosAbiertos;
    }

    public void ResetearPuerta()
    {
        puertaAbierta = false;
        candadosAbiertos = 0;
        foreach (Candado candado in candados)
        {
            candado.isOpen = false;
            if (candado.candadoGO != null)
            {
                candado.candadoGO.SetActive(true);
            }
        }
        Debug.Log("?? Puerta final reseteada");
    }
}