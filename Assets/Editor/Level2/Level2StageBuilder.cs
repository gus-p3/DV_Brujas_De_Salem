using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Constructor integral del escenario jugable del Nivel 2: Acto 2 — Pueblo y Mercado.
/// Genera prefabs modulares en Assets/Prefabs/Level2/, construye el escenario completo en Level2.unity
/// con 6 zonas, >25 plataformas (alta densidad), 3 capas de paralaje, iluminación URP 2D,
/// pasaje para gato, escondites, checkpoints, KillZones y marcadores de integración.
/// NO modifica archivos protegidos ni configuración global.
/// </summary>
public static class Level2StageBuilder
{
    public const string ScenePath = "Assets/Scenes/Level2.unity";
    public const string PrefabDir = "Assets/Prefabs/Level2";
    public const string ArtDir = "Assets/Art/Environment/Level2";

    [MenuItem("Brujas/Nivel 2/Construir Escenario Pueblo y Mercado")]
    public static void BuildFullLevel2()
    {
        Directory.CreateDirectory(PrefabDir);
        ConfigureImportedSprites();
        BuildPrefabs();

        // 1. Abrir Level2.unity
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // 2. Limpiar contenido previo de Environment, Gameplay, NPCs, Ingredients
        Transform envRoot = GameObject.Find("Environment")?.transform;
        Transform gameplayRoot = GameObject.Find("Gameplay")?.transform;
        Transform npsRoot = GameObject.Find("NPCs")?.transform;
        Transform ingRoot = GameObject.Find("Ingredients")?.transform;

        if (envRoot == null) envRoot = new GameObject("Environment").transform;
        if (gameplayRoot == null) gameplayRoot = new GameObject("Gameplay").transform;
        if (npsRoot == null) npsRoot = new GameObject("NPCs").transform;
        if (ingRoot == null) ingRoot = new GameObject("Ingredients").transform;

        // Limpiar hijos anteriores de las raíces de entorno
        ClearChildren(envRoot);
        ClearChildren(npsRoot);
        ClearChildren(ingRoot);

        // Limpiar solo elementos de entorno/suelo/checkpoints de Gameplay, conservando Witch
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform ch in gameplayRoot)
        {
            if (ch.name != "Witch") toDestroy.Add(ch.gameObject);
        }
        foreach (var go in toDestroy) Object.DestroyImmediate(go);

        // Asegurar Witch en posición inicial
        GameObject witch = GameObject.Find("Witch");
        if (witch != null)
        {
            witch.transform.position = new Vector3(1.5f, 0.17f, 0f);
        }

        int groundLayer = LayerMask.NameToLayer("Ground");

        // -------------------------------------------------------------
        // A. PARALAJE (3 CAPAS MÍNIMO)
        // -------------------------------------------------------------
        GameObject parallaxRoot = new GameObject("Parallax");
        parallaxRoot.transform.SetParent(envRoot, false);

        // Capa 1: Cielo lejano (Background, order 0, factor 0.9)
        Sprite skySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Env_Sky_Far.png");
        if (skySprite != null)
        {
            GameObject sky = new GameObject("Sky_Far");
            sky.transform.SetParent(parallaxRoot.transform, false);
            sky.transform.position = new Vector3(0f, 4.5f, 0f);
            ParallaxUtility.SetupThreeCopies(sky, skySprite, "Background", 0);
            ParallaxLayer plSky = sky.GetComponent<ParallaxLayer>();
            plSky.Setup(0.9f, true, true);
        }

        // Capa 2: Árboles lejanos / Bosque límite (Midground, order 0, factor 0.65)
        Sprite treesSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Env_Trees_Mid.png");
        if (treesSprite != null)
        {
            GameObject trees = new GameObject("Trees_Mid");
            trees.transform.SetParent(parallaxRoot.transform, false);
            trees.transform.position = new Vector3(0f, 2.0f, 0f);
            ParallaxUtility.SetupThreeCopies(trees, treesSprite, "Midground", 0);
            ParallaxLayer plTrees = trees.GetComponent<ParallaxLayer>();
            plTrees.Setup(0.65f, true, true);
        }

        // Capa 3: Silueta de casas coloniales y techos del pueblo (Midground, order 2, factor 0.4)
        Sprite villageSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Env_Village_Mid.png");
        if (villageSprite != null)
        {
            GameObject village = new GameObject("Village_Mid");
            village.transform.SetParent(parallaxRoot.transform, false);
            village.transform.position = new Vector3(0f, 1.2f, 0f);
            ParallaxUtility.SetupThreeCopies(village, villageSprite, "Midground", 2);
            ParallaxLayer plVillage = village.GetComponent<ParallaxLayer>();
            plVillage.Setup(0.4f, true, true);
        }

        // -------------------------------------------------------------
        // B. ILUMINACIÓN URP 2D (AMBIENTACIÓN CÁLIDA NOCTURNA)
        // -------------------------------------------------------------
        GameObject lightRoot = new GameObject("Lighting");
        lightRoot.transform.SetParent(envRoot, false);

        // Luz global tenue azul noche
        GameObject globalLightObj = new GameObject("Global Light 2D");
        globalLightObj.transform.SetParent(lightRoot.transform, false);
        Light2D globalLight = globalLightObj.AddComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.color = new Color(0.38f, 0.42f, 0.65f, 1f);
        globalLight.intensity = 0.65f;

        // Luz lunar suave cenital
        GameObject moonLightObj = new GameObject("Moon Light 2D");
        moonLightObj.transform.SetParent(lightRoot.transform, false);
        moonLightObj.transform.position = new Vector3(50f, 12f, 0f);
        Light2D moonLight = moonLightObj.AddComponent<Light2D>();
        moonLight.lightType = Light2D.LightType.Point;
        moonLight.color = new Color(1f, 0.95f, 0.75f, 1f);
        moonLight.intensity = 0.5f;
        moonLight.pointLightOuterRadius = 15f;

        // Cargar sprites para props
        Sprite cobbleSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_Cobble_Ground.png");
        Sprite awningRed = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_MarketStall_Awning_Red.png");
        Sprite awningGold = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_MarketStall_Awning_Gold.png");
        Sprite awningLarge = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_MarketStall_Awning_Large.png");
        Sprite stallPosts = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_MarketStall_WoodPosts.png");
        Sprite cartSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_Market_Cart.png");
        Sprite crateSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_Market_Crate.png");
        Sprite cratesStackSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_Market_Crates_Stack.png");
        Sprite barrelSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_Market_Barrel.png");
        Sprite lanternSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_Market_Lantern.png");
        Sprite roofSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_Market_RoofPlatform.png");
        Sprite tunnelSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_Market_Canopy_CatTunnel.png");
        Sprite gateSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Env_Goal_Gate.png");
        GameObject checkpointPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Checkpoint.prefab");

        // -------------------------------------------------------------
        // C. CONSTRUCCIÓN DE LAS 6 ZONAS
        // -------------------------------------------------------------

        // --- ZONA 1: Entrada al Pueblo (x = 0 .. 28) ---
        Transform z1 = CreateSubRoot(envRoot, "Zone_1_Entrada");
        CreateCobbleGround(gameplayRoot, "Ground_Z1", -5f, 34f, groundLayer, cobbleSprite); // x = -5 .. 29
        CreateLantern(z1, new Vector3(8f, 1.25f, 0f), lanternSprite);
        CreateStall(z1, "Stall_Z1_1", new Vector2(13f, 1.2f), stallPosts, awningRed, true, groundLayer);
        CreateCratesStack(z1, new Vector2(19.5f, 0.75f), cratesStackSprite, groundLayer);
        CreateBarrel(z1, new Vector2(22f, 0.5f), barrelSprite, groundLayer);
        CreateLantern(z1, new Vector3(24f, 1.25f, 0f), lanternSprite);
        InstantiateCP(gameplayRoot, checkpointPrefab, "Checkpoint_Z1", new Vector3(26f, 0.17f, 0f));

        // Marcadores Z1
        CreateMarker(npsRoot, "Guard_Spawn_Z1", new Vector3(16f, 0.17f, 0f));
        CreateMarker(npsRoot, "Waypoint_Z1_A", new Vector3(11f, 0.17f, 0f));
        CreateMarker(npsRoot, "Waypoint_Z1_B", new Vector3(21f, 0.17f, 0f));
        CreateMarker(ingRoot, "Ingredient_Spawn_Z1_Mushroom", new Vector3(13f, 2.6f, 0f)); // sobre el toldo

        // --- ZONA 2: Calle de Puestos Bajos con Toldos (x = 28 .. 60) ---
        Transform z2 = CreateSubRoot(envRoot, "Zone_2_PuestosBajos");
        CreateCobbleGround(gameplayRoot, "Ground_Z2", 28.5f, 32.5f, groundLayer, cobbleSprite); // x = 28.5 .. 61
        CreateLantern(z2, new Vector3(31f, 1.25f, 0f), lanternSprite);
        CreateStall(z2, "Stall_Z2_1", new Vector2(35f, 1.2f), stallPosts, awningGold, true, groundLayer);
        CreateCart(z2, new Vector2(40f, 0.75f), cartSprite, groundLayer);
        CreateStall(z2, "Stall_Z2_2", new Vector2(45f, 1.2f), stallPosts, awningRed, true, groundLayer);
        CreateRoofPlatform(z2, "RoofWalk_Z2", new Vector2(45f, 3.8f), roofSprite, groundLayer); // segundo piso
        CreateHideout(gameplayRoot, "Hideout_Z2_Alley", new Vector3(45f, 0.9f, 0f), new Vector2(3f, 1.8f));
        CreateBarrel(z2, new Vector2(50f, 0.5f), barrelSprite, groundLayer);
        CreateStall(z2, "Stall_Z2_3", new Vector2(54f, 1.2f), stallPosts, awningGold, true, groundLayer);
        CreateLantern(z2, new Vector3(57f, 1.25f, 0f), lanternSprite);
        InstantiateCP(gameplayRoot, checkpointPrefab, "Checkpoint_Z2", new Vector3(59f, 0.17f, 0f));

        // Marcadores Z2
        CreateMarker(npsRoot, "Guard_Spawn_Z2", new Vector3(38f, 0.17f, 0f));
        CreateMarker(npsRoot, "Waypoint_Z2_A", new Vector3(33f, 0.17f, 0f));
        CreateMarker(npsRoot, "Waypoint_Z2_B", new Vector3(51f, 0.17f, 0f));
        CreateMarker(npsRoot, "Villager_Spawn_Z2", new Vector3(48f, 0.17f, 0f));
        CreateMarker(ingRoot, "Ingredient_Spawn_Z2_Spider", new Vector3(45f, 4.3f, 0f)); // en el techo alto

        // --- ZONA 3: Plaza Central con Carretas y Brecha Canal (x = 60 .. 95) ---
        Transform z3 = CreateSubRoot(envRoot, "Zone_3_PlazaCentral");
        CreateCobbleGround(gameplayRoot, "Ground_Z3_A", 60.5f, 14f, groundLayer, cobbleSprite); // x = 60.5 .. 74.5
        // Brecha / Canal en x = 74.5 .. 81.5 (7u de hueco con KillZone abajo)
        CreateCobbleGround(gameplayRoot, "Ground_Z3_B", 81.5f, 14.5f, groundLayer, cobbleSprite); // x = 81.5 .. 96.0

        // Plataformas escalonadas y carreta en la brecha
        CreateCratesStack(z3, new Vector2(65f, 0.75f), cratesStackSprite, groundLayer);
        CreateStall(z3, "Stall_Z3_Left", new Vector2(70f, 1.2f), stallPosts, awningRed, true, groundLayer);
        CreateRoofPlatform(z3, "Roof_Z3_LeftHigh", new Vector2(71f, 4.0f), roofSprite, groundLayer);

        // Carreta puente en el canal (sólida a y = 1.35)
        CreateCart(z3, new Vector2(78f, 0.85f), cartSprite, groundLayer);
        CreateOneWayAwning(z3, "Awning_CanalOverhead", new Vector2(78f, 3.6f), awningLarge, groundLayer);

        // KillZone profunda en el canal
        CreateKillZone(gameplayRoot, "KillZone_Canal", new Vector3(78f, -4f, 0f), new Vector2(8f, 4f));

        // Lado derecho de la plaza
        CreateStall(z3, "Stall_Z3_Right", new Vector2(86f, 1.2f), stallPosts, awningGold, true, groundLayer);
        CreateCratesStack(z3, new Vector2(90f, 0.75f), cratesStackSprite, groundLayer);
        CreateRoofPlatform(z3, "Roof_Z3_RightHigh", new Vector2(87f, 4.2f), roofSprite, groundLayer);
        CreateLantern(z3, new Vector3(64f, 1.25f, 0f), lanternSprite);
        CreateLantern(z3, new Vector3(84f, 1.25f, 0f), lanternSprite);
        InstantiateCP(gameplayRoot, checkpointPrefab, "Checkpoint_Z3", new Vector3(93.5f, 0.17f, 0f));

        // Marcadores Z3
        CreateMarker(npsRoot, "Guard_Spawn_Z3_1", new Vector3(67f, 0.17f, 0f));
        CreateMarker(npsRoot, "Waypoint_Z3_1A", new Vector3(62f, 0.17f, 0f));
        CreateMarker(npsRoot, "Waypoint_Z3_1B", new Vector3(72f, 0.17f, 0f));
        CreateMarker(npsRoot, "Guard_Spawn_Z3_2", new Vector3(88f, 0.17f, 0f));
        CreateMarker(npsRoot, "Waypoint_Z3_2A", new Vector3(83f, 0.17f, 0f));
        CreateMarker(npsRoot, "Waypoint_Z3_2B", new Vector3(92f, 0.17f, 0f));
        CreateMarker(npsRoot, "Villager_Spawn_Z3", new Vector3(85f, 0.17f, 0f));
        CreateMarker(ingRoot, "Ingredient_Spawn_Z3_Flower", new Vector3(78f, 4.2f, 0f)); // sobre el toldo del canal

        // --- ZONA 4: Callejón Estrecho con Pasaje Solo Gato (x = 95 .. 125) ---
        Transform z4 = CreateSubRoot(envRoot, "Zone_4_CallejonGato");
        CreateCobbleGround(gameplayRoot, "Ground_Z4", 95.5f, 30.5f, groundLayer, cobbleSprite); // x = 95.5 .. 126
        CreateLantern(z4, new Vector3(98f, 1.25f, 0f), lanternSprite);

        // Estructura de Callejón Techado con pasaje bajo para gato (x = 104 .. 116)
        // Techo sólido con collider inferior a y = 1.22u (altura libre 1.05u desde el césped/suelo a 0.17u)
        CreateCatTunnelStructure(gameplayRoot, z4, "CatPassage_MarketAlley", 110f, 12f, groundLayer, tunnelSprite, roofSprite);
        CreateHideout(gameplayRoot, "Hideout_Z4_TunnelShadow", new Vector3(110f, 0.6f, 0f), new Vector2(8f, 1.2f));

        // Ruta superior alternativa sobre los techos del callejón (accesible por salto escalonado o vuelo)
        CreateCratesStack(z4, new Vector2(101f, 0.75f), cratesStackSprite, groundLayer);
        CreateStall(z4, "Stall_Z4_Entry", new Vector2(101f, 2.5f), stallPosts, awningRed, true, groundLayer);
        CreateRoofPlatform(z4, "Roof_Z4_OverAlley", new Vector2(110f, 4.4f), roofSprite, groundLayer);
        CreateCratesStack(z4, new Vector2(119f, 0.75f), cratesStackSprite, groundLayer);
        CreateLantern(z4, new Vector3(121f, 1.25f, 0f), lanternSprite);
        InstantiateCP(gameplayRoot, checkpointPrefab, "Checkpoint_Z4", new Vector3(124f, 0.17f, 0f));

        // Marcadores Z4
        CreateMarker(npsRoot, "Guard_Spawn_Z4_Roof", new Vector3(110f, 4.8f, 0f));
        CreateMarker(npsRoot, "Waypoint_Z4_A", new Vector3(106f, 4.8f, 0f));
        CreateMarker(npsRoot, "Waypoint_Z4_B", new Vector3(114f, 4.8f, 0f));
        CreateMarker(ingRoot, "Ingredient_Spawn_Z4_Root", new Vector3(110f, 0.5f, 0f)); // dentro del túnel felino

        // --- ZONA 5: Gran Mercado Techado (x = 125 .. 165) ---
        Transform z5 = CreateSubRoot(envRoot, "Zone_5_GranMercado");
        CreateCobbleGround(gameplayRoot, "Ground_Z5", 125.5f, 40f, groundLayer, cobbleSprite); // x = 125.5 .. 165.5
        CreateLantern(z5, new Vector3(128f, 1.25f, 0f), lanternSprite);

        // Nivel 1 (puestos y carretas en zigzag)
        CreateCart(z5, new Vector2(131f, 0.75f), cartSprite, groundLayer);
        CreateStall(z5, "Stall_Z5_1", new Vector2(136f, 1.2f), stallPosts, awningLarge, true, groundLayer);
        CreateRoofPlatform(z5, "Roof_Z5_Low_1", new Vector2(136f, 3.8f), roofSprite, groundLayer);

        CreateBarrel(z5, new Vector2(142f, 0.5f), barrelSprite, groundLayer);
        CreateCratesStack(z5, new Vector2(144f, 0.75f), cratesStackSprite, groundLayer);
        CreateStall(z5, "Stall_Z5_2", new Vector2(149f, 1.2f), stallPosts, awningRed, true, groundLayer);
        CreateRoofPlatform(z5, "Roof_Z5_Mid_2", new Vector2(149f, 4.4f), roofSprite, groundLayer);

        // Plataforma superior alta de vigas (y = 6.2u) que conecta techos
        CreateRoofPlatform(z5, "Roof_Z5_HighWalk", new Vector2(142.5f, 6.2f), roofSprite, groundLayer);
        CreateLantern(z5, new Vector3(143f, 6.7f, 0f), lanternSprite);

        CreateCart(z5, new Vector2(155f, 0.75f), cartSprite, groundLayer);
        CreateStall(z5, "Stall_Z5_3", new Vector2(159f, 1.2f), stallPosts, awningGold, true, groundLayer);
        CreateHideout(gameplayRoot, "Hideout_Z5_MarketShadow", new Vector3(150f, 0.9f, 0f), new Vector2(3f, 1.8f));
        CreateLantern(z5, new Vector3(161f, 1.25f, 0f), lanternSprite);
        InstantiateCP(gameplayRoot, checkpointPrefab, "Checkpoint_Z5", new Vector3(163.5f, 0.17f, 0f));

        // Marcadores Z5
        CreateMarker(npsRoot, "Guard_Spawn_Z5_1", new Vector3(137f, 0.17f, 0f));
        CreateMarker(npsRoot, "Waypoint_Z5_1A", new Vector3(132f, 0.17f, 0f));
        CreateMarker(npsRoot, "Waypoint_Z5_1B", new Vector3(143f, 0.17f, 0f));
        CreateMarker(npsRoot, "Guard_Spawn_Z5_2", new Vector3(154f, 0.17f, 0f));
        CreateMarker(npsRoot, "Waypoint_Z5_2A", new Vector3(148f, 0.17f, 0f));
        CreateMarker(npsRoot, "Waypoint_Z5_2B", new Vector3(160f, 0.17f, 0f));
        CreateMarker(npsRoot, "Villager_Spawn_Z5_1", new Vector3(140f, 0.17f, 0f));
        CreateMarker(npsRoot, "Villager_Spawn_Z5_2", new Vector3(156f, 0.17f, 0f));
        CreateMarker(ingRoot, "Ingredient_Spawn_Z5_BonusMushroom", new Vector3(142.5f, 6.7f, 0f)); // en la viga más alta

        // --- ZONA 6: Salida del Mercado y Portón de la Iglesia (x = 165 .. 190) ---
        Transform z6 = CreateSubRoot(envRoot, "Zone_6_SalidaPorton");
        CreateCobbleGround(gameplayRoot, "Ground_Z6", 165.5f, 26.5f, groundLayer, cobbleSprite); // x = 165.5 .. 192
        CreateLantern(z6, new Vector3(168f, 1.25f, 0f), lanternSprite);
        CreateCratesStack(z6, new Vector2(171f, 0.75f), cratesStackSprite, groundLayer);
        CreateBarrel(z6, new Vector2(173.5f, 0.5f), barrelSprite, groundLayer);
        InstantiateCP(gameplayRoot, checkpointPrefab, "Checkpoint_Z6", new Vector3(175.5f, 0.17f, 0f));

        // Portón Colonial de Salida (Visual)
        if (gateSprite != null)
        {
            GameObject gateObj = new GameObject("Goal_Gate_Visual");
            gateObj.transform.SetParent(z6, false);
            gateObj.transform.position = new Vector3(182f, 2.5f, 0f);
            SpriteRenderer gateSr = gateObj.AddComponent<SpriteRenderer>();
            gateSr.sprite = gateSprite;
            gateSr.sortingLayerName = "Gameplay";
            gateSr.sortingOrder = 1;
        }

        // Marcador de salida con Trigger Collider (lógica a integrar en Prompt 4/5)
        GameObject goalMarker = new GameObject("Goal_Exit_Marker");
        goalMarker.transform.SetParent(gameplayRoot, false);
        goalMarker.transform.position = new Vector3(182f, 2.0f, 0f);
        BoxCollider2D goalCol = goalMarker.AddComponent<BoxCollider2D>();
        goalCol.isTrigger = true;
        goalCol.size = new Vector2(2.5f, 4.0f);

        CreateLantern(z6, new Vector3(180f, 1.25f, 0f), lanternSprite);
        CreateLantern(z6, new Vector3(185f, 1.25f, 0f), lanternSprite);

        // Marcadores Z6
        CreateMarker(npsRoot, "Guard_Spawn_Z6", new Vector3(178f, 0.17f, 0f));
        CreateMarker(npsRoot, "Waypoint_Z6_A", new Vector3(174f, 0.17f, 0f));
        CreateMarker(npsRoot, "Waypoint_Z6_B", new Vector3(180f, 0.17f, 0f));

        // -------------------------------------------------------------
        // D. AJUSTE DE LÍMITES DE CÁMARA
        // -------------------------------------------------------------
        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = Object.FindFirstObjectByType<Camera>();
        if (mainCam != null)
        {
            CameraFollow follow = mainCam.GetComponent<CameraFollow>();
            if (follow != null)
            {
                if (witch != null) follow.SetTarget(witch.transform);
                SerializedObject so = new SerializedObject(follow);
                so.FindProperty("offset").vector3Value = new Vector3(0f, 1.2f, -10f);
                so.FindProperty("useLimits").boolValue = true;
                so.FindProperty("minLimit").vector2Value = new Vector2(-5f, -2.5f);
                so.FindProperty("maxLimit").vector2Value = new Vector2(195f, 15f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Level2StageBuilder] ¡Escenario del Nivel 2 construido exitosamente en Level2.unity!");
    }

    // -------------------------------------------------------------
    // HELPERS DE CONSTRUCCIÓN Y PREFABS
    // -------------------------------------------------------------

    private static void ConfigureImportedSprites()
    {
        string[] pngs = Directory.GetFiles(ArtDir, "*.png");
        foreach (string path in pngs)
        {
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;
            bool dirty = false;
            if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; dirty = true; }
            if (imp.spritePixelsPerUnit != 64) { imp.spritePixelsPerUnit = 64; dirty = true; }
            if (imp.filterMode != FilterMode.Bilinear) { imp.filterMode = FilterMode.Bilinear; dirty = true; }
            if (imp.textureCompression != TextureImporterCompression.Uncompressed) { imp.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
            if (dirty) { EditorUtility.SetDirty(imp); imp.SaveAndReimport(); }
        }
    }

    private static void BuildPrefabs()
    {
        // Guardar prefabs modulares reutilizables bajo Assets/Prefabs/Level2/
        CreateCratePrefab($"{PrefabDir}/Market_Crate.prefab");
        CreateBarrelPrefab($"{PrefabDir}/Market_Barrel.prefab");
        CreateLanternPrefab($"{PrefabDir}/Market_Lantern.prefab");
    }

    private static void CreateCratePrefab(string path)
    {
        GameObject go = new GameObject("Market_Crate");
        go.layer = LayerMask.NameToLayer("Ground");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_Market_Crate.png");
        sr.sortingLayerName = "Gameplay";
        sr.sortingOrder = 1;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.96f, 0.96f);
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    private static void CreateBarrelPrefab(string path)
    {
        GameObject go = new GameObject("Market_Barrel");
        go.layer = LayerMask.NameToLayer("Ground");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_Market_Barrel.png");
        sr.sortingLayerName = "Gameplay";
        sr.sortingOrder = 1;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.85f, 0.98f);
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    private static void CreateLanternPrefab(string path)
    {
        GameObject go = new GameObject("Market_Lantern");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Placeholder_Market_Lantern.png");
        sr.sortingLayerName = "Gameplay";
        sr.sortingOrder = 2;

        GameObject lightObj = new GameObject("PointLight2D");
        lightObj.transform.SetParent(go.transform, false);
        lightObj.transform.localPosition = new Vector3(0.12f, 0.65f, 0f);
        Light2D l = lightObj.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = new Color(1f, 0.82f, 0.42f, 1f);
        l.intensity = 0.85f;
        l.pointLightOuterRadius = 4.5f;

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    private static Transform CreateSubRoot(Transform parent, string name)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        return g.transform;
    }

    private static void ClearChildren(Transform t)
    {
        List<GameObject> list = new List<GameObject>();
        foreach (Transform ch in t) list.Add(ch.gameObject);
        foreach (var go in list) Object.DestroyImmediate(go);
    }

    private static void CreateCobbleGround(Transform parent, string name, float startX, float length, int layer, Sprite sprite)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.layer = layer;
        g.transform.position = new Vector3(startX + length * 0.5f, -2f, 0f);

        BoxCollider2D col = g.AddComponent<BoxCollider2D>();
        col.size = new Vector2(length, 4.17f);
        col.offset = new Vector2(0f, 0.085f); // Superficie superior a +0.17f (alineada con el césped)

        if (sprite != null)
        {
            float w = sprite.rect.width / sprite.pixelsPerUnit;
            int n = Mathf.CeilToInt(length / w) + 1;
            for (int i = 0; i < n; i++)
            {
                GameObject t = new GameObject($"Tile_{i}");
                t.transform.SetParent(g.transform, false);
                t.transform.localPosition = new Vector3(-length * 0.5f + (i * w) + (w * 0.5f), 1f, 0f);
                SpriteRenderer sr = t.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingLayerName = "Gameplay";
                sr.sortingOrder = 0;
            }
        }
    }

    private static void CreateStall(Transform parent, string name, Vector2 pos, Sprite postsSprite, Sprite awningSprite, bool oneWayAwning, int groundLayer)
    {
        GameObject stall = new GameObject(name);
        stall.transform.SetParent(parent, false);
        stall.transform.position = pos;

        // Estructura y mostrador
        if (postsSprite != null)
        {
            GameObject posts = new GameObject("PostsAndCounter");
            posts.transform.SetParent(stall.transform, false);
            posts.layer = groundLayer;
            SpriteRenderer sr = posts.AddComponent<SpriteRenderer>();
            sr.sprite = postsSprite;
            sr.sortingLayerName = "Gameplay";
            sr.sortingOrder = 1;

            // Mostrador sólido a media altura
            BoxCollider2D col = posts.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2.6f, 0.38f);
            col.offset = new Vector2(0f, 0.12f);
        }

        // Toldo superior (plataforma caminable)
        if (awningSprite != null)
        {
            GameObject awning = new GameObject("AwningRoof");
            awning.transform.SetParent(stall.transform, false);
            awning.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            awning.layer = groundLayer;

            SpriteRenderer sr = awning.AddComponent<SpriteRenderer>();
            sr.sprite = awningSprite;
            sr.sortingLayerName = "Gameplay";
            sr.sortingOrder = 2;

            BoxCollider2D col = awning.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2.8f, 0.35f);
            col.offset = new Vector2(0f, 0.15f);

            if (oneWayAwning)
            {
                col.usedByEffector = true;
                PlatformEffector2D eff = awning.AddComponent<PlatformEffector2D>();
                eff.useOneWay = true;
                eff.useOneWayGrouping = true;
                eff.surfaceArc = 170f;
            }
        }
    }

    private static void CreateOneWayAwning(Transform parent, string name, Vector2 pos, Sprite sprite, int groundLayer)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.position = pos;
        obj.layer = groundLayer;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Gameplay";
        sr.sortingOrder = 2;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        float w = sprite.rect.width / sprite.pixelsPerUnit;
        col.size = new Vector2(w * 0.92f, 0.35f);
        col.offset = new Vector2(0f, 0.15f);
        col.usedByEffector = true;

        PlatformEffector2D eff = obj.AddComponent<PlatformEffector2D>();
        eff.useOneWay = true;
        eff.useOneWayGrouping = true;
        eff.surfaceArc = 170f;
    }

    private static void CreateRoofPlatform(Transform parent, string name, Vector2 pos, Sprite sprite, int groundLayer)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.position = pos;
        obj.layer = groundLayer;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Gameplay";
        sr.sortingOrder = 2;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        float w = sprite != null ? sprite.rect.width / sprite.pixelsPerUnit : 4f;
        col.size = new Vector2(w * 0.94f, 0.35f);
        col.offset = new Vector2(0f, 0.08f);
        col.usedByEffector = true;

        PlatformEffector2D eff = obj.AddComponent<PlatformEffector2D>();
        eff.useOneWay = true;
        eff.useOneWayGrouping = true;
        eff.surfaceArc = 170f;
    }

    private static void CreateCart(Transform parent, Vector2 pos, Sprite sprite, int groundLayer)
    {
        GameObject cart = new GameObject("Market_Cart");
        cart.transform.SetParent(parent, false);
        cart.transform.position = pos;
        cart.layer = groundLayer;

        SpriteRenderer sr = cart.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Gameplay";
        sr.sortingOrder = 1;

        BoxCollider2D col = cart.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2.1f, 0.75f);
        col.offset = new Vector2(0f, -0.05f);
    }

    private static void CreateCratesStack(Transform parent, Vector2 pos, Sprite sprite, int groundLayer)
    {
        GameObject stack = new GameObject("Crates_Stack");
        stack.transform.SetParent(parent, false);
        stack.transform.position = pos;
        stack.layer = groundLayer;

        SpriteRenderer sr = stack.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Gameplay";
        sr.sortingOrder = 1;

        BoxCollider2D col = stack.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.85f, 1.35f);
        col.offset = new Vector2(0f, 0f);
    }

    private static void CreateBarrel(Transform parent, Vector2 pos, Sprite sprite, int groundLayer)
    {
        GameObject b = new GameObject("Barrel");
        b.transform.SetParent(parent, false);
        b.transform.position = pos;
        b.layer = groundLayer;

        SpriteRenderer sr = b.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Gameplay";
        sr.sortingOrder = 1;

        BoxCollider2D col = b.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.82f, 0.95f);
    }

    private static void CreateLantern(Transform parent, Vector3 pos, Sprite sprite)
    {
        GameObject lObj = new GameObject("Market_Lantern");
        lObj.transform.SetParent(parent, false);
        lObj.transform.position = pos;

        SpriteRenderer sr = lObj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Gameplay";
        sr.sortingOrder = 2;

        GameObject pLight = new GameObject("Light2D_Lantern");
        pLight.transform.SetParent(lObj.transform, false);
        pLight.transform.localPosition = new Vector3(0.12f, 0.65f, 0f);
        Light2D l = pLight.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = new Color(1f, 0.82f, 0.45f, 1f);
        l.intensity = 0.9f;
        l.pointLightOuterRadius = 4.2f;
    }

    private static void CreateHideout(Transform parent, string name, Vector3 pos, Vector2 size)
    {
        GameObject hObj = new GameObject(name);
        hObj.transform.SetParent(parent, false);
        hObj.transform.position = pos;
        hObj.layer = LayerMask.NameToLayer("Hideout");

        HideoutZone hz = hObj.AddComponent<HideoutZone>();
        BoxCollider2D col = hObj.GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = size;
    }

    private static void CreateKillZone(Transform parent, string name, Vector3 pos, Vector2 size)
    {
        GameObject kz = new GameObject(name);
        kz.transform.SetParent(parent, false);
        kz.transform.position = pos;

        BoxCollider2D col = kz.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = size;
        kz.AddComponent<KillZone>();
    }

    private static void CreateCatTunnelStructure(Transform gameplayParent, Transform envParent, string name, float centerX, float width, int groundLayer, Sprite canopySprite, Sprite roofSprite)
    {
        GameObject tunnel = new GameObject(name);
        tunnel.transform.SetParent(envParent, false);
        tunnel.transform.position = new Vector3(centerX, 0f, 0f);

        // Techo sólido físico: parte inferior en y = 1.22u (altura libre sobre el suelo: 1.05u)
        // Bloquea a la forma humana (1.66u) y permite holgadamente al gato (0.56u)
        GameObject ceiling = new GameObject("Alley_LowCeiling");
        ceiling.transform.SetParent(tunnel.transform, false);
        ceiling.layer = groundLayer;
        // Altura centro = 2.22u, grosor = 2.0u -> borde inferior = 2.22 - 1.0 = 1.22u
        ceiling.transform.position = new Vector3(centerX, 2.22f, 0f);
        BoxCollider2D col = ceiling.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, 2.0f);

        // Visuales de toldo y vigas
        int canopyCount = Mathf.CeilToInt(width / 3f);
        for (int i = 0; i < canopyCount; i++)
        {
            GameObject c = new GameObject($"Canopy_{i}");
            c.transform.SetParent(tunnel.transform, false);
            c.transform.localPosition = new Vector3((-width * 0.5f) + (i * 3f) + 1.5f, 1.45f, 0f);
            SpriteRenderer sr = c.AddComponent<SpriteRenderer>();
            sr.sprite = canopySprite;
            sr.sortingLayerName = "Gameplay";
            sr.sortingOrder = 3;
        }

        // Triggers informativos CatOnlyPassage a la entrada y a la salida
        GameObject entryTrigger = new GameObject("CatPassage_EntryTrigger");
        entryTrigger.transform.SetParent(gameplayParent, false);
        entryTrigger.transform.position = new Vector3(centerX - (width * 0.5f) - 0.5f, 0.8f, 0f);
        var entryCol = entryTrigger.AddComponent<BoxCollider2D>();
        entryCol.isTrigger = true;
        entryCol.size = new Vector2(1.2f, 2.0f);
        entryTrigger.AddComponent<CatOnlyPassage>();

        GameObject exitTrigger = new GameObject("CatPassage_ExitTrigger");
        exitTrigger.transform.SetParent(gameplayParent, false);
        exitTrigger.transform.position = new Vector3(centerX + (width * 0.5f) + 0.5f, 0.8f, 0f);
        var exitCol = exitTrigger.AddComponent<BoxCollider2D>();
        exitCol.isTrigger = true;
        exitCol.size = new Vector2(1.2f, 2.0f);
        exitTrigger.AddComponent<CatOnlyPassage>();
    }

    private static void InstantiateCP(Transform parent, GameObject prefab, string name, Vector3 pos)
    {
        if (prefab == null) return;
        GameObject cp = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
        cp.name = name;
        cp.transform.SetParent(parent, false);
        cp.transform.position = pos;
    }

    private static void CreateMarker(Transform parent, string name, Vector3 pos)
    {
        GameObject m = new GameObject(name);
        m.transform.SetParent(parent, false);
        m.transform.position = pos;
    }
}
