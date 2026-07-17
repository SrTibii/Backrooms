using UnityEngine;

public class NotaPapel : MonoBehaviour
{
    [Header("Texto de la nota")]
    [TextArea(5, 15)]
    public string texto = "Escribe aquí el texto de la nota...";

    [Header("Título (opcional)")]
    public string titulo = "";

    void Start()
    {
        // Asegurar que tiene el tag correcto
        if (!gameObject.CompareTag("NotaPapel"))
        {
            gameObject.tag = "NotaPapel";
        }
    }
}