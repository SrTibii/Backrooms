using UnityEngine;
using TMPro;

public class MostrarFPS : MonoBehaviour
{
    [Header("Referencias")]
    public TextMeshProUGUI textoFPS;

    [Header("Configuración")]
    [Tooltip("Si está activado, muestra los FPS")]
    public bool mostrarFPS = true;

    [Header("Posición en Pantalla")]
    [Range(0f, 1f)]
    public float posicionX = 0.02f;

    [Range(0f, 1f)]
    public float posicionY = 0.98f;

    [Header("Apariencia")]
    public Color colorFPS = Color.white;
    public float tamañoFuente = 16f;
    public string formato = "FPS: {0}";

    [Header("Colores por Rendimiento")]
    public bool cambiarColorPorRendimiento = true;
    public Color colorBueno = Color.green;
    public Color colorMedio = Color.yellow;
    public Color colorMalo = Color.red;
    public int fpsBueno = 60;
    public int fpsMedio = 30;

    private float tiempoActual = 0f;
    private int frames = 0;
    private int fpsActual = 0;
    private RectTransform rectTransform;

    void Start()
    {
        if (textoFPS == null)
        {
            CrearTextoFPS();
        }

        rectTransform = textoFPS.GetComponent<RectTransform>();
        ActualizarPosicion();

        // ============================================
        // NO CARGAR PlayerPrefs AQUÍ
        // El estado lo controla MenuOpciones
        // Solo aplicar el estado actual de mostrarFPS
        // ============================================
        if (textoFPS != null)
        {
            textoFPS.gameObject.SetActive(mostrarFPS);
        }

        Debug.Log($"?? MostrarFPS inicializado. Visible: {mostrarFPS}");
    }

    void Update()
    {
        tiempoActual += Time.unscaledDeltaTime;
        frames++;

        if (tiempoActual >= 1f)
        {
            fpsActual = Mathf.RoundToInt(frames / tiempoActual);
            tiempoActual = 0f;
            frames = 0;

            ActualizarTexto();
        }
    }

    void ActualizarTexto()
    {
        if (textoFPS == null) return;

        textoFPS.text = string.Format(formato, fpsActual);

        if (cambiarColorPorRendimiento)
        {
            if (fpsActual >= fpsBueno)
                textoFPS.color = colorBueno;
            else if (fpsActual >= fpsMedio)
                textoFPS.color = colorMedio;
            else
                textoFPS.color = colorMalo;
        }
        else
        {
            textoFPS.color = colorFPS;
        }
    }

    void ActualizarPosicion()
    {
        if (rectTransform == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        Vector2 tamanioCanvas = canvas.GetComponent<RectTransform>().rect.size;

        float x = posicionX * tamanioCanvas.x;
        float y = posicionY * tamanioCanvas.y;

        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.pivot = new Vector2(0, 1);

        rectTransform.anchoredPosition = new Vector2(x, y);

        textoFPS.fontSize = tamañoFuente;
        textoFPS.color = colorFPS;
    }

    void CrearTextoFPS()
    {
        GameObject go = new GameObject("FPS_Text");
        go.transform.SetParent(transform);

        rectTransform = go.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 50);

        textoFPS = go.AddComponent<TextMeshProUGUI>();
        textoFPS.fontSize = tamañoFuente;
        textoFPS.color = colorFPS;
        textoFPS.alignment = TextAlignmentOptions.Left;
        textoFPS.text = "FPS: 0";
    }

    // ============================================
    // MÉTODOS PÚBLICOS
    // ============================================

    public void ToggleMostrarFPS(bool activar)
    {
        mostrarFPS = activar;
        PlayerPrefs.SetInt("MostrarFPS", activar ? 1 : 0);
        PlayerPrefs.Save();

        if (textoFPS != null)
        {
            textoFPS.gameObject.SetActive(activar);
        }

        Debug.Log($"?? Mostrar FPS: {(activar ? "ACTIVADO" : "DESACTIVADO")}");
    }

    public void AlternarFPS()
    {
        ToggleMostrarFPS(!mostrarFPS);
    }

    public bool EstaActivo()
    {
        return mostrarFPS;
    }

    public int GetFPSActual()
    {
        return fpsActual;
    }

    public void SetPosicion(float x, float y)
    {
        posicionX = Mathf.Clamp01(x);
        posicionY = Mathf.Clamp01(y);
        ActualizarPosicion();
        Debug.Log($"?? Posición FPS cambiada: X={posicionX}, Y={posicionY}");
    }
}