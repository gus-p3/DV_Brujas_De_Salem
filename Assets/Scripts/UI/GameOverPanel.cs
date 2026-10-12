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
        if (retryBtn != null) retryBtn.onClick.AddListener(() => GameManager.Instance.RestartLevel());
        if (menuBtn != null) menuBtn.onClick.AddListener(() => GameManager.Instance.LoadMainMenu());
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
        bool isGameOver = (state == GameState.GameOver);
        gameObject.SetActive(isGameOver);
        if (isGameOver && messageText != null)
        {
            messageText.text = "La hoguera te alcanzó…";
        }
    }
}
