using UnityEngine;
using UnityEngine.UI;

public class PausePanel : MonoBehaviour
{
    public Button resumeBtn;
    public Button restartBtn;
    public Button optionsBtn;
    public Button menuBtn;
    
    public GameObject optionsPanel;

    private void Awake()
    {
        if (resumeBtn != null)
        {
            resumeBtn.onClick.RemoveAllListeners();
            resumeBtn.onClick.AddListener(() => {
                if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
            });
        }
        if (restartBtn != null)
        {
            restartBtn.onClick.RemoveAllListeners();
            restartBtn.onClick.AddListener(() => {
                if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
            });
        }
        if (menuBtn != null)
        {
            menuBtn.onClick.RemoveAllListeners();
            menuBtn.onClick.AddListener(() => {
                if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
            });
        }
        
        if (optionsBtn != null && optionsPanel != null)
        {
            optionsBtn.onClick.RemoveAllListeners();
            optionsBtn.onClick.AddListener(() => optionsPanel.SetActive(!optionsPanel.activeSelf));
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
        gameObject.SetActive(state == GameState.Paused);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }
}
