using UnityEngine;

/// <summary>
/// Controla el fundido de opacidad (alfa) de los tallos frontales del maizal
/// cuando el jugador entra o sale de la HideoutZone, permitiendo que el gato
/// sea visible para el jugador mientras permanece oculto ante los guardias.
/// </summary>
public class CornFoliageFade : MonoBehaviour
{
    [Header("Configuración de Alfa")]
    [Tooltip("Opacidad de los tallos frontales cuando el jugador está fuera del maizal.")]
    [Range(0f, 1f)]
    [SerializeField] private float normalAlpha = 0.55f;

    [Tooltip("Opacidad de los tallos frontales cuando el jugador entra al maizal.")]
    [Range(0f, 1f)]
    [SerializeField] private float fadeAlpha = 0.35f;

    [Tooltip("Velocidad de transición del fundido.")]
    [SerializeField] private float fadeSpeed = 5f;

    private SpriteRenderer[] foregroundStalks;
    private HideoutZone hideoutZone;
    private float targetAlpha;
    private float currentAlpha;

    private void Awake()
    {
        hideoutZone = GetComponent<HideoutZone>();
        // Buscar todos los SpriteRenderer en objetos hijos cuyos nombres contengan Stalk_Fg
        SpriteRenderer[] allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        var stalkList = new System.Collections.Generic.List<SpriteRenderer>();
        foreach (var sr in allRenderers)
        {
            if (sr.gameObject.name.Contains("Stalk_Fg"))
            {
                stalkList.Add(sr);
            }
        }
        foregroundStalks = stalkList.ToArray();

        currentAlpha = normalAlpha;
        targetAlpha = normalAlpha;
        ApplyAlpha(currentAlpha);
    }

    private void OnEnable()
    {
        if (hideoutZone != null)
        {
            hideoutZone.OnPlayerEntered += HandlePlayerEntered;
            hideoutZone.OnPlayerExited += HandlePlayerExited;
        }
    }

    private void OnDisable()
    {
        if (hideoutZone != null)
        {
            hideoutZone.OnPlayerEntered -= HandlePlayerEntered;
            hideoutZone.OnPlayerExited -= HandlePlayerExited;
        }
    }

    private void HandlePlayerEntered()
    {
        targetAlpha = fadeAlpha;
        SetPlayerStealthVisual(true);
    }

    private void HandlePlayerExited()
    {
        targetAlpha = normalAlpha;
        SetPlayerStealthVisual(false);
    }

    // ------------------------------------------------------------- Visibilidad del jugador
    private static int activeFieldCount = 0;
    private static SpriteRenderer playerRenderer;
    private static string originalLayer;
    private static int originalOrder;
    private static Color originalColor = Color.white;
    private bool playerInside = false;

    [Header("Indicador de sigilo")]
    [Tooltip("Tinte aplicado a la bruja/gato mientras está oculto en el maizal.")]
    [SerializeField] private Color stealthTint = new Color(0.72f, 0.68f, 1f, 0.9f);

    private void SetPlayerStealthVisual(bool inside)
    {
        if (inside == playerInside) return;
        playerInside = inside;

        if (playerRenderer == null)
        {
            PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                Animator a = pc.GetComponentInChildren<Animator>();
                playerRenderer = a != null ? a.GetComponent<SpriteRenderer>() : pc.GetComponentInChildren<SpriteRenderer>();
            }
        }
        if (playerRenderer == null) return;

        if (inside)
        {
            if (activeFieldCount == 0)
            {
                originalLayer = playerRenderer.sortingLayerName;
                originalOrder = playerRenderer.sortingOrder;
                originalColor = playerRenderer.color;
            }
            activeFieldCount++;
            // Delante de los tallos frontales (Foreground/5) para que nunca desaparezca
            playerRenderer.sortingLayerName = "Foreground";
            playerRenderer.sortingOrder = 8;
            playerRenderer.color = stealthTint;
        }
        else
        {
            activeFieldCount = Mathf.Max(0, activeFieldCount - 1);
            if (activeFieldCount == 0)
            {
                playerRenderer.sortingLayerName = string.IsNullOrEmpty(originalLayer) ? "Gameplay" : originalLayer;
                playerRenderer.sortingOrder = originalOrder;
                playerRenderer.color = originalColor.a <= 0f ? Color.white : originalColor;
            }
        }
    }

    private void OnDestroy()
    {
        if (playerInside)
        {
            activeFieldCount = Mathf.Max(0, activeFieldCount - 1);
        }
        if (activeFieldCount == 0) playerRenderer = null;
    }

    private void Update()
    {
        if (!Mathf.Approximately(currentAlpha, targetAlpha))
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
            ApplyAlpha(currentAlpha);
        }
    }

    private void ApplyAlpha(float alpha)
    {
        if (foregroundStalks == null) return;
        for (int i = 0; i < foregroundStalks.Length; i++)
        {
            if (foregroundStalks[i] != null)
            {
                Color c = foregroundStalks[i].color;
                c.a = alpha;
                foregroundStalks[i].color = c;
            }
        }
    }
}
