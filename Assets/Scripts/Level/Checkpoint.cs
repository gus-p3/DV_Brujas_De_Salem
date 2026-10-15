using System.Collections;
using UnityEngine;

/// <summary>
/// Punto de control en el mundo (Farol rústico). Al ser alcanzado por la bruja,
/// se enciende con una animación de fuego oscilante, registra la posición segura en
/// CheckpointManager, reproduce un sonido y muestra el aviso "Punto de control".
/// </summary>
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class Checkpoint : MonoBehaviour
{
    [Header("Sprites de Animación")]
    [Tooltip("Sprite mientras el punto de control está inactivo/apagado.")]
    [SerializeField] private Sprite unlitSprite;

    [Tooltip("Cuadros de animación cuando la llama está encendida (en bucle).")]
    [SerializeField] private Sprite[] litFrames;

    [Tooltip("Velocidad de la animación de la llama en cuadros por segundo.")]
    [SerializeField] private float animationFps = 6f;

    [Header("Efectos")]
    [Tooltip("Clip de sonido al encenderse (si está vacío usa el de AudioManager).")]
    [SerializeField] private AudioClip activateSound;

    [Tooltip("Luz puntual 2D o partículas opcionales que se activan al encenderse.")]
    [SerializeField] private GameObject litLightObject;

    [Header("Desplazamiento")]
    [Tooltip("Desplazamiento aplicado a la posición de reaparición del jugador.")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.5f, 0f);

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private bool isActivated = false;
    private Coroutine flameAnimationRoutine;

    public bool IsActivated => isActivated;
    public Vector3 RespawnPosition => transform.position + spawnOffset;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;

        if (litLightObject != null)
        {
            litLightObject.SetActive(false);
        }

        if (unlitSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = unlitSprite;
        }
    }

    public void SetSprites(Sprite unlit, Sprite[] lit)
    {
        unlitSprite = unlit;
        litFrames = lit;
        if (spriteRenderer != null && !isActivated)
        {
            spriteRenderer.sprite = unlitSprite;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isActivated) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null && !other.CompareTag("Player")) return;

        ActivateCheckpoint();
    }

    /// <summary>
    /// Enciende el punto de control y registra la posición en CheckpointManager.
    /// </summary>
    public void ActivateCheckpoint()
    {
        if (isActivated) return;
        isActivated = true;

        // 1. Registrar en CheckpointManager
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.SetCheckpoint(RespawnPosition);
        }

        // 2. Iniciar animación de la llama encendida en bucle
        if (litFrames != null && litFrames.Length > 0)
        {
            if (flameAnimationRoutine != null) StopCoroutine(flameAnimationRoutine);
            flameAnimationRoutine = StartCoroutine(FlameAnimationRoutine());
        }

        if (litLightObject != null)
        {
            litLightObject.SetActive(true);
        }

        // 3. Reproducir sonido
        if (AudioManager.Instance != null)
        {
            AudioClip clip = activateSound != null ? activateSound : AudioManager.Instance.checkpointClip;
            AudioManager.Instance.PlaySFX(clip);
        }

        // 4. Mostrar aviso durante 1.5 s
        HUDController hud = Object.FindFirstObjectByType<HUDController>();
        if (hud != null)
        {
            hud.ShowMessage("Punto de control", 1.5f);
        }

        Debug.Log($"[Checkpoint] {name} activado exitosamente en {RespawnPosition}.");
    }

    private IEnumerator FlameAnimationRoutine()
    {
        float frameTime = 1f / animationFps;
        int frameIndex = 0;

        while (true)
        {
            if (litFrames != null && litFrames.Length > 0)
            {
                spriteRenderer.sprite = litFrames[frameIndex];
                frameIndex = (frameIndex + 1) % litFrames.Length;
            }

            yield return new WaitForSeconds(frameTime);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isActivated ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(RespawnPosition, 0.4f);
        Gizmos.DrawLine(transform.position, RespawnPosition);
    }
}
