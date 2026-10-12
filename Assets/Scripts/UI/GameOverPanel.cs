using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverPanel : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public Image fireImage;
    public Button retryBtn;
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
        {
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    public void SetupButtons()
    {
        if (retryBtn == null)
        {
            foreach (var b in GetComponentsInChildren<Button>(true))
            {
                if (b.name.ToLower().Contains("retry") || b.name.ToLower().Contains("reintentar"))
                {
                    retryBtn = b;
                    break;
                }
            }
        }

        if (menuBtn == null)
        {
            foreach (var b in GetComponentsInChildren<Button>(true))
            {
                if (b.name.ToLower().Contains("menu") || b.name.ToLower().Contains("menú"))
                {
                    menuBtn = b;
                    break;
                }
            }
        }

        if (retryBtn != null)
        {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(OnRetryClicked);
        }

        if (menuBtn != null)
        {
            menuBtn.onClick.RemoveAllListeners();
            menuBtn.onClick.AddListener(OnMenuClicked);
        }
    }

    public void OnRetryClicked()
    {
        Debug.Log("[GameOverPanel] Botón Reintentar pulsado.");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartLevel();
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void OnMenuClicked()
    {
        Debug.Log("[GameOverPanel] Botón Salir al Menú pulsado.");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadMainMenu();
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }
    }

    public void HandleStateChanged(GameState state)
    {
        bool isGameOver = (state == GameState.GameOver);
        gameObject.SetActive(isGameOver);
        if (isGameOver)
        {
            SetupButtons();
            if (messageText != null)
            {
                messageText.text = "La hoguera te alcanzó…";
            }
        }
    }
}
