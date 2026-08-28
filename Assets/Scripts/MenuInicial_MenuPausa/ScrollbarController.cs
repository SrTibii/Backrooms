using UnityEngine;
using UnityEngine.UI;

public class ScrollbarController : MonoBehaviour
{
    [Header("Referencias")]
    public ScrollRect scrollRect;
    public Scrollbar scrollbarVertical;

    [Header("Configuración")]
    public float scrollSensibilidad = 20f;
    public bool resetearAlInicio = true;

    void Start()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        if (scrollbarVertical == null && scrollRect != null)
            scrollbarVertical = scrollRect.verticalScrollbar;

        if (resetearAlInicio && scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }

        // Forzar que el scrollbar siempre sea visible
        if (scrollbarVertical != null)
        {
            scrollbarVertical.gameObject.SetActive(true);
        }

        // ?? AUMENTAR LA SENSIBILIDAD DEL SCROLL
        if (scrollRect != null)
        {
            scrollRect.scrollSensitivity = scrollSensibilidad;
        }
    }

    void Update()
    {
        if (scrollbarVertical != null && !scrollbarVertical.gameObject.activeSelf)
        {
            scrollbarVertical.gameObject.SetActive(true);
        }

        // ?? FORZAR QUE EL CONTENT TENGA TAMAÑO SUFICIENTE
        if (scrollRect != null && scrollRect.content != null)
        {
            // Si el contenido es más pequeño que el viewport, no hay scroll
            RectTransform content = scrollRect.content;
            RectTransform viewport = scrollRect.viewport;

            if (content != null && viewport != null)
            {
                // El content debe ser más alto que el viewport para que haya scroll
                if (content.rect.height <= viewport.rect.height)
                {
                    // Forzar un tamaño mínimo para que haya scroll
                    content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, viewport.rect.height + 100);
                }
            }
        }
    }

    public void ResetearScroll()
    {
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public void ResetearScrollFinal()
    {
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}