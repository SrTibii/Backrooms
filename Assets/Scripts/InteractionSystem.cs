using UnityEngine;
using UnityEngine.UI;

public class InteractionSystem : MonoBehaviour
{
    [Header("Configuración Raycast")]
    public float interactionRange = 3f;
    public string[] targetTags = { "Object" };

    [Header("Crosshair")]
    public RectTransform crosshair;
    public float normalSize = 50f;  // Tamaño más grande 
    public float expandedSize = 70f; // Tamaño expandido
    public float sizeTransitionSpeed = 8f;

    [Header("Feedback Visual")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.green;

    // Variables internas
    private Camera playerCamera;
    private float currentSize;
    private Color currentColor;
    private bool isLookingAtObject = false;
    private GameObject currentTarget;

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("InteractionSystem: No se encontró una cámara con tag 'MainCamera'");
        }

        if (crosshair != null)
        {
            if (!crosshair.gameObject.activeSelf)
            {
                crosshair.gameObject.SetActive(true);
                Debug.Log("?? Crosshair forzado a activo en Start()");
            }

            // ============================================
            // ?? FIJAR ANCLAS EN CENTRO PARA EVITAR DEFORMACIÓN
            // ============================================
            crosshair.anchorMin = new Vector2(0.5f, 0.5f);
            crosshair.anchorMax = new Vector2(0.5f, 0.5f);
            crosshair.pivot = new Vector2(0.5f, 0.5f);

            currentSize = normalSize;
            // Usar SetSizeWithCurrentAnchors en lugar de sizeDelta
            crosshair.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentSize);
            crosshair.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentSize);

            Image crosshairImage = crosshair.GetComponent<Image>();
            if (crosshairImage != null)
            {
                Color color = crosshairImage.color;
                if (color.a < 0.1f)
                {
                    color.a = 1f;
                    crosshairImage.color = color;
                    Debug.Log("?? Crosshair alpha corregido a 1");
                }

                crosshairImage.color = normalColor;
                currentColor = normalColor;
            }
            else
            {
                Debug.LogWarning("?? El crosshair no tiene componente Image");
            }

            Debug.Log($"?? Crosshair inicializado: active={crosshair.gameObject.activeSelf}, size={currentSize}");
        }
        else
        {
            Debug.LogError("?? Crosshair NO ASIGNADO en InteractionSystem");
        }
    }

    void Update()
    {
        if (crosshair != null && !crosshair.gameObject.activeSelf)
        {
            crosshair.gameObject.SetActive(true);
            Debug.Log("?? Crosshair reactivado en Update()");
        }

        PerformRaycast();
        UpdateCrosshair();
    }

    void PerformRaycast()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.yellow);

        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            bool hasValidTag = false;
            foreach (string tag in targetTags)
            {
                if (hit.collider.CompareTag(tag))
                {
                    hasValidTag = true;
                    break;
                }
            }

            if (hasValidTag)
            {
                isLookingAtObject = true;
                currentTarget = hit.collider.gameObject;
                currentColor = highlightColor;
            }
            else
            {
                isLookingAtObject = false;
                currentTarget = null;
                currentColor = normalColor;
            }
        }
        else
        {
            isLookingAtObject = false;
            currentTarget = null;
            currentColor = normalColor;
        }
    }

    void UpdateCrosshair()
    {
        if (crosshair == null) return;
        if (!crosshair.gameObject.activeInHierarchy) return;

        float targetSize = isLookingAtObject ? expandedSize : normalSize;
        currentSize = Mathf.Lerp(currentSize, targetSize, Time.deltaTime * sizeTransitionSpeed);

        // ============================================
        // ?? USAR SetSizeWithCurrentAnchors EN VEZ DE sizeDelta
        // ============================================
        crosshair.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentSize);
        crosshair.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentSize);

        Image crosshairImage = crosshair.GetComponent<Image>();
        if (crosshairImage != null)
        {
            crosshairImage.color = Color.Lerp(crosshairImage.color, currentColor, Time.deltaTime * sizeTransitionSpeed);
        }
    }

    public bool IsLookingAtInteractable()
    {
        if (crosshair != null && !crosshair.gameObject.activeInHierarchy) return false;
        return isLookingAtObject;
    }

    public GameObject GetTargetObject()
    {
        if (!isLookingAtObject || playerCamera == null) return null;
        if (crosshair != null && !crosshair.gameObject.activeInHierarchy) return null;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            foreach (string tag in targetTags)
            {
                if (hit.collider.CompareTag(tag))
                {
                    return hit.collider.gameObject;
                }
            }
        }

        return null;
    }

    public float GetTargetDistance()
    {
        if (!isLookingAtObject || playerCamera == null) return -1f;
        if (crosshair != null && !crosshair.gameObject.activeInHierarchy) return -1f;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            foreach (string tag in targetTags)
            {
                if (hit.collider.CompareTag(tag))
                {
                    return hit.distance;
                }
            }
        }

        return -1f;
    }
}