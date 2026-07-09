using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class MaquinaExpendedora : MonoBehaviour
{
    [Header("Configuración")]
    public float interactionDistance = 3f;
    public float tiempoEspera = 2f;

    [Header("Tags")]
    public string monedaTag = "Moneda";

    [Header("Llaves")]
    public GameObject llaveFalsa;
    public GameObject llaveBuena;

    [Header("Audio")]
    public AudioClip sonidoInsertarMoneda;
    public AudioClip sonidoCaerLlave;
    [Range(0f, 1f)] public float volumenSonidos = 0.7f;

    [Header("Input")]
    public InputActionReference interactAction;

    // Estado interno
    private bool monedaInsertada = false;
    private bool llaveEntregada = false;
    private AudioSource audioSource;
    private InteractionSystem interactionSystem;

    void Start()
    {
        // Configurar AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        // Buscar referencias
        interactionSystem = FindObjectOfType<InteractionSystem>();
        if (interactionSystem == null)
        {
            //Debug.LogError("? No se encontró InteractionSystem en la escena");
        }

        // Estado inicial
        if (llaveFalsa != null) llaveFalsa.SetActive(true);
        if (llaveBuena != null) llaveBuena.SetActive(false);

        //Debug.Log("?? Máquina expendedora inicializada correctamente");
    }

    private void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed += OnInteractPerformed;
            interactAction.action.Enable();
            //Debug.Log("? Input Action 'Interact' habilitado");
        }
        else
        {
            //Debug.LogError("? interactAction NO está asignado en el Inspector");
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
        Debug.Log("?? Tecla de interacción presionada");

        // Si ya se insertó una moneda o ya se entregó la llave
        if (monedaInsertada || llaveEntregada)
        {
            //Debug.Log($"? Máquina ya usada (monedaInsertada={monedaInsertada}, llaveEntregada={llaveEntregada})");
            return;
        }

        // ?? Verificar si está mirando la máquina
        if (!IsLookingAtMachine())
        {
            //Debug.Log("?? No estás mirando la máquina expendedora");
            return;
        }

        // ?? Verificar si tiene una moneda en la mano
        if (!TieneMonedaEnMano())
        {
            //Debug.Log("?? No tienes una moneda en la mano");
            return;
        }

        //Debug.Log("? Todas las condiciones cumplidas. Insertando moneda...");
        StartCoroutine(InsertarMoneda());
    }

    private bool IsLookingAtMachine()
    {
        if (interactionSystem == null)
        {
            //Debug.LogWarning("?? InteractionSystem es NULL, usando Raycast directo");
            return IsLookingAtMachineDirect();
        }

        GameObject target = interactionSystem.GetTargetObject();
        if (target == null)
        {
            //Debug.Log("?? No hay objeto en el punto de mira");
            return false;
        }

        //Debug.Log($"?? Objeto en mira: {target.name} (tag: {target.tag})");

        // ?? Verificar si es la máquina (por tag o por GameObject)
        if (target == gameObject || target.CompareTag("MaquinaExpendedora"))
        {
            return true;
        }

        return false;
    }

    private bool IsLookingAtMachineDirect()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            //Debug.Log($"?? Raycast directo impactó en: {hit.collider.gameObject.name}");
            return hit.collider.gameObject == gameObject;
        }

        return false;
    }

    private bool TieneMonedaEnMano()
    {
        // Buscar el script de recogida de objetos
        RecogerObjeto recoger = FindObjectOfType<RecogerObjeto>();
        if (recoger == null)
        {
            //Debug.LogWarning("?? No se encontró RecogerObjeto en la escena");
            return false;
        }

        // Verificar si tiene un objeto en la mano
        GameObject objetoEnMano = recoger.GetHeldObject();
        if (objetoEnMano == null)
        {
            //Debug.Log("??? No hay objeto en la mano");
            return false;
        }

        //Debug.Log($"??? Objeto en mano: {objetoEnMano.name} (tag: {objetoEnMano.tag})");

        // Verificar si el objeto tiene el tag "Moneda"
        if (objetoEnMano.CompareTag(monedaTag))
        {
            //Debug.Log("?? ¡Es una moneda!");
            return true;
        }

        //Debug.Log($"? El objeto en mano NO es una moneda (tag: {objetoEnMano.tag})");
        return false;
    }

    private IEnumerator InsertarMoneda()
    {
        monedaInsertada = true;
        //Debug.Log("?? Iniciando inserción de moneda...");

        // ?? 1. Sonido de insertar moneda
        if (sonidoInsertarMoneda != null)
        {
            ReproducirSonido(sonidoInsertarMoneda);
            Debug.Log("?? Sonido de insertar moneda reproducido");
        }
        else
        {
            Debug.LogWarning("?? No hay sonido de insertar moneda asignado");
        }

        // ?? 2. Eliminar la moneda de la mano del jugador
        RecogerObjeto recoger = FindObjectOfType<RecogerObjeto>();
        if (recoger != null)
        {
            GameObject moneda = recoger.GetHeldObject();
            if (moneda != null)
            {
                Destroy(moneda);
                //Debug.Log("?? Moneda destruida");
                recoger.ForceDrop();
            }
            else
            {
                Debug.LogWarning("?? No se encontró moneda en la mano (ya fue destruida?)");
            }
        }

        // ?? 3. Esperar
        //Debug.Log($"? Esperando {tiempoEspera} segundos...");
        yield return new WaitForSeconds(tiempoEspera);

        // ?? 4. Sonido de caer la llave
        if (sonidoCaerLlave != null)
        {
            ReproducirSonido(sonidoCaerLlave);
            //Debug.Log("?? Sonido de caer llave reproducido");
        }
        else
        {
            //Debug.LogWarning("?? No hay sonido de caer llave asignado");
        }

        // ?? 5. Ocultar llave falsa
        if (llaveFalsa != null)
        {
            llaveFalsa.SetActive(false);
            //Debug.Log("?? Llave falsa oculta");
        }
        else
        {
            //Debug.LogWarning("?? No hay llave falsa asignada");
        }

        // ?? 6. Mostrar llave buena
        if (llaveBuena != null)
        {
            llaveBuena.SetActive(true);
            //Debug.Log("?? Llave buena activada en el suelo");
        }
        else
        {
            //Debug.LogWarning("?? No hay llave buena asignada");
        }

        llaveEntregada = true;
        monedaInsertada = false;
        //Debug.Log("? Proceso completado. Llave entregada.");
    }

    private void ReproducirSonido(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.volume = volumenSonidos;
        audioSource.PlayOneShot(clip);
    }

    //public void ResetearMaquina()
    //{
    //    monedaInsertada = false;
    //    llaveEntregada = false;
    //    if (llaveFalsa != null) llaveFalsa.SetActive(true);
    //    if (llaveBuena != null) llaveBuena.SetActive(false);
    //    Debug.Log("?? Máquina reseteada");
    //}
}