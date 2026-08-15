using UnityEngine;
using UnityEngine.SceneManagement;

public class FosoCollider : MonoBehaviour
{
    [Header("Configuración")]
    public float delayReinicio = 1f; // Tiempo antes de reiniciar la escena

    [Header("Tags")]
    public string tagJugador = "Player";

    [Header("Audio")]
    public AudioClip sonidoMuerte;
    [Range(0f, 1f)] public float volumenSonido = 0.8f;

    [Header("Efectos (Opcional)")]
    public GameObject efectoMuerte; // Prefab de partículas o efecto visual

    private AudioSource audioSource;
    private bool jugadorMuerto = false;

    void Start()
    {
        // Configurar AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.volume = volumenSonido;

        // Asegurar que el foso tiene el tag correcto
        if (!gameObject.CompareTag("Foso"))
        {
            gameObject.tag = "Foso";
            Debug.Log("??? Tag del foso configurado automáticamente a 'Foso'");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Si el jugador ya murió, no hacer nada
        if (jugadorMuerto) return;

        // Verificar si el objeto que entra es el jugador
        if (other.CompareTag(tagJugador))
        {
            // ?? EL JUGADOR HA CAÍDO AL FOSO
            MatarJugador();
        }
    }

    // ?? También funciona con TriggerStay (por si el jugador no activa el TriggerEnter)
    private void OnTriggerStay(Collider other)
    {
        if (jugadorMuerto) return;

        if (other.CompareTag(tagJugador))
        {
            MatarJugador();
        }
    }

    private void MatarJugador()
    {
        if (jugadorMuerto) return;

        jugadorMuerto = true;
        Debug.Log("?? El jugador ha caído al foso. Reiniciando...");

        // ?? Reproducir sonido de muerte
        if (sonidoMuerte != null)
        {
            audioSource.PlayOneShot(sonidoMuerte);
        }

        // ?? Efecto visual (si hay)
        if (efectoMuerte != null)
        {
            Instantiate(efectoMuerte, transform.position, Quaternion.identity);
        }

        // ?? Desactivar el jugador visualmente (opcional)
        GameObject player = GameObject.FindGameObjectWithTag(tagJugador);
        if (player != null)
        {
            // Opción 1: Desactivar el jugador (no se moverá)
            // player.SetActive(false);

            // Opción 2: Ocultar el renderer del jugador
            Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                r.enabled = false;
            }

            // Opción 3: Desactivar el CharacterController
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }
        }

        // ?? Reiniciar la escena después de un delay
        Invoke(nameof(ReiniciarEscena), delayReinicio);
    }

    private void ReiniciarEscena()
    {
        Debug.Log("?? Reiniciando escena...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ?? Método para reiniciar manualmente (si se necesita desde otro script)
    public void ReiniciarManual()
    {
        if (!jugadorMuerto)
        {
            MatarJugador();
        }
    }

    // ?? Para debug: dibujar el área del foso en la escena
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        // Si tiene collider, dibujar su volumen
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}