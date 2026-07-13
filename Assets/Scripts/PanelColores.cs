using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PanelColores : MonoBehaviour
{
    [Header("Configuración")]
    public string[] combinacionCorrecta = new string[4];
    public int maxPulsaciones = 4;

    [Header("Referencias")]
    public GameObject[] botonesColores;
    public InputActionReference interactAction;

    [Header("Puerta")]
    public GameObject puerta;
    public string triggerAbrir = "Abrir";

    [Header("Audio")]
    public AudioClip sonidoPulsarBoton;
    public AudioClip sonidoCombinacionCorrecta;
    public AudioClip sonidoCombinacionIncorrecta;
    [Range(0f, 1f)] public float volumenSonidos = 0.7f;

    private List<string> pulsaciones = new List<string>();
    private bool puzzleCompletado = false;
    private AudioSource audioSource;
    private InteractionSystem interactionSystem;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        interactionSystem = FindObjectOfType<InteractionSystem>();
        if (interactionSystem == null)
        {
            Debug.LogError("? No se encontró InteractionSystem en la escena");
        }

        foreach (GameObject boton in botonesColores)
        {
            if (boton != null)
            {
                boton.tag = "BotonColor";
            }
        }

        // ?? Asignar tag a la puerta
        if (puerta != null)
        {
            puerta.tag = "PuertaColoresTV";
        }

        Debug.Log("?? Panel de colores inicializado");
    }

    private void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed += OnInteractPerformed;
            interactAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed -= OnInteractPerformed;
            interactAction.action.Disable();
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (puzzleCompletado) return;

        GameObject target = GetTargetObject();
        if (target == null) return;

        if (target.CompareTag("BotonColor"))
        {
            string color = ObtenerColorDelBoton(target);
            if (!string.IsNullOrEmpty(color))
            {
                PulsarBoton(color);
            }
        }
    }

    private GameObject GetTargetObject()
    {
        if (interactionSystem != null)
        {
            return interactionSystem.GetTargetObject();
        }

        Camera cam = Camera.main;
        if (cam == null) return null;

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 5f))
        {
            return hit.collider.gameObject;
        }

        return null;
    }

    private string ObtenerColorDelBoton(GameObject boton)
    {
        if (boton.name.Contains("Rojo")) return "Rojo";
        if (boton.name.Contains("Azul")) return "Azul";
        if (boton.name.Contains("Verde")) return "Verde";
        if (boton.name.Contains("Amarillo")) return "Amarillo";
        if (boton.name.Contains("Naranja")) return "Naranja";
        if (boton.name.Contains("Rosa")) return "Rosa";
        if (boton.name.Contains("Morado")) return "Morado";
        if (boton.name.Contains("Cian")) return "Cian";
        if (boton.name.Contains("Blanco")) return "Blanco";

        BotonColorColor botonScript = boton.GetComponent<BotonColorColor>();
        if (botonScript != null)
        {
            return botonScript.color;
        }

        return "";
    }

    private void PulsarBoton(string color)
    {
        if (pulsaciones.Count >= maxPulsaciones) return;

        pulsaciones.Add(color);
        Debug.Log($"?? Botón pulsado: {color} ({pulsaciones.Count}/{maxPulsaciones})");

        ReproducirSonido(sonidoPulsarBoton);

        if (pulsaciones.Count >= maxPulsaciones)
        {
            VerificarCombinacion();
        }
    }

    private void VerificarCombinacion()
    {
        bool esCorrecta = true;

        for (int i = 0; i < maxPulsaciones; i++)
        {
            if (pulsaciones[i] != combinacionCorrecta[i])
            {
                esCorrecta = false;
                break;
            }
        }

        if (esCorrecta)
        {
            puzzleCompletado = true;
            ReproducirSonido(sonidoCombinacionCorrecta);
            Debug.Log("? ? ? ¡COMBINACIÓN CORRECTA!");
            AbrirPuerta();
        }
        else
        {
            ReproducirSonido(sonidoCombinacionIncorrecta);
            Debug.Log("? Combinación incorrecta. Reiniciando...");
            ReiniciarPanel();
        }
    }

    private void AbrirPuerta()
    {
        if (puerta == null)
        {
            Debug.LogWarning("?? No hay puerta asignada en el PanelColores");
            return;
        }

        Animator anim = puerta.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetTrigger(triggerAbrir);
            Debug.Log($"?? Puerta abierta con trigger: {triggerAbrir}");
        }
        else
        {
            puerta.SetActive(false);
        }
    }

    private void ReiniciarPanel()
    {
        pulsaciones.Clear();
        Debug.Log("?? Panel reiniciado. Vuelve a intentarlo.");
    }

    private void ReproducirSonido(AudioClip clip)
    {
        if (clip == null) return;
        audioSource.volume = volumenSonidos;
        audioSource.PlayOneShot(clip);
    }

    public void ResetearPuzzle()
    {
        puzzleCompletado = false;
        pulsaciones.Clear();
        Debug.Log("?? Puzzle reseteado");
    }

    public bool EstaCompletado()
    {
        return puzzleCompletado;
    }

    // ?? NUEVO: Método para saber si la puerta está abierta (para PuertaBloqueada)
    public bool IsOpen()
    {
        return puzzleCompletado;
    }
}