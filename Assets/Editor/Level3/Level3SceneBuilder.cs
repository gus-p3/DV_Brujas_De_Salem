using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Level2;
using Level3;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Level3.Editor
{
    /// <summary>
    /// Generador automatizado e idempotente de la escena Assets/Scenes/Level3.unity.
    /// Construye la infraestructura base (Cámara, Luces 2D, Managers, HUD, Level3Bootstrap,
    /// Level3StateController, KeyInventory, InteractionPromptUI) y el Acto 1 del Nivel 3:
    /// El Sendero del Bosque hacia la Cabaña de la Bruja (X: 0 a X: 115).
    /// </summary>
    public static class Level3SceneBuilder
    {
        public const string Level3Path = "Assets/Scenes/Level3.unity";
        public const string Level1Path = "Assets/Scenes/Level1.unity";
        public const string Level2Path = "Assets/Scenes/Level2.unity";

        private const string WitchPrefabPath = "Assets/Prefabs/Witch.prefab";
        private const string GuardPrefabPath = "Assets/Prefabs/Guard.prefab";
        private const string VillagerPrefabPath = "Assets/Prefabs/Villager.prefab";
        private const string VillagerFPrefabPath = "Assets/Prefabs/VillagerF.prefab";
        private const string CheckpointPrefabPath = "Assets/Prefabs/Checkpoint.prefab";

        private const string PotionBlindPath = "Assets/Prefabs/PotionPickup_Blind.prefab";
        private const string PotionHypnotizePath = "Assets/Prefabs/PotionPickup_Hypnotize.prefab";
        private const string PotionSleepPath = "Assets/Prefabs/PotionPickup_Sleep.prefab";
        private const string HeartPickupPath = "Assets/Prefabs/HeartPickup.prefab";

        private const string EnvPath = "Assets/Art/Environment/";
        private const string GroundSpritePath = EnvPath + "Env_Ground_Tile.png";
        private const string CainosAtlasPath = "Assets/Cainos/Pixel Art Platformer - Village Props/Texture/TX Village Props.png";

        [MenuItem("Tools/Level 3/Build Complete Level 3 Scene")]
        public static void BuildCompleteLevel3Scene()
        {
            var log = new StringBuilder();
            log.AppendLine("=== CONSTRUCCIÓN AUTOMATIZADA DE LEVEL 3 (ACTO 1: EL SENDERO DEL BOSQUE) ===");

            ProjectSetup.Configure();

            Scene l3 = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Scene l1 = EditorSceneManager.OpenScene(Level1Path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(l3);

            var roots = new Dictionary<string, Transform>();
            foreach (string r in new[] { "Managers", "Background", "Environment", "Gameplay", "NPCs", "UI" })
            {
                roots[r] = new GameObject(r).transform;
            }

            GameObject witch = null;
            GameObject camCopy = null;

            try
            {
                // ------------------------------------------------------------------
                // 1. Clonar infraestructura base desde Level1 (sin modificar Level1)
                // ------------------------------------------------------------------
                System.Type[] infraTypes =
                {
                    typeof(GameManager),
                    typeof(AudioManager),
                    typeof(TutorialManager),
                    typeof(CheckpointManager),
                    typeof(FrenzyController),
                    typeof(CaptureListener),
                    typeof(UnityEngine.EventSystems.EventSystem)
                };

                var copied = new HashSet<GameObject>();

                Camera cam1 = l1.GetRootGameObjects()
                    .SelectMany(g => g.GetComponentsInChildren<Camera>(true))
                    .FirstOrDefault(c => c.CompareTag("MainCamera"));
                if (cam1 != null)
                {
                    camCopy = CloneObject(cam1.gameObject, null);
                    copied.Add(cam1.gameObject);
                }

                HUDController hud1 = l1.GetRootGameObjects()
                    .SelectMany(g => g.GetComponentsInChildren<HUDController>(true))
                    .FirstOrDefault();
                if (hud1 != null && copied.Add(hud1.gameObject))
                {
                    CloneObject(hud1.gameObject, roots["UI"]);
                }

                foreach (var t in infraTypes)
                {
                    foreach (Component c in l1.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren(t, true)))
                    {
                        if (copied.Contains(c.gameObject) || IsUnderCopied(c.transform, copied)) continue;
                        copied.Add(c.gameObject);
                        Transform targetParent = t == typeof(UnityEngine.EventSystems.EventSystem) ? roots["UI"] : roots["Managers"];
                        CloneObject(c.gameObject, targetParent);
                    }
                }

                // ------------------------------------------------------------------
                // 2. Instanciar Bruja en X: 2, Y: 1 con componentes de Nivel 2/3
                // ------------------------------------------------------------------
                GameObject witchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WitchPrefabPath);
                witch = (GameObject)PrefabUtility.InstantiatePrefab(witchPrefab, l3);
                witch.name = "Witch";
                witch.transform.SetParent(roots["Gameplay"], false);
                witch.transform.position = new Vector3(2f, 1f, 0f);

                PlayerController pc1 = l1.GetRootGameObjects()
                    .SelectMany(g => g.GetComponentsInChildren<PlayerController>(true))
                    .FirstOrDefault();
                GameObject w1 = pc1 != null ? PrefabUtility.GetOutermostPrefabInstanceRoot(pc1.gameObject) : null;
                if (w1 != null)
                {
                    foreach (var ac in PrefabUtility.GetAddedComponents(w1))
                    {
                        Component srcComp = ac.instanceComponent;
                        if (srcComp == null) continue;
                        Component dst = witch.GetComponent(srcComp.GetType()) ?? witch.AddComponent(srcComp.GetType());
                        EditorUtility.CopySerialized(srcComp, dst);
                    }
                }

                EnsureWitchLevel3Components(witch);

                // Limpiar referencias cruzadas a Level1
                foreach (GameObject g in l3.GetRootGameObjects())
                {
                    foreach (MonoBehaviour mb in g.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        FixCrossSceneRefs(mb, l1, witch);
                    }
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(l1, true);
            }

            // Crear Luz Global 2D después de cerrar Level1 para evitar conflicto de múltiples Global Light2D en URP
            GameObject globalLightGo = new GameObject("Global Light 2D");
            globalLightGo.transform.SetParent(roots["Environment"], false);
            Light2D globalLight = globalLightGo.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.color = new Color(0.32f, 0.42f, 0.72f, 1f);
            globalLight.intensity = 0.58f;

            // ------------------------------------------------------------------
            // 3. Configurar Managers propios de Level 3 e Interfaz Contextual
            // ------------------------------------------------------------------
            SetupLevel3ManagersAndUI(roots["Managers"], roots["UI"], camCopy, witch);

            // ------------------------------------------------------------------
            // 4. Fondo Parallax de Bosque Nocturno
            // ------------------------------------------------------------------
            BuildForestParallax(roots["Background"]);

            // ------------------------------------------------------------------
            // 5. Construcción del Acto 1: El Sendero del Bosque (X: 0 a X: 115)
            // ------------------------------------------------------------------
            BuildAct1ForestTrail(roots["Environment"], roots["Gameplay"], roots["NPCs"], log);

            // ------------------------------------------------------------------
            // 6. Construcción del Acto 2: El Claro y la Casa de la Bruja (X: 112 a X: 150)
            // ------------------------------------------------------------------
            BuildAct2WitchCabinAndClearing(roots["Environment"], roots["Gameplay"], log);

            // ------------------------------------------------------------------
            // 7. Conectar CaptureListener a todos los guardias creados
            // ------------------------------------------------------------------
            CaptureListener captureListener = Object.FindFirstObjectByType<CaptureListener>();
            if (captureListener != null)
            {
                captureListener.ConnectEnemies();
            }

            Physics2D.SyncTransforms();

            // ------------------------------------------------------------------
            // 7. Guardar escena y registrar en EditorBuildSettings después de Level2
            // ------------------------------------------------------------------
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.MarkSceneDirty(l3);
            EditorSceneManager.SaveScene(l3, Level3Path);
            RegisterLevel3InBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Level3SceneBuilder] Escena Level3.unity construida y registrada exitosamente.\n" + log);
        }

        private static void EnsureWitchLevel3Components(GameObject witch)
        {
            if (witch == null) return;

            PlayerController pc = witch.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.CanFly = true;
                var soPc = new SerializedObject(pc);
                var canFlyProp = soPc.FindProperty("canFly");
                if (canFlyProp != null) canFlyProp.boolValue = true;
                soPc.ApplyModifiedPropertiesWithoutUndo();
            }

            if (witch.GetComponent<BroomInventory>() == null)
            {
                witch.AddComponent<BroomInventory>();
            }

            if (witch.GetComponent<PotionInventory>() == null)
            {
                witch.AddComponent<PotionInventory>();
            }

            if (witch.GetComponent<PlayerInvulnerability>() == null)
            {
                witch.AddComponent<PlayerInvulnerability>();
            }

            if (witch.GetComponent<LadderClimber>() == null)
            {
                witch.AddComponent<LadderClimber>();
            }

            if (witch.GetComponent<LadderClimbAnimator>() == null)
            {
                witch.AddComponent<LadderClimbAnimator>();
            }

            if (witch.GetComponent<PlayerInteractor>() == null)
            {
                witch.AddComponent<PlayerInteractor>();
            }

            if (witch.GetComponent<Level2Controls>() == null)
            {
                witch.AddComponent<Level2Controls>();
            }
        }

        private static void SetupLevel3ManagersAndUI(Transform managersRoot, Transform uiRoot, GameObject camCopy, GameObject witch)
        {
            // Level3Bootstrap
            GameObject bootGo = new GameObject("Level3_Bootstrap");
            bootGo.transform.SetParent(managersRoot, false);
            bootGo.AddComponent<Level3Bootstrap>();

            // Level3StateController
            GameObject stateGo = new GameObject("Level3_StateController");
            stateGo.transform.SetParent(managersRoot, false);
            stateGo.AddComponent<Level3StateController>();

            // KeyInventory
            GameObject keyInvGo = new GameObject("KeyInventory");
            keyInvGo.transform.SetParent(managersRoot, false);
            keyInvGo.AddComponent<KeyInventory>();

            // InteractionPromptUI
            GameObject promptGo = new GameObject("InteractionPrompt_Canvas");
            promptGo.transform.SetParent(uiRoot, false);
            InteractionPromptUI promptUI = promptGo.AddComponent<InteractionPromptUI>();
            promptUI.EnsureUIHierarchy();

            // Asegurar mínimo 3 pociones de sueño en GameManager serializado
            GameManager gm = Object.FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.SleepCharges = Mathf.Max(gm.SleepCharges, 3);
                gm.BlindCharges = Mathf.Max(gm.BlindCharges, 1);
                gm.HypnotizeCharges = Mathf.Max(gm.HypnotizeCharges, 1);
                EditorUtility.SetDirty(gm);
            }

            // Configurar cámara principal
            if (camCopy != null && witch != null)
            {
                camCopy.transform.position = new Vector3(2f, 2.2f, -10f);
                CameraFollow follow = camCopy.GetComponent<CameraFollow>();
                if (follow != null)
                {
                    follow.SetTarget(witch.transform);
                    var so = new SerializedObject(follow);
                    var minProp = so.FindProperty("minLimit");
                    var maxProp = so.FindProperty("maxLimit");
                    var useLimitsProp = so.FindProperty("useLimits");
                    if (useLimitsProp != null) useLimitsProp.boolValue = true;
                    if (minProp != null) minProp.vector2Value = new Vector2(-2f, -1f);
                    if (maxProp != null) maxProp.vector2Value = new Vector2(240f, 18f);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void BuildForestParallax(Transform bgRoot)
        {
            Sprite skySprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Sky_Far.png");
            if (skySprite != null)
            {
                GameObject skyObj = new GameObject("Sky_Far");
                skyObj.transform.SetParent(bgRoot, false);
                skyObj.transform.position = new Vector3(0f, 4.5f, 0f);
                ParallaxUtility.SetupThreeCopies(skyObj, skySprite, "Background", 0);
                ParallaxLayer skyPl = skyObj.GetComponent<ParallaxLayer>();
                if (skyPl != null) skyPl.Setup(0.9f, true, true);
            }

            Sprite treesSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Trees_Mid.png");
            if (treesSprite != null)
            {
                GameObject treesObj = new GameObject("Trees_Mid");
                treesObj.transform.SetParent(bgRoot, false);
                treesObj.transform.position = new Vector3(0f, 2.0f, 0f);
                ParallaxUtility.SetupThreeCopies(treesObj, treesSprite, "Midground", 0);
                ParallaxLayer treesPl = treesObj.GetComponent<ParallaxLayer>();
                if (treesPl != null) treesPl.Setup(0.6f, true, true);
            }
        }

        // =========================================================================
        // CONSTRUCCIÓN DEL ACTO 1: EL SENDERO DEL BOSQUE (X: 0 a X: 115)
        // =========================================================================
        private static void BuildAct1ForestTrail(Transform envRoot, Transform gameplayRoot, Transform npcsRoot, StringBuilder log)
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            LayerMask groundMask = 1 << groundLayer;

            Sprite groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GroundSpritePath);
            Sprite logASprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_A.png");
            Sprite logBSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_B.png");
            Sprite logCSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_C.png");
            Sprite signSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Sign.png");
            Sprite gateSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Goal_Gate.png");

            Sprite cornCluster = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Corn_Cluster.png");
            Sprite stalkA = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Corn_Stalk_A.png");
            Sprite stalkB = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Corn_Stalk_B.png");
            Sprite stalkC = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Corn_Stalk_C.png");

            GameObject guardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GuardPrefabPath);
            GameObject villagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VillagerPrefabPath);
            GameObject villagerFPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VillagerFPrefabPath);
            GameObject checkpointPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CheckpointPrefabPath);

            GameObject potSleepPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PotionSleepPath);
            GameObject potBlindPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PotionBlindPath);
            GameObject potHypnoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PotionHypnotizePath);
            GameObject heartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeartPickupPath);

            Transform act1Env = new GameObject("Act1_ForestTrail_Env").transform;
            act1Env.SetParent(envRoot, false);

            Transform act1Gameplay = new GameObject("Act1_ForestTrail_Gameplay").transform;
            act1Gameplay.SetParent(gameplayRoot, false);

            Transform act1Pickups = new GameObject("Act1_Pickups").transform;
            act1Pickups.SetParent(act1Gameplay, false);

            Transform act1Npcs = new GameObject("Act1_NPCs").transform;
            act1Npcs.SetParent(npcsRoot, false);

            // ------------------------------------------------------------------
            // SUELO PRINCIPAL DEL ACTO 1 Y ACTO 2:
            // - Tramo A: X: -6 a X: 52 (58u de largo, superficie en Y: 0.17)
            // - Hueco del Barranco del Río Oscuro: X: 52 a X: 68 (16u de ancho)
            // - Tramo B: X: 68 a X: 168 (100u de largo, superficie en Y: 0.17, cubre Bastión + Claro + Cabaña)
            // ------------------------------------------------------------------
            CreateGroundSegment(act1Env, "Ground_Forest_West", -6f, 58f, groundLayer, groundSprite);
            CreateGroundSegment(act1Env, "Ground_Forest_East", 68f, 100f, groundLayer, groundSprite);

            // Muro límite izquierdo en X: -4 para impedir caer fuera del mapa
            CreateSolidWall(act1Env, "Boundary_WestWall", new Vector2(-4.5f, 6f), new Vector2(1.5f, 14f), groundLayer, null);

            // ------------------------------------------------------------------
            // 1. INICIO: SALIDA DE LA ALDEA HACIA EL BOSQUE (X: 0 a 34)
            // ------------------------------------------------------------------
            // Checkpoint inicial junto al spawn
            CreateCheckpoint(act1Gameplay, checkpointPrefab, "Checkpoint_Act1_Start", new Vector3(1.2f, 0.17f, 0f));

            // Cartel inicial (WorldSign)
            CreateWorldSign(
                act1Gameplay,
                "Sign_Act1_Intro",
                new Vector2(4.2f, 0.9f),
                signSprite,
                "El bosque hacia tu cabaña está vigilado. Busca a los guardias con AURA DORADA, duérmelos con una Poción de Sueño [Q] para quitarles la llave de los portones.");

            // Entrega inicial garantizada: 2 PotionPickup (Sleep) en X: 6 y X: 10
            InstantiatePickup(act1Pickups, potSleepPrefab, "PotionPickup_Sleep_Start_1", new Vector3(6f, 0.72f, 0f));
            InstantiatePickup(act1Pickups, potSleepPrefab, "PotionPickup_Sleep_Start_2", new Vector3(10f, 0.72f, 0f));

            // Escondite (HideoutZone) en X: 16 para observar la patrulla
            CreateForestHideout(act1Env, "Hideout_Forest_1", 16f, 0f, 4.5f, 3.8f, cornCluster, stalkA, stalkB, stalkC);

            // Aldeano patrullando en la parte baja (X: 20)
            CreateVillager(act1Npcs, villagerPrefab, "Villager_Forest_1", new Vector3(20f, 0.17f, 0f), WanderPattern.WalkAndReturn, 3.2f);

            // Guardia Portador #1 (KeyCarrierGuard) patrullando entre X: 18 y X: 28
            Transform[] wpCarrier1 = CreateWaypoints(act1Npcs, "Waypoints_KeyCarrier_1", new Vector2(18f, 0.17f), new Vector2(28f, 0.17f));
            GameObject guardCarrier1 = CreateGuard(act1Npcs, guardPrefab, "Guard_KeyCarrier_1", new Vector3(24f, 0.17f, 0f), wpCarrier1, PatrolMode.PingPong, groundMask);
            if (guardCarrier1 != null)
            {
                KeyCarrierGuard kc1 = guardCarrier1.AddComponent<KeyCarrierGuard>();
                kc1.Configure(
                    Level3StateController.ForestGateKey1Id,
                    "Llave del Portón del Bosque I",
                    new Color(1f, 0.85f, 0.2f, 0.9f),
                    autoCollect: false);
                EditorUtility.SetDirty(kc1);
            }

            // En X: 34: Portón Bloqueado #1 (LockedGateInteractable) con barrera superior antiescoba
            CreateLockedGateWithCanopyBlocker(
                act1Gameplay,
                "Gate_Forest_1",
                new Vector3(34f, 0.17f, 0f),
                "forest_gate_1",
                "Portón del Bosque I",
                Level3StateController.ForestGateKey1Id,
                "Llave del Portón del Bosque I",
                requireMasterPotion: false,
                advanceCabinPhase: false,
                gateSprite,
                groundLayer);

            log.AppendLine("  - Tramo 1 (X: 0..34): Spawn (2,1), Sign inicial, 2 Pociones Sleep (X:6, X:10), Hideout (X:16), Aldeano, Guardia Portador #1 (X:18..28) y Portón #1 (X:34).");

            // ------------------------------------------------------------------
            // 2. RETO 1: EL TRONCO HUECO (GATO) Y EL BARRANCO DE ESPINAS (X: 35 a 72)
            // ------------------------------------------------------------------
            // Checkpoint en X: 37
            CreateCheckpoint(act1Gameplay, checkpointPrefab, "Checkpoint_Act1_Trunk", new Vector3(37f, 0.17f, 0f));

            CreateWorldSign(
                act1Gameplay,
                "Sign_Act1_TrunkAndRavine",
                new Vector2(38.6f, 0.9f),
                signSprite,
                "Muro de raíces bajas: transfórmate en Gato [F] para cruzar por debajo. En el Barranco del Río Oscuro vuela con tu Escoba.");

            // En X: 40 a 50: Muro de raíces bajas / Tronco Hueco con CatOnlyPassage
            // El techo bajo bloquea a la bruja humana (deja paso de Y: 0.17 a Y: 1.12, altura ~0.95u)
            // y su cara superior en Y: 2.55 sirve de plataforma para el guardia superior.
            BuildHollowRootTunnel(
                act1Env,
                act1Gameplay,
                act1Pickups,
                act1Npcs,
                40f,
                50f,
                groundLayer,
                groundMask,
                logBSprite,
                potSleepPrefab,
                potBlindPrefab,
                heartPrefab,
                guardPrefab);

            // En X: 52 a 68: Gran Barranco del Río Oscuro con KillZone en Y: -4
            BuildDarkRiverRavine(
                act1Env,
                act1Gameplay,
                act1Npcs,
                52f,
                68f,
                groundLayer,
                groundMask,
                logASprite,
                logCSprite,
                cornCluster,
                stalkA,
                stalkB,
                stalkC,
                guardPrefab);

            log.AppendLine("  - Reto 1 (X: 35..72): Checkpoint (X:37), Túnel de Gato (X:40..50) con Guardia arriba y 2 Pociones + 1 Corazón dentro, Barranco (X:52..68) con KillZone (Y:-4), Copas (Y:4.5, Y:6.5) y FlightOnlyHideout (X:60, Y:6.8).");

            // ------------------------------------------------------------------
            // 3. RETO 2: EL BASTIÓN DEL BOSQUE Y GUARDIA PORTADOR #2 (X: 72 a 112)
            // ------------------------------------------------------------------
            // Checkpoint en X: 74
            CreateCheckpoint(act1Gameplay, checkpointPrefab, "Checkpoint_Act1_Bastion", new Vector3(74f, 0.17f, 0f));

            CreateWorldSign(
                act1Gameplay,
                "Sign_Act1_Bastion",
                new Vector2(76.2f, 0.9f),
                signSprite,
                "Bastión del Bosque: Vuela a la plataforma superior (Y: 4), duerme al Guardia Dorado con [Q/E] y toma la Llave del Portón de la Cabaña.");

            BuildForestBastion(
                act1Env,
                act1Gameplay,
                act1Pickups,
                act1Npcs,
                groundLayer,
                groundMask,
                logASprite,
                logBSprite,
                logCSprite,
                cornCluster,
                stalkA,
                stalkB,
                stalkC,
                potSleepPrefab,
                potHypnoPrefab,
                heartPrefab,
                guardPrefab,
                villagerFPrefab);

            // En X: 110: Portón Bloqueado #2 (LockedGateInteractable) que requiere "forest_gate_key_2"
            CreateLockedGateWithCanopyBlocker(
                act1Gameplay,
                "Gate_Forest_2",
                new Vector3(110f, 0.17f, 0f),
                "forest_gate_2",
                "Portón de la Cabaña de la Bruja",
                Level3StateController.ForestGateKey2Id,
                "Llave del Portón de la Cabaña",
                requireMasterPotion: false,
                advanceCabinPhase: true,
                gateSprite,
                groundLayer);

            // Decoración atmosférica del bosque con props de Cainos y luces de farol
            DecorateAct1Forest(act1Env);

            log.AppendLine("  - Reto 2 (X: 72..112): Checkpoint (X:74), Bastión de 2 pisos con Guardia + Aldeana abajo, CatOnlyPassage lateral (X:82) con pociones Sleep, Guardia Portador #2 en piso alto (X:86..98, Y:4.0) y Portón #2 (X:110).");
        }

        // =========================================================================
        // CONSTRUCCIÓN DEL ACTO 2: EL CLARO Y LA CASA DE LA BRUJA (X: 112 a X: 150)
        // =========================================================================
        private static void BuildAct2WitchCabinAndClearing(Transform envRoot, Transform gameplayRoot, StringBuilder log)
        {
            int groundLayer = LayerMask.NameToLayer("Ground");

            Sprite logASprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_A.png");
            Sprite logBSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_B.png");
            Sprite logCSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_C.png");
            Sprite signSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Sign.png");
            Sprite gateSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Goal_Gate.png");
            Sprite treesSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Trees_Mid.png");

            GameObject checkpointPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CheckpointPrefabPath);
            GameObject potSleepPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PotionSleepPath);
            Sprite[] cainosSprites = AssetDatabase.LoadAllAssetsAtPath(CainosAtlasPath).OfType<Sprite>().ToArray();

            Transform act2Env = new GameObject("Act2_WitchCabin_Env").transform;
            act2Env.SetParent(envRoot, false);

            Transform act2Gameplay = new GameObject("Act2_WitchCabin_Gameplay").transform;
            act2Gameplay.SetParent(gameplayRoot, false);

            // 1. Exterior del Claro (X: 112 a 148): Checkpoint en X: 116, árboles antiguos y faroles mágicos
            CreateCheckpoint(act2Gameplay, checkpointPrefab, "Checkpoint_Act2_CabinClearing", new Vector3(116f, 0.17f, 0f));

            CreateWorldSign(
                act2Gameplay,
                "Sign_Act2_CabinClearing",
                new Vector2(118.8f, 0.9f),
                signSprite,
                "Claro de la Cabaña de la Bruja: Entra a tu santuario y prepara la Poción Maestra en el Caldero [E / Q] antes de regresar a la Iglesia de Salem.");

            // Árboles antiguos de fondo en el claro
            if (treesSprite != null)
            {
                float[] treeXs = { 114.5f, 121.0f, 139.0f, 144.5f };
                for (int i = 0; i < treeXs.Length; i++)
                {
                    GameObject treeGo = new GameObject($"AncientTree_Backdrop_{i}");
                    treeGo.transform.SetParent(act2Env, false);
                    treeGo.transform.position = new Vector3(treeXs[i], 2.4f, 0f);
                    treeGo.transform.localScale = new Vector3(0.65f, 0.85f, 1f);
                    SpriteRenderer tSr = treeGo.AddComponent<SpriteRenderer>();
                    tSr.sprite = treesSprite;
                    tSr.color = new Color(0.55f, 0.48f, 0.72f, 0.85f);
                    tSr.sortingLayerName = "Midground";
                    tSr.sortingOrder = 1;
                }
            }

            // Faroles mágicos con LanternFlicker (tonos violeta y ámbar)
            CreateMysticLantern(act2Env, cainosSprites, "MysticLantern_Violet_1", new Vector3(114.2f, 1.35f, 0f), new Color(0.75f, 0.38f, 1f, 1f));
            CreateMysticLantern(act2Env, cainosSprites, "MysticLantern_Amber_1", new Vector3(122.2f, 1.35f, 0f), new Color(1f, 0.78f, 0.28f, 1f));
            CreateMysticLantern(act2Env, cainosSprites, "MysticLantern_Amber_2", new Vector3(137.8f, 1.35f, 0f), new Color(1f, 0.78f, 0.28f, 1f));
            CreateMysticLantern(act2Env, cainosSprites, "MysticLantern_Violet_2", new Vector3(143.2f, 1.35f, 0f), new Color(0.75f, 0.38f, 1f, 1f));

            // 2. Pabellón / Casa de la Bruja en X: 124 a 136 (centro X: 128..130)
            GameObject cabinRoot = new GameObject("WitchCabin_Pavilion");
            cabinRoot.transform.SetParent(act2Env, false);
            cabinRoot.transform.position = new Vector3(130f, 0.17f, 0f);

            // Pared de fondo de madera oscura del santuario (X: 124 a 136)
            for (int i = 0; i < 3; i++)
            {
                GameObject backWall = new GameObject($"Cabin_BackWall_{i}");
                backWall.transform.SetParent(cabinRoot.transform, false);
                backWall.transform.localPosition = new Vector3(-4.0f + i * 4.0f, 2.2f, 0f);
                backWall.transform.localScale = new Vector3(1.35f, 6.8f, 1f);
                SpriteRenderer bwSr = backWall.AddComponent<SpriteRenderer>();
                bwSr.sprite = logBSprite;
                bwSr.color = new Color(0.24f, 0.17f, 0.22f, 0.96f);
                bwSr.sortingLayerName = "Midground";
                bwSr.sortingOrder = 3;
            }

            // Pilares de madera en los extremos del porche (X: 124 y X: 136)
            PlaceCainosSprite(cabinRoot.transform, cainosSprites, "Cabin_Pillar_West", "TX Village Props - Stall Tent Pillar", new Vector3(124.2f, 1.65f, 0f), 1.35f);
            PlaceCainosSprite(cabinRoot.transform, cainosSprites, "Cabin_Pillar_East", "TX Village Props - Stall Tent Pillar", new Vector3(135.8f, 1.65f, 0f), 1.35f);

            // Techo alto del pabellón (X: 124 a 136, Y: 4.8)
            GameObject cabinRoof = new GameObject("Cabin_HighRoof");
            cabinRoof.transform.SetParent(cabinRoot.transform, false);
            cabinRoof.layer = groundLayer;
            cabinRoof.transform.position = new Vector3(130f, 4.8f, 0f);

            BoxCollider2D roofCol = cabinRoof.AddComponent<BoxCollider2D>();
            roofCol.size = new Vector2(13.2f, 0.45f);
            roofCol.usedByEffector = true;
            PlatformEffector2D roofEf = cabinRoof.AddComponent<PlatformEffector2D>();
            roofEf.useOneWay = true;
            roofEf.surfaceArc = 170f;

            for (int i = 0; i < 3; i++)
            {
                GameObject roofSeg = new GameObject($"Cabin_RoofSeg_{i}");
                roofSeg.transform.SetParent(cabinRoof.transform, false);
                roofSeg.transform.localPosition = new Vector3(-4.2f + i * 4.2f, 0.1f, 0f);
                roofSeg.transform.localScale = new Vector3(1.3f, 1.35f, 1f);
                SpriteRenderer rSr = roofSeg.AddComponent<SpriteRenderer>();
                rSr.sprite = logCSprite;
                rSr.color = new Color(0.38f, 0.25f, 0.34f, 1f);
                rSr.sortingLayerName = "Gameplay";
                rSr.sortingOrder = 3;
            }

            // Repisas interiores de alquimia (Y: 2.35) y mesas laterales
            CreateOneWayPlatform(cabinRoot.transform, "Cabin_AlchemyShelf_Left", logASprite, new Vector2(126.5f, 2.4f), 0.9f, groundLayer);
            CreateOneWayPlatform(cabinRoot.transform, "Cabin_AlchemyShelf_Right", logASprite, new Vector2(133.5f, 2.4f), 0.9f, groundLayer);

            PlaceCainosSprite(cabinRoot.transform, cainosSprites, "AlchemyTable_Left", "TX Village Props - Stall Table", new Vector3(126.6f, 0.72f, 0f), 1.0f);
            PlaceCainosSprite(cabinRoot.transform, cainosSprites, "AlchemyTable_Right", "TX Village Props - Stall Table", new Vector3(133.4f, 0.72f, 0f), 1.0f);
            PlaceCainosSprite(cabinRoot.transform, cainosSprites, "AlchemyPot_1", "TX Village Props - Pot 01", new Vector3(126.2f, 1.35f, 0f), 1.0f);
            PlaceCainosSprite(cabinRoot.transform, cainosSprites, "AlchemyPot_2", "TX Village Props - Pot 02", new Vector3(133.8f, 1.35f, 0f), 1.0f);
            PlaceCainosSprite(cabinRoot.transform, cainosSprites, "AlchemyKettle", "TX Village Props - Kettle", new Vector3(127.1f, 1.32f, 0f), 1.0f);

            // 3. Salida hacia el Regreso a la Aldea en X: 146 (LockedGateInteractable con requireMasterPotion = true)
            LockedGateInteractable returnGate = CreateLockedGateWithCanopyBlocker(
                act2Gameplay,
                "Gate_ReturnVillage",
                new Vector3(146f, 0.17f, 0f),
                WitchCauldronInteractable.ReturnGateId,
                "Portón del Sendero de Regreso a la Aldea",
                requiredKeyId: string.Empty,
                requiredKeyDisplayName: "Poción Maestra del Caldero",
                requireMasterPotion: true,
                advanceCabinPhase: false,
                gateSprite,
                groundLayer);

            // Teñir el portón de regreso con un tono místico violeta
            SpriteRenderer retGateSr = returnGate.GetComponentInChildren<SpriteRenderer>();
            if (retGateSr != null)
            {
                retGateSr.color = new Color(0.78f, 0.52f, 1f, 1f);
            }

            // 4. WitchCauldronInteractable en el centro de la Casa de la Bruja (X: 130, Y: 0.17)
            GameObject cauldronGo = new GameObject("Witch_Cauldron_Interactable");
            cauldronGo.transform.SetParent(act2Gameplay, false);
            cauldronGo.transform.position = new Vector3(130f, 0.82f, 0f);
            cauldronGo.transform.localScale = new Vector3(1.45f, 1.45f, 1f);

            Sprite cauldronSprite = cainosSprites.FirstOrDefault(s => s != null && s.name == "TX Village Props - Cauldron");
            SpriteRenderer cauldronSr = cauldronGo.AddComponent<SpriteRenderer>();
            cauldronSr.sprite = cauldronSprite != null ? cauldronSprite : logASprite;
            cauldronSr.sortingLayerName = "Gameplay";
            cauldronSr.sortingOrder = 4;

            BoxCollider2D cauldronCol = cauldronGo.AddComponent<BoxCollider2D>();
            cauldronCol.isTrigger = true;
            cauldronCol.size = new Vector2(2.6f, 2.4f);

            WitchCauldronInteractable cauldronComp = cauldronGo.AddComponent<WitchCauldronInteractable>();
            cauldronComp.SetReturnGate(returnGate);
            var soCauldron = new SerializedObject(cauldronComp);
            soCauldron.FindProperty("returnVillageGate").objectReferenceValue = returnGate;
            soCauldron.FindProperty("cauldronRenderer").objectReferenceValue = cauldronSr;
            soCauldron.ApplyModifiedPropertiesWithoutUndo();

            // 5. Estaciones de Reabastecimiento de Pociones (PotionRefillStation) en la Cabaña y ruta de regreso
            Sprite sleepFlaskSprite = null;
            if (potSleepPrefab != null)
            {
                SpriteRenderer pfSr = potSleepPrefab.GetComponentInChildren<SpriteRenderer>();
                if (pfSr != null) sleepFlaskSprite = pfSr.sprite;
            }

            CreatePotionRefillStation(act2Gameplay, "PotionRefillStation_CabinPorch", new Vector3(134.8f, 0.72f, 0f),
                "refill_cabin_porch", "Estante Alquímico de la Cabaña", PotionType.Sleep, 2, sleepFlaskSprite);

            CreatePotionRefillStation(act2Gameplay, "PotionRefillStation_ReturnTrail", new Vector3(150.5f, 0.72f, 0f),
                "refill_return_trail_1", "Alijo del Sendero de Regreso", PotionType.Sleep, 2, sleepFlaskSprite);

            log.AppendLine("  - Acto 2 (X: 112..150): Claro seguro con Checkpoint (X:116), Faroles con LanternFlicker, Casa de la Bruja (X:124..136), WitchCauldronInteractable (X:130), PotionRefillStations y Portón de Regreso a la Aldea (X:146).");
        }

        public static void CreateMysticLantern(Transform parent, Sprite[] cainosAtlas, string name, Vector3 pos, Color lightColor)
        {
            GameObject lanternGo = new GameObject(name);
            lanternGo.transform.SetParent(parent, false);
            lanternGo.transform.position = pos;
            lanternGo.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

            Sprite pillarSp = cainosAtlas.FirstOrDefault(s => s != null && s.name == "TX Village Props - Road Lamp Pillar");
            if (pillarSp != null)
            {
                SpriteRenderer sr = lanternGo.AddComponent<SpriteRenderer>();
                sr.sprite = pillarSp;
                sr.sortingLayerName = "Midground";
                sr.sortingOrder = 2;
            }

            GameObject lightChild = new GameObject("MysticLantern_Light2D");
            lightChild.transform.SetParent(lanternGo.transform, false);
            lightChild.transform.localPosition = new Vector3(0f, 1.85f, 0f);

            Light2D l2d = lightChild.AddComponent<Light2D>();
            l2d.lightType = Light2D.LightType.Point;
            l2d.color = lightColor;
            l2d.intensity = 1.45f;
            l2d.pointLightInnerRadius = 0.4f;
            l2d.pointLightOuterRadius = 4.5f;
            l2d.falloffIntensity = 0.5f;

            lightChild.AddComponent<LanternFlicker>();
        }

        public static PotionRefillStation CreatePotionRefillStation(
            Transform parent,
            string objectName,
            Vector3 pos,
            string stationId,
            string displayName,
            PotionType type,
            int amount,
            Sprite flaskSprite)
        {
            GameObject stationGo = new GameObject(objectName);
            stationGo.transform.SetParent(parent, false);
            stationGo.transform.position = pos;

            BoxCollider2D col = stationGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.8f, 1.8f);

            GameObject visualGo = new GameObject("FlaskVisual");
            visualGo.transform.SetParent(stationGo.transform, false);
            visualGo.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            visualGo.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

            SpriteRenderer sr = visualGo.AddComponent<SpriteRenderer>();
            sr.sprite = flaskSprite != null ? flaskSprite : LootPopupVisualizer.GetStarSprite();
            sr.color = new Color(0.55f, 0.82f, 1f, 1f);
            sr.sortingLayerName = "Foreground";
            sr.sortingOrder = 10;

            PotionRefillStation station = stationGo.AddComponent<PotionRefillStation>();
            station.Configure(stationId, displayName, type, amount, 25f);

            var so = new SerializedObject(station);
            so.FindProperty("flaskRenderer").objectReferenceValue = sr;
            so.ApplyModifiedPropertiesWithoutUndo();

            return station;
        }

        private static void BuildHollowRootTunnel(
            Transform envParent,
            Transform gameplayParent,
            Transform pickupsParent,
            Transform npcsParent,
            float xStart,
            float xEnd,
            int groundLayer,
            LayerMask groundMask,
            Sprite logSprite,
            GameObject potSleepPrefab,
            GameObject potBlindPrefab,
            GameObject heartPrefab,
            GameObject guardPrefab)
        {
            float width = xEnd - xStart;
            float centerX = (xStart + xEnd) * 0.5f;

            GameObject tunnelRoot = new GameObject("HollowTrunk_CatTunnel");
            tunnelRoot.transform.SetParent(envParent, false);

            // Bloque físico de raíces/tronco superior:
            // Base inferior en Y = 1.12 (bloquea forma humana de 1.7u, permite forma de gato de 0.7u)
            // Cara superior en Y = 2.52 (piso caminable para el guardia y la bruja)
            GameObject ceilingGo = new GameObject("Tunnel_Roof_Solid");
            ceilingGo.transform.SetParent(tunnelRoot.transform, false);
            ceilingGo.layer = groundLayer;
            ceilingGo.transform.position = new Vector3(centerX, 1.82f, 0f);

            BoxCollider2D ceilingCol = ceilingGo.AddComponent<BoxCollider2D>();
            ceilingCol.size = new Vector2(width, 1.40f);
            ceilingCol.offset = Vector2.zero;

            // Visuales del tronco hueco y plataforma superior
            int visualSegments = 3;
            float segW = width / visualSegments;
            for (int i = 0; i < visualSegments; i++)
            {
                GameObject vis = new GameObject($"TrunkRoof_Visual_{i}");
                vis.transform.SetParent(ceilingGo.transform, false);
                vis.transform.localPosition = new Vector3((-width * 0.5f) + segW * (i + 0.5f), 0.15f, 0f);
                vis.transform.localScale = new Vector3(1.35f, 2.2f, 1f);
                SpriteRenderer sr = vis.AddComponent<SpriteRenderer>();
                sr.sprite = logSprite;
                sr.color = new Color(0.55f, 0.42f, 0.30f, 1f);
                sr.sortingLayerName = "Gameplay";
                sr.sortingOrder = 2;
            }

            // Follaje frontal semitransparente en el túnel para que se vea a la gata cruzar
            GameObject tunnelFront = new GameObject("Tunnel_Bark_Front");
            tunnelFront.transform.SetParent(tunnelRoot.transform, false);
            tunnelFront.transform.position = new Vector3(centerX, 0.65f, 0f);
            tunnelFront.transform.localScale = new Vector3(width * 0.32f, 0.65f, 1f);
            SpriteRenderer frontSr = tunnelFront.AddComponent<SpriteRenderer>();
            frontSr.sprite = logSprite;
            frontSr.color = new Color(0.28f, 0.20f, 0.14f, 0.42f);
            frontSr.sortingLayerName = "Foreground";
            frontSr.sortingOrder = 6;

            // Triggers CatOnlyPassage en la entrada (X: 40) y salida (X: 50)
            CreateCatOnlyTrigger(gameplayParent, "CatOnlyPassage_Trunk_Entry", new Vector3(xStart - 0.35f, 1.05f, 0f),
                "Raíces bajas: Pulsa [F] para transformarte en Gato y pasar por debajo.");
            CreateCatOnlyTrigger(gameplayParent, "CatOnlyPassage_Trunk_Exit", new Vector3(xEnd + 0.35f, 1.05f, 0f),
                "Raíces bajas: Pulsa [F] para transformarte en Gato y pasar por debajo.");

            // Recompensas dentro del túnel de gato: 2 PotionPickup (Sleep y Blind) + 1 HeartPickup
            InstantiatePickup(pickupsParent, potSleepPrefab, "PotionPickup_Tunnel_Sleep", new Vector3(43f, 0.58f, 0f));
            InstantiatePickup(pickupsParent, heartPrefab, "HeartPickup_Tunnel", new Vector3(45f, 0.58f, 0f));
            InstantiatePickup(pickupsParent, potBlindPrefab, "PotionPickup_Tunnel_Blind", new Vector3(47f, 0.58f, 0f));

            // Guardia patrullando la plataforma superior del tronco (X: 41 a 49, Y: 2.55)
            Transform[] wpRoof = CreateWaypoints(npcsParent, "Waypoints_Guard_TrunkRoof", new Vector2(41.2f, 2.55f), new Vector2(48.8f, 2.55f));
            CreateGuard(npcsParent, guardPrefab, "Guard_TrunkRoof", new Vector3(45f, 2.55f, 0f), wpRoof, PatrolMode.PingPong, groundMask);
        }

        private static void BuildDarkRiverRavine(
            Transform envParent,
            Transform gameplayParent,
            Transform npcsParent,
            float ravineStartX,
            float ravineEndX,
            int groundLayer,
            LayerMask groundMask,
            Sprite logASprite,
            Sprite logCSprite,
            Sprite cornCluster,
            Sprite stalkA,
            Sprite stalkB,
            Sprite stalkC,
            GameObject guardPrefab)
        {
            float ravineWidth = ravineEndX - ravineStartX;
            float centerX = (ravineStartX + ravineEndX) * 0.5f;

            // 1. KillZone en el fondo (Y: -4)
            GameObject kzGo = new GameObject("KillZone_DarkRiverRavine");
            kzGo.transform.SetParent(gameplayParent, false);
            kzGo.transform.position = new Vector3(centerX, -4f, 0f);
            BoxCollider2D kzCol = kzGo.AddComponent<BoxCollider2D>();
            kzCol.isTrigger = true;
            kzCol.size = new Vector2(ravineWidth + 2f, 2.2f);
            kzGo.AddComponent<KillZone>();

            // 2. Aviso de vuelo al borde del barranco (FlightOnlyHint)
            GameObject hintGo = new GameObject("FlightOnlyHint_RavineWest");
            hintGo.transform.SetParent(gameplayParent, false);
            hintGo.transform.position = new Vector3(ravineStartX - 0.8f, 1.4f, 0f);
            BoxCollider2D hintCol = hintGo.AddComponent<BoxCollider2D>();
            hintCol.isTrigger = true;
            hintCol.size = new Vector2(2.4f, 2.8f);
            hintGo.AddComponent<FlightOnlyHint>();

            // 3. Plataformas elevadas en las copas de los árboles (Y: 4.5 y Y: 6.5)
            CreateOneWayPlatform(envParent, "Platform_Canopy_West", logASprite, new Vector2(55.2f, 4.5f), 1.35f, groundLayer);
            CreateOneWayPlatform(envParent, "Platform_Canopy_HighCenter", logCSprite, new Vector2(60.0f, 6.5f), 1.55f, groundLayer);
            CreateOneWayPlatform(envParent, "Platform_Canopy_East", logASprite, new Vector2(64.8f, 4.5f), 1.45f, groundLayer);

            // 4. FlightOnlyHideout en las ramas altas (X: 60, Y: 6.8) para esquivar la vista del centinela
            CreateForestHideout(envParent, "FlightOnlyHideout_Canopy", 60.0f, 6.65f, 3.8f, 2.6f, cornCluster, stalkA, stalkB, stalkC);

            // 5. Centinela vigilando las copas / borde oriental del barranco
            Transform[] wpSentinel = CreateWaypoints(npcsParent, "Waypoints_Guard_RavineSentinel", new Vector2(63.6f, 4.77f), new Vector2(66.0f, 4.77f));
            CreateGuard(npcsParent, guardPrefab, "Guard_RavineSentinel", new Vector3(64.8f, 4.77f, 0f), wpSentinel, PatrolMode.PingPong, groundMask);
        }

        private static void BuildForestBastion(
            Transform envParent,
            Transform gameplayParent,
            Transform pickupsParent,
            Transform npcsParent,
            int groundLayer,
            LayerMask groundMask,
            Sprite logASprite,
            Sprite logBSprite,
            Sprite logCSprite,
            Sprite cornCluster,
            Sprite stalkA,
            Sprite stalkB,
            Sprite stalkC,
            GameObject potSleepPrefab,
            GameObject potHypnoPrefab,
            GameObject heartPrefab,
            GameObject guardPrefab,
            GameObject villagerFPrefab)
        {
            GameObject bastionRoot = new GameObject("Forest_Bastion_Structure");
            bastionRoot.transform.SetParent(envParent, false);

            // 1. Pequeño CatOnlyPassage lateral en X: 82 (nicho de raíces de X: 80.5 a X: 84.5)
            // Techo bajo en Y: 1.12 a Y: 2.35 para que solo entre en forma de gato
            GameObject alcoveRoof = new GameObject("Bastion_CatAlcove_Roof");
            alcoveRoof.transform.SetParent(bastionRoot.transform, false);
            alcoveRoof.layer = groundLayer;
            alcoveRoof.transform.position = new Vector3(82.5f, 1.72f, 0f);
            BoxCollider2D alcoveCol = alcoveRoof.AddComponent<BoxCollider2D>();
            alcoveCol.size = new Vector2(4.2f, 1.2f);

            SpriteRenderer alcoveSr = alcoveRoof.AddComponent<SpriteRenderer>();
            alcoveSr.sprite = logBSprite;
            alcoveSr.color = new Color(0.48f, 0.36f, 0.26f, 1f);
            alcoveSr.sortingLayerName = "Gameplay";
            alcoveSr.sortingOrder = 2;
            alcoveRoof.transform.localScale = new Vector3(1.25f, 1.8f, 1f);
            alcoveCol.size = new Vector2(4.2f / 1.25f, 1.2f / 1.8f);

            CreateCatOnlyTrigger(gameplayParent, "CatOnlyPassage_BastionAlcove", new Vector3(82f, 0.95f, 0f),
                "Nicho estrecho: Usa forma de Gato [F] para recoger las pociones ocultas.");

            // Recargas de PotionPickup (Sleep) ocultas tras el CatOnlyPassage lateral en X: 82
            InstantiatePickup(pickupsParent, potSleepPrefab, "PotionPickup_Bastion_Sleep_1", new Vector3(81.6f, 0.58f, 0f));
            InstantiatePickup(pickupsParent, potSleepPrefab, "PotionPickup_Bastion_Sleep_2", new Vector3(83.2f, 0.58f, 0f));

            // 2. Planta baja (Y: 0.17): 1 Guardia normal + 1 Aldeano chismoso
            Transform[] wpBastionGround = CreateWaypoints(npcsParent, "Waypoints_Guard_BastionGround", new Vector2(76.5f, 0.17f), new Vector2(88.5f, 0.17f));
            CreateGuard(npcsParent, guardPrefab, "Guard_Bastion_Ground", new Vector3(79f, 0.17f, 0f), wpBastionGround, PatrolMode.PingPong, groundMask);

            CreateVillager(npcsParent, villagerFPrefab, "Villager_Bastion_Gossip", new Vector3(93.5f, 0.17f, 0f), WanderPattern.LookAround, 4.0f);

            // Escondite en planta baja (X: 90)
            CreateForestHideout(bastionRoot.transform, "Hideout_Bastion_Ground", 90f, 0f, 4.2f, 3.6f, cornCluster, stalkA, stalkB, stalkC);

            // 3. Plataformas de apoyo (Y: 2.2) y Plataforma Superior del Bastión (X: 86 a 98, Y: 4.0)
            CreateOneWayPlatform(bastionRoot.transform, "Bastion_Step_West", logASprite, new Vector2(83.5f, 2.3f), 1.1f, groundLayer);
            CreateOneWayPlatform(bastionRoot.transform, "Bastion_Step_East", logASprite, new Vector2(100.5f, 2.3f), 1.1f, groundLayer);

            // Plataforma sólida/one-way superior de X: 86 a X: 98 en Y: 4.0 (12u de largo)
            GameObject upperFloor = new GameObject("Bastion_Upper_Platform");
            upperFloor.transform.SetParent(bastionRoot.transform, false);
            upperFloor.layer = groundLayer;
            upperFloor.transform.position = new Vector3(92f, 4.0f, 0f);

            BoxCollider2D upperCol = upperFloor.AddComponent<BoxCollider2D>();
            upperCol.size = new Vector2(12.5f, 0.45f);
            upperCol.offset = new Vector2(0f, 0.05f);
            upperCol.usedByEffector = true;

            PlatformEffector2D upperEffector = upperFloor.AddComponent<PlatformEffector2D>();
            upperEffector.useOneWay = true;
            upperEffector.surfaceArc = 170f;

            for (int i = 0; i < 3; i++)
            {
                GameObject tile = new GameObject($"UpperDeck_Visual_{i}");
                tile.transform.SetParent(upperFloor.transform, false);
                tile.transform.localPosition = new Vector3(-4.2f + i * 4.2f, 0f, 0f);
                tile.transform.localScale = new Vector3(1.25f, 1.15f, 1f);
                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = logCSprite;
                sr.sortingLayerName = "Gameplay";
                sr.sortingOrder = 2;
            }

            // Escondite elevado en un extremo del piso superior para facilitar sigilo
            CreateForestHideout(bastionRoot.transform, "Hideout_Bastion_Upper", 87.2f, 4.2f, 3.0f, 2.8f, cornCluster, stalkA, stalkB, stalkC);

            // Recurso extra en la subida oriental
            InstantiatePickup(pickupsParent, potHypnoPrefab, "PotionPickup_Bastion_East", new Vector3(100.5f, 2.85f, 0f));
            InstantiatePickup(pickupsParent, heartPrefab, "HeartPickup_Bastion_BeforeGate", new Vector3(105f, 0.65f, 0f));

            // 4. Guardia Portador #2 (KeyCarrierGuard) en la plataforma superior (X: 86 a 98, Y: 4.0)
            Transform[] wpCarrier2 = CreateWaypoints(npcsParent, "Waypoints_KeyCarrier_2", new Vector2(87.2f, 4.28f), new Vector2(96.8f, 4.28f));
            GameObject guardCarrier2 = CreateGuard(npcsParent, guardPrefab, "Guard_KeyCarrier_2", new Vector3(92.5f, 4.28f, 0f), wpCarrier2, PatrolMode.PingPong, groundMask);
            if (guardCarrier2 != null)
            {
                KeyCarrierGuard kc2 = guardCarrier2.AddComponent<KeyCarrierGuard>();
                kc2.Configure(
                    Level3StateController.ForestGateKey2Id,
                    "Llave del Portón de la Cabaña",
                    new Color(1f, 0.88f, 0.15f, 0.95f),
                    autoCollect: false);
                EditorUtility.SetDirty(kc2);
            }
        }

        // =========================================================================
        // HELPERS DE CONSTRUCCIÓN
        // =========================================================================

        public static void CreateGroundSegment(Transform parent, string name, float startX, float length, int layer, Sprite sprite)
        {
            GameObject g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.layer = layer;
            g.transform.position = new Vector3(startX + length * 0.5f, -2f, 0f);

            BoxCollider2D col = g.AddComponent<BoxCollider2D>();
            col.size = new Vector2(length, 4.17f);
            col.offset = new Vector2(0f, 0.085f); // Cara superior exacta en Y = +0.17f

            if (sprite == null) return;
            float w = sprite.rect.width / sprite.pixelsPerUnit;
            int n = Mathf.CeilToInt(length / w);
            for (int i = 0; i < n; i++)
            {
                GameObject t = new GameObject("Tile_" + i);
                t.transform.SetParent(g.transform, false);
                t.transform.localPosition = new Vector3(-length * 0.5f + w * 0.5f + i * w, 1f, 0f);
                SpriteRenderer sr = t.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingLayerName = "Gameplay";
                sr.sortingOrder = 0;
            }
        }

        public static GameObject CreateOneWayPlatform(Transform parent, string name, Sprite sprite, Vector2 pos, float scaleX, int layer)
        {
            GameObject plat = new GameObject(name);
            plat.transform.SetParent(parent, false);
            plat.layer = layer;
            plat.transform.position = pos;
            plat.transform.localScale = new Vector3(scaleX, 1f, 1f);

            SpriteRenderer sr = plat.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Gameplay";
            sr.sortingOrder = 2;

            BoxCollider2D col = plat.AddComponent<BoxCollider2D>();
            float w = sprite != null ? (sprite.rect.width / sprite.pixelsPerUnit) : 3.5f;
            float targetTop = 0.27f;
            float colThickness = 0.35f;
            col.size = new Vector2(w * 0.92f, colThickness);
            col.offset = new Vector2(0f, targetTop - (colThickness * 0.5f));
            col.usedByEffector = true;

            PlatformEffector2D effector = plat.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.useOneWayGrouping = true;
            effector.surfaceArc = 170f;

            return plat;
        }

        public static GameObject CreateSolidWall(Transform parent, string name, Vector2 pos, Vector2 size, int layer, Sprite sprite)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.layer = layer;
            wall.transform.position = pos;

            BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
            col.size = size;

            if (sprite != null)
            {
                SpriteRenderer sr = wall.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = new Color(0.22f, 0.18f, 0.15f, 1f);
                sr.sortingLayerName = "Gameplay";
                sr.sortingOrder = 1;
            }

            return wall;
        }

        public static LockedGateInteractable CreateLockedGateWithCanopyBlocker(
            Transform parent,
            string objectName,
            Vector3 basePos,
            string gateId,
            string gateDisplayName,
            string requiredKeyId,
            string requiredKeyDisplayName,
            bool requireMasterPotion,
            bool advanceCabinPhase,
            Sprite gateSprite,
            int groundLayer)
        {
            GameObject gateGo = new GameObject(objectName);
            gateGo.transform.SetParent(parent, false);
            gateGo.layer = groundLayer;
            gateGo.transform.position = basePos;

            // Visual de barrotes que se desliza hacia arriba al abrirse
            GameObject barsVisual = new GameObject("Gate_Bars_Visual");
            barsVisual.transform.SetParent(gateGo.transform, false);
            barsVisual.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            barsVisual.transform.localScale = new Vector3(1.15f, 1.35f, 1f);

            SpriteRenderer sr = barsVisual.AddComponent<SpriteRenderer>();
            sr.sprite = gateSprite;
            sr.color = new Color(0.85f, 0.78f, 0.62f, 1f);
            sr.sortingLayerName = "Gameplay";
            sr.sortingOrder = 3;

            // Luz cálida sobre el portón para que destaque en el bosque
            GameObject gateLightGo = new GameObject("Gate_LanternLight2D");
            gateLightGo.transform.SetParent(gateGo.transform, false);
            gateLightGo.transform.localPosition = new Vector3(0f, 3.2f, 0f);
            Light2D l2d = gateLightGo.AddComponent<Light2D>();
            l2d.lightType = Light2D.LightType.Point;
            l2d.color = new Color(1f, 0.82f, 0.35f, 1f);
            l2d.intensity = 1.15f;
            l2d.pointLightOuterRadius = 3.8f;

            // Collider sólido del portón (se desactiva al abrirse)
            BoxCollider2D solidCol = gateGo.AddComponent<BoxCollider2D>();
            solidCol.isTrigger = false;
            solidCol.size = new Vector2(0.95f, 4.6f);
            solidCol.offset = new Vector2(0f, 2.3f);

            // Collider trigger más ancho para la interacción con E / Q
            BoxCollider2D triggerCol = gateGo.AddComponent<BoxCollider2D>();
            triggerCol.isTrigger = true;
            triggerCol.size = new Vector2(3.6f, 4.6f);
            triggerCol.offset = new Vector2(0f, 2.3f);

            LockedGateInteractable gate = gateGo.AddComponent<LockedGateInteractable>();
            gate.Configure(
                gateId,
                gateDisplayName,
                requiredKeyId,
                requiredKeyDisplayName,
                needsMasterPotion: requireMasterPotion,
                advanceCabinPhase: advanceCabinPhase,
                completesLevel: false,
                closed: gateSprite,
                open: null);

            var soGate = new SerializedObject(gate);
            soGate.FindProperty("solidBlockerCollider").objectReferenceValue = solidCol;
            soGate.FindProperty("interactionTriggerCollider").objectReferenceValue = triggerCol;
            soGate.FindProperty("gateRenderer").objectReferenceValue = sr;
            soGate.FindProperty("slidingBarsTransform").objectReferenceValue = barsVisual.transform;
            soGate.FindProperty("openSlideHeight").floatValue = 3.8f;
            soGate.ApplyModifiedPropertiesWithoutUndo();

            // Barrera superior antiescoba (Y: 4.6 a Y: 16.6) para impedir volar por encima del portón cerrado
            GameObject canopyBlocker = new GameObject("Canopy_AntiFlight_Arch");
            canopyBlocker.transform.SetParent(gateGo.transform, false);
            canopyBlocker.layer = groundLayer;
            canopyBlocker.transform.localPosition = new Vector3(0f, 10.6f, 0f);
            BoxCollider2D archCol = canopyBlocker.AddComponent<BoxCollider2D>();
            archCol.isTrigger = false;
            archCol.size = new Vector2(1.4f, 12.0f);

            return gate;
        }

        public static void CreateForestHideout(
            Transform parent,
            string name,
            float centerX,
            float baseY,
            float width,
            float height,
            Sprite clusterSprite,
            Sprite sA,
            Sprite sB,
            Sprite sC)
        {
            GameObject cf = new GameObject(name);
            cf.transform.SetParent(parent, false);
            cf.transform.position = new Vector3(centerX, baseY, 0f);

            cf.AddComponent<HideoutZone>();
            cf.AddComponent<CornFoliageFade>();

            BoxCollider2D col = cf.GetComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(width, height);
            col.offset = new Vector2(0f, height * 0.5f);

            if (clusterSprite != null)
            {
                GameObject cl = new GameObject("Cluster_Bg");
                cl.transform.SetParent(cf.transform, false);
                cl.transform.localPosition = new Vector3(0f, height * 0.55f, 0f);
                cl.transform.localScale = new Vector3(Mathf.Clamp(width / 6f, 0.55f, 1.2f), 0.75f, 1f);
                SpriteRenderer sr = cl.AddComponent<SpriteRenderer>();
                sr.sprite = clusterSprite;
                sr.sortingLayerName = "Gameplay";
                sr.sortingOrder = 0;
            }

            Sprite[] stalks = { sA, sB, sC };
            int stalkCount = Mathf.Max(2, Mathf.CeilToInt(width / 1.4f));
            for (int i = 0; i < stalkCount; i++)
            {
                if (stalks[i % stalks.Length] == null) continue;
                GameObject st = new GameObject($"Stalk_Fg_{i}");
                st.transform.SetParent(cf.transform, false);
                float lx = (-width * 0.5f) + (i + 0.5f) * (width / stalkCount);
                st.transform.localPosition = new Vector3(lx, height * 0.5f, 0f);
                st.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
                SpriteRenderer sr = st.AddComponent<SpriteRenderer>();
                sr.sprite = stalks[i % stalks.Length];
                sr.color = new Color(0.82f, 0.92f, 0.78f, 0.58f);
                sr.sortingLayerName = "Foreground";
                sr.sortingOrder = 5;
            }
        }

        public static void CreateCatOnlyTrigger(Transform parent, string name, Vector3 pos, string message)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.4f, 1.8f);

            CatOnlyPassage passage = go.AddComponent<CatOnlyPassage>();
            var so = new SerializedObject(passage);
            var msgProp = so.FindProperty("humanWarningMessage");
            if (msgProp != null) msgProp.stringValue = message;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void CreateWorldSign(Transform parent, string name, Vector2 pos, Sprite signSprite, string text)
        {
            GameObject signObj = new GameObject(name);
            signObj.transform.SetParent(parent, false);
            signObj.transform.position = pos;

            if (signSprite != null)
            {
                SpriteRenderer sr = signObj.AddComponent<SpriteRenderer>();
                sr.sprite = signSprite;
                sr.sortingLayerName = "Midground";
                sr.sortingOrder = 2;
            }

            GameObject textObj = new GameObject("Sign_Text");
            textObj.transform.SetParent(signObj.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 2.8f;
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
            so.FindProperty("detectionRadius").floatValue = 2.6f;
            so.FindProperty("messageDuration").floatValue = 3.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static GameObject CreateCheckpoint(Transform parent, GameObject cpPrefab, string name, Vector3 pos)
        {
            if (cpPrefab == null) return null;
            GameObject cp = (GameObject)PrefabUtility.InstantiatePrefab(cpPrefab, parent.gameObject.scene);
            cp.name = name;
            cp.transform.SetParent(parent, false);
            cp.transform.position = pos;
            return cp;
        }

        public static Transform[] CreateWaypoints(Transform parent, string containerName, Vector2 a, Vector2 b)
        {
            GameObject c = new GameObject(containerName);
            c.transform.SetParent(parent, false);

            GameObject wpA = new GameObject("WP_A");
            wpA.transform.SetParent(c.transform, false);
            wpA.transform.position = a;

            GameObject wpB = new GameObject("WP_B");
            wpB.transform.SetParent(c.transform, false);
            wpB.transform.position = b;

            return new[] { wpA.transform, wpB.transform };
        }

        public static GameObject CreateGuard(Transform parent, GameObject prefab, string name, Vector3 pos, Transform[] waypoints, PatrolMode mode, LayerMask groundMask)
        {
            if (prefab == null) return null;
            GameObject guard = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
            guard.name = name;
            guard.transform.SetParent(parent, false);
            guard.transform.position = pos;

            PatrolRoute pr = guard.GetComponent<PatrolRoute>();
            if (pr != null)
            {
                var so = new SerializedObject(pr);
                so.FindProperty("patrolMode").enumValueIndex = (int)mode;
                so.FindProperty("groundMask").intValue = groundMask.value;

                SerializedProperty wpProp = so.FindProperty("waypoints");
                wpProp.ClearArray();
                if (waypoints != null)
                {
                    for (int i = 0; i < waypoints.Length; i++)
                    {
                        wpProp.InsertArrayElementAtIndex(i);
                        wpProp.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i];
                    }
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            return guard;
        }

        public static GameObject CreateVillager(Transform parent, GameObject prefab, string name, Vector3 pos, WanderPattern pattern, float wanderRadius)
        {
            if (prefab == null) return null;
            GameObject v = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
            v.name = name;
            v.transform.SetParent(parent, false);
            v.transform.position = pos;

            VillagerAI ai = v.GetComponent<VillagerAI>();
            if (ai != null)
            {
                var so = new SerializedObject(ai);
                var radProp = so.FindProperty("maxWanderRadius");
                if (radProp != null) radProp.floatValue = wanderRadius;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            return v;
        }

        public static GameObject InstantiatePickup(Transform parent, GameObject prefab, string name, Vector3 pos)
        {
            if (prefab == null) return null;
            GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
            p.name = name;
            p.transform.SetParent(parent, false);
            p.transform.position = pos;
            return p;
        }

        private static void DecorateAct1Forest(Transform envParent)
        {
            Transform decoRoot = new GameObject("Act1_Cainos_Decor").transform;
            decoRoot.SetParent(envParent, false);

            Sprite[] cainosSprites = AssetDatabase.LoadAllAssetsAtPath(CainosAtlasPath).OfType<Sprite>().ToArray();
            if (cainosSprites.Length == 0) return;

            PlaceCainosSprite(decoRoot, cainosSprites, "Lamp_Start", "TX Village Props - Road Lamp Pillar", new Vector3(3.2f, 1.35f, 0f), 0.85f);
            PlaceCainosSprite(decoRoot, cainosSprites, "Pumpkins_Start", "TX Village Props - Pumpkin 01", new Vector3(8.0f, 0.55f, 0f), 1.0f);
            PlaceCainosSprite(decoRoot, cainosSprites, "Barrel_Gate1", "TX Village Props - Barrel", new Vector3(31.8f, 0.65f, 0f), 1.0f);
            PlaceCainosSprite(decoRoot, cainosSprites, "Gravestone_Ravine", "TX Village Props - Gravestone 03", new Vector3(50.8f, 0.65f, 0f), 1.0f);
            PlaceCainosSprite(decoRoot, cainosSprites, "Scarecrow_Bastion", "TX Village Props - Scarecrow", new Vector3(72.5f, 0.95f, 0f), 1.0f);
            PlaceCainosSprite(decoRoot, cainosSprites, "Crate_Bastion", "TX Village Props - Crate Large", new Vector3(99.0f, 0.72f, 0f), 0.9f);
        }

        private static void PlaceCainosSprite(Transform parent, Sprite[] atlas, string objName, string spriteName, Vector3 pos, float scale)
        {
            Sprite sp = atlas.FirstOrDefault(s => s != null && s.name == spriteName);
            if (sp == null) return;

            GameObject go = new GameObject(objName);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingLayerName = "Midground";
            sr.sortingOrder = 2;
        }

        private static GameObject CloneObject(GameObject src, Transform parent)
        {
            GameObject c;
            if (PrefabUtility.IsAnyPrefabInstanceRoot(src))
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(src);
                c = (GameObject)PrefabUtility.InstantiatePrefab(source, SceneManager.GetActiveScene());
                PrefabUtility.SetPropertyModifications(c, PrefabUtility.GetPropertyModifications(src));
            }
            else
            {
                c = Object.Instantiate(src);
            }
            c.name = src.name;
            if (parent != null) c.transform.SetParent(parent, true);
            return c;
        }

        private static bool IsUnderCopied(Transform t, HashSet<GameObject> copied)
        {
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                if (copied.Contains(p.gameObject)) return true;
            }
            return false;
        }

        private static void FixCrossSceneRefs(MonoBehaviour mb, Scene srcScene, GameObject witch)
        {
            if (mb == null) return;
            var so = new SerializedObject(mb);
            var it = so.GetIterator();
            bool changed = false;
            while (it.Next(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference || it.objectReferenceValue == null) continue;
                GameObject go = it.objectReferenceValue as GameObject ?? (it.objectReferenceValue as Component)?.gameObject;
                if (go == null || go.scene != srcScene) continue;
                Object replacement = null;
                if (PrefabUtility.GetOutermostPrefabInstanceRoot(go) is GameObject r && r.GetComponent<PlayerController>() != null)
                {
                    replacement = it.objectReferenceValue is GameObject
                        ? (Object)witch
                        : witch.GetComponent(it.objectReferenceValue.GetType());
                }
                it.objectReferenceValue = replacement;
                changed = true;
            }
            if (changed) so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RegisterLevel3InBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();

            // Eliminar entradas duplicadas previas de Level3 si existieran
            scenes.RemoveAll(s => s.path == Level3Path);

            int level2Index = scenes.FindIndex(s => s.path == Level2Path);
            EditorBuildSettingsScene level3Entry = new EditorBuildSettingsScene(Level3Path, true);

            if (level2Index >= 0)
            {
                scenes.Insert(level2Index + 1, level3Entry);
            }
            else
            {
                scenes.Add(level3Entry);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
