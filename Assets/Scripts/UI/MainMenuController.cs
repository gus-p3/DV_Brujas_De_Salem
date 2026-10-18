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

    private void ApplyCleanMenuLayout()
    {
        Transform tTitle = transform.Find("TitleText");
        if (tTitle != null)
        {
            titleObject = tTitle.gameObject;
            RectTransform rt = tTitle.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -48f);
                rt.sizeDelta = new Vector2(1200f, 120f);
            }
            TextMeshProUGUI tmp = tTitle.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.fontSize = 92f;
                tmp.alignment = TextAlignmentOptions.Center;
            }
        }

        // Si existen subtítulos o textos extra debajo de TitleText, espaciarlos sin chocar con Jugar
        int extraIdx = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform ch = transform.GetChild(i);
            if (ch == tTitle || ch.name == "Background" || ch.name == "MainPanel" || ch.name == "OptionsPanel" || ch.name == "CreditsPanel")
                continue;
            TextMeshProUGUI subTmp = ch.GetComponent<TextMeshProUGUI>();
            if (subTmp != null)
            {
                RectTransform srt = ch.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0.5f, 1f);
                srt.anchorMax = new Vector2(0.5f, 1f);
                srt.pivot = new Vector2(0.5f, 1f);
                srt.anchoredPosition = new Vector2(0f, -172f - (extraIdx * 42f));
                srt.sizeDelta = new Vector2(900f, 40f);
                subTmp.fontSize = Mathf.Min(subTmp.fontSize, 28f);
                extraIdx++;
            }
        }

        if (mainPanel != null)
        {
            RectTransform mpRect = mainPanel.GetComponent<RectTransform>();
            if (mpRect != null)
            {
                mpRect.anchorMin = new Vector2(0.5f, 0.5f);
                mpRect.anchorMax = new Vector2(0.5f, 0.5f);
                mpRect.pivot = new Vector2(0.5f, 0.5f);
                mpRect.anchoredPosition = new Vector2(0f, -90f);
                mpRect.sizeDelta = new Vector2(420f, 520f);
            }

            Button[] buttons = { playButton, continueButton, optionsButton, creditsButton, exitButton };
            float[] yPositions = { 160f, 70f, -20f, -110f, -200f };
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                RectTransform brt = buttons[i].GetComponent<RectTransform>();
                if (brt != null)
                {
                    brt.anchorMin = new Vector2(0.5f, 0.5f);
                    brt.anchorMax = new Vector2(0.5f, 0.5f);
                    brt.pivot = new Vector2(0.5f, 0.5f);
                    brt.anchoredPosition = new Vector2(0f, yPositions[i]);
                    brt.sizeDelta = new Vector2(290f, 66f);
                }
            }
        }
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
    }

    public void ShowOptions()
    {
        if (titleObject != null) titleObject.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    public void ShowCredits()
    {
        if (titleObject != null) titleObject.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
