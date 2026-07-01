using UnityEngine;
using UnityEngine.InputSystem;

public class RecogerObjeto : MonoBehaviour
{
    [Header("Referencias")]
    public InteractionSystem interactionSystem;
    public InputActionReference interactAction;
    public Transform holdPosition;

    [Header("Configuración")]
    public float pickupRange = 3f;
    public float smoothSpeed = 15f;

    // Estado interno
    private GameObject currentObject = null;
    private Rigidbody currentRigidbody = null;
    private Collider currentCollider = null;
    private Vector3 originalScale;
    private bool isHolding = false;

    // Guardar configuración original del Rigidbody
    private bool originalIsKinematic;
    private RigidbodyConstraints originalConstraints;

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
            // Seguir la mano suavemente
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

        GameObject target = interactionSystem.GetTargetObject();
        if (target == null) return;

        // Verificar distancia
        float distance = Vector3.Distance(Camera.main.transform.position, target.transform.position);
        if (distance > pickupRange)
        {
            Debug.Log($"Demasiado lejos ({distance:F1}m)");
            return;
        }

        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning($"{target.name} no tiene Rigidbody");
            return;
        }

        // Guardar el objeto
        currentObject = target;
        currentRigidbody = rb;
        currentCollider = target.GetComponent<Collider>();
        originalScale = target.transform.localScale;

        // Guardar configuración original
        originalIsKinematic = rb.isKinematic;
        originalConstraints = rb.constraints;

        // Desactivar físicas COMPLETAMENTE mientras se sostiene
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        if (currentCollider != null)
        {
            currentCollider.enabled = false;
        }

        // Posicionar en la mano
        target.transform.position = holdPosition.position;
        target.transform.rotation = holdPosition.rotation;
        target.transform.SetParent(holdPosition);

        isHolding = true;
        Debug.Log($"Recogido: {target.name}");
    }

    void DropObject()
    {
        if (currentObject == null)
        {
            isHolding = false;
            return;
        }

        // Desvincular del holdPosition
        currentObject.transform.SetParent(null);

        // Restaurar físicas
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

        // Restaurar escala
        currentObject.transform.localScale = originalScale;

        string objectName = currentObject.name;
        currentObject = null;
        currentRigidbody = null;
        currentCollider = null;
        isHolding = false;

        Debug.Log($"Soltado: {objectName}");
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