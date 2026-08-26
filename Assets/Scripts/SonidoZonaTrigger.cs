using UnityEngine;

public class SonidoZonaTrigger : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public float volumen = 0.8f;

    [Header("Configuración")]
    public bool reproducirUnaVez = false;
    public float tiempoEspera = 0.5f;

    private bool yaReproducido = false;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.loop = true; // O false si quieres que suene una vez
            audioSource.playOnAwake = false;
            audioSource.volume = volumen;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (reproducirUnaVez && yaReproducido) return;

            if (audioSource != null && !audioSource.isPlaying)
            {
                audioSource.Play();
                yaReproducido = true;
                Debug.Log($"?? Sonido activado en {gameObject.name}");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                // ?? En lugar de FadeOut, solo baja el volumen o detén
                audioSource.Stop();
            }
        }
    }

    System.Collections.IEnumerator FadeOut(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        source.Stop();
        source.volume = startVolume;
    }
}