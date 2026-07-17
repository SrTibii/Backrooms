using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class LeerNota : MonoBehaviour
{
    [Header("Configuración")]
    public float interactionDistance = 3f;
    public string notaTag = "NotaPapel";

    [Header("UI")]
    public GameObject panelNota;
    public TextMeshProUGUI textoNotaUI;
    public TextMeshProUGUI tituloNotaUI;

    [Header("Input")]
    public InputActionReference interactAction;

    [Header("Audio")]
    public AudioClip sonidoAbrirNota;
    public AudioClip sonidoCerrarNota;
    [Range(0f, 1f)] public float volumenSonido = 0.7f;

    [Header("Manos")]
    public GameObject holdPositionObject;
    public GameObject holdPositionLinterna;
    public GameObject holdPositionMartillo;

    // Referencias
    private FirstPersonController playerController;
    private InteractionSystem interactionSystem;
    private RecogerObjeto recogerObjeto;
    private RecogerLinterna recogerLinterna;
    private RecogerMartillo recogerMartillo;
    private VHSGlitchManager vhsGlitchManager;
    private VHSCameraEffects vhsCameraEffects;

    private bool notaAbierta = false;
    private GameObject notaActual = null;
    private AudioSource audioSource;

    void Start()
    {
        playerController = FindObjectOfType<FirstPersonController>();
        interactionSystem = FindObjectOfType<InteractionSystem>();
        recogerObjeto = FindObjectOfType<RecogerObjeto>();
        recogerLinterna = FindObjectOfType<RecogerLinterna>();
        recogerMartillo = FindObjectOfType<RecogerMartillo>();
        vhsGlitchManager = FindObjectOfType<VHSGlitchManager>();
        vhsCameraEffects = Camera.main.GetComponent<VHSCameraEffects>();
        if (vhsCameraEffects == null)
        {
            vhsCameraEffects = FindObjectOfType<VHSCameraEffects>();
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = volumenSonido;

        if (panelNota != null)
        {
            panelNota.SetActive(false);
        }

        Debug.Log("?? Sistema de lectura de notas inicializado");
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
        if (notaAbierta)
        {
            CerrarNota();
            return;
        }

        AbrirNota();
    }

    private void AbrirNota()
    {
        GameObject target = GetTargetObject();
        if (target == null) return;

        if (!target.CompareTag(notaTag)) return;

        float distance = Vector3.Distance(Camera.main.transform.position, target.transform.position);
        if (distance > interactionDistance) return;

        notaActual = target;

        NotaPapel notaPapel = target.GetComponent<NotaPapel>();

        if (notaPapel != null)
        {
            if (textoNotaUI != null)
            {
                textoNotaUI.text = notaPapel.texto;
            }
            if (tituloNotaUI != null)
            {
                tituloNotaUI.text = notaPapel.titulo;
            }
        }
        else
        {
            if (textoNotaUI != null)
            {
                textoNotaUI.text = "Nota sin texto";
            }
        }

        if (panelNota != null)
        {
            panelNota.SetActive(true);
        }

        if (sonidoAbrirNota != null)
        {
            audioSource.PlayOneShot(sonidoAbrirNota);
        }

        // ?? DESACTIVAR MOVIMIENTO DEL JUGADOR
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // ?? DESACTIVAR INTERACTION SYSTEM (para no poder interactuar con nada)
        if (interactionSystem != null)
        {
            interactionSystem.enabled = false;
        }

        // ?? DESACTIVAR SCRIPTS DE RECOGIDA (para no poder soltar objetos)
        if (recogerObjeto != null)
        {
            recogerObjeto.enabled = false;
        }
        if (recogerLinterna != null)
        {
            recogerLinterna.enabled = false;
        }
        if (recogerMartillo != null)
        {
            recogerMartillo.enabled = false;
        }

        // ?? DESACTIVAR EFECTOS DE CÁMARA VHS
        if (vhsCameraEffects != null)
        {
            vhsCameraEffects.enabled = false;
        }

        // ?? PAUSAR GLITCHES
        if (vhsGlitchManager != null)
        {
            vhsGlitchManager.PausarGlitches();
        }

        // ?? OCULTAR MANOS
        if (holdPositionObject != null)
        {
            holdPositionObject.SetActive(false);
        }
        if (holdPositionLinterna != null)
        {
            holdPositionLinterna.SetActive(false);
        }
        if (holdPositionMartillo != null)
        {
            holdPositionMartillo.SetActive(false);
        }

        notaAbierta = true;
        Debug.Log($"?? Nota abierta: {target.name}");
    }

    private void CerrarNota()
    {
        if (panelNota != null)
        {
            panelNota.SetActive(false);
        }

        if (textoNotaUI != null)
        {
            textoNotaUI.text = "";
        }

        if (sonidoCerrarNota != null)
        {
            audioSource.PlayOneShot(sonidoCerrarNota);
        }

        // ?? REACTIVAR MOVIMIENTO DEL JUGADOR
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        // ?? REACTIVAR INTERACTION SYSTEM
        if (interactionSystem != null)
        {
            interactionSystem.enabled = true;
        }

        // ?? REACTIVAR SCRIPTS DE RECOGIDA
        if (recogerObjeto != null)
        {
            recogerObjeto.enabled = true;
        }
        if (recogerLinterna != null)
        {
            recogerLinterna.enabled = true;
        }
        if (recogerMartillo != null)
        {
            recogerMartillo.enabled = true;
        }

        // ?? REACTIVAR EFECTOS DE CÁMARA VHS
        if (vhsCameraEffects != null)
        {
            vhsCameraEffects.enabled = true;
            vhsCameraEffects.ResetEffects();
        }

        // ?? REANUDAR GLITCHES
        if (vhsGlitchManager != null)
        {
            vhsGlitchManager.ReanudarGlitches();
        }

        // ?? MOSTRAR MANOS
        if (holdPositionObject != null)
        {
            holdPositionObject.SetActive(true);
        }
        if (holdPositionLinterna != null)
        {
            holdPositionLinterna.SetActive(true);
        }
        if (holdPositionMartillo != null)
        {
            holdPositionMartillo.SetActive(true);
        }

        notaAbierta = false;
        notaActual = null;
        Debug.Log("?? Nota cerrada");
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

        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            return hit.collider.gameObject;
        }

        return null;
    }

    public bool EstaLeyendo()
    {
        return notaAbierta;
    }

    public void CerrarNotaForzado()
    {
        if (notaAbierta)
        {
            CerrarNota();
        }
    }
}