using UnityEngine;
using UnityEngine.UI;

public class InteractionSystem : MonoBehaviour
{
    [Header("Configuración Raycast")]
    public float interactionRange = 3f; // Distancia máxima de interacción
    public string targetTag = "Object"; // Tag que deben tener los objetos interactuables

    [Header("Crosshair")]
    public RectTransform crosshair; // El circulo (UI Image)
    public float normalSize = 20f; // Tamaño normal del crosshair
    public float expandedSize = 35f; // Tamaño cuando mira un objeto
    public float sizeTransitionSpeed = 8f; // Velocidad de transición

    [Header("Feedback Visual (Opcional)")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.green;

    // Variables internas
    private Camera playerCamera;
    private float currentSize;
    private Color currentColor;
    private bool isLookingAtObject = false;

    void Start()
    {
        // Buscar la cámara principal
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("InteractionSystem: No se encontró una cámara con tag 'MainCamera'");
        }

        // Configurar crosshair inicial
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
        // Lanzar el raycast desde el centro de la pantalla
        PerformRaycast();

        // Actualizar el tamaño y color del crosshair
        UpdateCrosshair();
    }

    void PerformRaycast()
    {
        if (playerCamera == null) return;

        // Crear un raycast desde el centro de la pantalla
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        // Debug: Dibujar el raycast en la escena
        Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.yellow);

        // Lanzar el raycast
        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            // Comprobar si el objeto tiene el tag correcto
            if (hit.collider.CompareTag(targetTag))
            {
                isLookingAtObject = true;

                // Opcional: Debug para ver qué objeto estamos mirando
                // Debug.Log("Mirando: " + hit.collider.gameObject.name);

                // Cambiar color del crosshair a verde
                currentColor = highlightColor;

                // Opcional: Puedes añadir más feedback aquí
                // Por ejemplo, cambiar el material del objeto o mostrar un texto
            }
            else
            {
                isLookingAtObject = false;
                currentColor = normalColor;
            }
        }
        else
        {
            isLookingAtObject = false;
            currentColor = normalColor;
        }
    }

    void UpdateCrosshair()
    {
        if (crosshair == null) return;

        // Calcular el tamaño objetivo
        float targetSize = isLookingAtObject ? expandedSize : normalSize;

        // Suavizar la transición del tamaño
        currentSize = Mathf.Lerp(currentSize, targetSize, Time.deltaTime * sizeTransitionSpeed);

        // Aplicar el tamaño al crosshair
        crosshair.sizeDelta = new Vector2(currentSize, currentSize);

        // Suavizar el cambio de color
        Image crosshairImage = crosshair.GetComponent<Image>();
        if (crosshairImage != null)
        {
            crosshairImage.color = Color.Lerp(crosshairImage.color, currentColor, Time.deltaTime * sizeTransitionSpeed);
        }
    }

    // Método público para saber si el jugador está mirando un objeto
    public bool IsLookingAtInteractable()
    {
        return isLookingAtObject;
    }

    // Obtiene el objeto que el jugador está mirando (si existe)

    public GameObject GetTargetObject()
    {
        if (!isLookingAtObject || playerCamera == null) return null;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            if (hit.collider.CompareTag(targetTag))
            {
                return hit.collider.gameObject;
            }
        }

        return null;
    }


    //Obtiene la distancia al objeto que se está mirando
    public float GetTargetDistance()
    {
        if (!isLookingAtObject || playerCamera == null) return -1f;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            if (hit.collider.CompareTag(targetTag))
            {
                return hit.distance;
            }
        }

        return -1f;
    }
}