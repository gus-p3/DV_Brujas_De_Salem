using System.Linq;
using System.Text;
using Level2;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Reemplaza placeholders del Nivel 2 por sprites del paquete gratuito de Cainos (Pixel Art Platformer - Village
/// Props, Asset Store) sin cambiar la jugabilidad: se usan solo los sprites (nunca los prefabs con física) y cada
/// prop conserva su objeto, su collider (ajustado al nuevo dibujo) y su lugar.
/// - Puestos: mesa + 2 pilares + toldo recoloreado a los tonos cálidos del juego + fruta, canasta, saco y farolito.
/// - Calle: barriles, cajas, faroles y carretas con el arte de Cainos.
/// - Cofres: los 16 con el cofre de Cainos y su animación de abrir (madera, hierro con llave, plata en los retos
///   de vuelo y pociones, oro en la Mansión). Escaleras: "Ladder 01" con 9-slice (Level2GameplayFixer.FixLadders).
/// - Decoración sin colisión en huecos libres: calabazas, heno, girasol, caldero de la Botica, sacos, tablón de
///   avisos, espantapájaros, cruz junto a la Casa en Cuarentena, antorchas y pebetero con fuego animado.
/// </summary>
public static class Level2CainosArtSwap
{
    private const string Cainos = "Assets/Cainos/Pixel Art Platformer - Village Props/";
    private const string Atlas = Cainos + "Texture/TX Village Props.png";
    private const string ChestAtlas = Cainos + "Texture/TX Chest Animation.png";
    private const string LitSpriteMaterial = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";
    private const string Art = "Assets/Art/Environment/Level2/";
    private const float CounterTopAboveGround = 1.03f; // alto de "Stall Table" (33 px a 32 PPU)

    [MenuItem("Tools/Nivel 2/Arte/Usar props de Cainos")]
    public static void SwapMenu() => Level2StallsDoorsFixer.Run("Props de Cainos", Swap);

    public static void Swap(StringBuilder log)
    {
        foreach (string c in new[] { "Red", "Gold", "Orange", "Olive" })
            ImportPixelSprite(Art + "Cainos_Stall_Tent_" + c + ".png");

        int stalls = 0;
        foreach (Transform zone in GameObject.Find("Environment").transform)
        {
            if (!zone.name.StartsWith("Zone_")) continue;
            foreach (Transform t in zone.Cast<Transform>().ToArray())
            {
                // La escala del toldo es la del puesto (1 en la calle; 0.75–1 en los elevados)
                if (t.name.StartsWith("Stall_")) { DressStall(t, t.Find("PostsAndCounter"), 1f); stalls++; }
                else if (t.Find("Elevated_CanopyPosts") != null)
                {
                    DressStall(t, t.Find("Elevated_CanopyPosts"), t.Find("Awning_Sway_Visual").localScale.y); stalls++;
                }
                else if (t.name.StartsWith("Barrel")) SwapProp(t, "Barrel", 1f, log);
                else if (t.name.StartsWith("Crates_Stack")) SwapProp(t, "Crate Large", t.localScale.y, log);
                else if (t.name.StartsWith("Market_Cart")) SwapCart(t, log);
                else if (t.name.StartsWith("Market_Lantern")) SwapLamp(t, log);
            }
        }
        log.AppendLine($"  {stalls} puestos con mesa, pilares, toldo cálido y mercancía de Cainos");

        Transform deco = Child(GameObject.Find("Environment").transform, "Decoration_Cainos");
        Deco(deco, "Pumpkins_Z1", "Pumpkin 01", 23.1f);
        Deco(deco, "HayBale_Z2", "Hay Bale", 29.7f);
        Deco(deco, "Sunflower_Z5", "Sunflower 01", 165.3f);
        Deco(deco, "Cauldron_Botica", "Cauldron", 14.6f, 1.5f);
        Deco(deco, "Pot_Botica", "Pot 02", 15.4f);
        Deco(deco, "GunnyBag_Z1", "Gunny Bag 01", 20.9f);
        Transform board = Deco(deco, "NoticeBoard_Z2", "Billboard", 37.2f);
        Part(board, "Notice_0", "Billboard Noti 01", new Vector2(-0.5f, 1.05f), 1);
        Part(board, "Notice_1", "Billboard Noti 04", new Vector2(0.05f, 1.1f), 1);
        Part(board, "Notice_2", "Billboard Noti 02", new Vector2(0.55f, 1.0f), 1);
        Deco(deco, "GunnyBag_Z4", "Gunny Bag 02", 117.0f);
        Deco(deco, "Scarecrow_Z5", "Scarecrow", 126.2f); // entre el checkpoint y el letrero del guardia
        // Pasado x 175 todo queda detrás del portón de salida (Goal_Gate_Visual): ahí no va nada
        Deco(deco, "Gravestone_Z3", "Gravestone 03", 66.3f);
        FireProp(deco, "FireBowl_Z6", "Fire Bowl", new Vector2(169.55f, 0.6f));
        Bounds door = GameObject.Find("Environment/Village_Exterior_Structures/Door_Ext_Z6_Sanctum").GetComponent<SpriteRenderer>().bounds;
        FireProp(deco, "Torch_Sanctum_L", "Torch", new Vector2(door.min.x - 0.35f, door.center.y + 0.2f));
        FireProp(deco, "Torch_Sanctum_R", "Torch", new Vector2(door.max.x + 0.35f, door.center.y + 0.2f));
        Physics2D.SyncTransforms();
        foreach (Transform d in deco)
            if (!d.name.StartsWith("Torch")) Level2StallsDoorsFixer.Ground(d, d.GetComponentInChildren<SpriteRenderer>(), log);
        log.AppendLine("  Decoración sin colisión: calabazas, heno, girasol, caldero y olla de la Botica, sacos, tablón de avisos,");
        log.AppendLine("  espantapájaros, cruz junto a la Casa en Cuarentena, pebetero y antorchas del Santuario con fuego animado");

        SwapChests(log);
        Level2GameplayFixer.FixLadders(log);
    }

    // ------------------------------------------------------------------ Cofres

    /// <summary>
    /// Cofre de Cainos (pivote abajo, 7 cuadros de tapa). El transform baja a la base del cofre y su trigger y su
    /// indicador se compensan para quedarse en el mismo lugar del mundo.
    /// </summary>
    private static void SwapChests(StringBuilder log)
    {
        Sprite[] all = AssetDatabase.LoadAllAssetsAtPath(ChestAtlas).OfType<Sprite>().ToArray();
        foreach (Level2Chest chest in Object.FindObjectsByType<Level2Chest>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string kind = chest.name switch
            {
                "Chest_Int_House4_Royal" => "Golden",
                "Chest_Int_House2_Locked" => "Iron",
                "Chest_Fly_Z3_CanalPerch" or "Chest_Z5_MarketPlaza" => "Silver",
                _ => "Wooden",
            };
            Sprite[] frames = Enumerable.Range(0, 7).Select(i => all.First(sp => sp.name == $"TX Chest Animation {kind} {i}")).ToArray();

            SpriteRenderer sr = chest.GetComponent<SpriteRenderer>();
            if (!Level2GroundingTool.TryOpaqueBottom(sr, out float baseY)) continue;
            SerializedObject so = new SerializedObject(chest);
            so.FindProperty("closedSprite").objectReferenceValue = frames[0];
            so.FindProperty("openSprite").objectReferenceValue = frames[6];
            SerializedProperty list = so.FindProperty("openFrames");
            list.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            sr.sprite = frames[0];

            float dy = baseY - chest.transform.position.y;
            if (Mathf.Abs(dy) > 0.001f)
            {
                chest.transform.position += new Vector3(0f, dy, 0f);
                BoxCollider2D col = chest.GetComponent<BoxCollider2D>();
                col.offset -= new Vector2(0f, dy / chest.transform.lossyScale.y);
                foreach (Transform child in chest.transform) child.position -= new Vector3(0f, dy, 0f);
            }
            Physics2D.SyncTransforms();
            Level2StallsDoorsFixer.Ground(chest.transform, sr, log, splitPadding: true);
            log.AppendLine($"  {chest.name}: cofre de {kind} con animación de abrir");
        }
    }

    // ------------------------------------------------------------------ Puestos

    /// <summary>
    /// La mesa reemplaza al antiguo bloque de postes y mostrador: su base y su collider de mostrador (one-way) se
    /// conservan en el mismo lugar del mundo, así no cambia por dónde salta la bruja.
    /// </summary>
    private static void DressStall(Transform root, Transform posts, float s)
    {
        SpriteRenderer postsSr = posts.GetComponent<SpriteRenderer>();
        Level2GroundingTool.TryOpaqueBottom(postsSr, out float groundY);
        Vector3 oldPos = posts.position;
        float lx = posts.localPosition.x;
        float groundLocal = groundY - root.position.y;

        postsSr.sprite = Sprite("Stall Table");
        postsSr.sortingOrder = 1;
        posts.localScale = new Vector3(s, s, 1f);
        posts.localPosition = new Vector3(lx, groundLocal, 0f);
        BoxCollider2D counter = posts.GetComponent<BoxCollider2D>();
        if (counter != null) counter.offset += (Vector2)(oldPos - posts.position) / s; // el mostrador físico no se mueve
        Physics2D.SyncTransforms();
        Level2GroundingTool.TryOpaqueBottom(postsSr, out float bottom);
        posts.position += new Vector3(0f, groundY - bottom, 0f);
        if (counter != null) counter.offset -= new Vector2(0f, (groundY - bottom) / s);
        float counterTop = groundLocal + CounterTopAboveGround * s;

        // Toldo: su borde superior coincide con el techo físico (AwningRoof) para que la bruja pise lo que ve
        Transform awning = root.Find("Awning_Sway_Visual");
        SpriteRenderer awningSr = awning.GetComponent<SpriteRenderer>();
        string color = new[] { "Red", "Gold", "Orange", "Olive" }.FirstOrDefault(c => awningSr.sprite != null && awningSr.sprite.name.Contains(c)) ?? "Red";
        awningSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Cainos_Stall_Tent_" + color + ".png");
        awning.localScale = new Vector3(s, s, 1f);
        Collider2D roof = root.Find("AwningRoof")?.GetComponent<Collider2D>();
        float tentHalf = awningSr.sprite.bounds.extents.y * s;
        float tentTop = roof != null ? roof.bounds.max.y - root.position.y + 0.03f : groundLocal + 2.35f * s;
        awning.localPosition = new Vector3(lx, tentTop - tentHalf, 0f);

        foreach (float side in new[] { -1f, 1f })
        {
            Transform pillar = Child(root, side < 0 ? "Stall_Pillar_L" : "Stall_Pillar_R");
            SetSprite(pillar, Sprite("Stall Tent Pillar"), 1);
            pillar.localScale = new Vector3(s, s, 1f);
            pillar.localPosition = new Vector3(lx + side * 1.25f * s, groundLocal + 1.36f * s, 0f);
        }

        // Mercancía: canasta o caldero en un extremo del mostrador y fruta en fila
        Transform wares = root.Find("Counter_Wares_Visual");
        bool left = wares.localPosition.x < lx;
        SetSprite(wares, Sprite(left ? "Basket" : "Kettle"), 2);
        wares.localScale = new Vector3(s, s, 1f);
        wares.localPosition = new Vector3(lx + (left ? -0.75f : 0.75f) * s, counterTop, 0f);
        for (int i = 0; i < 4; i++)
        {
            Transform fruit = Child(root, "Stall_Fruit_" + i);
            SetSprite(fruit, Sprite("Stall Fruit 0" + (i + 1)), 2);
            fruit.localScale = new Vector3(s, s, 1f);
            fruit.localPosition = new Vector3(lx + (left ? -0.25f : -0.85f) * s + i * 0.3f * s, counterTop + 0.2f * s, 0f);
        }

        // Colgantes: saco que se mece desde el toldo y farolito colgando del borde (conserva su Light2D)
        Transform hanging = root.Find("Hanging_Wares_Pivot/Hanging_Wares_Visual");
        SetSprite(hanging, Sprite("Hanging Bag"), 2);
        hanging.localScale = new Vector3(0.55f * s, 0.55f * s, 1f);
        hanging.localPosition = new Vector3(0f, -0.34f * s, 0f);
        root.Find("Hanging_Wares_Pivot").localPosition = new Vector3(lx + 1.0f * s, awning.localPosition.y - tentHalf, 0f);

        Transform lamp = root.Find("Hanging_Lantern_Visual");
        SetSprite(lamp, Sprite("Road Lamp Light On"), 2);
        lamp.localScale = new Vector3(s, s, 1f);
        lamp.localPosition = new Vector3(lx - 1.0f * s, awning.localPosition.y - tentHalf, 0f);
    }

    // ------------------------------------------------------------------ Calle

    private static void SwapProp(Transform t, string sprite, float scale, StringBuilder log)
    {
        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        if (sr == null) return;
        sr.sprite = Sprite(sprite);
        t.localScale = new Vector3(scale, scale, 1f);
        FitBoxCollider(t, sr);
        Physics2D.SyncTransforms();
        Level2StallsDoorsFixer.Ground(t, sr, log);
    }

    private static void SwapCart(Transform t, StringBuilder log)
    {
        // Carretilla al 80 %: mide como la carreta anterior y sigue siendo un obstáculo saltable
        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        sr.sprite = Sprite("Wheelbarrow");
        t.localScale = new Vector3(0.8f, 0.8f, 1f);
        Transform wheel = Child(t, "Wheel");
        SetSprite(wheel, Sprite("Wheelbarrow Wheel"), sr.sortingOrder + 1);
        wheel.localScale = Vector3.one;
        wheel.localPosition = new Vector3(-1.22f, -0.14f, 0f);
        FitBoxCollider(t, sr);
        Physics2D.SyncTransforms();
        Level2StallsDoorsFixer.Ground(t, sr, log);
    }

    private static void SwapLamp(Transform t, StringBuilder log)
    {
        // Poste de Cainos al 80 % (2.4 u) con brazo, cadena y farol; la luz se mueve al farol
        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        sr.sprite = Sprite("Road Lamp Pillar");
        t.localScale = new Vector3(0.8f, 0.8f, 1f);
        Part(t, "Support", "Road Lamp Support", new Vector2(0.06f, 2.72f), sr.sortingOrder);
        Part(t, "Chain", "Chain 02", new Vector2(0.84f, 2.59f), sr.sortingOrder);
        Part(t, "Lamp", "Road Lamp Light On", new Vector2(0.86f, 2.22f), sr.sortingOrder + 1);
        Transform light = t.Find("Light2D_Lantern");
        if (light != null) light.localPosition = new Vector3(0.86f, 2.0f, 0f);
        Physics2D.SyncTransforms();
        Level2StallsDoorsFixer.Ground(t, sr, log);
    }

    private static void Part(Transform parent, string name, string sprite, Vector2 pos, int order)
    {
        Transform p = Child(parent, name);
        SetSprite(p, Sprite(sprite), order);
        p.localScale = Vector3.one;
        p.localPosition = pos;
    }

    private static Transform Deco(Transform parent, string name, string sprite, float x, float scale = 1f)
    {
        Transform t = Child(parent, name);
        SetSprite(t, Sprite(sprite), 0); // detrás de la bruja (≥1) y delante de las fachadas
        t.localScale = new Vector3(scale, scale, 1f);
        t.position = new Vector3(x, 0.6f, 0f);
        return t;
    }

    /// <summary>Prefab de Cainos con fuego de partículas (sin física): se desempaqueta y se lleva a la capa Gameplay.</summary>
    private static void FireProp(Transform parent, string name, string prefab, Vector2 pos)
    {
        Transform t = parent.Find(name);
        if (t == null)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(Cainos + "Prefab/PF Village Props - " + prefab + ".prefab"));
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = name;
            t = go.transform;
            t.SetParent(parent, true);
        }
        t.position = new Vector3(pos.x, pos.y, 0f);
        Material lit = AssetDatabase.LoadAssetAtPath<Material>(LitSpriteMaterial);
        foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true))
        {
            r.sortingLayerName = "Gameplay";
            r.sortingOrder = r is SpriteRenderer ? 0 : 1; // la llama delante de su soporte
            if (r is SpriteRenderer && lit != null) r.sharedMaterial = lit; // el prefab trae Sprites/Default, sin luces 2D
        }
    }

    // ------------------------------------------------------------------ Utilidades

    private static void FitBoxCollider(Transform t, SpriteRenderer sr)
    {
        BoxCollider2D col = t.GetComponent<BoxCollider2D>();
        if (col == null) return;
        col.size = sr.sprite.bounds.size;
        col.offset = sr.sprite.bounds.center;
    }

    private static Sprite Sprite(string name) =>
        AssetDatabase.LoadAllAssetsAtPath(Atlas).OfType<Sprite>().First(s => s.name == "TX Village Props - " + name);

    private static void SetSprite(Transform t, Sprite sprite, int order)
    {
        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        if (sr == null) sr = t.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Gameplay";
        sr.sortingOrder = order;
        sr.color = Color.white;
        sr.drawMode = SpriteDrawMode.Simple;
        sr.enabled = true;
    }

    private static Transform Child(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null) return t;
        t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    private static void ImportPixelSprite(string path)
    {
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
        if (ti == null) return;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 64f; // los toldos se exportan al doble del atlas de Cainos (32 PPU)
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.SaveAndReimport();
    }
}
