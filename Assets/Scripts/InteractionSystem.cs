using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; // IMPORTANTE: Necesario para el Input System

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

    [Header("Cámara de Juego")]
    public Camera gameCamera;

    // Variables internas
    private float currentSize;
    private Color currentColor;
    private bool isLookingAtObject = false;
    private GameObject currentTarget;

    // Puente entre el Arduino y el sistema de interacción
    private bool botonInteraccionAnterior = false;

    void Start()
    {
        if (gameCamera == null)
        {
            Camera[] allCameras = FindObjectsOfType<Camera>();
            foreach (Camera cam in allCameras)
            {
                if (cam.gameObject.layer != 5 && cam.gameObject.tag == "MainCamera")
                {
                    gameCamera = cam;
                    break;
                }
            }
            if (gameCamera == null) gameCamera = Camera.main;
        }

        if (crosshair != null)
        {
            if (!crosshair.gameObject.activeSelf) crosshair.gameObject.SetActive(true);
            crosshair.anchorMin = new Vector2(0.5f, 0.5f);
            crosshair.anchorMax = new Vector2(0.5f, 0.5f);
            crosshair.pivot = new Vector2(0.5f, 0.5f);
            currentSize = normalSize;
            crosshair.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentSize);
            crosshair.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentSize);
            Image crosshairImage = crosshair.GetComponent<Image>();
            if (crosshairImage != null)
            {
                crosshairImage.color = normalColor;
                currentColor = normalColor;
            }
        }
    }

    void Update()
    {
        // NUEVO: Leer el clic derecho del Arduino
        if (ArduinoManager.Instance != null)
        {
            if (ArduinoManager.Instance.clickDerecho && !botonInteraccionAnterior)
            {
                InteractuarConObjetoActual();
            }
            botonInteraccionAnterior = ArduinoManager.Instance.clickDerecho;
        }

        PerformRaycast();
        UpdateCrosshair();
    }

    void PerformRaycast()
    {
        if (gameCamera == null) return;

        Vector3 centerPoint = new Vector3(0.5f, 0.5f, 0f);
        Ray ray = gameCamera.ViewportPointToRay(centerPoint);

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
        if (crosshair == null || !crosshair.gameObject.activeInHierarchy) return;

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

    public void InteractuarConObjetoActual()
    {
        if (currentTarget == null) return;

        Debug.Log($"?? Clic derecho del Arduino en: {currentTarget.name}");

        // 1. Puerta normal
        DoorInteraction puerta = currentTarget.GetComponent<DoorInteraction>();
        if (puerta != null)
        {
            puerta.HandleInteraction();
            return;
        }

        // 2. Taquilla
        LockerHideSystem taquilla = currentTarget.GetComponent<LockerHideSystem>();
        if (taquilla != null)
        {
            if (taquilla.IsHiding()) taquilla.ExitLocker();
            else taquilla.EnterLocker();
            return;
        }

        // 3. Cajón
        DrawerInteraction cajon = currentTarget.GetComponent<DrawerInteraction>();
        if (cajon != null)
        {
            cajon.HandleInteraction();
            return;
        }

        // 4. Generador (Usa SendMessage porque su método es privado)
        Generador generador = currentTarget.GetComponent<Generador>();
        if (generador != null)
        {
            generador.SendMessage("OnInteractPerformed", new UnityEngine.InputSystem.InputAction.CallbackContext(), SendMessageOptions.DontRequireReceiver);
            return;
        }

        // 5. Caja Fuerte
        CajaFuerte caja = currentTarget.GetComponent<CajaFuerte>();
        if (caja != null)
        {
            caja.SendMessage("OnInteractPerformed", new UnityEngine.InputSystem.InputAction.CallbackContext(), SendMessageOptions.DontRequireReceiver);
            return;
        }

        // 6. Notas (LeerNota)
        LeerNota nota = currentTarget.GetComponent<LeerNota>();
        if (nota != null)
        {
            nota.SendMessage("OnInteractPerformed", new UnityEngine.InputSystem.InputAction.CallbackContext(), SendMessageOptions.DontRequireReceiver);
            return;
        }
    }

    public bool IsLookingAtInteractable() { return isLookingAtObject; }

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
                if (hit.collider.CompareTag(tag)) return hit.collider.gameObject;
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
                if (hit.collider.CompareTag(tag)) return hit.distance;
            }
        }
        return -1f;
    }
}