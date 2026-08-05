using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LuzParpadeante : MonoBehaviour
{
    [Header("Luces a controlar")]
    public Light[] luces; // Array de luces que quieres controlar

    [Header("Configuración Global")]
    public bool activarAlInicio = true;
    public float tiempoMinimoApagado = 0.1f;
    public float tiempoMaximoApagado = 5f;
    public float tiempoMinimoEncendido = 0.1f;
    public float tiempoMaximoEncendido = 5f;

    [Header("Probabilidades")]
    [Range(0f, 1f)] public float probabilidadParpadeoRapido = 0.3f;
    [Range(0f, 1f)] public float probabilidadApagadoLargo = 0.3f;
    [Range(0f, 1f)] public float probabilidadEncendidoLargo = 0.3f;

    [Header("Intensidad")]
    public float intensidadMaxima = 1f;
    public float intensidadMinima = 0f;

    // Estado interno
    private List<Coroutine> corrutinas = new List<Coroutine>();
    private bool sistemaActivo = false;

    void Start()
    {
        if (activarAlInicio)
        {
            ActivarSistema();
        }
    }

    void OnDestroy()
    {
        DetenerSistema();
    }

    
    // Activa el sistema de parpadeo para todas las luces
    public void ActivarSistema()
    {
        if (sistemaActivo) return;

        sistemaActivo = true;

        foreach (Light luz in luces)
        {
            if (luz != null)
            {
                // Asegurar que la luz empiece encendida
                luz.enabled = true;
                luz.intensity = intensidadMaxima;

                // Iniciar corrutina para cada luz
                Coroutine corr = StartCoroutine(ComportamientoLuz(luz));
                corrutinas.Add(corr);
            }
        }

        Debug.Log($"?? Sistema de luces activado con {luces.Length} luces");
    }

  
    // Desactiva el sistema de parpadeo para todas las luces
    public void DetenerSistema()
    {
        sistemaActivo = false;

        // Detener todas las corrutinas
        foreach (Coroutine corr in corrutinas)
        {
            if (corr != null)
            {
                StopCoroutine(corr);
            }
        }
        corrutinas.Clear();

        // Apagar todas las luces
        foreach (Light luz in luces)
        {
            if (luz != null)
            {
                luz.enabled = false;
            }
        }

        Debug.Log("?? Sistema de luces desactivado");
    }

    
    // Reinicia el sistema de luces
    public void ReiniciarSistema()
    {
        DetenerSistema();
        ActivarSistema();
    }

    
    // Comportamiento individual para cada luz
    private IEnumerator ComportamientoLuz(Light luz)
    {
        while (sistemaActivo && luz != null)
        {
            // Decidir el comportamiento aleatorio
            float decision = Random.value;

            if (decision < probabilidadParpadeoRapido)
            {
                // PARPADEO RÁPIDO
                yield return StartCoroutine(ParpadeoRapido(luz));
            }
            else if (decision < probabilidadParpadeoRapido + probabilidadApagadoLargo)
            {
                // APAGADO LARGO
                yield return StartCoroutine(ApagadoLargo(luz));
            }
            else if (decision < probabilidadParpadeoRapido + probabilidadApagadoLargo + probabilidadEncendidoLargo)
            {
                // ENCENDIDO LARGO
                yield return StartCoroutine(EncendidoLargo(luz));
            }
            else
            {
                // COMPORTAMIENTO NORMAL (encendido y apagado aleatorio)
                yield return StartCoroutine(ComportamientoNormal(luz));
            }
        }
    }

    
    // Parpadeo rápido (varios parpadeos cortos)
    private IEnumerator ParpadeoRapido(Light luz)
    {
        int parpadeos = Random.Range(2, 6);
        float duracionParpadeo = Random.Range(0.05f, 0.2f);

        for (int i = 0; i < parpadeos; i++)
        {
            if (!sistemaActivo || luz == null) yield break;

            luz.enabled = false;
            yield return new WaitForSeconds(duracionParpadeo);

            if (!sistemaActivo || luz == null) yield break;

            luz.enabled = true;
            luz.intensity = Random.Range(intensidadMinima, intensidadMaxima);
            yield return new WaitForSeconds(duracionParpadeo * Random.Range(0.5f, 1.5f));
        }
    }

  
    // Apagado largo (se apaga y espera varios segundos)
    private IEnumerator ApagadoLargo(Light luz)
    {
        float tiempoApagado = Random.Range(2f, 5f);

        luz.enabled = false;
        yield return new WaitForSeconds(tiempoApagado);

        if (sistemaActivo && luz != null)
        {
            luz.enabled = true;
            luz.intensity = Random.Range(intensidadMinima, intensidadMaxima);
        }
    }


    // Encendido largo (se queda encendida varios segundos)
    private IEnumerator EncendidoLargo(Light luz)
    {
        float tiempoEncendido = Random.Range(2f, 5f);

        luz.enabled = true;
        luz.intensity = Random.Range(intensidadMinima, intensidadMaxima);
        yield return new WaitForSeconds(tiempoEncendido);
    }

   
    // Comportamiento normal (encendido/apagado aleatorio)
    private IEnumerator ComportamientoNormal(Light luz)
    {
        float tiempoEncendido = Random.Range(tiempoMinimoEncendido, tiempoMaximoEncendido);
        float tiempoApagado = Random.Range(tiempoMinimoApagado, tiempoMaximoApagado);

        // Estar encendida
        luz.enabled = true;
        luz.intensity = Random.Range(intensidadMinima, intensidadMaxima);
        yield return new WaitForSeconds(tiempoEncendido);

        if (!sistemaActivo || luz == null) yield break;

        // Estar apagada
        luz.enabled = false;
        yield return new WaitForSeconds(tiempoApagado);
    }


    // Método para añadir una luz al sistema en tiempo de ejecución
    public void AgregarLuz(Light nuevaLuz)
    {
        if (nuevaLuz == null) return;

        // Añadir a la lista
        List<Light> listaLuces = new List<Light>(luces);
        listaLuces.Add(nuevaLuz);
        luces = listaLuces.ToArray();

        // Si el sistema está activo, iniciar corrutina para la nueva luz
        if (sistemaActivo)
        {
            Coroutine corr = StartCoroutine(ComportamientoLuz(nuevaLuz));
            corrutinas.Add(corr);
        }

        Debug.Log($"?? Luz añadida: {nuevaLuz.name}");
    }


    // Método para eliminar una luz del sistema
    public void EliminarLuz(Light luzAEliminar)
    {
        if (luzAEliminar == null) return;

        List<Light> listaLuces = new List<Light>(luces);
        listaLuces.Remove(luzAEliminar);
        luces = listaLuces.ToArray();

        // Apagar la luz eliminada
        luzAEliminar.enabled = false;

        Debug.Log($"?? Luz eliminada: {luzAEliminar.name}");
    }


    // Activa/Desactiva todas las luces instantáneamente (para eventos)
    public void SetLuces(bool estado)
    {
        foreach (Light luz in luces)
        {
            if (luz != null)
            {
                luz.enabled = estado;
                if (estado)
                {
                    luz.intensity = intensidadMaxima;
                }
            }
        }
    }


    //Forzar que todas las luces se sincronicen (opcional)
    public void SincronizarLuces()
    {
        foreach (Light luz in luces)
        {
            if (luz != null)
            {
                luz.enabled = true;
                luz.intensity = intensidadMaxima;
            }
        }
    }
}