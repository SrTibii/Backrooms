using UnityEngine;
using UnityEngine.SceneManagement;

public class FosoCollider : MonoBehaviour
{
    [Header("Configuración")]
    public float delayReinicio = 1f;

    [Header("Tags")]
    public string tagJugador = "Player";

    [Header("Audio")]
    public AudioClip sonidoMuerte;
    [Range(0f, 1f)] public float volumenSonido = 0.8f;

    [Header("Efectos (Opcional)")]
    public GameObject efectoMuerte;

    private AudioSource audioSource;
    private bool jugadorMuerto = false;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.volume = volumenSonido;

        if (!gameObject.CompareTag("Foso"))
        {
            gameObject.tag = "Foso";
            Debug.Log("??? Tag del foso configurado automáticamente a 'Foso'");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (jugadorMuerto) return;

        if (other.CompareTag(tagJugador))
        {
            MatarJugador();
        }
    }

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

        // ?? OBTENER EL PLAYER Y MARCARLO COMO MUERTO
        GameObject player = GameObject.FindGameObjectWithTag(tagJugador);
        if (player != null)
        {
            // ?? Llamar al método del FirstPersonController
            FirstPersonController fps = player.GetComponent<FirstPersonController>();
            if (fps != null)
            {
                fps.MarcarComoMuerto();
                Debug.Log("? Player marcado como muerto en FirstPersonController");
            }

            // Opcional: ocultar el renderer del jugador
            Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                r.enabled = false;
            }
        }

        // Reproducir sonido de muerte
        if (sonidoMuerte != null)
        {
            audioSource.PlayOneShot(sonidoMuerte);
        }

        // Efecto visual (si hay)
        if (efectoMuerte != null)
        {
            Instantiate(efectoMuerte, transform.position, Quaternion.identity);
        }

        // Reiniciar la escena después de un delay
        Invoke(nameof(ReiniciarEscena), delayReinicio);
    }

    private void ReiniciarEscena()
    {
        SceneManager.LoadScene("EndVHS");
    }

    public void ReiniciarManual()
    {
        if (!jugadorMuerto)
        {
            MatarJugador();
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

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