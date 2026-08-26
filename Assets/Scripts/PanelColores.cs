using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
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

    [Header("Cubos de Retroalimentación")]
    [Tooltip("Cubos que se pintarán con los colores pulsados")]
    public GameObject[] cubosRetroalimentacion;

    // ============================================
    // DELAY AL FALLAR
    // ============================================
    [Header("Delay al Fallar")]
    [Tooltip("Tiempo que espera antes de reiniciar el panel al fallar")]
    public float delayAlFallar = 1.5f;

    [Header("Volumen")]
    [Range(0f, 1f)] public float volumenSonidos = 0.7f;

    private List<string> pulsaciones = new List<string>();
    private bool puzzleCompletado = false;
    private bool esperandoReinicio = false;
    private InteractionSystem interactionSystem;

    private Dictionary<string, Color> coloresMap = new Dictionary<string, Color>();
    private Color[] coloresOriginalesCubos;

    void Start()
    {
        InicializarDiccionarioColores();

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

        if (puerta != null)
        {
            puerta.tag = "PuertaColoresTV";
        }

        GuardarColoresOriginales();

        Debug.Log("? Panel de colores inicializado");
    }

    private void GuardarColoresOriginales()
    {
        coloresOriginalesCubos = new Color[cubosRetroalimentacion.Length];

        for (int i = 0; i < cubosRetroalimentacion.Length; i++)
        {
            if (cubosRetroalimentacion[i] != null)
            {
                Renderer renderer = cubosRetroalimentacion[i].GetComponent<Renderer>();
                if (renderer != null)
                {
                    coloresOriginalesCubos[i] = renderer.material.color;
                }
                else
                {
                    coloresOriginalesCubos[i] = Color.black;
                }
            }
            else
            {
                coloresOriginalesCubos[i] = Color.black;
            }
        }
    }

    private void InicializarDiccionarioColores()
    {
        coloresMap.Clear();
        coloresMap.Add("Rojo", Color.red);
        coloresMap.Add("Azul", Color.blue);
        coloresMap.Add("Verde", Color.green);
        coloresMap.Add("Amarillo", Color.yellow);
        coloresMap.Add("Naranja", new Color(1f, 0.5f, 0f));
        coloresMap.Add("Rosa", new Color(1f, 0.41f, 0.71f));
        coloresMap.Add("Morado", new Color(0.5f, 0f, 0.5f));
        coloresMap.Add("Cian", Color.cyan);
        coloresMap.Add("Blanco", Color.white);
        coloresMap.Add("Negro", Color.black);
        coloresMap.Add("Gris", Color.gray);
        coloresMap.Add("Marron", new Color(0.5f, 0.25f, 0f));
    }

    private void SetCuboColor(GameObject cubo, Color color)
    {
        if (cubo == null) return;

        Renderer renderer = cubo.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    private void RestaurarCubosOriginales()
    {
        for (int i = 0; i < cubosRetroalimentacion.Length; i++)
        {
            if (cubosRetroalimentacion[i] != null && i < coloresOriginalesCubos.Length)
            {
                SetCuboColor(cubosRetroalimentacion[i], coloresOriginalesCubos[i]);
            }
        }
        Debug.Log("?? Cubos restaurados a su color original");
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
        // Si el puzzle está completado o estamos esperando reinicio, ignorar
        if (puzzleCompletado || esperandoReinicio) return;

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
        if (botonScript != null && !string.IsNullOrEmpty(botonScript.color))
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

        int index = pulsaciones.Count - 1;
        if (index < cubosRetroalimentacion.Length && cubosRetroalimentacion[index] != null)
        {
            Color unityColor = Color.black;
            if (coloresMap.TryGetValue(color, out unityColor))
            {
                SetCuboColor(cubosRetroalimentacion[index], unityColor);
                Debug.Log($"?? Cubo {index + 1} pintado de {color}");
            }
        }

        // ============================================
        // REPRODUCIR SONIDO DE PULSAR BOTÓN
        // ============================================
        if (AudioManager.Instance != null && AudioManager.Instance.sonidoPulsarBoton != null)
        {
            AudioManager.Instance.PlayOneShotAtPosition(
                AudioManager.Instance.sonidoPulsarBoton,
                transform.position,
                volumenSonidos,
                8f
            );
        }
        else
        {
            Debug.LogWarning("?? AudioManager o sonidoPulsarBoton no disponible");
        }

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

            // ============================================
            // REPRODUCIR SONIDO DE COMBINACIÓN CORRECTA
            // ============================================
            if (AudioManager.Instance != null && AudioManager.Instance.sonidoCombinacionCorrecta != null)
            {
                AudioManager.Instance.PlayOneShotAtPosition(
                    AudioManager.Instance.sonidoCombinacionCorrecta,
                    transform.position,
                    volumenSonidos,
                    15f
                );
            }
            else
            {
                Debug.LogWarning("?? AudioManager o sonidoCombinacionCorrecta no disponible");
            }

            Debug.Log("?? ¡COMBINACIÓN CORRECTA!");
            AbrirPuerta();
        }
        else
        {
            // ============================================
            // REPRODUCIR SONIDO DE COMBINACIÓN INCORRECTA
            // ============================================
            if (AudioManager.Instance != null && AudioManager.Instance.sonidoCombinacionIncorrecta != null)
            {
                AudioManager.Instance.PlayOneShotAtPosition(
                    AudioManager.Instance.sonidoCombinacionIncorrecta,
                    transform.position,
                    volumenSonidos,
                    10f
                );
            }
            else
            {
                Debug.LogWarning("?? AudioManager o sonidoCombinacionIncorrecta no disponible");
            }

            Debug.Log($"? Combinación incorrecta. Esperando {delayAlFallar}s antes de reiniciar...");

            // INICIAR DELAY ANTES DE REINICIAR
            StartCoroutine(ReiniciarConDelay());
        }
    }

    // ============================================
    // CORRUTINA CON DELAY PARA REINICIAR
    // ============================================
    private IEnumerator ReiniciarConDelay()
    {
        esperandoReinicio = true;

        // Esperar el tiempo configurado
        yield return new WaitForSeconds(delayAlFallar);

        // Ahora sí reiniciamos
        pulsaciones.Clear();
        RestaurarCubosOriginales();
        esperandoReinicio = false;

        Debug.Log("?? Panel reiniciado. Vuelve a intentarlo.");
    }

    private void AbrirPuerta()
    {
        if (puerta == null)
        {
            Debug.LogWarning("?? No hay puerta asignada en el PanelColores");
            return;
        }

        puerta.tag = "Usado";
        Debug.Log("??? Tag de la puerta cambiado a 'Usado'");

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

    // ============================================
    // MÉTODOS PÚBLICOS
    // ============================================

    public void ResetearPuzzle()
    {
        // Cancelar cualquier delay pendiente
        StopAllCoroutines();
        esperandoReinicio = false;

        puzzleCompletado = false;
        pulsaciones.Clear();
        RestaurarCubosOriginales();
        Debug.Log("?? Puzzle reseteado manualmente");
    }

    public bool EstaCompletado()
    {
        return puzzleCompletado;
    }

    public bool IsOpen()
    {
        return puzzleCompletado;
    }

    public void MostrarCombinacionCorrecta()
    {
        string combinacion = "";
        foreach (string color in combinacionCorrecta)
        {
            combinacion += color + " ";
        }
        Debug.Log($"?? Combinación correcta: {combinacion}");
    }
}