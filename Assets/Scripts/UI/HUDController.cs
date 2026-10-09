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

    private PlayerController player;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStatsUpdated += UpdateStatsUI;
            UpdateStatsUI();
        }

        player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.OnFlightEnergyChanged += UpdateFlightEnergy;
            UpdateFlightEnergy(player.FlightEnergy01);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStatsUpdated -= UpdateStatsUI;
        }
        if (player != null)
        {
            player.OnFlightEnergyChanged -= UpdateFlightEnergy;
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
}
