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
        nextLevelBtn.onClick.AddListener(() => GameManager.Instance.LoadNextLevel());
        menuBtn.onClick.AddListener(() => GameManager.Instance.LoadMainMenu());
    }

    private void Start()
    {
        GameManager.Instance.OnStateChanged += HandleStateChanged;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState state)
    {
        bool isActive = (state == GameState.LevelComplete);
        gameObject.SetActive(isActive);

        if (isActive)
        {
            scoreText.text = "Puntaje: " + GameManager.Instance.Score;
            float t = GameManager.Instance.TimeElapsed;
            timeText.text = $"Tiempo: {Mathf.FloorToInt(t/60):00}:{Mathf.FloorToInt(t%60):00}";
        }
    }
}
