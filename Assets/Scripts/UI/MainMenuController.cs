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

    [Header("Options")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public TMP_Dropdown qualityDropdown;

    private void Start()
    {
        // Setup initial panels
        ShowMainPanel();

        // Check if there is saved progress
        continueButton.interactable = SaveSystem.HasProgress();

        // Listeners
        playButton.onClick.AddListener(StartNewGame);
        continueButton.onClick.AddListener(ContinueGame);
        optionsButton.onClick.AddListener(ShowOptions);
        creditsButton.onClick.AddListener(ShowCredits);
        exitButton.onClick.AddListener(ExitGame);

        // Options Setup
        if (musicSlider != null)
        {
            musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
            musicSlider.onValueChanged.AddListener(v => {
                if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(v);
            });
        }
        if (sfxSlider != null)
        {
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
            sfxSlider.onValueChanged.AddListener(v => {
                if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(v);
            });
        }
        if (qualityDropdown != null)
        {
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.onValueChanged.AddListener(v => QualitySettings.SetQualityLevel(v));
        }
    }

    public void StartNewGame()
    {
        SceneManager.LoadScene(1); // Load level 1
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
        mainPanel.SetActive(true);
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(false);
    }

    public void ShowOptions()
    {
        mainPanel.SetActive(false);
        optionsPanel.SetActive(true);
        creditsPanel.SetActive(false);
    }

    public void ShowCredits()
    {
        mainPanel.SetActive(false);
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
