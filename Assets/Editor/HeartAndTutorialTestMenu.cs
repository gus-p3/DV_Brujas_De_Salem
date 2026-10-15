using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menús de prueba en Editor para corazones y colocación mínima de tutoriales,
/// checkpoints e ítems en Level1.
/// </summary>
public static class HeartAndTutorialTestMenu
{
    [MenuItem("Brujas/6. Pruebas/Quitar corazón")]
    public static void TestLoseHeart()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoseHeart(1, "Prueba");
            Debug.Log("[TestMenu] Quitar corazón invocado.");
        }
        else
        {
            Debug.LogWarning("[TestMenu] GameManager no está disponible (ejecuta en Play Mode).");
        }
    }

    [MenuItem("Brujas/6. Pruebas/Dar corazón")]
    public static void TestGainHeart()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GainHeart(1);
            Debug.Log("[TestMenu] Dar corazón invocado.");
        }
        else
        {
            Debug.LogWarning("[TestMenu] GameManager no está disponible (ejecuta en Play Mode).");
        }
    }

    [MenuItem("Brujas/3. Nivel/Configurar Tutoriales, Checkpoints y Pickups de Prueba")]
    public static void SetupTutorialAndLevelElements()
    {
        // 1. Asegurar TutorialManager en la escena
        TutorialManager tm = Object.FindFirstObjectByType<TutorialManager>();
        if (tm == null)
        {
            GameObject tmObj = new GameObject("TutorialManager");
            tm = tmObj.AddComponent<TutorialManager>();
            Debug.Log("[Setup] TutorialManager creado.");
        }

        // 2. Asegurar TutorialPanel en el Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            TutorialPanel panel = canvas.GetComponentInChildren<TutorialPanel>(true);
            if (panel == null)
            {
                panel = BuildTutorialPanel(canvas);
            }
        }

        // 3. Colocar 1 Checkpoint de prueba
        Checkpoint existingCp = Object.FindFirstObjectByType<Checkpoint>();
        if (existingCp == null)
        {
            GameObject cpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Checkpoint.prefab");
            if (cpPrefab != null)
            {
                GameObject cpInstance = (GameObject)PrefabUtility.InstantiatePrefab(cpPrefab);
                cpInstance.name = "Checkpoint_Test";
                cpInstance.transform.position = new Vector3(14f, 0.2f, 0f);
                Debug.Log("[Setup] Checkpoint_Test instanciado en x=14.");
            }
        }

        // 4. Colocar 2 HeartPickup de prueba
        HeartPickup[] existingPickups = Object.FindObjectsByType<HeartPickup>(FindObjectsSortMode.None);
        if (existingPickups.Length < 2)
        {
            GameObject heartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HeartPickup.prefab");
            if (heartPrefab != null)
            {
                if (existingPickups.Length == 0)
                {
                    GameObject hp1 = (GameObject)PrefabUtility.InstantiatePrefab(heartPrefab);
                    hp1.name = "HeartPickup_Test1";
                    hp1.transform.position = new Vector3(8f, 1.2f, 0f);

                    GameObject hp2 = (GameObject)PrefabUtility.InstantiatePrefab(heartPrefab);
                    hp2.name = "HeartPickup_Test2";
                    hp2.transform.position = new Vector3(20f, 1.5f, 0f);
                }
                else if (existingPickups.Length == 1)
                {
                    GameObject hp2 = (GameObject)PrefabUtility.InstantiatePrefab(heartPrefab);
                    hp2.name = "HeartPickup_Test2";
                    hp2.transform.position = new Vector3(20f, 1.5f, 0f);
                }
                Debug.Log("[Setup] HeartPickups de prueba colocados en el nivel.");
            }
        }

        // 5. Colocar dos TutorialTrigger en el inicio
        TutorialTrigger[] triggers = Object.FindObjectsByType<TutorialTrigger>(FindObjectsSortMode.None);
        if (triggers.Length == 0)
        {
            // Trigger 1: tip_test_a (Movimiento y Salto)
            GameObject t1 = new GameObject("TutorialTrigger_TestA");
            t1.transform.position = new Vector3(1.5f, 1.0f, 0f);
            BoxCollider2D c1 = t1.AddComponent<BoxCollider2D>();
            c1.isTrigger = true;
            c1.size = new Vector2(2f, 4f);
            TutorialTrigger tt1 = t1.AddComponent<TutorialTrigger>();
            SetTriggerFields(tt1, "tip_test_a", "Movimiento", "Usa {Mover} para explorar la aldea y {Saltar} para brincar sobre obstáculos.");

            // Trigger 2: tip_corazones
            GameObject t2 = new GameObject("TutorialTrigger_Corazones");
            t2.transform.position = new Vector3(5.5f, 1.0f, 0f);
            BoxCollider2D c2 = t2.AddComponent<BoxCollider2D>();
            c2.isTrigger = true;
            c2.size = new Vector2(2f, 4f);
            TutorialTrigger tt2 = t2.AddComponent<TutorialTrigger>();
            SetTriggerFields(tt2, "tip_corazones", "Corazones", "Un guardia o una caída te quitan un corazón. Si pierdes los tres, el nivel se reinicia. Busca corazones por el camino para recuperarlos.");

            Debug.Log("[Setup] TutorialTriggers de prueba creados en x=1.5 y x=5.5.");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
    }

    private static void SetTriggerFields(TutorialTrigger trigger, string id, string title, string body)
    {
        var so = new SerializedObject(trigger);
        so.FindProperty("tipId").stringValue = id;
        so.FindProperty("tipTitle").stringValue = title;
        so.FindProperty("tipBody").stringValue = body;
        so.FindProperty("oneShot").boolValue = true;
        so.ApplyModifiedProperties();
    }

    private static TutorialPanel BuildTutorialPanel(Canvas canvas)
    {
        GameObject panelObj = new GameObject("TutorialPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        Image bgDim = panelObj.GetComponent<Image>();
        bgDim.color = new Color(0f, 0f, 0f, 0.65f); // Fondo oscurecido

        // Cuadro de pergamino central
        GameObject dialogObj = new GameObject("DialogBox", typeof(RectTransform), typeof(Image));
        dialogObj.transform.SetParent(panelObj.transform, false);
        RectTransform dialogRt = dialogObj.GetComponent<RectTransform>();
        dialogRt.sizeDelta = new Vector2(560f, 280f);
        dialogRt.anchoredPosition = Vector2.zero;

        Image dialogImg = dialogObj.GetComponent<Image>();
        dialogImg.color = new Color(0.95f, 0.90f, 0.78f, 0.98f); // Tono pergamino

        // Borde oscuro del pergamino
        Outline outline = dialogObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.22f, 0.14f, 0.08f, 1f);
        outline.effectDistance = new Vector2(4f, -4f);

        // Título
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(dialogObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -20f);
        titleRt.sizeDelta = new Vector2(-40f, 40f);

        TextMeshProUGUI titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
        titleTMP.fontSize = 26;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = new Color(0.25f, 0.12f, 0.05f, 1f);
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.text = "Título";

        // Cuerpo
        GameObject bodyObj = new GameObject("BodyText", typeof(RectTransform), typeof(TextMeshProUGUI));
        bodyObj.transform.SetParent(dialogObj.transform, false);
        RectTransform bodyRt = bodyObj.GetComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0f, 0.25f);
        bodyRt.anchorMax = new Vector2(1f, 0.85f);
        bodyRt.pivot = new Vector2(0.5f, 0.5f);
        bodyRt.anchoredPosition = Vector2.zero;
        bodyRt.sizeDelta = new Vector2(-50f, 0f);

        TextMeshProUGUI bodyTMP = bodyObj.GetComponent<TextMeshProUGUI>();
        bodyTMP.fontSize = 18;
        bodyTMP.color = new Color(0.18f, 0.12f, 0.08f, 1f);
        bodyTMP.alignment = TextAlignmentOptions.Center;
        bodyTMP.enableWordWrapping = true;
        bodyTMP.text = "Cuerpo del tutorial...";

        // Pie: Continuar
        GameObject promptObj = new GameObject("ContinuePromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
        promptObj.transform.SetParent(dialogObj.transform, false);
        RectTransform promptRt = promptObj.GetComponent<RectTransform>();
        promptRt.anchorMin = new Vector2(0f, 0f);
        promptRt.anchorMax = new Vector2(1f, 0f);
        promptRt.pivot = new Vector2(0.5f, 0f);
        promptRt.anchoredPosition = new Vector2(0f, 16f);
        promptRt.sizeDelta = new Vector2(-40f, 30f);

        TextMeshProUGUI promptTMP = promptObj.GetComponent<TextMeshProUGUI>();
        promptTMP.fontSize = 15;
        promptTMP.fontStyle = FontStyles.Italic;
        promptTMP.color = new Color(0.45f, 0.25f, 0.15f, 1f);
        promptTMP.alignment = TextAlignmentOptions.Center;
        promptTMP.text = "Continuar: Enter o clic";

        TutorialPanel panelComp = panelObj.AddComponent<TutorialPanel>();

        var so = new SerializedObject(panelComp);
        so.FindProperty("titleText").objectReferenceValue = titleTMP;
        so.FindProperty("bodyText").objectReferenceValue = bodyTMP;
        so.FindProperty("continuePromptText").objectReferenceValue = promptTMP;
        so.FindProperty("canvasGroup").objectReferenceValue = panelObj.GetComponent<CanvasGroup>();
        so.FindProperty("contentContainer").objectReferenceValue = dialogRt;
        so.ApplyModifiedProperties();

        panelObj.SetActive(false);
        return panelComp;
    }
}
