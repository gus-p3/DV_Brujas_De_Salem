using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelCompletePanel : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;
    public Button nextLevelBtn;
    public Button menuBtn;

    private void Awake()
    {
        if (nextLevelBtn != null)
        {
            nextLevelBtn.onClick.RemoveAllListeners();
            nextLevelBtn.onClick.AddListener(() => {
                if (GameManager.Instance != null) GameManager.Instance.LoadNextLevel();
            });
        }
        if (menuBtn != null)
        {
            menuBtn.onClick.RemoveAllListeners();
            menuBtn.onClick.AddListener(() => {
                if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
            });
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            HandleStateChanged(GameManager.Instance.CurrentState);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    public void HandleStateChanged(GameState state)
    {
        bool isActive = (state == GameState.LevelComplete);
        gameObject.SetActive(isActive);

        if (isActive && GameManager.Instance != null)
        {
            if (scoreText != null) scoreText.text = "Puntaje: " + GameManager.Instance.Score;
            float t = GameManager.Instance.TimeElapsed;
            if (timeText != null) timeText.text = $"Tiempo: {Mathf.FloorToInt(t/60):00}:{Mathf.FloorToInt(t%60):00}";
        }
    }
}
