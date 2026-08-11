using UnityEngine;
using System.Collections.Generic;

public class PuertaGeneradores : MonoBehaviour
{
    [Header("Configuración")]
    public int generadoresNecesarios = 3;

    [Header("Cubos de la puerta")]
    public Renderer[] cubosPuerta;
    public Material materialRojo;
    public Material materialVerde;

    [Header("Puerta")]
    public GameObject puerta;
    public string triggerAbrir = "Abrir";

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
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (puerta != null)
        {
            puerta.tag = "PuertaGenerador";
        }

        ActualizarCubos();
    }

    public void GeneradorActivado(int id)
    {
        int index = id - 1;

        if (generadoresActivados[index]) return;

        generadoresActivados[index] = true;
        contadorActivados++;

        Debug.Log($"? Generadores activados: {contadorActivados}/{generadoresNecesarios}");

        ActualizarCubos();

        if (contadorActivados >= generadoresNecesarios)
        {
            AbrirPuerta();
        }
    }

    private void ActualizarCubos()
    {
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

        // ?? CAMBIAR EL TAG PARA QUE NO SE MUESTRE EL MENSAJE
        if (puerta != null)
        {
            puerta.tag = "Usado";
            Debug.Log("??? Tag de la puerta cambiado a 'Usado'");
        }

        if (sonidoPuertaAbierta != null)
        {
            audioSource.volume = volumenSonido;
            audioSource.PlayOneShot(sonidoPuertaAbierta);
        }

        if (puerta != null)
        {
            Animator anim = puerta.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetTrigger(triggerAbrir);
            }
            else
            {
                puerta.SetActive(false);
            }
        }
    }

    public bool IsOpen()
    {
        return puertaAbierta;
    }

    public int GeneradoresFaltantes()
    {
        return generadoresNecesarios - contadorActivados;
    }
}