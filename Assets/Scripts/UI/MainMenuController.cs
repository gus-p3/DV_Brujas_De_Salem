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

    private void Start()
    {
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
            musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(v => {
                if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(v);
            });
        }
        if (sfxSlider != null)
        {
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
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

    public void StartNewGame()
    {
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
        if (mainPanel != null) mainPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    public void ShowOptions()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    public void ShowCredits()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
