using UnityEngine;
using UnityEngine.UI;

public class InteractionSystem : MonoBehaviour
{
    [Header("Configuración Raycast")]
    public float interactionRange = 3f;
    public string[] targetTags = { "Object" };

    [Header("Crosshair")]
    public RectTransform crosshair;
    public float normalSize = 50f;
    public float expandedSize = 70f;
    public float sizeTransitionSpeed = 8f;

    [Header("Feedback Visual")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.green;

    // ============================================
    // REFERENCIA A LA CÁMARA DE JUEGO
    // ============================================
    [Header("Cámara de Juego")]
    public Camera gameCamera; // Arrastra la cámara que renderiza la escena

    // Variables internas
    private float currentSize;
    private Color currentColor;
    private bool isLookingAtObject = false;
    private GameObject currentTarget;

    void Start()
    {
        // Buscar la cámara de juego si no está asignada
        if (gameCamera == null)
        {
            // Buscar la cámara que NO es la de UI (layer 5 = UI)
            Camera[] allCameras = FindObjectsOfType<Camera>();
            foreach (Camera cam in allCameras)
            {
                if (cam.gameObject.layer != 5 && cam.gameObject.tag == "MainCamera")
                {
                    gameCamera = cam;
                    break;
                }
            }

            // Si no se encontró, usar Camera.main
            if (gameCamera == null)
            {
                gameCamera = Camera.main;
                Debug.LogWarning("InteractionSystem: Usando Camera.main como fallback");
            }
        }

        if (gameCamera == null)
        {
            Debug.LogError("InteractionSystem: No se encontró una cámara de juego");
        }
        else
        {
            Debug.Log($"?? InteractionSystem: Cámara de juego asignada - {gameCamera.name}");
        }

        if (crosshair != null)
        {
            if (!crosshair.gameObject.activeSelf)
            {
                crosshair.gameObject.SetActive(true);
                Debug.Log("?? Crosshair forzado a activo en Start()");
            }

            crosshair.anchorMin = new Vector2(0.5f, 0.5f);
            crosshair.anchorMax = new Vector2(0.5f, 0.5f);
            crosshair.pivot = new Vector2(0.5f, 0.5f);

            currentSize = normalSize;
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

    // ============================================
    // RAYCAST - Usando la cámara de juego
    // ============================================

    void PerformRaycast()
    {
        if (gameCamera == null) return;

        // ============================================
        // USAR EL CENTRO DE LA CÁMARA DE JUEGO
        // ViewportPointToRay(0.5, 0.5) = centro exacto de la cámara
        // Esto funciona tanto con pantalla normal como con Render Texture
        // ============================================
        Vector3 centerPoint = new Vector3(0.5f, 0.5f, 0f);
        Ray ray = gameCamera.ViewportPointToRay(centerPoint);

        Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.yellow);

        RaycastHit hit;

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

        crosshair.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentSize);
        crosshair.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentSize);

        Image crosshairImage = crosshair.GetComponent<Image>();
        if (crosshairImage != null)
        {
            crosshairImage.color = Color.Lerp(crosshairImage.color, currentColor, Time.deltaTime * sizeTransitionSpeed);
        }
    }

    // ============================================
    // MÉTODOS PÚBLICOS
    // ============================================

    public bool IsLookingAtInteractable()
    {
        if (crosshair != null && !crosshair.gameObject.activeInHierarchy) return false;
        return isLookingAtObject;
    }

    public GameObject GetTargetObject()
    {
        if (!isLookingAtObject || gameCamera == null) return null;
        if (crosshair != null && !crosshair.gameObject.activeInHierarchy) return null;

        Vector3 centerPoint = new Vector3(0.5f, 0.5f, 0f);
        Ray ray = gameCamera.ViewportPointToRay(centerPoint);
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
        if (!isLookingAtObject || gameCamera == null) return -1f;
        if (crosshair != null && !crosshair.gameObject.activeInHierarchy) return -1f;

        Vector3 centerPoint = new Vector3(0.5f, 0.5f, 0f);
        Ray ray = gameCamera.ViewportPointToRay(centerPoint);
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