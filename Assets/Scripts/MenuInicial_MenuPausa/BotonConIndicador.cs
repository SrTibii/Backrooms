using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BotonConIndicador : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referencias")]
    public RawImage indicador; // El cuadrado blanco que aparece a la izquierda

    [Header("Configuración")]
    [Tooltip("Offset desde el botón hacia la izquierda")]
    public float offsetX = -30f;

    [Tooltip("Offset vertical (0 = centrado)")]
    public float offsetY = 0f;

    [Tooltip("Tamaño del cuadrado")]
    public float tamañoIndicador = 20f;

    [Tooltip("Velocidad de aparición/desaparición (0 = instantáneo)")]
    public float velocidadAnimacion = 0.1f;

    private RectTransform rectTransform;
    private RectTransform botonRect;
    private Color colorActual;
    private bool isVisible = false;
    private float alphaActual = 0f;
    private float alphaTarget = 0f;

    void Start()
    {
        if (indicador != null)
        {
            // Asegurar que el indicador está activo pero invisible
            indicador.gameObject.SetActive(true);

            rectTransform = indicador.GetComponent<RectTransform>();
            botonRect = GetComponent<RectTransform>();

            // Configurar color inicial (totalmente transparente)
            colorActual = indicador.color;
            colorActual.a = 0f;
            indicador.color = colorActual;
            alphaActual = 0f;
            alphaTarget = 0f;

            // Posicionar a la izquierda del botón
            if (rectTransform != null && botonRect != null)
            {
                // Hacer que el indicador sea hijo del Canvas (no del botón)
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    rectTransform.SetParent(canvas.transform);
                }
                else
                {
                    rectTransform.SetParent(botonRect.parent);
                }

                // Posicionar relativo al botón
                Vector2 botonPos = botonRect.anchoredPosition;
                Vector2 pos = new Vector2(botonPos.x + offsetX, botonPos.y + offsetY);
                rectTransform.anchoredPosition = pos;

                // Tamaño del cuadrado
                rectTransform.sizeDelta = new Vector2(tamañoIndicador, tamañoIndicador);

                // Anclas en el centro
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }

            // Asegurar que el Raycast Target está desactivado para no bloquear clics
            indicador.raycastTarget = false;
        }
        else
        {
            Debug.LogError($"?? {gameObject.name}: No se ha asignado el indicador");
        }
    }

    void Update()
    {
        // Actualizar el alpha con animación suave
        if (Mathf.Abs(alphaActual - alphaTarget) > 0.01f)
        {
            alphaActual = Mathf.Lerp(alphaActual, alphaTarget, Time.deltaTime * (1f / Mathf.Max(velocidadAnimacion, 0.01f)));

            Color c = indicador.color;
            c.a = alphaActual;
            indicador.color = c;
        }
        else if (alphaActual != alphaTarget)
        {
            alphaActual = alphaTarget;
            Color c = indicador.color;
            c.a = alphaActual;
            indicador.color = c;
        }
    }

    // ============================================
    // CUANDO EL RATÓN ENTRA EN EL BOTÓN
    // ============================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"?? Ratón entró en {gameObject.name}");
        alphaTarget = 1f;
    }

    // ============================================
    // CUANDO EL RATÓN SALE DEL BOTÓN
    // ============================================

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"?? Ratón salió de {gameObject.name}");
        alphaTarget = 0f;
    }

    // ============================================
    // MÉTODO PARA ACTUALIZAR LA POSICIÓN (OPCIONAL)
    // ============================================

    public void ActualizarPosicion(float nuevoOffsetX, float nuevoOffsetY)
    {
        offsetX = nuevoOffsetX;
        offsetY = nuevoOffsetY;

        if (rectTransform != null && botonRect != null)
        {
            Vector2 pos = botonRect.anchoredPosition;
            pos.x += offsetX;
            pos.y += offsetY;
            rectTransform.anchoredPosition = pos;
        }
    }
}