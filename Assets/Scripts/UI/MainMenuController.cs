using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject creditsPanel;

    [Header("Buttons")]
    public Button continueButton;
    public Button playButton;
    public Button optionsButton;
    public Button creditsButton;
    public Button exitButton;

    [Header("Back Buttons")]
    public Button backFromOptionsButton;
    public Button backFromCreditsButton;

    [Header("Options")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public TMP_Dropdown qualityDropdown;

    private GameObject titleObject;

    private void Start()
    {
        ApplyCleanMenuLayout();

        // Setup initial panels
        ShowMainPanel();

        // Check if there is saved progress
        if (continueButton != null)
        {
            continueButton.interactable = SaveSystem.HasProgress();
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(ContinueGame);
        }

        // Listeners for main buttons
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(StartNewGame);
        }

        if (optionsButton != null)
        {
            optionsButton.onClick.RemoveAllListeners();
            optionsButton.onClick.AddListener(ShowOptions);
        }

        if (creditsButton != null)
        {
            creditsButton.onClick.RemoveAllListeners();
            creditsButton.onClick.AddListener(ShowCredits);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(ExitGame);
        }

        // Auto-discover Back Buttons in children if not assigned
        if (backFromOptionsButton == null && optionsPanel != null)
        {
            foreach (var b in optionsPanel.GetComponentsInChildren<Button>(true))
            {
                if (b.name.ToLower().Contains("back") || b.name.ToLower().Contains("volver"))
                {
                    backFromOptionsButton = b;
                    break;
                }
            }
        }

        if (backFromCreditsButton == null && creditsPanel != null)
        {
            foreach (var b in creditsPanel.GetComponentsInChildren<Button>(true))
            {
                if (b.name.ToLower().Contains("back") || b.name.ToLower().Contains("volver"))
                {
                    backFromCreditsButton = b;
                    break;
                }
            }
        }

        if (backFromOptionsButton != null)
        {
            backFromOptionsButton.onClick.RemoveAllListeners();
            backFromOptionsButton.onClick.AddListener(ShowMainPanel);
        }

        if (backFromCreditsButton != null)
        {
            backFromCreditsButton.onClick.RemoveAllListeners();
            backFromCreditsButton.onClick.AddListener(ShowMainPanel);
        }

        // Options Setup
        if (musicSlider != null)
        {
            musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1.0f);
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(v => {
                if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(v);
            });
        }
        if (sfxSlider != null)
        {
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.85f);
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(v => {
                if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(v);
            });
        }
        if (qualityDropdown != null)
        {
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.onValueChanged.RemoveAllListeners();
            qualityDropdown.onValueChanged.AddListener(v => QualitySettings.SetQualityLevel(v));
        }

        if (AudioManager.Instance != null && AudioManager.Instance.menuMusicClip != null)
        {
            AudioManager.Instance.PlayMusic(AudioManager.Instance.menuMusicClip);
        }
    }

    /// <summary>
    /// La maquetación del menú la hace Tools > Menú > Maquetar menú principal (MainMenuStyler) y queda
    /// guardada en la escena; aquí solo se localiza el título para ocultarlo en los paneles secundarios.
    /// </summary>
    private void ApplyCleanMenuLayout()
    {
        Transform tTitle = transform.Find("TitleGroup");
        if (tTitle == null) tTitle = transform.Find("TitleText");
        if (tTitle != null) titleObject = tTitle.gameObject;
    }

    public void StartNewGame()
    {
        TutorialManager.ResetSeenTips();
        SceneManager.LoadScene("Level1");
        if (GameManager.Instance != null) GameManager.Instance.StartLevel();
    }

    public void ContinueGame()
    {
        int maxLevel = SaveSystem.GetMaxLevelUnlocked();
        SceneManager.LoadScene(maxLevel);
        if (GameManager.Instance != null) GameManager.Instance.StartLevel();
    }

    public void ShowMainPanel()
    {
        if (titleObject != null) titleObject.SetActive(true);
        if (mainPanel != null) mainPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        Select(playButton);
    }

    public void ShowOptions()
    {
        if (titleObject != null) titleObject.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        Select(backFromOptionsButton);
    }

    public void ShowCredits()
    {
        if (titleObject != null) titleObject.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
        Select(backFromCreditsButton);
    }

    /// <summary>Esc vuelve al menú principal desde Opciones o Créditos.</summary>
    private void Update()
    {
        bool inSubPanel = (optionsPanel != null && optionsPanel.activeSelf) || (creditsPanel != null && creditsPanel.activeSelf);
        if (inSubPanel && Input.GetKeyDown(KeyCode.Escape)) ShowMainPanel();
    }

    /// <summary>Deja un botón seleccionado para poder navegar con teclado o mando.</summary>
    private static void Select(Button b)
    {
        if (b != null && b.gameObject.activeInHierarchy && UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(b.gameObject);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
