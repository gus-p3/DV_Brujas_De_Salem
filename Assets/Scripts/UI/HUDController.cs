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

    [Header("Panels")]
    public PausePanel pausePanel;
    public LevelCompletePanel levelCompletePanel;
    public GameOverPanel gameOverPanel;

    [Header("Messages")]
    [Tooltip("Texto en pantalla para avisos breves al jugador.")]
    public TextMeshProUGUI messageBannerText;
    private Coroutine messageCoroutine;

    private PlayerController player;

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
}
