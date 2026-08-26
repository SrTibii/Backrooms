using UnityEngine;
using UnityEngine.InputSystem;

public class RecogerLinterna : MonoBehaviour
{
    [Header("Referencias")]
    public InteractionSystem interactionSystem;
    public InputActionReference interactAction;
    public Transform holdPosition;

    [Header("Configuración")]
    public float pickupRange = 3f;
    public float smoothSpeed = 15f;
    public string linternaTag = "Linterna";

    [Header("Volumen")]
    [Range(0f, 1f)] public float volumenSonidos = 0.7f;

    private GameObject currentObject = null;
    private Rigidbody currentRigidbody = null;
    private Collider currentCollider = null;
    private Linterna currentLinterna = null;
    private Vector3 originalScale;
    private bool isHolding = false;
    private Material[] originalMaterials;

    private bool originalIsKinematic;
    private RigidbodyConstraints originalConstraints;

    private ManosManager manosManager;
    private Material alwaysOnTopMat;

    void Start()
    {
        if (interactionSystem == null)
            interactionSystem = FindObjectOfType<InteractionSystem>();

        if (holdPosition == null)
        {
            GameObject defaultHold = new GameObject("HoldPosition");
            defaultHold.transform.SetParent(Camera.main.transform);
            defaultHold.transform.localPosition = new Vector3(-0.5f, -0.3f, 0.8f);
            defaultHold.transform.localRotation = Quaternion.identity;
            holdPosition = defaultHold.transform;
        }

        Shader shader = Shader.Find("Custom/AlwaysOnTop");
        if (shader != null)
        {
            alwaysOnTopMat = new Material(shader);
            alwaysOnTopMat.renderQueue = 4000;
        }

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
            currentObject.transform.position = Vector3.Lerp(
                currentObject.transform.position,
                holdPosition.position,
                Time.deltaTime * smoothSpeed
            );

            currentObject.transform.rotation = Quaternion.Lerp(
                currentObject.transform.rotation,
                holdPosition.rotation,
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

        if (manosManager.manoIzquierdaOcupada)
        {
            Debug.Log("?? No puedes coger la linterna, tienes un objeto en la mano izquierda");
            return;
        }

        GameObject target = interactionSystem.GetTargetObject();
        if (target == null) return;

        if (!target.CompareTag(linternaTag))
        {
            Debug.Log($"? {target.name} no es una linterna (tag: {target.tag})");
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

        if (!manosManager.OcuparManoDerecha(target))
        {
            Debug.Log("?? Mano derecha ocupada, no puedes recoger más objetos");
            return;
        }

        currentObject = target;
        currentRigidbody = rb;
        currentCollider = target.GetComponent<Collider>();
        currentLinterna = target.GetComponent<Linterna>();
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

        // ?? APLICAR SHADER
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null && alwaysOnTopMat != null)
        {
            originalMaterials = renderer.materials;

            Material[] newMats = new Material[originalMaterials.Length];
            for (int i = 0; i < originalMaterials.Length; i++)
            {
                Material mat = new Material(alwaysOnTopMat);
                if (originalMaterials[i].mainTexture != null)
                {
                    mat.mainTexture = originalMaterials[i].mainTexture;
                }
                if (originalMaterials[i].HasProperty("_Color"))
                {
                    mat.color = originalMaterials[i].color;
                }
                newMats[i] = mat;
            }
            renderer.materials = newMats;
        }

        target.transform.position = holdPosition.position;
        target.transform.rotation = holdPosition.rotation;
        target.transform.SetParent(holdPosition);

        target.transform.localScale = originalScale;

        if (currentLinterna != null)
        {
            currentLinterna.SetInHand(true);
        }

        isHolding = true;

        // ============================================
        // REPRODUCIR SONIDO RECOGER CON AUDIOMANAGER
        // ============================================
        if (AudioManager.Instance != null && AudioManager.Instance.sonidoRecogerLinterna != null)
        {
            AudioManager.Instance.PlayOneShotAtPosition(
                AudioManager.Instance.sonidoRecogerLinterna,
                transform.position,
                volumenSonidos
            );
        }
        else
        {
            Debug.LogWarning("?? AudioManager o sonidoRecogerLinterna no disponible");
        }

        Debug.Log($"? Linterna recogida: {target.name}");
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
            manosManager.LiberarManoDerecha();
        }

        // ?? RESTAURAR MATERIALES
        Renderer renderer = currentObject.GetComponent<Renderer>();
        if (renderer != null && originalMaterials != null)
        {
            renderer.materials = originalMaterials;
            originalMaterials = null;
        }

        // ?? CALCULAR POSICIÓN SEGURA
        Camera cam = Camera.main;
        Vector3 posicionFinal;
        Quaternion rotacionFinal = holdPosition.rotation;

        if (cam != null)
        {
            Vector3 camForward = cam.transform.forward;
            float distanciaSegura = 0.3f;

            RaycastHit hit;
            Vector3 startPos = cam.transform.position;
            float maxDistance = 3f;

            if (Physics.Raycast(startPos, camForward, out hit, maxDistance))
            {
                posicionFinal = hit.point - camForward * distanciaSegura;
            }
            else
            {
                posicionFinal = holdPosition.position + camForward * 0.3f;
            }
        }
        else
        {
            posicionFinal = holdPosition.position;
        }

        if (currentLinterna != null)
        {
            currentLinterna.SetInHand(false);
        }

        currentObject.transform.SetParent(null);
        currentObject.transform.position = posicionFinal;
        currentObject.transform.rotation = rotacionFinal;

        currentObject.transform.localScale = originalScale;

        if (currentRigidbody != null)
        {
            currentRigidbody.isKinematic = false;
            currentRigidbody.useGravity = true;
            currentRigidbody.constraints = RigidbodyConstraints.None;
            currentRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            currentRigidbody.linearVelocity = Vector3.zero;
            currentRigidbody.angularVelocity = Vector3.zero;
            currentRigidbody.linearVelocity = Vector3.down * 0.5f;
        }

        if (currentCollider != null)
        {
            currentCollider.enabled = true;
        }

        // ============================================
        // REPRODUCIR SONIDO SOLTAR CON AUDIOMANAGER
        // ============================================
        if (AudioManager.Instance != null && AudioManager.Instance.sonidoSoltarLinterna != null)
        {
            AudioManager.Instance.PlayOneShotAtPosition(
                AudioManager.Instance.sonidoSoltarLinterna,
                posicionFinal,
                volumenSonidos
            );
        }
        else
        {
            Debug.LogWarning("?? AudioManager o sonidoSoltarLinterna no disponible");
        }

        string objectName = currentObject.name;
        currentObject = null;
        currentRigidbody = null;
        currentCollider = null;
        currentLinterna = null;
        isHolding = false;

        Debug.Log($"? Linterna soltada: {objectName}");
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