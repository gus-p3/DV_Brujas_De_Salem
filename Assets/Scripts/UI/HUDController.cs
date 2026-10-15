using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;
    public Image[] heartIcons;
    public Slider flightEnergySlider;

    public TextMeshProUGUI blindChargesText;
    public TextMeshProUGUI hypnotizeChargesText;
    public TextMeshProUGUI sleepChargesText;

    [Header("Potion Slots & Selection")]
    [Tooltip("Marcos resaltadores para cada tipo de poción (0: Cegar, 1: Hipnotizar, 2: Dormir).")]
    public Image[] potionSelectionFrames;
    [Tooltip("Textos que indican la tecla de acceso rápido (1, 2, 3) bajo cada poción.")]
    public TextMeshProUGUI[] potionKeyHints;
    [Tooltip("Color de resaltado para la poción seleccionada.")]
    public Color selectedFrameColor = new Color(1f, 0.85f, 0.2f, 1f);
    [Tooltip("Color normal para las pociones no seleccionadas.")]
    public Color unselectedFrameColor = new Color(1f, 1f, 1f, 0.25f);

    [Header("Panels")]
    public PausePanel pausePanel;
    public LevelCompletePanel levelCompletePanel;
    public GameOverPanel gameOverPanel;

    [Header("Messages")]
    [Tooltip("Texto en pantalla para avisos breves al jugador.")]
    public TextMeshProUGUI messageBannerText;
    private Coroutine messageCoroutine;

    private PlayerController player;
    private int selectedPotionIndex = 0;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        EnsureEventSystem();
    }

    private void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[HUDController] EventSystem creado dinámicamente en escena.");
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStatsUpdated += UpdateStatsUI;
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            UpdateStatsUI();
            HandleStateChanged(GameManager.Instance.CurrentState);
        }

        player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.OnFlightEnergyChanged += UpdateFlightEnergy;
            player.OnFlightChanged += HandleFlightChanged;
            UpdateFlightEnergy(player.FlightEnergy01);
            SetFlightSliderVisible(player.CanFly || player.IsFlying);
        }
        else if (flightEnergySlider != null)
        {
            flightEnergySlider.gameObject.SetActive(false);
        }

        PotionInventory inv = Object.FindFirstObjectByType<PotionInventory>();
        if (inv != null)
        {
            inv.OnPotionSelected += HandlePotionSelected;
            SetSelectedPotion((int)inv.SelectedPotion);
        }
        else
        {
            UpdatePotionSelectionVisuals();
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStatsUpdated -= UpdateStatsUI;
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }
        if (player != null)
        {
            player.OnFlightEnergyChanged -= UpdateFlightEnergy;
            player.OnFlightChanged -= HandleFlightChanged;
        }
        PotionInventory inv = Object.FindFirstObjectByType<PotionInventory>();
        if (inv != null)
        {
            inv.OnPotionSelected -= HandlePotionSelected;
        }
    }

    private void HandleFlightChanged(bool isFlying)
    {
        if (player != null)
        {
            SetFlightSliderVisible(player.CanFly || isFlying);
        }
    }

    public void SetFlightSliderVisible(bool visible)
    {
        if (flightEnergySlider != null)
        {
            flightEnergySlider.gameObject.SetActive(visible);
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
        {
            float time = GameManager.Instance.TimeElapsed;
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            timeText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    private void HandleStateChanged(GameState state)
    {
        if (pausePanel != null)
        {
            pausePanel.gameObject.SetActive(state == GameState.Paused);
        }
        if (levelCompletePanel != null)
        {
            levelCompletePanel.gameObject.SetActive(state == GameState.LevelComplete);
            if (state == GameState.LevelComplete && levelCompletePanel.scoreText != null && GameManager.Instance != null)
            {
                levelCompletePanel.scoreText.text = "Puntaje: " + GameManager.Instance.Score;
                float t = GameManager.Instance.TimeElapsed;
                levelCompletePanel.timeText.text = $"Tiempo: {Mathf.FloorToInt(t/60):00}:{Mathf.FloorToInt(t%60):00}";
            }
        }
        if (gameOverPanel != null)
        {
            gameOverPanel.gameObject.SetActive(state == GameState.GameOver);
            if (state == GameState.GameOver && gameOverPanel.messageText != null)
            {
                gameOverPanel.messageText.text = "La hoguera te alcanzó…";
            }
        }
    }

    private void UpdateStatsUI()
    {
        if (GameManager.Instance == null) return;
        scoreText.text = GameManager.Instance.Score.ToString("D4");

        for (int i = 0; i < heartIcons.Length; i++)
        {
            heartIcons[i].enabled = i < GameManager.Instance.Lives;
        }

        blindChargesText.text = $"x {GameManager.Instance.BlindCharges}";
        hypnotizeChargesText.text = $"x {GameManager.Instance.HypnotizeCharges}";
        sleepChargesText.text = $"x {GameManager.Instance.SleepCharges}";
    }

    private void UpdateFlightEnergy(float energy01)
    {
        if (flightEnergySlider != null)
        {
            flightEnergySlider.value = energy01;
        }
    }

    /// <summary>
    /// Muestra un mensaje emergente en pantalla durante el tiempo indicado (en tiempo no escalado).
    /// </summary>
    public void ShowMessage(string message, float duration = 2.5f)
    {
        if (messageBannerText == null) return;
        if (messageCoroutine != null) StopCoroutine(messageCoroutine);
        messageCoroutine = StartCoroutine(ShowMessageRoutine(message, duration));
    }

    private System.Collections.IEnumerator ShowMessageRoutine(string message, float duration)
    {
        messageBannerText.text = message;
        messageBannerText.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        messageBannerText.gameObject.SetActive(false);
        messageCoroutine = null;
    }

    private void HandlePotionSelected(PotionType type)
    {
        SetSelectedPotion((int)type);
    }

    /// <summary>
    /// Selecciona visualmente la poción activa (0: Cegar, 1: Hipnotizar, 2: Dormir) y actualiza su marco.
    /// </summary>
    public void SetSelectedPotion(int index)
    {
        selectedPotionIndex = Mathf.Clamp(index, 0, 2);
        UpdatePotionSelectionVisuals();
    }

    private void UpdatePotionSelectionVisuals()
    {
        if (potionSelectionFrames == null || potionSelectionFrames.Length == 0) return;

        for (int i = 0; i < potionSelectionFrames.Length; i++)
        {
            if (potionSelectionFrames[i] == null) continue;
            bool isSelected = (i == selectedPotionIndex);
            potionSelectionFrames[i].enabled = isSelected;
            potionSelectionFrames[i].color = isSelected ? selectedFrameColor : unselectedFrameColor;
        }
    }

    /// <summary>
    /// Hace parpadear en rojo el contador de la poción cuando se intenta arrojar sin cargas disponibles.
    /// </summary>
    public void FlashEmptyCharges(int potionIndex)
    {
        TextMeshProUGUI targetText = potionIndex switch
        {
            0 => blindChargesText,
            1 => hypnotizeChargesText,
            2 => sleepChargesText,
            _ => null
        };

        if (targetText == null) return;
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashTextRoutine(targetText));
        ShowMessage("¡No te quedan cargas de esa poción!", 1.5f);
    }

    private System.Collections.IEnumerator FlashTextRoutine(TextMeshProUGUI text)
    {
        Color originalColor = text.color;
        for (int i = 0; i < 3; i++)
        {
            text.color = Color.red;
            yield return new WaitForSecondsRealtime(0.09f);
            text.color = Color.white;
            yield return new WaitForSecondsRealtime(0.09f);
        }
        text.color = originalColor;
        flashCoroutine = null;
    }
}
