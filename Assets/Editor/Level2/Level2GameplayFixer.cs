using System.Linq;
using System.Text;
using Level2;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Segunda ronda de correcciones del Nivel 2 (solo Level2.unity y arte del Nivel 2):
/// 1. Avisos: un solo banner abajo al centro (antes salían dos a la vez y tapaban los indicadores sobre la bruja).
/// 2. Lector de crónicas: hoja de pergamino quemado con el juego en pausa.
/// 3. Escaleras: sprite con remates de 9-slice y tramo central adaptable (antes los remates partían los largueros).
/// 4. Guardias: el del callejón camina sobre el techo visible del pasaje y el de la galería apoya en su plataforma.
/// 5. Misiones de formas: las flores y raíces requeridas obligan a volar, usar pociones y pasar en forma de gato.
/// </summary>
public static class Level2GameplayFixer
{
    private const string Art = "Assets/Art/Environment/Level2/";
    private const float AlleyRoofTop = 1.94f; // borde superior visible de los banderines del pasaje de gato

    [MenuItem("Tools/Nivel 2/Jugabilidad/1. Avisos sin solaparse y lector de crónicas")]
    public static void MessagesMenu() => Level2StallsDoorsFixer.Run("Avisos y lector", FixMessagesAndLoreReader);

    [MenuItem("Tools/Nivel 2/Jugabilidad/2. Escaleras sin cortes")]
    public static void LaddersMenu() => Level2StallsDoorsFixer.Run("Escaleras", FixLadders);

    [MenuItem("Tools/Nivel 2/Jugabilidad/3. Guardias sobre su suelo")]
    public static void GuardsMenu() => Level2StallsDoorsFixer.Run("Guardias", FixGuards);

    [MenuItem("Tools/Nivel 2/Jugabilidad/4. Misiones de formas (gato, vuelo, pociones)")]
    public static void FormsMenu() => Level2StallsDoorsFixer.Run("Misiones de formas", BuildFormQuests);

    // ------------------------------------------------------------------ 1. Avisos y lector

    public static void FixMessagesAndLoreReader(StringBuilder log)
    {
        // Sin panel propio, IngredientHUD.ShowWarning usa el banner del HUD: los scripts que llamaban a ambos
        // (cofres, portón, gato) muestran un solo aviso
        IngredientHUD ingredientHud = Object.FindFirstObjectByType<IngredientHUD>(FindObjectsInactive.Include);
        SerializedObject so = new SerializedObject(ingredientHud);
        GameObject oldPanel = so.FindProperty("bannerPanel").objectReferenceValue as GameObject;
        so.FindProperty("bannerText").objectReferenceValue = null;
        so.FindProperty("bannerPanel").objectReferenceValue = null;
        so.ApplyModifiedPropertiesWithoutUndo();
        if (oldPanel != null) oldPanel.SetActive(false);
        log.AppendLine("  IngredientHUD sin banner propio: un solo aviso en el HUD");

        RectTransform banner = (RectTransform)GameObject.Find("UI/HUD_Canvas").transform.Find("MessageBanner");
        banner.anchorMin = banner.anchorMax = banner.pivot = new Vector2(0.5f, 0f);
        banner.anchoredPosition = new Vector2(0f, 96f);
        banner.sizeDelta = new Vector2(1300f, 90f);
        TextMeshProUGUI bannerText = banner.GetComponent<TextMeshProUGUI>();
        bannerText.fontSize = 34f;
        bannerText.textWrappingMode = TextWrappingModes.Normal;
        bannerText.outlineWidth = 0.22f;
        bannerText.outlineColor = new Color32(24, 12, 4, 255);
        log.AppendLine("  Banner del HUD abajo al centro, con contorno oscuro (no tapa los indicadores sobre la bruja)");

        BuildLoreReader(log);
    }

    private static void BuildLoreReader(StringBuilder log)
    {
        ImportSprite(Art + "UI_Lore_Parchment.png", 100f, Vector4.zero, SpriteMeshType.FullRect);
        Sprite parchment = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "UI_Lore_Parchment.png");

        Transform ui = GameObject.Find("UI").transform;
        Transform existing = ui.Find("LoreReader_Canvas");
        GameObject root = existing != null ? existing.gameObject : new GameObject("LoreReader_Canvas", typeof(RectTransform));
        root.transform.SetParent(ui, false);
        Canvas canvas = Get<Canvas>(root);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        CanvasScaler scaler = Get<CanvasScaler>(root);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform panel = Child(root.transform, "Panel");
        Stretch(panel);
        Get<Image>(panel.gameObject).color = new Color(0.04f, 0.02f, 0.01f, 0.6f);

        RectTransform paper = Child(panel, "Parchment");
        paper.anchorMin = paper.anchorMax = paper.pivot = new Vector2(0.5f, 0.5f);
        paper.sizeDelta = new Vector2(1120f, 840f);
        paper.anchoredPosition = Vector2.zero;
        Image paperImg = Get<Image>(paper.gameObject);
        paperImg.sprite = parchment;
        paperImg.preserveAspect = true;
        paperImg.raycastTarget = false;

        TextMeshProUGUI title = Text(paper, "Title", new Vector2(0.14f, 0.76f), new Vector2(0.86f, 0.88f), 48f, FontStyles.Bold, TextAlignmentOptions.Center);
        TextMeshProUGUI body = Text(paper, "Body", new Vector2(0.14f, 0.24f), new Vector2(0.86f, 0.74f), 36f, FontStyles.Italic, TextAlignmentOptions.Center);
        TextMeshProUGUI hint = Text(paper, "Hint", new Vector2(0.14f, 0.12f), new Vector2(0.86f, 0.2f), 24f, FontStyles.Normal, TextAlignmentOptions.Center);
        hint.text = "Pulsa Q, Enter o haz clic para seguir";
        hint.color = new Color32(92, 58, 30, 255);

        Level2LoreReader reader = Get<Level2LoreReader>(root);
        reader.SetupReferences(panel.gameObject, title, body);
        panel.gameObject.SetActive(false);
        log.AppendLine("  LoreReader_Canvas: pergamino quemado con pausa para diarios, grimorios y actas");
    }

    private static TextMeshProUGUI Text(Transform parent, string name, Vector2 min, Vector2 max, float size, FontStyles style, TextAlignmentOptions align)
    {
        RectTransform rt = Child(parent, name);
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = Get<TextMeshProUGUI>(rt.gameObject);
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.color = new Color32(48, 26, 10, 255); // tinta sepia oscura sobre el papel claro
        tmp.raycastTarget = false;
        return tmp;
    }

    // ------------------------------------------------------------------ 2. Escaleras

    public static void FixLadders(StringBuilder log)
    {
        // Remates de 16 px arriba y abajo: en modo Tiled solo se repite el tramo central (un peldaño por unidad)
        ImportSprite(Art + "Village_Ladder.png", 64f, new Vector4(0f, 16f, 0f, 16f), SpriteMeshType.FullRect);
        Sprite ladder = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Village_Ladder.png");
        int n = 0;
        foreach (LadderInteractable li in Object.FindObjectsByType<LadderInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            SpriteRenderer sr = li.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            Vector2 size = sr.drawMode == SpriteDrawMode.Simple ? (Vector2)sr.bounds.size : sr.size;
            sr.sprite = ladder;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Adaptive; // estira el tramo en vez de dejar un peldaño cortado arriba
            sr.adaptiveModeThreshold = 0.5f;
            sr.size = new Vector2(0.85f, size.y);
            n++;
        }
        log.AppendLine($"  {n} escaleras con remates completos y tramo central adaptable");
    }

    // ------------------------------------------------------------------ 3. Guardias

    public static void FixGuards(StringBuilder log)
    {
        // El techo del pasaje de gato era un bloque invisible 1.3 u por encima de los banderines: el guardia
        // parecía flotar. Se baja al borde visible manteniendo la altura libre de 1.05 u para el gato.
        BoxCollider2D roof = GameObject.Find("Environment/Zone_4_CallejonGato/CatPassage_MarketAlley/Alley_LowCeiling").GetComponent<BoxCollider2D>();
        float bottom = roof.bounds.min.y;
        float scaleY = roof.transform.lossyScale.y;
        roof.size = new Vector2(roof.size.x, (AlleyRoofTop - bottom) / scaleY);
        // Centro en el transform y offset vertical 0: Level2Verifier mide la altura libre con la posición del transform
        roof.transform.position = new Vector3(roof.transform.position.x, (AlleyRoofTop + bottom) * 0.5f, roof.transform.position.z);
        roof.offset = new Vector2(roof.offset.x, 0f);
        Physics2D.SyncTransforms();
        log.AppendLine($"  Alley_LowCeiling: techo transitable a y={roof.bounds.max.y:F3} (antes 3.220), altura libre {bottom - 0.17f:F2} u");

        PlaceGuard("NPCs/Zona_4/Guard_Z4_Roof", AlleyRoofTop, log);
        PlaceGuard("NPCs/Zona_5/Guard_Z5_Upper", GameObject.Find("Environment/Zone_5_GranMercado/Roof_Z5_Mid_2").GetComponent<Collider2D>().bounds.max.y, log);
    }

    private static void PlaceGuard(string path, float surface, StringBuilder log)
    {
        GameObject guard = GameObject.Find(path);
        BoxCollider2D col = guard.GetComponent<BoxCollider2D>();
        float dy = surface - col.bounds.min.y;
        guard.transform.position += new Vector3(0f, dy, 0f);
        PatrolRoute route = guard.GetComponent<PatrolRoute>();
        foreach (Transform wp in route.Waypoints.Where(w => w != null))
            wp.position = new Vector3(wp.position.x, guard.transform.position.y, wp.position.z);
        // El sprite apoya los pies en la raíz como el resto de guardias (este estaba desplazado 1.2 u hacia abajo)
        Transform visual = guard.transform.Find("Visual");
        visual.localPosition = new Vector3(0f, 0.008f, 0f);
        log.AppendLine($"  {guard.name}: pies y ruta en y={surface:F3} (movido {dy:+0.000;-0.000} u)");
    }

    // ------------------------------------------------------------------ 4. Misiones de formas

    public static void BuildFormQuests(StringBuilder log)
    {
        Transform chests = GameObject.Find("Gameplay/Chests").transform;

        // Vuelo: cofre sobre una plataforma que flota sobre el canal, inalcanzable saltando
        Transform zone3 = GameObject.Find("Environment/Zone_3_PlazaCentral").transform;
        GameObject template = GameObject.Find("Environment/Zone_3_PlazaCentral/Roof_Z3_LeftHigh");
        Transform perch = zone3.Find("Perch_Z3_CanalFlight");
        if (perch == null)
        {
            perch = new GameObject("Perch_Z3_CanalFlight").transform;
            perch.SetParent(zone3, false);
            SpriteRenderer src = template.GetComponent<SpriteRenderer>();
            SpriteRenderer sr = perch.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = src.sprite;
            sr.sortingLayerName = src.sortingLayerName;
            sr.sortingOrder = src.sortingOrder;
            foreach (Component c in new Component[] { template.GetComponent<BoxCollider2D>(), template.GetComponent<PlatformEffector2D>() })
            {
                UnityEditorInternal.ComponentUtility.CopyComponent(c);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(perch.gameObject);
            }
            perch.gameObject.layer = template.layer;
        }
        perch.position = new Vector3(78f, 7.2f, 0f);
        Physics2D.SyncTransforms();
        float perchTop = perch.GetComponent<Collider2D>().bounds.max.y;

        Level2Chest flyChest = CloneChest(chests, "Chest_Z3_PlazaStreet", "Chest_Fly_Z3_CanalPerch", "chest_fly_z3_canal_perch", "Cofre sobre el Canal");
        flyChest.transform.position = new Vector3(78f, perchTop + 0.5f, 0f);
        Physics2D.SyncTransforms();
        Level2StallsDoorsFixer.Ground(flyChest.transform, flyChest.GetComponent<SpriteRenderer>(), log, splitPadding: true);
        SetContents(flyChest, Ing(IngredientType.WitheredFlower, "L2_Ing_Flower_FlyPerch"), Heart());
        log.AppendLine($"  Vuelo: Chest_Fly_Z3_CanalPerch sobre el canal (plataforma a y={perchTop:F2}; el salto llega a 2.8 u)");

        // Pociones: el guardia de la calle del Gran Mercado vigila de cerca el cofre de la plaza
        GameObject guard = GameObject.Find("NPCs/Zona_5/Guard_Z5_Street");
        Transform[] wps = guard.GetComponent<PatrolRoute>().Waypoints.ToArray();
        wps[0].position = new Vector3(132.2f, wps[0].position.y, 0f);
        wps[1].position = new Vector3(134.4f, wps[1].position.y, 0f);
        guard.transform.position = new Vector3(134.0f, guard.transform.position.y, 0f);
        SerializedObject pso = new SerializedObject(guard.GetComponent<PatrolRoute>());
        pso.FindProperty("waitTimeAtPoint").floatValue = 0.6f;
        pso.ApplyModifiedPropertiesWithoutUndo();
        Level2Chest guarded = GameObject.Find("Gameplay/Chests/Chest_Z5_MarketPlaza").GetComponent<Level2Chest>();
        SetContents(guarded, Ing(IngredientType.Mushroom, "L2_Ing_Mushroom_Z5"), Ing(IngredientType.WitheredFlower, "L2_Ing_Flower_Z5_Guarded"),
            Potion(PotionType.Hypnotize), Heart());
        log.AppendLine("  Pociones: Guard_Z5_Street patrulla 132.2–134.4 encima de Chest_Z5_MarketPlaza (Flor marchita)");

        // Gato: la Casa Abandonada (Raíz del ático) solo se entra por la gatera
        Transform ext = GameObject.Find("Environment/Village_Exterior_Structures").transform;
        HouseDoor flap = ext.Find("Door_Ext_House3_Ground").GetComponent<HouseDoor>();
        SerializedObject fso = new SerializedObject(flap);
        fso.FindProperty("catOnly").boolValue = true;
        fso.FindProperty("closedSprite").objectReferenceValue = fso.FindProperty("lockedSprite").objectReferenceValue;
        fso.ApplyModifiedPropertiesWithoutUndo();
        flap.GetComponent<SpriteRenderer>().sprite = fso.FindProperty("lockedSprite").objectReferenceValue as Sprite;
        HouseDoor roofDoor = ext.Find("Door_Ext_House3_Roof").GetComponent<HouseDoor>();
        SerializedObject rso = new SerializedObject(roofDoor);
        rso.FindProperty("permanentlyBarred").boolValue = true;
        rso.FindProperty("barredMessage").stringValue = "La puerta del tejado está atrancada por dentro. Busca la gatera en la calle.";
        rso.ApplyModifiedPropertiesWithoutUndo();
        roofDoor.GetComponent<SpriteRenderer>().sprite = rso.FindProperty("lockedSprite").objectReferenceValue as Sprite;
        log.AppendLine("  Gato: Door_Ext_House3_Ground es una gatera y la puerta del tejado está atrancada");

        // Reparto: flores y raíces quedan en 2 (las requeridas), cada una detrás de una forma distinta
        SetContents(Find("Chest_Z2_MarketStreet"), Ing(IngredientType.Spider, "L2_Ing_Spider_Z2"), Potion(PotionType.Sleep));
        SetContents(Find("Chest_Z4_AlleyEntry"), Potion(PotionType.Sleep), Potion(PotionType.Blind));
        SetContents(Find("Chest_Z3_EastOverlook"), Ing(IngredientType.Spider, "L2_Ing_Spider_Z3_East"), Heart());
        log.AppendLine("  Flores: vuelo + pociones · Raíces: túnel de gato + ático por la gatera · Hongos 4 · Arañas 4");

        // Letreros que explican cada reto
        Sign(zone3, "Sign_Z3_FlightChest", new Vector2(74.6f, 3.45f), new Vector2(2.6f, 0.8f),
            "Ese cofre flota sobre el canal: vuela con la escoba (F) para alcanzarlo.");
        Sign(GameObject.Find("Environment/Zone_5_GranMercado").transform, "Sign_Z5_GuardedChest", new Vector2(128.6f, 2.85f), new Vector2(3.0f, 0.8f),
            "Un guardia vigila este cofre: duérmelo o ciégalo. Elige poción con 1, 2 o 3 y lánzala con E.");
        log.AppendLine("  Letreros de vuelo y de pociones");
    }

    private static Level2Chest Find(string name) => GameObject.Find("Gameplay/Chests/" + name).GetComponent<Level2Chest>();

    private static Level2Chest CloneChest(Transform parent, string sourceName, string name, string id, string title)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : Object.Instantiate(parent.Find(sourceName).gameObject, parent);
        go.name = name;
        SerializedObject so = new SerializedObject(go.GetComponent<Level2Chest>());
        so.FindProperty("chestId").stringValue = id;
        so.FindProperty("chestTitle").stringValue = title;
        so.ApplyModifiedPropertiesWithoutUndo();
        return go.GetComponent<Level2Chest>();
    }

    private struct Entry { public ChestItemKind kind; public IngredientType ing; public string id; public PotionType potion; }

    private static Entry Ing(IngredientType t, string id) => new Entry { kind = ChestItemKind.Ingredient, ing = t, id = id };
    private static Entry Potion(PotionType p) => new Entry { kind = ChestItemKind.Potion, potion = p };
    private static Entry Heart() => new Entry { kind = ChestItemKind.Heart };

    private static void SetContents(Level2Chest chest, params Entry[] entries)
    {
        SerializedObject so = new SerializedObject(chest);
        SerializedProperty list = so.FindProperty("contents");
        list.ClearArray();
        for (int i = 0; i < entries.Length; i++)
        {
            list.InsertArrayElementAtIndex(i);
            SerializedProperty e = list.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("itemKind").enumValueIndex = (int)entries[i].kind;
            e.FindPropertyRelative("ingredientType").enumValueIndex = (int)entries[i].ing;
            e.FindPropertyRelative("ingredientUniqueId").stringValue = entries[i].id ?? "";
            e.FindPropertyRelative("potionType").enumValueIndex = (int)entries[i].potion;
            e.FindPropertyRelative("amount").intValue = 1;
            e.FindPropertyRelative("keyId").stringValue = "";
            e.FindPropertyRelative("keyDisplayName").stringValue = "";
            e.FindPropertyRelative("claimed").boolValue = false;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Sign(Transform parent, string name, Vector2 pos, Vector2 size, string text)
    {
        Transform t = parent.Find(name);
        if (t == null) { t = new GameObject(name).transform; t.SetParent(parent, false); }
        t.position = pos;
        Level2InfoSign sign = Get<Level2InfoSign>(t.gameObject);
        SerializedObject so = new SerializedObject(sign);
        so.FindProperty("idleAlpha").floatValue = 0.6f;
        so.ApplyModifiedPropertiesWithoutUndo();
        sign.Configure(text, size, 0.16f);
    }

    // ------------------------------------------------------------------ Utilidades

    private static void ImportSprite(string path, float ppu, Vector4 border, SpriteMeshType mesh)
    {
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        TextureImporterSettings s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spriteMeshType = mesh;
        s.spriteBorder = border;
        s.spriteAlignment = (int)SpriteAlignment.Center;
        ti.SetTextureSettings(s);
        ti.SaveAndReimport();
    }

    private static T Get<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    private static RectTransform Child(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t == null) { t = new GameObject(name, typeof(RectTransform)).transform; t.SetParent(parent, false); }
        return (RectTransform)t;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
