using UnityEngine;
using UnityEngine.InputSystem;

public class RecogerMartillo : MonoBehaviour
{
    [Header("Referencias")]
    public InteractionSystem interactionSystem;
    public InputActionReference interactAction;
    public Transform holdPosition; // Mano izquierda (o la que quieras)

    [Header("Configuración")]
    public float pickupRange = 3f;
    public float smoothSpeed = 15f;
    public string martilloTag = "Martillo";

    [Header("Sonidos")]
    public AudioClip sonidoRecogerMartillo;
    public AudioClip sonidoSoltarMartillo;
    [Range(0f, 1f)] public float volumenSonidos = 0.7f;

    // Estado interno
    private GameObject currentObject = null;
    private Rigidbody currentRigidbody = null;
    private Collider currentCollider = null;
    private Vector3 originalScale;
    private bool isHolding = false;

    private bool originalIsKinematic;
    private RigidbodyConstraints originalConstraints;

    private ManosManager manosManager;
    private AudioSource audioSource;

    void Start()
    {
        if (interactionSystem == null)
            interactionSystem = FindObjectOfType<InteractionSystem>();

        if (holdPosition == null)
        {
            GameObject defaultHold = new GameObject("HoldPosition");
            defaultHold.transform.SetParent(Camera.main.transform);
            defaultHold.transform.localPosition = new Vector3(0.5f, -0.3f, 0.8f);
            defaultHold.transform.localRotation = Quaternion.identity;
            holdPosition = defaultHold.transform;
            Debug.Log("HoldPosition no asignado. Se ha creado uno por defecto.");
        }

        // Configurar AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        // Buscar el ManosManager
        manosManager = FindObjectOfType<ManosManager>();
        if (manosManager == null)
        {
            Debug.LogError("? No se encontró ManosManager en la escena");
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
        HandleInteraction();
    }

    void Update()
    {
        if (isHolding && currentObject != null)
        {
            Vector3 targetPosition = holdPosition.position;
            Quaternion targetRotation = holdPosition.rotation;

            currentObject.transform.position = Vector3.Lerp(
                currentObject.transform.position,
                targetPosition,
                Time.deltaTime * smoothSpeed
            );

            currentObject.transform.rotation = Quaternion.Lerp(
                currentObject.transform.rotation,
                targetRotation,
                Time.deltaTime * smoothSpeed
            );
        }
    }

    void HandleInteraction()
    {
        if (isHolding)
        {
            DropObject();
            return;
        }

        TryPickUp();
    }

    void TryPickUp()
    {
        if (interactionSystem == null) return;
        if (manosManager == null) return;

        // ?? Verificar si la mano derecha (linterna) está ocupada
        if (manosManager.manoDerechaOcupada)
        {
            Debug.Log("?? No puedes coger el martillo, tienes la linterna en la mano derecha");
            return;
        }

        // ?? Verificar si la mano izquierda (objetos) está ocupada
        if (manosManager.manoIzquierdaOcupada)
        {
            Debug.Log("?? No puedes coger el martillo, tienes un objeto en la mano izquierda");
            return;
        }

        GameObject target = interactionSystem.GetTargetObject();
        if (target == null) return;

        if (!target.CompareTag(martilloTag))
        {
            Debug.Log($"? {target.name} no es un martillo (tag: {target.tag})");
            return;
        }

        float distance = Vector3.Distance(Camera.main.transform.position, target.transform.position);
        if (distance > pickupRange)
        {
            Debug.Log($"?? Demasiado lejos ({distance:F1}m)");
            return;
        }

        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning($"?? {target.name} no tiene Rigidbody");
            return;
        }

        // ?? Intentar ocupar la mano izquierda (o derecha, según prefieras)
        if (!manosManager.OcuparManoIzquierda(target))
        {
            Debug.Log("?? Mano izquierda ocupada, no puedes recoger más objetos");
            return;
        }

        currentObject = target;
        currentRigidbody = rb;
        currentCollider = target.GetComponent<Collider>();
        originalScale = target.transform.localScale;

        originalIsKinematic = rb.isKinematic;
        originalConstraints = rb.constraints;

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        if (currentCollider != null)
        {
            currentCollider.enabled = false;
        }

        target.transform.position = holdPosition.position;
        target.transform.rotation = holdPosition.rotation;
        target.transform.SetParent(holdPosition);

        target.transform.localScale = originalScale;

        isHolding = true;

        // ?? REPRODUCIR SONIDO DE RECOGER MARTILLO
        if (sonidoRecogerMartillo != null)
        {
            audioSource.volume = volumenSonidos;
            audioSource.PlayOneShot(sonidoRecogerMartillo);
        }

        Debug.Log($"?? Martillo recogido: {target.name}");
    }

    void DropObject()
    {
        if (currentObject == null)
        {
            isHolding = false;
            return;
        }

        Vector3 currentPosition = currentObject.transform.position;
        Quaternion currentRotation = currentObject.transform.rotation;

        if (manosManager != null)
        {
            manosManager.LiberarManoIzquierda();
        }

        currentObject.transform.SetParent(null);

        currentObject.transform.position = currentPosition;
        currentObject.transform.rotation = currentRotation;

        currentObject.transform.localScale = originalScale;

        if (currentRigidbody != null)
        {
            currentRigidbody.isKinematic = false;
            currentRigidbody.useGravity = true;
            currentRigidbody.constraints = RigidbodyConstraints.None;

            currentRigidbody.linearVelocity = Vector3.zero;
            currentRigidbody.angularVelocity = Vector3.zero;
            currentRigidbody.linearVelocity = Vector3.down * 0.5f;
        }

        if (currentCollider != null)
        {
            currentCollider.enabled = true;
        }

        // ?? REPRODUCIR SONIDO DE SOLTAR MARTILLO
        if (sonidoSoltarMartillo != null)
        {
            audioSource.volume = volumenSonidos;
            audioSource.PlayOneShot(sonidoSoltarMartillo);
        }

        string objectName = currentObject.name;
        currentObject = null;
        currentRigidbody = null;
        currentCollider = null;
        isHolding = false;

        Debug.Log($"?? Martillo soltado: {objectName}");
    }

    public bool IsHolding()
    {
        return isHolding;
    }

    public GameObject GetHeldObject()
    {
        return currentObject;
    }

    public void ForceDrop()
    {
        if (isHolding)
        {
            DropObject();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (holdPosition != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(holdPosition.position, 0.2f);
            Gizmos.DrawLine(holdPosition.position, holdPosition.position + holdPosition.forward * 0.3f);
        }
    }
}