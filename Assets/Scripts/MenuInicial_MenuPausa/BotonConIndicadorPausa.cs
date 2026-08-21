using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BotonConIndicadorPausa : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referencias")]
    public RawImage indicador;

    [Header("Configuración")]
    public float offsetX = -40f;
    public float offsetY = 0f;
    public float tamañoIndicador = 20f;
    public float velocidadAnimacion = 0.1f;

    private RectTransform rectTransform;
    private RectTransform botonRect;
    private Color colorActual;
    private float alphaActual = 0f;
    private float alphaTarget = 0f;
    private Canvas canvas;
    private bool isInitialized = false;

    void Start()
    {
        Inicializar();
    }

    void OnEnable()
    {
        if (isInitialized)
        {
            ReposicionarIndicador();
        }
    }

    void Inicializar()
    {
        if (indicador == null)
        {
            Debug.LogError($"?? {gameObject.name}: No se ha asignado el indicador");
            return;
        }

        rectTransform = indicador.GetComponent<RectTransform>();
        botonRect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        if (rectTransform == null || botonRect == null || canvas == null)
        {
            Debug.LogError($"?? {gameObject.name}: Faltan componentes necesarios");
            return;
        }

        indicador.gameObject.SetActive(true);
        indicador.raycastTarget = false;

        rectTransform.SetParent(botonRect.parent, false);

        colorActual = indicador.color;
        colorActual.a = 0f;
        indicador.color = colorActual;
        alphaActual = 0f;
        alphaTarget = 0f;

        rectTransform.sizeDelta = new Vector2(tamañoIndicador, tamañoIndicador);

        rectTransform.anchorMin = new Vector2(0f, 0.5f);
        rectTransform.anchorMax = new Vector2(0f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        ReposicionarIndicador();

        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized || indicador == null) return;

        if (Mathf.Abs(alphaActual - alphaTarget) > 0.01f)
        {
            alphaActual = Mathf.Lerp(alphaActual, alphaTarget, Time.unscaledDeltaTime * (1f / Mathf.Max(velocidadAnimacion, 0.01f)));

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

    public void ReposicionarIndicador()
    {
        if (rectTransform == null || botonRect == null) return;

        Vector2 botonPos = botonRect.anchoredPosition;
        Vector2 pos = new Vector2(botonPos.x + offsetX, botonPos.y + offsetY);
        rectTransform.anchoredPosition = pos;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        alphaTarget = 1f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        alphaTarget = 0f;
    }

    public void SetOffset(float x, float y)
    {
        offsetX = x;
        offsetY = y;
        ReposicionarIndicador();
    }

    public void SetTamaño(float nuevoTamaño)
    {
        tamañoIndicador = nuevoTamaño;
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(tamañoIndicador, tamañoIndicador);
        }
    }

    public void MostrarInstantaneo(bool mostrar)
    {
        alphaTarget = mostrar ? 1f : 0f;
        alphaActual = alphaTarget;
        Color c = indicador.color;
        c.a = alphaActual;
        indicador.color = c;
    }

    // ============================================
    // NUEVO: OCULTAR TODOS LOS INDICADORES
    // ============================================

    public static void OcultarTodosLosIndicadores()
    {
        BotonConIndicadorPausa[] todosLosIndicadores = FindObjectsOfType<BotonConIndicadorPausa>(true);
        foreach (var indicador in todosLosIndicadores)
        {
            if (indicador != null)
            {
                indicador.MostrarInstantaneo(false);
            }
        }
    }
}