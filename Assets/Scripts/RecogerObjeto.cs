using UnityEngine;
using UnityEngine.InputSystem;

public class RecogerObjeto : MonoBehaviour
{
    [Header("Referencias")]
    public InteractionSystem interactionSystem;
    public InputActionReference interactAction;
    public Transform holdPosition; // Mano izquierda

    [Header("Configuración")]
    public float pickupRange = 3f;
    public float smoothSpeed = 15f;
    public string[] tagsValidos = { "Object" }; // Tags que puede recoger

    [Header("Sonidos por Tag")]
    public SonidoPorTag[] sonidosPorTag; // Array de sonidos según el tag

    [Header("Sonido por defecto")]
    public AudioClip sonidoPorDefecto;
    [Range(0f, 1f)] public float volumenPorDefecto = 0.7f;

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

    [System.Serializable]
    public class SonidoPorTag
    {
        public string tag;
        public AudioClip sonido;
        [Range(0f, 1f)] public float volumen = 0.7f;
    }

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
            Debug.LogError("❌ No se encontró ManosManager en la escena");
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

        // Verificar si la mano derecha está ocupada
        if (manosManager.manoDerechaOcupada)
        {
            Debug.Log("⚠️ No puedes coger nada, tienes la linterna en la mano derecha");
            return;
        }

        GameObject target = interactionSystem.GetTargetObject();
        if (target == null) return;

        // Verificar que el objeto tenga un tag válido
        bool tagValido = false;
        foreach (string tag in tagsValidos)
        {
            if (target.CompareTag(tag))
            {
                tagValido = true;
                break;
            }
        }

        if (!tagValido)
        {
            Debug.Log($"❌ {target.name} no es válido para recoger (tag: {target.tag})");
            return;
        }

        float distance = Vector3.Distance(Camera.main.transform.position, target.transform.position);
        if (distance > pickupRange)
        {
            Debug.Log($"📏 Demasiado lejos ({distance:F1}m)");
            return;
        }

        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning($"⚠️ {target.name} no tiene Rigidbody");
            return;
        }

        // Intentar ocupar la mano izquierda
        if (!manosManager.OcuparManoIzquierda(target))
        {
            Debug.Log("⚠️ Mano izquierda ocupada, no puedes recoger más objetos");
            return;
        }

        // Guardar el objeto
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

        isHolding = true;

        // 🔥 REPRODUCIR SONIDO SEGÚN EL TAG
        ReproducirSonidoPorTag(target.tag);

        Debug.Log($"✅ Recogido: {target.name} (tag: {target.tag})");
    }

    // 🔥 Método para reproducir sonido según el tag
    private void ReproducirSonidoPorTag(string tag)
    {
        AudioClip clip = null;
        float volumen = volumenPorDefecto;

        // Buscar si hay un sonido configurado para este tag
        foreach (SonidoPorTag item in sonidosPorTag)
        {
            if (item.tag == tag)
            {
                clip = item.sonido;
                volumen = item.volumen;
                break;
            }
        }

        // Si no se encontró sonido para el tag, usar el por defecto
        if (clip == null)
        {
            clip = sonidoPorDefecto;
        }

        if (clip != null)
        {
            audioSource.volume = volumen;
            audioSource.PlayOneShot(clip);
            Debug.Log($"🔊 Sonido reproducido para tag '{tag}'");
        }
        else
        {
            Debug.Log($"🔇 No hay sonido asignado para el tag '{tag}'");
        }
    }

    void DropObject()
    {
        if (currentObject == null)
        {
            isHolding = false;
            return;
        }

        if (manosManager != null)
        {
            manosManager.LiberarManoIzquierda();
        }

        currentObject.transform.SetParent(null);

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

        currentObject.transform.localScale = originalScale;

        string objectName = currentObject.name;
        currentObject = null;
        currentRigidbody = null;
        currentCollider = null;
        isHolding = false;

        Debug.Log($"✅ Soltado: {objectName}");
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