using UnityEngine;
using UnityEngine.UI;

public class GameOverPanel : MonoBehaviour
{
    public TMPro.TextMeshProUGUI messageText;
    public Image fireImage;
    public Button retryBtn;
    public Button menuBtn;

    private void Awake()
    {
        if (retryBtn != null) 
        {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(() => {
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
        {
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    public void HandleStateChanged(GameState state)
    {
        bool isGameOver = (state == GameState.GameOver);
        gameObject.SetActive(isGameOver);
        if (isGameOver && messageText != null)
        {
            messageText.text = "La hoguera te alcanzó…";
        }
    }
}
