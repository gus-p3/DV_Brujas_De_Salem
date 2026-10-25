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
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying = false;
            return;
        }

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
                case "MEGA_A":
                    ApplyMegaBlockA_CatPickupGuards();
                    break;
                case "MEGA_B":
                    ApplyMegaBlockB_MarketStallAnimations();
                    break;
                case "MEGA_C":
                    ApplyMegaBlockC_InteriorExitDoorsAndIndicators();
                    break;
                case "MEGA_D":
                    ApplyMegaBlockD_QKeyForDoors();
                    break;
                case "MEGA_E":
                    ApplyBlockE_NewParallaxBackground();
                    break;
                case "TASK_1":
                    ApplyTaskBlock1_Controls();
                    break;
                case "TASK_2":
                    ApplyTaskBlock2_ChestsSystemAndPrefab();
                    break;
                case "TASK_3":
                    ApplyTaskBlock3_PlaceItemsInChests();
                    break;
                case "TASK_4":
                    ApplyTaskBlock4_RecomposedBackground();
                    break;
                case "TASK_5":
                    ApplyTaskBlock5_CompleteElevatedStalls();
                    break;
                case "CAPTURE_TASK_BEFORE":
                    CaptureTaskBeforeEvidences();
                    break;
                case "CAPTURE_TASK_AFTER":
                    CaptureTaskAfterEvidences();
                    break;
                case "VERIFY_TASK":
                    int tCount = Level2StructureVerifier.VerifyControlsChestsAndBackground();
                    int m2Count = Level2StructureVerifier.VerifyMegaCorrections();
                    int s2Count = Level2StructureVerifier.VerifyAllStructures();
                    bool ingOk = Level2IngredientVerifier.RunAllTests();
                    Level2StealthVerifier.RunVerification();
                    var exp2Report = Level2.Editor.Level2ExplorationVerifier.RunAllExplorationTests();
                    logResult = $"VERIFY_TASK_DONE: TaskFindings={tCount}, MegaFindings={m2Count}, StructureFindings={s2Count}, IngOk={ingOk}, Exploration={exp2Report.passed}/{exp2Report.total}";
                    break;
                case "CAPTURE_MEGA_BEFORE":
                    CaptureMegaBeforeEvidences();
                    break;
                case "CAPTURE_MEGA_AFTER":
                    CaptureMegaAfterEvidences();
                    break;
                case "VERIFY_MEGA":
                    int mCount = Level2StructureVerifier.VerifyMegaCorrections();
                    int sCount = Level2StructureVerifier.VerifyAllStructures();
                    Level2GroundingTool.VerifyMenu();
                    var expReport = Level2.Editor.Level2ExplorationVerifier.RunAllExplorationTests();
                    logResult = $"VERIFY_MEGA_DONE: MegaFindings={mCount}, StructureFindings={sCount}, Exploration={expReport.passed}/{expReport.total}";
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
        Level2BackgroundProcessor.ImportAndProcessSourceImage();
        Level2BackgroundProcessor.ApplyBackgroundToLevel2();
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
        pl.Setup(factor.x, false, true);

        float tileWorldWidth = (sprite.rect.width / sprite.pixelsPerUnit) * uniformScale;

        for (int i = -2; i <= 6; i++)
        {
            GameObject copy = new GameObject($"Copy_{i + 2}");
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

    [MenuItem("Tools/Nivel 2/Mega Correccion/A. Proteger Pickups contra Forma de Gato")]
    public static void ApplyMegaBlockA_CatPickupGuards()
    {
        EnsureLevel2Open();
        int attached = 0;

        MonoBehaviour[] allMbs = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MonoBehaviour mb in allMbs)
        {
            if (mb == null) continue;
            string tn = mb.GetType().Name;
            if (tn == "Ingredient" || tn == "PotionPickup" || tn == "HeartPickup" || tn == "Collectible" || tn == "KeyItem")
            {
                Level2CatPickupGuard guard = mb.GetComponent<Level2CatPickupGuard>();
                if (guard == null)
                {
                    guard = mb.gameObject.AddComponent<Level2CatPickupGuard>();
                }
                EditorUtility.SetDirty(mb.gameObject);
                attached++;
            }
        }

        SaveLevel2();
        Debug.Log($"[Level2CompleteFixer] Mega Bloque A aplicado: {attached} objetos recogibles protegidos con Level2CatPickupGuard.");
    }

    [MenuItem("Tools/Nivel 2/Mega Correccion/B. Animar Puestos del Mercado")]
    public static void ApplyMegaBlockB_MarketStallAnimations()
    {
        EnsureLevel2Open();

        Sprite lanternSprite = LoadSingleSprite("Assets/Art/Environment/Level2/Placeholder_Market_Lantern.png");
        Sprite herbSprite1 = LoadSingleSprite("Assets/Art/Environment/Level2/Placeholder_Ingredient_WitheredFlower.png");
        Sprite herbSprite2 = LoadSingleSprite("Assets/Art/Environment/Level2/Placeholder_Ingredient_Root.png");
        Sprite awningGold = LoadSingleSprite("Assets/Art/Environment/Level2/Placeholder_MarketStall_Awning_Gold.png");
        Sprite awningRed = LoadSingleSprite("Assets/Art/Environment/Level2/Placeholder_MarketStall_Awning_Red.png");

        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<GameObject> stalls = allObjects
            .Where(g => g != null && g.name.StartsWith("Stall_Z") && g.transform.parent != null && !g.transform.parent.name.StartsWith("Stall_Z"))
            .OrderBy(g => g.transform.position.x)
            .ToList();

        int idx = 0;
        foreach (GameObject stall in stalls)
        {
            // Crear o actualizar el hijo visual del toldo sin collider para que oscile con el viento
            Transform swayVisualTr = stall.transform.Find("Awning_Sway_Visual");
            GameObject swayVisualGO = swayVisualTr != null ? swayVisualTr.gameObject : new GameObject("Awning_Sway_Visual");
            swayVisualGO.transform.SetParent(stall.transform, false);
            swayVisualGO.transform.localPosition = new Vector3(0f, 2.25f, 0f);
            swayVisualGO.transform.localRotation = Quaternion.identity;
            swayVisualGO.transform.localScale = new Vector3(1.02f, 0.45f, 1f);

            SpriteRenderer awningSr = swayVisualGO.GetComponent<SpriteRenderer>();
            if (awningSr == null) awningSr = swayVisualGO.AddComponent<SpriteRenderer>();
            awningSr.sprite = (idx % 2 == 0) ? awningGold : awningRed;
            awningSr.sortingLayerName = "Gameplay";
            awningSr.sortingOrder = -1;
            awningSr.color = new Color(1f, 0.96f, 0.88f, 0.95f);

            // Crear farola colgante con luz calida y parpadeo
            Transform lanternTr = stall.transform.Find("Hanging_Lantern_Visual");
            GameObject lanternGO = lanternTr != null ? lanternTr.gameObject : new GameObject("Hanging_Lantern_Visual");
            lanternGO.transform.SetParent(stall.transform, false);
            lanternGO.transform.localPosition = new Vector3(-1.15f, 1.75f, 0f);
            lanternGO.transform.localRotation = Quaternion.identity;
            lanternGO.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

            if (lanternSprite != null)
            {
                SpriteRenderer lsr = lanternGO.GetComponent<SpriteRenderer>();
                if (lsr == null) lsr = lanternGO.AddComponent<SpriteRenderer>();
                lsr.sprite = lanternSprite;
                lsr.sortingLayerName = "Gameplay";
                lsr.sortingOrder = 0;
            }

            UnityEngine.Rendering.Universal.Light2D light2D = lanternGO.GetComponent<UnityEngine.Rendering.Universal.Light2D>();
            if (light2D == null) light2D = lanternGO.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            light2D.color = new Color(1.0f, 0.78f, 0.38f, 1f);
            light2D.intensity = 1.15f;
            light2D.pointLightOuterRadius = 3.2f;
            light2D.pointLightInnerRadius = 0.4f;

            LanternFlicker flicker = lanternGO.GetComponent<LanternFlicker>();
            if (flicker == null) flicker = lanternGO.AddComponent<LanternFlicker>();

            // Crear manojo de hierbas/mercancia colgante al otro lado del puesto
            Transform herbsTr = stall.transform.Find("Hanging_Wares_Visual");
            GameObject herbsGO = herbsTr != null ? herbsTr.gameObject : new GameObject("Hanging_Wares_Visual");
            herbsGO.transform.SetParent(stall.transform, false);
            herbsGO.transform.localPosition = new Vector3(1.15f, 1.78f, 0f);
            herbsGO.transform.localRotation = Quaternion.identity;
            herbsGO.transform.localScale = new Vector3(0.68f, 0.68f, 1f);

            Sprite wareSprite = (idx % 2 == 0) ? herbSprite1 : herbSprite2;
            if (wareSprite != null)
            {
                SpriteRenderer hsr = herbsGO.GetComponent<SpriteRenderer>();
                if (hsr == null) hsr = herbsGO.AddComponent<SpriteRenderer>();
                hsr.sprite = wareSprite;
                hsr.sortingLayerName = "Gameplay";
                hsr.sortingOrder = 0;
            }

            MarketStallAnimator anim = stall.GetComponent<MarketStallAnimator>();
            if (anim == null) anim = stall.AddComponent<MarketStallAnimator>();

            float speedMult = 0.90f + (idx % 5) * 0.05f;
            float phaseRad = (idx + 1) * 1.17f;
            anim.Configure(
                swayVisualGO.transform,
                new Transform[] { lanternGO.transform, herbsGO.transform },
                phaseRad,
                speedMult);

            EditorUtility.SetDirty(stall);
            idx++;
        }

        SaveLevel2();
        Debug.Log($"[Level2CompleteFixer] Mega Bloque B aplicado: {idx} puestos del mercado animados y desincronizados.");
    }

    [MenuItem("Tools/Nivel 2/Mega Correccion/C. Puertas Interiores con Indicador de Salida")]
    public static void ApplyMegaBlockC_InteriorExitDoorsAndIndicators()
    {
        EnsureLevel2Open();
        AlignAllDoorsToFloorSurfaces();

        Sprite openDoorSprite = LoadSingleSprite("Assets/Art/Environment/Level2/Village_Door_Open.png");
        Sprite closedDoorSprite = LoadSingleSprite("Assets/Art/Environment/Level2/Village_Door_Closed.png");

        Dictionary<string, string> doorLabels = new Dictionary<string, string>
        {
            { "Door_Int_House1_Ground",  "Salir a la calle" },
            { "Door_Int_House1_Balcony", "Salir al balcón (2º Piso)" },
            { "Door_Int_House2_Ground",  "Salir a la calle" },
            { "Door_Int_House2_Balcony", "Salir a la pasarela (2º Piso)" },
            { "Door_Int_House3_Ground",  "Salir a la calle" },
            { "Door_Int_House3_Roof",    "Salir al tejado" },
            { "Door_Int_House4_Ground",  "Salir a la calle" },
            { "Door_Int_House4_Balcony", "Salir a la galería (2º Piso)" },
            { "Door_Ext_House1_Ground",  "Entrar a la Botica" },
            { "Door_Ext_House1_Balcony", "Entrar al 2º Piso (Botica)" },
            { "Door_Ext_House2_Ground",  "Entrar a la Posada" },
            { "Door_Ext_House2_Balcony", "Entrar al 2º Piso (Posada)" },
            { "Door_Ext_House3_Ground",  "Entrar a la Casa Abandonada" },
            { "Door_Ext_House3_Roof",    "Entrar al Ático Secreto" },
            { "Door_Ext_House4_Ground",  "Entrar a la Mansión" },
            { "Door_Ext_House4_Balcony", "Entrar al 2º Piso (Mansión)" },
            { "Door_Ext_Z6_Sanctum",     "Abrir Puerta del Santuario" }
        };

        HouseDoor[] doors = Object.FindObjectsByType<HouseDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int configured = 0;
        foreach (HouseDoor door in doors)
        {
            if (door == null) continue;
            bool isInteriorExit = door.name.StartsWith("Door_Int_") || door.transform.position.y > 15f;

            SpriteRenderer sr = door.GetComponent<SpriteRenderer>();
            if (sr == null) sr = door.gameObject.AddComponent<SpriteRenderer>();
            if (isInteriorExit && openDoorSprite != null)
            {
                sr.sprite = openDoorSprite;
            }
            else if (!isInteriorExit && sr.sprite == null && closedDoorSprite != null)
            {
                sr.sprite = closedDoorSprite;
            }
            sr.sortingLayerName = "Gameplay";
            sr.sortingOrder = isInteriorExit ? 1 : -1;

            BoxCollider2D trig = door.GetComponent<BoxCollider2D>();
            if (trig == null) trig = door.gameObject.AddComponent<BoxCollider2D>();
            trig.isTrigger = true;
            if (trig.size.x < 1.2f || trig.size.y < 1.8f)
            {
                trig.size = new Vector2(Mathf.Max(trig.size.x, 1.35f), Mathf.Max(trig.size.y, 2.1f));
            }

            SerializedObject so = new SerializedObject(door);
            if (doorLabels.TryGetValue(door.name, out string label))
            {
                so.FindProperty("actionLabel").stringValue = label;
            }
            if (openDoorSprite != null && so.FindProperty("openSprite") != null) so.FindProperty("openSprite").objectReferenceValue = openDoorSprite;
            if (closedDoorSprite != null && so.FindProperty("closedSprite") != null) so.FindProperty("closedSprite").objectReferenceValue = closedDoorSprite;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (isInteriorExit)
            {
                Transform warmLightTr = door.transform.Find("ExitDoor_WarmLight");
                GameObject warmLightGO = warmLightTr != null ? warmLightTr.gameObject : new GameObject("ExitDoor_WarmLight");
                warmLightGO.transform.SetParent(door.transform, false);
                warmLightGO.transform.localPosition = new Vector3(0f, 1.0f, 0f);

                var l2d = warmLightGO.GetComponent<UnityEngine.Rendering.Universal.Light2D>();
                if (l2d == null) l2d = warmLightGO.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
                l2d.color = new Color(1.0f, 0.85f, 0.45f, 1f);
                l2d.intensity = 1.3f;
                l2d.pointLightOuterRadius = 2.8f;
                l2d.pointLightInnerRadius = 0.5f;
            }

            Level2DoorIndicator indicator = door.GetComponent<Level2DoorIndicator>();
            if (indicator == null) indicator = door.gameObject.AddComponent<Level2DoorIndicator>();
            indicator.Configure(isInteriorExit, 1.55f);

            EditorUtility.SetDirty(door.gameObject);
            configured++;
        }

        CellarExitBackupInteractable[] cellarExits = Object.FindObjectsByType<CellarExitBackupInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var ce in cellarExits)
        {
            if (ce == null) continue;
            Level2DoorIndicator ind = ce.GetComponent<Level2DoorIndicator>();
            if (ind == null) ind = ce.gameObject.AddComponent<Level2DoorIndicator>();
            ind.Configure(true, 1.4f);
            EditorUtility.SetDirty(ce.gameObject);
        }

        AlignAllDoorsToFloorSurfaces();
        SaveLevel2();
        Debug.Log($"[Level2CompleteFixer] Mega Bloque C aplicado: {configured} puertas configuradas con Level2DoorIndicator y luz guia.");
    }

    [MenuItem("Tools/Nivel 2/Mega Correccion/D. Configurar Tecla Q en Casas del Nivel 2")]
    public static void ApplyMegaBlockD_QKeyForDoors()
    {
        EnsureLevel2Open();
        GameObject witch = GameObject.Find("Witch");
        if (witch != null)
        {
            PlayerInteractor pi = witch.GetComponent<PlayerInteractor>();
            if (pi == null) pi = witch.AddComponent<PlayerInteractor>();
            SerializedObject so = new SerializedObject(pi);
            so.FindProperty("interactKey").intValue = (int)KeyCode.Q;
            so.FindProperty("interactionCooldown").floatValue = 0.35f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pi);
        }

        SaveLevel2();
        Debug.Log("[Level2CompleteFixer] Mega Bloque D aplicado: PlayerInteractor configurado con KeyCode.Q.");
    }

    public static void CaptureMegaBeforeEvidences()
    {
        EnsureLevel2Open();
        System.IO.Directory.CreateDirectory("Assets/Screenshots/NIVEL2/Evidencias");
        CaptureCameraAt(new Vector3(17.5f, 30.2f, -10f), 4.2f, "Assets/Screenshots/NIVEL2/Evidencias/BEFORE_Puerta_Interior_Casa1.png");
        CaptureCameraAt(new Vector3(44.5f, 2.5f, -10f), 4.5f, "Assets/Screenshots/NIVEL2/Evidencias/BEFORE_Puestos_Mercado_Z2.png");
        CaptureCameraAt(new Vector3(36.0f, 4.5f, -10f), 6.5f, "Assets/Screenshots/NIVEL2/Evidencias/BEFORE_Union_Fondo_Parallax.png");
    }

    public static void CaptureMegaAfterEvidences()
    {
        EnsureLevel2Open();
        System.IO.Directory.CreateDirectory("Assets/Screenshots/NIVEL2/Evidencias");
        CaptureCameraAt(new Vector3(15.5f, 3.2f, -10f), 4.2f, "Assets/Screenshots/NIVEL2/Evidencias/AFTER_Puerta_Exterior_IndicadorQ.png");
        CaptureCameraAt(new Vector3(14.5f, 30.2f, -10f), 4.2f, "Assets/Screenshots/NIVEL2/Evidencias/AFTER_Puerta_Interior_IndicadorQ.png");
        CaptureCameraAt(new Vector3(44.5f, 2.5f, -10f), 4.5f, "Assets/Screenshots/NIVEL2/Evidencias/AFTER_Puestos_Mercado_Animados.png");
        CaptureCameraAt(new Vector3(43.44f, 4.5f, -10f), 6.5f, "Assets/Screenshots/NIVEL2/Evidencias/AFTER_Union_Fondo_SinCostura_1.png");
        CaptureCameraAt(new Vector3(86.88f, 4.5f, -10f), 6.5f, "Assets/Screenshots/NIVEL2/Evidencias/AFTER_Union_Fondo_SinCostura_2.png");
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

    private static void EnsureSinglePanoramaTextureImport(string assetPath)
    {
        TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti == null) return;
        bool changed = false;
        if (ti.textureType != TextureImporterType.Sprite) { ti.textureType = TextureImporterType.Sprite; changed = true; }
        if (ti.spriteImportMode != SpriteImportMode.Single) { ti.spriteImportMode = SpriteImportMode.Single; changed = true; }
        if (Mathf.Abs(ti.spritePixelsPerUnit - 100f) > 0.01f) { ti.spritePixelsPerUnit = 100f; changed = true; }
        if (ti.maxTextureSize < 4096) { ti.maxTextureSize = 4096; changed = true; }
        if (ti.wrapMode != TextureWrapMode.Clamp) { ti.wrapMode = TextureWrapMode.Clamp; changed = true; }
        if (changed)
        {
            ti.SaveAndReimport();
        }
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

    // =========================================================================
    // BLOQUES DE CORRECCION: CONTROLES Q/1-2-3/E, COFRES, FONDO Y PUESTOS
    // =========================================================================

    [MenuItem("Tools/Nivel 2/Cofres y Controles/1. Esquema de Controles Q Interactuar / E Lanzar Pocion")]
    public static void ApplyTaskBlock1_Controls()
    {
        EnsureLevel2Open();

        GameObject witch = GameObject.Find("Witch");
        if (witch != null)
        {
            Level2Controls ctrl = witch.GetComponent<Level2Controls>();
            if (ctrl == null) ctrl = witch.AddComponent<Level2Controls>();
            Level2Controls.EnsureHUDControlsGuide();
            EditorUtility.SetDirty(ctrl);

            PlayerInteractor pi = witch.GetComponent<PlayerInteractor>();
            if (pi == null) pi = witch.AddComponent<PlayerInteractor>();
            SerializedObject soPi = new SerializedObject(pi);
            soPi.FindProperty("interactKey").intValue = (int)Level2Controls.InteractKey;
            soPi.FindProperty("interactionCooldown").floatValue = Level2Controls.DefaultInteractCooldown;
            soPi.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pi);
        }

        // Actualizar textos de tutoriales en Nivel 2 para reflejar Q (cofres/puertas) y 1/2/3 + E (pociones)
        TutorialTrigger[] triggers = Object.FindObjectsByType<TutorialTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TutorialTrigger tt in triggers)
        {
            if (tt == null) continue;
            SerializedObject soTt = new SerializedObject(tt);
            SerializedProperty msgProp = soTt.FindProperty("message");
            if (msgProp != null && !string.IsNullOrEmpty(msgProp.stringValue))
            {
                msgProp.stringValue = "Abre cofres y puertas con Q para reunir 2 ingredientes de cada tipo (Hongos, Arañas, Flores y Raíces). Elige poción con 1, 2 o 3 y lánzala con E.";
                soTt.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(tt);
            }
        }

        SaveLevel2();
        Debug.Log("[Level2CompleteFixer] Task Block 1 aplicado: Level2Controls activo, Panel_ControlsGuide creado y textos actualizados.");
    }

    [MenuItem("Tools/Nivel 2/Cofres y Controles/2. Crear Prefab Level2_Chest")]
    public static void ApplyTaskBlock2_ChestsSystemAndPrefab()
    {
        EnsureLevel2Open();
        System.IO.Directory.CreateDirectory("Assets/Prefabs/Level2");

        EnsureSingleSpriteImport("Assets/Art/Environment/Level2/Interactive_Chest_Closed.png", 64f);
        EnsureSingleSpriteImport("Assets/Art/Environment/Level2/Interactive_Chest_Open.png", 64f);

        Sprite closedSp = LoadSingleSprite("Assets/Art/Environment/Level2/Interactive_Chest_Closed.png");
        Sprite openSp = LoadSingleSprite("Assets/Art/Environment/Level2/Interactive_Chest_Open.png");

        GameObject prefabRoot = new GameObject("Level2_Chest");
        SpriteRenderer sr = prefabRoot.AddComponent<SpriteRenderer>();
        sr.sprite = closedSp;
        sr.sortingLayerName = "Gameplay";
        sr.sortingOrder = 2;

        BoxCollider2D col = prefabRoot.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.45f, 1.35f);
        col.offset = new Vector2(0f, 0.25f);

        Level2Chest chest = prefabRoot.AddComponent<Level2Chest>();
        chest.ConfigureChest(
            "chest_template",
            "Cofre del Mercado",
            closedSp,
            openSp,
            new List<ChestContentEntry>
            {
                new ChestContentEntry
                {
                    itemKind = ChestItemKind.Ingredient,
                    ingredientType = IngredientType.Mushroom,
                    ingredientUniqueId = "L2_Ing_Template",
                    amount = 1
                }
            });

        Level2DoorIndicator ind = prefabRoot.AddComponent<Level2DoorIndicator>();
        ind.Configure(false, 1.15f);

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, "Assets/Prefabs/Level2/Level2_Chest.prefab");
        Object.DestroyImmediate(prefabRoot);
        AssetDatabase.SaveAssets();
        Debug.Log("[Level2CompleteFixer] Task Block 2 aplicado: Prefab Assets/Prefabs/Level2/Level2_Chest.prefab creado.");
    }

    [MenuItem("Tools/Nivel 2/Cofres y Controles/3. Mover Ingredientes y Pociones Dentro de Cofres")]
    public static void ApplyTaskBlock3_PlaceItemsInChests()
    {
        EnsureLevel2Open();

        EnsureSingleSpriteImport("Assets/Art/Environment/Level2/Interactive_Chest_Closed.png", 64f);
        EnsureSingleSpriteImport("Assets/Art/Environment/Level2/Interactive_Chest_Open.png", 64f);
        Sprite closedSp = LoadSingleSprite("Assets/Art/Environment/Level2/Interactive_Chest_Closed.png");
        Sprite openSp = LoadSingleSprite("Assets/Art/Environment/Level2/Interactive_Chest_Open.png");

        // 1. Eliminar todos los Ingredient, PotionPickup y HeartPickup sueltos de Level2.unity
        Ingredient[] looseIngs = Object.FindObjectsByType<Ingredient>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var ing in looseIngs)
        {
            if (ing != null) Object.DestroyImmediate(ing.gameObject);
        }

        PotionPickup[] loosePots = Object.FindObjectsByType<PotionPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var pot in loosePots)
        {
            if (pot != null) Object.DestroyImmediate(pot.gameObject);
        }

        HeartPickup[] looseHearts = Object.FindObjectsByType<HeartPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var h in looseHearts)
        {
            if (h != null) Object.DestroyImmediate(h.gameObject);
        }

        // 2. Eliminar cualquier TreasureChest antiguo o Level2Chest previo para regenerar los 15 cofres limpios
        TreasureChest[] oldChests = Object.FindObjectsByType<TreasureChest>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var oc in oldChests)
        {
            if (oc != null) Object.DestroyImmediate(oc.gameObject);
        }

        Level2Chest[] prevChests = Object.FindObjectsByType<Level2Chest>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var pc in prevChests)
        {
            if (pc != null) Object.DestroyImmediate(pc.gameObject);
        }

        GameObject gameplay = GameObject.Find("Gameplay") ?? new GameObject("Gameplay");
        Transform chestsRootTr = gameplay.transform.Find("Chests");
        if (chestsRootTr != null) Object.DestroyImmediate(chestsRootTr.gameObject);
        GameObject chestsRoot = new GameObject("Chests");
        chestsRoot.transform.SetParent(gameplay.transform, false);

        Transform h1Root = GameObject.Find("Environment/Interiors/Interior_House1_Apothecary")?.transform ?? chestsRoot.transform;
        Transform h2Root = GameObject.Find("Environment/Interiors/Interior_House2_Tavern")?.transform ?? chestsRoot.transform;
        Transform h3Root = GameObject.Find("Environment/Interiors/Interior_House3_Abandoned")?.transform ?? chestsRoot.transform;
        Transform h4Root = GameObject.Find("Environment/Interiors/Interior_House4_Manor")?.transform ?? chestsRoot.transform;

        // 3. Instanciar y apoyar a ras de suelo (<= 0.02 u) los 15 cofres del Nivel 2
        // Cofre 1: Zona 1 - Calle de entrada
        SpawnGroundedChest(chestsRoot.transform, "Chest_Z1_Intro", "chest_z1_intro", "Cofre de la Entrada",
            18.5f, 1.0f, closedSp, openSp, new List<ChestContentEntry>
            {
                MakeIngredientEntry(IngredientType.Mushroom, "L2_Ing_Mushroom_Z1"),
                MakeHeartEntry(1)
            });

        // Cofre 2: Zona 1 - Sobre el toldo del primer puesto (Stall_Z1_1)
        SpawnGroundedChest(chestsRoot.transform, "Chest_Z1_StallRoof", "chest_z1_stall_roof", "Cofre del Toldo de Entrada",
            13.0f, 3.3f, closedSp, openSp, new List<ChestContentEntry>
            {
                MakePotionEntry(PotionType.Sleep, 1)
            });

        // Cofre 3: Interior Casa 1 (Botica, 2º piso)
        SpawnGroundedChest(h1Root, "Chest_Int_House1_Upper", "chest_int_house1_upper", "Cofre del Boticario",
            20.6f, 33.6f, closedSp, openSp, new List<ChestContentEntry>
            {
                MakeIngredientEntry(IngredientType.Mushroom, "L2_Ing_Mushroom_H1"),
                MakePotionEntry(PotionType.Blind, 1)
            });

        // Cofre 4: Zona 2 - Sobre el puesto Stall_Z2_2 (x = 38.8)
        SpawnGroundedChest(chestsRoot.transform, "Chest_Z2_MarketStreet", "chest_z2_market_street", "Cofre de los Puestos Bajos",
            38.8f, 3.3f, closedSp, openSp, new List<ChestContentEntry>
            {
                MakeIngredientEntry(IngredientType.Spider, "L2_Ing_Spider_Z2"),
                MakeIngredientEntry(IngredientType.WitheredFlower, "L2_Ing_Flower_Z2")
            });

        // Cofre 5: Zona 2 - Pasarela / Balcón de la Posada (RoofWalk_Z2)
        SpawnGroundedChest(chestsRoot.transform, "Chest_Ext_Z2_Balcony", "chest_ext_z2_balcony", "Cofre del Balcón de la Posada",
            46.4f, 4.8f, closedSp, openSp, new List<ChestContentEntry>
            {
                MakePotionEntry(PotionType.Blind, 1),
                MakeHeartEntry(1)
            });

        // Cofre 6: Interior Casa 2 (Posada, 2º piso - requiere llave de la bodega)
        SpawnGroundedChest(h2Root, "Chest_Int_House2_Locked", "chest_int_house2_locked", "Cofre del Posadero",
            52.4f, 33.6f, closedSp, openSp, new List<ChestContentEntry>
            {
                MakePotionEntry(PotionType.Sleep, 1),
                MakeHeartEntry(1)
            },
            needsKey: true, keyId: "key_tavern_chest", keyName: "Llave de Hierro de la Posada");

        // Cofre 7: Zona 3 - Entrada de la Plaza Central
        SpawnGroundedChest(chestsRoot.transform, "Chest_Z3_PlazaStreet", "chest_z3_plaza_street", "Cofre de la Plaza Central",
            67.5f, 1.0f, closedSp, openSp, new List<ChestContentEntry>
            {
                MakeIngredientEntry(IngredientType.Spider, "L2_Ing_Spider_Z3"),
                MakePotionEntry(PotionType.Blind, 1)
            });

        // Cofre 8: Zona 3 - Mirador izquierdo sobre el canal (Roof_Z3_LeftHigh)
        SpawnGroundedChest(chestsRoot.transform, "Chest_Ext_Z3_CanalOverlook", "chest_ext_z3_canal_overlook", "Cofre del Mirador del Canal",
            71.2f, 5.0f, closedSp, openSp, new List<ChestContentEntry>
            {
                MakePotionEntry(PotionType.Sleep, 1)
            });

        // Cofre 9: Zona 3 - Mirador derecho tras el canal (Roof_Z3_RightHigh)
        SpawnGroundedChest(chestsRoot.transform, "Chest_Z3_EastOverlook", "chest_z3_east_overlook", "Cofre del Mirador Oriental",
            87.0f, 5.2f, closedSp, openSp, new List<ChestContentEntry>
            {
                MakeIngredientEntry(IngredientType.Root, "L2_Ing_Root_Z3"),
                MakeHeartEntry(1)
            });

        // Cofre 10: Zona 4 - Entrada del Callejón (en la calle antes del puesto elevado)
        SpawnGroundedChest(chestsRoot.transform, "Chest_Z4_AlleyEntry", "chest_z4_alley_entry", "Cofre del Callejón",
            97.5f, 0.8f, closedSp, openSp, new List<ChestContentEntry>
            {
                MakeIngredientEntry(IngredientType.WitheredFlower, "L2_Ing_Flower_Z4"),
                MakePotionEntry(PotionType.Sleep, 1)
            });

        // Cofre 11: Zona 4 - Salida del Pasaje Felino (requiere cruzar como gato y volver a forma humana)
        SpawnGroundedChest(chestsRoot.transform, "Chest_Z4_CatTunnelExit", "chest_z4_cat_tunnel_exit", "Cofre del Pasaje Felino",
            115.2f, 0.8f, closedSp, openSp, new List<ChestContentEntry>
            {
                MakeIngredientEntry(IngredientType.Root, "L2_Ing_Root_Z4")
            });

        // Cofre 12: Interior Casa 3 (Casa Abandonada, Ático Secreto 3er piso)
        SpawnGroundedChest(h3Root, "Chest_Int_House3_Attic", "chest_int_house3_attic", "Cofre Oculto del Ático",
            112.2f, 37.6f, closedSp, openSp, new List<ChestContentEntry>
            {
                MakeIngredientEntry(IngredientType.Root, "L2_Ing_Root_H3"),
                MakePotionEntry(PotionType.Hypnotize, 1)
            });

        // Cofre 13: Zona 5 - Calle del Gran Mercado
        SpawnGroundedChest(chestsRoot.transform, "Chest_Z5_MarketPlaza", "chest_z5_market_plaza", "Cofre del Gran Mercado",
            133.2f, 1.0f, closedSp, openSp, new List<ChestContentEntry>
            {
                MakeIngredientEntry(IngredientType.Mushroom, "L2_Ing_Mushroom_Z5"),
                MakePotionEntry(PotionType.Hypnotize, 1),
                MakeHeartEntry(1)
            });

        // Cofre 14: Zona 5 - Pasarela alta del Gran Mercado (Roof_Z5_HighWalk)
        SpawnGroundedChest(chestsRoot.transform, "Chest_Z5_HighTower", "chest_z5_high_tower", "Cofre de la Torre del Mercado",
            142.5f, 7.2f, closedSp, openSp, new List<ChestContentEntry>
            {
                MakeIngredientEntry(IngredientType.Mushroom, "L2_Ing_Mushroom_Z5_Bonus"),
                MakeIngredientEntry(IngredientType.Spider, "L2_Ing_Spider_Z5_Bonus")
            });

        // Cofre 15: Interior Casa 4 (Mansión del Magistrado, Altillo de Biblioteca - requiere Llave Maestra)
        SpawnGroundedChest(h4Root, "Chest_Int_House4_Royal", "chest_int_house4_royal", "Cofre Real del Magistrado",
            142.5f, 35.4f, closedSp, openSp, new List<ChestContentEntry>
            {
                MakePotionEntry(PotionType.Hypnotize, 1),
                MakeHeartEntry(1)
            },
            needsKey: true, keyId: "key_manor_master", keyName: "Llave Maestra del Magistrado");

        SaveLevel2();
        Debug.Log("[Level2CompleteFixer] Task Block 3 aplicado: 0 objetos sueltos y 15 cofres Level2Chest colocados a ras de suelo.");
    }

    private static ChestContentEntry MakeIngredientEntry(IngredientType type, string uniqueId)
    {
        return new ChestContentEntry
        {
            itemKind = ChestItemKind.Ingredient,
            ingredientType = type,
            ingredientUniqueId = uniqueId,
            amount = 1,
            claimed = false
        };
    }

    private static ChestContentEntry MakePotionEntry(PotionType type, int count)
    {
        return new ChestContentEntry
        {
            itemKind = ChestItemKind.Potion,
            potionType = type,
            amount = count,
            claimed = false
        };
    }

    private static ChestContentEntry MakeHeartEntry(int count)
    {
        return new ChestContentEntry
        {
            itemKind = ChestItemKind.Heart,
            amount = count,
            claimed = false
        };
    }

    private static Level2Chest SpawnGroundedChest(
        Transform parent,
        string objectName,
        string chestId,
        string title,
        float x,
        float rayStartY,
        Sprite closedSp,
        Sprite openSp,
        List<ChestContentEntry> entries,
        bool needsKey = false,
        string keyId = "",
        string keyName = "")
    {
        Physics2D.SyncTransforms();
        float surfaceY = FindBestSurfaceBelow(x, rayStartY);

        GameObject go = new GameObject(objectName);
        go.transform.SetParent(parent, true);
        go.transform.localScale = Vector3.one;
        go.transform.rotation = Quaternion.identity;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = closedSp;
        sr.sortingLayerName = "Gameplay";
        sr.sortingOrder = 2;

        // Colocar el borde inferior del sprite exactamente sobre surfaceY (gap = 0.000 u <= 0.02 u)
        go.transform.position = new Vector3(x, surfaceY + 0.5f, 0f);
        SnapSpriteBottomToY(go, surfaceY);

        BoxCollider2D trig = go.AddComponent<BoxCollider2D>();
        trig.isTrigger = true;
        trig.size = new Vector2(1.45f, 1.35f);
        trig.offset = new Vector2(0f, 0.25f);

        Level2Chest chest = go.AddComponent<Level2Chest>();
        chest.ConfigureChest(chestId, title, closedSp, openSp, entries, needsKey, keyId, keyName);

        Level2DoorIndicator ind = go.AddComponent<Level2DoorIndicator>();
        ind.Configure(false, 1.05f);

        Level2CatPickupGuard guard = go.AddComponent<Level2CatPickupGuard>();
        EditorUtility.SetDirty(go);
        return chest;
    }

    public static float FindBestSurfaceBelow(float x, float rayStartY)
    {
        int groundMask = 1 << LayerMask.NameToLayer("Ground");
        RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(x, rayStartY), Vector2.down, 4.5f, groundMask);
        float bestY = GROUND_SURFACE_Y;
        float bestDist = float.MaxValue;
        foreach (var h in hits)
        {
            if (h.collider != null && !h.collider.isTrigger)
            {
                float d = rayStartY - h.point.y;
                if (d >= -0.05f && d < bestDist)
                {
                    bestDist = d;
                    bestY = h.point.y;
                }
            }
        }
        return bestY;
    }

    [MenuItem("Tools/Nivel 2/Cofres y Controles/4. Recomponer Fondo Continuo de Aldea")]
    public static void ApplyTaskBlock4_RecomposedBackground()
    {
        Level2BackgroundProcessor.ImportAndProcessSourceImage();
        Level2BackgroundProcessor.ApplyBackgroundToLevel2();
    }

    private static void BuildContinuousLayer(
        Transform parent,
        string layerName,
        Sprite spriteA,
        Sprite spriteB,
        Vector2 factor,
        int sortingOrder,
        float posY,
        float uniformScale,
        Color tint)
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
        pl.Setup(factor.x, false, true);

        float tileWorldWidth = (spriteA.rect.width / spriteA.pixelsPerUnit) * uniformScale;

        for (int i = -3; i <= 10; i++)
        {
            GameObject copy = new GameObject($"Copy_{i + 3}");
            copy.transform.SetParent(layerGO.transform, false);
            copy.transform.localPosition = new Vector3(i * tileWorldWidth, 0f, 0f);
            copy.transform.localRotation = Quaternion.identity;
            copy.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);

            SpriteRenderer sr = copy.AddComponent<SpriteRenderer>();
            sr.sprite = (Mathf.Abs(i) % 2 == 0) ? spriteA : spriteB;
            sr.drawMode = SpriteDrawMode.Simple;
            sr.color = tint;
            sr.sortingLayerName = "Background";
            sr.sortingOrder = sortingOrder;
        }

        EditorUtility.SetDirty(layerGO);
    }

    private static void EnsureSeamlessBackgroundImport(string assetPath)
    {
        TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti == null) return;
        bool changed = false;
        if (ti.textureType != TextureImporterType.Sprite) { ti.textureType = TextureImporterType.Sprite; changed = true; }
        if (ti.spriteImportMode != SpriteImportMode.Single) { ti.spriteImportMode = SpriteImportMode.Single; changed = true; }
        if (Mathf.Abs(ti.spritePixelsPerUnit - 100f) > 0.01f) { ti.spritePixelsPerUnit = 100f; changed = true; }
        if (ti.maxTextureSize < 4096) { ti.maxTextureSize = 4096; changed = true; }
        if (ti.filterMode != FilterMode.Bilinear) { ti.filterMode = FilterMode.Bilinear; changed = true; }
        if (ti.wrapMode != TextureWrapMode.Repeat) { ti.wrapMode = TextureWrapMode.Repeat; changed = true; }

        TextureImporterSettings settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        if (settings.spriteMeshType != SpriteMeshType.FullRect)
        {
            settings.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(settings);
            changed = true;
        }

        if (changed)
        {
            ti.SaveAndReimport();
        }
    }

    private static void EnsureSingleSpriteImport(string assetPath, float ppu)
    {
        TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti == null) return;
        bool changed = false;
        if (ti.textureType != TextureImporterType.Sprite) { ti.textureType = TextureImporterType.Sprite; changed = true; }
        if (ti.spriteImportMode != SpriteImportMode.Single) { ti.spriteImportMode = SpriteImportMode.Single; changed = true; }
        if (Mathf.Abs(ti.spritePixelsPerUnit - ppu) > 0.01f) { ti.spritePixelsPerUnit = ppu; changed = true; }
        if (ti.filterMode != FilterMode.Bilinear) { ti.filterMode = FilterMode.Bilinear; changed = true; }

        TextureImporterSettings settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        if (settings.spriteMeshType != SpriteMeshType.FullRect)
        {
            settings.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(settings);
            changed = true;
        }

        if (changed)
        {
            ti.SaveAndReimport();
        }
    }

    [MenuItem("Tools/Nivel 2/Cofres y Controles/5. Completar Puestos Elevados con Toldo Completo")]
    public static void ApplyTaskBlock5_CompleteElevatedStalls()
    {
        EnsureLevel2Open();

        string[] awningPaths =
        {
            "Assets/Art/Environment/Level2/Placeholder_MarketStall_Awning_Red.png",
            "Assets/Art/Environment/Level2/Placeholder_MarketStall_Awning_Gold.png",
            "Assets/Art/Environment/Level2/Placeholder_MarketStall_Awning_Orange.png",
            "Assets/Art/Environment/Level2/Placeholder_MarketStall_Awning_Olive.png",
            "Assets/Art/Environment/Level2/Placeholder_MarketStall_Awning_Large.png",
            "Assets/Art/Environment/Level2/Placeholder_MarketStall_WoodPosts.png"
        };

        foreach (string p in awningPaths)
        {
            EnsureSingleSpriteImport(p, 64f);
        }

        Sprite[] awningVariants =
        {
            LoadSingleSprite("Assets/Art/Environment/Level2/Placeholder_MarketStall_Awning_Red.png"),
            LoadSingleSprite("Assets/Art/Environment/Level2/Placeholder_MarketStall_Awning_Gold.png"),
            LoadSingleSprite("Assets/Art/Environment/Level2/Placeholder_MarketStall_Awning_Orange.png"),
            LoadSingleSprite("Assets/Art/Environment/Level2/Placeholder_MarketStall_Awning_Olive.png")
        };
        Sprite awningLarge = LoadSingleSprite("Assets/Art/Environment/Level2/Placeholder_MarketStall_Awning_Large.png");
        Sprite woodPostsSprite = LoadSingleSprite("Assets/Art/Environment/Level2/Placeholder_MarketStall_WoodPosts.png");
        Sprite lanternSprite = LoadSingleSprite("Assets/Art/Environment/Level2/Placeholder_Market_Lantern.png");
        Sprite crateSprite = LoadSingleSprite("Assets/Art/Environment/Level2/Placeholder_Market_Crate.png");
        Sprite barrelSprite = LoadSingleSprite("Assets/Art/Environment/Level2/Placeholder_Market_Barrel.png");
        Sprite herbSprite1 = LoadSingleSprite("Assets/Art/Environment/Level2/Placeholder_Ingredient_WitheredFlower.png");
        Sprite herbSprite2 = LoadSingleSprite("Assets/Art/Environment/Level2/Placeholder_Ingredient_Root.png");

        // 1. Completar todos los puestos Stall_Z* (incluyendo Stall_Z4_Entry elevado en x=101, y=2.5)
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<GameObject> stalls = allObjects
            .Where(g => g != null && g.name.StartsWith("Stall_Z") && g.transform.parent != null && !g.transform.parent.name.StartsWith("Stall_Z"))
            .OrderBy(g => g.transform.position.x)
            .ToList();

        int idx = 0;
        foreach (GameObject stall in stalls)
        {
            Sprite chosenAwning = awningVariants[idx % awningVariants.Length];

            // Asegurar que PostsAndCounter apoye a ras de suelo o tenga columnas hasta el suelo si es elevado
            Transform postsTr = stall.transform.Find("PostsAndCounter");
            if (postsTr != null)
            {
                SpriteRenderer psr = postsTr.GetComponent<SpriteRenderer>();
                if (psr != null && woodPostsSprite != null)
                {
                    psr.sprite = woodPostsSprite;
                    psr.sortingLayerName = "Gameplay";
                    psr.sortingOrder = 1;
                }

                float postsBottomY = psr != null ? psr.bounds.min.y : stall.transform.position.y - 1.0f;
                float groundY = RaycastGroundY(stall.transform.position.x, GROUND_SURFACE_Y);
                if (postsBottomY - groundY > 0.02f)
                {
                    // Si es un puesto de calle normal (y < 1.6), bajarlo exactamente a ras de suelo;
                    // si es un puesto elevado sobre callejón (Stall_Z4_Entry en y=2.5), crear pilares de soporte hasta groundY
                    if (stall.transform.position.y < 1.8f)
                    {
                        stall.transform.position -= new Vector3(0f, postsBottomY - groundY, 0f);
                    }
                    else
                    {
                        EnsureSupportColumnsToGround(stall.transform, groundY, postsBottomY + 0.15f, 2.4f, woodPostsSprite);
                    }
                }
            }

            // Configurar AwningRoof (colisionador físico de techo) y Awning_Sway_Visual (toldo completo que se mece)
            Transform awningRoofTr = stall.transform.Find("AwningRoof");
            if (awningRoofTr != null)
            {
                awningRoofTr.localPosition = new Vector3(0f, 1.25f, 0f);
                SpriteRenderer roofSr = awningRoofTr.GetComponent<SpriteRenderer>();
                if (roofSr != null)
                {
                    roofSr.sprite = chosenAwning;
                    roofSr.enabled = false; // El visual completo lo dibuja Awning_Sway_Visual en la misma posición
                }
            }

            Transform swayTr = stall.transform.Find("Awning_Sway_Visual");
            GameObject swayGO = swayTr != null ? swayTr.gameObject : new GameObject("Awning_Sway_Visual");
            swayGO.transform.SetParent(stall.transform, false);
            swayGO.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            swayGO.transform.localRotation = Quaternion.identity;
            swayGO.transform.localScale = Vector3.one;

            SpriteRenderer swaySr = swayGO.GetComponent<SpriteRenderer>();
            if (swaySr == null) swaySr = swayGO.AddComponent<SpriteRenderer>();
            swaySr.sprite = chosenAwning;
            swaySr.sortingLayerName = "Gameplay";
            swaySr.sortingOrder = 2;
            swaySr.color = Color.white;
            swaySr.enabled = true;

            // Mercancía sobre el mostrador (cajas/barriles/hierbas)
            Transform counterWaresTr = stall.transform.Find("Counter_Wares_Visual");
            GameObject counterWaresGO = counterWaresTr != null ? counterWaresTr.gameObject : new GameObject("Counter_Wares_Visual");
            counterWaresGO.transform.SetParent(stall.transform, false);
            counterWaresGO.transform.localPosition = new Vector3((idx % 2 == 0) ? -0.55f : 0.55f, 0.52f, 0f);
            counterWaresGO.transform.localRotation = Quaternion.identity;
            counterWaresGO.transform.localScale = new Vector3(0.48f, 0.48f, 1f);
            SpriteRenderer cwSr = counterWaresGO.GetComponent<SpriteRenderer>();
            if (cwSr == null) cwSr = counterWaresGO.AddComponent<SpriteRenderer>();
            cwSr.sprite = (idx % 2 == 0) ? crateSprite : barrelSprite;
            cwSr.sortingLayerName = "Gameplay";
            cwSr.sortingOrder = 2;

            // Farola colgante y hierbas
            Transform lanternTr = stall.transform.Find("Hanging_Lantern_Visual");
            GameObject lanternGO = lanternTr != null ? lanternTr.gameObject : new GameObject("Hanging_Lantern_Visual");
            lanternGO.transform.SetParent(stall.transform, false);
            lanternGO.transform.localPosition = new Vector3(-1.15f, 0.82f, 0f);
            lanternGO.transform.localScale = new Vector3(0.72f, 0.72f, 1f);
            if (lanternSprite != null)
            {
                SpriteRenderer lsr = lanternGO.GetComponent<SpriteRenderer>();
                if (lsr == null) lsr = lanternGO.AddComponent<SpriteRenderer>();
                lsr.sprite = lanternSprite;
                lsr.sortingLayerName = "Gameplay";
                lsr.sortingOrder = 2;
            }
            var l2d = lanternGO.GetComponent<UnityEngine.Rendering.Universal.Light2D>();
            if (l2d == null) l2d = lanternGO.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            l2d.color = new Color(1.0f, 0.78f, 0.38f, 1f);
            l2d.intensity = 1.15f;
            l2d.pointLightOuterRadius = 3.2f;
            if (lanternGO.GetComponent<LanternFlicker>() == null) lanternGO.AddComponent<LanternFlicker>();

            Transform herbsTr = stall.transform.Find("Hanging_Wares_Visual");
            GameObject herbsGO = herbsTr != null ? herbsTr.gameObject : new GameObject("Hanging_Wares_Visual");
            herbsGO.transform.SetParent(stall.transform, false);
            herbsGO.transform.localPosition = new Vector3(1.15f, 0.85f, 0f);
            herbsGO.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
            Sprite wareSp = (idx % 2 == 0) ? herbSprite1 : herbSprite2;
            if (wareSp != null)
            {
                SpriteRenderer hsr = herbsGO.GetComponent<SpriteRenderer>();
                if (hsr == null) hsr = herbsGO.AddComponent<SpriteRenderer>();
                hsr.sprite = wareSp;
                hsr.sortingLayerName = "Gameplay";
                hsr.sortingOrder = 2;
            }

            MarketStallAnimator anim = stall.GetComponent<MarketStallAnimator>();
            if (anim == null) anim = stall.AddComponent<MarketStallAnimator>();
            anim.Configure(
                swayGO.transform,
                new Transform[] { lanternGO.transform, herbsGO.transform },
                (idx + 1) * 1.23f,
                0.90f + (idx % 5) * 0.05f);

            EditorUtility.SetDirty(stall);
            idx++;
        }

        // 2. Completar las plataformas de puestos elevados sobre la calle (Roof_Z3_LeftHigh, Roof_Z3_RightHigh,
        //    Awning_CanalOverhead, Roof_Z5_Low_1, Roof_Z5_Mid_2, Roof_Z5_HighWalk) con su sombrilla/toldo completo y postes!
        string[] elevatedPaths =
        {
            "Environment/Zone_3_PlazaCentral/Roof_Z3_LeftHigh",
            "Environment/Zone_3_PlazaCentral/Awning_CanalOverhead",
            "Environment/Zone_3_PlazaCentral/Roof_Z3_RightHigh",
            "Environment/Zone_5_GranMercado/Roof_Z5_Low_1",
            "Environment/Zone_5_GranMercado/Roof_Z5_Mid_2",
            "Environment/Zone_5_GranMercado/Roof_Z5_HighWalk"
        };

        for (int eIdx = 0; eIdx < elevatedPaths.Length; eIdx++)
        {
            GameObject elev = GameObject.Find(elevatedPaths[eIdx]);
            if (elev == null) continue;

            Sprite chosenAwning = (eIdx % 3 == 0) ? awningLarge : awningVariants[(eIdx + 1) % awningVariants.Length];
            SpriteRenderer baseSr = elev.GetComponent<SpriteRenderer>();
            if (elev.name == "Awning_CanalOverhead" && baseSr != null && awningLarge != null)
            {
                baseSr.sprite = awningLarge;
                baseSr.sortingLayerName = "Gameplay";
                baseSr.sortingOrder = 2;
            }

            // Columnas de madera hasta el suelo/plataforma inferior para que nunca se vea como mesa flotante
            float bottomY = baseSr != null ? baseSr.bounds.min.y : elev.transform.position.y - 0.2f;
            float supportFloorY = FindBestSurfaceBelow(elev.transform.position.x, bottomY - 0.1f);
            EnsureSupportColumnsToGround(elev.transform, supportFloorY, bottomY + 0.08f, 3.2f, woodPostsSprite);

            // Si es una plataforma Roof_Z*, añadir sus postes superiores y su toldo/sombrilla completo encima
            if (elev.name.StartsWith("Roof_Z"))
            {
                Transform canopyPostsTr = elev.transform.Find("Elevated_CanopyPosts");
                GameObject canopyPostsGO = canopyPostsTr != null ? canopyPostsTr.gameObject : new GameObject("Elevated_CanopyPosts");
                canopyPostsGO.transform.SetParent(elev.transform, false);
                canopyPostsGO.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                canopyPostsGO.transform.localScale = new Vector3(1.35f, 0.95f, 1f);
                SpriteRenderer cpsr = canopyPostsGO.GetComponent<SpriteRenderer>();
                if (cpsr == null) cpsr = canopyPostsGO.AddComponent<SpriteRenderer>();
                cpsr.sprite = woodPostsSprite;
                cpsr.sortingLayerName = "Gameplay";
                cpsr.sortingOrder = -1;

                Transform swayTr = elev.transform.Find("Awning_Sway_Visual");
                GameObject swayGO = swayTr != null ? swayTr.gameObject : new GameObject("Awning_Sway_Visual");
                swayGO.transform.SetParent(elev.transform, false);
                swayGO.transform.localPosition = new Vector3(0f, 2.05f, 0f);
                swayGO.transform.localRotation = Quaternion.identity;
                swayGO.transform.localScale = new Vector3(1.15f, 1.0f, 1f);

                SpriteRenderer swaySr = swayGO.GetComponent<SpriteRenderer>();
                if (swaySr == null) swaySr = swayGO.AddComponent<SpriteRenderer>();
                swaySr.sprite = chosenAwning;
                swaySr.sortingLayerName = "Gameplay";
                swaySr.sortingOrder = 2;

                Transform lanternTr = elev.transform.Find("Hanging_Lantern_Visual");
                GameObject lanternGO = lanternTr != null ? lanternTr.gameObject : new GameObject("Hanging_Lantern_Visual");
                lanternGO.transform.SetParent(elev.transform, false);
                lanternGO.transform.localPosition = new Vector3(-1.45f, 1.55f, 0f);
                lanternGO.transform.localScale = new Vector3(0.72f, 0.72f, 1f);
                if (lanternSprite != null)
                {
                    SpriteRenderer lsr = lanternGO.GetComponent<SpriteRenderer>();
                    if (lsr == null) lsr = lanternGO.AddComponent<SpriteRenderer>();
                    lsr.sprite = lanternSprite;
                    lsr.sortingLayerName = "Gameplay";
                    lsr.sortingOrder = 2;
                }

                MarketStallAnimator anim = elev.GetComponent<MarketStallAnimator>();
                if (anim == null) anim = elev.AddComponent<MarketStallAnimator>();
                anim.Configure(
                    swayGO.transform,
                    new Transform[] { lanternGO.transform },
                    (eIdx + 5) * 1.37f,
                    0.92f + (eIdx % 4) * 0.05f);
            }

            EditorUtility.SetDirty(elev);
        }

        // Guardar prefabs propios de puestos completos en Assets/Prefabs/Level2/
        if (stalls.Count > 0)
        {
            PrefabUtility.SaveAsPrefabAsset(stalls[0], "Assets/Prefabs/Level2/Market_Stall_Complete.prefab");
        }
        GameObject sampleElev = GameObject.Find("Environment/Zone_3_PlazaCentral/Roof_Z3_LeftHigh");
        if (sampleElev != null)
        {
            PrefabUtility.SaveAsPrefabAsset(sampleElev, "Assets/Prefabs/Level2/Market_Stall_Elevated.prefab");
        }

        // Volver a alinear todos los cofres por si algún puesto ajustó su altura en milímetros
        Level2Chest[] allChests = Object.FindObjectsByType<Level2Chest>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Level2Chest c in allChests)
        {
            if (c == null) continue;
            SpriteRenderer csr = c.GetComponent<SpriteRenderer>();
            float curMinY = csr != null && csr.sprite != null ? csr.bounds.min.y : c.transform.position.y;
            float surfY = FindBestSurfaceBelow(c.transform.position.x, curMinY + 0.25f);
            SnapSpriteBottomToY(c.gameObject, surfY);
            EditorUtility.SetDirty(c.gameObject);
        }

        SaveLevel2();
        AssetDatabase.SaveAssets();
        Debug.Log("[Level2CompleteFixer] Task Block 5 aplicado: Puestos bajos y elevados completados con toldo/sombrilla y columnas apoyadas.");
    }

    private static void EnsureSupportColumnsToGround(Transform parent, float bottomWorldY, float topWorldY, float spanWidth, Sprite woodPostsSprite)
    {
        float height = Mathf.Max(0.5f, topWorldY - bottomWorldY);
        Transform suppTr = parent.Find("SupportColumns_Base");
        GameObject suppGO = suppTr != null ? suppTr.gameObject : new GameObject("SupportColumns_Base");
        suppGO.transform.SetParent(parent, true);
        suppGO.transform.position = new Vector3(parent.position.x, bottomWorldY + height * 0.5f, 0f);
        suppGO.transform.rotation = Quaternion.identity;

        SpriteRenderer sr = suppGO.GetComponent<SpriteRenderer>();
        if (sr == null) sr = suppGO.AddComponent<SpriteRenderer>();
        sr.sprite = woodPostsSprite;
        sr.sortingLayerName = "Gameplay";
        sr.sortingOrder = -1;
        sr.color = new Color(0.82f, 0.72f, 0.62f, 1f);

        if (woodPostsSprite != null)
        {
            float baseW = woodPostsSprite.rect.width / woodPostsSprite.pixelsPerUnit;
            float baseH = woodPostsSprite.rect.height / woodPostsSprite.pixelsPerUnit;
            suppGO.transform.localScale = new Vector3(spanWidth / Mathf.Max(0.1f, baseW), height / Mathf.Max(0.1f, baseH), 1f);
            SnapSpriteBottomToY(suppGO, bottomWorldY);
        }
    }

    public static void CaptureTaskBeforeEvidences()
    {
        EnsureLevel2Open();
        System.IO.Directory.CreateDirectory("Assets/Screenshots/NIVEL2/Evidencias");
        CaptureCameraAt(new Vector3(16.0f, 3.2f, -10f), 5.0f, "Assets/Screenshots/NIVEL2/Evidencias/BEFORE_Objetos_Sueltos_Z1.png");
        CaptureCameraAt(new Vector3(42.0f, 3.8f, -10f), 5.8f, "Assets/Screenshots/NIVEL2/Evidencias/BEFORE_Puestos_Sin_Toldo_Z2.png");
        CaptureCameraAt(new Vector3(78.0f, 4.2f, -10f), 6.2f, "Assets/Screenshots/NIVEL2/Evidencias/BEFORE_Puestos_Elevados_Z3.png");
        CaptureCameraAt(new Vector3(60.0f, 4.8f, -10f), 6.8f, "Assets/Screenshots/NIVEL2/Evidencias/BEFORE_Fondo_Fragmentado.png");
    }

    public static void CaptureTaskAfterEvidences()
    {
        EnsureLevel2Open();
        System.IO.Directory.CreateDirectory("Assets/Screenshots/NIVEL2/Evidencias");
        CaptureCameraAt(new Vector3(16.0f, 3.2f, -10f), 5.0f, "Assets/Screenshots/NIVEL2/Evidencias/AFTER_Cofres_Y_Guia_Controles_Z1.png");
        CaptureCameraAt(new Vector3(42.0f, 3.8f, -10f), 5.8f, "Assets/Screenshots/NIVEL2/Evidencias/AFTER_Puestos_Con_Toldo_Completo_Z2.png");
        CaptureCameraAt(new Vector3(78.0f, 4.2f, -10f), 6.2f, "Assets/Screenshots/NIVEL2/Evidencias/AFTER_Puestos_Elevados_Completos_Z3.png");
        CaptureCameraAt(new Vector3(60.0f, 4.8f, -10f), 6.8f, "Assets/Screenshots/NIVEL2/Evidencias/AFTER_Fondo_Aldea_Continuo_1.png");
        CaptureCameraAt(new Vector3(138.0f, 4.8f, -10f), 6.8f, "Assets/Screenshots/NIVEL2/Evidencias/AFTER_Fondo_Aldea_Continuo_2.png");
    }
}
#endif
