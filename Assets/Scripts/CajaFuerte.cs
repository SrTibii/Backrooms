using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class CajaFuerte : MonoBehaviour
{
    [Header("Configuración")]
    public string[] combinacionCorrecta = new string[4]; // Ej: {"1", "2", "3", "4"}
    public int maxDigitos = 4;

    [Header("Referencias UI")]
    public TextMeshProUGUI[] digitosUI; // Array de 4 TextMeshPro (las X)

    [Header("Volumen")]
    [Range(0f, 1f)] public float volumenSonidos = 0.7f;

    [Header("Puerta de la caja")]
    public GameObject puertaCaja; // La puerta de la caja fuerte (animacion)

    // Input System
    public InputActionReference interactAction;

    // Estado interno
    private string[] digitosIngresados = new string[4];
    private int pasoActual = 0;
    private bool isOpen = false;

    void Start()
    {
        // Reiniciar la UI
        ReiniciarUI();

        Debug.Log("? CajaFuerte inicializada");
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
        if (isOpen) return;

        GameObject target = GetTargetObject();
        if (target == null) return;

        // Intentar obtener el componente BotonNumero
        BotonNumero boton = target.GetComponent<BotonNumero>();
        if (boton != null)
        {
            if (pasoActual >= maxDigitos) return;
            AgregarNumero(boton.GetNumero());
        }
    }

    private GameObject GetTargetObject()
    {
        // Buscar el InteractionSystem en la escena
        InteractionSystem interactionSystem = FindObjectOfType<InteractionSystem>();
        if (interactionSystem != null)
        {
            return interactionSystem.GetTargetObject();
        }

        // Si no hay InteractionSystem, usar Raycast directo
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

    private void AgregarNumero(string numero)
    {
        digitosIngresados[pasoActual] = numero;

        if (digitosUI != null && pasoActual < digitosUI.Length)
        {
            digitosUI[pasoActual].text = numero;
        }

        // ============================================
        // REPRODUCIR SONIDO DE BOTÓN CON AUDIOMANAGER
        // ============================================
        if (AudioManager.Instance != null && AudioManager.Instance.sonidoCajaBoton != null)
        {
            AudioManager.Instance.PlayOneShotAtPosition(
                AudioManager.Instance.sonidoCajaBoton,
                transform.position,
                volumenSonidos,
                10f
            );
        }
        else
        {
            Debug.LogWarning("?? AudioManager o sonidoCajaBoton no disponible");
        }

        pasoActual++;

        if (pasoActual >= maxDigitos)
        {
            VerificarCombinacion();
        }
    }

    private void VerificarCombinacion()
    {
        bool esCorrecta = true;

        for (int i = 0; i < maxDigitos; i++)
        {
            if (digitosIngresados[i] != combinacionCorrecta[i])
            {
                esCorrecta = false;
                break;
            }
        }

        if (esCorrecta)
        {
            // CIERTO - ABRIR CAJA
            isOpen = true;

            // ============================================
            // REPRODUCIR SONIDO DE ACIERTO CON AUDIOMANAGER
            // ============================================
            if (AudioManager.Instance != null && AudioManager.Instance.sonidoCajaAcierto != null)
            {
                AudioManager.Instance.PlayOneShotAtPosition(
                    AudioManager.Instance.sonidoCajaAcierto,
                    transform.position,
                    volumenSonidos,
                    15f
                );
            }
            else
            {
                Debug.LogWarning("?? AudioManager o sonidoCajaAcierto no disponible");
            }

            Debug.Log("?? ¡CAJA FUERTE ABIERTA!");

            // Animación de la puerta
            if (puertaCaja != null)
            {
                Animator anim = puertaCaja.GetComponent<Animator>();
                if (anim != null)
                    anim.SetTrigger("Abrir");
            }

            // Cambiar color de los dígitos a verde
            foreach (TextMeshProUGUI digit in digitosUI)
            {
                if (digit != null)
                    digit.color = Color.green;
            }
        }
        else
        {
            // ERROR - Reiniciar
            // ============================================
            // REPRODUCIR SONIDO DE ERROR CON AUDIOMANAGER
            // ============================================
            if (AudioManager.Instance != null && AudioManager.Instance.sonidoCajaError != null)
            {
                AudioManager.Instance.PlayOneShotAtPosition(
                    AudioManager.Instance.sonidoCajaError,
                    transform.position,
                    volumenSonidos,
                    10f
                );
            }
            else
            {
                Debug.LogWarning("?? AudioManager o sonidoCajaError no disponible");
            }

            Debug.Log("? Combinación incorrecta. Reiniciando...");
            ReiniciarUI();
        }
    }

    private void ReiniciarUI()
    {
        pasoActual = 0;

        for (int i = 0; i < digitosIngresados.Length; i++)
        {
            digitosIngresados[i] = "";
        }

        if (digitosUI != null)
        {
            foreach (TextMeshProUGUI digit in digitosUI)
            {
                if (digit != null)
                {
                    digit.text = "X";
                    digit.color = Color.white;
                }
            }
        }
    }

    public void ReiniciarCaja()
    {
        if (!isOpen)
        {
            ReiniciarUI();
        }
    }

    public bool EstaAbierta()
    {
        return isOpen;
    }
}