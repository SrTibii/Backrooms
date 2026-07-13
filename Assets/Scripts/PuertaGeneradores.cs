using UnityEngine;
using System.Collections.Generic;

public class PuertaGeneradores : MonoBehaviour
{
    [Header("Configuración")]
    public int generadoresNecesarios = 3;

    [Header("Cubos de la puerta")]
    public Renderer[] cubosPuerta; // Array de 3 cubos rojos/verdes (Renderer)
    public Material materialRojo;
    public Material materialVerde;

    [Header("Puerta")]
    public GameObject puerta; // La puerta que se abre
    public string triggerAbrir = "Abrir"; // Nombre del Trigger en el Animator

    [Header("Audio")]
    public AudioClip sonidoPuertaAbierta;
    [Range(0f, 1f)] public float volumenSonido = 0.7f;

    // Estado interno
    private bool[] generadoresActivados = new bool[3];
    private int contadorActivados = 0;
    private bool puertaAbierta = false;
    private AudioSource audioSource;

    void Start()
    {
        // Configurar AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (puerta != null)
        {
            puerta.tag = "PuertaGenerador";
        }

        // Estado inicial: todos los cubos rojos
        ActualizarCubos();
    }

    // ?? Método que llaman los generadores cuando se activan
    public void GeneradorActivado(int id)
    {
        // Convertir ID (1,2,3) a índice (0,1,2)
        int index = id - 1;

        // Si el generador ya estaba activado, ignorar
        if (generadoresActivados[index]) return;

        // Marcar como activado
        generadoresActivados[index] = true;
        contadorActivados++;

        Debug.Log($"?? Generadores activados: {contadorActivados}/{generadoresNecesarios}");

        // ?? Actualizar los cubos de la puerta
        ActualizarCubos();

        // ?? Si todos los generadores están activados, abrir la puerta
        if (contadorActivados >= generadoresNecesarios)
        {
            AbrirPuerta();
        }
    }

    private void ActualizarCubos()
    {
        // Actualizar cada cubo según el estado de su generador
        for (int i = 0; i < cubosPuerta.Length && i < generadoresActivados.Length; i++)
        {
            if (cubosPuerta[i] != null)
            {
                cubosPuerta[i].material = generadoresActivados[i] ? materialVerde : materialRojo;
            }
        }
    }

    private void AbrirPuerta()
    {
        if (puertaAbierta) return;

        puertaAbierta = true;
        Debug.Log("?? ¡PUERTA ABIERTA! Todos los generadores activados.");

        // ?? Reproducir sonido
        if (sonidoPuertaAbierta != null)
        {
            audioSource.volume = volumenSonido;
            audioSource.PlayOneShot(sonidoPuertaAbierta);
        }

        // ?? Activar animación de la puerta
        if (puerta != null)
        {
            Animator anim = puerta.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetTrigger(triggerAbrir);
            }
            else
            {
                // Si no tiene Animator, hacer que desaparezca
                puerta.SetActive(false);
            }
        }
    }

    // ?? Método para comprobar si la puerta está abierta
    public bool IsOpen()
    {
        return puertaAbierta;
    }

    // ?? Método para saber cuántos generadores faltan
    public int GeneradoresFaltantes()
    {
        return generadoresNecesarios - contadorActivados;
    }
}