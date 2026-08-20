using UnityEngine;
using System.Collections.Generic;

public class ControlSombras : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Si está activado, las luces tienen sombras")]
    public bool sombrasActivadas = true;

    [Header("Luces")]
    [Tooltip("Si está vacío, busca todas las luces automáticamente")]
    public Light[] lucesPersonalizadas; // Opcional: si quieres controlar luces específicas

    [Tooltip("Si es true, busca todas las luces automáticamente")]
    public bool buscarLucesAutomaticamente = true;

    [Header("Debug")]
    public bool mostrarLogs = true;

    // Lista interna de luces controladas
    private List<Light> lucesControladas = new List<Light>();

    // Guardar el tipo de sombra original de cada luz
    private Dictionary<Light, LightShadows> sombrasOriginales = new Dictionary<Light, LightShadows>();

    // Tipo de sombra cuando están desactivadas
    private LightShadows sombraDesactivada = LightShadows.None;

    void Start()
    {
        // Buscar o asignar luces
        if (buscarLucesAutomaticamente)
        {
            BuscarTodasLasLuces();
        }
        else if (lucesPersonalizadas != null && lucesPersonalizadas.Length > 0)
        {
            foreach (var luz in lucesPersonalizadas)
            {
                if (luz != null && !lucesControladas.Contains(luz))
                {
                    lucesControladas.Add(luz);
                }
            }
        }

        // Guardar el estado original de cada luz
        foreach (var luz in lucesControladas)
        {
            if (!sombrasOriginales.ContainsKey(luz))
            {
                sombrasOriginales[luz] = luz.shadows;
            }
        }

        // Cargar estado guardado
        sombrasActivadas = PlayerPrefs.GetInt("SombrasActivadas", 1) == 1;
        AplicarSombras(sombrasActivadas);

        if (mostrarLogs)
        {
            Debug.Log($"?? ControlSombras inicializado. {lucesControladas.Count} luces controladas. Sombras: {(sombrasActivadas ? "ACTIVADAS" : "DESACTIVADAS")}");
        }
    }

    // ============================================
    // BUSCAR TODAS LAS LUCES AUTOMÁTICAMENTE
    // ============================================

    public void BuscarTodasLasLuces()
    {
        lucesControladas.Clear();
        sombrasOriginales.Clear();

        Light[] todasLasLuces = FindObjectsOfType<Light>(true);

        foreach (var luz in todasLasLuces)
        {
            if (luz != null && luz.type != LightType.Directional)
            {
                // Solo controlar luces que pueden tener sombras (no direccionales)
                lucesControladas.Add(luz);
                sombrasOriginales[luz] = luz.shadows;
            }
        }

        if (mostrarLogs)
        {
            Debug.Log($"?? Encontradas {lucesControladas.Count} luces no direccionales");
        }
    }

    // ============================================
    // APLICAR SOMBRAS
    // ============================================

    public void SetSombras(bool activar)
    {
        sombrasActivadas = activar;
        PlayerPrefs.SetInt("SombrasActivadas", activar ? 1 : 0);
        PlayerPrefs.Save();
        AplicarSombras(activar);

        if (mostrarLogs)
        {
            Debug.Log($"?? Sombras {(activar ? "ACTIVADAS" : "DESACTIVADAS")}");
        }
    }

    private void AplicarSombras(bool activar)
    {
        foreach (var luz in lucesControladas)
        {
            if (luz == null) continue;

            if (activar)
            {
                // Restaurar sombras originales
                if (sombrasOriginales.ContainsKey(luz))
                {
                    luz.shadows = sombrasOriginales[luz];
                }
            }
            else
            {
                // Desactivar sombras
                luz.shadows = LightShadows.None;
            }
        }
    }

    // ============================================
    // MÉTODOS PÚBLICOS
    // ============================================

    public bool EstaActivado()
    {
        return sombrasActivadas;
    }

    public void AlternarSombras()
    {
        SetSombras(!sombrasActivadas);
    }

    public void RestaurarPorDefecto()
    {
        SetSombras(true);
    }

    // ============================================
    // LIMPIEZA (OPCIONAL)
    // ============================================

    private void OnDestroy()
    {
        // Restaurar sombras originales al destruir el script
        foreach (var kvp in sombrasOriginales)
        {
            if (kvp.Key != null)
            {
                kvp.Key.shadows = kvp.Value;
            }
        }
    }
}