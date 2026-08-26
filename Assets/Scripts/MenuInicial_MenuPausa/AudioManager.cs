using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Volumen Global")]
    [Range(0f, 1f)]
    public float volumenGlobal = 0.8f;

    // ============================================
    // AUDIOCLIPS DEL JUEGO
    // ============================================

    [Header("UI - Menús")]
    public AudioClip sonidoClick;
    public AudioClip sonidoHover;

    [Header("UI - Notas")]
    public AudioClip sonidoAbrirNota;
    public AudioClip sonidoCerrarNota;

    [Header("Enemigo")]
    public AudioClip[] ambientSounds;
    public AudioClip[] chaseSounds;
    public AudioClip jumpscareSound;

    [Header("Interacciones - Puertas")]
    public AudioClip sonidoPuertaAbierta;
    public AudioClip[] sonidosPuertaBloqueada;

    [Header("Interacciones - Objetos")]
    public AudioClip sonidoRecogerObjeto;
    public AudioClip sonidoRecogerLinterna;
    public AudioClip sonidoRecogerMartillo;
    public AudioClip sonidoSoltarObjeto;
    public AudioClip sonidoSoltarLinterna;
    public AudioClip sonidoSoltarMartillo;

    [Header("Interacciones - Puzzles")]
    public AudioClip sonidoPulsarBoton;
    public AudioClip sonidoCombinacionCorrecta;
    public AudioClip sonidoCombinacionIncorrecta;
    public AudioClip sonidoCandadoAbierto;
    public AudioClip sonidoLlaveIncorrecta;
    public AudioClip sonidoCajaAcierto;
    public AudioClip sonidoCajaError;
    public AudioClip sonidoCajaBoton;
    public AudioClip sonidoActivacionGenerador;
    public AudioClip sonidoInsertarMoneda;
    public AudioClip sonidoCaerLlave;
    public AudioClip sonidoRomperTabla;

    [Header("Interacciones - Taquillas")]
    public AudioClip sonidoEnterLocker;
    public AudioClip sonidoExitLocker;

    [Header("Jugador - Movimiento")]
    public AudioClip[] footstepSounds;
    public AudioClip[] sprintFootstepSounds;
    public AudioClip breathingClip;
    public AudioClip staminaDepletedSound;
    public AudioClip staminaRecoveredSound;
    public AudioClip crouchSound;
    public AudioClip standSound;

    [Header("Jugador - Zoom")]
    public AudioClip zoomInSound;
    public AudioClip zoomOutSound;

    [Header("Jugador - Linterna")]
    public AudioClip sonidoEncenderLinterna;
    public AudioClip sonidoApagarLinterna;

    [Header("Efectos")]
    public AudioClip glitchSound;

    // ============================================
    // LISTA DE AUDIOSOURCES CONTROLADOS
    // ============================================
    private List<AudioSource> audioSourcesControlados = new List<AudioSource>();
    private List<float> volumenesOriginales = new List<float>();
    private List<bool> estabaSonando = new List<bool>();

    private const string VOLUMEN_GLOBAL_KEY = "VolumenGlobal";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("? AudioManager creado y persistente");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        volumenGlobal = PlayerPrefs.GetFloat(VOLUMEN_GLOBAL_KEY, 0.8f);
        StartCoroutine(BuscarAudioSourcesPeriodicamente());
    }

    IEnumerator BuscarAudioSourcesPeriodicamente()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>(true);

            foreach (var src in allAudioSources)
            {
                if (src != null && !audioSourcesControlados.Contains(src))
                {
                    if (src != GetComponent<AudioSource>())
                    {
                        RegistrarAudioSource(src);
                    }
                }
            }
        }
    }

    public void RegistrarAudioSource(AudioSource source)
    {
        if (source == null) return;
        if (audioSourcesControlados.Contains(source)) return;
        if (source == GetComponent<AudioSource>()) return;

        float volOriginal = source.volume;

        audioSourcesControlados.Add(source);
        volumenesOriginales.Add(volOriginal);
        estabaSonando.Add(source.isPlaying);

        AplicarVolumenASource(source, audioSourcesControlados.Count - 1);

        Debug.Log($"?? AudioSource registrado: {source.gameObject.name} (clip: {source.clip?.name ?? "NULL"})");
    }

    public void DesregistrarAudioSource(AudioSource source)
    {
        if (source == null) return;

        int index = audioSourcesControlados.IndexOf(source);
        if (index != -1)
        {
            audioSourcesControlados.RemoveAt(index);
            volumenesOriginales.RemoveAt(index);
            estabaSonando.RemoveAt(index);
        }
    }

    private void AplicarVolumenASource(AudioSource source, int index)
    {
        if (source == null || index >= volumenesOriginales.Count) return;

        float volOriginal = volumenesOriginales[index];
        float volumenFinal = volOriginal * volumenGlobal;

        if (volumenFinal <= 0.001f)
        {
            if (source.isPlaying)
            {
                if (index < estabaSonando.Count) estabaSonando[index] = true;
                source.Stop();
            }
            source.volume = 0f;
        }
        else
        {
            if (index < estabaSonando.Count && estabaSonando[index] && !source.isPlaying)
            {
                source.Play();
            }
            source.volume = Mathf.Clamp01(volumenFinal);
        }
    }

    private void AplicarVolumenATodos()
    {
        for (int i = audioSourcesControlados.Count - 1; i >= 0; i--)
        {
            if (audioSourcesControlados[i] == null)
            {
                audioSourcesControlados.RemoveAt(i);
                volumenesOriginales.RemoveAt(i);
                estabaSonando.RemoveAt(i);
                continue;
            }
            AplicarVolumenASource(audioSourcesControlados[i], i);
        }
    }

    // ============================================
    // ?? FORZAR MUTEO POR NOMBRE DE CLIP
    // ============================================
    public void ForzarMuteoPorNombreClip(bool mute)
    {
        Debug.Log($"?? FORZANDO MUTEO POR NOMBRE DE CLIP: {mute}");

        AudioSource[] allSources = FindObjectsOfType<AudioSource>(true);
        int contador = 0;

        // Nombres de los clips problemáticos
        string[] nombresProblematicos = { "Breath", "BacteriaIdleWalk", "BacteriaChase1", "BacteriaChase2" };

        foreach (var src in allSources)
        {
            if (src == null || src == GetComponent<AudioSource>()) continue;

            if (src.clip != null)
            {
                string clipName = src.clip.name;
                bool esProblematico = false;

                foreach (string nombre in nombresProblematicos)
                {
                    if (clipName.Contains(nombre))
                    {
                        esProblematico = true;
                        break;
                    }
                }

                if (esProblematico)
                {
                    if (mute)
                    {
                        src.volume = 0f;
                        if (src.isPlaying) src.Stop();
                        contador++;
                        Debug.Log($"?? MUTEADO: {src.gameObject.name} | Clip: {src.clip.name}");
                    }
                    else
                    {
                        src.volume = 0.8f;
                        if (!src.isPlaying && src.clip != null) src.Play();
                        contador++;
                        Debug.Log($"?? REACTIVADO: {src.gameObject.name} | Clip: {src.clip.name}");
                    }
                }
            }
        }

        Debug.Log($"?? {contador} AudioSources procesados por nombre de clip");
    }

    public void SetVolumenGlobal(float value)
    {
        volumenGlobal = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VOLUMEN_GLOBAL_KEY, volumenGlobal);
        PlayerPrefs.Save();

        // ?? FORZAR MUTEO POR NOMBRE DE CLIP
        ForzarMuteoPorNombreClip(volumenGlobal <= 0.001f);

        AplicarVolumenATodos();
        Debug.Log($"?? Volumen Global cambiado a: {volumenGlobal}");
    }

    public float GetVolumenGlobal()
    {
        return volumenGlobal;
    }

    // ============================================
    // MÉTODOS PARA REPRODUCIR SONIDOS
    // ============================================

    public void PlayOneShot(AudioSource source, AudioClip clip, float volume = 1f)
    {
        if (source == null || clip == null) return;
        if (volumenGlobal <= 0.001f) return;

        if (!audioSourcesControlados.Contains(source))
        {
            RegistrarAudioSource(source);
        }

        int index = audioSourcesControlados.IndexOf(source);
        if (index != -1)
        {
            volumenesOriginales[index] = volume;
            AplicarVolumenASource(source, index);
        }

        source.PlayOneShot(clip);
    }

    public void PlayOneShotAtPosition(AudioClip clip, Vector3 position, float volume = 1f, float maxDistance = 10f)
    {
        if (clip == null) return;
        if (volumenGlobal <= 0.001f) return;

        GameObject tempGO = new GameObject($"TempAudio_{clip.name}");
        tempGO.transform.position = position;

        AudioSource src = tempGO.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = 0.5f;
        src.maxDistance = maxDistance;

        audioSourcesControlados.Add(src);
        volumenesOriginales.Add(volume);
        estabaSonando.Add(true);

        AplicarVolumenASource(src, audioSourcesControlados.Count - 1);

        src.Play();
        Destroy(tempGO, clip.length + 0.2f);
    }

    public void PlayClick(AudioSource source, float volume = 1f)
    {
        PlayOneShot(source, sonidoClick, volume);
    }

    public void PlayHover(AudioSource source, float volume = 1f)
    {
        PlayOneShot(source, sonidoHover, volume);
    }

    public void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        PlayOneShotAtPosition(clip, position, volume);
    }

    public void ResetearAudioSources()
    {
        audioSourcesControlados.Clear();
        volumenesOriginales.Clear();
        estabaSonando.Clear();
    }

    public void SilenciarTodo()
    {
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>(true);
        foreach (var src in allAudioSources)
        {
            if (src != null && src != GetComponent<AudioSource>())
            {
                if (src.isPlaying) src.Stop();
                src.volume = 0f;
            }
        }
        audioSourcesControlados.Clear();
        volumenesOriginales.Clear();
        estabaSonando.Clear();
    }
}