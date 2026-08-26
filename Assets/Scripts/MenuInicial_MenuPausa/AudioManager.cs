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

    [Header("UI - Menús")] // MenuPausa.cs, MenuInicial.cs
    public AudioClip sonidoClick;
    public AudioClip sonidoHover;

    [Header("UI - Notas")] // LeerNota.cs
    public AudioClip sonidoAbrirNota;
    public AudioClip sonidoCerrarNota;

    [Header("Enemigo")] // EnemyIA.cs
    public AudioClip[] ambientSounds;
    public AudioClip[] chaseSounds;
    public AudioClip jumpscareSound;

    [Header("Interacciones - Puertas")] // PuertaGeneradores.cs, PuertaFinal.cs, PuertaBloqueada.cs
    public AudioClip sonidoPuertaAbierta;
    public AudioClip[] sonidosPuertaBloqueada; // Para diferentes tags en PuertaBloqueada

    [Header("Interacciones - Objetos")] // RecogerObjeto.cs, RecogerLinterna.cs, RecogerMartillo.cs
    public AudioClip sonidoRecogerObjeto;
    public AudioClip sonidoRecogerLinterna;
    public AudioClip sonidoRecogerMartillo;
    public AudioClip sonidoSoltarObjeto;
    public AudioClip sonidoSoltarLinterna;
    public AudioClip sonidoSoltarMartillo;

    [Header("Interacciones - Puzzles")] // PanelColores.cs, PuertaFinal.cs, CajaFuerte.cs, Generador.cs, MaquinaExpendedora.cs, TablaMadera.cs
    public AudioClip sonidoPulsarBoton;              // PanelColores.cs
    public AudioClip sonidoCombinacionCorrecta;       // PanelColores.cs
    public AudioClip sonidoCombinacionIncorrecta;     // PanelColores.cs
    public AudioClip sonidoCandadoAbierto;            // PuertaFinal.cs
    public AudioClip sonidoLlaveIncorrecta;           // PuertaFinal.cs
    public AudioClip sonidoCajaAcierto;               // CajaFuerte.cs
    public AudioClip sonidoCajaError;                 // CajaFuerte.cs
    public AudioClip sonidoCajaBoton;                 // CajaFuerte.cs
    public AudioClip sonidoActivacionGenerador;       // Generador.cs
    public AudioClip sonidoInsertarMoneda;            // MaquinaExpendedora.cs
    public AudioClip sonidoCaerLlave;                 // MaquinaExpendedora.cs
    public AudioClip sonidoRomperTabla;               // TablaMadera.cs

    [Header("Interacciones - Taquillas")] // LockerHideSystem.cs
    public AudioClip sonidoEnterLocker;
    public AudioClip sonidoExitLocker;

    [Header("Jugador - Movimiento")] // FirstPersonController.cs
    public AudioClip[] footstepSounds;
    public AudioClip[] sprintFootstepSounds;
    public AudioClip breathingClip;
    public AudioClip staminaDepletedSound;
    public AudioClip staminaRecoveredSound;
    public AudioClip crouchSound;
    public AudioClip standSound;

    [Header("Jugador - Zoom")] // FirstPersonController.cs
    public AudioClip zoomInSound;
    public AudioClip zoomOutSound;

    [Header("Jugador - Linterna")] // Linterna.cs
    public AudioClip sonidoEncenderLinterna;
    public AudioClip sonidoApagarLinterna;

    [Header("Efectos")] // VHSGlitchManager.cs
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

    // ============================================
    // BUSCAR AUDIOSOURCES PERIÓDICAMENTE
    // ============================================

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

    // ============================================
    // REGISTRAR UN AUDIOSOURCE
    // ============================================

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

        Debug.Log($"?? AudioSource registrado: {source.gameObject.name}");
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

    // ============================================
    // APLICAR VOLUMEN
    // ============================================

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
    // CAMBIAR VOLUMEN GLOBAL
    // ============================================

    public void SetVolumenGlobal(float value)
    {
        volumenGlobal = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VOLUMEN_GLOBAL_KEY, volumenGlobal);
        PlayerPrefs.Save();
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

    // ============================================
    // MÉTODOS DE AYUDA PARA REPRODUCIR SONIDOS COMUNES
    // ============================================

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

    // ============================================
    // RESETEAR Y SILENCIAR
    // ============================================

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