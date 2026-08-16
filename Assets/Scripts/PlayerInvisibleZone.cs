using UnityEngine;
using System.Collections;

public class PlayerInvisibleZone : MonoBehaviour
{
    [Header("Referencias")]
    public EnemyIA enemyIA;

    [Header("Configuración")]
    [Tooltip("Tiempo que tarda en volverse invisible después de entrar en la zona")]
    public float fadeToInvisibleDelay = 1.5f;

    [Tooltip("Tiempo que tarda en volverse visible después de salir de la zona")]
    public float fadeToVisibleDelay = 1.0f;

    [Tooltip("Si está activado, el jugador se vuelve invisible inmediatamente al entrar")]
    public bool instantInvisibleOnEnter = false;

    public bool debugLogs = true;

    // Corrutinas para controlar los delays
    private Coroutine invisibleCoroutine;
    private Coroutine visibleCoroutine;

    // Estado actual del jugador en la zona
    private bool isPlayerInZone = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = true;

            // Cancelar cualquier corrutina pendiente
            if (visibleCoroutine != null)
            {
                StopCoroutine(visibleCoroutine);
                visibleCoroutine = null;
            }

            // Si ya hay una corrutina de invisibilidad, no hacer nada
            if (invisibleCoroutine != null) return;

            // Iniciar el delay para volverse invisible
            invisibleCoroutine = StartCoroutine(DelayInvisible());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;

            // Cancelar cualquier corrutina pendiente
            if (invisibleCoroutine != null)
            {
                StopCoroutine(invisibleCoroutine);
                invisibleCoroutine = null;
            }

            // Si ya hay una corrutina de visibilidad, no hacer nada
            if (visibleCoroutine != null) return;

            // Iniciar el delay para volverse visible
            visibleCoroutine = StartCoroutine(DelayVisible());
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Si el jugador sigue dentro y por alguna razón no está invisible, forzarlo
        if (other.CompareTag("Player") && isPlayerInZone)
        {
            if (enemyIA != null && !enemyIA.IsPlayerHidden() && invisibleCoroutine == null)
            {
                // Si está dentro pero no invisible, iniciar el proceso
                invisibleCoroutine = StartCoroutine(DelayInvisible());
            }
        }
    }

    private IEnumerator DelayInvisible()
    {
        if (debugLogs) Debug.Log($"? Jugador en zona invisible - Esperando {fadeToInvisibleDelay}s para volverse invisible");

        // Si el jugador debe volverse invisible instantáneamente
        if (instantInvisibleOnEnter)
        {
            if (enemyIA != null)
            {
                enemyIA.SetPlayerHidden(true);
                if (debugLogs) Debug.Log("? Jugador INVISIBLE instantáneo");
            }
            invisibleCoroutine = null;
            yield break;
        }

        // Esperar el delay configurado
        float elapsed = 0f;
        while (elapsed < fadeToInvisibleDelay)
        {
            // Si el jugador salió de la zona durante la espera, cancelar
            if (!isPlayerInZone)
            {
                if (debugLogs) Debug.Log("? Cancelando invisibilidad - Jugador salió de la zona");
                invisibleCoroutine = null;
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Aplicar invisibilidad
        if (enemyIA != null && isPlayerInZone)
        {
            enemyIA.SetPlayerHidden(true);
            if (debugLogs) Debug.Log($"? Jugador INVISIBLE después de {fadeToInvisibleDelay}s");
        }

        invisibleCoroutine = null;
    }

    private IEnumerator DelayVisible()
    {
        if (debugLogs) Debug.Log($"? Jugador salió de zona invisible - Esperando {fadeToVisibleDelay}s para volverse visible");

        // Esperar el delay configurado
        float elapsed = 0f;
        while (elapsed < fadeToVisibleDelay)
        {
            // Si el jugador volvió a entrar durante la espera, cancelar
            if (isPlayerInZone)
            {
                if (debugLogs) Debug.Log("? Cancelando visibilidad - Jugador volvió a entrar en la zona");
                visibleCoroutine = null;
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Aplicar visibilidad
        if (enemyIA != null && !isPlayerInZone)
        {
            enemyIA.SetPlayerHidden(false);
            if (debugLogs) Debug.Log($"? Jugador VISIBLE después de {fadeToVisibleDelay}s");
        }

        visibleCoroutine = null;
    }

    // Método para forzar la invisibilidad (útil para debugging)
    public void ForceInvisible()
    {
        if (enemyIA != null)
        {
            enemyIA.SetPlayerHidden(true);
            if (debugLogs) Debug.Log("?? Forzando invisibilidad");
        }
    }

    // Método para forzar la visibilidad (útil para debugging)
    public void ForceVisible()
    {
        if (enemyIA != null)
        {
            enemyIA.SetPlayerHidden(false);
            if (debugLogs) Debug.Log("?? Forzando visibilidad");
        }
    }

    // Método para cancelar cualquier delay pendiente
    public void CancelAllDelays()
    {
        if (invisibleCoroutine != null)
        {
            StopCoroutine(invisibleCoroutine);
            invisibleCoroutine = null;
        }
        if (visibleCoroutine != null)
        {
            StopCoroutine(visibleCoroutine);
            visibleCoroutine = null;
        }
        if (debugLogs) Debug.Log("?? Todos los delays cancelados");
    }
}