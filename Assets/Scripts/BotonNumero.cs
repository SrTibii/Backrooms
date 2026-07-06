using UnityEngine;
using TMPro;

public class BotonNumero : MonoBehaviour
{
    [Header("Configuración")]
    public string numero; // "0", "1", "2", "3", "4", "5", "6", "7", "8", "9"

    [Header("Referencias")]
    public TextMeshProUGUI textMesh; // El texto que muestra el número en el botón

    void Start()
    {
        // Si no se asignó el TextMesh, buscarlo automáticamente
        if (textMesh == null)
        {
            textMesh = GetComponentInChildren<TextMeshProUGUI>();
        }

        // Actualizar el texto del botón
        if (textMesh != null && string.IsNullOrEmpty(textMesh.text))
        {
            textMesh.text = numero;
        }
    }

    // ?? Método para obtener el número del botón
    public string GetNumero()
    {
        return numero;
    }

    // ?? Método para cambiar el número (opcional)
    public void SetNumero(string nuevoNumero)
    {
        numero = nuevoNumero;
        if (textMesh != null)
        {
            textMesh.text = numero;
        }
    }
}