using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class VHSTextEffect : MonoBehaviour
{
    [Header("Textos")]
    public TextMeshProUGUI[] texts;

    [Header("Raw Image")]
    public RawImage rawImage; // La RawImage que también tendrá el efecto

    [Header("Configuración General")]
    public float minAlpha = 0.5f;
    public float maxAlpha = 1f;
    public float flickerSpeed = 1.5f;
    public float glitchChance = 0.03f;
    public float staticChance = 0.01f;
    public bool randomOffset = true;

    [Header("Chromatic Aberration")]
    public float chromaticDisplacement = 2.5f;
    public float chromaticSpeed = 1.2f;
    public bool enableChromatic = true;
    public float rgbIntensity = 0.5f;

    // ============================================
    // RAW IMAGE - EFECTOS DE GLITCH
    // ============================================
    [Header("Raw Image - Efectos")]
    public float rawImageGlitchChance = 0.02f;
    public float rawImageGlitchDuration = 0.1f;
    public float rawImageGlitchIntensity = 5f;

    private float[] offsets;
    private float[] currentAlphas;
    private bool[] isGlitching;

    // Estado de la RawImage
    private Color rawImageOriginalColor;
    private bool isRawImageGlitching = false;
    private Vector2 rawImageOriginalPosition;

    private Dictionary<TextMeshProUGUI, TextMeshProUGUI[]> chromaticTexts = new Dictionary<TextMeshProUGUI, TextMeshProUGUI[]>();

    void Start()
    {
        // Guardar estado original de la RawImage
        if (rawImage != null)
        {
            rawImageOriginalColor = rawImage.color;
            rawImageOriginalPosition = rawImage.rectTransform.anchoredPosition;
        }

        InitializeEffects();
    }

    void InitializeEffects()
    {
        if (texts.Length > 0)
        {
            offsets = new float[texts.Length];
            currentAlphas = new float[texts.Length];
            isGlitching = new bool[texts.Length];

            for (int i = 0; i < texts.Length; i++)
            {
                offsets[i] = randomOffset ? Random.Range(0f, 10f) : 0f;
                currentAlphas[i] = maxAlpha;
                isGlitching[i] = false;

                if (enableChromatic && texts[i] != null)
                {
                    CreateChromaticTexts(texts[i]);
                }
            }
        }
    }

    void CreateChromaticTexts(TextMeshProUGUI original)
    {
        if (chromaticTexts.ContainsKey(original))
        {
            foreach (var copy in chromaticTexts[original])
            {
                if (copy != null)
                    DestroyImmediate(copy.gameObject);
            }
            chromaticTexts.Remove(original);
        }

        if (original == null) return;

        TextMeshProUGUI[] copies = new TextMeshProUGUI[3];
        Color[] colors = { Color.red, Color.green, Color.blue };

        for (int i = 0; i < 3; i++)
        {
            GameObject copyObj = Instantiate(original.gameObject, original.transform.parent);
            TextMeshProUGUI copy = copyObj.GetComponent<TextMeshProUGUI>();

            copy.text = original.text;
            Color c = colors[i];
            c.a = rgbIntensity * 0.6f;
            copy.color = c;
            copy.outlineWidth = 0.1f;
            copy.transform.SetSiblingIndex(original.transform.GetSiblingIndex() - 1);

            copies[i] = copy;
        }

        chromaticTexts[original] = copies;

        Color origColor = original.color;
        origColor.a = 1f;
        original.color = origColor;
    }

    void Update()
    {
        // ============================================
        // PROCESAR TEXTOS
        // ============================================
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null)
            {
                if (isGlitching[i])
                    continue;

                float alpha = Mathf.Lerp(minAlpha, maxAlpha,
                    Mathf.PingPong((Time.time + offsets[i]) * flickerSpeed, 1));

                if (Random.value < glitchChance)
                {
                    alpha = 0f;
                    StartCoroutine(GlitchEffect(i));
                }

                if (Random.value < staticChance)
                {
                    alpha = Random.Range(0f, 1f);
                }

                Color color = texts[i].color;
                color.a = alpha;
                texts[i].color = color;

                if (enableChromatic && chromaticTexts.ContainsKey(texts[i]))
                {
                    UpdateChromaticEffect(texts[i], alpha);
                }
            }
        }

        // ============================================
        // PROCESAR RAW IMAGE
        // ============================================
        if (rawImage != null && !isRawImageGlitching)
        {
            // Flicker de alpha en la RawImage
            float rawAlpha = Mathf.Lerp(minAlpha, maxAlpha,
                Mathf.PingPong((Time.time + 5f) * flickerSpeed * 0.7f, 1));

            if (Random.value < staticChance * 0.5f)
            {
                rawAlpha = Random.Range(0.2f, 1f);
            }

            Color rawColor = rawImage.color;
            rawColor.a = rawAlpha;
            rawImage.color = rawColor;

            // Glitch de la RawImage
            if (Random.value < rawImageGlitchChance)
            {
                StartCoroutine(RawImageGlitchEffect());
            }
        }
    }

    void UpdateChromaticEffect(TextMeshProUGUI original, float alpha)
    {
        if (!chromaticTexts.ContainsKey(original)) return;

        TextMeshProUGUI[] copies = chromaticTexts[original];
        Vector2 originalPos = original.rectTransform.anchoredPosition;

        float offsetX = Mathf.Sin(Time.time * chromaticSpeed) * chromaticDisplacement;
        float offsetY = Mathf.Cos(Time.time * chromaticSpeed * 0.7f + 1) * chromaticDisplacement * 0.3f;

        copies[0].rectTransform.anchoredPosition = originalPos + new Vector2(offsetX, offsetY);
        copies[0].color = new Color(1f, 0f, 0f, alpha * rgbIntensity * 0.5f);

        copies[1].rectTransform.anchoredPosition = originalPos + new Vector2(-offsetX * 0.5f, -offsetY * 0.5f);
        copies[1].color = new Color(0f, 1f, 0f, alpha * rgbIntensity * 0.5f);

        copies[2].rectTransform.anchoredPosition = originalPos + new Vector2(offsetX * 0.3f, offsetY * 0.7f);
        copies[2].color = new Color(0f, 0f, 1f, alpha * rgbIntensity * 0.5f);

        if (Random.value < glitchChance * 2)
        {
            float glitchOffset = Random.Range(-8f, 8f);
            copies[0].rectTransform.anchoredPosition += new Vector2(glitchOffset, Random.Range(-3f, 3f));
            copies[1].rectTransform.anchoredPosition += new Vector2(-glitchOffset * 0.7f, Random.Range(-3f, 3f));
            copies[2].rectTransform.anchoredPosition += new Vector2(glitchOffset * 0.5f, Random.Range(-3f, 3f));
        }
    }

    // ============================================
    // CORRUTINA DE GLITCH PARA TEXTOS
    // ============================================

    IEnumerator GlitchEffect(int index)
    {
        isGlitching[index] = true;

        for (int i = 0; i < Random.Range(2, 5); i++)
        {
            Color color = texts[index].color;
            color.a = 0f;
            texts[index].color = color;

            if (enableChromatic && chromaticTexts.ContainsKey(texts[index]))
            {
                foreach (var copy in chromaticTexts[texts[index]])
                {
                    Color c = copy.color;
                    c.a = 0f;
                    copy.color = c;
                }
            }

            yield return new WaitForSeconds(Random.Range(0.02f, 0.08f));

            color.a = 1f;
            texts[index].color = color;

            if (enableChromatic && chromaticTexts.ContainsKey(texts[index]))
            {
                foreach (var copy in chromaticTexts[texts[index]])
                {
                    Color c = copy.color;
                    c.a = rgbIntensity * 0.5f;
                    copy.color = c;
                }
            }

            yield return new WaitForSeconds(Random.Range(0.02f, 0.08f));
        }

        Color finalColor = texts[index].color;
        finalColor.a = maxAlpha;
        texts[index].color = finalColor;

        if (enableChromatic && chromaticTexts.ContainsKey(texts[index]))
        {
            foreach (var copy in chromaticTexts[texts[index]])
            {
                Color c = copy.color;
                c.a = maxAlpha * rgbIntensity * 0.5f;
                copy.color = c;
            }
        }

        isGlitching[index] = false;
    }

    // ============================================
    // CORRUTINA DE GLITCH PARA RAW IMAGE
    // ============================================

    IEnumerator RawImageGlitchEffect()
    {
        isRawImageGlitching = true;

        float duration = rawImageGlitchDuration;
        float elapsed = 0f;

        // Guardar posición original
        Vector2 originalPos = rawImageOriginalPosition;
        Color originalColor = rawImageOriginalColor;

        while (elapsed < duration)
        {
            float intensity = rawImageGlitchIntensity;
            float progress = elapsed / duration;

            // Desplazamiento aleatorio
            float offsetX = Random.Range(-intensity, intensity);
            float offsetY = Random.Range(-intensity * 0.5f, intensity * 0.5f);
            rawImage.rectTransform.anchoredPosition = originalPos + new Vector2(offsetX, offsetY);

            // Cambiar alpha aleatorio
            Color color = rawImage.color;
            color.a = Random.Range(0f, 0.5f);
            rawImage.color = color;

            // Cambiar escala para efecto de estiramiento
            float scaleX = 1f + Random.Range(-0.1f, 0.1f);
            float scaleY = 1f + Random.Range(-0.1f, 0.1f);
            rawImage.rectTransform.localScale = new Vector3(scaleX, scaleY, 1f);

            elapsed += Time.deltaTime * 2f;
            yield return new WaitForSeconds(Random.Range(0.01f, 0.04f));
        }

        // Restaurar estado original
        rawImage.rectTransform.anchoredPosition = rawImageOriginalPosition;
        rawImage.rectTransform.localScale = Vector3.one;
        rawImage.color = rawImageOriginalColor;

        isRawImageGlitching = false;
    }

    // ============================================
    // MÉTODOS PÚBLICOS
    // ============================================

    public void OnTextChanged()
    {
        foreach (var kvp in chromaticTexts)
        {
            TextMeshProUGUI original = kvp.Key;
            TextMeshProUGUI[] copies = kvp.Value;

            if (original != null && copies != null)
            {
                string currentText = original.text;
                foreach (var copy in copies)
                {
                    if (copy != null)
                    {
                        copy.text = currentText;
                    }
                }
            }
        }
    }

    public void SetRawImage(RawImage newRawImage)
    {
        rawImage = newRawImage;
        if (rawImage != null)
        {
            rawImageOriginalColor = rawImage.color;
            rawImageOriginalPosition = rawImage.rectTransform.anchoredPosition;
        }
    }

    // ============================================
    // LIMPIEZA
    // ============================================

    void OnDestroy()
    {
        foreach (var kvp in chromaticTexts)
        {
            foreach (var copy in kvp.Value)
            {
                if (copy != null)
                    Destroy(copy.gameObject);
            }
        }
        chromaticTexts.Clear();
    }
}