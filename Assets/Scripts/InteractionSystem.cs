using UnityEngine;
using UnityEngine.UI;

public class InteractionSystem : MonoBehaviour
{
    [Header("Configuración Raycast")]
    public float interactionRange = 3f;
    public string[] targetTags = { "Object" }; // ?? MÚLTIPLES TAGS

    [Header("Crosshair")]
    public RectTransform crosshair;
    public float normalSize = 20f;
    public float expandedSize = 35f;
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
            currentSize = normalSize;
            crosshair.sizeDelta = new Vector2(currentSize, currentSize);
            crosshair.GetComponent<Image>().color = normalColor;
            currentColor = normalColor;
        }
    }

    void Update()
    {
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
            // ?? Comprobar si el objeto tiene ALGUNO de los tags
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

        float targetSize = isLookingAtObject ? expandedSize : normalSize;
        currentSize = Mathf.Lerp(currentSize, targetSize, Time.deltaTime * sizeTransitionSpeed);
        crosshair.sizeDelta = new Vector2(currentSize, currentSize);

        Image crosshairImage = crosshair.GetComponent<Image>();
        if (crosshairImage != null)
        {
            crosshairImage.color = Color.Lerp(crosshairImage.color, currentColor, Time.deltaTime * sizeTransitionSpeed);
        }
    }

    public bool IsLookingAtInteractable()
    {
        return isLookingAtObject;
    }

    public GameObject GetTargetObject()
    {
        if (!isLookingAtObject || playerCamera == null) return null;

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