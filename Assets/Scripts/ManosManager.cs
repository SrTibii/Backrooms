using UnityEngine;

public class ManosManager : MonoBehaviour
{
    public static ManosManager Instance; // Singleton para acceder desde cualquier script

    [Header("Referencias")]
    public Transform manoIzquierda; // Para objetos normales
    public Transform manoDerecha;   // Para la linterna

    [Header("Estado")]
    public bool manoIzquierdaOcupada = false;
    public bool manoDerechaOcupada = false;

    public GameObject objetoEnManoIzquierda = null;
    public GameObject objetoEnManoDerecha = null;

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ?? Métodos para la mano izquierda (objetos normales)
    public bool OcuparManoIzquierda(GameObject objeto)
    {
        if (manoIzquierdaOcupada)
        {
            Debug.Log("?? Mano izquierda ya está ocupada");
            return false;
        }

        manoIzquierdaOcupada = true;
        objetoEnManoIzquierda = objeto;
        Debug.Log($"? Mano izquierda ocupada por: {objeto.name}");
        return true;
    }

    public void LiberarManoIzquierda()
    {
        manoIzquierdaOcupada = false;
        objetoEnManoIzquierda = null;
        Debug.Log("? Mano izquierda liberada");
    }

    // ?? Métodos para la mano derecha (linterna)
    public bool OcuparManoDerecha(GameObject objeto)
    {
        if (manoDerechaOcupada)
        {
            Debug.Log("?? Mano derecha ya está ocupada");
            return false;
        }

        manoDerechaOcupada = true;
        objetoEnManoDerecha = objeto;
        Debug.Log($"? Mano derecha ocupada por: {objeto.name}");
        return true;
    }

    public void LiberarManoDerecha()
    {
        manoDerechaOcupada = false;
        objetoEnManoDerecha = null;
        Debug.Log("? Mano derecha liberada");
    }

    // ?? Método para saber si alguna mano está ocupada
    public bool AlgunaManoOcupada()
    {
        return manoIzquierdaOcupada || manoDerechaOcupada;
    }

    // ?? Método para saber qué mano está ocupada
    public string GetManoOcupada()
    {
        if (manoIzquierdaOcupada) return "Izquierda";
        if (manoDerechaOcupada) return "Derecha";
        return "Ninguna";
    }
}