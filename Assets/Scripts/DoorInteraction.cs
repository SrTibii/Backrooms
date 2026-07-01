using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class DoorInteraction : MonoBehaviour
{
    [Header("Referencias")]
    public InteractionSystem interactionSystem;
    public InputActionReference interactAction;
    public Animator doorAnimator;

    [Header("Configuración")]
    public float interactionDistance = 3f;
    public bool startOpen = false;

    [Header("Tags para interacción")]
    public string doorTag = "Puerta";

    [Header("Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;
    [Range(0f, 1f)] public float soundVolume = 0.7f;

    // Estado interno
    private bool isOpen = false;
    private bool isAnimating = false;
    private AudioSource audioSource;

    void Start()
    {
        // Buscar referencias
        if (interactionSystem == null)
            interactionSystem = FindObjectOfType<InteractionSystem>();

        if (doorAnimator == null)
            doorAnimator = GetComponent<Animator>();

        // Configurar AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        // Asegurar tag
        if (!string.IsNullOrEmpty(doorTag))
            gameObject.tag = doorTag;

        // Estado inicial
        isOpen = startOpen;
        if (doorAnimator != null)
        {
            doorAnimator.SetBool("isOpen", isOpen);
            // Forzar el estado inicial sin animación
            doorAnimator.Play(isOpen ? "DoorOpen" : "DoorClosed", 0, 1f);
        }
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
        HandleInteraction();
    }

    void HandleInteraction()
    {
        if (interactionSystem == null) return;

        GameObject target = interactionSystem.GetTargetObject();
        if (target == null) return;

        if (target != gameObject && !target.transform.IsChildOf(transform))
            return;

        float distance = interactionSystem.GetTargetDistance();
        if (distance < 0 || distance > interactionDistance)
            return;

        if (isAnimating) return;

        //Alternar estado
        isOpen = !isOpen;

        if (doorAnimator != null)
        {
            doorAnimator.SetBool("isOpen", isOpen);
        }

        // Reproducir sonido
        PlaySound(isOpen ? openSound : closeSound);

        // Iniciar corutina para evitar spam
        StartCoroutine(AnimationCooldown());
    }

    IEnumerator AnimationCooldown()
    {
        isAnimating = true;

        // Esperar a que termine la animación
        if (doorAnimator != null)
        {
            AnimatorStateInfo stateInfo = doorAnimator.GetCurrentAnimatorStateInfo(0);
            float animationLength = stateInfo.length;
            yield return new WaitForSeconds(animationLength * 0.8f);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        isAnimating = false;
        //Debug.Log($"?? Puerta {(isOpen ? "abierta" : "cerrada")}");
    }

    void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        audioSource.volume = soundVolume;
        audioSource.PlayOneShot(clip);
    }

    // Métodos públicos
    public void ToggleDoor()
    {
        if (isAnimating) return;
        isOpen = !isOpen;
        if (doorAnimator != null)
            doorAnimator.SetBool("isOpen", isOpen);
        PlaySound(isOpen ? openSound : closeSound);
    }

    public bool IsOpen() => isOpen;
    public bool IsAnimating() => isAnimating;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}