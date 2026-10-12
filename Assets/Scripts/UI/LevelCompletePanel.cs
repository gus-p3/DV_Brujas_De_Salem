using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelCompletePanel : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;
    public Button nextLevelBtn;
    public Button menuBtn;

    private void Awake()
    {
        SetupButtons();
    }

    private void OnEnable()
    {
        SetupButtons();
    }

    private void Start()
    {
        SetupButtons();
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

    public void SetupButtons()
    {
        if (nextLevelBtn != null)
        {
            nextLevelBtn.onClick.RemoveAllListeners();
            nextLevelBtn.onClick.AddListener(OnNextLevelClicked);
        }
        if (menuBtn != null)
        {
            menuBtn.onClick.RemoveAllListeners();
            menuBtn.onClick.AddListener(OnMenuClicked);
        }
    }

    public void OnNextLevelClicked()
    {
        Debug.Log("[LevelCompletePanel] Siguiente nivel pulsado.");
        if (GameManager.Instance != null) GameManager.Instance.LoadNextLevel();
        else { Time.timeScale = 1f; SceneManager.LoadScene(0); }
    }

    public void OnMenuClicked()
    {
        Debug.Log("[LevelCompletePanel] Salir al Menú pulsado.");
        if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
        else { Time.timeScale = 1f; SceneManager.LoadScene(0); }
    }

    public void HandleStateChanged(GameState state)
    {
        bool isActive = (state == GameState.LevelComplete);
        gameObject.SetActive(isActive);

        if (isActive)
        {
            SetupButtons();
            if (GameManager.Instance != null)
            {
                if (scoreText != null) scoreText.text = "Puntaje: " + GameManager.Instance.Score;
                float t = GameManager.Instance.TimeElapsed;
                if (timeText != null) timeText.text = $"Tiempo: {Mathf.FloorToInt(t/60):00}:{Mathf.FloorToInt(t%60):00}";
            }
        }
    }
}
