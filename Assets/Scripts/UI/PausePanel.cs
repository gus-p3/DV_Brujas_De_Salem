using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PausePanel : MonoBehaviour
{
    public Button resumeBtn;
    public Button restartBtn;
    public Button optionsBtn;
    public Button menuBtn;
    
    public GameObject optionsPanel;

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
        if (resumeBtn != null)
        {
            resumeBtn.onClick.RemoveAllListeners();
            resumeBtn.onClick.AddListener(OnResumeClicked);
        }
        if (restartBtn != null)
        {
            restartBtn.onClick.RemoveAllListeners();
            restartBtn.onClick.AddListener(OnRestartClicked);
        }
        if (menuBtn != null)
        {
            menuBtn.onClick.RemoveAllListeners();
            menuBtn.onClick.AddListener(OnMenuClicked);
        }
        if (optionsBtn != null && optionsPanel != null)
        {
            optionsBtn.onClick.RemoveAllListeners();
            optionsBtn.onClick.AddListener(() => optionsPanel.SetActive(!optionsPanel.activeSelf));
        }
    }

    public void OnResumeClicked()
    {
        Debug.Log("[PausePanel] Reanudar pulsado.");
        if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
        else { Time.timeScale = 1f; gameObject.SetActive(false); }
    }

    public void OnRestartClicked()
    {
        Debug.Log("[PausePanel] Reiniciar pulsado.");
        if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        else { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    }

    public void OnMenuClicked()
    {
        Debug.Log("[PausePanel] Salir al Menú pulsado.");
        if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
        else { Time.timeScale = 1f; SceneManager.LoadScene(0); }
    }

    public void HandleStateChanged(GameState state)
    {
        bool isPaused = (state == GameState.Paused);
        gameObject.SetActive(isPaused);
        if (isPaused)
        {
            SetupButtons();
            if (optionsPanel != null) optionsPanel.SetActive(false);
        }
    }
}
