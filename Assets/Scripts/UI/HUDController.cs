using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;
    public Image[] heartIcons;
    public Slider flightEnergySlider;
    [Tooltip("Barra de energía de la forma de gato (se busca 'CatSlider' si está vacía).")]
    public Slider catEnergySlider;

    public TextMeshProUGUI blindChargesText;
    public TextMeshProUGUI hypnotizeChargesText;
    public TextMeshProUGUI sleepChargesText;

    [Header("Potion Slots & Selection")]
    [Tooltip("Marcos resaltadores para cada tipo de poción (0: Cegar, 1: Hipnotizar, 2: Dormir).")]
    public Image[] potionSelectionFrames;
    [Tooltip("Textos que indican la tecla de acceso rápido (1, 2, 3) bajo cada poción.")]
    public TextMeshProUGUI[] potionKeyHints;
    [Tooltip("Color de resaltado para la poción seleccionada.")]
    public Color selectedFrameColor = new Color(1f, 0.85f, 0.2f, 1f);
    [Tooltip("Color normal para las pociones no seleccionadas.")]
    public Color unselectedFrameColor = new Color(1f, 1f, 1f, 0.25f);

    [Header("Panels")]
    public PausePanel pausePanel;
    public LevelCompletePanel levelCompletePanel;
    public GameOverPanel gameOverPanel;

    [Header("Frenzy Alert")]
    [Tooltip("Viñeta roja en pantalla completa durante el frenesí.")]
    public Image frenzyVignetteImage;
    [Tooltip("Icono de alarma parpadeante junto a los corazones.")]
    public Image frenzyAlarmIcon;
    private Coroutine frenzyPulseCoroutine;

    [Header("Messages")]
    [Tooltip("Texto en pantalla para avisos breves al jugador.")]
    public TextMeshProUGUI messageBannerText;
    private Coroutine messageCoroutine;

    private PlayerController player;
    private int selectedPotionIndex = 0;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        EnsureEventSystem();
    }

    private void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[HUDController] EventSystem creado dinámicamente en escena.");
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStatsUpdated += UpdateStatsUI;
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            UpdateStatsUI();
            HandleStateChanged(GameManager.Instance.CurrentState);
        }

        player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.OnFlightEnergyChanged += UpdateFlightEnergy;
            player.OnFlightChanged += HandleFlightChanged;
            UpdateFlightEnergy(player.FlightEnergy01);
            SetFlightSliderVisible(player.IsFlying);
        }
        else if (flightEnergySlider != null)
        {
            flightEnergySlider.gameObject.SetActive(false);
        }

        if (catEnergySlider == null)
        {
            Transform t = transform.Find("CatSlider");
            if (t != null) catEnergySlider = t.GetComponent<Slider>();
        }
        transformation = Object.FindFirstObjectByType<TransformationManager>();
        if (transformation != null)
        {
            transformation.OnCatEnergyChanged += UpdateCatEnergy;
            transformation.OnTransformed += HandleTransformed;
            UpdateCatEnergy(transformation.CatEnergy01);
        }
        else if (catEnergySlider != null)
        {
            catEnergySlider.gameObject.SetActive(false);
        }

        PotionInventory inv = Object.FindFirstObjectByType<PotionInventory>();
        if (inv != null)
        {
            inv.OnPotionSelected += HandlePotionSelected;
            SetSelectedPotion((int)inv.SelectedPotion);
        }
        else
        {
            UpdatePotionSelectionVisuals();
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStatsUpdated -= UpdateStatsUI;
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }
        if (player != null)
        {
            player.OnFlightEnergyChanged -= UpdateFlightEnergy;
            player.OnFlightChanged -= HandleFlightChanged;
        }
        if (transformation != null)
        {
            transformation.OnCatEnergyChanged -= UpdateCatEnergy;
            transformation.OnTransformed -= HandleTransformed;
        }
        PotionInventory inv = Object.FindFirstObjectByType<PotionInventory>();
        if (inv != null)
        {
            inv.OnPotionSelected -= HandlePotionSelected;
        }
    }

    private TransformationManager transformation;

    private void HandleTransformed(bool isCat)
    {
        if (transformation != null) UpdateCatEnergy(transformation.CatEnergy01);
    }

    /// <summary>Visible mientras es gato o mientras la energía se recupera; roja cuando queda poca.</summary>
    private void UpdateCatEnergy(float energy01)
    {
        if (catEnergySlider == null || transformation == null) return;
        bool show = transformation.IsCatForm || energy01 < 0.999f;
        if (catEnergySlider.gameObject.activeSelf != show) catEnergySlider.gameObject.SetActive(show);
        catEnergySlider.value = energy01;
        if (catEnergySlider.fillRect != null)
        {
            Image fill = catEnergySlider.fillRect.GetComponent<Image>();
            if (fill != null)
            {
                bool low = energy01 <= transformation.catLowEnergyWarning;
                bool blocked = !transformation.IsCatForm && energy01 < transformation.catMinEnergyToTransform;
                Color normal = new Color(0.62f, 0.42f, 0.95f, 1f);
                Color warn = new Color(0.95f, 0.3f, 0.3f, 1f);
                // Parpadea en rojo cuando se agota siendo gato; gris mientras no alcanza para transformarse
                fill.color = blocked ? new Color(0.5f, 0.5f, 0.55f, 1f)
                    : low && transformation.IsCatForm ? Color.Lerp(normal, warn, (Mathf.Sin(Time.unscaledTime * 12f) + 1f) * 0.5f)
                    : normal;
            }
        }
    }

    private void HandleFlightChanged(bool isFlying)
    {
        SetFlightSliderVisible(isFlying);
    }

    public void SetFlightSliderVisible(bool visible)
    {
        if (flightEnergySlider != null)
        {
            flightEnergySlider.gameObject.SetActive(visible);
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
        {
            float time = GameManager.Instance.TimeElapsed;
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            timeText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    private void HandleStateChanged(GameState state)
    {
        if (pausePanel != null)
        {
            pausePanel.gameObject.SetActive(state == GameState.Paused);
        }
        if (levelCompletePanel != null)
        {
            levelCompletePanel.gameObject.SetActive(state == GameState.LevelComplete);
            if (state == GameState.LevelComplete && levelCompletePanel.scoreText != null && GameManager.Instance != null)
            {
                levelCompletePanel.scoreText.text = "Puntaje: " + GameManager.Instance.Score;
                float t = GameManager.Instance.TimeElapsed;
                levelCompletePanel.timeText.text = $"Tiempo: {Mathf.FloorToInt(t/60):00}:{Mathf.FloorToInt(t%60):00}";
            }
        }
        if (gameOverPanel != null)
        {
            gameOverPanel.gameObject.SetActive(state == GameState.GameOver);
            if (state == GameState.GameOver && gameOverPanel.messageText != null)
            {
                gameOverPanel.messageText.text = "La hoguera te alcanzó…";
            }
        }
    }

    private void UpdateStatsUI()
    {
        if (GameManager.Instance == null) return;
        if (scoreText != null) scoreText.text = GameManager.Instance.Score.ToString("D4");

        if (heartIcons != null)
        {
            for (int i = 0; i < heartIcons.Length; i++)
            {
                if (heartIcons[i] != null)
                {
                    heartIcons[i].enabled = i < GameManager.Instance.Lives;
                }
            }
        }

        if (blindChargesText != null) blindChargesText.text = $"x {GameManager.Instance.BlindCharges}";
        if (hypnotizeChargesText != null) hypnotizeChargesText.text = $"x {GameManager.Instance.HypnotizeCharges}";
        if (sleepChargesText != null) sleepChargesText.text = $"x {GameManager.Instance.SleepCharges}";

        UpdatePotionSelectionVisuals();
    }

    private void UpdateFlightEnergy(float energy01)
    {
        if (flightEnergySlider != null)
        {
            flightEnergySlider.value = energy01;
        }
    }

    /// <summary>
    /// Muestra un mensaje emergente en pantalla durante el tiempo indicado (en tiempo no escalado).
    /// Limpia cualquier mensaje anterior para evitar superposiciones.
    /// </summary>
    public void ShowMessage(string message, float duration = 2.0f)
    {
        if (messageBannerText == null) return;
        // Mensajes legibles a 1080p: letra grande y caja ancha con ajuste de línea
        if (messageBannerText.fontSize < 42f)
        {
            messageBannerText.fontSize = 42f;
        }
        RectTransform bannerRect = messageBannerText.rectTransform;
        if (bannerRect.sizeDelta.x < 1300f)
        {
            bannerRect.sizeDelta = new Vector2(1300f, Mathf.Max(bannerRect.sizeDelta.y, 120f));
        }
        messageBannerText.textWrappingMode = TMPro.TextWrappingModes.Normal;
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
            messageBannerText.text = "";
            messageBannerText.gameObject.SetActive(false);
            messageCoroutine = null;
        }
        messageCoroutine = StartCoroutine(ShowMessageRoutine(message, duration));
    }

    private System.Collections.IEnumerator ShowMessageRoutine(string message, float duration)
    {
        messageBannerText.text = message;
        messageBannerText.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        messageBannerText.text = "";
        messageBannerText.gameObject.SetActive(false);
        messageCoroutine = null;
    }

    private void HandlePotionSelected(PotionType type)
    {
        SetSelectedPotion((int)type);
    }

    /// <summary>
    /// Selecciona visualmente la poción activa (0: Cegar, 1: Hipnotizar, 2: Dormir) y actualiza su marco.
    /// </summary>
    public void SetSelectedPotion(int index)
    {
        selectedPotionIndex = Mathf.Clamp(index, 0, 2);
        UpdatePotionSelectionVisuals();
    }

    private void UpdatePotionSelectionVisuals()
    {
        if (potionSelectionFrames == null || potionSelectionFrames.Length == 0) return;

        int blindCharges = GameManager.Instance != null ? GameManager.Instance.BlindCharges : 0;
        int hypCharges = GameManager.Instance != null ? GameManager.Instance.HypnotizeCharges : 0;
        int sleepCharges = GameManager.Instance != null ? GameManager.Instance.SleepCharges : 0;
        int[] charges = { blindCharges, hypCharges, sleepCharges };

        for (int i = 0; i < potionSelectionFrames.Length; i++)
        {
            if (potionSelectionFrames[i] == null) continue;
            bool isSelected = (i == selectedPotionIndex);
            bool hasCharges = i < charges.Length && charges[i] > 0;

            // Desactivar Image cuando la ranura no tiene sprite asignado para evitar el recuadro amarillo sólido
            if (potionSelectionFrames[i].sprite == null)
            {
                potionSelectionFrames[i].enabled = false;
            }
            else
            {
                potionSelectionFrames[i].enabled = isSelected && hasCharges;
                potionSelectionFrames[i].color = isSelected ? selectedFrameColor : unselectedFrameColor;
            }

            // Resaltar el icono padre con escala y borde dorado sutil cuando está seleccionado
            Transform flaskParent = potionSelectionFrames[i].transform.parent;
            if (flaskParent != null)
            {
                flaskParent.localScale = isSelected ? new Vector3(1.15f, 1.15f, 1f) : Vector3.one;
                Outline outline = flaskParent.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = flaskParent.gameObject.AddComponent<Outline>();
                    outline.effectDistance = new Vector2(2f, -2f);
                }
                outline.effectColor = new Color(1f, 0.85f, 0.2f, 0.95f);
                outline.enabled = isSelected;
            }
        }
    }

    /// <summary>
    /// Hace parpadear en rojo el contador de la poción cuando se intenta arrojar sin cargas disponibles.
    /// </summary>
    public void FlashEmptyCharges(int potionIndex)
    {
        TextMeshProUGUI targetText = potionIndex switch
        {
            0 => blindChargesText,
            1 => hypnotizeChargesText,
            2 => sleepChargesText,
            _ => null
        };

        if (targetText == null) return;
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashTextRoutine(targetText));
        ShowMessage("¡No te quedan cargas de esa poción!", 1.5f);
    }

    private System.Collections.IEnumerator FlashTextRoutine(TextMeshProUGUI text)
    {
        Color originalColor = text.color;
        for (int i = 0; i < 3; i++)
        {
            text.color = Color.red;
            yield return new WaitForSecondsRealtime(0.09f);
            text.color = Color.white;
            yield return new WaitForSecondsRealtime(0.09f);
        }
        text.color = originalColor;
        flashCoroutine = null;
    }

    /// <summary>
    /// Activa o desactiva la viñeta roja y el icono parpadeante de alarma por frenesí en el HUD.
    /// </summary>
    public void SetFrenzyVisuals(bool active)
    {
        EnsureFrenzyElements();

        if (frenzyPulseCoroutine != null)
        {
            StopCoroutine(frenzyPulseCoroutine);
            frenzyPulseCoroutine = null;
        }

        if (active)
        {
            frenzyPulseCoroutine = StartCoroutine(FrenzyPulseRoutine());
        }
        else
        {
            if (frenzyVignetteImage != null) frenzyVignetteImage.gameObject.SetActive(false);
            if (frenzyAlarmIcon != null) frenzyAlarmIcon.gameObject.SetActive(false);
        }
    }

    private void EnsureFrenzyElements()
    {
        if (frenzyVignetteImage == null)
        {
            Transform t = transform.Find("FrenzyVignette");
            if (t != null) frenzyVignetteImage = t.GetComponent<Image>();
            else
            {
                GameObject g = new GameObject("FrenzyVignette");
                g.transform.SetParent(transform, false);
                g.transform.SetAsFirstSibling();
                frenzyVignetteImage = g.AddComponent<Image>();
                frenzyVignetteImage.color = new Color(0.9f, 0.1f, 0.1f, 0.25f);
                frenzyVignetteImage.raycastTarget = false;
                RectTransform rt = g.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                g.SetActive(false);
            }
        }

        if (frenzyAlarmIcon == null)
        {
            Transform t = transform.Find("FrenzyAlarmIcon");
            if (t != null) frenzyAlarmIcon = t.GetComponent<Image>();
            else
            {
                GameObject g = new GameObject("FrenzyAlarmIcon");
                g.transform.SetParent(transform, false);
                frenzyAlarmIcon = g.AddComponent<Image>();
                frenzyAlarmIcon.color = new Color(1f, 0.2f, 0.2f, 1f);
                frenzyAlarmIcon.raycastTarget = false;
                RectTransform rt = g.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(170f, -24f);
                rt.sizeDelta = new Vector2(32f, 32f);
                g.SetActive(false);
            }
        }
    }

    private System.Collections.IEnumerator FrenzyPulseRoutine()
    {
        if (frenzyVignetteImage != null) frenzyVignetteImage.gameObject.SetActive(true);
        if (frenzyAlarmIcon != null) frenzyAlarmIcon.gameObject.SetActive(true);

        while (true)
        {
            float alpha = 0.15f + Mathf.Sin(Time.time * 6f) * 0.15f;
            if (frenzyVignetteImage != null)
            {
                Color c = frenzyVignetteImage.color;
                c.a = alpha;
                frenzyVignetteImage.color = c;
            }
            if (frenzyAlarmIcon != null)
            {
                frenzyAlarmIcon.enabled = (Mathf.FloorToInt(Time.time * 4f) % 2) == 0;
            }
            yield return null;
        }
    }
}
