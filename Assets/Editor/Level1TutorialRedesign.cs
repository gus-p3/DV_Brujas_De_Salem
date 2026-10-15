using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Constructor y reensamblador del rediseño largo de Level1.unity (~232 unidades, 9 zonas).
/// Garantiza la integridad de los componentes, físicas medidas y directrices de diseño.
/// </summary>
public static class Level1TutorialRedesign
{
    private const string ScenePath = "Assets/Scenes/Level1.unity";
    private const string BackupScenePath = "Assets/Scenes/Level1_Backup.unity";
    private const string EnvPath = "Assets/Art/Environment/";
    private const string PrefabsPath = "Assets/Prefabs/";

    [MenuItem("Brujas/Tutorial Largo/1. Asegurar Escena y Backup")]
    public static void EnsureSceneAndBackup()
    {
        if (File.Exists(ScenePath) && !File.Exists(BackupScenePath))
        {
            AssetDatabase.CopyAsset(ScenePath, BackupScenePath);
            Debug.Log("[Redesign] Copia de seguridad Level1_Backup.unity creada con éxito.");
        }
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    [MenuItem("Brujas/Tutorial Largo/2. Construir Zonas 1 a 5 (Commit 3)")]
    public static void BuildZones1To5()
    {
        EnsureSceneAndBackup();
        Scene scene = SceneManager.GetActiveScene();

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0) groundLayer = 0;

        // 1. Configurar o crear raíz _Zones
        GameObject zonesRoot = GameObject.Find("_Zones");
        if (zonesRoot != null)
        {
            // Limpiar zonas 1 a 5 si ya existían para reconstrucción limpia
            for (int i = 1; i <= 5; i++)
            {
                Transform existing = zonesRoot.transform.Find($"Zone_{i}");
                if (existing != null) Object.DestroyImmediate(existing.gameObject);
            }
        }
        else
        {
            zonesRoot = new GameObject("_Zones");
        }

        // Limpiar enemigos o spawns antiguos que no estén en _Zones para evitar duplicados
        CleanupLegacyObjects();

        // 2. Expandir Suelo Base y Físicas Generales
        SetupGlobalTerrainAndBounds(groundLayer);

        // 3. Construir Zonas 1 a 5
        BuildZone1(zonesRoot.transform, groundLayer);
        BuildZone2(zonesRoot.transform, groundLayer);
        BuildZone3(zonesRoot.transform, groundLayer);
        BuildZone4(zonesRoot.transform, groundLayer);
        BuildZone5(zonesRoot.transform, groundLayer);

        // Reposicionar Witch en spawn Z1
        GameObject witch = GameObject.Find("Witch");
        if (witch != null)
        {
            witch.transform.position = new Vector3(1.5f, 0.2f, 0f);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Redesign] Zonas 1 a 5 construidas y guardadas exitosamente en Level1.unity.");
    }

    private static void CleanupLegacyObjects()
    {
        string[] legacyNames = { "Guard_1", "Guard_2", "Guard_3", "Villager_1", "Villager_2", "Villager_3", "Villager_4",
                                 "Cornfield_Zone1", "Cornfield_Zone3", "Checkpoint_Test", "HeartPickup_Test1", "HeartPickup_Test2",
                                 "TutorialTrigger_TestA", "TutorialTrigger_Corazones", "Item_Spawn_Branch", "Item_Spawn_Straw", "Item_Spawn_Rope" };
        foreach (string n in legacyNames)
        {
            GameObject obj = GameObject.Find(n);
            if (obj != null && obj.transform.parent != null && obj.transform.parent.name.StartsWith("Zone_")) continue;
            if (obj != null) Object.DestroyImmediate(obj);
        }

        // Limpiar coleccionables sueltos
        Collectible[] cols = Object.FindObjectsByType<Collectible>(FindObjectsSortMode.None);
        foreach (var c in cols)
        {
            if (c.transform.parent == null || !c.transform.parent.name.StartsWith("Zone_"))
            {
                Object.DestroyImmediate(c.gameObject);
            }
        }
    }

    private static void SetupGlobalTerrainAndBounds(int groundLayer)
    {
        // Ajustar CameraFollow
        CameraFollow camFollow = Object.FindFirstObjectByType<CameraFollow>();
        if (camFollow != null)
        {
            SerializedObject soCam = new SerializedObject(camFollow);
            soCam.FindProperty("useLimits").boolValue = true;
            soCam.FindProperty("minLimit").vector2Value = new Vector2(-5f, 0.17f);
            soCam.FindProperty("maxLimit").vector2Value = new Vector2(232f, 15f);
            soCam.ApplyModifiedProperties();
        }

        // Ajustar KillZone
        KillZone kz = Object.FindFirstObjectByType<KillZone>();
        if (kz == null)
        {
            GameObject kzObj = new GameObject("KillZone");
            kz = kzObj.AddComponent<KillZone>();
        }
        kz.transform.position = new Vector3(115f, -4f, 0f);
        BoxCollider2D kzCol = kz.GetComponent<BoxCollider2D>();
        if (kzCol == null) kzCol = kz.gameObject.AddComponent<BoxCollider2D>();
        kzCol.isTrigger = true;
        kzCol.size = new Vector2(260f, 4f);
        kzCol.offset = Vector2.zero;

        // Suelo continuo base para Z1-Z6 (x = -5 a 164)
        GameObject groundRoot = GameObject.Find("Ground");
        if (groundRoot == null)
        {
            GameObject world = GameObject.Find("World");
            if (world == null) world = new GameObject("World");
            groundRoot = new GameObject("Ground");
            groundRoot.transform.SetParent(world.transform);
        }
        groundRoot.layer = groundLayer;
        groundRoot.transform.position = Vector3.zero;

        // Configurar BoxCollider2D del suelo continuo Z1-Z6 (169 unidades de largo: -5 a 164)
        BoxCollider2D gCol = groundRoot.GetComponent<BoxCollider2D>();
        if (gCol == null) gCol = groundRoot.AddComponent<BoxCollider2D>();
        gCol.size = new Vector2(170f, 4.17f);
        gCol.offset = new Vector2(79.5f, -1.915f); // col top en +0.17f

        // Repetir visualmente baldosas
        Sprite groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Ground_Tile.png");
        if (groundSprite != null)
        {
            // Limpiar baldosas hijas viejas
            while (groundRoot.transform.childCount > 0)
            {
                Object.DestroyImmediate(groundRoot.transform.GetChild(0).gameObject);
            }

            float tileWidth = groundSprite.rect.width / groundSprite.pixelsPerUnit; // 8 unidades
            int tileCount = Mathf.CeilToInt(170f / tileWidth) + 1;
            for (int i = 0; i < tileCount; i++)
            {
                GameObject gTile = new GameObject($"Tile_{i}");
                gTile.transform.SetParent(groundRoot.transform, false);
                gTile.transform.localPosition = new Vector3(-5f + (i * tileWidth) + (tileWidth * 0.5f), -1f, 0f);
                SpriteRenderer gSr = gTile.AddComponent<SpriteRenderer>();
                gSr.sprite = groundSprite;
                gSr.sortingLayerName = "Gameplay";
                gSr.sortingOrder = 0;
            }
        }
    }

    private static void BuildZone1(Transform parent, int groundLayer)
    {
        // Z1 Bienvenida: 0-24 u
        GameObject z1 = new GameObject("Zone_1_Bienvenida");
        z1.transform.SetParent(parent);

        // Plataformas bajas
        Sprite logA = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_A.png");
        Sprite logB = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_B.png");
        CreatePlatform(z1, "Platform_Z1_1", logA, new Vector2(12f, 1.8f), groundLayer);
        CreatePlatform(z1, "Platform_Z1_2", logB, new Vector2(17f, 2.2f), groundLayer);

        // Letrero WorldSign
        CreateWorldSign(z1, "Sign_Z1_Bienvenida", new Vector2(6f, 0.5f), "Muévete con {Mover} y salta con {Saltar}.");

        // CP1 en x = 22
        CreateCheckpoint(z1, "Checkpoint_Z1", new Vector3(22f, 0.2f, 0f));
    }

    private static void BuildZone2(Transform parent, int groundLayer)
    {
        // Z2 Salto y guardia: 24-48 u
        GameObject z2 = new GameObject("Zone_2_SaltoGuardia");
        z2.transform.SetParent(parent);

        Sprite logA = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_A.png");
        Sprite logB = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_B.png");
        Sprite ledgeRock = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Ledge_Rock_A.png");

        // Obstáculo en el suelo a x = 35
        GameObject obs = new GameObject("Obstacle_Trunk");
        obs.transform.SetParent(z2.transform);
        obs.transform.position = new Vector3(35f, 0.8f, 0f);
        obs.layer = groundLayer;
        BoxCollider2D obsCol = obs.AddComponent<BoxCollider2D>();
        obsCol.size = new Vector2(1.2f, 1.6f);
        SpriteRenderer obsSr = obs.AddComponent<SpriteRenderer>();
        obsSr.sprite = logA;
        obsSr.sortingLayerName = "Gameplay";
        obsSr.sortingOrder = 1;

        // RAMA a y = 2.8 (salto máx 2.8 u) en x = 36
        GameObject ramaPlat = CreatePlatform(z2, "Platform_Rama_Z2", logB, new Vector2(36f, 2.8f), groundLayer);
        // Pieza 1 de escoba: Rama
        InstantiateBroomPiece("Collectible_Branch", BroomPart.Branch, new Vector3(36f, 3.3f, 0f), z2.transform);

        // Guardia 1: x = 30 a 42 (velocidad 2.0 u/s)
        CreateGuard(z2, "Guard_Z2", new Vector3(31f, 0.8f, 0f), 30f, 42f, 2.0f);

        // Saliente inalcanzable a pie a y = 5.2 con sparkle y letrero pista
        if (ledgeRock != null)
        {
            CreatePlatform(z2, "Ledge_Inalcanzable_Z2", ledgeRock, new Vector2(43f, 5.2f), groundLayer);
        }
        CreateWorldSign(z2, "Sign_Pista_Vuelo_Z2", new Vector2(42f, 0.5f), "Parece muy alto... necesitarás volar.");
        CreateSparkleFX(z2, new Vector3(43f, 5.7f, 0f));

        // CP2 en x = 46
        CreateCheckpoint(z2, "Checkpoint_Z2", new Vector3(46f, 0.2f, 0f));
    }

    private static void BuildZone3(Transform parent, int groundLayer)
    {
        // Z3 Escondite: 48-72 u
        GameObject z3 = new GameObject("Zone_3_EsconditeMaizal");
        z3.transform.SetParent(parent);

        Sprite clusterSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Corn_Cluster.png");
        Sprite sA = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Corn_Stalk_A.png");
        Sprite sB = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Corn_Stalk_B.png");
        Sprite sC = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Corn_Stalk_C.png");

        // 2 Maizales con HideoutZone
        CreateCornfield(z3, "Cornfield_Z3_A", 52f, 8f, clusterSprite, sA, sB, sC);
        CreateCornfield(z3, "Cornfield_Z3_B", 64f, 8f, clusterSprite, sA, sB, sC);

        // Pieza 2 de escoba: Paja en x = 56
        InstantiateBroomPiece("Collectible_Straw", BroomPart.Straw, new Vector3(56f, 0.6f, 0f), z3.transform);

        // Guardia 2: x = 61 a 69 (velocidad 2.2 u/s)
        CreateGuard(z3, "Guard_Z3", new Vector3(62f, 0.8f, 0f), 61f, 69f, 2.2f);

        // Plataforma baja en x = 66 con 1 corazón
        Sprite logA = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_A.png");
        CreatePlatform(z3, "Platform_Z3_Heart", logA, new Vector2(66f, 1.8f), groundLayer);
        InstantiateHeartPickup(new Vector3(66f, 2.3f, 0f), z3.transform);

        // CP3 en x = 70
        CreateCheckpoint(z3, "Checkpoint_Z3", new Vector3(70f, 0.2f, 0f));
    }

    private static void BuildZone4(Transform parent, int groundLayer)
    {
        // Z4 Poción Cegar/Dormir: 72-98 u
        GameObject z4 = new GameObject("Zone_4_PocionesGuardia");
        z4.transform.SetParent(parent);

        // Frascos de pociones en x = 74, 76, 78
        InstantiatePotionPickup(PotionType.Sleep, new Vector3(74f, 0.5f, 0f), z4.transform);
        InstantiatePotionPickup(PotionType.Blind, new Vector3(76f, 0.5f, 0f), z4.transform);
        InstantiatePotionPickup(PotionType.Sleep, new Vector3(78f, 0.5f, 0f), z4.transform);

        // Paso estrecho con techo bajo en x = 86 (impide saltar sobre el guardia)
        Sprite logC = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_C.png");
        CreateSolidCeiling(z4, "Low_Ceiling_Z4", logC, new Vector2(86f, 3.2f), new Vector2(8f, 0.8f), groundLayer);

        // Guardia 3 bloqueando el paso estrecho en x = 86
        CreateGuard(z4, "Guard_Z4_Blocker", new Vector3(86f, 0.8f, 0f), 84f, 88f, 1.6f);

        // Corazón en x = 92
        InstantiateHeartPickup(new Vector3(92f, 0.5f, 0f), z4.transform);

        // CP4 en x = 96
        CreateCheckpoint(z4, "Checkpoint_Z4", new Vector3(96f, 0.2f, 0f));
    }

    private static void BuildZone5(Transform parent, int groundLayer)
    {
        // Z5 Aldeanos: 98-126 u
        GameObject z5 = new GameObject("Zone_5_Aldeanos");
        z5.transform.SetParent(parent);

        // 4 Aldeanos con patrones erráticos
        CreateVillager(z5, "Villager_Z5_1", "Villager", new Vector3(103f, 0.6f, 0f), WanderPattern.WalkAndReturn);
        CreateVillager(z5, "Villager_Z5_2", "VillagerF", new Vector3(109f, 0.6f, 0f), WanderPattern.LookAround);
        CreateVillager(z5, "Villager_Z5_3", "Villager", new Vector3(115f, 0.6f, 0f), WanderPattern.ShortStops);
        CreateVillager(z5, "Villager_Z5_4", "VillagerF", new Vector3(121f, 0.6f, 0f), WanderPattern.WalkAndReturn);

        // Frascos de pociones: 1 Hipnotizar, 1 Dormir
        InstantiatePotionPickup(PotionType.Hypnotize, new Vector3(107f, 0.5f, 0f), z5.transform);
        InstantiatePotionPickup(PotionType.Sleep, new Vector3(117f, 0.5f, 0f), z5.transform);

        // Corazón en x = 120
        InstantiateHeartPickup(new Vector3(120f, 0.5f, 0f), z5.transform);

        // CP5 en x = 124
        CreateCheckpoint(z5, "Checkpoint_Z5", new Vector3(124f, 0.2f, 0f));
    }

    // --- Helpers de Creación ---

    public static GameObject CreatePlatform(GameObject parent, string name, Sprite sprite, Vector2 pos, int layer)
    {
        GameObject plat = new GameObject(name);
        plat.transform.SetParent(parent.transform, false);
        plat.layer = layer;
        plat.transform.position = pos;

        SpriteRenderer sr = plat.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Gameplay";
        sr.sortingOrder = 2;

        BoxCollider2D col = plat.AddComponent<BoxCollider2D>();
        float w = sprite != null ? sprite.rect.width / sprite.pixelsPerUnit : 4f;
        col.size = new Vector2(w * 0.92f, 0.35f);
        col.offset = new Vector2(0f, 0.10f);
        col.usedByEffector = true;

        PlatformEffector2D effector = plat.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.useOneWayGrouping = true;
        effector.surfaceArc = 170f;

        return plat;
    }

    public static GameObject CreateSolidCeiling(GameObject parent, string name, Sprite sprite, Vector2 pos, Vector2 size, int layer)
    {
        GameObject ceiling = new GameObject(name);
        ceiling.transform.SetParent(parent.transform, false);
        ceiling.layer = layer;
        ceiling.transform.position = pos;

        SpriteRenderer sr = ceiling.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Gameplay";
        sr.sortingOrder = 3;

        BoxCollider2D col = ceiling.AddComponent<BoxCollider2D>();
        col.size = size;
        col.offset = Vector2.zero;

        return ceiling;
    }

    public static void CreateWorldSign(GameObject parent, string name, Vector2 pos, string text)
    {
        Sprite signSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Sign.png");
        GameObject signObj = new GameObject(name);
        signObj.transform.SetParent(parent.transform, false);
        signObj.transform.position = pos;

        SpriteRenderer sr = signObj.AddComponent<SpriteRenderer>();
        sr.sprite = signSprite;
        sr.sortingLayerName = "Midground";
        sr.sortingOrder = 1;

        GameObject textObj = new GameObject("Sign_Text");
        textObj.transform.SetParent(signObj.transform, false);
        textObj.transform.localPosition = new Vector3(0f, 1.8f, 0f);

        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text = ControlsMap.Resolve(text);
        tmp.fontSize = 3.2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.96f, 0.92f, 0.78f);
        tmp.sortingLayerID = SortingLayer.NameToID("Gameplay");
        tmp.sortingOrder = 10;

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(4.5f, 2f);

        WorldSign ws = signObj.AddComponent<WorldSign>();
        var so = new SerializedObject(ws);
        so.FindProperty("textMesh").objectReferenceValue = tmp;
        so.FindProperty("message").stringValue = text;
        so.ApplyModifiedProperties();
    }

    public static void CreateSparkleFX(GameObject parent, Vector3 pos)
    {
        Sprite sparkleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/FX_Sparkle.png");
        if (sparkleSprite == null) return;

        GameObject spObj = new GameObject("FX_Sparkle_Visual");
        spObj.transform.SetParent(parent.transform, false);
        spObj.transform.position = pos;

        SpriteRenderer sr = spObj.AddComponent<SpriteRenderer>();
        sr.sprite = sparkleSprite;
        sr.sortingLayerName = "Foreground";
        sr.sortingOrder = 10;
        sr.color = new Color(1f, 1f, 0.8f, 0.85f);
    }

    public static void CreateCornfield(GameObject parent, string name, float centerX, float width, Sprite clusterSprite, Sprite sA, Sprite sB, Sprite sC)
    {
        GameObject cf = new GameObject(name);
        cf.transform.SetParent(parent.transform, false);
        cf.transform.position = new Vector3(centerX, 0f, 0f);

        HideoutZone hz = cf.AddComponent<HideoutZone>();
        cf.AddComponent<CornFoliageFade>();

        BoxCollider2D col = cf.GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(width, 4.5f);
        col.offset = new Vector2(0f, 2.25f);

        int clusterCount = Mathf.CeilToInt(width / 8f) + 1;
        for (int i = 0; i < clusterCount; i++)
        {
            GameObject cl = new GameObject($"Cluster_Bg_{i}");
            cl.transform.SetParent(cf.transform, false);
            cl.transform.localPosition = new Vector3((-width / 2f) + (i * 7f) + 3f, 2.8f, 0f);
            SpriteRenderer sr = cl.AddComponent<SpriteRenderer>();
            sr.sprite = clusterSprite;
            sr.sortingLayerName = "Gameplay";
            sr.sortingOrder = 0;
        }

        Sprite[] stalks = { sA, sB, sC };
        int stalkCount = Mathf.CeilToInt(width / 1.5f);
        for (int i = 0; i < stalkCount; i++)
        {
            GameObject st = new GameObject($"Stalk_Fg_{i}");
            st.transform.SetParent(cf.transform, false);
            st.transform.localPosition = new Vector3((-width / 2f) + (i * 1.5f) + 0.5f, 2.5f, 0f);
            SpriteRenderer sr = st.AddComponent<SpriteRenderer>();
            sr.sprite = stalks[i % stalks.Length];
            sr.color = new Color(1f, 1f, 1f, 0.55f);
            sr.sortingLayerName = "Foreground";
            sr.sortingOrder = 5;
        }
    }

    public static void CreateCheckpoint(GameObject parent, string name, Vector3 pos)
    {
        GameObject cpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsPath + "Checkpoint.prefab");
        GameObject cpInstance = cpPrefab != null
            ? (GameObject)PrefabUtility.InstantiatePrefab(cpPrefab, parent.transform)
            : new GameObject(name);
        cpInstance.name = name;
        cpInstance.transform.position = pos;
    }

    public static void CreateGuard(GameObject parent, string name, Vector3 pos, float minX, float maxX, float speed)
    {
        GameObject guardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsPath + "Guard.prefab");
        if (guardPrefab == null) return;

        GameObject guard = (GameObject)PrefabUtility.InstantiatePrefab(guardPrefab, parent.transform);
        guard.name = name;
        guard.transform.position = pos;

        // Configurar waypoints
        GameObject wpA = new GameObject($"{name}_WpA");
        wpA.transform.SetParent(parent.transform, false);
        wpA.transform.position = new Vector3(minX, pos.y, 0f);

        GameObject wpB = new GameObject($"{name}_WpB");
        wpB.transform.SetParent(parent.transform, false);
        wpB.transform.position = new Vector3(maxX, pos.y, 0f);

        PatrolRoute pr = guard.GetComponent<PatrolRoute>();
        if (pr != null)
        {
            SerializedObject so = new SerializedObject(pr);
            SerializedProperty wpProp = so.FindProperty("waypoints");
            wpProp.arraySize = 2;
            wpProp.GetArrayElementAtIndex(0).objectReferenceValue = wpA.transform;
            wpProp.GetArrayElementAtIndex(1).objectReferenceValue = wpB.transform;
            so.FindProperty("walkSpeed").floatValue = speed;
            so.ApplyModifiedProperties();
        }
    }

    public static void CreateVillager(GameObject parent, string name, string prefabName, Vector3 pos, WanderPattern pattern)
    {
        GameObject vPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsPath + $"{prefabName}.prefab");
        if (vPrefab == null) return;

        GameObject villager = (GameObject)PrefabUtility.InstantiatePrefab(vPrefab, parent.transform);
        villager.name = name;
        villager.transform.position = pos;
    }

    public static void InstantiateBroomPiece(string prefabName, BroomPart part, Vector3 pos, Transform parent)
    {
        GameObject pPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsPath + $"{prefabName}.prefab");
        if (pPrefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(pPrefab, parent);
            instance.name = prefabName;
            instance.transform.position = pos;
        }
    }

    public static void InstantiateHeartPickup(Vector3 pos, Transform parent)
    {
        GameObject hPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsPath + "HeartPickup.prefab");
        if (hPrefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(hPrefab, parent);
            instance.transform.position = pos;
        }
    }

    public static void InstantiatePotionPickup(PotionType type, Vector3 pos, Transform parent)
    {
        string pName = $"PotionPickup_{type}";
        GameObject pPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsPath + $"{pName}.prefab");
        if (pPrefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(pPrefab, parent);
            instance.transform.position = pos;
        }
    }

    [MenuItem("Brujas/Tutorial Largo/3. Construir Zonas 6 a 9 (Commit 4)")]
    public static void BuildZones6To9()
    {
        EnsureSceneAndBackup();
        Scene scene = SceneManager.GetActiveScene();

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0) groundLayer = 0;

        GameObject zonesRoot = GameObject.Find("_Zones");
        if (zonesRoot == null) zonesRoot = new GameObject("_Zones");

        for (int i = 6; i <= 9; i++)
        {
            Transform existing = zonesRoot.transform.Find($"Zone_{i}");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
        }

        BuildZone6(zonesRoot.transform, groundLayer);
        BuildZone7(zonesRoot.transform, groundLayer);
        BuildZone8(zonesRoot.transform, groundLayer);
        BuildZone9(zonesRoot.transform, groundLayer);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Redesign] Zonas 6 a 9 construidas y guardadas exitosamente en Level1.unity.");
    }

    private static void BuildZone6(Transform parent, int groundLayer)
    {
        // Z6 Gato: 126-152 u (Túnel de tronco hueco, altura libre y=1.05)
        GameObject z6 = new GameObject("Zone_6_GatoTunel");
        z6.transform.SetParent(parent);

        Sprite tunnelBack = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Log_Tunnel_Back.png");
        Sprite tunnelFront = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Log_Tunnel_Front.png");
        Sprite logC = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_C.png");

        // Visual del túnel trasero (orden 1) y frontal (orden 8)
        if (tunnelBack != null)
        {
            GameObject tbObj = new GameObject("Tunnel_Visual_Back");
            tbObj.transform.SetParent(z6.transform, false);
            tbObj.transform.position = new Vector3(140f, 1.6f, 0f);
            tbObj.transform.localScale = new Vector3(2.0f, 1.3f, 1f);
            SpriteRenderer tbSr = tbObj.AddComponent<SpriteRenderer>();
            tbSr.sprite = tunnelBack;
            tbSr.sortingLayerName = "Gameplay";
            tbSr.sortingOrder = 1;
        }

        if (tunnelFront != null)
        {
            GameObject tfObj = new GameObject("Tunnel_Visual_Front");
            tfObj.transform.SetParent(z6.transform, false);
            tfObj.transform.position = new Vector3(140f, 1.6f, 0f);
            tfObj.transform.localScale = new Vector3(2.0f, 1.3f, 1f);
            SpriteRenderer tfSr = tfObj.AddComponent<SpriteRenderer>();
            tfSr.sprite = tunnelFront;
            tfSr.sortingLayerName = "Gameplay";
            tfSr.sortingOrder = 8;
        }

        // Techo físico del tronco: apertura libre de 1.05u desde el suelo (+0.17f a +1.05f = 0.88u libre)
        // El collider humano mide 1.60u de alto, colisiona físicamente.
        // El gato mide 0.50u de alto, pasa sin problemas.
        GameObject ceiling = new GameObject("Tunnel_Solid_Ceiling");
        ceiling.transform.SetParent(z6.transform, false);
        ceiling.layer = groundLayer;
        ceiling.transform.position = new Vector3(140f, 2.2f, 0f);
        BoxCollider2D ceilCol = ceiling.AddComponent<BoxCollider2D>();
        ceilCol.size = new Vector2(16.5f, 2.3f); // Base del collider en 2.2 - 1.15 = 1.05f

        // CatOnlyPassage a la entrada (x=131.5) y a la salida (x=148.5)
        CreateCatPassage(z6, "CatOnlyPassage_Entry", new Vector3(131.5f, 1.0f, 0f));
        CreateCatPassage(z6, "CatOnlyPassage_Exit", new Vector3(148.5f, 1.0f, 0f));

        // Cámara amplia adentro: Cuerda en x=145 y Corazón en x=142
        InstantiateBroomPiece("Collectible_Rope", BroomPart.Rope, new Vector3(145f, 0.6f, 0f), z6.transform);
        InstantiateHeartPickup(new Vector3(142f, 0.6f, 0f), z6.transform);

        // Pasarela superior y guardia patrullando por arriba/afuera del tronco (x=132 a 148 a y=3.4)
        CreatePlatform(z6, "Platform_Tunnel_Roof", logC, new Vector2(140f, 3.4f), groundLayer);
        CreateGuard(z6, "Guard_Z6_Roof", new Vector3(134f, 4.1f, 0f), 132f, 147f, 1.8f);

        // CP6 en x = 150
        CreateCheckpoint(z6, "Checkpoint_Z6", new Vector3(150f, 0.2f, 0f));
    }

    private static void BuildZone7(Transform parent, int groundLayer)
    {
        // Z7 Vuelo 1: 152-180 u (Repisas altas a y=5.0, brecha de 10u x=164-174)
        GameObject z7 = new GameObject("Zone_7_Vuelo1");
        z7.transform.SetParent(parent);

        Sprite ledgeA = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Ledge_Rock_A.png");
        Sprite ledgeB = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Ledge_Rock_B.png");

        // Repisas altas a y = 5.0 con premios
        if (ledgeA != null)
        {
            CreatePlatform(z7, "Ledge_High_Reward_1", ledgeA, new Vector2(158f, 5.0f), groundLayer);
            InstantiatePotionPickup(PotionType.Hypnotize, new Vector3(158f, 5.5f, 0f), z7.transform);
        }
        if (ledgeB != null)
        {
            CreatePlatform(z7, "Ledge_High_Reward_2", ledgeB, new Vector2(176f, 5.0f), groundLayer);
            InstantiatePotionPickup(PotionType.Blind, new Vector3(176f, 5.5f, 0f), z7.transform);
        }

        // FlightOnlyHint antes de la brecha en x = 162
        CreateFlightHint(z7, "FlightHint_Z7", new Vector3(162f, 1.2f, 0f));

        // Brecha de 10 u: x = 164 a 174 (sin suelo, caída a KillZone)
        // Suelo seguro al otro lado de la brecha: x = 174 a 180
        CreateGroundSection(z7, "Ground_Z7_Landing", 174f, 180f, groundLayer);

        // CP7 en x = 178
        CreateCheckpoint(z7, "Checkpoint_Z7", new Vector3(178f, 0.2f, 0f));
    }

    private static void BuildZone8(Transform parent, int groundLayer)
    {
        // Z8 Reto combinado: 180-206 u
        GameObject z8 = new GameObject("Zone_8_RetoCombinado");
        z8.transform.SetParent(parent);

        // Suelo continuo de Z8: x = 180 a 208
        CreateGroundSection(z8, "Ground_Z8", 180f, 208f, groundLayer);

        // Frascos de pociones y corazón al inicio (x = 181-183)
        InstantiatePotionPickup(PotionType.Sleep, new Vector3(181f, 0.5f, 0f), z8.transform);
        InstantiatePotionPickup(PotionType.Blind, new Vector3(182f, 0.5f, 0f), z8.transform);
        InstantiateHeartPickup(new Vector3(183f, 0.5f, 0f), z8.transform);

        // Plataformas sobre desniveles
        Sprite logA = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_A.png");
        Sprite logB = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_B.png");
        CreatePlatform(z8, "Platform_Z8_1", logA, new Vector2(187f, 2.2f), groundLayer);
        CreatePlatform(z8, "Platform_Z8_2", logB, new Vector2(197f, 2.5f), groundLayer);

        // 2 Maizales con HideoutZone
        Sprite cluster = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Corn_Cluster.png");
        Sprite sA = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Corn_Stalk_A.png");
        Sprite sB = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Corn_Stalk_B.png");
        Sprite sC = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Corn_Stalk_C.png");
        CreateCornfield(z8, "Cornfield_Z8_A", 190f, 7f, cluster, sA, sB, sC);
        CreateCornfield(z8, "Cornfield_Z8_B", 200f, 6f, cluster, sA, sB, sC);

        // 2 Guardias patrullando
        CreateGuard(z8, "Guard_Z8_1", new Vector3(186f, 0.8f, 0f), 185f, 193f, 2.0f);
        CreateGuard(z8, "Guard_Z8_2", new Vector3(198f, 0.8f, 0f), 196f, 203f, 2.2f);

        // 3 Aldeanos erráticos
        CreateVillager(z8, "Villager_Z8_1", "Villager", new Vector3(188f, 0.6f, 0f), WanderPattern.WalkAndReturn);
        CreateVillager(z8, "Villager_Z8_2", "VillagerF", new Vector3(194f, 0.6f, 0f), WanderPattern.LookAround);
        CreateVillager(z8, "Villager_Z8_3", "Villager", new Vector3(201f, 0.6f, 0f), WanderPattern.ShortStops);

        // CP8 en x = 204
        CreateCheckpoint(z8, "Checkpoint_Z8", new Vector3(204f, 0.2f, 0f));
    }

    private static void BuildZone9(Transform parent, int groundLayer)
    {
        // Z9 Vuelo final y meta: 206-232 u (Brecha de 16u x=208-224 con isla en x=216, meta en x=228)
        GameObject z9 = new GameObject("Zone_9_VueloMeta");
        z9.transform.SetParent(parent);

        Sprite logB = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_B.png");
        Sprite gateSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Goal_Gate.png");

        // Plataforma isla de descanso en x = 216, y = 1.5
        CreatePlatform(z9, "Island_Rest_Z9", logB, new Vector2(216f, 1.5f), groundLayer);

        // Suelo final de x = 224 a 234
        CreateGroundSection(z9, "Ground_Z9_Final", 224f, 235f, groundLayer);

        // Meta del Nivel (LevelGoal) en x = 228
        GameObject goalObj = new GameObject("LevelGoal");
        goalObj.transform.SetParent(z9.transform, false);
        goalObj.transform.position = new Vector3(228f, 0.05f, 0f);

        if (gateSprite != null)
        {
            GameObject gateVis = new GameObject("Goal_Gate_Visual");
            gateVis.transform.SetParent(goalObj.transform, false);
            gateVis.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            SpriteRenderer gateSr = gateVis.AddComponent<SpriteRenderer>();
            gateSr.sprite = gateSprite;
            gateSr.sortingLayerName = "Gameplay";
            gateSr.sortingOrder = 1;
        }

        BoxCollider2D goalCol = goalObj.AddComponent<BoxCollider2D>();
        goalCol.isTrigger = true;
        goalCol.size = new Vector2(2.5f, 4.5f);
        goalCol.offset = new Vector2(0f, 2.25f);
        goalObj.AddComponent<LevelGoal>();
    }

    private static void CreateCatPassage(GameObject parent, string name, Vector3 pos)
    {
        GameObject passageObj = new GameObject(name);
        passageObj.transform.SetParent(parent.transform, false);
        passageObj.transform.position = pos;

        BoxCollider2D col = passageObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.5f, 3.5f);
        col.offset = new Vector2(0f, 1.75f);

        passageObj.AddComponent<CatOnlyPassage>();
    }

    private static void CreateFlightHint(GameObject parent, string name, Vector3 pos)
    {
        GameObject hintObj = new GameObject(name);
        hintObj.transform.SetParent(parent.transform, false);
        hintObj.transform.position = pos;

        BoxCollider2D col = hintObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2f, 4f);
        col.offset = new Vector2(0f, 2f);

        hintObj.AddComponent<FlightOnlyHint>();
    }

    private static void CreateGroundSection(GameObject parent, string name, float startX, float endX, int groundLayer)
    {
        float width = endX - startX;
        float centerX = startX + (width * 0.5f);

        GameObject gObj = new GameObject(name);
        gObj.transform.SetParent(parent.transform, false);
        gObj.layer = groundLayer;
        gObj.transform.position = new Vector3(centerX, 0f, 0f);

        BoxCollider2D col = gObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, 4.17f);
        col.offset = new Vector2(0f, -1.915f);

        Sprite groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Ground_Tile.png");
        if (groundSprite != null)
        {
            float tileWidth = groundSprite.rect.width / groundSprite.pixelsPerUnit;
            int count = Mathf.CeilToInt(width / tileWidth) + 1;
            for (int i = 0; i < count; i++)
            {
                float x = (-width * 0.5f) + (i * tileWidth) + (tileWidth * 0.5f);
                if (x > (width * 0.5f) + (tileWidth * 0.5f)) continue;

                GameObject tile = new GameObject($"Tile_{i}");
                tile.transform.SetParent(gObj.transform, false);
                tile.transform.localPosition = new Vector3(x, -1f, 0f);

                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = groundSprite;
                sr.sortingLayerName = "Gameplay";
                sr.sortingOrder = 0;
            }
        }
    }
}

