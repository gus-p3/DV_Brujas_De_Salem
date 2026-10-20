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

        FrenzyController.OnFrenzyStarted -= HandleFrenzyStarted;
        FrenzyController.OnFrenzyStarted += HandleFrenzyStarted;

        PotionPickup.OnCollected -= HandlePotionCollected;
        PotionPickup.OnCollected += HandlePotionCollected;

        InitializePredefinedTips();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHeartLost -= HandleFirstHeartLost;
        }

        VillagerAI.OnWitchApproaching -= HandleWitchApproachingVillager;
        FrenzyController.OnFrenzyStarted -= HandleFrenzyStarted;
        PotionPickup.OnCollected -= HandlePotionCollected;
    }

    private void InitializePredefinedTips()
    {
        AddPredefinedTip("tip_mover", "Movimiento y Salto", "Muévete con {Mover} y salta con {Saltar}.");
        AddPredefinedTip("tip_guardia", "Guardias de Salem", "¡Cuidado con los guardias! Si entras en su cono de visión, te capturarán. Busca rutas alternativas o escóndete.");
        AddPredefinedTip("tip_gato_maizal", "Sigilo Felino", "Usa {Transformar} para convertirte en gato. En forma felina puedes ocultarte en el maizal y los guardias no te verán.");
        AddPredefinedTip("tip_poción_recogida", "Pociones Mágicas", "Has recogido una poción. Elige el tipo con {ElegirPoción} y lánzala con {LanzarPoción}.");
        AddPredefinedTip("tip_pocion_recogida", "Pociones Mágicas", "Has recogido una poción. Elige el tipo con {ElegirPoción} y lánzala con {LanzarPoción}.");
        AddPredefinedTip("tip_guardia_bloqueo", "Guardia Bloqueando", "Este guardia no deja pasar. Lánzale una poción de dormir o cegar para neutralizarlo y cruzar.");
        AddPredefinedTip("tip_aldeano", "Aldeanos", "Los aldeanos caminan de forma errática. Si te ven en forma humana, gritarán y entrarán en frenesí para atacarte.");
        AddPredefinedTip("tip_frenesí", "Frenesí de Aldeano", "¡El aldeano está en frenesí! Transfórmate en gato para calmarlo o apártate de su camino antes de que te golpee.");
        AddPredefinedTip("tip_frenesi", "Frenesí de Aldeano", "¡El aldeano está en frenesí! Transfórmate en gato para calmarlo o apártate de su camino antes de que te golpee.");
        AddPredefinedTip("tip_gato_tiempo", "Forma Felina Limitada", "La forma de gato dura poco: vigila la barra morada bajo tus corazones. Vuelve a ser bruja con {Transformar} para recuperar fuerzas.");
        AddPredefinedTip("tip_gato_túnel", "Pasaje Estrecho", "Este pasaje es demasiado estrecho para una bruja. Usa {Transformar} para cruzarlo en forma de gato.");
        AddPredefinedTip("tip_gato_tunel", "Pasaje Estrecho", "Este pasaje es demasiado estrecho para una bruja. Usa {Transformar} para cruzarlo en forma de gato.");
        AddPredefinedTip("tip_escoba_completa", "Escoba Completa", "¡Escoba completada! Mantén {Volar} para volar y usa {VolarSubirBajar} para cambiar de altura. ¡La energía es limitada!");
        AddPredefinedTip("tip_vuelo_brecha", "Vuelo sobre el Abismo", "Esta brecha es demasiado ancha para saltar. Mantén {Volar} para cruzarla volando.");
        AddPredefinedTip("tip_corazones", "Corazones", "Un guardia o una caída te quitan un corazón. Si pierdes los tres, el nivel se reinicia. Busca corazones por el camino para recuperarlos.");
    }

    private void AddPredefinedTip(string id, string title, string body)
    {
        if (!predefinedTips.Exists(t => t.id == id))
        {
            predefinedTips.Add(new TutorialTip(id, title, body, null, true));
        }
    }

    private void HandleWitchApproachingVillager()
    {
        if (!HasSeen("tip_aldeano"))
        {
            ShowTipById("tip_aldeano");
        }
    }

    private void HandleFrenzyStarted()
    {
        if (!HasSeen("tip_frenesí") && !HasSeen("tip_frenesi"))
        {
            ShowTipById("tip_frenesí");
        }
    }

    private void HandlePotionCollected(PotionType type)
    {
        if (!HasSeen("tip_poción_recogida") && !HasSeen("tip_pocion_recogida"))
        {
            ShowTipById("tip_poción_recogida");
        }
    }

    private void HandleFirstHeartLost()
    {
        if (!HasSeen("tip_corazones"))
        {
            ShowTipById("tip_corazones");
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
        isOpen = false;

        // Si hay tips en cola, mostrar el siguiente
        if (tipQueue.Count > 0)
        {
            TutorialTip next = tipQueue.Dequeue();
            DisplayTip(next);
        }
    }
}
