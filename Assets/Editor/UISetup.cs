using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class UISetup
{
    [MenuItem("Brujas/5. Escenas/Generar Escena MainMenu")]
    public static void BuildMainMenu()
    {
        Scene mainMenuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        GameObject cameraObj = new GameObject("Main Camera");
        Camera cam = cameraObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();
        
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/main_menu_bg.png");
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Brujas de Salem";
        titleText.fontSize = 120;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.9f, 0.7f, 0.2f);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.8f);
        titleRect.anchorMax = new Vector2(0.5f, 0.9f);
        titleRect.sizeDelta = new Vector2(1000, 200);
        titleRect.anchoredPosition = Vector2.zero;

        MainMenuController mmc = canvasObj.AddComponent<MainMenuController>();
        
        GameObject mainPanel = new GameObject("MainPanel");
        mainPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform mpRect = mainPanel.AddComponent<RectTransform>();
        mpRect.anchorMin = new Vector2(0.5f, 0.2f); mpRect.anchorMax = new Vector2(0.5f, 0.7f);
        mpRect.sizeDelta = new Vector2(400, 0); mpRect.anchoredPosition = Vector2.zero;
        
        mmc.mainPanel = mainPanel;
        
        mmc.playButton = CreateButton("PlayButton", "Jugar", mainPanel.transform, new Vector2(0, 150));
        mmc.continueButton = CreateButton("ContinueButton", "Continuar", mainPanel.transform, new Vector2(0, 50));
        mmc.optionsButton = CreateButton("OptionsButton", "Opciones", mainPanel.transform, new Vector2(0, -50));
        mmc.creditsButton = CreateButton("CreditsButton", "Créditos", mainPanel.transform, new Vector2(0, -150));
        mmc.exitButton = CreateButton("ExitButton", "Salir", mainPanel.transform, new Vector2(0, -250));

        GameObject optionsPanel = new GameObject("OptionsPanel");
        optionsPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform opRect = optionsPanel.AddComponent<RectTransform>();
        opRect.anchorMin = new Vector2(0.3f, 0.2f); opRect.anchorMax = new Vector2(0.7f, 0.8f);
        opRect.sizeDelta = Vector2.zero; opRect.anchoredPosition = Vector2.zero;
        optionsPanel.AddComponent<Image>().color = new Color(0.2f, 0.1f, 0.05f, 0.9f); 
        mmc.optionsPanel = optionsPanel;
        optionsPanel.SetActive(false);
        Button backFromOpt = CreateButton("BackButton", "Volver", optionsPanel.transform, new Vector2(0, -250));
        backFromOpt.onClick.AddListener(() => mmc.ShowMainPanel());

        GameObject creditsPanel = new GameObject("CreditsPanel");
        creditsPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform cpRect = creditsPanel.AddComponent<RectTransform>();
        cpRect.anchorMin = new Vector2(0.3f, 0.2f); cpRect.anchorMax = new Vector2(0.7f, 0.8f);
        cpRect.sizeDelta = Vector2.zero; cpRect.anchoredPosition = Vector2.zero;
        creditsPanel.AddComponent<Image>().color = new Color(0.2f, 0.1f, 0.05f, 0.9f);
        GameObject credTextObj = new GameObject("CreditsText");
        credTextObj.transform.SetParent(creditsPanel.transform, false);
        TextMeshProUGUI credText = credTextObj.AddComponent<TextMeshProUGUI>();
        credText.text = "Autores: Brandon Gustavo Mendoza Amaro, Lizeth Ramírez Ramírez\nHerramientas: Unity, Gemini/Nano Banana, GIMP, Antigravity";
        credText.fontSize = 36; credText.alignment = TextAlignmentOptions.Center;
        RectTransform ctcRect = credTextObj.GetComponent<RectTransform>();
        ctcRect.anchorMin = Vector2.zero; ctcRect.anchorMax = Vector2.one; ctcRect.sizeDelta = Vector2.zero;
        mmc.creditsPanel = creditsPanel;
        creditsPanel.SetActive(false);
        Button backFromCred = CreateButton("BackButton", "Volver", creditsPanel.transform, new Vector2(0, -250));
        backFromCred.onClick.AddListener(() => mmc.ShowMainPanel());

        EditorSceneManager.SaveScene(mainMenuScene, "Assets/Scenes/MainMenu.unity");
        Debug.Log("MainMenu creado con éxito.");
    }

    [MenuItem("Brujas/6. Core/Crear Prefabs Managers")]
    public static void BuildManagers()
    {
        GameObject gmObj = new GameObject("GameManager");
        gmObj.AddComponent<GameManager>();
        PrefabUtility.SaveAsPrefabAsset(gmObj, "Assets/Prefabs/GameManager.prefab");
        Object.DestroyImmediate(gmObj);

        GameObject amObj = new GameObject("AudioManager");
        amObj.AddComponent<AudioManager>();
        PrefabUtility.SaveAsPrefabAsset(amObj, "Assets/Prefabs/AudioManager.prefab");
        Object.DestroyImmediate(amObj);

        Debug.Log("Prefabs de GameManager y AudioManager creados.");
    }

    private static Button CreateButton(string name, string text, Transform parent, Vector2 pos)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 80);
        rect.anchoredPosition = pos;
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.6f, 0.4f, 0.2f);
        Button btn = btnObj.AddComponent<Button>();
        btnObj.AddComponent<UIButtonAnimator>();
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 40;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one; txtRect.sizeDelta = Vector2.zero;
        return btn;
    }

    [MenuItem("Brujas/5. Escenas/Generar HUD en escena actual")]
    public static void BuildHUD()
    {
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

        // Timer
        GameObject timeObj = new GameObject("TimeText");
        timeObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI timeTxt = timeObj.AddComponent<TextMeshProUGUI>();
        timeTxt.text = "00:00"; timeTxt.fontSize = 60;
        timeTxt.alignment = TextAlignmentOptions.Top;
        RectTransform timeRect = timeObj.GetComponent<RectTransform>();
        timeRect.anchorMin = new Vector2(0.5f, 1f); timeRect.anchorMax = new Vector2(0.5f, 1f);
        timeRect.anchoredPosition = new Vector2(0, -50);
        hud.timeText = timeTxt;

        // Score
        GameObject scoreObj = new GameObject("ScoreText");
        scoreObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI scoreTxt = scoreObj.AddComponent<TextMeshProUGUI>();
        scoreTxt.text = "0000"; scoreTxt.fontSize = 60;
        scoreTxt.alignment = TextAlignmentOptions.TopRight;
        RectTransform scoreRect = scoreObj.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(1f, 1f); scoreRect.anchorMax = new Vector2(1f, 1f);
        scoreRect.anchoredPosition = new Vector2(-50, -50);
        hud.scoreText = scoreTxt;

        // Vidas
        hud.heartIcons = new Image[3];
        for (int i=0; i<3; i++)
        {
            GameObject heartObj = new GameObject("Heart_" + i);
            heartObj.transform.SetParent(canvasObj.transform, false);
            Image heartImg = heartObj.AddComponent<Image>();
            heartImg.color = Color.red; // Placeholder for heart
            RectTransform hRect = heartObj.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0, 1); hRect.anchorMax = new Vector2(0, 1);
            hRect.sizeDelta = new Vector2(60, 60);
            hRect.anchoredPosition = new Vector2(50 + (i * 70), -50);
            hud.heartIcons[i] = heartImg;
        }

        // Hechizos (Esquina inferior izquierda)
        string[] spellNames = { "Cegar", "Hipnotizar", "Dormir" };
        Color[] spellColors = { Color.gray, new Color(0.5f, 0f, 0.5f), Color.blue };
        
        for (int i=0; i<3; i++)
        {
            GameObject spellObj = new GameObject("Spell_" + spellNames[i]);
            spellObj.transform.SetParent(canvasObj.transform, false);
            Image spellImg = spellObj.AddComponent<Image>();
            spellImg.color = spellColors[i]; // Placeholder for potion
            RectTransform sRect = spellObj.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0, 0); sRect.anchorMax = new Vector2(0, 0);
            sRect.sizeDelta = new Vector2(80, 80);
            sRect.anchoredPosition = new Vector2(60 + (i * 120), 60);

            GameObject txtObj = new GameObject("Count");
            txtObj.transform.SetParent(spellObj.transform, false);
            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = "x 0"; txt.fontSize = 30; txt.alignment = TextAlignmentOptions.BottomRight;
            RectTransform tRect = txtObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one;
            tRect.anchoredPosition = new Vector2(20, -20);
            
            if (i==0) hud.blindChargesText = txt;
            if (i==1) hud.hypnotizeChargesText = txt;
            if (i==2) hud.sleepChargesText = txt;
        }

        // Flight Energy
        GameObject sliderObj = new GameObject("FlightSlider");
        sliderObj.transform.SetParent(canvasObj.transform, false);
        Slider slider = sliderObj.AddComponent<Slider>();
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0); sliderRect.anchorMax = new Vector2(0.5f, 0);
        sliderRect.sizeDelta = new Vector2(400, 40);
        sliderRect.anchoredPosition = new Vector2(0, 50);

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>(); bgImg.color = Color.black;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one; bgRect.sizeDelta = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform faRect = fillArea.AddComponent<RectTransform>();
        faRect.anchorMin = Vector2.zero; faRect.anchorMax = Vector2.one; faRect.sizeDelta = Vector2.zero;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        Image fillImg = fillObj.AddComponent<Image>(); fillImg.color = Color.yellow;
        RectTransform fRect = fillObj.GetComponent<RectTransform>();
        fRect.anchorMin = Vector2.zero; fRect.anchorMax = Vector2.one; fRect.sizeDelta = Vector2.zero;
        
        slider.fillRect = fRect;
        hud.flightEnergySlider = slider;

        Debug.Log("HUD generado en la escena actual.");
    }

    [MenuItem("Brujas/7. Player/Configurar Prefab Bruja (Gato)")]
    public static void SetupWitchPrefab()
    {
        string prefabPath = "Assets/Prefabs/Witch.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            if (prefab.GetComponent<TransformationManager>() == null)
            {
                TransformationManager tm = prefab.AddComponent<TransformationManager>();
                tm.humanController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Witch/Witch.controller");
                tm.catController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Cat/Cat.controller");
                tm.obstacleMask = LayerMask.GetMask("Default", "Ground");
            }
            if (prefab.GetComponent<WitchDetectable>() == null)
            {
                prefab.AddComponent<WitchDetectable>();
            }
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            Debug.Log("Prefab de Bruja actualizado exitosamente con los componentes del Gato.");
        }
        else
        {
            Debug.LogError("No se encontró el prefab en " + prefabPath);
        }
    }
}
