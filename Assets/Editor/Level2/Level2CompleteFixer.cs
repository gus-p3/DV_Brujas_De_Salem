#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Level2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Herramienta integral de correccion del Nivel 2 (Sotano/Escaleras, Estructuras Huerfanas,
/// Casas Decorativas, Casas Interactivas y Fondo con Parallax).
/// </summary>
public static class Level2CompleteFixer
{
    private const string LEVEL2_SCENE_PATH = "Assets/Scenes/Level2.unity";
    private const string HOUSES_ATLAS_PATH = "Assets/Screenshots/NIVEL2/houses_sprite.png.png";
    private const string SKY_FONDO_PATH = "Assets/Screenshots/NIVEL2/village_day_fondo.png";
    private const string SKY_CLEAN_PATH = "Assets/Art/Environment/Level2/Level2_Sky_Far.png";
    private const string FAR_CLEAN_PATH = "Assets/Art/Environment/Level2/Level2_Village_Far.png";
    private const string MID_CLEAN_PATH = "Assets/Art/Environment/Level2/Level2_Facades_Mid.png";
    private const float GROUND_SURFACE_Y = 0.17f;
    private const string CMD_FILE = "Temp/level2_cmd.txt";
    private const string RES_FILE = "Temp/level2_result.txt";

    [InitializeOnLoadMethod]
    private static void RegisterCommandPoller()
    {
        EditorApplication.update -= PollCommandFile;
        EditorApplication.update += PollCommandFile;
    }

    private static void PollCommandFile()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!System.IO.File.Exists(CMD_FILE)) return;

        string cmd = System.IO.File.ReadAllText(CMD_FILE).Trim();
        System.IO.File.Delete(CMD_FILE);

        try
        {
            string logResult = "OK: " + cmd;
            switch (cmd)
            {
                case "BLOCK_A":
                    ApplyBlockA_BasementAndLadders();
                    break;
                case "BLOCK_B":
                    ApplyBlockB_OrphanLaddersAndDoors();
                    break;
                case "BLOCK_C":
                    ApplyBlockC_DecorativeHouses();
                    break;
                case "BLOCK_D":
                    ApplyBlockD_InteractiveHouses();
                    break;
                case "BLOCK_E":
                    ApplyBlockE_NewParallaxBackground();
                    break;
                case "VERIFY":
                    int count = Level2StructureVerifier.VerifyAllStructures();
                    Level2GroundingTool.VerifyMenu();
                    logResult = $"VERIFY_DONE: StructureFindings={count}";
                    break;
                case "CAPTURE_AFTER":
                    CaptureAfterEvidences();
                    break;
                case "RUN_ALL_FIXES":
                    ApplyBlockA_BasementAndLadders();
                    ApplyBlockB_OrphanLaddersAndDoors();
                    ApplyBlockC_DecorativeHouses();
                    ApplyBlockD_InteractiveHouses();
                    ApplyBlockE_NewParallaxBackground();
                    CaptureAfterEvidences();
                    int fCount = Level2StructureVerifier.VerifyAllStructures();
                    Level2GroundingTool.VerifyMenu();
                    logResult = $"ALL_FIXES_DONE: StructureFindings={fCount}";
                    break;
                case "RUN_L1_REGRESSION":
                    EditorApplication.ExecuteMenuItem("Tools/Verify Level 1 (Master 9-Point Check)");
                    EditorApplication.ExecuteMenuItem("Tools/Run Level 1 Full Verification Suite (15 Milestones)");
                    EditorSceneManager.OpenScene(LEVEL2_SCENE_PATH, OpenSceneMode.Single);
                    logResult = "L1_REGRESSION_DONE";
                    break;
                default:
                    if (cmd.StartsWith("MENU:"))
                    {
                        string menuPath = cmd.Substring(5);
                        bool ok = EditorApplication.ExecuteMenuItem(menuPath);
                        logResult = $"MENU:{menuPath} => {ok}";
                    }
                    break;
            }
            System.IO.File.WriteAllText(RES_FILE, logResult);
        }
        catch (System.Exception ex)
        {
            System.IO.File.WriteAllText(RES_FILE, "ERROR: " + ex.ToString());
            Debug.LogException(ex);
        }
    }

    public static void CaptureAfterEvidences()
    {
        EnsureLevel2Open();
        System.IO.Directory.CreateDirectory("Assets/Screenshots/NIVEL2/Evidencias");
        CaptureCameraAt(new Vector3(15.5f, 4.5f, -10f), 6.5f, "Assets/Screenshots/NIVEL2/Evidencias/AFTER_Casa1_Fondo.png");
        CaptureCameraAt(new Vector3(45.0f, 4.5f, -10f), 6.5f, "Assets/Screenshots/NIVEL2/Evidencias/AFTER_Casa2_Exterior.png");
        CaptureCameraAt(new Vector3(49.0f, 27.5f, -10f), 4.5f, "Assets/Screenshots/NIVEL2/Evidencias/AFTER_Sotano_House2.png");
        CaptureCameraAt(new Vector3(109.0f, 4.5f, -10f), 6.5f, "Assets/Screenshots/NIVEL2/Evidencias/AFTER_Casa3_Exterior.png");
        CaptureCameraAt(new Vector3(149.0f, 4.5f, -10f), 6.5f, "Assets/Screenshots/NIVEL2/Evidencias/AFTER_Casa4_Exterior.png");
        Debug.Log("[Level2CompleteFixer] Capturas AFTER guardadas en Assets/Screenshots/NIVEL2/Evidencias/");
    }

    private static void CaptureCameraAt(Vector3 pos, float orthoSize, string outPath)
    {
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null) return;

        Vector3 oldPos = cam.transform.position;
        float oldSize = cam.orthographicSize;
        RenderTexture oldRt = cam.targetTexture;

        cam.transform.position = pos;
        cam.orthographicSize = orthoSize;

        int w = 1920, h = 1080;
        RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        System.IO.File.WriteAllBytes(outPath, tex.EncodeToPNG());

        RenderTexture.active = null;
        cam.targetTexture = oldRt;
        cam.transform.position = oldPos;
        cam.orthographicSize = oldSize;

        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }

    [MenuItem("Tools/Nivel 2/1. Aplicar Bloque A (Sotano y Escaleras Interiores)")]
    public static void ApplyBlockA_BasementAndLadders()
    {
        EnsureLevel2Open();
        int groundLayer = LayerMask.NameToLayer("Ground");

        FixInteriorHouseLadders("Interior_House1_Apothecary", groundLayer);
        FixInteriorHouseLadders("Interior_House2_Tavern", groundLayer);
        FixInteriorHouseLadders("Interior_House3_Abandoned", groundLayer);
        FixInteriorHouseLadders("Interior_House4_Manor", groundLayer);

        BuildCellarWalkableStepsAndBackup("Interior_House2_Tavern", 54.0f, 41.5f, 52.8f, groundLayer);
        BuildCellarWalkableStepsAndBackup("Interior_House4_Manor", 141.6f, 142.8f, 152.0f, groundLayer);

        SaveLevel2();
        Debug.Log("[Level2CompleteFixer] Bloque A (Sotano y Escaleras Interiores) aplicado correctamente.");
    }

    private static void FixInteriorHouseLadders(string interiorName, int groundLayer)
    {
        GameObject root = GameObject.Find("Environment/Interiors/" + interiorName);
        if (root == null) return;

        Collider2D[] allSlabColliders = root.GetComponentsInChildren<Collider2D>(true)
            .Where(c => !c.isTrigger && (c.name.Contains("Slab") || c.name.Contains("Landing") || c.name.Contains("Floor")))
            .ToArray();

        Sprite plankSprite = LoadSingleSprite("Assets/Art/Environment/Level2/Interior_Floor_Planks.png");

        LadderInteractable[] ladders = root.GetComponentsInChildren<LadderInteractable>(true);
        foreach (LadderInteractable ladder in ladders)
        {
            SerializedObject so = new SerializedObject(ladder);
            float centerX = ladder.transform.position.x;
            float bottomY = SnapToInteriorFloorSurface(ladder.BottomY);
            float topY = SnapToInteriorFloorSurface(ladder.TopY);

            so.FindProperty("bottomY").floatValue = bottomY;
            so.FindProperty("topY").floatValue = topY;
            so.FindProperty("autoCalculateBounds").boolValue = false;
            so.FindProperty("topStandTriggerMargin").floatValue = 0.75f;
            so.ApplyModifiedPropertiesWithoutUndo();

            float height = topY - bottomY;
            ladder.transform.position = new Vector3(centerX, bottomY + height * 0.5f, 0f);
            BoxCollider2D trig = ladder.GetComponent<BoxCollider2D>();
            if (trig != null)
            {
                trig.isTrigger = true;
                trig.offset = Vector2.zero;
                trig.size = new Vector2(1.35f, height + 0.6f);
            }

            string bridgeName = "LadderTopBridge_" + ladder.name;
            Transform existingBridge = root.transform.Find(bridgeName);
            GameObject bridgeGO = existingBridge != null ? existingBridge.gameObject : new GameObject(bridgeName);
            bridgeGO.transform.SetParent(root.transform, true);
            bridgeGO.layer = groundLayer;
            bridgeGO.transform.position = new Vector3(centerX, topY - 0.25f, 0f);
            bridgeGO.transform.localScale = Vector3.one;

            if (plankSprite != null)
            {
                SpriteRenderer bsr = bridgeGO.GetComponent<SpriteRenderer>();
                if (bsr == null) bsr = bridgeGO.AddComponent<SpriteRenderer>();
                bsr.sprite = plankSprite;
                bsr.drawMode = SpriteDrawMode.Tiled;
                bsr.size = new Vector2(2.6f, 0.5f);
                bsr.sortingLayerName = "Gameplay";
                bsr.sortingOrder = -1;
            }

            BoxCollider2D bridgeCol = bridgeGO.GetComponent<BoxCollider2D>();
            if (bridgeCol == null) bridgeCol = bridgeGO.AddComponent<BoxCollider2D>();
            bridgeCol.isTrigger = false;
            bridgeCol.offset = Vector2.zero;
            bridgeCol.size = new Vector2(2.6f, 0.5f);
            bridgeCol.usedByEffector = true;

            PlatformEffector2D eff = bridgeGO.GetComponent<PlatformEffector2D>();
            if (eff == null) eff = bridgeGO.AddComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.useOneWayGrouping = true;
            eff.surfaceArc = 170f;
            eff.rotationalOffset = 0f;

            List<Collider2D> passList = new List<Collider2D> { bridgeCol };
            foreach (Collider2D sc in allSlabColliders)
            {
                if (sc != null && Mathf.Abs(sc.bounds.max.y - topY) < 0.8f && !passList.Contains(sc))
                {
                    passList.Add(sc);
                }
            }

            SerializedObject so2 = new SerializedObject(ladder);
            SerializedProperty propPass = so2.FindProperty("passThroughPlatforms");
            propPass.ClearArray();
            for (int i = 0; i < passList.Count; i++)
            {
                propPass.InsertArrayElementAtIndex(i);
                propPass.GetArrayElementAtIndex(i).objectReferenceValue = passList[i];
            }
            so2.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ladder);
        }
    }

    private static float SnapToInteriorFloorSurface(float y)
    {
        float[] validSurfaces = { 24.75f, 28.75f, 32.75f, 36.75f };
        float best = y;
        float minDiff = 999f;
        foreach (float s in validSurfaces)
        {
            float d = Mathf.Abs(y - s);
            if (d < minDiff && d < 1.2f)
            {
                minDiff = d;
                best = s;
            }
        }
        return best;
    }

    private static void BuildCellarWalkableStepsAndBackup(string interiorName, float ladderX, float stepStartX, float stepEndX, int groundLayer)
    {
        GameObject root = GameObject.Find("Environment/Interiors/" + interiorName);
        if (root == null) return;

        Transform oldSteps = root.transform.Find("Cellar_WalkableSteps");
        if (oldSteps != null) Object.DestroyImmediate(oldSteps.gameObject);

        GameObject stepsRoot = new GameObject("Cellar_WalkableSteps");
        stepsRoot.transform.SetParent(root.transform, false);
        stepsRoot.transform.position = Vector3.zero;

        Sprite plankSprite = LoadSingleSprite("Assets/Art/Environment/Level2/Interior_Floor_Planks.png");

        int stepCount = 8;
        for (int i = 1; i <= stepCount; i++)
        {
            float t = (float)i / stepCount;
            float stepX = Mathf.Lerp(stepStartX, stepEndX, t);
            float stepTopY = 24.75f + i * 0.50f;
            float stepH = 0.24f;

            GameObject stepGO = new GameObject($"CellarStep_{i}");
            stepGO.transform.SetParent(stepsRoot.transform, true);
            stepGO.layer = groundLayer;
            stepGO.transform.position = new Vector3(stepX, stepTopY - stepH * 0.5f, 0f);

            if (plankSprite != null)
            {
                SpriteRenderer ssr = stepGO.AddComponent<SpriteRenderer>();
                ssr.sprite = plankSprite;
                ssr.drawMode = SpriteDrawMode.Tiled;
                ssr.size = new Vector2(1.45f, stepH);
                ssr.color = new Color(0.85f, 0.78f, 0.70f, 0.95f);
                ssr.sortingLayerName = "Gameplay";
                ssr.sortingOrder = -1;
            }

            BoxCollider2D col = stepGO.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.45f, stepH);
            col.usedByEffector = true;

            PlatformEffector2D eff = stepGO.AddComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.useOneWayGrouping = true;
            eff.surfaceArc = 170f;
        }

        Transform oldBackup = root.transform.Find("CellarExitBackupTrigger");
        if (oldBackup != null) Object.DestroyImmediate(oldBackup.gameObject);

        GameObject backupGO = new GameObject("CellarExitBackupTrigger");
        backupGO.transform.SetParent(root.transform, true);
        backupGO.transform.position = new Vector3(ladderX, 25.45f, 0f);

        BoxCollider2D backupTrig = backupGO.AddComponent<BoxCollider2D>();
        backupTrig.isTrigger = true;
        backupTrig.size = new Vector2(1.8f, 1.4f);

        CellarExitBackupInteractable backupComp = backupGO.AddComponent<CellarExitBackupInteractable>();
        float safeExitX = ladderX > 100f ? ladderX + 1.4f : ladderX - 1.4f;
        backupComp.Configure("cellar_exit_" + interiorName, safeExitX, 28.75f, "Subir a Planta Baja");
        EditorUtility.SetDirty(backupComp);
    }

    [MenuItem("Tools/Nivel 2/2. Aplicar Bloque B (Eliminar Escaleras Huerfanas y Alinear Puertas)")]
    public static void ApplyBlockB_OrphanLaddersAndDoors()
    {
        EnsureLevel2Open();
        Sprite[] houseSprites = LoadHouseSprites();
        Transform extRoot = GameObject.Find("Environment/Village_Exterior_Structures")?.transform;
        if (extRoot == null)
        {
            Debug.LogError("[Level2CompleteFixer] No se encontro Environment/Village_Exterior_Structures");
            return;
        }

        // 1. Eliminar pasarelas y escaleras huerfanas que flotaban sin casa detras
        string[] orphans =
        {
            "Walkway_Ext_Z1_Bridge",
            "Walkway_Ext_Z2_West",
            "Walkway_Ext_Z2_East",
            "Ladder_Ext_Z2_East"
        };
        foreach (string n in orphans)
        {
            Transform t = extRoot.Find(n);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        // 2. Desplazar puestos de mercado que tapaban las puertas de House2 (x=45) y House4 (x=149)
        MoveStall("Environment/Zone_2_PuestosBajos/Stall_Z2_2", 38.8f);
        MoveStall("Environment/Zone_5_GranMercado/Stall_Z5_2", 142.8f);

        // 3. Reubicar Chest_Ext_Z2_Balcony sobre el balcon real de House2 (RoofWalk_Z2 en x=45.0, topY=4.02)
        Transform chestZ2 = extRoot.Find("Chest_Ext_Z2_Balcony");
        if (chestZ2 != null)
        {
            chestZ2.position = new Vector3(46.3f, 4.02f, 0f);
            SnapSpriteBottomToY(chestZ2.gameObject, 4.02f);
        }

        // 4. Conectar cada escalera exterior directamente a su casa y plataforma real (BottomY y TopY exactos)
        // House 1 (x=15.5): Balcony_Ext_House1 esta en x=15.5, topY=4.02
        float h1TopY = GetPlatformTopY("Environment/Village_Exterior_Structures/Balcony_Ext_House1", 4.02f);
        CreateOrUpdateExteriorLadder(extRoot, "Ladder_Ext_Z1_Balcony", 13.6f, GROUND_SURFACE_Y, h1TopY, "Environment/Village_Exterior_Structures/Balcony_Ext_House1");

        // House 2 (x=45.0): RoofWalk_Z2 esta en x=45.0, topY=4.02
        float h2TopY = GetPlatformTopY("Environment/Zone_2_PuestosBajos/RoofWalk_Z2", 4.02f);
        CreateOrUpdateExteriorLadder(extRoot, "Ladder_Ext_Z2_West", 43.2f, GROUND_SURFACE_Y, h2TopY, "Environment/Zone_2_PuestosBajos/RoofWalk_Z2");

        // Zona 3: Roof_Z3_LeftHigh (x=71.0, topY=4.22) y Roof_Z3_RightHigh (x=87.0, topY=4.42)
        float z3LeftTopY = GetPlatformTopY("Environment/Zone_3_PlazaCentral/Roof_Z3_LeftHigh", 4.22f);
        float z3RightTopY = GetPlatformTopY("Environment/Zone_3_PlazaCentral/Roof_Z3_RightHigh", 4.42f);
        CreateOrUpdateExteriorLadder(extRoot, "Ladder_Ext_Z3_Left", 69.5f, GROUND_SURFACE_Y, z3LeftTopY, "Environment/Zone_3_PlazaCentral/Roof_Z3_LeftHigh");
        CreateOrUpdateExteriorLadder(extRoot, "Ladder_Ext_Z3_Right", 88.5f, GROUND_SURFACE_Y, z3RightTopY, "Environment/Zone_3_PlazaCentral/Roof_Z3_RightHigh");

        // House 3 (x=109.0): Roof_Z4_OverAlley (x=110.0, topY=4.62)
        // Ensanchar ligeramente Roof_Z4_OverAlley o ajustar posicion de escalera y puertas de House3 para que coincidan con la fachada en x=109.0
        GameObject roofZ4 = GameObject.Find("Environment/Zone_4_CallejonGato/Roof_Z4_OverAlley");
        if (roofZ4 != null)
        {
            roofZ4.transform.position = new Vector3(110.0f, 4.40f, 0f);
            BoxCollider2D rc = roofZ4.GetComponent<BoxCollider2D>();
            if (rc != null && rc.size.x < 7.5f) rc.size = new Vector2(8.0f, rc.size.y);
        }
        Physics2D.SyncTransforms();
        float h3TopY = GetPlatformTopY("Environment/Zone_4_CallejonGato/Roof_Z4_OverAlley", 4.62f);
        CreateOrUpdateExteriorLadder(extRoot, "Ladder_Ext_Z4_RoofExit", 112.2f, GROUND_SURFACE_Y, h3TopY, "Environment/Zone_4_CallejonGato/Roof_Z4_OverAlley");

        Transform dH3G = extRoot.Find("Door_Ext_House3_Ground");
        if (dH3G != null) dH3G.position = new Vector3(107.2f, dH3G.position.y, 0f);
        Transform dH3R = extRoot.Find("Door_Ext_House3_Roof");
        if (dH3R != null) dH3R.position = new Vector3(109.5f, h3TopY + 0.8f, 0f);

        // Zona 5: Roof_Z5_Low_1 (x=136.0 -> extender a x=137.5, ancho 7.5 para conectar con Ladder_Ext_Z5_HighTower en x=140.5)
        GameObject roofZ5Low = GameObject.Find("Environment/Zone_5_GranMercado/Roof_Z5_Low_1");
        if (roofZ5Low != null)
        {
            roofZ5Low.transform.position = new Vector3(137.5f, 3.80f, 0f);
            BoxCollider2D rc = roofZ5Low.GetComponent<BoxCollider2D>();
            if (rc != null && rc.size.x < 7.5f) rc.size = new Vector2(7.6f, rc.size.y);
        }
        GameObject roofZ5High = GameObject.Find("Environment/Zone_5_GranMercado/Roof_Z5_HighWalk");
        if (roofZ5High != null)
        {
            roofZ5High.transform.position = new Vector3(142.5f, 6.20f, 0f);
            BoxCollider2D rc = roofZ5High.GetComponent<BoxCollider2D>();
            if (rc != null && rc.size.x < 6.0f) rc.size = new Vector2(6.5f, rc.size.y);
        }
        GameObject roofZ5Mid = GameObject.Find("Environment/Zone_5_GranMercado/Roof_Z5_Mid_2");
        if (roofZ5Mid != null)
        {
            roofZ5Mid.transform.position = new Vector3(149.0f, 4.40f, 0f);
            BoxCollider2D rc = roofZ5Mid.GetComponent<BoxCollider2D>();
            if (rc != null && rc.size.x < 6.5f) rc.size = new Vector2(7.0f, rc.size.y);
        }
        Physics2D.SyncTransforms();

        float z5LowTopY = GetPlatformTopY("Environment/Zone_5_GranMercado/Roof_Z5_Low_1", 4.02f);
        float z5HighTopY = GetPlatformTopY("Environment/Zone_5_GranMercado/Roof_Z5_HighWalk", 6.42f);
        float z5MidTopY = GetPlatformTopY("Environment/Zone_5_GranMercado/Roof_Z5_Mid_2", 4.62f);

        CreateOrUpdateExteriorLadder(extRoot, "Ladder_Ext_Z5_West", 134.5f, GROUND_SURFACE_Y, z5LowTopY, "Environment/Zone_5_GranMercado/Roof_Z5_Low_1");
        CreateOrUpdateExteriorLadder(extRoot, "Ladder_Ext_Z5_HighTower", 140.4f, z5LowTopY, z5HighTopY, "Environment/Zone_5_GranMercado/Roof_Z5_HighWalk");
        CreateOrUpdateExteriorLadder(extRoot, "Ladder_Ext_Z5_East", 151.4f, GROUND_SURFACE_Y, z5MidTopY, "Environment/Zone_5_GranMercado/Roof_Z5_Mid_2");

        // 5. Colocar fachadas de casas coherentes detras de las puertas independientes (Quarantine y Sanctum) y plataformas con escalera
        if (houseSprites.Length > 0)
        {
            PlaceFacadeHouse(extRoot, "House_Struct_Z3_Quarantine", houseSprites[0], 62.8f, GROUND_SURFACE_Y, 1.12f, false, -3);
            PlaceFacadeHouse(extRoot, "House_Struct_Z3_Left",       houseSprites[3], 70.5f, GROUND_SURFACE_Y, 1.15f, false, -3);
            PlaceFacadeHouse(extRoot, "House_Struct_Z3_Right",      houseSprites[4], 87.5f, GROUND_SURFACE_Y, 1.15f, true,  -3);
            PlaceFacadeHouse(extRoot, "House_Struct_Z5_West",       houseSprites[5], 136.5f, GROUND_SURFACE_Y, 1.18f, false, -3);
            PlaceFacadeHouse(extRoot, "House_Struct_Z5_Tower",      houseSprites[2], 141.8f, GROUND_SURFACE_Y, 1.38f, false, -4);
            PlaceFacadeHouse(extRoot, "House_Struct_Z6_Sanctum",    houseSprites[6 % houseSprites.Length], 173.0f, GROUND_SURFACE_Y, 1.15f, false, -3);
        }

        // 6. Alinear todas las puertas (exteriores e interiores) exactamente a su suelo/balcon (<= 0.001u)
        AlignAllDoorsToFloorSurfaces();

        SaveLevel2();
        Debug.Log("[Level2CompleteFixer] Bloque B (Escaleras huerfanas eliminadas y puertas alineadas) aplicado correctamente.");
    }

    [MenuItem("Tools/Nivel 2/3. Aplicar Bloque C (Casas Decorativas con houses_sprite)")]
    public static void ApplyBlockC_DecorativeHouses()
    {
        EnsureLevel2Open();
        Sprite[] houseSprites = LoadHouseSprites();
        if (houseSprites.Length == 0)
        {
            Debug.LogError("[Level2CompleteFixer] No se encontraron sprites en " + HOUSES_ATLAS_PATH);
            return;
        }

        Transform extRoot = GameObject.Find("Environment/Village_Exterior_Structures")?.transform;
        if (extRoot == null) return;

        List<GameObject> oldDecos = new List<GameObject>();
        foreach (Transform child in extRoot)
        {
            if (child.name.StartsWith("House_Deco_"))
            {
                oldDecos.Add(child.gameObject);
            }
        }
        foreach (GameObject go in oldDecos) Object.DestroyImmediate(go);

        // Casas decorativas a lo largo de las 6 zonas (evitando el canal x=74.5..81.5 y las 4 casas interactivas)
        (string name, float x, int spriteIdx, float scale, bool flip)[] decoSpecs = new[]
        {
            ("House_Deco_Z1_01", 4.5f,   0,  0.96f, false),
            ("House_Deco_Z1_02", 9.8f,   4,  0.94f, true),
            ("House_Deco_Z1_03", 22.5f,  6,  1.02f, false),
            ("House_Deco_Z2_01", 31.0f,  8,  0.96f, false),
            ("House_Deco_Z2_02", 37.5f,  10, 1.03f, true),
            ("House_Deco_Z2_03", 52.0f,  11, 0.95f, false),
            ("House_Deco_Z2_04", 57.5f,  7,  0.92f, true),
            ("House_Deco_Z4_01", 98.0f,  9,  0.98f, false),
            ("House_Deco_Z4_02", 118.5f, 1,  1.04f, true),
            ("House_Deco_Z4_03", 123.5f, 5,  0.94f, false),
            ("House_Deco_Z5_01", 129.5f, 3,  0.97f, true),
            ("House_Deco_Z5_02", 157.5f, 8,  1.01f, false),
            ("House_Deco_Z5_03", 163.0f, 10, 0.95f, true),
            ("House_Deco_Z6_01", 168.0f, 4,  0.96f, false),
            ("House_Deco_Z6_02", 179.5f, 11, 1.00f, true),
            ("House_Deco_Z6_03", 186.0f, 0,  0.93f, false)
        };

        foreach (var spec in decoSpecs)
        {
            Sprite sp = houseSprites[spec.spriteIdx % houseSprites.Length];
            PlaceFacadeHouse(extRoot, spec.name, sp, spec.x, GROUND_SURFACE_Y, spec.scale, spec.flip, -4);
        }

        ReplaceAnyRemainingOldHouseSprites(houseSprites);

        SaveLevel2();
        Debug.Log("[Level2CompleteFixer] Bloque C (Casas Decorativas con houses_sprite.png.png) aplicado correctamente.");
    }

    [MenuItem("Tools/Nivel 2/4. Aplicar Bloque D (Casas Interactivas Tecla E con houses_sprite)")]
    public static void ApplyBlockD_InteractiveHouses()
    {
        EnsureLevel2Open();
        Sprite[] houseSprites = LoadHouseSprites();
        if (houseSprites.Length == 0) return;

        Transform extRoot = GameObject.Find("Environment/Village_Exterior_Structures")?.transform;
        if (extRoot == null) return;

        ConfigureInteractiveHouse(extRoot, "House1_Facade_Apothecary", houseSprites[1 % houseSprites.Length], 15.5f, 1.22f, false);
        ConfigureInteractiveHouse(extRoot, "House2_Facade_Tavern",     houseSprites[2 % houseSprites.Length], 45.0f, 1.22f, false);
        ConfigureInteractiveHouse(extRoot, "House3_Facade_Abandoned",  houseSprites[5 % houseSprites.Length], 109.0f, 1.28f, false);
        ConfigureInteractiveHouse(extRoot, "House4_Facade_Manor",      houseSprites[3 % houseSprites.Length], 149.0f, 1.26f, false);

        AlignAllDoorsToFloorSurfaces();

        SaveLevel2();
        Debug.Log("[Level2CompleteFixer] Bloque D (Casas Interactivas con houses_sprite.png.png) aplicado correctamente.");
    }

    [MenuItem("Tools/Nivel 2/5. Aplicar Bloque E (Nuevo Fondo con Parallax >= 3 Capas)")]
    public static void ApplyBlockE_NewParallaxBackground()
    {
        EnsureLevel2Open();

        Sprite skySprite = LoadSingleSprite(SKY_FONDO_PATH);
        if (skySprite == null) skySprite = LoadSingleSprite(SKY_CLEAN_PATH);
        Sprite farSprite = LoadSingleSprite(FAR_CLEAN_PATH);
        Sprite midSprite = LoadSingleSprite(MID_CLEAN_PATH);

        if (skySprite == null || farSprite == null || midSprite == null)
        {
            Debug.LogError($"[Level2CompleteFixer] Faltan sprites de fondo: sky={skySprite}, far={farSprite}, mid={midSprite}");
            return;
        }

        GameObject parallaxRoot = GameObject.Find("Environment/Parallax");
        if (parallaxRoot == null)
        {
            GameObject env = GameObject.Find("Environment");
            parallaxRoot = new GameObject("Parallax");
            if (env != null) parallaxRoot.transform.SetParent(env.transform, false);
        }
        parallaxRoot.transform.position = Vector3.zero;
        parallaxRoot.transform.localScale = Vector3.one;

        // Eliminar capas antiguas si tuvieran otro nombre (p. ej. Trees_Mid o Village_Mid)
        List<GameObject> oldLayers = new List<GameObject>();
        foreach (Transform ch in parallaxRoot.transform)
        {
            if (ch.name != "Sky_Far" && ch.name != "Village_Far" && ch.name != "Facades_Mid")
            {
                oldLayers.Add(ch.gameObject);
            }
        }
        foreach (var go in oldLayers) Object.DestroyImmediate(go);

        BuildParallaxLayer(parallaxRoot.transform, "Sky_Far",     skySprite, new Vector2(0.90f, 0.95f), -20, 4.8f, 1.85f, Color.white);
        BuildParallaxLayer(parallaxRoot.transform, "Village_Far", farSprite, new Vector2(0.72f, 0.88f), -15, 4.1f, 1.65f, new Color(0.92f, 0.94f, 1.0f, 0.95f));
        BuildParallaxLayer(parallaxRoot.transform, "Facades_Mid", midSprite, new Vector2(0.42f, 0.75f), -10, 3.6f, 1.60f, Color.white);

        SaveLevel2();
        Debug.Log("[Level2CompleteFixer] Bloque E (Nuevo Fondo con Parallax >= 3 capas) aplicado correctamente.");
    }

    private static void BuildParallaxLayer(Transform parent, string layerName, Sprite sprite, Vector2 factor, int sortingOrder, float posY, float uniformScale, Color tint)
    {
        Transform layerTr = parent.Find(layerName);
        GameObject layerGO = layerTr != null ? layerTr.gameObject : new GameObject(layerName);
        layerGO.transform.SetParent(parent, false);
        layerGO.transform.position = new Vector3(0f, posY, 0f);
        layerGO.transform.localRotation = Quaternion.identity;
        layerGO.transform.localScale = Vector3.one;

        for (int i = layerGO.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(layerGO.transform.GetChild(i).gameObject);
        }

        SpriteRenderer parentSr = layerGO.GetComponent<SpriteRenderer>();
        if (parentSr != null) Object.DestroyImmediate(parentSr);

        ParallaxLayer pl = layerGO.GetComponent<ParallaxLayer>();
        if (pl == null) pl = layerGO.AddComponent<ParallaxLayer>();
        pl.Setup(factor.x, true, true);

        float tileWorldWidth = (sprite.rect.width / sprite.pixelsPerUnit) * uniformScale;

        for (int i = -1; i <= 5; i++)
        {
            GameObject copy = new GameObject($"Copy_{i + 1}");
            copy.transform.SetParent(layerGO.transform, false);
            copy.transform.localPosition = new Vector3(i * tileWorldWidth, 0f, 0f);
            copy.transform.localRotation = Quaternion.identity;
            copy.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);

            SpriteRenderer sr = copy.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Simple;
            sr.color = tint;
            sr.sortingLayerName = "Background";
            sr.sortingOrder = sortingOrder;
        }

        EditorUtility.SetDirty(layerGO);
    }

    private static void ConfigureInteractiveHouse(Transform parent, string name, Sprite sprite, float x, float scale, bool flipX)
    {
        Transform tr = parent.Find(name);
        GameObject go = tr != null ? tr.gameObject : new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = new Vector3(scale, scale, 1f);

        foreach (Collider2D c in go.GetComponents<Collider2D>())
        {
            if (!c.isTrigger) Object.DestroyImmediate(c);
        }

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Simple;
        sr.color = Color.white;
        sr.flipX = flipX;
        sr.sortingLayerName = "Gameplay";
        sr.sortingOrder = -2;

        float groundY = RaycastGroundY(x, GROUND_SURFACE_Y);
        go.transform.position = new Vector3(x, groundY, 0f);
        SnapSpriteBottomToY(go, groundY);
        EditorUtility.SetDirty(go);
    }

    private static void PlaceFacadeHouse(Transform parent, string name, Sprite sprite, float x, float defaultGroundY, float scale, bool flipX, int sortingOrder)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = new Vector3(scale, scale, 1f);

        foreach (Collider2D col in go.GetComponents<Collider2D>())
        {
            Object.DestroyImmediate(col);
        }

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Simple;
        sr.color = Color.white;
        sr.flipX = flipX;
        sr.sortingLayerName = "Gameplay";
        sr.sortingOrder = sortingOrder;

        float groundY = RaycastGroundY(x, defaultGroundY);
        go.transform.position = new Vector3(x, groundY, 0f);
        SnapSpriteBottomToY(go, groundY);
        EditorUtility.SetDirty(go);
    }

    private static void SnapSpriteBottomToY(GameObject go, float targetY)
    {
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;
        float delta = sr.bounds.min.y - targetY;
        if (Mathf.Abs(delta) > 0.0001f)
        {
            go.transform.position -= new Vector3(0f, delta, 0f);
        }
    }

    private static void ReplaceAnyRemainingOldHouseSprites(Sprite[] houseSprites)
    {
        SpriteRenderer[] allSrs = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int idx = 0;
        foreach (SpriteRenderer sr in allSrs)
        {
            if (sr == null || sr.sprite == null) continue;
            string sName = sr.sprite.name;
            if (sName.Contains("Village_House_Facade") || sName.Contains("Placeholder_Village_Facades") || sName.Contains("Env_Village_Mid"))
            {
                sr.sprite = houseSprites[idx % houseSprites.Length];
                sr.drawMode = SpriteDrawMode.Simple;
                sr.color = Color.white;
                SnapSpriteBottomToY(sr.gameObject, RaycastGroundY(sr.transform.position.x, GROUND_SURFACE_Y));
                idx++;
                EditorUtility.SetDirty(sr);
            }
        }
    }

    private static float GetPlatformTopY(string path, float fallback)
    {
        GameObject go = GameObject.Find(path);
        if (go == null) return fallback;
        Collider2D col = go.GetComponent<Collider2D>();
        return col != null ? col.bounds.max.y : fallback;
    }

    private static void CreateOrUpdateExteriorLadder(Transform parent, string name, float x, float bottomY, float topY, string topPlatformPath)
    {
        if (parent == null) return;
        Transform existing = parent.Find(name);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            Transform template = parent.Find("Ladder_Ext_Z3_Left");
            go = template != null ? Object.Instantiate(template.gameObject, parent) : new GameObject(name);
            go.name = name;
        }

        float height = topY - bottomY;
        go.transform.position = new Vector3(x, bottomY + height * 0.5f, 0f);
        go.transform.localScale = Vector3.one;

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (sr.drawMode != SpriteDrawMode.Simple)
            {
                sr.size = new Vector2(0.85f, height);
            }
            sr.sortingLayerName = "Gameplay";
            sr.sortingOrder = -1;
        }

        BoxCollider2D trig = go.GetComponent<BoxCollider2D>();
        if (trig == null) trig = go.AddComponent<BoxCollider2D>();
        trig.isTrigger = true;
        trig.offset = Vector2.zero;
        trig.size = new Vector2(1.25f, height + 0.5f);

        LadderInteractable li = go.GetComponent<LadderInteractable>();
        if (li == null) li = go.AddComponent<LadderInteractable>();

        GameObject topPlat = GameObject.Find(topPlatformPath);
        Collider2D topCol = topPlat != null ? topPlat.GetComponent<Collider2D>() : null;

        SerializedObject so = new SerializedObject(li);
        so.FindProperty("bottomY").floatValue = bottomY;
        so.FindProperty("topY").floatValue = topY;
        so.FindProperty("autoCalculateBounds").boolValue = false;
        so.FindProperty("topStandTriggerMargin").floatValue = 0.65f;
        SerializedProperty passProp = so.FindProperty("passThroughPlatforms");
        passProp.ClearArray();
        if (topCol != null)
        {
            passProp.InsertArrayElementAtIndex(0);
            passProp.GetArrayElementAtIndex(0).objectReferenceValue = topCol;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(go);
    }

    private static void AlignAllDoorsToFloorSurfaces()
    {
        Physics2D.SyncTransforms();
        HouseDoor[] doors = Object.FindObjectsByType<HouseDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (HouseDoor door in doors)
        {
            if (door == null) continue;
            GameObject go = door.gameObject;
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            float currentBottom = sr != null && sr.sprite != null ? sr.bounds.min.y : go.transform.position.y;
            float targetFloorY = FindSurfaceBelow(go.transform.position.x, currentBottom + 0.8f);

            if (sr != null && sr.sprite != null)
            {
                float shiftY = targetFloorY - sr.bounds.min.y;
                go.transform.position += new Vector3(0f, shiftY, 0f);
            }
            else
            {
                go.transform.position = new Vector3(go.transform.position.x, targetFloorY, 0f);
            }

            // Also update safeFloorY in HouseDoor so player spawns at the exact floor surface
            SerializedObject so = new SerializedObject(door);
            SerializedProperty propFloor = so.FindProperty("safeFloorY");
            if (propFloor != null)
            {
                propFloor.floatValue = targetFloorY;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(go);
        }
    }

    private static void MoveStall(string path, float newX)
    {
        GameObject go = GameObject.Find(path);
        if (go == null) return;
        Vector3 pos = go.transform.position;
        go.transform.position = new Vector3(newX, pos.y, pos.z);
        EditorUtility.SetDirty(go);
    }

    public static float RaycastGroundY(float x, float fallbackY)
    {
        int groundMask = 1 << LayerMask.NameToLayer("Ground");
        RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(x, 1.0f), Vector2.down, 3.0f, groundMask);
        foreach (var h in hits)
        {
            if (h.collider != null && !h.collider.isTrigger && h.point.y < 0.35f)
            {
                return h.point.y;
            }
        }
        return fallbackY;
    }

    public static float FindSurfaceBelow(float x, float startY)
    {
        int groundMask = 1 << LayerMask.NameToLayer("Ground");
        RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(x, startY), Vector2.down, 4.0f, groundMask);
        float bestY = GROUND_SURFACE_Y;
        float minDist = 999f;
        foreach (var h in hits)
        {
            if (h.collider != null && !h.collider.isTrigger)
            {
                float d = Mathf.Abs((startY - 0.8f) - h.point.y);
                if (d < minDist)
                {
                    minDist = d;
                    bestY = h.point.y;
                }
            }
        }
        return bestY;
    }

    public static Sprite[] LoadHouseSprites()
    {
        Object[] all = AssetDatabase.LoadAllAssetsAtPath(HOUSES_ATLAS_PATH);
        return all.OfType<Sprite>().OrderBy(s => s.name).ToArray();
    }

    private static Sprite LoadSingleSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s != null) return s;
        Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
        return all.OfType<Sprite>().FirstOrDefault();
    }

    private static void EnsureLevel2Open()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.path != LEVEL2_SCENE_PATH)
        {
            EditorSceneManager.OpenScene(LEVEL2_SCENE_PATH, OpenSceneMode.Single);
        }
        Physics2D.SyncTransforms();
    }

    private static void SaveLevel2()
    {
        Physics2D.SyncTransforms();
        Scene active = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(active);
        EditorSceneManager.SaveScene(active);
    }
}
#endif
