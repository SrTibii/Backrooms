using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class CreditosManager : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject panelCreditos;
    public ScrollRect scrollRect;
    public TextMeshProUGUI textoCreditos;

    [Header("Contenido de Créditos")]
    [TextArea(5, 20)]
    public string creadoPor = "Created by : tibii";

    [TextArea(5, 20)]
    public string musicaSFX = "Music / SFX:\nhttps://pixabay.com";

    [TextArea(5, 20)]
    public string animaciones = "Animations:\nhttps://www.mixamo.com/";

    [TextArea(5, 20)]
    public string assets = @"Assets:
- ""Simple Safe"" (https://skfb.ly/F8wG) by avhatar is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).
- ""Bacteria backrooms"" (https://skfb.ly/oXTGY) by purple guy is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).
";

    [Header("Formato")]
    public string separador = "\n\n========================================\n\n";
    public string tituloFormato = "<b><size=36>{0}</size></b>\n";
    public float paddingContent = 80f;

    [Header("Botones")]
    public Button botonAbrirCreditos;
    public Button botonCerrarCreditos;

    void Start()
    {
        // ?? CONFIGURAR EL CONTENT AL INICIO
        ConfigurarContent();

        // ?? ACTUALIZAR TEXTO
        if (textoCreditos != null)
        {
            textoCreditos.text = ConstruirCreditos();
        }

        // ?? FORZAR TAMAÑO
        Invoke("ForzarTamanoContent", 0.1f);

        // Configurar botones
        if (botonAbrirCreditos != null)
            botonAbrirCreditos.onClick.AddListener(AbrirCreditos);

        if (botonCerrarCreditos != null)
            botonCerrarCreditos.onClick.AddListener(CerrarCreditos);

        // Panel cerrado al inicio
        if (panelCreditos != null)
            panelCreditos.SetActive(false);
    }

    // ============================================
    // ?? CONFIGURAR EL CONTENT
    // ============================================

    private void ConfigurarContent()
    {
        if (scrollRect == null || scrollRect.content == null) return;

        RectTransform content = scrollRect.content;

        // ?? ANCLAS: TOP STRETCH
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = new Vector2(0, 0);

        Debug.Log("? Content configurado correctamente");
    }

    // ============================================
    // CONSTRUIR EL TEXTO DE CRÉDITOS
    // ============================================

    private string ConstruirCreditos()
    {
        List<string> secciones = new List<string>();

        if (!string.IsNullOrEmpty(creadoPor))
            secciones.Add(string.Format(tituloFormato, "CREATED BY") + creadoPor);

        if (!string.IsNullOrEmpty(musicaSFX))
            secciones.Add(string.Format(tituloFormato, "MUSIC / SFX") + musicaSFX);

        if (!string.IsNullOrEmpty(animaciones))
            secciones.Add(string.Format(tituloFormato, "ANIMATIONS") + animaciones);

        if (!string.IsNullOrEmpty(assets))
            secciones.Add(string.Format(tituloFormato, "3D ASSETS") + assets);

        return string.Join(separador, secciones);
    }

    // ============================================
    // ?? FORZAR EL TAMAÑO DEL CONTENT
    // ============================================

    public void ForzarTamanoContent()
    {
        if (textoCreditos == null || scrollRect == null || scrollRect.content == null) return;

        RectTransform content = scrollRect.content;

        // ?? ACTUALIZAR EL TEXTO
        textoCreditos.ForceMeshUpdate();

        // ?? CALCULAR ALTURA DEL TEXTO
        float alturaTexto = textoCreditos.preferredHeight;

        // ?? OBTENER ANCHO DEL VIEWPORT
        float anchoViewport = scrollRect.viewport.rect.width;

        // ?? AJUSTAR ANCHO DEL CONTENT
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, anchoViewport);

        // ?? AJUSTAR ALTURA DEL CONTENT
        float alturaTotal = alturaTexto + paddingContent;
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, alturaTotal);

        // ?? POSICIONAR EN ARRIBA
        content.anchoredPosition = new Vector2(0, 0);

        // ?? SCROLL AL PRINCIPIO
        scrollRect.verticalNormalizedPosition = 1f;

        Debug.Log($"?? Content: Ancho={anchoViewport}, Altura={alturaTotal}, Texto={alturaTexto}");
    }

    // ============================================
    // MÉTODOS PÚBLICOS
    // ============================================

    public void AbrirCreditos()
    {
        if (panelCreditos != null)
        {
            panelCreditos.SetActive(true);
            Invoke("ForzarTamanoContent", 0.05f);
            scrollRect.verticalNormalizedPosition = 1f;
            Debug.Log("?? Créditos abiertos");
        }
    }

    public void CerrarCreditos()
    {
        if (panelCreditos != null)
        {
            panelCreditos.SetActive(false);
            Debug.Log("?? Créditos cerrados");
        }
    }

    public void ActualizarCreditos()
    {
        if (textoCreditos != null)
        {
            textoCreditos.text = ConstruirCreditos();
            Invoke("ForzarTamanoContent", 0.1f);
        }
    }
}