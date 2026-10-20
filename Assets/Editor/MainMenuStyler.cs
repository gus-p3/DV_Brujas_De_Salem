using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Rediseña el menú principal (Tools > Menú > Maquetar menú principal), con Hollow Knight como referencia:
/// fondo con viñeta, título grande con subtítulo y adornos, botones solo de texto con adornos al
/// seleccionarlos, luciérnagas y hojas, un gato animado junto al camino y paneles de Opciones y Créditos
/// a juego. Los cambios quedan guardados en MainMenu.unity.
/// </summary>
public static class MainMenuStyler
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string UiDir = "Assets/Art/UI";
    private static readonly Color Cream = new Color(0.95f, 0.9f, 0.8f, 1f);
    private static readonly Color Gold = new Color(1f, 0.82f, 0.42f, 1f);
    private static readonly Color FieldColor = new Color(0.16f, 0.11f, 0.1f, 1f);
    private static Sprite panelFrame;

    [MenuItem("Tools/Menú/Maquetar menú principal")]
    public static void Apply()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath);

        Sprite vignette = SaveSprite("menu_vignette", 256, 256, VignettePixel);
        Sprite ornament = SaveSprite("menu_ornament", 256, 32, OrnamentPixel);
        Sprite softOval = SaveSprite("menu_soft_oval", 256, 256, SoftOvalPixel);
        panelFrame = SaveSprite("menu_panel_frame", 64, 64, PanelFramePixel, new Vector4(12f, 12f, 12f, 12f));

        GameObject canvasGo = GameObject.Find("Canvas");
        Transform canvas = canvasGo.transform;
        MainMenuController controller = canvasGo.GetComponent<MainMenuController>();

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        // Fondo (se agranda un poco para que el paneo lento no muestre bordes) y capas encima
        RectTransform bg = (RectTransform)canvas.Find("Background");
        Stretch(bg, -60f);
        bg.SetSiblingIndex(0);
        // Se apaga el fondo para que el título y los botones dominen (el arte de otoño es muy brillante)
        Image bgImg = bg.GetComponent<Image>();
        if (bgImg != null) bgImg.color = new Color(0.58f, 0.52f, 0.56f, 1f);
        Image vignetteImg = Layer(canvas, "Vignette", 1, vignette, new Color(1f, 1f, 1f, 1f));
        vignetteImg.raycastTarget = false;
        RectTransform particles = Layer(canvas, "MenuParticles", 2, null, Color.clear).rectTransform;
        Object.DestroyImmediate(particles.GetComponent<Image>());

        // Título
        TMP_Text oldTitle = canvas.Find("TitleText") != null ? canvas.Find("TitleText").GetComponent<TMP_Text>() : null;
        RectTransform group = Child(canvas, "TitleGroup");
        group.SetSiblingIndex(3);
        Anchor(group, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1400f, 260f));
        if (oldTitle != null)
        {
            oldTitle.transform.SetParent(group, false);
            Anchor(oldTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1400f, 170f));
            oldTitle.text = "Brujas de Salem";
            oldTitle.fontSize = 140f;
            oldTitle.fontStyle = FontStyles.Bold;
            oldTitle.characterSpacing = 4f;
            oldTitle.alignment = TextAlignmentOptions.Center;
            oldTitle.enableVertexGradient = true;
            oldTitle.colorGradient = new VertexGradient(new Color(1f, 0.93f, 0.72f), new Color(1f, 0.93f, 0.72f), new Color(0.95f, 0.66f, 0.3f), new Color(0.95f, 0.66f, 0.3f));
            oldTitle.color = Color.white;
            ApplyOutline(oldTitle, new Color(0.12f, 0.06f, 0.03f, 1f), 0.22f);
        }
        TMP_Text subtitle = Text(group, "Subtitle", "SALEM  ·  1692", 30f, Cream);
        subtitle.characterSpacing = 22f;
        Anchor(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -178f), new Vector2(700f, 44f));
        Image ornLeft = Layer(group, "OrnamentLeft", -1, ornament, Gold);
        Anchor(ornLeft.rectTransform, new Vector2(0.5f, 1f), new Vector2(1f, 0.5f), new Vector2(-230f, -200f), new Vector2(260f, 32f));
        ornLeft.rectTransform.localScale = Vector3.one;
        Image ornRight = Layer(group, "OrnamentRight", -1, ornament, Gold);
        Anchor(ornRight.rectTransform, new Vector2(0.5f, 1f), new Vector2(1f, 0.5f), new Vector2(230f, -200f), new Vector2(260f, 32f));
        ornRight.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

        // Sombra suave detrás de los botones para que se lean sobre el fondo
        Image backdrop = Layer(canvas, "ButtonBackdrop", 4, softOval, new Color(0.02f, 0.01f, 0.03f, 0.78f));
        Anchor(backdrop.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(1100f, 720f));
        backdrop.raycastTarget = false;

        // Botones del menú principal
        RectTransform main = (RectTransform)controller.mainPanel.transform;
        main.SetSiblingIndex(5);
        Anchor(main, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(640f, 480f));
        Button[] buttons = { controller.playButton, controller.continueButton, controller.optionsButton, controller.creditsButton, controller.exitButton };
        string[] labels = { "Nueva partida", "Continuar", "Opciones", "Créditos", "Salir" };
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            StyleTextButton(buttons[i], labels[i], 48f);
            Anchor((RectTransform)buttons[i].transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 170f - i * 84f), new Vector2(560f, 76f));
        }

        // Gato junto al camino
        Sprite[] catFrames = Directory.GetFiles("Assets/Art/Characters/CatSprites", "idle_*.png").OrderBy(p => p)
            .Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p.Replace('\\', '/'))).Where(s => s != null).ToArray();
        Image catImg = Layer(canvas, "MenuCat", 6, catFrames.Length > 0 ? catFrames[0] : null, Color.white);
        catImg.preserveAspect = true;
        catImg.raycastTarget = false;
        Anchor(catImg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(560f, 30f), new Vector2(480f, 320f));

        // Ayuda de controles
        TMP_Text hint = Text(canvas, "ControlsHint", "Enter o clic: seleccionar      Flechas: moverse      Esc: volver", 24f, new Color(0.95f, 0.9f, 0.8f, 0.55f));
        hint.transform.SetSiblingIndex(7);
        Anchor(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(1400f, 40f));

        // Paneles secundarios
        StylePanel(controller.optionsPanel, controller.backFromOptionsButton);
        StylePanel(controller.creditsPanel, controller.backFromCreditsButton);
        controller.optionsPanel.transform.SetAsLastSibling();
        controller.creditsPanel.transform.SetAsLastSibling();

        // Ambientación
        CanvasGroup fade = GetOrAdd<CanvasGroup>(canvasGo);
        MenuAmbience amb = GetOrAdd<MenuAmbience>(canvasGo);
        SerializedObject so = new SerializedObject(amb);
        so.FindProperty("background").objectReferenceValue = bg;
        so.FindProperty("title").objectReferenceValue = group;
        so.FindProperty("fadeGroup").objectReferenceValue = fade;
        so.FindProperty("particleLayer").objectReferenceValue = particles;
        so.FindProperty("cat").objectReferenceValue = catImg;
        SerializedProperty frames = so.FindProperty("catFrames");
        frames.arraySize = catFrames.Length;
        for (int i = 0; i < catFrames.Length; i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = catFrames[i];
        so.FindProperty("firstSelected").objectReferenceValue = controller.playButton != null ? controller.playButton.gameObject : null;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MainMenuStyler] Menú principal maquetado.");
    }

    // ------------------------------------------------------------------ piezas

    private static void StyleTextButton(Button b, string label, float size)
    {
        Image img = b.GetComponent<Image>();
        if (img != null) img.color = new Color(0f, 0f, 0f, 0f); // transparente pero recibe el clic
        UIButtonAnimator old = b.GetComponent<UIButtonAnimator>();
        if (old != null) Object.DestroyImmediate(old);
        if (b.GetComponent<MenuButtonFX>() == null) b.gameObject.AddComponent<MenuButtonFX>();
        TMP_Text t = b.GetComponentInChildren<TMP_Text>();
        if (t != null)
        {
            t.text = label;
            t.fontSize = size;
            t.fontStyle = FontStyles.Bold;
            t.characterSpacing = 6f;
            t.color = Cream;
            t.alignment = TextAlignmentOptions.Center;
            t.enableVertexGradient = false;
            ApplyOutline(t, new Color(0.05f, 0.02f, 0.02f, 1f), 0.18f);
        }
    }

    private static void StylePanel(GameObject panel, Button back)
    {
        if (panel == null) return;
        Image img = panel.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = panelFrame;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        Outline outline = panel.GetComponent<Outline>();
        if (outline != null) Object.DestroyImmediate(outline);

        // Deslizadores y lista de calidad en tonos oscuros con detalles dorados
        foreach (Slider sl in panel.GetComponentsInChildren<Slider>(true))
        {
            foreach (Image i in sl.GetComponentsInChildren<Image>(true))
                i.color = i.name == "Fill" ? Gold : i.name == "Handle" ? Cream : FieldColor;
        }
        foreach (TMP_Dropdown dd in panel.GetComponentsInChildren<TMP_Dropdown>(true))
        {
            foreach (Image i in dd.GetComponentsInChildren<Image>(true))
                i.color = i.name == "Arrow" || i.name == "Item Checkmark" ? Gold
                    : i.name == "Item Background" ? new Color(0.26f, 0.18f, 0.14f, 1f) : FieldColor;
            foreach (TMP_Text t in dd.GetComponentsInChildren<TMP_Text>(true))
            {
                t.color = Cream;
                t.fontSize = 28f;
            }
            ColorBlock cb = dd.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 0.92f, 0.75f);
            cb.selectedColor = new Color(1f, 0.92f, 0.75f);
            dd.colors = cb;
        }
        foreach (TMP_Text t in panel.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.GetComponentInParent<Button>() != null || t.GetComponentInParent<TMP_Dropdown>() != null) continue;
            bool isTitle = t.name.StartsWith("Título") || t.name.StartsWith("Titulo");
            t.color = isTitle ? Gold : Cream;
            if (isTitle) { t.fontSize = 60f; t.fontStyle = FontStyles.Bold; t.characterSpacing = 8f; }
            else if (t.name.EndsWith("Label")) t.fontSize = 34f;
            else t.fontSize = Mathf.Max(t.fontSize, 28f);
        }
        if (back != null) StyleTextButton(back, "Volver", 42f);
    }

    private static void ApplyOutline(TMP_Text t, Color color, float width)
    {
        // Material propio por texto para no alterar el material compartido de la fuente
        Material m = new Material(t.font.material);
        m.name = t.font.name + " " + t.name + " Outline";
        m.EnableKeyword("OUTLINE_ON");
        m.SetColor("_OutlineColor", color);
        m.SetFloat("_OutlineWidth", width);
        m.EnableKeyword("UNDERLAY_ON");
        m.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.6f));
        m.SetFloat("_UnderlayOffsetX", 0.6f);
        m.SetFloat("_UnderlayOffsetY", -0.6f);
        m.SetFloat("_UnderlaySoftness", 0.4f);
        string dir = UiDir + "/MenuMaterials";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(UiDir, "MenuMaterials");
        // Nombre único por ruta en la jerarquía (hay dos botones "BackButton" en paneles distintos)
        string id = t.name;
        for (Transform p = t.transform.parent; p != null && p.GetComponent<Canvas>() == null; p = p.parent) id = p.name + "_" + id;
        id = id.Replace(" ", "").Replace("(", "").Replace(")", "");
        string path = $"{dir}/{id}.mat";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(m, path);
        t.fontSharedMaterial = m;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    private static RectTransform Child(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t == null)
        {
            t = new GameObject(name, typeof(RectTransform)).transform;
            t.SetParent(parent, false);
        }
        return (RectTransform)t;
    }

    private static Image Layer(Transform parent, string name, int sibling, Sprite sprite, Color color)
    {
        RectTransform rt = Child(parent, name);
        if (sibling >= 0) rt.SetSiblingIndex(sibling);
        Image img = GetOrAdd<Image>(rt.gameObject);
        img.sprite = sprite;
        img.color = color;
        if (parent.GetComponent<Canvas>() != null) Stretch(rt, 0f);
        return img;
    }

    private static TMP_Text Text(Transform parent, string name, string text, float size, Color color)
    {
        RectTransform rt = Child(parent, name);
        TMP_Text t = GetOrAdd<TextMeshProUGUI>(rt.gameObject);
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt, float overscan)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(overscan, overscan);
        rt.offsetMax = new Vector2(-overscan, -overscan);
        rt.anchoredPosition = Vector2.zero;
    }

    // ------------------------------------------------------------------ texturas generadas

    private delegate Color PixelFn(float u, float v);

    private static Sprite SaveSprite(string name, int w, int h, PixelFn fn) => SaveSprite(name, w, h, fn, Vector4.zero);

    private static Sprite SaveSprite(string name, int w, int h, PixelFn fn, Vector4 border)
    {
        string path = $"{UiDir}/{name}.png";
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            tex.SetPixel(x, y, fn((x + 0.5f) / w, (y + 0.5f) / h));
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.spriteBorder = border;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // Viñeta: bordes oscuros y degradado inferior para que el título y los botones resalten
    private static Color VignettePixel(float u, float v)
    {
        float dx = (u - 0.5f) * 1.6f, dy = (v - 0.55f) * 1.9f;
        float edge = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) - 0.35f) * 0.9f;
        float bottom = Mathf.Clamp01(0.35f - v) * 1.6f;
        float top = Mathf.Clamp01(v - 0.82f) * 2.2f;
        float a = Mathf.Clamp01(edge + bottom * 0.7f + top * 0.6f);
        return new Color(0.03f, 0.02f, 0.05f, a);
    }

    private static Color OrnamentPixel(float u, float v)
    {
        float cy = Mathf.Abs(v - 0.5f) * 32f;
        float thickness = Mathf.Lerp(0.5f, 2.4f, u);
        float line = Mathf.Clamp01(thickness - cy + 0.5f) * Mathf.Clamp01(u * 8f);
        float dx = Mathf.Abs(u - 0.93f) * 256f;
        float diamond = Mathf.Clamp01(10f - (dx + cy) + 0.5f);
        float dot = Mathf.Clamp01(3.5f - Mathf.Sqrt(Mathf.Pow((u - 0.82f) * 256f, 2f) + cy * cy) + 0.5f);
        return new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Max(line, Mathf.Max(diamond, dot))));
    }

    // Marco de panel: fondo oscuro casi opaco y doble filete dorado (se usa en 9 partes)
    private static Color PanelFramePixel(float u, float v)
    {
        float d = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) * 64f; // distancia al borde en píxeles
        if (d < 1f) return new Color(0.04f, 0.02f, 0.02f, 1f);
        if (d < 3f) return new Color(Gold.r, Gold.g, Gold.b, 1f);
        if (d >= 6f && d < 7f) return new Color(Gold.r * 0.7f, Gold.g * 0.7f, Gold.b * 0.7f, 1f);
        return new Color(0.07f, 0.05f, 0.06f, 0.95f);
    }

    private static Color SoftOvalPixel(float u, float v)
    {
        float dx = (u - 0.5f) * 2f, dy = (v - 0.5f) * 2f;
        float d = Mathf.Sqrt(dx * dx + dy * dy);
        float a = Mathf.Clamp01(1f - d);
        return new Color(1f, 1f, 1f, a * a);
    }
}
