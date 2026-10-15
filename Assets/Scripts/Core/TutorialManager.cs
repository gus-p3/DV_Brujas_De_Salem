using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor de tutoriales contextuales en pantalla.
/// Pausa el tiempo del juego mientras se muestra el tip, encola múltiples avisos
/// y asegura que cada tutorial solo se muestre una vez durante la partida.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    // Memoria estática de tips ya vistos: sobrevive a Reintentar y se reinicia al volver al menú principal
    private static readonly HashSet<string> seenTips = new HashSet<string>();

    [Header("Referencias UI")]
    [Tooltip("Panel de tutorial contextual en el HUD.")]
    [SerializeField] private TutorialPanel tutorialPanel;

    [Header("Tips Registrados")]
    [Tooltip("Biblioteca opcional de tips predefinidos para invocar por identificador.")]
    [SerializeField] private List<TutorialTip> predefinedTips = new List<TutorialTip>();

    private readonly Queue<TutorialTip> tipQueue = new Queue<TutorialTip>();
    private float previousTimeScale = 1.0f;
    private bool isOpen = false;

    /// <summary>
    /// Indica si hay un cuadro de tutorial actualmente desplegado en pantalla.
    /// </summary>
    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (tutorialPanel == null)
        {
            tutorialPanel = Object.FindFirstObjectByType<TutorialPanel>(FindObjectsInactive.Include);
        }
    }

    private void Start()
    {
        // Suscribirse a la primera pérdida de corazón para mostrar tip_corazones (Requisito C8)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHeartLost -= HandleFirstHeartLost;
            GameManager.Instance.OnHeartLost += HandleFirstHeartLost;
        }

        VillagerAI.OnWitchApproaching -= HandleWitchApproachingVillager;
        VillagerAI.OnWitchApproaching += HandleWitchApproachingVillager;

        PotionPickup.OnCollected -= HandlePotionCollected;
        PotionPickup.OnCollected += HandlePotionCollected;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHeartLost -= HandleFirstHeartLost;
        }

        VillagerAI.OnWitchApproaching -= HandleWitchApproachingVillager;
        PotionPickup.OnCollected -= HandlePotionCollected;
    }

    private void HandleWitchApproachingVillager()
    {
        if (!HasSeen("tip_aldeano"))
        {
            ShowTip(new TutorialTip(
                "tip_aldeano",
                "Aldeanos",
                "Los aldeanos no te persiguen de inmediato, pero si te ven en forma humana gritarán y alertarán a los guardias, desatando una turba frenética. ¡Conviértete en gato o escóndete en los maizales!",
                null,
                true
            ));
        }
    }

    private void HandlePotionCollected(PotionType type)
    {
        if (!HasSeen("tip_pocion_recogida"))
        {
            ShowTip(new TutorialTip(
                "tip_pocion_recogida",
                "Pociones Mágicas",
                "Has obtenido una poción mágica. Selecciona con las teclas 1, 2 o 3 y arrójala con la tecla E o clic izquierdo hacia los guardias o aldeanos para neutralizarlos.",
                null,
                true
            ));
        }
    }

    private void HandleFirstHeartLost()
    {
        if (!HasSeen("tip_corazones"))
        {
            ShowTip(new TutorialTip(
                "tip_corazones",
                "Corazones",
                "Un guardia o una caída te quitan un corazón. Si pierdes los tres, el nivel se reinicia. Busca corazones por el camino para recuperarlos.",
                null,
                true
            ));
        }
    }

    /// <summary>
    /// Reinicia el historial de tutoriales vistos (llamado al pulsar Jugar desde el Menú Principal).
    /// </summary>
    public static void ResetSeenTips()
    {
        seenTips.Clear();
        Debug.Log("[TutorialManager] Memoria de tutoriales vistos reiniciada.");
    }

    /// <summary>
    /// Comprueba si un tip ya fue visualizado previamente.
    /// </summary>
    public bool HasSeen(string id)
    {
        return !string.IsNullOrEmpty(id) && seenTips.Contains(id);
    }

    /// <summary>
    /// Despliega un consejo de tutorial. Si ya fue visto, no hace nada. Si hay otro abierto, lo encola.
    /// </summary>
    public void ShowTip(TutorialTip tip)
    {
        if (tip == null || string.IsNullOrEmpty(tip.id)) return;

        if (HasSeen(tip.id))
        {
            return;
        }

        if (isOpen)
        {
            tipQueue.Enqueue(tip);
            return;
        }

        DisplayTip(tip);
    }

    /// <summary>
    /// Despliega un tip buscándolo por su ID en la lista predefinida.
    /// </summary>
    public void ShowTipById(string id)
    {
        if (string.IsNullOrEmpty(id) || HasSeen(id)) return;

        TutorialTip found = predefinedTips.Find(t => t.id == id);
        if (found != null)
        {
            ShowTip(found);
        }
        else
        {
            Debug.LogWarning($"[TutorialManager] No se encontró el tip con id '{id}'.");
        }
    }

    private void DisplayTip(TutorialTip tip)
    {
        isOpen = true;
        seenTips.Add(tip.id);

        if (tip.pauseGame)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        if (tutorialPanel == null)
        {
            tutorialPanel = Object.FindFirstObjectByType<TutorialPanel>(FindObjectsInactive.Include);
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.Show(tip, OnPanelClosed);
        }
        else
        {
            Debug.LogWarning("[TutorialManager] No se encontró TutorialPanel en la escena.");
            OnPanelClosed();
        }
    }

    private void OnPanelClosed()
    {
        Time.timeScale = previousTimeScale;
        isOpen = false;

        // Si hay tips en cola, mostrar el siguiente
        if (tipQueue.Count > 0)
        {
            TutorialTip next = tipQueue.Dequeue();
            DisplayTip(next);
        }
    }
}
