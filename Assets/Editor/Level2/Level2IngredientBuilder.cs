using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Level2;

public static class Level2IngredientBuilder
{
    private const string ScenePath = "Assets/Scenes/Level2.unity";
    private const string PrefabsDir = "Assets/Prefabs/Level2";
    private const string ArtDir = "Assets/Art/Environment/Level2";

    [MenuItem("Brujas/Nivel 2/Construir Ingredientes, Portón y HUD")]
    public static void BuildIngredientsAndHUD()
    {
        Debug.Log("[Level2IngredientBuilder] Iniciando construcción de Ingredientes, Portón y HUD del Nivel 2...");

        // 1. Abrir la escena Level2
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[Level2IngredientBuilder] No se pudo abrir la escena en {ScenePath}");
            return;
        }

        // 2. Cargar sprites necesarios
        Sprite mushroomSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_Ingredient_Mushroom.png");
        Sprite spiderSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_Ingredient_Spider.png");
        Sprite flowerSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_Ingredient_WitheredFlower.png");
        Sprite rootSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_Ingredient_Root.png");
        Sprite gateClosedSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_IngredientGate_Closed.png");
        Sprite gateOpenSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_IngredientGate_Open.png");

        if (mushroomSprite == null || spiderSprite == null || flowerSprite == null || rootSprite == null)
        {
            AssetDatabase.Refresh();
            mushroomSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_Ingredient_Mushroom.png");
            spiderSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_Ingredient_Spider.png");
            flowerSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_Ingredient_WitheredFlower.png");
            rootSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_Ingredient_Root.png");
            gateClosedSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_IngredientGate_Closed.png");
            gateOpenSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_IngredientGate_Open.png");
        }

        // 3. Crear/Actualizar Prefabs de Ingredientes
        Directory.CreateDirectory(PrefabsDir);
        GameObject mushroomPrefab = CreateOrUpdateIngredientPrefab("Ingredient_Mushroom", IngredientType.Mushroom, mushroomSprite);
        GameObject spiderPrefab = CreateOrUpdateIngredientPrefab("Ingredient_Spider", IngredientType.Spider, spiderSprite);
        GameObject flowerPrefab = CreateOrUpdateIngredientPrefab("Ingredient_WitheredFlower", IngredientType.WitheredFlower, flowerSprite);
        GameObject rootPrefab = CreateOrUpdateIngredientPrefab("Ingredient_Root", IngredientType.Root, rootSprite);

        // 4. Configurar IngredientInventory en Managers
        GameObject managers = GameObject.Find("Managers");
        if (managers == null) managers = new GameObject("Managers");

        IngredientInventory inv = managers.GetComponentInChildren<IngredientInventory>();
        if (inv == null)
        {
            GameObject invObj = new GameObject("IngredientInventory");
            invObj.transform.SetParent(managers.transform);
            inv = invObj.AddComponent<IngredientInventory>();
        }
        inv.RequiredMushrooms = 2;
        inv.RequiredSpiders = 2;
        inv.RequiredFlowers = 2;
        inv.RequiredRoots = 2;

        // 5. Instanciar los 10 Ingredientes en Gameplay/Ingredients
        GameObject gameplay = GameObject.Find("Gameplay");
        if (gameplay == null) gameplay = new GameObject("Gameplay");

        Transform oldIngredients = gameplay.transform.Find("Ingredients");
        if (oldIngredients != null)
        {
            Object.DestroyImmediate(oldIngredients.gameObject);
        }

        GameObject ingredientsRoot = new GameObject("Ingredients");
        ingredientsRoot.transform.SetParent(gameplay.transform);

        // Ubicación de los 10 ingredientes:
        // 3 Hongos: Z1 (intro), Z5 (plaza), Z5 (bonus viga alta)
        SpawnIngredientInstance(mushroomPrefab, ingredientsRoot.transform, "Ingredient_Mushroom_Z1", IngredientType.Mushroom, new Vector3(18.5f, 0.4f, 0f));
        SpawnIngredientInstance(mushroomPrefab, ingredientsRoot.transform, "Ingredient_Mushroom_Z5", IngredientType.Mushroom, new Vector3(134.0f, 0.4f, 0f));
        SpawnIngredientInstance(mushroomPrefab, ingredientsRoot.transform, "Ingredient_Mushroom_Z5_Bonus", IngredientType.Mushroom, new Vector3(142.5f, 6.7f, 0f));

        // 3 Arañas: Z2 (toldo alto guardia), Z3 (puesto mercado), Z5 (sombra bonus)
        SpawnIngredientInstance(spiderPrefab, ingredientsRoot.transform, "Ingredient_Spider_Z2", IngredientType.Spider, new Vector3(40.0f, 2.5f, 0f));
        SpawnIngredientInstance(spiderPrefab, ingredientsRoot.transform, "Ingredient_Spider_Z3", IngredientType.Spider, new Vector3(68.0f, 0.4f, 0f));
        SpawnIngredientInstance(spiderPrefab, ingredientsRoot.transform, "Ingredient_Spider_Z5_Bonus", IngredientType.Spider, new Vector3(150.0f, 1.0f, 0f));

        // 2 Flores: Z2 (cajas mercado), Z4 (callejón guardia)
        SpawnIngredientInstance(flowerPrefab, ingredientsRoot.transform, "Ingredient_Flower_Z2", IngredientType.WitheredFlower, new Vector3(49.0f, 1.5f, 0f));
        SpawnIngredientInstance(flowerPrefab, ingredientsRoot.transform, "Ingredient_Flower_Z4", IngredientType.WitheredFlower, new Vector3(102.0f, 0.4f, 0f));

        // 2 Raíces: Z3 (viga canal), Z4 (pasaje gato: requiere cruzar como gato y transformar en humano)
        SpawnIngredientInstance(rootPrefab, ingredientsRoot.transform, "Ingredient_Root_Z3", IngredientType.Root, new Vector3(86.0f, 2.8f, 0f));
        SpawnIngredientInstance(rootPrefab, ingredientsRoot.transform, "Ingredient_Root_Z4", IngredientType.Root, new Vector3(112.5f, 0.35f, 0f));

        // 6. Configurar IngredientGate
        Transform oldGate = gameplay.transform.Find("IngredientGate");
        if (oldGate != null) Object.DestroyImmediate(oldGate.gameObject);

        GameObject gateObj = new GameObject("IngredientGate");
        gateObj.transform.SetParent(gameplay.transform);
        gateObj.transform.position = new Vector3(181.5f, 2.0f, 0f);

        SpriteRenderer gateSr = gateObj.AddComponent<SpriteRenderer>();
        gateSr.sprite = gateClosedSprite;
        gateSr.sortingLayerName = "Midground";
        gateSr.sortingOrder = 12;

        // Blocker físico
        GameObject blockerObj = new GameObject("SolidBlocker");
        blockerObj.transform.SetParent(gateObj.transform);
        blockerObj.transform.localPosition = Vector3.zero;
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0) blockerObj.layer = groundLayer;
        BoxCollider2D blockerCol = blockerObj.AddComponent<BoxCollider2D>();
        blockerCol.size = new Vector2(0.8f, 3.8f);

        // Warning trigger frontal
        GameObject warningObj = new GameObject("WarningTrigger");
        warningObj.transform.SetParent(gateObj.transform);
        warningObj.transform.localPosition = new Vector3(-1.0f, 0f, 0f);
        BoxCollider2D warningCol = warningObj.AddComponent<BoxCollider2D>();
        warningCol.isTrigger = true;
        warningCol.size = new Vector2(1.8f, 3.8f);

        IngredientGate gateComp = gateObj.AddComponent<IngredientGate>();
        gateComp.SetupComponents(blockerCol, warningCol, gateSr, gateClosedSprite, gateOpenSprite);

        // 7. Configurar Tutorial contextual de ingredientes en Zona 1
        Transform oldTutorials = gameplay.transform.Find("Tutorials");
        if (oldTutorials == null)
        {
            GameObject tutRoot = new GameObject("Tutorials");
            tutRoot.transform.SetParent(gameplay.transform);
            oldTutorials = tutRoot.transform;
        }

        Transform oldTutIng = oldTutorials.Find("TutorialTrigger_Ingredients");
        if (oldTutIng != null) Object.DestroyImmediate(oldTutIng.gameObject);

        GameObject tutObj = new GameObject("TutorialTrigger_Ingredients");
        tutObj.transform.SetParent(oldTutorials);
        tutObj.transform.position = new Vector3(16.0f, 0.5f, 0f);

        BoxCollider2D tutCol = tutObj.AddComponent<BoxCollider2D>();
        tutCol.isTrigger = true;
        tutCol.size = new Vector2(2.5f, 3.0f);

        TutorialTrigger tutComp = tutObj.AddComponent<TutorialTrigger>();
        var tutSo = new SerializedObject(tutComp);
        tutSo.FindProperty("tipId").stringValue = "tip_ingredientes_mercado";
        tutSo.FindProperty("tipTitle").stringValue = "Ingredientes del Mercado";
        tutSo.FindProperty("tipBody").stringValue = "Reúne hongos, arañas, flores marchitas y raíces entre los puestos del mercado para preparar los hechizos del Acto 3. ¡Recuerda que solo puedes recolectarlos en forma humana!";
        tutSo.FindProperty("tipIcon").objectReferenceValue = mushroomSprite;
        tutSo.FindProperty("oneShot").boolValue = true;
        tutSo.ApplyModifiedProperties();

        // 8. Construir UI IngredientHUD Canvas
        BuildIngredientHUDCanvas(mushroomSprite, spiderSprite, flowerSprite, rootSprite);

        // Guardar escena
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[Level2IngredientBuilder] ¡Ingredientes, Portón y HUD construidos exitosamente en Level2.unity!");
    }

    private static GameObject CreateOrUpdateIngredientPrefab(string name, IngredientType type, Sprite sprite)
    {
        string path = $"{PrefabsDir}/{name}.prefab";
        GameObject tempObj = new GameObject(name);

        SpriteRenderer sr = tempObj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Foreground";
        sr.sortingOrder = 10;

        CircleCollider2D col = tempObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.35f;

        Ingredient ing = tempObj.AddComponent<Ingredient>();
        ing.Configure($"ing_{type.ToString().ToLower()}_default", type, 150);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tempObj, path);
        Object.DestroyImmediate(tempObj);

        return prefab;
    }

    private static void SpawnIngredientInstance(GameObject prefab, Transform parent, string id, IngredientType type, Vector3 position)
    {
        GameObject inst = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (inst == null) inst = Object.Instantiate(prefab, parent);

        inst.name = id;
        inst.transform.position = position;

        Ingredient ing = inst.GetComponent<Ingredient>();
        if (ing != null)
        {
            ing.Configure(id, type, 150);
        }
    }

    private static void BuildIngredientHUDCanvas(Sprite mSprite, Sprite sSprite, Sprite fSprite, Sprite rSprite)
    {
        GameObject uiRoot = GameObject.Find("UI");
        if (uiRoot == null) uiRoot = new GameObject("UI");

        Transform oldCanvas = uiRoot.transform.Find("IngredientHUD_Canvas");
        if (oldCanvas != null) Object.DestroyImmediate(oldCanvas.gameObject);

        GameObject canvasObj = new GameObject("IngredientHUD_Canvas");
        canvasObj.transform.SetParent(uiRoot.transform);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Barra de Ingredientes (Top-Right)
        GameObject barObj = new GameObject("Panel_IngredientsBar");
        barObj.transform.SetParent(canvasObj.transform, false);

        RectTransform barRt = barObj.AddComponent<RectTransform>();
        barRt.anchorMin = new Vector2(1f, 1f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.pivot = new Vector2(1f, 1f);
        barRt.anchoredPosition = new Vector2(-25f, -20f);
        barRt.sizeDelta = new Vector2(360f, 50f);

        Image barBg = barObj.AddComponent<Image>();
        barBg.color = new Color(0.12f, 0.08f, 0.06f, 0.85f);

        HorizontalLayoutGroup layout = barObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.padding = new RectOffset(10, 10, 6, 6);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        // Slots
        var (mText, mIcon) = CreateIngredientSlot(barObj.transform, "Slot_Mushroom", mSprite);
        var (sText, sIcon) = CreateIngredientSlot(barObj.transform, "Slot_Spider", sSprite);
        var (fText, fIcon) = CreateIngredientSlot(barObj.transform, "Slot_Flower", fSprite);
        var (rText, rIcon) = CreateIngredientSlot(barObj.transform, "Slot_Root", rSprite);

        // Banner de Mensaje Propio
        GameObject bannerPanel = new GameObject("Panel_WarningBanner");
        bannerPanel.transform.SetParent(canvasObj.transform, false);

        RectTransform bannerRt = bannerPanel.AddComponent<RectTransform>();
        bannerRt.anchorMin = new Vector2(0.5f, 1f);
        bannerRt.anchorMax = new Vector2(0.5f, 1f);
        bannerRt.pivot = new Vector2(0.5f, 1f);
        bannerRt.anchoredPosition = new Vector2(0f, -115f);
        bannerRt.sizeDelta = new Vector2(560f, 44f);

        Image bannerBg = bannerPanel.AddComponent<Image>();
        bannerBg.color = new Color(0.20f, 0.06f, 0.06f, 0.90f);

        GameObject bannerTextObj = new GameObject("BannerText");
        bannerTextObj.transform.SetParent(bannerPanel.transform, false);

        RectTransform textRt = bannerTextObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(12f, 4f);
        textRt.offsetMax = new Vector2(-12f, -4f);

        TextMeshProUGUI bTmp = bannerTextObj.AddComponent<TextMeshProUGUI>();
        bTmp.fontSize = 20;
        bTmp.alignment = TextAlignmentOptions.Center;
        bTmp.color = new Color(1f, 0.94f, 0.65f, 1f);
        bTmp.text = "";

        // Componente IngredientHUD
        IngredientHUD hudComp = canvasObj.AddComponent<IngredientHUD>();
        hudComp.SetupReferences(mText, sText, fText, rText, mIcon, sIcon, fIcon, rIcon, bTmp, bannerPanel);
    }

    private static (TextMeshProUGUI, RectTransform) CreateIngredientSlot(Transform parent, string slotName, Sprite iconSprite)
    {
        GameObject slotObj = new GameObject(slotName);
        slotObj.transform.SetParent(parent, false);

        RectTransform slotRt = slotObj.AddComponent<RectTransform>();
        slotRt.sizeDelta = new Vector2(76f, 38f);

        // Icono
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slotObj.transform, false);

        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0f, 0.5f);
        iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0f, 0.5f);
        iconRt.anchoredPosition = new Vector2(2f, 0f);
        iconRt.sizeDelta = new Vector2(30f, 30f);

        Image img = iconObj.AddComponent<Image>();
        img.sprite = iconSprite;
        img.preserveAspect = true;

        // Texto
        GameObject textObj = new GameObject("CountText");
        textObj.transform.SetParent(slotObj.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(1f, 0.5f);
        textRt.anchorMax = new Vector2(1f, 0.5f);
        textRt.pivot = new Vector2(1f, 0.5f);
        textRt.anchoredPosition = new Vector2(-2f, 0f);
        textRt.sizeDelta = new Vector2(40f, 26f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 17;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Right;
        tmp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        tmp.text = "0/2";

        return (tmp, iconRt);
    }
}
