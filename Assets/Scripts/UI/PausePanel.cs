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
        resumeBtn.onClick.AddListener(() => GameManager.Instance.ResumeGame());
        restartBtn.onClick.AddListener(() => GameManager.Instance.RestartLevel());
        menuBtn.onClick.AddListener(() => GameManager.Instance.LoadMainMenu());
        
        if (optionsBtn != null && optionsPanel != null)
        {
            optionsBtn.onClick.AddListener(() => optionsPanel.SetActive(!optionsPanel.activeSelf));
        }
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
        gameObject.SetActive(state == GameState.Paused);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }
}
