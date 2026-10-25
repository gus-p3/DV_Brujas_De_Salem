using System.Collections.Generic;
using System.Linq;
using System.Text;
using Level2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Corrección del Nivel 2 (Pueblo y Mercado): puestos completos y reacomodados, animación desincronizada,
/// puertas adaptadas a las casas y postes y props sobre el suelo (los textos de cofres viven en Level2ChestTextsBuilder).
/// Cada paso es idempotente y solo toca Level2.unity (nunca Level1 ni prefabs/scripts compartidos).
/// Las posiciones salen de la inspección de la escena: ver Docs/Nivel2_Textos.md y el informe de la tarea.
/// </summary>
public static class Level2StallsDoorsFixer
{
    private const string ScenePath = "Assets/Scenes/Level2.unity";
    private const string Art = "Assets/Art/Environment/Level2/";
    private const float GroundY = 0.17f;

    // Medidas del sprite de postes (3×2 u): la base opaca está 0.051 u sobre el borde del rect y el mostrador a 1.045 u
    private const float PostsCounterTop = 0.045f;   // y local del mostrador respecto al centro del puesto
    private const float AwningLocalY = 1.25f;

    public const float DoorTint = 0.94f;

    // ------------------------------------------------------------------ Datos de diseño

    private struct StallSpec
    {
        public string path; public float x; public float scale; public string awning; public string wares; public bool crateLeft;
        public StallSpec(string path, float x, float scale, string awning, string wares, bool crateLeft)
        { this.path = path; this.x = x; this.scale = scale; this.awning = awning; this.wares = wares; this.crateLeft = crateLeft; }
    }

    // Puestos de calle: separados ≥1.0 u entre sí y sin tapar puertas, escaleras, cofres, checkpoints ni aldeanos
    private static readonly StallSpec[] StreetStalls =
    {
        new StallSpec("Environment/Zone_1_Entrada/Stall_Z1_1", 9.7f, 1f, "Red", "WitheredFlower", true),
        new StallSpec("Environment/Zone_2_PuestosBajos/Stall_Z2_1", 33.9f, 1f, "Gold", "Root", false),
        new StallSpec("Environment/Zone_2_PuestosBajos/Stall_Z2_2", 50.9f, 1f, "Orange", "WitheredFlower", true),
        new StallSpec("Environment/Zone_2_PuestosBajos/Stall_Z2_3", 55.8f, 1f, "Olive", "Root", false),
        new StallSpec("Environment/Zone_3_PlazaCentral/Stall_Z3_Left", 72.9f, 1f, "Red", "Root", true),
        new StallSpec("Environment/Zone_3_PlazaCentral/Stall_Z3_Right", 86.3f, 1f, "Gold", "WitheredFlower", false),
        new StallSpec("Environment/Zone_4_CallejonGato/Stall_Z4_Entry", 100.9f, 1f, "Orange", "Root", true),
        new StallSpec("Environment/Zone_5_GranMercado/Stall_Z5_1", 137.4f, 1f, "Olive", "WitheredFlower", false),
        new StallSpec("Environment/Zone_5_GranMercado/Stall_Z5_2", 143.0f, 1f, "Red", "Root", true),
        new StallSpec("Environment/Zone_5_GranMercado/Stall_Z5_3", 159.0f, 1f, "Gold", "WitheredFlower", false),
    };

    // Puestos elevados: el puesto se apoya sobre su plataforma (sin pilares al suelo, que se retiraron en el diseño)
    private static readonly StallSpec[] ElevatedStalls =
    {
        new StallSpec("Environment/Zone_3_PlazaCentral/Roof_Z3_LeftHigh", 71.55f, 0.85f, "Olive", "Root", false),
        new StallSpec("Environment/Zone_3_PlazaCentral/Roof_Z3_RightHigh", 86.48f, 0.85f, "Orange", "WitheredFlower", true),
        new StallSpec("Environment/Zone_5_GranMercado/Roof_Z5_Low_1", 137.0f, 1f, "Red", "Root", true),
        new StallSpec("Environment/Zone_5_GranMercado/Roof_Z5_HighWalk", 143.0f, 1f, "Gold", "WitheredFlower", false),
        new StallSpec("Environment/Zone_5_GranMercado/Roof_Z5_Mid_2", 146.67f, 0.75f, "Olive", "Root", true),
    };

    // Cofres que viajan con el toldo de su puesto
    private static readonly (string chest, string stall)[] RoofChests =
    {
        ("Gameplay/Chests/Chest_Z1_StallRoof", "Environment/Zone_1_Entrada/Stall_Z1_1"),
        ("Gameplay/Chests/Chest_Z2_MarketStreet", "Environment/Zone_2_PuestosBajos/Stall_Z2_2"),
    };

    // Props reubicados para dejar paso libre (zona, x actual aproximada, x nueva)
    private static readonly (string zone, string name, float fromX, float toX)[] PropMoves =
    {
        ("Zone_1_Entrada", "Market_Lantern", 8f, 6.6f),
        ("Zone_2_PuestosBajos", "Barrel", 50f, 48.5f),
        ("Zone_2_PuestosBajos", "Market_Lantern", 57f, 60.9f),
        ("Zone_3_PlazaCentral", "Market_Cart", 82.56f, 82.62f),
        ("Zone_5_GranMercado", "Barrel", 142f, 145.5f),
        ("Zone_5_GranMercado", "Crates_Stack", 144f, 152.85f),
        ("Zone_5_GranMercado", "Market_Lantern", 161f, 161.4f),
        ("Zone_6_SalidaPorton", "Barrel", 173.5f, 166.6f),   // tapaba la puerta del Santuario
    };

    // Aldeanos cuyo punto de inicio quedaba dentro de un puesto (se mueven dentro de su propio radio de paseo)
    private static readonly (string path, float toX)[] VillagerMoves =
    {
        ("NPCs/Zona_2/Villager_Z2_2", 53.35f),
        ("NPCs/Zona_3/Villager_Z3_2", 70.55f),
        ("NPCs/Zona_3/Villager_Z3_3_Preacher", 89.4f),
    };

    private struct HouseSpec { public string path; public float scale; public HouseSpec(string p, float s) { path = p; scale = s; } }

    // Escala uniforme (sin distorsión). Botica y Posada se agrandan para que la puerta jugable quepa bajo las ventanas
    private static readonly HouseSpec[] Houses =
    {
        new HouseSpec("Environment/Village_Exterior_Structures/House1_Facade_Apothecary", 1.6f),
        new HouseSpec("Environment/Village_Exterior_Structures/House2_Facade_Tavern", 1.6f),
        new HouseSpec("Environment/Village_Exterior_Structures/House3_Facade_Abandoned", 2.12f),
        new HouseSpec("Environment/Village_Exterior_Structures/House4_Facade_Manor", 2.0f),
        new HouseSpec("Environment/Village_Exterior_Structures/House_Deco_Z4_01", 1.27f),
        new HouseSpec("Environment/Village_Exterior_Structures/House_Deco_Z5_03", 1.26f),
    };

    private struct DoorSpec
    {
        public string name; public float x; public string surface; public float height;
        public DoorSpec(string n, float x, string surface, float h) { name = n; this.x = x; this.surface = surface; height = h; }
    }

    // x alineada con la puerta pintada de cada fachada; surface = plataforma donde apoya (null = calle)
    private static readonly DoorSpec[] ExteriorDoors =
    {
        new DoorSpec("Door_Ext_House1_Ground", 16.745f, null, 1.9f),
        new DoorSpec("Door_Ext_House1_Balcony", 15.5f, "Environment/Village_Exterior_Structures/Balcony_Ext_House1", 1.9f),
        new DoorSpec("Door_Ext_House2_Ground", 45.0f, null, 1.85f),
        new DoorSpec("Door_Ext_House2_Balcony", 45.0f, "Environment/Zone_2_PuestosBajos/RoofWalk_Z2", 1.9f),
        new DoorSpec("Door_Ext_Z3_Quarantine", 62.8f, null, 2.05f),
        new DoorSpec("Door_Ext_House3_Ground", 110.06f, null, 2.2f),
        new DoorSpec("Door_Ext_House3_Roof", 109.67f, "Environment/Zone_4_CallejonGato/Roof_Z4_OverAlley", 2.0f),
        new DoorSpec("Door_Ext_House4_Ground", 149.57f, null, 2.2f),
        new DoorSpec("Door_Ext_House4_Balcony", 149.13f, "Environment/Zone_5_GranMercado/Roof_Z5_Mid_2", 2.0f),
        new DoorSpec("Door_Ext_Z6_Sanctum", 173.25f, null, 1.9f),
    };

    private static readonly (string ladder, string bottom, string top)[] Ladders =
    {
        ("Ladder_Ext_Z1_Balcony", null, "Environment/Village_Exterior_Structures/Balcony_Ext_House1"),
        ("Ladder_Ext_Z4_RoofExit", null, "Environment/Zone_4_CallejonGato/Roof_Z4_OverAlley"),
        ("Ladder_Ext_Z5_East", null, "Environment/Zone_5_GranMercado/Roof_Z5_Mid_2"),
        ("Ladder_Ext_Z5_HighTower", "Environment/Zone_5_GranMercado/Roof_Z5_Low_1", "Environment/Zone_5_GranMercado/Roof_Z5_HighWalk"),
    };

    // ------------------------------------------------------------------ Menús

    [MenuItem("Tools/Nivel 2/Puestos y puertas/1. Completar y reacomodar puestos")]
    public static void StallsMenu() => Run("Puestos", FixStalls);

    [MenuItem("Tools/Nivel 2/Puestos y puertas/2. Animar puestos (desincronizados)")]
    public static void AnimateMenu() => Run("Animación", AnimateStalls);

    [MenuItem("Tools/Nivel 2/Puestos y puertas/3. Adaptar puertas a las casas")]
    public static void DoorsMenu() => Run("Puertas", FixDoors);

    [MenuItem("Tools/Nivel 2/Puestos y puertas/4. Apoyar postes y props en el suelo")]
    public static void GroundMenu() => Run("Suelo", GroundProps);

    internal static void Run(string label, System.Action<StringBuilder> step)
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
        {
            Debug.LogError("[Level2StallsDoorsFixer] Abre Assets/Scenes/Level2.unity como escena activa: la herramienta no toca otras escenas.");
            return;
        }
        StringBuilder log = new StringBuilder();
        step(log);
        Physics2D.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log($"[Level2StallsDoorsFixer] {label}:\n{log}");
    }

    // ------------------------------------------------------------------ C) Puestos completos y reacomodados

    public static void FixStalls(StringBuilder log)
    {
        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Level2/Market_Stall_Complete.prefab");
        Transform templatePosts = template.transform.Find("PostsAndCounter");
        Transform templateRoof = template.transform.Find("AwningRoof");
        Transform templateLantern = template.transform.Find("Hanging_Lantern_Visual");

        foreach (StallSpec spec in StreetStalls)
        {
            GameObject stall = GameObject.Find(spec.path);
            if (stall == null) { log.AppendLine($"  [FALTA] {spec.path}"); continue; }

            Transform posts = Child(stall.transform, "PostsAndCounter");
            SetSprite(posts, Sprite("Placeholder_MarketStall_WoodPosts"), 1, true);
            posts.localPosition = Vector3.zero;
            posts.localScale = Vector3.one;
            CopyComponentsIfMissing(templatePosts.gameObject, posts.gameObject);
            posts.gameObject.layer = LayerMask.NameToLayer("Ground"); // la bruja solo detecta suelo en esta capa

            Transform roof = Child(stall.transform, "AwningRoof");
            SetSprite(roof, Awning(spec.awning), 2, false); // el dibujo lo hace Awning_Sway_Visual
            roof.localPosition = new Vector3(0f, AwningLocalY, 0f);
            roof.localScale = Vector3.one;
            CopyComponentsIfMissing(templateRoof.gameObject, roof.gameObject);
            roof.gameObject.layer = LayerMask.NameToLayer("Ground");

            BuildStallDressing(stall.transform, 0f, 0f, 1f, spec, templateLantern);
            DestroyChild(stall.transform, "SupportColumns_Base");

            // La base opaca de los postes queda exactamente sobre el empedrado
            stall.transform.position = new Vector3(spec.x, 1.12f, 0f);
            Physics2D.SyncTransforms();
            Level2GroundingTool.TryOpaqueBottom(posts.GetComponent<SpriteRenderer>(), out float bottom);
            stall.transform.position += new Vector3(0f, GroundY - bottom, 0f);
            log.AppendLine($"  {stall.name}: x={spec.x:F2} toldo {spec.awning}, completo y apoyado");
        }

        foreach (StallSpec spec in ElevatedStalls)
        {
            GameObject platform = GameObject.Find(spec.path);
            if (platform == null) { log.AppendLine($"  [FALTA] {spec.path}"); continue; }
            float topLocal = platform.GetComponent<Collider2D>().bounds.max.y - platform.transform.position.y;
            float lx = spec.x - platform.transform.position.x;

            Transform posts = Child(platform.transform, "Elevated_CanopyPosts");
            SetSprite(posts, Sprite("Placeholder_MarketStall_WoodPosts"), 1, true);
            posts.localScale = new Vector3(spec.scale, spec.scale, 1f);
            posts.localPosition = new Vector3(lx, topLocal + spec.scale, 0f);
            Physics2D.SyncTransforms();
            Level2GroundingTool.TryOpaqueBottom(posts.GetComponent<SpriteRenderer>(), out float bottom);
            posts.localPosition += new Vector3(0f, topLocal + platform.transform.position.y - bottom, 0f);

            BuildStallDressing(platform.transform, lx, posts.localPosition.y, spec.scale, spec, templateLantern);
            DestroyChild(platform.transform, "SupportColumns_Base");
            log.AppendLine($"  {platform.name}: puesto elevado en x={spec.x:F2} (escala {spec.scale}) sobre la plataforma");
        }

        // Restos desplazados de puestos antiguos que ya forman parte de su puesto reconstruido
        foreach (Transform t in GameObject.Find("Environment").GetComponentsInChildren<Transform>(true).ToArray())
        {
            if (t != null && t.name == "Awning_CanalOverhead") { Object.DestroyImmediate(t.gameObject); log.AppendLine("  Retirado Awning_CanalOverhead suelto"); }
        }

        foreach (var (chestPath, stallPath) in RoofChests)
        {
            GameObject chest = GameObject.Find(chestPath);
            Collider2D roof = GameObject.Find(stallPath)?.transform.Find("AwningRoof")?.GetComponent<Collider2D>();
            if (chest == null || roof == null) continue;
            chest.transform.position = new Vector3(roof.bounds.center.x, chest.transform.position.y, 0f);
            chest.GetComponent<SpriteRenderer>().sortingOrder = 3; // por delante del borde del toldo sobre el que descansa
            Physics2D.SyncTransforms();
            Ground(chest.transform, chest.GetComponent<SpriteRenderer>(), log, splitPadding: true);
            log.AppendLine($"  {chest.name} acompaña a su puesto (x={roof.bounds.center.x:F2})");
        }

        // Un cofre junto a un puesto se dibuja por delante de la mercancía y el farol, nunca tapado
        Physics2D.SyncTransforms();
        List<Bounds> stallPieces = StreetStalls.Concat(ElevatedStalls).Select(s => GameObject.Find(s.path)).Where(g => g != null)
            .SelectMany(g => g.GetComponentsInChildren<SpriteRenderer>()).Where(r => r.enabled && r.transform.parent != null && r.name != "AwningRoof")
            .Select(r => r.bounds).ToList();
        foreach (Level2Chest chest in Object.FindObjectsByType<Level2Chest>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            SpriteRenderer cr = chest.GetComponent<SpriteRenderer>();
            if (stallPieces.Any(b => b.Intersects(cr.bounds))) cr.sortingOrder = 3;
        }

        foreach (var (zone, name, fromX, toX) in PropMoves)
        {
            Transform prop = FindProp(zone, name, fromX);
            if (prop == null) { log.AppendLine($"  [FALTA] {zone}/{name} cerca de x={fromX}"); continue; }
            prop.position = new Vector3(toX, prop.position.y, prop.position.z);
            log.AppendLine($"  {zone}/{name}: x {fromX:F2} → {toX:F2}");
        }

        Transform crates = FindProp("Zone_3_PlazaCentral", "Crates_Stack", 90.29f);
        if (crates != null) { crates.localScale = new Vector3(0.63f, 0.63f, 1f); log.AppendLine("  Zone_3/Crates_Stack: escala uniforme 0.63"); }

        foreach (var (path, toX) in VillagerMoves)
        {
            GameObject v = GameObject.Find(path);
            if (v == null) continue;
            v.transform.position = new Vector3(toX, v.transform.position.y, 0f);
            log.AppendLine($"  {v.name}: punto de inicio x={toX:F2} (fuera del puesto)");
        }
    }

    /// <summary>Toldo, farol sobre el mostrador, mercancía del mostrador y colgante con pivote arriba.</summary>
    private static void BuildStallDressing(Transform root, float lx, float postsY, float s, StallSpec spec, Transform templateLantern)
    {
        Transform awning = Child(root, "Awning_Sway_Visual");
        SetSprite(awning, Awning(spec.awning), 2, true);
        awning.localScale = new Vector3(s, s, 1f);
        awning.localRotation = Quaternion.identity;
        awning.localPosition = new Vector3(lx, postsY + AwningLocalY * s, 0f);

        Transform lantern = Child(root, "Hanging_Lantern_Visual");
        SetSprite(lantern, Sprite("Placeholder_Market_Lantern"), 2, true);
        lantern.localScale = new Vector3(0.72f * s, 0.72f * s, 1f);
        lantern.localRotation = Quaternion.identity;
        lantern.localPosition = new Vector3(lx - 1.15f * s, postsY + (PostsCounterTop + 0.77f) * s, 0f);
        CopyComponentsIfMissing(templateLantern.gameObject, lantern.gameObject);

        Transform wares = Child(root, "Counter_Wares_Visual");
        SetSprite(wares, Sprite(spec.crateLeft ? "Placeholder_Market_Crate" : "Placeholder_Market_Barrel"), 2, true);
        wares.localScale = new Vector3(0.48f * s, 0.48f * s, 1f);
        wares.localPosition = new Vector3(lx + (spec.crateLeft ? -0.55f : 0.55f) * s, postsY + (PostsCounterTop + 0.23f) * s, 0f);

        // Colgante: el pivote cuelga del borde inferior del toldo para oscilar como un péndulo
        Transform pivot = Child(root, "Hanging_Wares_Pivot");
        pivot.localScale = Vector3.one;
        pivot.localRotation = Quaternion.identity;
        pivot.localPosition = new Vector3(lx + 1.15f * s, postsY + (AwningLocalY - 0.5f) * s, 0f);
        Transform hanging = root.Find("Hanging_Wares_Visual");
        if (hanging != null) hanging.SetParent(pivot, true);
        hanging = Child(pivot, "Hanging_Wares_Visual");
        SetSprite(hanging, Sprite("Placeholder_Ingredient_" + spec.wares), 2, true);
        hanging.localScale = new Vector3(0.65f * s, 0.65f * s, 1f);
        hanging.localRotation = Quaternion.identity;
        hanging.localPosition = new Vector3(0f, -0.28f * s, 0f);

        MarketStallAnimator anim = root.GetComponent<MarketStallAnimator>();
        if (anim == null) anim = root.gameObject.AddComponent<MarketStallAnimator>();
    }

    // ------------------------------------------------------------------ A) Animación desincronizada

    public static void AnimateStalls(StringBuilder log)
    {
        // Ángulo áureo: fases repartidas sin repetirse; velocidad dentro de ±10 %
        int i = 0;
        foreach (StallSpec spec in StreetStalls.Concat(ElevatedStalls))
        {
            GameObject go = GameObject.Find(spec.path);
            MarketStallAnimator anim = go != null ? go.GetComponent<MarketStallAnimator>() : null;
            if (anim == null) continue;
            float phase = 0.35f + (i * 2.39996f) % (2f * Mathf.PI);
            float speed = 0.9f + ((i * 0.137f) % 0.2f);
            anim.Configure(go.transform.Find("Awning_Sway_Visual"), new[] { go.transform.Find("Hanging_Wares_Pivot") }, phase, speed);
            SerializedObject so = new SerializedObject(anim);
            so.FindProperty("awningSwayAngle").floatValue = 2.4f;
            so.FindProperty("awningScalePulse").floatValue = 0.03f;
            so.FindProperty("hangingSwingAngle").floatValue = 6f;
            so.FindProperty("phaseOffset").floatValue = phase;
            so.FindProperty("speedMultiplier").floatValue = speed;
            so.ApplyModifiedPropertiesWithoutUndo();

            LanternFlicker flicker = go.transform.Find("Hanging_Lantern_Visual")?.GetComponent<LanternFlicker>();
            log.AppendLine($"  {go.name}: fase {phase:F2} rad, velocidad ×{speed:F2}, farol {(flicker != null ? "con parpadeo" : "sin luz")}");
            i++;
        }

        // Las pasarelas sin puesto no necesitan animador (quedaban con referencias vacías)
        foreach (MarketStallAnimator anim in Object.FindObjectsByType<MarketStallAnimator>(FindObjectsSortMode.None))
        {
            if (anim.AwningVisual == null)
            {
                log.AppendLine($"  Retirado animador vacío de {anim.name}");
                Object.DestroyImmediate(anim);
            }
        }
    }

    // ------------------------------------------------------------------ B) Puertas adaptadas a las casas

    public static void FixDoors(StringBuilder log)
    {
        foreach (HouseSpec h in Houses)
        {
            GameObject house = GameObject.Find(h.path);
            if (house == null) continue;
            Vector3 oldScale = house.transform.localScale;
            float k = h.scale / oldScale.y;
            Vector3 basePoint = new Vector3(house.transform.position.x, GroundY, 0f);

            house.transform.localScale = new Vector3(h.scale, h.scale, 1f);
            house.transform.position = basePoint;
            log.AppendLine($"  {house.name}: escala ({oldScale.x:F2},{oldScale.y:F2}) → {h.scale:F2} uniforme, base en el suelo");

            // El balcón de la Botica es parte del dibujo de la fachada: crece con ella
            if (house.name == "House1_Facade_Apothecary")
            {
                Transform balcony = GameObject.Find("Environment/Village_Exterior_Structures/Balcony_Ext_House1").transform;
                balcony.position = basePoint + (balcony.position - basePoint) * k;
                balcony.localScale = balcony.localScale * k;
                Physics2D.SyncTransforms();
                log.AppendLine($"  Balcony_Ext_House1: piso a y={balcony.GetComponent<Collider2D>().bounds.max.y:F3}");
            }
        }
        Physics2D.SyncTransforms();

        Transform ext = GameObject.Find("Environment/Village_Exterior_Structures").transform;
        foreach (DoorSpec d in ExteriorDoors)
        {
            Transform door = ext.Find(d.name);
            if (door == null) { log.AppendLine($"  [FALTA] {d.name}"); continue; }
            float surface = d.surface == null ? GroundY : GameObject.Find(d.surface).GetComponent<Collider2D>().bounds.max.y;
            FitDoor(door.GetComponent<HouseDoor>(), d.x, surface, d.height);
            log.AppendLine($"  {d.name}: alto {d.height:F2} u, x={d.x:F2}, base y={surface:F3}");
        }

        // Puertas interiores al mismo alto jugable (2.0 u) sobre su piso
        foreach (HouseDoor door in Object.FindObjectsByType<HouseDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (door.OwningInterior == null) continue;
            FitDoor(door, door.transform.position.x, door.FloorSurfaceY, 2.0f);
        }

        // Escalera de la Botica hasta su balcón agrandado
        Transform ladder = ext.Find("Ladder_Ext_Z1_Balcony");
        float h1Scale = Houses[0].scale / 1.22f;
        ladder.position = new Vector3(15.5f + (13.6f - 15.5f) * h1Scale, ladder.position.y, 0f);
        float balconyTop = GameObject.Find("Environment/Village_Exterior_Structures/Balcony_Ext_House1").GetComponent<Collider2D>().bounds.max.y;
        FitLadder(ladder, GroundY, balconyTop);
        log.AppendLine($"  Ladder_Ext_Z1_Balcony: x={ladder.position.x:F2}, tope y={balconyTop:F3}");
    }

    private static void FitDoor(HouseDoor door, float x, float surface, float height)
    {
        SpriteRenderer sr = door.GetComponent<SpriteRenderer>();
        SerializedObject so = new SerializedObject(door);
        bool locked = so.FindProperty("isLocked").boolValue || so.FindProperty("permanentlyBarred").boolValue || so.FindProperty("requiresAllIngredients").boolValue;
        Sprite closed = so.FindProperty("closedSprite").objectReferenceValue as Sprite;
        Sprite lockedSprite = so.FindProperty("lockedSprite").objectReferenceValue as Sprite;
        // En el editor se ve como en juego: cerrada (o con candado), no la variante abierta
        if (door.OwningInterior == null) sr.sprite = locked && lockedSprite != null ? lockedSprite : closed != null ? closed : sr.sprite;
        sr.color = new Color(1f, DoorTint, DoorTint - 0.08f, 1f);

        float s = height / sr.sprite.bounds.size.y;
        door.transform.localScale = new Vector3(s, s, 1f);
        door.transform.position = new Vector3(x, surface + height * 0.5f, 0f);
        Physics2D.SyncTransforms();
        door.transform.position += new Vector3(0f, PaddedBaseOffset(sr, surface), 0f);
        so.FindProperty("floorSurfaceY").floatValue = surface;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ------------------------------------------------------------------ D) Postes y props en el suelo

    public static void GroundProps(StringBuilder log)
    {
        Physics2D.SyncTransforms();
        List<Transform> props = new List<Transform>();
        foreach (Transform zone in GameObject.Find("Environment").transform)
        {
            if (!zone.name.StartsWith("Zone_")) continue;
            props.AddRange(zone.Cast<Transform>().Where(t => new[] { "Market_Lantern", "Crates_Stack", "Barrel", "Market_Cart", "Goal_Gate_Visual" }.Any(p => t.name.StartsWith(p))));
        }
        foreach (Transform prop in props) Ground(prop, prop.GetComponent<SpriteRenderer>(), log);

        // Postes de los puestos de calle (el puesto entero sube o baja)
        foreach (StallSpec spec in StreetStalls)
        {
            Transform stall = GameObject.Find(spec.path)?.transform;
            if (stall != null) Ground(stall, stall.Find("PostsAndCounter")?.GetComponent<SpriteRenderer>(), log);
        }

        // Cofres sobre su suelo, toldo o plataforma
        foreach (Level2Chest chest in Object.FindObjectsByType<Level2Chest>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Ground(chest.transform, chest.GetComponent<SpriteRenderer>(), log, splitPadding: true);
        }

        foreach (var (name, bottomPath, topPath) in Ladders)
        {
            Transform ladder = GameObject.Find("Environment/Village_Exterior_Structures/" + name)?.transform;
            if (ladder == null) continue;
            float bottom = bottomPath == null ? GroundY : GameObject.Find(bottomPath).GetComponent<Collider2D>().bounds.max.y;
            float top = GameObject.Find(topPath).GetComponent<Collider2D>().bounds.max.y;
            FitLadder(ladder, bottom, top);
            log.AppendLine($"  {name}: de y={bottom:F3} a y={top:F3}");
        }
    }

    internal static void Ground(Transform t, SpriteRenderer sr, StringBuilder log, bool splitPadding = false)
    {
        if (sr == null || !Level2GroundingTool.TryOpaqueBottom(sr, out float bottom)) return;
        if (!Level2GroundingTool.TryFindSurface(sr.bounds.center.x, bottom, t, out float surface)) return;
        float dy = splitPadding ? PaddedBaseOffset(sr, surface) : surface - bottom;
        if (Mathf.Abs(dy) < 0.0005f) return;
        t.position += new Vector3(0f, dy, 0f);
        Physics2D.SyncTransforms();
        log.AppendLine($"  {PathOf(t)}: {(dy > 0 ? "subido" : "bajado")} {Mathf.Abs(dy):F3} u hasta y={surface:F3}");
    }

    /// <summary>
    /// Cofres y puertas tienen ~0.03 u de margen transparente bajo la madera. Los verificadores previos del Nivel 2
    /// miden el borde del rect y Level2GroundingTool la fila opaca: centrar la superficie entre ambos deja los dos
    /// criterios dentro de ±0.02 u (la madera queda a ≤1 px del suelo).
    /// </summary>
    private static float PaddedBaseOffset(SpriteRenderer sr, float surface)
    {
        if (!Level2GroundingTool.TryOpaqueBottom(sr, out float opaque)) return surface - sr.bounds.min.y;
        return surface - (opaque + sr.bounds.min.y) * 0.5f;
    }

    /// <summary>Misma construcción que las escaleras exteriores de Level2CompleteFixer, con el tope sobre su plataforma.</summary>
    private static void FitLadder(Transform ladder, float bottomY, float topY)
    {
        float height = topY - bottomY;
        ladder.position = new Vector3(ladder.position.x, bottomY + height * 0.5f, 0f);
        SpriteRenderer sr = ladder.GetComponent<SpriteRenderer>();
        if (sr != null && sr.drawMode != SpriteDrawMode.Simple) sr.size = new Vector2(0.85f, height);
        BoxCollider2D trig = ladder.GetComponent<BoxCollider2D>();
        trig.offset = Vector2.zero;
        trig.size = new Vector2(1.25f, height + 0.5f);
        SerializedObject so = new SerializedObject(ladder.GetComponent<LadderInteractable>());
        so.FindProperty("bottomY").floatValue = bottomY;
        so.FindProperty("topY").floatValue = topY;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ------------------------------------------------------------------ Utilidades

    internal static Transform Child(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null) return t;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void DestroyChild(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null) Object.DestroyImmediate(t.gameObject);
    }

    internal static void SetSprite(Transform t, Sprite sprite, int order, bool enabled)
    {
        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        if (sr == null) sr = t.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Gameplay";
        sr.sortingOrder = order;
        sr.color = Color.white;
        sr.enabled = enabled;
    }

    private static void CopyComponentsIfMissing(GameObject source, GameObject target)
    {
        foreach (Component c in source.GetComponents<Component>())
        {
            if (c is Transform || c is SpriteRenderer || target.GetComponent(c.GetType()) != null) continue;
            ComponentUtility.CopyComponent(c);
            ComponentUtility.PasteComponentAsNew(target);
        }
    }

    private static Sprite Sprite(string file)
    {
        return AssetDatabase.LoadAllAssetsAtPath(Art + file + ".png").OfType<Sprite>().First();
    }

    private static Sprite Awning(string color) => Sprite("Placeholder_MarketStall_Awning_" + color);

    private static Transform FindProp(string zone, string name, float nearX)
    {
        Transform z = GameObject.Find("Environment/" + zone)?.transform;
        if (z == null) return null;
        return z.Cast<Transform>().Where(t => t.name == name && Mathf.Abs(t.position.x - nearX) < 0.6f)
            .OrderBy(t => Mathf.Abs(t.position.x - nearX)).FirstOrDefault();
    }

    private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
}
