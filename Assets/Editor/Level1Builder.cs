using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Constructor de la escena Level1.unity con todas las especificaciones de diseño:
/// parallax en 3 capas, suelo, plataformas, maizales, letreros de tutorial,
/// marcadores de spawn, iluminación 2D, bruja y HUD completo.
/// </summary>
public static class Level1Builder
{
    private const string ScenePath = "Assets/Scenes/Level1.unity";
    private const string WitchPrefabPath = "Assets/Prefabs/Witch.prefab";
    private const string GameManagerPrefabPath = "Assets/Prefabs/GameManager.prefab";
    private const string AudioManagerPrefabPath = "Assets/Prefabs/AudioManager.prefab";
    private const string EnvPath = "Assets/Art/Environment/";

    [MenuItem("Brujas/Escena/Construir Level 1 completo")]
    public static void BuildLevel1()
    {
        // 1. Asegurar layers y sorting layers
        ProjectSetup.Configure();
        EnvironmentImporter.ConfigureEnvironmentSprites();

        // 2. Crear nueva escena vacía
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 3. _Managers
        GameObject managersRoot = new GameObject("_Managers");
        GameObject gmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameManagerPrefabPath);
        if (gmPrefab != null)
        {
            GameObject gm = (GameObject)PrefabUtility.InstantiatePrefab(gmPrefab);
            gm.transform.SetParent(managersRoot.transform);
        }
        GameObject amPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AudioManagerPrefabPath);
        if (amPrefab != null)
        {
            GameObject am = (GameObject)PrefabUtility.InstantiatePrefab(amPrefab);
            am.transform.SetParent(managersRoot.transform);
        }

        // 4. Luces 2D
        GameObject globalLightObj = new GameObject("Global Light 2D");
        Light2D globalLight = globalLightObj.AddComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.color = new Color(0.35f, 0.45f, 0.75f);
        globalLight.intensity = 0.55f;

        GameObject moonLightObj = new GameObject("Moon Point Light 2D");
        moonLightObj.transform.position = new Vector3(15f, 8f, 0f);
        Light2D moonLight = moonLightObj.AddComponent<Light2D>();
        moonLight.lightType = Light2D.LightType.Point;
        moonLight.color = new Color(1f, 0.88f, 0.55f);
        moonLight.intensity = 0.8f;
        moonLight.pointLightOuterRadius = 7f;

        // 5. Background (Parallax)
        GameObject bgRoot = new GameObject("Background");

        // Sky Far (factor 0.9, sorting layer Background, order 0)
        Sprite skySprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Sky_Far.png");
        GameObject skyObj = new GameObject("Sky_Far");
        skyObj.transform.SetParent(bgRoot.transform);
        skyObj.transform.position = new Vector3(0f, 4.5f, 0f);
        ParallaxUtility.SetupThreeCopies(skyObj, skySprite, "Background", 0);
        ParallaxLayer skyPl = skyObj.GetComponent<ParallaxLayer>();
        skyPl.Setup(0.9f, true, true);

        // Trees Mid (factor 0.6, sorting layer Midground, order 0)
        Sprite treesSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Trees_Mid.png");
        GameObject treesObj = new GameObject("Trees_Mid");
        treesObj.transform.SetParent(bgRoot.transform);
        treesObj.transform.position = new Vector3(0f, 2.0f, 0f);
        ParallaxUtility.SetupThreeCopies(treesObj, treesSprite, "Midground", 0);
        ParallaxLayer treesPl = treesObj.GetComponent<ParallaxLayer>();
        treesPl.Setup(0.6f, true, true);

        // 6. World (Suelo y Plataformas)
        GameObject worldRoot = new GameObject("World");
        int groundLayer = LayerMask.NameToLayer("Ground");

        // Suelo continuo modular (x = -5 a 105, 110 unidades de largo)
        Sprite groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Ground_Tile.png");
        GameObject groundObj = new GameObject("Ground");
        groundObj.transform.SetParent(worldRoot.transform);
        groundObj.layer = groundLayer;
        groundObj.transform.position = new Vector3(50f, -2f, 0f);

        BoxCollider2D groundCol = groundObj.AddComponent<BoxCollider2D>();
        groundCol.size = new Vector2(115f, 4.17f);
        groundCol.offset = new Vector2(0f, 0.085f); // col.max.y = -2.0 + 0.085 + 2.085 = +0.17f (alineado con el césped)

        // Repetir visualmente el suelo a lo largo de las 115 unidades
        float tileWidth = groundSprite.rect.width / groundSprite.pixelsPerUnit; // 8 unidades
        int tileCount = Mathf.CeilToInt(115f / tileWidth) + 1;
        for (int i = 0; i < tileCount; i++)
        {
            GameObject gTile = new GameObject($"Tile_{i}");
            gTile.transform.SetParent(groundObj.transform, false);
            gTile.transform.localPosition = new Vector3(-55f + (i * tileWidth), 1f, 0f);
            SpriteRenderer gSr = gTile.AddComponent<SpriteRenderer>();
            gSr.sprite = groundSprite;
            gSr.sortingLayerName = "Gameplay";
            gSr.sortingOrder = 0;
        }

        // Plataformas (Mínimo 8 de troncos A/B/C)
        Sprite logA = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_A.png");
        Sprite logB = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_B.png");
        Sprite logC = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_C.png");

        GameObject platformsRoot = new GameObject("Platforms");
        platformsRoot.transform.SetParent(worldRoot.transform);

        // Zona 2: Bosque bajo (3 plataformas)
        CreatePlatform(platformsRoot, "Platform_Z2_1", logA, new Vector2(24f, 1.8f), groundLayer);
        CreatePlatform(platformsRoot, "Platform_Z2_2", logB, new Vector2(31f, 3.2f), groundLayer);
        CreatePlatform(platformsRoot, "Platform_Z2_3_High", logC, new Vector2(39f, 4.6f), groundLayer); // Aquí va Item_Spawn_Branch

        // Zona 3: Maizal grande (2 plataformas)
        CreatePlatform(platformsRoot, "Platform_Z3_1", logA, new Vector2(48f, 2.2f), groundLayer);
        CreatePlatform(platformsRoot, "Platform_Z3_2", logB, new Vector2(57f, 3.2f), groundLayer);

        // Zona 4: Bosque alto (4 plataformas escalonadas con brecha)
        CreatePlatform(platformsRoot, "Platform_Z4_1", logA, new Vector2(65f, 2.0f), groundLayer);
        CreatePlatform(platformsRoot, "Platform_Z4_2", logB, new Vector2(72f, 3.4f), groundLayer);
        CreatePlatform(platformsRoot, "Platform_Z4_3", logC, new Vector2(80f, 4.8f), groundLayer);
        CreatePlatform(platformsRoot, "Platform_Z4_4_Final", logB, new Vector2(88f, 3.4f), groundLayer); // Aquí va Item_Spawn_Rope

        // 7. Zonas de Maizal (HideoutZone y arte de maíz)
        GameObject maizalesRoot = new GameObject("Cornfields");
        maizalesRoot.transform.SetParent(worldRoot.transform);

        Sprite cornCluster = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Corn_Cluster.png");
        Sprite stalkA = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Corn_Stalk_A.png");
        Sprite stalkB = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Corn_Stalk_B.png");
        Sprite stalkC = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Corn_Stalk_C.png");

        // Zona 1: Maizal tutorial pequeño (x = 12 a 18)
        CreateCornfield(maizalesRoot, "Cornfield_Zone1", 15f, 6f, cornCluster, stalkA, stalkB, stalkC);

        // Zona 3: Maizal grande denso (x = 44 a 56)
        CreateCornfield(maizalesRoot, "Cornfield_Zone3", 50f, 12f, cornCluster, stalkA, stalkB, stalkC);

        // 8. Letreros de Tutorial
        Sprite signSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Sign.png");
        GameObject tutorialsRoot = new GameObject("Tutorials");
        tutorialsRoot.transform.SetParent(worldRoot.transform);

        CreateTutorialSign(tutorialsRoot, "Tutorial_Sign_1", new Vector2(4f, 1.2f), signSprite,
            "Pulsa T para transformarte en gato y ocultarte en el maizal");

        CreateTutorialSign(tutorialsRoot, "Tutorial_Sign_2", new Vector2(20f, 1.2f), signSprite,
            "Reúne rama, paja y cuerda para armar tu escoba");

        // 9. Marcadores de Spawn (Empty con nombres exactos)
        GameObject spawnsRoot = new GameObject("Spawns");
        CreateSpawnMarker(spawnsRoot, "Guard_Spawn_1", new Vector3(31f, 0.8f, 0f));
        CreateSpawnMarker(spawnsRoot, "Guard_Spawn_2", new Vector3(62f, 0.8f, 0f));
        CreateSpawnMarker(spawnsRoot, "Item_Spawn_Branch", new Vector3(39f, 5.6f, 0f));
        CreateSpawnMarker(spawnsRoot, "Item_Spawn_Straw", new Vector3(52f, 0.8f, 0f));
        CreateSpawnMarker(spawnsRoot, "Item_Spawn_Rope", new Vector3(88f, 4.4f, 0f));

        // Meta del Nivel (Goal_Spawn y Portón)
        GameObject goalSpawn = CreateSpawnMarker(spawnsRoot, "Goal_Spawn", new Vector3(95f, 0f, 0f));
        Sprite gateSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Goal_Gate.png");
        if (gateSprite != null)
        {
            GameObject gateObj = new GameObject("Goal_Gate_Visual");
            gateObj.transform.SetParent(goalSpawn.transform, false);
            gateObj.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            SpriteRenderer gateSr = gateObj.AddComponent<SpriteRenderer>();
            gateSr.sprite = gateSprite;
            gateSr.sortingLayerName = "Gameplay";
            gateSr.sortingOrder = 1;
        }

        // 10. Bruja (Instancia de Prefab en x = 0)
        GameObject witchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WitchPrefabPath);
        GameObject witch = null;
        if (witchPrefab != null)
        {
            witch = (GameObject)PrefabUtility.InstantiatePrefab(witchPrefab);
            witch.name = "Witch";
            witch.transform.position = new Vector3(0f, 0.05f, 0f);
            SpriteRenderer wSr = witch.GetComponentInChildren<SpriteRenderer>();
            if (wSr != null)
            {
                wSr.sortingLayerName = "Gameplay";
                wSr.sortingOrder = 5;
            }
        }
        else
        {
            Debug.LogError("[Level1Builder] No se encontró el prefab de la bruja en " + WitchPrefabPath);
        }

        // 11. Main Camera
        GameObject cameraObj = new GameObject("Main Camera");
        cameraObj.tag = "MainCamera";
        cameraObj.transform.position = new Vector3(0f, 1.2f, -10f);
        Camera cam = cameraObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.5f; // Altura ortográfica
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.08f, 0.22f);
        cameraObj.AddComponent<AudioListener>();

        CameraFollow follow = cameraObj.AddComponent<CameraFollow>();
        if (witch != null)
        {
            follow.SetTarget(witch.transform);
        }
        SerializedObject soFollow = new SerializedObject(follow);
        soFollow.FindProperty("offset").vector3Value = new Vector3(0f, 1.2f, -10f);
        soFollow.FindProperty("useLimits").boolValue = true;
        soFollow.FindProperty("minLimit").vector2Value = new Vector2(-5f, 0.17f);
        soFollow.FindProperty("maxLimit").vector2Value = new Vector2(110f, 15f);
        soFollow.ApplyModifiedPropertiesWithoutUndo();

        // 12. HUD Canvas y paneles
        UISetup.BuildHUD();

        // 13. Guardar escena
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);

        // 14. Actualizar Build Settings: MainMenu (0), Level1 (1), Test_Bruja (2)
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Level1Builder] Escena Level1.unity generada y guardada exitosamente.");
    }

    private static void CreatePlatform(GameObject parent, string name, Sprite sprite, Vector2 pos, int layer)
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
        float w = sprite.rect.width / sprite.pixelsPerUnit;
        
        // Superficie superior según sprite real: Log A ~0.28u, Log B ~0.30u, Log C ~0.23u (media ~0.27u)
        // Fijamos la cara superior del collider en +0.27u relativo al centro de la plataforma con grosor 0.35u
        float targetTop = 0.27f;
        float colThickness = 0.35f;
        col.size = new Vector2(w * 0.92f, colThickness);
        col.offset = new Vector2(0f, targetTop - (colThickness * 0.5f));
        col.usedByEffector = true;

        PlatformEffector2D effector = plat.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.useOneWayGrouping = true;
        effector.surfaceArc = 170f;
    }

    private static void CreateCornfield(GameObject parent, string name, float centerX, float width, Sprite clusterSprite, Sprite sA, Sprite sB, Sprite sC)
    {
        GameObject cf = new GameObject(name);
        cf.transform.SetParent(parent.transform, false);
        cf.transform.position = new Vector3(centerX, 0f, 0f);

        // Hideout Zone Trigger y componente de fundido
        HideoutZone hz = cf.AddComponent<HideoutZone>();
        cf.AddComponent<CornFoliageFade>();

        BoxCollider2D col = cf.GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(width, 4.5f);
        col.offset = new Vector2(0f, 2.25f);

        // Fondo de maizal (detrás de la bruja, Gameplay orden 0)
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

        // Tallos al frente (delante de la bruja, Foreground orden 5, alfa base 0.55f)
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

    private static void CreateTutorialSign(GameObject parent, string name, Vector2 pos, Sprite signSprite, string text)
    {
        GameObject signObj = new GameObject(name);
        signObj.transform.SetParent(parent.transform, false);
        signObj.transform.position = pos;

        SpriteRenderer sr = signObj.AddComponent<SpriteRenderer>();
        sr.sprite = signSprite;
        sr.sortingLayerName = "Midground";
        sr.sortingOrder = 1;

        // Texto en World Space sobre el letrero
        GameObject textObj = new GameObject("Sign_Text");
        textObj.transform.SetParent(signObj.transform, false);
        textObj.transform.localPosition = new Vector3(0f, 1.8f, 0f);

        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = 3.2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.96f, 0.92f, 0.78f);
        tmp.sortingLayerID = SortingLayer.NameToID("Gameplay");
        tmp.sortingOrder = 10;

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(4.5f, 2f);
    }

    private static GameObject CreateSpawnMarker(GameObject parent, string name, Vector3 pos)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent.transform, false);
        marker.transform.position = pos;
        return marker;
    }

    private static void UpdateBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Level1.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Test_Bruja.unity", true)
        };
        EditorBuildSettings.scenes = scenes;
        Debug.Log("[Level1Builder] Build Settings actualizadas: MainMenu (0), Level1 (1), Test_Bruja (2).");
    }
}
