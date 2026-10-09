using UnityEngine;
using UnityEngine.UI;

public class GameOverPanel : MonoBehaviour
{
    public Button retryBtn;
    public Button menuBtn;

    private void Awake()
    {
        retryBtn.onClick.AddListener(() => GameManager.Instance.RestartLevel());
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
        gameObject.SetActive(state == GameState.GameOver);
    }
}
