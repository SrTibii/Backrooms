using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BotonConIndicador : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referencias")]
    public RawImage indicador;

    [Header("Configuración")]
    public float offsetX = -30f;
    public float offsetY = 0f;
    public float tamañoIndicador = 20f;
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
            indicador.gameObject.SetActive(true);

            rectTransform = indicador.GetComponent<RectTransform>();
            botonRect = GetComponent<RectTransform>();

            colorActual = indicador.color;
            colorActual.a = 0f;
            indicador.color = colorActual;
            alphaActual = 0f;
            alphaTarget = 0f;

            if (rectTransform != null && botonRect != null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    rectTransform.SetParent(canvas.transform);
                }
                else
                {
                    rectTransform.SetParent(botonRect.parent);
                }

                Vector2 botonPos = botonRect.anchoredPosition;
                Vector2 pos = new Vector2(botonPos.x + offsetX, botonPos.y + offsetY);
                rectTransform.anchoredPosition = pos;

                rectTransform.sizeDelta = new Vector2(tamañoIndicador, tamañoIndicador);

                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }

            indicador.raycastTarget = false;
        }
        else
        {
            Debug.LogError($"? {gameObject.name}: No se ha asignado el indicador");
        }
    }

    void Update()
    {
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        alphaTarget = 1f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        alphaTarget = 0f;
    }

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

    // ============================================
    // ?? MÉTODO PARA FORZAR LA OCULTACIÓN DEL INDICADOR
    // ============================================

    public void ForzarOcultar()
    {
        alphaTarget = 0f;
        alphaActual = 0f;

        if (indicador != null)
        {
            Color c = indicador.color;
            c.a = 0f;
            indicador.color = c;
        }

        isVisible = false;
    }
}