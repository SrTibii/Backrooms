using UnityEngine;
using TMPro;

public class MostrarFPS : MonoBehaviour
{
    [Header("Referencias")]
    public TextMeshProUGUI textoFPS; // Texto donde se mostrarán los FPS

    [Header("Configuración")]
    [Tooltip("Si está activado, muestra los FPS")]
    public bool mostrarFPS = true;

    [Header("Posición en Pantalla")]
    [Tooltip("Posición horizontal (0 = izquierda, 0.5 = centro, 1 = derecha)")]
    [Range(0f, 1f)]
    public float posicionX = 0.02f;

    [Tooltip("Posición vertical (0 = abajo, 0.5 = centro, 1 = arriba)")]
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

    // Variables internas
    private float tiempoActual = 0f;
    private int frames = 0;
    private int fpsActual = 0;
    private RectTransform rectTransform;

    void Start()
    {
        if (textoFPS == null)
        {
            // Si no hay texto asignado, crear uno automáticamente
            CrearTextoFPS();
        }

        rectTransform = textoFPS.GetComponent<RectTransform>();
        ActualizarPosicion();

        // Cargar estado guardado
        mostrarFPS = PlayerPrefs.GetInt("MostrarFPS", 1) == 1;
        textoFPS.gameObject.SetActive(mostrarFPS);

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

        // Obtener el Canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        // Convertir coordenadas de 0-1 a píxeles
        Vector2 tamanioCanvas = canvas.GetComponent<RectTransform>().rect.size;

        float x = posicionX * tamanioCanvas.x;
        float y = posicionY * tamanioCanvas.y;

        // Ajustar anclas para que la posición sea relativa
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.pivot = new Vector2(0, 1); // Pivot arriba-izquierda

        rectTransform.anchoredPosition = new Vector2(x, y);

        // Aplicar tamaño de fuente
        textoFPS.fontSize = tamañoFuente;
        textoFPS.color = colorFPS;
    }

    void CrearTextoFPS()
    {
        // Crear un GameObject para el texto
        GameObject go = new GameObject("FPS_Text");
        go.transform.SetParent(transform);

        // Añadir RectTransform
        rectTransform = go.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 50);

        // Añadir TextMeshProUGUI
        textoFPS = go.AddComponent<TextMeshProUGUI>();
        textoFPS.fontSize = tamañoFuente;
        textoFPS.color = colorFPS;
        textoFPS.alignment = TextAlignmentOptions.Left;
        textoFPS.text = "FPS: 0";

        // Añadir CanvasRenderer (se añade automáticamente)
    }

    // ============================================
    // MÉTODOS PÚBLICOS PARA TOGGLE
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

    // ============================================
    // MÉTODOS PARA CAMBIAR POSICIÓN DESDE CÓDIGO
    // ============================================

    public void SetPosicion(float x, float y)
    {
        posicionX = Mathf.Clamp01(x);
        posicionY = Mathf.Clamp01(y);
        ActualizarPosicion();
        Debug.Log($"?? Posición FPS cambiada: X={posicionX}, Y={posicionY}");
    }
}