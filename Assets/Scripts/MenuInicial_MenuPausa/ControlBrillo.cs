using UnityEngine;
using UnityEngine.UI;

public class ControlBrillo : MonoBehaviour
{
    [Header("Referencias")]
    public RawImage rawImageBrillo; // RawImage negra que cubre toda la pantalla

    [Header("Valores")]
    [Range(0f, 1f)]
    public float brilloActual = 0f; // 0 = máximo brillo, 1 = mínimo brillo

    [Header("Valores por Defecto")]
    public float brilloPorDefecto = 0f;

    private const string BRILLO_KEY = "Brillo";

    void Start()
    {
        // Cargar el brillo guardado
        brilloActual = PlayerPrefs.GetFloat(BRILLO_KEY, brilloPorDefecto);
        AplicarBrillo(brilloActual);

        Debug.Log($"?? Brillo cargado: {brilloActual}");
    }

    public void SetBrillo(float valor)
    {
        brilloActual = Mathf.Clamp01(valor);
        PlayerPrefs.SetFloat(BRILLO_KEY, brilloActual);
        PlayerPrefs.Save();
        AplicarBrillo(brilloActual);
        Debug.Log($"?? Brillo cambiado a: {brilloActual}");
    }

    public float GetBrillo()
    {
        return brilloActual;
    }

    private void AplicarBrillo(float valor)
    {
        if (rawImageBrillo != null)
        {
            Color color = rawImageBrillo.color;
            color.a = valor; // 0 = transparente (máximo brillo), 1 = opaco (mínimo brillo)
            rawImageBrillo.color = color;
        }
    }

    public void RestaurarPorDefecto()
    {
        SetBrillo(brilloPorDefecto);
    }
}