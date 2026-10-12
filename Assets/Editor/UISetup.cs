using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class UISetup
{
    private static readonly Color ParchmentWood = new Color(0.45f, 0.28f, 0.12f, 1f);
    private static readonly Color ButtonGold = new Color(0.85f, 0.68f, 0.25f, 1f);
    private static readonly Color DarkPanelBg = new Color(0.12f, 0.08f, 0.05f, 0.92f);

    [MenuItem("Brujas/5. Escenas/Generar Escena MainMenu")]
    public static void BuildMainMenu()
    {
        Scene mainMenuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        // 1. Camera
        GameObject cameraObj = new GameObject("Main Camera");
        Camera cam = cameraObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        
        // 2. Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // EventSystem
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/main_menu_bg.png");
        if (bgSprite != null) bgImg.sprite = bgSprite;
        else bgImg.color = new Color(0.08f, 0.07f, 0.12f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Title
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Brujas de Salem";
        titleText.fontSize = 110;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.96f, 0.82f, 0.45f);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.78f);
        titleRect.anchorMax = new Vector2(0.5f, 0.92f);
        titleRect.sizeDelta = new Vector2(1200, 150);
        titleRect.anchoredPosition = Vector2.zero;

        MainMenuController mmc = canvasObj.AddComponent<MainMenuController>();
        
        // Main Panel
        GameObject mainPanel = new GameObject("MainPanel");
        mainPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform mpRect = mainPanel.AddComponent<RectTransform>();
        mpRect.anchorMin = new Vector2(0.5f, 0.15f); 
        mpRect.anchorMax = new Vector2(0.5f, 0.72f);
        mpRect.sizeDelta = new Vector2(420, 0); 
        mpRect.anchoredPosition = Vector2.zero;
        mmc.mainPanel = mainPanel;
        
        mmc.playButton = CreateStyledButton("PlayButton", "Jugar", mainPanel.transform, new Vector2(0, 170));
        mmc.continueButton = CreateStyledButton("ContinueButton", "Continuar", mainPanel.transform, new Vector2(0, 75));
        mmc.optionsButton = CreateStyledButton("OptionsButton", "Opciones", mainPanel.transform, new Vector2(0, -20));
        mmc.creditsButton = CreateStyledButton("CreditsButton", "Créditos", mainPanel.transform, new Vector2(0, -115));
        mmc.exitButton = CreateStyledButton("ExitButton", "Salir", mainPanel.transform, new Vector2(0, -210));

        // Options Panel
        GameObject optionsPanel = new GameObject("OptionsPanel");
        optionsPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform opRect = optionsPanel.AddComponent<RectTransform>();
        opRect.anchorMin = new Vector2(0.25f, 0.15f); 
        opRect.anchorMax = new Vector2(0.75f, 0.85f);
        opRect.sizeDelta = Vector2.zero; 
        opRect.anchoredPosition = Vector2.zero;
        optionsPanel.AddComponent<Image>().color = DarkPanelBg;
        mmc.optionsPanel = optionsPanel;

        CreatePanelTitle("TítuloOpciones", "Opciones", optionsPanel.transform, new Vector2(0, 280));

        CreateLabel("MusicLabel", "Volumen de Música", optionsPanel.transform, new Vector2(0, 170));
        mmc.musicSlider = CreateStyledSlider("MusicSlider", optionsPanel.transform, new Vector2(0, 120), 0.75f);

        CreateLabel("SFXLabel", "Volumen de Efectos", optionsPanel.transform, new Vector2(0, 40));
        mmc.sfxSlider = CreateStyledSlider("SFXSlider", optionsPanel.transform, new Vector2(0, -10), 0.75f);

        CreateLabel("QualityLabel", "Calidad Gráfica", optionsPanel.transform, new Vector2(0, -90));
        mmc.qualityDropdown = CreateStyledDropdown("QualityDropdown", optionsPanel.transform, new Vector2(0, -145));

        Button backFromOpt = CreateStyledButton("BackButton", "Volver", optionsPanel.transform, new Vector2(0, -260));
        backFromOpt.onClick.AddListener(() => mmc.ShowMainPanel());
        optionsPanel.SetActive(false);

        // Credits Panel
        GameObject creditsPanel = new GameObject("CreditsPanel");
        creditsPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform cpRect = creditsPanel.AddComponent<RectTransform>();
        cpRect.anchorMin = new Vector2(0.22f, 0.15f); 
        cpRect.anchorMax = new Vector2(0.78f, 0.85f);
        cpRect.sizeDelta = Vector2.zero; 
        cpRect.anchoredPosition = Vector2.zero;
        creditsPanel.AddComponent<Image>().color = DarkPanelBg;
        mmc.creditsPanel = creditsPanel;

        CreatePanelTitle("TítuloCréditos", "Créditos", creditsPanel.transform, new Vector2(0, 280));

        GameObject credTextObj = new GameObject("CreditsText");
        credTextObj.transform.SetParent(creditsPanel.transform, false);
        TextMeshProUGUI credText = credTextObj.AddComponent<TextMeshProUGUI>();
        credText.text = "<b>Autores:</b>\nBrandon Gustavo Mendoza Amaro\nLizeth Ramírez Ramírez\n\n<b>Herramientas:</b>\nUnity, Gemini / Nano Banana, GIMP, Antigravity\n\n<b>Recursos Externos:</b>\nEfectos de sonido y música de dominio público / provisionales";
        credText.fontSize = 28; 
        credText.alignment = TextAlignmentOptions.Center;
        credText.color = new Color(0.95f, 0.92f, 0.85f);
        RectTransform ctcRect = credTextObj.GetComponent<RectTransform>();
        ctcRect.anchorMin = new Vector2(0.05f, 0.15f); 
        ctcRect.anchorMax = new Vector2(0.95f, 0.82f); 
        ctcRect.sizeDelta = Vector2.zero;

        Button backFromCred = CreateStyledButton("BackButton", "Volver", creditsPanel.transform, new Vector2(0, -260));
        backFromCred.onClick.AddListener(() => mmc.ShowMainPanel());
        creditsPanel.SetActive(false);

        // Managers
        InstantiateManagersIfMissing();

        EditorSceneManager.SaveScene(mainMenuScene, "Assets/Scenes/MainMenu.unity");
        Debug.Log("[UISetup] MainMenu generado y guardado con éxito.");
    }

    [MenuItem("Brujas/5. Escenas/Generar HUD en escena actual")]
    public static void BuildHUD()
    {
        // 1. Verificar si ya existe HUD_Canvas
        GameObject existingCanvas = GameObject.Find("HUD_Canvas");
        if (existingCanvas != null)
        {
            Object.DestroyImmediate(existingCanvas);
        }

        // Crear Canvas
        GameObject canvasObj = new GameObject("HUD_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();
        
        HUDController hud = canvasObj.AddComponent<HUDController>();

        // Timer (Top Center)
        GameObject timeObj = new GameObject("TimeText");
        timeObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI timeTxt = timeObj.AddComponent<TextMeshProUGUI>();
        timeTxt.text = "00:00"; 
        timeTxt.fontSize = 54;
        timeTxt.fontStyle = FontStyles.Bold;
        timeTxt.color = new Color(0.95f, 0.9f, 0.7f);
        timeTxt.alignment = TextAlignmentOptions.Top;
        RectTransform timeRect = timeObj.GetComponent<RectTransform>();
        timeRect.anchorMin = new Vector2(0.5f, 1f); 
        timeRect.anchorMax = new Vector2(0.5f, 1f);
        timeRect.sizeDelta = new Vector2(300, 80);
        timeRect.anchoredPosition = new Vector2(0, -40);
        hud.timeText = timeTxt;

        // Score (Top Right)
        GameObject scoreObj = new GameObject("ScoreText");
        scoreObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI scoreTxt = scoreObj.AddComponent<TextMeshProUGUI>();
        scoreTxt.text = "0000"; 
        scoreTxt.fontSize = 54;
        scoreTxt.fontStyle = FontStyles.Bold;
        scoreTxt.color = new Color(0.95f, 0.82f, 0.45f);
        scoreTxt.alignment = TextAlignmentOptions.TopRight;
        RectTransform scoreRect = scoreObj.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(1f, 1f); 
        scoreRect.anchorMax = new Vector2(1f, 1f);
        scoreRect.sizeDelta = new Vector2(350, 80);
        scoreRect.anchoredPosition = new Vector2(-60, -40);
        hud.scoreText = scoreTxt;

        // Vidas (Top Left - 3 Hearts)
        hud.heartIcons = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject heartObj = new GameObject("Heart_" + i);
            heartObj.transform.SetParent(canvasObj.transform, false);
            Image heartImg = heartObj.AddComponent<Image>();
            heartImg.color = new Color(0.9f, 0.2f, 0.2f);
            RectTransform hRect = heartObj.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0, 1); 
            hRect.anchorMax = new Vector2(0, 1);
            hRect.sizeDelta = new Vector2(50, 50);
            hRect.anchoredPosition = new Vector2(60 + (i * 65), -50);
            hud.heartIcons[i] = heartImg;
        }

        // Hechizos (Bottom Left: Cegar gris/blanco, Hipnotizar violeta, Dormir azul)
        string[] spellNames = { "Cegar", "Hipnotizar", "Dormir" };
        Color[] spellColors = { 
            new Color(0.85f, 0.85f, 0.85f), 
            new Color(0.68f, 0.25f, 0.85f), 
            new Color(0.25f, 0.55f, 0.95f) 
        };
        
        for (int i = 0; i < 3; i++)
        {
            GameObject spellObj = new GameObject("Flask_" + spellNames[i]);
            spellObj.transform.SetParent(canvasObj.transform, false);
            Image spellImg = spellObj.AddComponent<Image>();
            spellImg.color = spellColors[i];
            RectTransform sRect = spellObj.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0, 0); 
            sRect.anchorMax = new Vector2(0, 0);
            sRect.sizeDelta = new Vector2(55, 55);
            sRect.anchoredPosition = new Vector2(60 + (i * 110), 60);

            GameObject txtObj = new GameObject("Count");
            txtObj.transform.SetParent(spellObj.transform, false);
            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = "x 0"; 
            txt.fontSize = 28; 
            txt.fontStyle = FontStyles.Bold;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.BottomRight;
            RectTransform tRect = txtObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero; 
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = new Vector2(45, 0);
            tRect.anchoredPosition = new Vector2(30, -5);
            
            if (i == 0) hud.blindChargesText = txt;
            if (i == 1) hud.hypnotizeChargesText = txt;
            if (i == 2) hud.sleepChargesText = txt;
        }

        // Flight Energy Slider (Bottom Center)
        GameObject sliderObj = new GameObject("FlightSlider");
        sliderObj.transform.SetParent(canvasObj.transform, false);
        Slider slider = sliderObj.AddComponent<Slider>();
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0); 
        sliderRect.anchorMax = new Vector2(0.5f, 0);
        sliderRect.sizeDelta = new Vector2(380, 28);
        sliderRect.anchoredPosition = new Vector2(0, 50);

        GameObject bgSliderObj = new GameObject("Background");
        bgSliderObj.transform.SetParent(sliderObj.transform, false);
        Image bgImg = bgSliderObj.AddComponent<Image>(); 
        bgImg.color = new Color(0.15f, 0.1f, 0.05f, 0.85f);
        RectTransform bgRect = bgSliderObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one; bgRect.sizeDelta = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform faRect = fillArea.AddComponent<RectTransform>();
        faRect.anchorMin = Vector2.zero; faRect.anchorMax = Vector2.one; faRect.sizeDelta = Vector2.zero;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        Image fillImg = fillObj.AddComponent<Image>(); 
        fillImg.color = new Color(0.95f, 0.82f, 0.25f);
        RectTransform fRect = fillObj.GetComponent<RectTransform>();
        fRect.anchorMin = Vector2.zero; fRect.anchorMax = Vector2.one; fRect.sizeDelta = Vector2.zero;
        
        slider.fillRect = fRect;
        hud.flightEnergySlider = slider;

        // 2. Paneles de Estado (Pausa, Fin de Nivel, Game Over)
        BuildGameplayPanels(canvasObj.transform, hud);

        // 3. Asegurar Managers y CaptureListener
        InstantiateManagersIfMissing();
        EnsureCaptureListener();

        Debug.Log("[UISetup] HUD y paneles de estado generados correctamente.");
    }

    private static void BuildGameplayPanels(Transform canvasTransform, HUDController hud)
    {
        // Panel de Pausa
        GameObject pausePanelObj = new GameObject("PausePanel");
        pausePanelObj.transform.SetParent(canvasTransform, false);
        RectTransform pRect = pausePanelObj.AddComponent<RectTransform>();
        pRect.anchorMin = Vector2.zero; pRect.anchorMax = Vector2.one; pRect.sizeDelta = Vector2.zero;
        pausePanelObj.AddComponent<Image>().color = new Color(0.08f, 0.05f, 0.03f, 0.88f);
        PausePanel pausePanel = pausePanelObj.AddComponent<PausePanel>();
        hud.pausePanel = pausePanel;

        CreatePanelTitle("PauseTitle", "Pausa", pausePanelObj.transform, new Vector2(0, 220));
        pausePanel.resumeBtn = CreateStyledButton("ResumeButton", "Reanudar", pausePanelObj.transform, new Vector2(0, 110));
        pausePanel.restartBtn = CreateStyledButton("RestartButton", "Reiniciar", pausePanelObj.transform, new Vector2(0, 20));
        pausePanel.optionsBtn = CreateStyledButton("OptionsButton", "Opciones", pausePanelObj.transform, new Vector2(0, -70));
        pausePanel.menuBtn = CreateStyledButton("MenuButton", "Salir al Menú", pausePanelObj.transform, new Vector2(0, -160));

        // Subpanel opciones dentro de pausa
        GameObject subOptions = new GameObject("SubOptionsPanel");
        subOptions.transform.SetParent(pausePanelObj.transform, false);
        RectTransform soRect = subOptions.AddComponent<RectTransform>();
        soRect.anchorMin = new Vector2(0.3f, 0.2f); soRect.anchorMax = new Vector2(0.7f, 0.8f);
        soRect.sizeDelta = Vector2.zero;
        subOptions.AddComponent<Image>().color = DarkPanelBg;
        CreateLabel("SubOptTitle", "Ajustes de Sonido", subOptions.transform, new Vector2(0, 140));
        CreateLabel("SubMusicLabel", "Música", subOptions.transform, new Vector2(0, 80));
        CreateStyledSlider("SubMusicSlider", subOptions.transform, new Vector2(0, 40), 0.75f);
        CreateLabel("SubSFXLabel", "Efectos", subOptions.transform, new Vector2(0, -20));
        CreateStyledSlider("SubSFXSlider", subOptions.transform, new Vector2(0, -60), 0.75f);
        Button closeSubOpt = CreateStyledButton("CloseSubOpt", "Cerrar", subOptions.transform, new Vector2(0, -150));
        closeSubOpt.onClick.AddListener(() => subOptions.SetActive(false));
        pausePanel.optionsPanel = subOptions;
        subOptions.SetActive(false);
        pausePanelObj.SetActive(false);

        // Panel de Fin de Nivel
        GameObject winPanelObj = new GameObject("LevelCompletePanel");
        winPanelObj.transform.SetParent(canvasTransform, false);
        RectTransform wRect = winPanelObj.AddComponent<RectTransform>();
        wRect.anchorMin = Vector2.zero; wRect.anchorMax = Vector2.one; wRect.sizeDelta = Vector2.zero;
        winPanelObj.AddComponent<Image>().color = new Color(0.12f, 0.08f, 0.04f, 0.92f);
        LevelCompletePanel winPanel = winPanelObj.AddComponent<LevelCompletePanel>();
        hud.levelCompletePanel = winPanel;

        CreatePanelTitle("WinTitle", "¡Nivel completado!", winPanelObj.transform, new Vector2(0, 220));
        winPanel.scoreText = CreateLabel("ScoreText", "Puntaje: 0000", winPanelObj.transform, new Vector2(0, 110));
        winPanel.timeText = CreateLabel("TimeText", "Tiempo: 00:00", winPanelObj.transform, new Vector2(0, 40));
        winPanel.nextLevelBtn = CreateStyledButton("NextLevelBtn", "Siguiente Nivel", winPanelObj.transform, new Vector2(0, -60));
        winPanel.menuBtn = CreateStyledButton("MenuBtn", "Salir al Menú", winPanelObj.transform, new Vector2(0, -150));
        winPanelObj.SetActive(false);

        // Panel de Game Over (Hoguera)
        GameObject goPanelObj = new GameObject("GameOverPanel");
        goPanelObj.transform.SetParent(canvasTransform, false);
        RectTransform goRect = goPanelObj.AddComponent<RectTransform>();
        goRect.anchorMin = Vector2.zero; goRect.anchorMax = Vector2.one; goRect.sizeDelta = Vector2.zero;
        goPanelObj.AddComponent<Image>().color = new Color(0.18f, 0.05f, 0.03f, 0.94f);
        GameOverPanel goPanel = goPanelObj.AddComponent<GameOverPanel>();
        hud.gameOverPanel = goPanel;

        // Imagen de la hoguera
        GameObject fireObj = new GameObject("BonfireImage");
        fireObj.transform.SetParent(goPanelObj.transform, false);
        Image fireImg = fireObj.AddComponent<Image>();
        Sprite fireSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/game_over_fire.png");
        if (fireSprite != null) fireImg.sprite = fireSprite;
        RectTransform fireRect = fireObj.GetComponent<RectTransform>();
        fireRect.sizeDelta = new Vector2(280, 280);
        fireRect.anchoredPosition = new Vector2(0, 110);
        goPanel.fireImage = fireImg;

        TextMeshProUGUI goMsg = CreateLabel("GameOverMessage", "La hoguera te alcanzó…", goPanelObj.transform, new Vector2(0, -70));
        goMsg.fontSize = 46;
        goMsg.color = new Color(0.96f, 0.65f, 0.25f);
        goPanel.messageText = goMsg;

        goPanel.retryBtn = CreateStyledButton("RetryBtn", "Reintentar", goPanelObj.transform, new Vector2(-160, -190));
        goPanel.menuBtn = CreateStyledButton("MenuBtn", "Salir al Menú", goPanelObj.transform, new Vector2(160, -190));
        goPanelObj.SetActive(false);
    }

    private static void InstantiateManagersIfMissing()
    {
        if (Object.FindFirstObjectByType<GameManager>() == null)
        {
            GameObject gmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameManager.prefab");
            if (gmPrefab != null) PrefabUtility.InstantiatePrefab(gmPrefab);
        }

        if (Object.FindFirstObjectByType<AudioManager>() == null)
        {
            GameObject amPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AudioManager.prefab");
            if (amPrefab != null) PrefabUtility.InstantiatePrefab(amPrefab);
        }
    }

    private static void EnsureCaptureListener()
    {
        if (Object.FindFirstObjectByType<CaptureListener>() == null)
        {
            GameObject listenerObj = new GameObject("CaptureListener");
            listenerObj.AddComponent<CaptureListener>();
        }
    }

    private static Button CreateStyledButton(string name, string text, Transform parent, Vector2 pos)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(290, 70);
        rect.anchoredPosition = pos;

        Image img = btnObj.AddComponent<Image>();
        img.color = ParchmentWood;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = ParchmentWood;
        colors.highlightedColor = ButtonGold;
        colors.pressedColor = new Color(0.35f, 0.2f, 0.08f);
        colors.selectedColor = ButtonGold;
        btn.colors = colors;

        btnObj.AddComponent<UIButtonAnimator>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 34;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero; 
        txtRect.anchorMax = Vector2.one; 
        txtRect.sizeDelta = Vector2.zero;

        return btn;
    }

    private static TextMeshProUGUI CreateLabel(string name, string text, Transform parent, Vector2 pos)
    {
        GameObject labelObj = new GameObject(name);
        labelObj.transform.SetParent(parent, false);
        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 32;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.92f, 0.88f, 0.78f);
        RectTransform rect = labelObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(500, 50);
        rect.anchoredPosition = pos;
        return label;
    }

    private static void CreatePanelTitle(string name, string text, Transform parent, Vector2 pos)
    {
        GameObject titleObj = new GameObject(name);
        titleObj.transform.SetParent(parent, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = text;
        title.fontSize = 58;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.96f, 0.82f, 0.35f);
        RectTransform rect = titleObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(600, 80);
        rect.anchoredPosition = pos;
    }

    private static Slider CreateStyledSlider(string name, Transform parent, Vector2 pos, float defaultValue)
    {
        var resources = new DefaultControls.Resources();
        GameObject sliderObj = DefaultControls.CreateSlider(resources);
        sliderObj.name = name;
        sliderObj.transform.SetParent(parent, false);
        RectTransform rect = sliderObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(360, 30);
        rect.anchoredPosition = pos;

        Slider slider = sliderObj.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = defaultValue;

        // Custom colors
        Image fill = slider.fillRect.GetComponent<Image>();
        if (fill != null) fill.color = ButtonGold;

        return slider;
    }

    private static TMP_Dropdown CreateStyledDropdown(string name, Transform parent, Vector2 pos)
    {
        var resources = new TMP_DefaultControls.Resources();
        GameObject dropObj = TMP_DefaultControls.CreateDropdown(resources);
        dropObj.name = name;
        dropObj.transform.SetParent(parent, false);
        RectTransform rect = dropObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(320, 50);
        rect.anchoredPosition = pos;

        TMP_Dropdown dropdown = dropObj.GetComponent<TMP_Dropdown>();
        dropdown.options.Clear();
        dropdown.options.Add(new TMP_Dropdown.OptionData("Baja"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("Media"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("Alta"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("Ultra"));
        dropdown.value = QualitySettings.GetQualityLevel();

        return dropdown;
    }

    [MenuItem("Brujas/8. REPARAR TODO (Animaciones, Bruja y Gato)")]
    public static void RepararTodo()
    {
        WitchAnimationBuilder.Build();

        string prefabPath = "Assets/Prefabs/Witch.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            TransformationManager tm = prefab.GetComponent<TransformationManager>();
            if (tm == null) tm = prefab.AddComponent<TransformationManager>();

            tm.humanController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Witch/Witch.controller");
            tm.catController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Cat/Cat.controller");
            tm.obstacleMask = 1 << LayerMask.NameToLayer("Ground");
            tm.humanColliderSize = new Vector2(0.6f, 1.6f);
            tm.humanColliderOffset = new Vector2(0f, 0.8f);
            tm.catColliderSize = new Vector2(0.8f, 0.5f);
            tm.catColliderOffset = new Vector2(0f, 0.25f);
            tm.catSpeedMultiplier = 1.3f;

            if (prefab.GetComponent<WitchDetectable>() == null)
            {
                prefab.AddComponent<WitchDetectable>();
            }

            EditorUtility.SetDirty(prefab);
        }

        GameObject sceneWitch = GameObject.Find("Witch");
        if (sceneWitch != null)
        {
            TransformationManager sceneTm = sceneWitch.GetComponent<TransformationManager>();
            if (sceneTm == null) sceneTm = sceneWitch.AddComponent<TransformationManager>();

            sceneTm.humanController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Witch/Witch.controller");
            sceneTm.catController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Cat/Cat.controller");
            sceneTm.obstacleMask = 1 << LayerMask.NameToLayer("Ground");
            sceneTm.humanColliderSize = new Vector2(0.6f, 1.6f);
            sceneTm.humanColliderOffset = new Vector2(0f, 0.8f);
            sceneTm.catColliderSize = new Vector2(0.8f, 0.5f);
            sceneTm.catColliderOffset = new Vector2(0f, 0.25f);
            sceneTm.catSpeedMultiplier = 1.3f;

            Animator anim = sceneWitch.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.runtimeAnimatorController = sceneTm.humanController;
                anim.Rebind();
            }

            if (sceneWitch.GetComponent<WitchDetectable>() == null)
            {
                sceneWitch.AddComponent<WitchDetectable>();
            }

            EditorUtility.SetDirty(sceneWitch);
            EditorSceneManager.MarkSceneDirty(sceneWitch.scene);
            EditorSceneManager.SaveScene(sceneWitch.scene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(">>> [REPARACIÓN COMPLETA] Animaciones, Prefab y Bruja en escena reparados exitosamente.");
    }
}
