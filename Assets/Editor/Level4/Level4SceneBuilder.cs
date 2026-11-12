using System.Collections.Generic;
using System.Linq;
using Level2;
using Level3;
using Salem.Level4;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using L3 = Level3.Editor.Level3SceneBuilder;

namespace Salem.Editor
{
    /// <summary>
    /// Generador idempotente del Nivel 4 (Assets/Scenes/Level4.unity), con la misma infraestructura que
    /// Level3SceneBuilder (cámara, HUD, UI, managers y componentes de la bruja clonados de Level1).
    /// - Sección 1 (X -2..74): Panteón tormentoso, portón de hierro sellado y fachada con la ventana alta.
    /// - Sección 2 (X 99..151): Nave de la iglesia (alfombras, duelas flojas, bancas, vigas e Inquisidor).
    /// La ventana alta (CathedralHighWindowEntry) solo deja pasar a la forma de gato y activa la Sección 2.
    /// </summary>
    public static class Level4SceneBuilder
    {
        public const string Level4Path = "Assets/Scenes/Level4.unity";

        private const float G = 0.17f;          // superficie del suelo (igual que Level3)
        private const float TerraceY = 1.55f;   // desnivel del panteón
        private const float NaveX0 = 99f, NaveX1 = 151f, NaveCeil = 11f;
        private const float CoroX0 = 168f, CoroX1 = 226f, CoroCeil = 10f;       // piso 2: Balcón del Coro
        private const float TowerX0 = 238f, TowerX1 = 264f, TowerTop = 27f;     // piso 3: Mecanismo del Reloj
        private static readonly float[] TowerFloors = { 6.5f, 12.8f, 19.1f };   // superficie de cada losa de la torre
        private const float BelfryX0 = 276f, BelfryX1 = 310f, BelfryTop = 16f, BellLoftY = 7f; // piso 4: Campanario

        private const string Env = "Assets/Art/Environment/";
        private const string L4 = Env + "Level4/";
        private const string Garden = L4 + "Garden/";
        private const string DF = Env + "DarkForest/";
        private const string Cainos = "Assets/Cainos/Pixel Art Platformer - Village Props/Texture/TX Village Props.png";
        private const string UnlitMat = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        private static readonly Color Moss = new Color(0.62f, 0.78f, 0.68f, 1f);
        private static readonly Color NightStone = new Color(0.46f, 0.56f, 0.52f, 1f);

        private static int _ground;
        private static LayerMask _groundMask;
        private static Sprite[] _cainos;

        // Arte nuevo de Docs/Level4_Prompts_NanoBanana.md: si el archivo existe se usa; si no, el respaldo actual.
        // (La ventana y el portón reemplazan sus archivos actuales, así que no necesitan ruta propia.)
        private const string NewTombstones = Garden + "Panteon_Tombstones_Sheet.png";
        private const string NewMausoleum = Garden + "Panteon_Mausoleum.png";
        private const string NewCarpet = L4 + "Carpet_Velvet_Runner.png";
        private const string NewCreaky = L4 + "Creaky_Floorboard.png";
        private const string NewInquisitorSheet = "Assets/Art/Characters/Inquisitor/Inquisitor_Sheet.png";
        private const string NewFogStrip = L4 + "FX_GroundFog_Strip.png";
        private const string NewSky = Garden + "Storm_Parallax_Sky.png";
        private const string NewFar = Garden + "Storm_Parallax_Far.png";
        private const string NewNear = Garden + "Storm_Parallax_Near.png";
        private const string NewGround = Garden + "Withered_Ground_Tile.png";
        private const string NewRuin = Garden + "Panteon_Ruined_Crypt.png";
        private const string NewOrgan = L4 + "Choir_Pipe_Organ.png";
        private const string NewStalls = L4 + "Choir_Stalls.png";
        private const string NewBalustrade = L4 + "Choir_Balustrade_Tile.png";
        private const string NewClockFace = L4 + "Clock_Face_Backlit.png";
        private const string NewClockHands = L4 + "Clock_Hands_Sheet.png";
        private const string NewPendulum = L4 + "Clock_Pendulum.png";
        private const string NewChalicePoisoned = L4 + "Preacher_Chalice_Poisoned.png";
        private const string NewParrisCollapsed = L4 + "Father_Parris_Collapsed.png";
        private const string NewBroomFlight = L4 + "Witch_Broom_Flight.png";
        private const string NewShockwave = L4 + "FX_Bell_Shockwave.png";
        private const string CreditsBg = "Assets/Art/UI/Level4/Credits_Parchment_BG_AI.png";
        private const string PreacherArt     = "Assets/Art/Characters/Villagers/preacher/";
        private const string EvilPriestArt   = "Assets/Art/Characters/Level4/EvilPriest/";
        private const string WitchArt = "Assets/Art/Characters/Witch/";


        [MenuItem("Salem/Build Level 4 Scene")]
        public static void BuildScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Level4SceneBuilder] Sal del modo Play para reconstruir el Nivel 4.");
                return;
            }
            ProjectSetup.Configure();
            L3.EnsurePixelArtImport(DF);
            PrepareNewArt();
            _ground = LayerMask.NameToLayer("Ground");
            _groundMask = 1 << _ground;
            _cainos = AssetDatabase.LoadAllAssetsAtPath(Cainos).OfType<Sprite>().ToArray();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Scene l1 = EditorSceneManager.OpenScene(L3.Level1Path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            Transform managers = new GameObject("Managers").transform;
            Transform ui = new GameObject("UI").transform;
            Transform gameplay = new GameObject("Gameplay").transform;
            GameObject camCopy = null, witch;

            try
            {
                camCopy = CloneInfrastructure(l1, managers, ui);
                witch = SpawnWitch(scene, l1, gameplay);
                foreach (GameObject g in scene.GetRootGameObjects())
                    foreach (MonoBehaviour mb in g.GetComponentsInChildren<MonoBehaviour>(true))
                        L3.FixCrossSceneRefs(mb, l1, witch);
            }
            finally
            {
                EditorSceneManager.CloseScene(l1, true);
            }

            // Luz global tras cerrar Level1 (URP solo admite una Global Light 2D por capa)
            Light2D global = new GameObject("Global Light 2D").AddComponent<Light2D>();
            global.lightType = Light2D.LightType.Global;
            global.color = new Color(0.36f, 0.58f, 0.48f, 1f);
            global.intensity = 0.55f;

            SetupManagersAndUI(managers, ui, camCopy, witch);

            GameObject courtyard = new GameObject("Section1_Panteon");
            GameObject nave = new GameObject("Section2_Nave");
            GameObject coro = new GameObject("Section3_Coro");
            GameObject tower = new GameObject("Section4_Reloj");
            GameObject belfry = new GameObject("Section5_Campanario");
            BuildCourtyard(courtyard.transform, global, out CathedralHighWindowEntry window);
            Transform naveSpawn = BuildNave(nave.transform, out CathedralHighWindowEntry naveStairs);
            Transform coroSpawn = BuildCoro(coro.transform, out CathedralHighWindowEntry coroDoor);
            Transform towerSpawn = BuildTower(tower.transform, out CathedralHighWindowEntry belfryDoor);
            Transform belfrySpawn = BuildBelfry(belfry.transform, ui, witch, camCopy != null ? camCopy.GetComponent<Camera>() : null);
            WireDoor(naveStairs, nave, coro, coroSpawn, new Vector2(CoroX0, -1f), new Vector2(CoroX1, CoroCeil + 0.6f), global,
                new Color(0.52f, 0.43f, 0.36f, 1f), 0.45f, "[Q] Subir por la escalera de caracol al Balcón del Coro",
                "Balcón del Coro. Escóndete como gato [T] en la sillería y no caigas por el tramo roto de la galería.");
            WireDoor(coroDoor, coro, tower, towerSpawn, new Vector2(TowerX0, -1f), new Vector2(TowerX1, TowerTop + 0.6f), global,
                new Color(0.38f, 0.48f, 0.5f, 1f), 0.5f, "[Q] Entrar al mecanismo del reloj",
                "El mecanismo del reloj. Sube por las trampillas y cuida el ritmo de los péndulos y el vapor.");
            WireDoor(belfryDoor, tower, belfry, belfrySpawn, new Vector2(BelfryX0, -1f), new Vector2(BelfryX1, BelfryTop + 0.6f), global,
                new Color(0.5f, 0.4f, 0.42f, 1f), 0.45f, "[Q] Subir al campanario",
                "El campanario. Acércate al Padre Parris sin que te vean y vierte la Poción Maestra en su cáliz [E].");

            var so = new SerializedObject(window);
            so.FindProperty("interiorSpawnPoint").objectReferenceValue = naveSpawn;
            so.FindProperty("courtyardSection").objectReferenceValue = courtyard;
            so.FindProperty("churchInteriorSection").objectReferenceValue = nave;
            so.FindProperty("globalLight").objectReferenceValue = global;
            so.FindProperty("interiorCameraMin").vector2Value = new Vector2(NaveX0, -1f);
            so.FindProperty("interiorCameraMax").vector2Value = new Vector2(NaveX1, NaveCeil + 0.6f);
            so.ApplyModifiedPropertiesWithoutUndo();

            Object.FindFirstObjectByType<CaptureListener>()?.ConnectEnemies();
            Physics2D.SyncTransforms();
            nave.SetActive(false); // se activa al cruzar la ventana
            coro.SetActive(false); // se activan al subir por cada puerta
            tower.SetActive(false);
            belfry.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Level4Path);
            RegisterInBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Level4SceneBuilder] Escena {Level4Path} construida: Panteón (X -2..74) + Nave (X {NaveX0}..{NaveX1}) + Coro (X {CoroX0}..{CoroX1}) + Reloj (X {TowerX0}..{TowerX1}) + Campanario (X {BelfryX0}..{BelfryX1}).");
        }

        // =====================================================================
        // INFRAESTRUCTURA (mismo patrón que Level3SceneBuilder)
        // =====================================================================

        private static GameObject CloneInfrastructure(Scene l1, Transform managers, Transform ui)
        {
            var copied = new HashSet<GameObject>();
            GameObject camCopy = null;

            Camera cam1 = l1.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).FirstOrDefault(c => c.CompareTag("MainCamera"));
            if (cam1 != null)
            {
                camCopy = L3.CloneObject(cam1.gameObject, null);
                copied.Add(cam1.gameObject);
            }

            HUDController hud1 = l1.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<HUDController>(true)).FirstOrDefault();
            if (hud1 != null && copied.Add(hud1.gameObject)) L3.CloneObject(hud1.gameObject, ui);

            System.Type[] infra =
            {
                typeof(GameManager), typeof(AudioManager), typeof(TutorialManager), typeof(CheckpointManager),
                typeof(FrenzyController), typeof(CaptureListener), typeof(UnityEngine.EventSystems.EventSystem)
            };
            foreach (var t in infra)
            {
                foreach (Component c in l1.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren(t, true)))
                {
                    if (copied.Contains(c.gameObject) || L3.IsUnderCopied(c.transform, copied)) continue;
                    copied.Add(c.gameObject);
                    L3.CloneObject(c.gameObject, t == typeof(UnityEngine.EventSystems.EventSystem) ? ui : managers);
                }
            }
            return camCopy;
        }

        private static GameObject SpawnWitch(Scene scene, Scene l1, Transform gameplay)
        {
            GameObject witch = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Witch.prefab"), scene);
            witch.name = "Witch";
            witch.transform.SetParent(gameplay, false);
            witch.transform.position = new Vector3(2f, 1f, 0f);

            PlayerController pc1 = l1.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlayerController>(true)).FirstOrDefault();
            GameObject w1 = pc1 != null ? PrefabUtility.GetOutermostPrefabInstanceRoot(pc1.gameObject) : null;
            if (w1 != null)
            {
                foreach (var ac in PrefabUtility.GetAddedComponents(w1))
                {
                    Component src = ac.instanceComponent;
                    if (src == null) continue;
                    Component dst = witch.GetComponent(src.GetType()) ?? witch.AddComponent(src.GetType());
                    EditorUtility.CopySerialized(src, dst);
                }
            }
            L3.EnsureWitchLevel3Components(witch);
            if (witch.GetComponent<NoiseProducer>() == null) witch.AddComponent<NoiseProducer>();
            return witch;
        }

        private static void SetupManagersAndUI(Transform managers, Transform ui, GameObject camCopy, GameObject witch)
        {
            var boot = new GameObject("Level4_Bootstrap").AddComponent<Level4Bootstrap>();
            boot.transform.SetParent(managers, false);
            var soBoot = new SerializedObject(boot);
            soBoot.FindProperty("courtyardMusic").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/BGM_Level4_Cathedral_Stealth.wav");
            soBoot.ApplyModifiedPropertiesWithoutUndo();

            new GameObject("KeyInventory").AddComponent<KeyInventory>().transform.SetParent(managers, false);

            var prompt = new GameObject("InteractionPrompt_Canvas").AddComponent<InteractionPromptUI>();
            prompt.transform.SetParent(ui, false);
            prompt.EnsureUIHierarchy();

            GameObject cardGo = new GameObject("Level4ChapterCard_Canvas", typeof(RectTransform));
            cardGo.transform.SetParent(ui, false);
            cardGo.layer = LayerMask.NameToLayer("UI");
            var card = cardGo.AddComponent<Level3ChapterCard>();
            card.Configure(true, "NIVEL 4  ·  PARTE 1", "El Panteón de la Catedral",
                "La tormenta cubre tus pasos. El portón está sellado: vuela [F] hasta la ventana del vitral y entra como gato [T].");
            card.EnsureUIHierarchy();
            CanvasGroup cg = cardGo.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0f;

            ui.GetComponentInChildren<HUDController>(true)?.EnsureFourPotionSlots();

            GameManager gm = Object.FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.SleepCharges = Mathf.Max(gm.SleepCharges, 3);
                gm.BlindCharges = Mathf.Max(gm.BlindCharges, 1);
                gm.HypnotizeCharges = Mathf.Max(gm.HypnotizeCharges, 1);
                EditorUtility.SetDirty(gm);
            }

            if (camCopy == null) return;
            camCopy.transform.position = new Vector3(2f, 3f, -10f);
            CameraFollow follow = camCopy.GetComponent<CameraFollow>();
            if (follow == null) return;
            follow.SetTarget(witch.transform);
            follow.SetLimits(new Vector2(-2f, -1f), new Vector2(74.5f, 17f));
            EditorUtility.SetDirty(follow);
        }

        // =====================================================================
        // SECCIÓN 1: PANTEÓN TORMENTOSO
        // =====================================================================

        private static void BuildCourtyard(Transform root, Light2D global, out CathedralHighWindowEntry window)
        {
            Transform bg = L3.NewChild(root, "Background_Parallax");
            Transform env = L3.NewChild(root, "Environment");
            Transform play = L3.NewChild(root, "Gameplay");
            Transform npcs = L3.NewChild(root, "NPCs");
            Transform fx = L3.NewChild(root, "Storm_FX");

            BuildStormParallax(bg);

            // --- Suelo con desnivel ---
            WitheredGround(env, "Ground_Panteon_West", -6f, 23f, G, 0);
            WitheredGround(env, "Ground_Panteon_Terrace", 23f, 34f, TerraceY, 2);
            WitheredGround(env, "Ground_Panteon_East", 34f, 76f, G, 0);

            Sprite brick = Named(DF + "castle_env", "castle_env_26");
            Sprite darkBrick = Named(DF + "castle_env", "castle_env_17");
            BrickWall(env, "Terrace_RetainingWall", 23f, 34f, -0.2f, TerraceY - 0.45f, brick, 3, NightStone);
            L3.AddSolidBox(env, "Terrace_Step", new Vector2(21.8f, G), new Vector2(23f, 0.85f), _ground, new Color(0, 0, 0, 0));
            BrickWall(env, "Terrace_Step_Visual", 21.8f, 23f, -0.2f, 0.85f, brick, 3, NightStone);

            L3.CreateSolidWall(env, "Boundary_West", new Vector2(-1.5f, 9f), new Vector2(1f, 20f), _ground, null);
            L3.CreateSolidWall(env, "Boundary_East", new Vector2(75f, 9f), new Vector2(1f, 20f), _ground, null);
            L3.CreateSolidWall(env, "Boundary_StormCeiling", new Vector2(36f, 18.5f), new Vector2(80f, 1f), _ground, null);

            // --- Árboles muertos retorcidos ---
            Fit(env, "DeadTree_Gate", Named(DF + "env_trees", "env_trees_0"), -0.2f, G - 0.15f, 5.2f, "Midground", 1, Moss);
            Fit(env, "DeadTree_Crypts", Named(DF + "env_trees", "env_trees_1"), 19.5f, G - 0.15f, 6f, "Midground", 1, Moss);
            Fit(env, "DeadTree_Terrace", Named(DF + "env_trees", "env_trees_5"), 32.4f, TerraceY - 0.15f, 3.6f, "Midground", 1, Moss);
            Fit(env, "DeadTree_Rows", Named(DF + "env_trees", "env_trees_6"), 49.2f, G - 0.15f, 3.4f, "Midground", 1, Moss);
            Fit(env, "DeadTree_Forecourt", Named(DF + "env_trees", "env_trees_1"), 73.2f, G - 0.15f, 6.5f, "Midground", 1, Moss, true);
            WitheredPlants(env, 1.8f, 4.5f, 9.4f, 12f, 18.6f, 24.2f, 27f, 31f, 36.2f, 39.5f, 46f, 50.2f, 53f, 59.6f, 61f, 69f, 71.6f);

            // --- Inicio (X 0..9) ---
            L3.CreateCheckpoint(play, LoadPrefab("Assets/Prefabs/Checkpoint.prefab"), "Checkpoint_Panteon_Start", new Vector3(5.2f, G, 0f));
            L3.CreateWorldSign(play, "Sign_Controls", new Vector2(3.2f, 0.9f), Load(Env + "Env_Sign.png"),
                "Panteón de Salem.\n[F] Escoba  ·  [T] Gato  ·  [Shift] Sigilo\n[Q] Interactuar  ·  [E] Poción");
            L3.CreateMysticLantern(env, _cainos, "Lamp_Entrance", new Vector3(0.9f, 1.35f, 0f), new Color(0.55f, 1f, 0.75f, 1f));
            Graves(env, G, 6.6f, 7.7f, 8.9f);
            L3.InstantiatePickup(play, LoadPrefab("Assets/Prefabs/PotionPickup_Sleep.prefab"), "Potion_Sleep_Entrance", new Vector3(8.2f, 0.72f, 0f));

            // --- Seto escondite 1 + Mausoleo A (tejado escalable) + Guardia 1 ---
            Hedge(env, "Hideout_Hedge_1", 10.6f, G, 3.2f);
            Mausoleum(env, "Mausoleum_A", Opt(NewMausoleum) ?? Named(DF + "castle_env", "castle_env_7"), 15.5f, G, 2.4f);
            Fit(env, "Mausoleum_A_Column", Named(DF + "env_mon", "env_mon_6"), 12.7f, G - 0.05f, 2.9f, "Midground", 2, NightStone);
            Guard(npcs, "Guard_Panteon_1", 9f, 21f, G);
            L3.CreateMysticLantern(env, _cainos, "Lamp_Crypts", new Vector3(20.6f, 1.35f, 0f), new Color(0.55f, 1f, 0.75f, 1f));

            // --- Terraza: Mausoleo B, poción y Guardia 2 ---
            Mausoleum(env, "Mausoleum_B", Opt(NewMausoleum) ?? Named(DF + "env_mon", "env_mon_11"), 29f, TerraceY, 2.4f);
            L3.CreateMysticLantern(env, _cainos, "Lamp_Terrace", new Vector3(24.4f, 2.7f, 0f), new Color(1f, 0.82f, 0.45f, 1f));
            Graves(env, TerraceY, 25.6f, 31.6f, 33.2f);
            Guard(npcs, "Guard_Panteon_Terrace", 24.6f, 33.2f, TerraceY);
            L3.InstantiatePickup(play, LoadPrefab("Assets/Prefabs/PotionPickup_Blind.prefab"), "Potion_Blind_MausoleumB", new Vector3(29f, TerraceY + 3.1f, 0f));

            // --- Hileras de tumbas, Mausoleo C de dos pisos y Guardia 3 ---
            L3.CreateCheckpoint(play, LoadPrefab("Assets/Prefabs/Checkpoint.prefab"), "Checkpoint_Panteon_Rows", new Vector3(35.4f, G, 0f));
            Hedge(env, "Hideout_Hedge_2", 37.4f, G, 2.8f);
            Graves(env, G, 39.2f, 40.1f, 48.2f, 49.9f, 51f);
            IronRailing(env, 38.6f, 40.6f);
            IronRailing(env, 47.6f, 50.4f);
            // Con Panteon_Ruined_Crypt.png (prompt #11) el techo queda en 5.0 y el escalón a media altura (2.65)
            Sprite ruin = Opt(NewRuin);
            if (ruin != null) Fit(env, "Mausoleum_C_Ruin", ruin, 44f, G - 0.05f, 4.88f, "Midground", 2, MausoleumTint);
            else Fit(env, "Mausoleum_C_Ruin", Named(DF + "env_mon", "env_mon_0"), 44f, G - 0.05f, 5.1f, "Midground", 2, NightStone);
            L3.CreateOneWayLedge(env, "Mausoleum_C_Tier1", 41.3f, 46.7f, 2.65f, _ground, null, false);
            L3.CreateOneWayLedge(env, "Mausoleum_C_Roof", 41.9f, 46.1f, 5.0f, _ground, null, false);
            L3.InstantiatePickup(play, LoadPrefab("Assets/Prefabs/HeartPickup.prefab"), "Heart_MausoleumC_Roof", new Vector3(44f, 5.6f, 0f));
            Guard(npcs, "Guard_Panteon_Rows", 38f, 52.4f, G);
            L3.CreateMysticLantern(env, _cainos, "Lamp_Rows", new Vector3(41f, 1.35f, 0f), new Color(0.55f, 1f, 0.75f, 1f));
            Hedge(env, "Hideout_Hedge_3", 51.6f, G, 2.4f);

            // --- Portón monumental sellado ---
            BuildSealedGate(env, play);

            // --- Patio de la catedral, fachada y ventana alta ---
            window = BuildFacade(env, play, npcs);

            // --- Niebla, lluvia y relámpagos ---
            BuildFog(env, fx);
            BuildStorm(fx, global);
        }

        private static void BuildStormParallax(Transform bg)
        {
            // Cielo verde esmeralda (cubre el vuelo hasta Y 17, sin bloqueo vertical que lo deje atrás)
            L3.AddParallaxBand(bg, "Sky_StormyCemetery", Opt(NewSky) ?? Load(Garden + "Stormy_Cemetery_GreenNight_BG.png"), 7.2f, 1.3f, 0, 0.95f, new Color(0.9f, 1f, 0.94f, 1f));
            L3.AddParallaxLandmark(bg, "Cathedral_Far", Load(Env + "Level3/Catedral_Moonlit_Feathered.png"), 62f, 64f, 7.4f, 1.15f, 2, 0.86f, new Color(0.38f, 0.55f, 0.48f, 0.9f));
            Sprite far = Opt(NewFar), near = Opt(NewNear);
            if (far != null) L3.AddParallaxBand(bg, "Storm_Far", far, 2.9f, 0.8f, 4, 0.8f, new Color(0.7f, 0.82f, 0.76f, 1f));
            else L3.AddParallaxBand(bg, "Hills_Graveyard", Named(DF + "top_far_bgrnd", "top_far_bgrnd_1"), 3.6f, 1.7f, 4, 0.8f, new Color(0.11f, 0.22f, 0.18f, 0.95f));
            L3.AddParallaxBand(bg, "Hills_Ridge", Named(DF + "top_far_bgrnd", "top_far_bgrnd_0"), 1.9f, 2.2f, 5, 0.72f, new Color(0.09f, 0.18f, 0.15f, 1f));
            L3.AddParallaxBand(bg, "Trees_Far", Load(DF + "bgrd_tree1.png"), 2.7f, 1.45f, 6, 0.62f, new Color(0.10f, 0.20f, 0.16f, 1f));
            if (near != null) L3.AddParallaxBand(bg, "Storm_Near", near, 2.15f, 0.7f, 7, 0.46f, new Color(0.55f, 0.66f, 0.6f, 1f));
            else L3.AddParallaxBand(bg, "Trees_Mid", Load(DF + "bgrd_tree3.png"), 2.6f, 1.55f, 7, 0.46f, new Color(0.07f, 0.15f, 0.12f, 0.92f));
        }

        private static void BuildSealedGate(Transform env, Transform play)
        {
            const float x0 = 53.9f, x1 = 57.1f;
            Fit(env, "Panteon_Iron_Gate", Load(Garden + "Panteon_Iron_Gate.png"), (x0 + x1) * 0.5f, G - 0.05f, 4.4f, "Gameplay", 3, new Color(0.78f, 0.86f, 0.82f, 1f));
            BrickWall(env, "Cemetery_Wall_West", 50.6f, x0, -0.1f, 1.5f, Named(DF + "castle_env", "castle_env_26"), 1, NightStone);
            L3.AddSolidBox(env, "Gate_Blocker", new Vector2(54.8f, G), new Vector2(56.2f, G + 4.3f), _ground, new Color(0, 0, 0, 0));
            PointLight(env, "Gate_ChainLantern", new Vector3(55.5f, 4.9f, 0f), new Color(1f, 0.55f, 0.3f), 1.2f, 3.5f, true);
            L3.CreateWorldSign(play, "Sign_Gate_Sealed", new Vector2(52.6f, 0.9f), Load(Env + "Env_Sign.png"),
                "Portón sellado por la Inquisición.\nVuela [F] por encima y busca la ventana del vitral.");
        }

        private static CathedralHighWindowEntry BuildFacade(Transform env, Transform play, Transform npcs)
        {
            const float fx0 = 58f, fx1 = 74.6f;
            Transform facade = L3.NewChild(env, "Cathedral_Facade");

            // Muro de piedra de fondo con pilastras y cornisa (no sólido: la bruja camina delante)
            // Fondo bajo los bloques: sin él, las juntas dejan ver el cielo como una rejilla de líneas
            L3.AddFitted(facade, "Facade_Backing", Load(Env + "Placeholder_Square.png"), new Vector2(fx0, -0.3f), new Vector2(fx1, 16f), "Midground", -3, new Color(0.06f, 0.07f, 0.07f, 1f));
            BrickWall(facade, "Facade_Wall", fx0, fx1, -0.3f, 16f, Named(DF + "castle_env", "castle_env_17"), -2, new Color(0.42f, 0.5f, 0.48f, 1f), "Midground");
            foreach (float px in new[] { 58.4f, 62.6f, 70.4f, 74.2f })
                Fit(facade, "Facade_Pilaster", Named(DF + "env_mon", "env_mon_6"), px, G - 0.1f, 13f, "Midground", -1, NightStone);
            float half = (fx0 + fx1) * 0.5f;
            L3.AddFitted(facade, "Facade_Cornice_L", Named(DF + "decorative_obj", "decorative_obj_17"), new Vector2(fx0, 14.4f), new Vector2(half, 16.6f), "Midground", -1, NightStone);
            L3.AddFitted(facade, "Facade_Cornice_R", Named(DF + "decorative_obj", "decorative_obj_17"), new Vector2(half, 14.4f), new Vector2(fx1, 16.6f), "Midground", -1, NightStone);
            Fit(facade, "Facade_Window_Left", Named(DF + "env_mon", "env_mon_3"), 60.5f, 9.4f, 2.6f, "Midground", 0, Color.white);
            Fit(facade, "Facade_Window_Right", Named(DF + "env_mon", "env_mon_3"), 72.4f, 9.4f, 2.6f, "Midground", 0, Color.white);
            PointLight(facade, "Facade_Window_Left_Glow", new Vector3(60.5f, 10.6f, 0f), new Color(0.95f, 0.65f, 0.3f), 0.9f, 2.6f, true);
            PointLight(facade, "Facade_Window_Right_Glow", new Vector3(72.4f, 10.6f, 0f), new Color(0.5f, 1f, 0.7f), 0.8f, 2.6f, true);

            // Puerta principal atrancada
            Fit(facade, "Main_Door_Barred", Named(DF + "castle_env", "castle_env_7"), 66.5f, G - 0.05f, 3.9f, "Midground", 0, NightStone);
            PlaceCainos(facade, "Door_Barricade", "TX Village Props - Barricade", 66.5f, 1.15f);
            L3.CreateWorldSign(play, "Sign_Facade", new Vector2(60.6f, 0.9f), Load(Env + "Env_Sign.png"),
                "Las puertas están atrancadas.\nLa ventana alta tiene barrotes rotos: solo cabe un gato.");

            // Ventana alta con barrotes rotos + repisa
            const float wx = 66.5f, sill = 8.55f;
            Fit(facade, "Gothic_High_Window", Load(Garden + "Gothic_Cat_Entry_Window.png"), wx, sill - 0.05f, 3.5f, "Midground", 1, Color.white);
            L3.AddFitted(facade, "Window_Sill_Visual", Named(DF + "castle_env", "castle_env_22"), new Vector2(wx - 1.9f, sill - 0.55f), new Vector2(wx + 1.9f, sill + 0.06f), "Gameplay", 2, NightStone);
            L3.CreateOneWayLedge(facade, "Window_Sill", wx - 1.8f, wx + 1.8f, sill, _ground, null, false);
            PointLight(facade, "Window_WarmInterior_Light", new Vector3(wx, sill + 1.9f, 0f), new Color(1f, 0.72f, 0.36f), 1.7f, 4.8f, true);

            GameObject trigger = new GameObject("Gothic_High_Window_Trigger");
            trigger.transform.SetParent(facade, false);
            trigger.transform.position = new Vector3(wx, sill + 1.2f, 0f);
            BoxCollider2D col = trigger.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.6f, 2.4f);
            var entry = trigger.AddComponent<CathedralHighWindowEntry>();

            L3.CreateCheckpoint(play, LoadPrefab("Assets/Prefabs/Checkpoint.prefab"), "Checkpoint_Forecourt", new Vector3(58.6f, G, 0f));
            L3.CreateMysticLantern(env, _cainos, "Lamp_Forecourt", new Vector3(63.4f, 1.35f, 0f), new Color(1f, 0.8f, 0.45f, 1f));
            Guard(npcs, "Guard_Forecourt", 59.5f, 72.5f, G);
            return entry;
        }

        private static void BuildFog(Transform env, Transform fx)
        {
            Sprite strip = Opt(NewFogStrip);
            if (strip != null)
            {
                GameObject farBand = new GameObject("Fog_Far_Parallax");
                farBand.transform.SetParent(fx, false);
                FogBand(farBand.transform, strip, -14f, 80f, 2.4f, 3.2f, new Color(0.5f, 0.78f, 0.66f, 0.2f), "Background", 9, 2.2f);
                farBand.AddComponent<ParallaxLayer>().Setup(0.35f, false, true);
                FogBand(env, strip, -6f, 23.5f, G + 0.35f, 1.4f, new Color(0.62f, 0.86f, 0.74f, 0.22f), "Midground", 6, 1.2f);
                FogBand(env, strip, 22.5f, 34.5f, TerraceY + 0.35f, 1.4f, new Color(0.62f, 0.86f, 0.74f, 0.22f), "Midground", 6, 0.8f);
                FogBand(env, strip, 33.5f, 78f, G + 0.35f, 1.4f, new Color(0.62f, 0.86f, 0.74f, 0.22f), "Midground", 6, 1.2f);
                FogBand(env, strip, -6f, 78f, -0.1f, 1.1f, new Color(0.66f, 0.9f, 0.78f, 0.18f), "Foreground", 8, 2.0f);
                return;
            }
            Sprite fog = Load(Env + "Level3/FX_Level3_SoftFog.png");
            // Banco lejano con paralaje, banco a ras de suelo entre lápidas y velo frontal
            GameObject far = new GameObject("Fog_Far_Parallax");
            far.transform.SetParent(fx, false);
            for (float x = -12f; x < 70f; x += 8f)
                FogBlob(far.transform, fog, new Vector3(x, 2.2f, 0f), new Vector2(22f, 3.6f), new Color(0.5f, 0.78f, 0.66f, 0.16f + 0.06f * Mathf.Sin(x)), "Background", 9, 2.2f);
            far.AddComponent<ParallaxLayer>().Setup(0.35f, false, true);

            for (float x = -4f; x < 76f; x += 7f)
                FogBlob(env, fog, new Vector3(x, (x > 23f && x < 34f ? TerraceY : G) + 0.2f, 0f), new Vector2(12f, 1.8f), new Color(0.62f, 0.86f, 0.74f, 0.12f + 0.05f * Mathf.Cos(x)), "Midground", 6, 1.4f);
            for (float x = 2f; x < 76f; x += 13f)
                FogBlob(env, fog, new Vector3(x, -0.15f, 0f), new Vector2(14f, 1.4f), new Color(0.66f, 0.9f, 0.78f, 0.12f), "Foreground", 8, 2.6f);
        }

        private static void BuildStorm(Transform fx, Light2D global)
        {
            Material unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitMat);
            var storm = fx.gameObject.AddComponent<StormWeatherController>();

            // Lluvia (espacio de mundo: el emisor sigue a la cámara)
            GameObject rainGo = new GameObject("Rain_Particles");
            rainGo.transform.SetParent(fx, false);
            ParticleSystem ps = rainGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 1.1f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.05f);
            main.startColor = new Color(0.72f, 0.92f, 0.86f, 0.55f);
            main.maxParticles = 1200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;
            var emission = ps.emission;
            emission.rateOverTime = 520f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(36f, 0.5f, 0.1f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-3.2f, -2.4f);
            vel.y = new ParticleSystem.MinMaxCurve(-19f, -15f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var pr = rainGo.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Stretch;
            pr.velocityScale = 0.035f;
            pr.lengthScale = 1f;
            pr.sharedMaterial = unlit;
            pr.sortingLayerName = "Foreground";
            pr.sortingOrder = 20;

            // Destello de pantalla
            Sprite square = Load(Env + "Placeholder_Square.png");
            SpriteRenderer flash = L3.AddFitted(fx, "Lightning_Flash_Overlay", square, new Vector2(-30f, -20f), new Vector2(30f, 20f), "Foreground", 50, new Color(1f, 1f, 1f, 0f));

            // Rayo dentado en el cielo
            GameObject boltGo = new GameObject("Lightning_Bolt");
            boltGo.transform.SetParent(fx, false);
            LineRenderer line = boltGo.AddComponent<LineRenderer>();
            line.sharedMaterial = unlit;
            line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.22f), new Keyframe(1f, 0.05f));
            line.startColor = new Color(0.92f, 1f, 0.95f, 1f);
            line.endColor = new Color(0.7f, 1f, 0.85f, 0.6f);
            line.useWorldSpace = true;
            line.sortingLayerName = "Background";
            line.sortingOrder = 3;
            line.enabled = false;
            Light2D boltLight = PointLight(fx, "Lightning_Bolt_Light2D", new Vector3(30f, 8f, 0f), new Color(0.8f, 1f, 0.9f), 0f, 22f, false);

            var so = new SerializedObject(storm);
            so.FindProperty("lightningFlashOverlay").objectReferenceValue = flash;
            so.FindProperty("rainParticles").objectReferenceValue = ps;
            so.FindProperty("globalLight").objectReferenceValue = global;
            so.FindProperty("boltLine").objectReferenceValue = line;
            so.FindProperty("boltLight").objectReferenceValue = boltLight;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // =====================================================================
        // SECCIÓN 2: NAVE DE LA IGLESIA (PLANTA BAJA)
        // =====================================================================

        private static Transform BuildNave(Transform root, out CathedralHighWindowEntry stairs)
        {
            Transform env = L3.NewChild(root, "Environment");
            Transform play = L3.NewChild(root, "Gameplay");
            Transform npcs = L3.NewChild(root, "NPCs");
            Sprite square = Load(Env + "Placeholder_Square.png");

            // --- Caja arquitectónica: muro de fondo, suelo de duelas, techo y muros laterales ---
            L3.AddFitted(env, "Nave_BackWall_Backing", square, new Vector2(NaveX0, -1f), new Vector2(NaveX1, NaveCeil + 1.5f), "Background", -11, new Color(0.07f, 0.05f, 0.06f, 1f));
            BrickWall(env, "Nave_BackWall", NaveX0, NaveX1, -1f, NaveCeil + 1.5f, Named(DF + "castle_env", "castle_env_17"), -10, new Color(0.40f, 0.36f, 0.38f, 1f), "Background");
            L3.AddFitted(env, "Nave_Ceiling_Shadow", square, new Vector2(NaveX0, NaveCeil - 0.4f), new Vector2(NaveX1, NaveCeil + 3f), "Midground", 8, new Color(0.06f, 0.05f, 0.06f, 1f));
            for (float x = NaveX0; x < NaveX1; x += 3.06f)
                L3.AddFitted(env, "Ceiling_Beam", Named(DF + "decorative_obj", "decorative_obj_1"), new Vector2(x, NaveCeil - 0.65f), new Vector2(x + 3.06f, NaveCeil - 0.3f), "Midground", 9, new Color(0.55f, 0.45f, 0.4f));

            GameObject floor = new GameObject("Nave_Floor");
            floor.transform.SetParent(env, false);
            floor.layer = _ground;
            floor.transform.position = new Vector3((NaveX0 + NaveX1) * 0.5f, G - 2.085f, 0f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(NaveX1 - NaveX0, 4.17f);
            Sprite planks = Load(Env + "Level2/Interior_Floor_Planks.png");
            for (float x = NaveX0; x < NaveX1; x += 4f)
                L3.AddFitted(env, "Floor_Planks", planks, new Vector2(x, G - 1f), new Vector2(x + 4f, G), "Midground", 0, new Color(0.62f, 0.5f, 0.42f));
            L3.AddFitted(env, "Floor_Under", square, new Vector2(NaveX0, -4f), new Vector2(NaveX1, G - 0.95f), "Midground", 0, new Color(0.08f, 0.06f, 0.05f));

            L3.CreateSolidWall(env, "Nave_WestWall", new Vector2(NaveX0 - 0.5f, 6f), new Vector2(1f, 14f), _ground, null);
            L3.CreateSolidWall(env, "Nave_EastWall", new Vector2(NaveX1 + 0.5f, 6f), new Vector2(1f, 14f), _ground, null);
            L3.CreateSolidWall(env, "Nave_Ceiling", new Vector2((NaveX0 + NaveX1) * 0.5f, NaveCeil + 0.5f), new Vector2(NaveX1 - NaveX0, 1f), _ground, null);

            // --- Arcadas góticas, columnas, vitrales con haces de luz ---
            Sprite arch = Named(DF + "castle_env", "castle_env_9");
            Sprite column = Named(DF + "env_mon", "env_mon_6");
            Sprite vitral = Named(DF + "env_mon", "env_mon_3");
            Sprite fog = Load(Env + "Level3/FX_Level3_SoftFog.png");
            float[] bays = { 105.5f, 118f, 130.5f, 143f };
            for (int i = 0; i < bays.Length; i++)
            {
                float bx = bays[i];
                Fit(env, $"Arcade_Arch_{i}", arch, bx, G - 0.05f, 9.2f, "Background", -6, new Color(0.5f, 0.44f, 0.46f));
                Fit(env, $"Vitral_{i}", vitral, bx, 6.2f, 3.2f, "Background", -5, Color.white);
                Color c = i % 2 == 0 ? new Color(1f, 0.72f, 0.34f) : new Color(0.45f, 1f, 0.62f);
                PointLight(env, $"Vitral_{i}_Light2D", new Vector3(bx, 7.6f, 0f), c, 1.1f, 3.4f, true);
                GameObject beam = new GameObject($"Vitral_{i}_LightShaft");
                beam.transform.SetParent(env, false);
                beam.transform.position = new Vector3(bx + 0.9f, 3.4f, 0f);
                beam.transform.rotation = Quaternion.Euler(0f, 0f, 12f);
                beam.transform.localScale = new Vector3(1.6f, 7.5f, 1f);
                SpriteRenderer bsr = beam.AddComponent<SpriteRenderer>();
                bsr.sprite = fog;
                bsr.color = new Color(c.r, c.g, c.b, 0.13f);
                bsr.sortingLayerName = "Midground";
                bsr.sortingOrder = 7;
            }
            foreach (float cx in new[] { 99.8f, 111.75f, 124.25f, 136.75f, 150.2f })
                Fit(env, "Nave_Column", column, cx, G - 0.1f, NaveCeil - 0.2f, "Midground", 1, new Color(0.58f, 0.52f, 0.52f));

            // --- Entrada desde la ventana: alféizar interior y spawn ---
            Fit(env, "Interior_Window", Load(Garden + "Gothic_Cat_Entry_Window.png"), 103f, 6.15f, 3.3f, "Background", -4, new Color(0.85f, 0.85f, 0.9f));
            L3.AddFitted(env, "Interior_Sill_Visual", Named(DF + "castle_env", "castle_env_22"), new Vector2(101f, 5.6f), new Vector2(105f, 6.22f), "Gameplay", 2, new Color(0.55f, 0.5f, 0.5f));
            L3.CreateOneWayLedge(env, "Interior_Sill", 101.1f, 104.9f, 6.15f, _ground, null, false);
            Transform spawn = L3.NewChild(play, "NaveSpawnPoint");
            spawn.position = new Vector3(103f, 6.8f, 0f);
            L3.CreateCheckpoint(play, LoadPrefab("Assets/Prefabs/Checkpoint.prefab"), "Checkpoint_Nave_Entry", new Vector3(101.2f, G, 0f));

            // --- Alfombras silenciosas y duelas flojas ---
            AudioClip creak = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_FloorCreak.wav");
            float[][] carpets = { new[] { 100f, 106.5f }, new[] { 108f, 115f }, new[] { 116.5f, 123f }, new[] { 124.5f, 131f }, new[] { 132.5f, 138f } };
            for (int i = 0; i < carpets.Length; i++) Carpet(env, $"Silent_Carpet_{i}", carpets[i][0], carpets[i][1], square);
            float[] creaks = { 106.5f, 115f, 123f, 131f };
            for (int i = 0; i < creaks.Length; i++) CreakyTile(env, $"Creaky_Floorboard_{i}", creaks[i], creaks[i] + 1.5f, planks, creak);

            // --- Bancas de roble (saltables y escondite para el gato) ---
            Sprite pew = Load(L4 + "Pew_Oak_Bench_AI.png");
            foreach (float px in new[] { 111.5f, 119.75f, 127.75f, 135.25f })
            {
                Fit(env, "Pew_Oak_Bench", pew, px, G - 0.05f, 1.45f, "Foreground", 2, new Color(0.82f, 0.74f, 0.68f));
                L3.CreateOneWayLedge(env, "Pew_Top", px - 1.4f, px + 1.4f, 1.35f, _ground, null, false);
                L3.CreateForestHideout(env, "Pew_Hideout", px, G, 2.6f, 1.3f, null, null, null, null);
            }

            // --- Vigas superiores (ruta de sigilo) y candelabros colgantes ---
            Sprite rafter = Load(Env + "Level2/Interior_Rafter_Loft.png");
            foreach (float rx in new[] { 106f, 112.5f, 119f, 125.5f, 132f, 138.5f })
            {
                L3.AddFitted(env, "Rafter_Visual", rafter, new Vector2(rx, 3.45f), new Vector2(rx + 4f, 4.05f), "Gameplay", 2, new Color(0.7f, 0.58f, 0.5f));
                L3.CreateOneWayLedge(env, "Rafter_Loft", rx, rx + 4f, 4.0f, _ground, null, false);
            }
            Sprite chandelier = Load(Env + "Level2/Interior_Chandelier.png");
            Sprite chain = _cainos.FirstOrDefault(s => s.name == "TX Village Props - Chain 01");
            foreach (float cx in new[] { 111.25f, 123.75f, 136.25f })
            {
                if (chain != null) L3.AddFitted(env, "Chandelier_Chain", chain, new Vector2(cx - 0.08f, 8.6f), new Vector2(cx + 0.08f, NaveCeil - 0.4f), "Midground", 4, new Color(0.6f, 0.55f, 0.5f));
                Fit(env, "Chandelier", chandelier, cx, 7.5f, 1.3f, "Midground", 5, Color.white);
                PointLight(env, "Chandelier_Light2D", new Vector3(cx, 8f, 0f), new Color(1f, 0.74f, 0.4f), 1.5f, 6.2f, true);
            }

            // --- Altar mayor y candelabros ---
            Fit(env, "Church_Altar_Pulpit", Load(L4 + "Church_Altar_Pulpit_AI.png"), 141.6f, G - 0.1f, 4.1f, "Midground", 2, new Color(0.85f, 0.8f, 0.78f));
            PointLight(env, "Altar_Candles_Light2D", new Vector3(140.4f, 3.6f, 0f), new Color(1f, 0.7f, 0.35f), 1.6f, 4.5f, true);
            foreach (float kx in new[] { 103.6f, 138.2f, 145.2f })
            {
                Fit(env, "Candelabra", Named(DF + "decorative_obj", "decorative_obj_3"), kx, G - 0.05f, 1.6f, "Midground", 3, new Color(0.9f, 0.8f, 0.6f));
                PointLight(env, "Candelabra_Light2D", new Vector3(kx, 1.75f, 0f), new Color(1f, 0.72f, 0.38f), 0.9f, 2.2f, true);
            }

            // --- Salida: escalera de caracol a la torre ---
            Fit(env, "Tower_Stairwell_Door", Named(DF + "castle_env", "castle_env_7"), 148.3f, G - 0.05f, 3.6f, "Midground", 1, new Color(0.55f, 0.5f, 0.5f));
            L3.AddFitted(env, "Tower_Stair_Railing", Named(DF + "decorative_obj", "decorative_obj_18"), new Vector2(145.6f, G), new Vector2(150f, 2.6f), "Gameplay", 3, new Color(0.7f, 0.6f, 0.55f));
            L3.CreateWorldSign(play, "Sign_Tower_Stairs", new Vector2(146.4f, 0.9f), Load(Env + "Env_Sign.png"),
                "Escalera de caracol al Balcón del Coro.\n[Q] para subir");
            stairs = DoorTrigger(play, "Tower_Stairs_Entry", new Vector2(148.3f, G + 1.3f), new Vector2(2.2f, 2.6f));

            // --- Pociones de apoyo ---
            L3.InstantiatePickup(play, LoadPrefab("Assets/Prefabs/PotionPickup_Sleep.prefab"), "Potion_Sleep_Rafter", new Vector3(121f, 4.55f, 0f));
            L3.InstantiatePickup(play, LoadPrefab("Assets/Prefabs/PotionPickup_Hypnotize.prefab"), "Potion_Hypno_Altar", new Vector3(140.5f, 4.55f, 0f));

            // --- Miniboss: el Inquisidor de Salem ---
            BuildInquisitor(npcs, 131f, 112.5f, 137.5f);
            return spawn;
        }

        private static void BuildInquisitor(Transform parent, float x, float wpA, float wpB)
        {
            GameObject inq = new GameObject("Inquisitor_Miniboss");
            inq.transform.SetParent(parent, false);
            inq.transform.position = new Vector3(x, G, 0f);
            int enemy = LayerMask.NameToLayer("Enemy");
            if (enemy >= 0) inq.layer = enemy;

            Rigidbody2D rb = inq.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.gravityScale = 3f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            CapsuleCollider2D col = inq.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(1.0f, 2.7f);
            col.offset = new Vector2(0f, 1.37f);

            // Hoja 6x5 (Docs/Level4_Prompts_NanoBanana.md #7); sin ella, la pose única actual
            Sprite[] idleF = Frames(NewInquisitorSheet, 0, 1), walkF = Frames(NewInquisitorSheet, 6, 7, 8, 9, 10, 11),
                listenF = Frames(NewInquisitorSheet, 12, 13, 14), chargeF = Frames(NewInquisitorSheet, 18, 19, 20, 21, 22, 23),
                stunF = Frames(NewInquisitorSheet, 24, 25, 26, 27);
            bool sheet = idleF != null && idleF.Length > 0;
            Sprite pose = sheet ? idleF[0] : Load("Assets/Art/Characters/Inquisitor/Inquisitor_Boss_Stylized.png");
            SpriteRenderer sr = Fit(inq.transform, "Visual", pose, x, G, 3.0f, "Gameplay", 6, Color.white);
            sr.flipX = true; // empieza mirando a la izquierda (hacia la bruja)

            GameObject lantern = new GameObject("LanternOrigin");
            lantern.transform.SetParent(inq.transform, false);
            lantern.transform.localPosition = new Vector3(-0.75f, 1.55f, 0f);
            lantern.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Light2D cone = lantern.AddComponent<Light2D>();
            cone.lightType = Light2D.LightType.Point;
            cone.color = new Color(1f, 0.78f, 0.42f);
            cone.intensity = 1.6f;
            cone.pointLightInnerRadius = 0.3f;
            cone.pointLightOuterRadius = 6.5f;
            cone.pointLightInnerAngle = 40f;
            cone.pointLightOuterAngle = 80f;
            PointLight(lantern.transform, "Lantern_Glow", lantern.transform.position, new Color(1f, 0.7f, 0.35f), 1.2f, 1.6f, true);

            var alert = L3.AddWorldText(inq.transform, "Alert_Icon", new Vector3(0f, 3.5f, 0f), "!", 9f, new Color(1f, 0.25f, 0.2f), "Foreground", 30, new Vector2(1.5f, 1.5f));

            Transform[] wps = L3.CreateWaypoints(parent, "Waypoints_Inquisitor", new Vector2(wpA, G), new Vector2(wpB, G));
            var ai = inq.AddComponent<InquisitorGuardAI>();
            var so = new SerializedObject(ai);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("lanternOrigin").objectReferenceValue = lantern.transform;
            so.FindProperty("alertIcon").objectReferenceValue = alert.gameObject;
            so.FindProperty("alertSfx").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_InquisitorAlert.wav");
            so.FindProperty("stompSfx").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_InquisitorStomp.wav");
            if (sheet)
            {
                SetFrames(so, "idleFrames", idleF); SetFrames(so, "walkFrames", walkF); SetFrames(so, "listenFrames", listenF);
                SetFrames(so, "chargeFrames", chargeF); SetFrames(so, "stunnedFrames", stunF);
            }
            SerializedProperty wpProp = so.FindProperty("patrolWaypoints");
            wpProp.arraySize = wps.Length;
            for (int i = 0; i < wps.Length; i++) wpProp.GetArrayElementAtIndex(i).objectReferenceValue = wps[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // =====================================================================
        // PISO 2: BALCÓN DEL CORO (galería de sigilo con guardias, sillería y tramo roto)
        // =====================================================================

        private static Transform BuildCoro(Transform root, out CathedralHighWindowEntry exit)
        {
            Transform env = L3.NewChild(root, "Environment");
            Transform play = L3.NewChild(root, "Gameplay");
            Transform npcs = L3.NewChild(root, "NPCs");
            Sprite square = Load(Env + "Placeholder_Square.png");
            Sprite planks = Load(Env + "Level2/Interior_Floor_Planks.png");
            Sprite rafter = Load(Env + "Level2/Interior_Rafter_Loft.png");
            const float gapX0 = 186f, gapX1 = 190f, loftY = 5f;

            // --- Caja: muro lejano (la galería mira a la nave), techo con vigas y muros laterales ---
            L3.AddFitted(env, "Coro_BackWall_Backing", square, new Vector2(CoroX0, -4f), new Vector2(CoroX1, CoroCeil + 1.5f), "Background", -11, new Color(0.07f, 0.05f, 0.06f, 1f));
            BrickWall(env, "Coro_BackWall", CoroX0, CoroX1, -1f, CoroCeil + 1.5f, Named(DF + "castle_env", "castle_env_17"), -10, new Color(0.40f, 0.35f, 0.34f, 1f), "Background");
            L3.AddFitted(env, "Coro_Ceiling_Shadow", square, new Vector2(CoroX0, CoroCeil - 0.4f), new Vector2(CoroX1, CoroCeil + 3f), "Midground", 8, new Color(0.06f, 0.05f, 0.06f, 1f));
            for (float x = CoroX0; x < CoroX1; x += 3.06f)
                L3.AddFitted(env, "Ceiling_Beam", Named(DF + "decorative_obj", "decorative_obj_1"), new Vector2(x, CoroCeil - 0.65f), new Vector2(x + 3.06f, CoroCeil - 0.3f), "Midground", 9, new Color(0.55f, 0.45f, 0.4f));
            L3.CreateSolidWall(env, "Coro_WestWall", new Vector2(CoroX0 - 0.5f, 5f), new Vector2(1f, 14f), _ground, null);
            L3.CreateSolidWall(env, "Coro_EastWall", new Vector2(CoroX1 + 0.5f, 5f), new Vector2(1f, 14f), _ground, null);
            L3.CreateSolidWall(env, "Coro_Ceiling", new Vector2((CoroX0 + CoroX1) * 0.5f, CoroCeil + 0.5f), new Vector2(CoroX1 - CoroX0, 1f), _ground, null);

            Sprite arch = Named(DF + "castle_env", "castle_env_9");
            Sprite vitral = Named(DF + "env_mon", "env_mon_3");
            foreach (float bx in new[] { 176f, 194f, 212f })
            {
                Fit(env, "Coro_Arch", arch, bx, G - 0.05f, 8.6f, "Background", -6, new Color(0.5f, 0.44f, 0.46f));
                Fit(env, "Coro_Vitral", vitral, bx, 5.6f, 2.8f, "Background", -5, Color.white);
                PointLight(env, "Coro_Vitral_Light2D", new Vector3(bx, 6.8f, 0f), new Color(0.55f, 0.8f, 1f), 0.9f, 3f, true);
            }
            foreach (float cx in new[] { 168.8f, 185.2f, 203f, 225.2f })
                Fit(env, "Coro_Column", Named(DF + "env_mon", "env_mon_6"), cx, G - 0.1f, CoroCeil - 0.2f, "Midground", 1, new Color(0.58f, 0.52f, 0.52f));

            // --- Piso de la galería con un tramo hundido (caer = corazón menos y vuelta al checkpoint) ---
            PlankFloor(env, "Coro_Floor_West", CoroX0, gapX0, planks, square);
            PlankFloor(env, "Coro_Floor_East", gapX1, CoroX1, planks, square);
            foreach (var (x, rot) in new[] { (gapX0 + 0.35f, -28f), (gapX1 - 0.35f, 22f) })
            {
                SpriteRenderer board = L3.AddFitted(env, "Broken_Plank", planks, new Vector2(x - 0.6f, G - 0.9f), new Vector2(x + 0.6f, G - 0.1f), "Midground", 1, new Color(0.5f, 0.4f, 0.34f));
                board.transform.rotation = Quaternion.Euler(0f, 0f, rot);
            }
            GameObject pit = new GameObject("Coro_Gap_KillZone");
            pit.transform.SetParent(play, false);
            pit.transform.position = new Vector3((gapX0 + gapX1) * 0.5f, -3.5f, 0f);
            pit.AddComponent<KillZone>();
            pit.GetComponent<BoxCollider2D>().size = new Vector2(gapX1 - gapX0 + 0.6f, 1.5f);

            // Candelabro colgante sobre el hueco: se puede pisar
            Sprite chandelier = Load(Env + "Level2/Interior_Chandelier.png");
            Sprite chain = _cainos.FirstOrDefault(s => s.name == "TX Village Props - Chain 01");
            if (chain != null) L3.AddFitted(env, "Gap_Chandelier_Chain", chain, new Vector2(187.92f, 2.4f), new Vector2(188.08f, CoroCeil - 0.4f), "Midground", 4, new Color(0.6f, 0.55f, 0.5f));
            Fit(env, "Gap_Chandelier", chandelier, 188f, 0.95f, 1.5f, "Gameplay", 3, Color.white);
            L3.CreateOneWayLedge(env, "Gap_Chandelier_Top", 187.1f, 188.9f, 1.85f, _ground, null, false);
            PointLight(env, "Gap_Chandelier_Light2D", new Vector3(188f, 1.6f, 0f), new Color(1f, 0.74f, 0.4f), 1.4f, 4.5f, true);

            // --- Balaustrada de la galería (detrás de la bruja: da a la nave) ---
            Balustrade(env, CoroX0, gapX0 - 0.2f);
            Balustrade(env, gapX1 + 0.2f, CoroX1);

            // --- Llegada por la escalera de caracol ---
            Fit(env, "Coro_Stairwell_Door", Named(DF + "castle_env", "castle_env_7"), 170.3f, G - 0.05f, 3.4f, "Background", -3, new Color(0.55f, 0.5f, 0.5f));
            Transform spawn = L3.NewChild(play, "CoroSpawnPoint");
            spawn.position = new Vector3(171.6f, G + 0.6f, 0f);
            L3.CreateCheckpoint(play, LoadPrefab("Assets/Prefabs/Checkpoint.prefab"), "Checkpoint_Coro_Entry", new Vector3(172.6f, G, 0f));
            L3.CreateCheckpoint(play, LoadPrefab("Assets/Prefabs/Checkpoint.prefab"), "Checkpoint_Coro_Gap", new Vector3(190.8f, G, 0f));

            // --- Sillería del coro: escondite para el gato y escalón para saltar ---
            Sprite stalls = Opt(NewStalls);
            Sprite pew = Load(L4 + "Pew_Oak_Bench_AI.png");
            foreach (float sx in new[] { 176.5f, 181.5f, 196f })
            {
                if (stalls != null) Fit(env, "Choir_Stalls", stalls, sx, G - 0.05f, 1.7f, "Foreground", 2, Color.white);
                else Fit(env, "Choir_Stalls", pew, sx, G - 0.05f, 1.45f, "Foreground", 2, new Color(0.7f, 0.58f, 0.52f));
                L3.CreateOneWayLedge(env, "Stalls_Top", sx - 1.3f, sx + 1.3f, stalls != null ? 1.75f : 1.35f, _ground, null, false);
                L3.CreateForestHideout(env, "Stalls_Hideout", sx, G, 2.6f, 1.3f, null, null, null, null);
            }

            // --- Estantes de himnarios, candelabros y arañas ---
            Sprite shelf = Load(Env + "Level2/Interior_Bookshelf.png");
            foreach (float hx in new[] { 179f, 199.5f })
                Fit(env, "Hymnal_Shelf", shelf, hx, G - 0.05f, 2.4f, "Background", -2, new Color(0.62f, 0.52f, 0.46f));
            foreach (float kx in new[] { 173.8f, 191.8f, 202f })
            {
                Fit(env, "Coro_Candelabra", Named(DF + "decorative_obj", "decorative_obj_3"), kx, G - 0.05f, 1.6f, "Midground", 3, new Color(0.9f, 0.8f, 0.6f));
                PointLight(env, "Coro_Candelabra_Light2D", new Vector3(kx, 1.75f, 0f), new Color(1f, 0.72f, 0.38f), 0.9f, 2.4f, true);
            }
            foreach (float cx in new[] { 179f, 197.5f })
            {
                if (chain != null) L3.AddFitted(env, "Chandelier_Chain", chain, new Vector2(cx - 0.08f, 7.8f), new Vector2(cx + 0.08f, CoroCeil - 0.4f), "Midground", 4, new Color(0.6f, 0.55f, 0.5f));
                Fit(env, "Chandelier", chandelier, cx, 6.7f, 1.3f, "Midground", 5, Color.white);
                PointLight(env, "Chandelier_Light2D", new Vector3(cx, 7.2f, 0f), new Color(1f, 0.74f, 0.4f), 1.4f, 6f, true);
            }

            // --- Guardias de la galería ---
            Guard(npcs, "Guard_Coro_1", 174f, 184.6f, G);
            Guard(npcs, "Guard_Coro_2", 193.5f, 203f, G);

            // --- Órgano de tubos y desván con la puerta al mecanismo del reloj ---
            float organTop = Organ(env, 208f, square);
            L3.CreateOneWayLedge(env, "Organ_Top", 205.7f, 210.3f, organTop, _ground, null, false);
            PointLight(env, "Organ_Candles_Light2D", new Vector3(206.6f, 1.6f, 0f), new Color(1f, 0.72f, 0.38f), 1.2f, 3.2f, true);
            L3.InstantiatePickup(play, LoadPrefab("Assets/Prefabs/PotionPickup_Sleep.prefab"), "Potion_Sleep_Organ", new Vector3(208f, organTop + 0.4f, 0f));

            for (float x = 211.6f; x < CoroX1; x += 4f)
                L3.AddFitted(env, "Loft_Rafter", rafter, new Vector2(x, loftY - 0.55f), new Vector2(Mathf.Min(x + 4f, CoroX1), loftY + 0.05f), "Gameplay", 2, new Color(0.7f, 0.58f, 0.5f));
            GameObject loft = L3.CreateOneWayLedge(env, "Coro_Loft", 211.6f, CoroX1, loftY, _ground, null, false);
            Ladder(play, "Ladder_Coro_Loft", 213.2f, G, loftY, loft.GetComponent<Collider2D>());

            Fit(env, "Clock_Tower_Door", Named(DF + "castle_env", "castle_env_7"), 222.6f, loftY - 0.05f, 3f, "Midground", 1, new Color(0.55f, 0.5f, 0.5f));
            PointLight(env, "Clock_Door_Light2D", new Vector3(222.6f, loftY + 2f, 0f), new Color(0.6f, 0.85f, 0.8f), 1f, 2.6f, true);
            L3.CreateWorldSign(play, "Sign_Clock_Tower", new Vector2(219.6f, loftY + 0.75f), Load(Env + "Env_Sign.png"),
                "Mecanismo del reloj de la torre.\n[Q] para entrar");
            exit = DoorTrigger(play, "Clock_Tower_Entry", new Vector2(222.6f, loftY + 1.2f), new Vector2(1.8f, 2.4f));
            return spawn;
        }

        // =====================================================================
        // PISO 3: MECANISMO DEL RELOJ (subida por tramos con trampillas alternas)
        // =====================================================================

        private static Transform BuildTower(Transform root, out CathedralHighWindowEntry belfryDoor)
        {
            Transform env = L3.NewChild(root, "Environment");
            Transform play = L3.NewChild(root, "Gameplay");
            Sprite square = Load(Env + "Placeholder_Square.png");
            Sprite planks = Load(Env + "Level2/Interior_Floor_Planks.png");
            Sprite rafter = Load(Env + "Level2/Interior_Rafter_Loft.png");
            Sprite gear = Load(L4 + "Clockwork_Gear_AI.png");
            Sprite fog = Load(Env + "Level3/FX_Level3_SoftFog.png");
            GameObject cp = LoadPrefab("Assets/Prefabs/Checkpoint.prefab");
            const float X0 = TowerX0, X1 = TowerX1;
            float f1 = TowerFloors[0], f2 = TowerFloors[1], f3 = TowerFloors[2];

            // --- Caja de la torre ---
            L3.AddFitted(env, "Tower_BackWall_Backing", square, new Vector2(X0, -4f), new Vector2(X1, TowerTop + 1.5f), "Background", -11, new Color(0.05f, 0.06f, 0.07f, 1f));
            BrickWall(env, "Tower_BackWall", X0, X1, -1f, TowerTop + 1.5f, Named(DF + "castle_env", "castle_env_17"), -10, new Color(0.34f, 0.38f, 0.40f, 1f), "Background");
            L3.CreateSolidWall(env, "Tower_WestWall", new Vector2(X0 - 0.5f, TowerTop * 0.5f), new Vector2(1f, TowerTop + 4f), _ground, null);
            L3.CreateSolidWall(env, "Tower_EastWall", new Vector2(X1 + 0.5f, TowerTop * 0.5f), new Vector2(1f, TowerTop + 4f), _ground, null);
            L3.CreateSolidWall(env, "Tower_Ceiling", new Vector2((X0 + X1) * 0.5f, TowerTop + 0.5f), new Vector2(X1 - X0, 1f), _ground, null);
            PlankFloor(env, "Tower_Floor", X0, X1, planks, square);

            // Engranajes girando en el muro (solo decorado) y tictac del mecanismo
            AudioClip tick = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_ClockworkTick.wav");
            var gears = new[] { (244.5f, 3.4f, 4.6f, true), (257.5f, 3.2f, 3.2f, false), (256.5f, 9.8f, 5.2f, false), (243.5f, 10.5f, 3f, true), (244.5f, 16.2f, 4.2f, true), (258f, 16f, 3.4f, false), (244f, 23.4f, 3.6f, false) };
            for (int i = 0; i < gears.Length; i++)
            {
                var (gx, gy, size, cw) = gears[i];
                GearDecor(env, $"Clock_Gear_{i}", gear, new Vector2(gx, gy), size, cw, 40f / size, i == 2 ? tick : null);
            }

            // --- Losas de cada tramo con su trampilla (alternan lado para obligar a recorrer el tramo) ---
            Slab(env, "Slab_1", X0, X1 - 4.5f, f1, rafter, square);       // trampilla 1 a la derecha (montacargas)
            Slab(env, "Slab_1b", X1 - 1.5f, X1, f1, rafter, square);
            Slab(env, "Slab_2", X0, X0 + 1.5f, f2, rafter, square);       // trampilla 2 a la izquierda (escalera)
            Slab(env, "Slab_2b", X0 + 4.5f, 247f, f2, rafter, square);    // foso de engranajes 247–255
            Slab(env, "Slab_2c", 255f, X1, f2, rafter, square);
            Slab(env, "Slab_3", X0, X1 - 4.5f, f3, rafter, square);       // trampilla 3 a la derecha (escalera)
            Slab(env, "Slab_3b", X1 - 1.5f, X1, f3, rafter, square);

            // --- Tramo 0 (suelo → losa 1): péndulo, vapor y montacargas ---
            Transform spawn = L3.NewChild(play, "TowerSpawnPoint");
            spawn.position = new Vector3(240.6f, G + 0.6f, 0f);
            Fit(env, "Tower_Entry_Door", Named(DF + "castle_env", "castle_env_7"), 240.2f, G - 0.05f, 3f, "Background", -3, new Color(0.5f, 0.5f, 0.52f));
            L3.CreateCheckpoint(play, cp, "Checkpoint_Tower_Base", new Vector3(241.8f, G, 0f));
            Pendulum(env, "Pendulum_0", new Vector2(249.5f, f1 - 0.6f), 4.6f, 0f, gear, square);
            SteamVent(env, "Steam_Vent_0", 254.6f, G, fog, square);
            MovingPlatform(env, "Gear_Lift_0", new Vector2(261f, G + 0.45f), new Vector2(261f, f1 + 0.25f), 2.4f, 1.6f, rafter, gear);
            Lantern(env, 246.5f, G);

            // --- Tramo 1 (losa 1 → losa 2): péndulo desfasado, vapor y escalera por la trampilla izquierda ---
            Pendulum(env, "Pendulum_1", new Vector2(252f, f2 - 0.6f), 4.6f, 0.5f, gear, square);
            SteamVent(env, "Steam_Vent_1", 246.8f, f1, fog, square);
            L3.CreateOneWayLedge(env, "Beam_Potion", 248.8f, 251.2f, f1 + 3.1f, _ground, null, false);
            L3.AddFitted(env, "Beam_Potion_Visual", rafter, new Vector2(248.8f, f1 + 2.55f), new Vector2(251.2f, f1 + 3.15f), "Gameplay", 2, new Color(0.7f, 0.58f, 0.5f));
            L3.InstantiatePickup(play, LoadPrefab("Assets/Prefabs/PotionPickup_Blind.prefab"), "Potion_Blind_Beam", new Vector3(250f, f1 + 3.5f, 0f));
            GameObject trap2 = Trapdoor(env, "Trapdoor_2", X0 + 1.5f, X0 + 4.5f, f2, planks);
            Ladder(play, "Ladder_Tower_1", X0 + 3f, f1, f2, trap2.GetComponent<Collider2D>());
            Lantern(env, 257.5f, f1);

            // --- Tramo 2 (losa 2 → losa 3): foso de engranajes con plataforma móvil y escalera derecha ---
            L3.CreateCheckpoint(play, cp, "Checkpoint_Tower_Mid", new Vector3(X0 + 5.4f, f2, 0f));
            MovingPlatform(env, "Gear_Shuttle_2", new Vector2(248.3f, f2 - 0.2f), new Vector2(253.7f, f2 - 0.2f), 2.4f, 1.8f, rafter, gear);
            for (float x = 247.6f; x < 255f; x += 1.6f)
                GearDecor(env, "Pit_Gear", gear, new Vector2(x, f1 + 4.6f), 1.5f, ((int)x) % 2 == 0, 90f, null);
            GameObject trap3 = Trapdoor(env, "Trapdoor_3", X1 - 4.5f, X1 - 1.5f, f3, planks);
            Ladder(play, "Ladder_Tower_2", X1 - 3f, f2, f3, trap3.GetComponent<Collider2D>());
            Lantern(env, 258.6f, f2);

            // --- Tramo 3 (losa 3 → techo): esfera del reloj, su péndulo y salida al campanario ---
            ClockFace(env, new Vector2(251f, f3 + 4.3f), 6.2f, gear);
            Pendulum(env, "Clock_Pendulum", new Vector2(251f, f3 + 4.3f), 3.75f, 0.25f, gear, square);
            L3.CreateCheckpoint(play, cp, "Checkpoint_Tower_Top", new Vector3(X1 - 2.4f, f3, 0f));
            Fit(env, "Belfry_Stairs_Door", Named(DF + "castle_env", "castle_env_7"), 241.4f, f3 - 0.05f, 3.2f, "Midground", 1, new Color(0.55f, 0.55f, 0.58f));
            L3.CreateWorldSign(play, "Sign_Belfry", new Vector2(244.6f, f3 + 0.75f), Load(Env + "Env_Sign.png"),
                "Escalera al campanario.\n[Q] para subir");
            belfryDoor = DoorTrigger(play, "Belfry_Entry", new Vector2(241.4f, f3 + 1.3f), new Vector2(1.8f, 2.4f));
            Lantern(env, 246.8f, f3);
            return spawn;
        }

        // =====================================================================
        // PISO 4: CAMPANARIO (Padre Parris y su cáliz, la Gran Campana y la huida por la ventana)
        // =====================================================================

        private static Transform BuildBelfry(Transform root, Transform ui, GameObject witch, Camera cam)
        {
            Transform env = L3.NewChild(root, "Environment");
            Transform play = L3.NewChild(root, "Gameplay");
            Transform npcs = L3.NewChild(root, "NPCs");
            Sprite square = Load(Env + "Placeholder_Square.png");
            Sprite planks = Load(Env + "Level2/Interior_Floor_Planks.png");
            Sprite rafter = Load(Env + "Level2/Interior_Rafter_Loft.png");
            Sprite fog = Load(Env + "Level3/FX_Level3_SoftFog.png");
            Sprite chain = _cainos.FirstOrDefault(s => s.name == "TX Village Props - Chain 01");
            GameObject cp = LoadPrefab("Assets/Prefabs/Checkpoint.prefab");
            const float X0 = BelfryX0, X1 = BelfryX1, loft = BellLoftY;
            const float gateX = 303.6f, holeX0 = 305.6f, holeX1 = 307.8f, fatherX = 290.6f, bellX = 293f, windowX = 280.4f;

            // --- Caja de la torre ---
            L3.AddFitted(env, "Belfry_BackWall_Backing", square, new Vector2(X0, -4f), new Vector2(X1, BelfryTop + 1.5f), "Background", -11, new Color(0.05f, 0.05f, 0.06f, 1f));
            BrickWall(env, "Belfry_BackWall", X0, X1, -1f, BelfryTop + 1.5f, Named(DF + "castle_env", "castle_env_17"), -10, new Color(0.38f, 0.34f, 0.35f, 1f), "Background");
            L3.CreateSolidWall(env, "Belfry_WestWall", new Vector2(X0 - 0.5f, BelfryTop * 0.5f), new Vector2(1f, BelfryTop + 4f), _ground, null);
            L3.CreateSolidWall(env, "Belfry_EastWall", new Vector2(X1 + 0.5f, BelfryTop * 0.5f), new Vector2(1f, BelfryTop + 4f), _ground, null);
            L3.CreateSolidWall(env, "Belfry_Ceiling", new Vector2((X0 + X1) * 0.5f, BelfryTop + 0.5f), new Vector2(X1 - X0, 1f), _ground, null);
            PlankFloor(env, "Belfry_Floor", X0, X1, planks, square);

            // Arcos del campanario abiertos a la tormenta, sobre el piso de la campana
            Sprite arch = Named(DF + "castle_env", "castle_env_9");
            foreach (float bx in new[] { 287f, 299f })
            {
                Fit(env, "Belfry_Arch", arch, bx, loft - 0.05f, 7.4f, "Background", -6, new Color(0.42f, 0.4f, 0.46f));
                PointLight(env, "Belfry_Arch_Storm_Light2D", new Vector3(bx, loft + 4.4f, 0f), new Color(0.6f, 0.8f, 1f), 0.8f, 4f, true);
            }

            // Capilla: vitrales y columnas en el muro bajo el piso de la campana
            foreach (float bx in new[] { 286.4f, 299.4f })
            {
                Fit(env, "Chapel_Arch", arch, bx, G - 0.05f, 5.8f, "Background", -6, new Color(0.5f, 0.44f, 0.46f));
                Fit(env, "Chapel_Vitral", Named(DF + "env_mon", "env_mon_3"), bx, 2.6f, 2.6f, "Background", -5, Color.white);
                PointLight(env, "Chapel_Vitral_Light2D", new Vector3(bx, 3.8f, 0f), new Color(0.75f, 0.55f, 1f), 0.9f, 3f, true);
            }
            foreach (float cx in new[] { 280.6f, 302.2f })
                Fit(env, "Chapel_Column", Named(DF + "env_mon", "env_mon_6"), cx, G - 0.1f, loft - G - 0.5f, "Midground", 1, new Color(0.58f, 0.52f, 0.52f));

            // --- Planta baja: capilla del Padre Parris ---
            Fit(env, "Belfry_Stairwell_Door", Named(DF + "castle_env", "castle_env_7"), 277.8f, G - 0.05f, 3.2f, "Background", -3, new Color(0.55f, 0.5f, 0.5f));
            Transform spawn = L3.NewChild(play, "BelfrySpawnPoint");
            spawn.position = new Vector3(278.8f, G + 0.6f, 0f);
            L3.CreateCheckpoint(play, cp, "Checkpoint_Belfry_Entry", new Vector3(280.2f, G, 0f));
            L3.InstantiatePickup(play, LoadPrefab("Assets/Prefabs/PotionPickup_Sleep.prefab"), "Potion_Sleep_Belfry", new Vector3(281.4f, 0.72f, 0f));
            Carpet(env, "Belfry_Carpet", 281.8f, 289f, square);
            Fit(env, "Belfry_Pew", Load(L4 + "Pew_Oak_Bench_AI.png"), 284.4f, G - 0.05f, 1.45f, "Foreground", 2, new Color(0.7f, 0.58f, 0.52f));
            L3.CreateForestHideout(env, "Belfry_Pew_Hideout", 284.4f, G, 2.6f, 1.3f, null, null, null, null);
            Guard(npcs, "Guard_Belfry_Chapel", 282f, 287.4f, G);
            Lantern(env, 282.6f, G);
            Lantern(env, 298.4f, G);

            // Altar con el cáliz y el Padre de pie frente a él, mirando hacia la entrada
            Fit(env, "Altar_Pulpit", Load(L4 + "Church_Altar_Pulpit_AI.png"), 294.6f, G - 0.08f, 3.2f, "Midground", 2, new Color(0.88f, 0.84f, 0.84f));
            PointLight(env, "Altar_Candles_Light2D", new Vector3(294.6f, G + 2.9f, 0f), new Color(1f, 0.72f, 0.38f), 1.4f, 4.2f, true);
            SpriteRenderer chalice = Fit(env, "Father_Chalice", Load(L4 + "Preacher_Chalice_AI.png"), 293.1f, 2.25f, 0.62f, "Midground", 4, Color.white);
            PointLight(env, "Chalice_Glint_Light2D", new Vector3(293.1f, 2.6f, 0f), new Color(1f, 0.85f, 0.5f), 0.7f, 1.4f, false);
            SpriteRenderer parris = Fit(npcs, "Father_Parris", Load(PreacherArt + "idle_00.png"), fatherX, G - 0.02f, 2.05f, "Gameplay", 4, Color.white, true);

            GameObject father = new GameObject("FatherParris_Encounter");
            father.transform.SetParent(play, false);
            father.transform.position = new Vector3(fatherX + 0.4f, G + 1.1f, 0f);
            BoxCollider2D fcol = father.AddComponent<BoxCollider2D>();
            fcol.isTrigger = true;
            fcol.size = new Vector2(3.4f, 2.2f);
            var encounter = father.AddComponent<PoisonFatherEncounter>();

            // --- Jefe: Padre Parris Maligno ---
            var boss = BuildEvilPriest(npcs, play, parris, fatherX);

            // --- Reja que cierra la escalera al piso de la campana: se abre con la alarma ---
            GameObject gate = new GameObject("Escape_Path_Gate");
            gate.transform.SetParent(play, false);
            L3.CreateSolidWall(gate.transform, "Gate_Blocker", new Vector2(gateX, G + 1.5f), new Vector2(0.5f, 3f), _ground, null);
            Sprite ironGate = Opt(Garden + "Panteon_Iron_Gate.png");
            if (ironGate != null) Fit(gate.transform, "Gate_Visual", ironGate, gateX, G - 0.05f, 3f, "Gameplay", 3, new Color(0.7f, 0.68f, 0.7f));
            else L3.AddFitted(gate.transform, "Gate_Visual", square, new Vector2(gateX - 0.25f, G), new Vector2(gateX + 0.25f, G + 3f), "Gameplay", 3, new Color(0.15f, 0.15f, 0.17f));
            // Dintel fijo sobre la reja hasta el piso de la campana (sin él se podría volar por encima)
            L3.CreateSolidWall(env, "Gate_Lintel", new Vector2(gateX, (G + 3f + loft) * 0.5f), new Vector2(0.9f, loft - G - 3f), _ground, null);
            BrickWall(env, "Gate_Lintel_Visual", gateX - 0.45f, gateX + 0.45f, G + 3f, loft - 0.4f, Named(DF + "castle_env", "castle_env_26"), 2, new Color(0.5f, 0.46f, 0.46f));

            // --- Piso de la campana (losa maciza salvo la trampilla de la escalera) ---
            Slab(env, "Bell_Loft", X0, holeX0, loft, rafter, square);
            Slab(env, "Bell_Loft_East", holeX1, X1, loft, rafter, square);
            GameObject trap = Trapdoor(env, "Bell_Loft_Trapdoor", holeX0, holeX1, loft, planks);
            Ladder(play, "Ladder_Bell_Loft", (holeX0 + holeX1) * 0.5f, G, loft, trap.GetComponent<Collider2D>());
            L3.CreateCheckpoint(play, cp, "Checkpoint_Bell_Loft", new Vector3(308.8f, loft, 0f));
            Lantern(env, 304f, loft);

            // --- La Gran Campana: el gato la hace repicar al tocar la cuerda y aturde a todos los guardias ---
            GameObject bell = new GameObject("Great_Bell");
            bell.transform.SetParent(play, false);
            bell.transform.position = new Vector3(bellX, loft + 1.25f, 0f);
            BoxCollider2D bcol = bell.AddComponent<BoxCollider2D>();
            bcol.isTrigger = true;
            bcol.size = new Vector2(1.6f, 2.3f);
            var bellTrigger = bell.AddComponent<GreatBellRopeTrigger>();

            const float bellBottom = loft + 2.1f, bellH = 4.3f;
            Transform pivot = L3.NewChild(bell.transform, "Bell_Pivot");
            pivot.position = new Vector3(bellX, bellBottom + bellH - 0.3f, 0f);
            Fit(pivot, "Bell_Visual", Load(L4 + "Salem_Great_Bell_AI.png"), bellX, bellBottom, bellH, "Midground", 4, new Color(0.9f, 0.95f, 0.92f));
            foreach (float px in new[] { bellX - 2.5f, bellX + 2.5f })
                L3.AddFitted(env, "Bell_Frame_Post", rafter, new Vector2(px - 0.2f, loft - 0.05f), new Vector2(px + 0.2f, bellBottom + bellH - 0.9f), "Midground", 3, new Color(0.55f, 0.45f, 0.38f));
            if (chain != null) L3.AddFitted(env, "Bell_Rope", chain, new Vector2(bellX - 0.08f, loft + 0.75f), new Vector2(bellX + 0.08f, bellBottom + 0.2f), "Gameplay", 3, new Color(0.75f, 0.62f, 0.45f));
            PointLight(env, "Bell_Light2D", new Vector3(bellX, bellBottom + 1.6f, 0f), new Color(0.75f, 1f, 0.85f), 0.9f, 4f, false);

            // Onda expansiva: el script la escala de 0.2 a 6; la raíz la normaliza a 2 u por unidad de escala
            Sprite ring = Opt(NewShockwave) ?? fog;
            Transform ringRoot = L3.NewChild(bell.transform, "Shockwave_Root");
            ringRoot.position = new Vector3(bellX, bellBottom + 1.2f, 0f);
            ringRoot.localScale = Vector3.one * (2f / Mathf.Max(0.01f, Mathf.Max(ring.bounds.size.x, ring.bounds.size.y)));
            GameObject ringGo = new GameObject("Shockwave");
            ringGo.transform.SetParent(ringRoot, false);
            SpriteRenderer ringSr = ringGo.AddComponent<SpriteRenderer>();
            ringSr.sprite = ring;
            ringSr.sortingLayerName = "Foreground";
            ringSr.sortingOrder = 8;
            ringSr.color = new Color(1f, 0.92f, 0.7f, 1f);
            ringGo.SetActive(false);

            // --- Alarma (tras envenenar al Padre): luces rojas y refuerzos en el piso de la campana ---
            GameObject alarm = new GameObject("Alarm_Visuals");
            alarm.transform.SetParent(root, false);
            foreach (var (ax, ay) in new[] { (283f, loft - 1.4f), (296f, loft - 1.4f), (286f, loft + 3.2f), (300f, loft + 3.2f) })
                PointLight(alarm.transform, "Alarm_Red_Light2D", new Vector3(ax, ay, 0f), new Color(1f, 0.22f, 0.18f), 1.1f, 6f, true);
            Guard(alarm.transform, "Guard_Alarm_Window", 278.6f, 286.6f, loft);
            Guard(alarm.transform, "Guard_Alarm_Loft", 296.6f, 302.4f, loft);
            alarm.SetActive(false);

            // --- Ventana rota de la torre: zona de huida que se habilita al repicar la campana ---
            Fit(env, "Escape_Window", Load(L4 + "Gothic_Window_Empty_BrokenBars.png"), windowX, loft + 0.35f, 3.6f, "Background", -2, Color.white);
            PointLight(env, "Escape_Window_Moon_Light2D", new Vector3(windowX, loft + 2.4f, 0f), new Color(0.7f, 0.85f, 1f), 1.2f, 3.6f, true);
            GameObject zone = new GameObject("Escape_Window_Zone");
            zone.transform.SetParent(play, false);
            zone.transform.position = new Vector3(windowX, loft + 1.3f, 0f);
            BoxCollider2D zcol = zone.AddComponent<BoxCollider2D>();
            zcol.isTrigger = true;
            zcol.size = new Vector2(2f, 2.6f);
            var ending = zone.AddComponent<Level4EndingSequence>();

            GameObject broom = BuildBroomWitch(root, zone.transform.position);
            BuildCredits(ui, ending, out CanvasGroup credits, out RectTransform creditsScroll);

            // --- Cableado de los scripts del final ---
            var so = new SerializedObject(encounter);
            so.FindProperty("chaliceRenderer").objectReferenceValue = chalice;
            so.FindProperty("poisonedChaliceSprite").objectReferenceValue = Opt(NewChalicePoisoned);
            so.FindProperty("preacherRenderer").objectReferenceValue = parris;
            so.FindProperty("preacherCollapsedSprite").objectReferenceValue = Opt(NewParrisCollapsed) ?? Load(PreacherArt + "sleep_02.png");
            so.FindProperty("escapePathBlocker").objectReferenceValue = gate;
            so.FindProperty("alarmVisuals").objectReferenceValue = alarm;
            so.FindProperty("bossAI").objectReferenceValue = boss;
            so.FindProperty("poisonSfx").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_PoisonGoblet.wav");
            so.FindProperty("alarmMusic").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/BGM_Level4_Boss_Escape.wav");
            so.ApplyModifiedPropertiesWithoutUndo();


            so = new SerializedObject(bellTrigger);
            so.FindProperty("bellTransform").objectReferenceValue = pivot;
            so.FindProperty("bellTollSfx").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_GreatBellToll.wav");
            so.FindProperty("shockwaveRenderer").objectReferenceValue = ringSr;
            so.FindProperty("escapeWindowZone").objectReferenceValue = zone;
            so.ApplyModifiedPropertiesWithoutUndo();

            so = new SerializedObject(ending);
            so.FindProperty("escapeWindowTransform").objectReferenceValue = zone.transform;
            so.FindProperty("playerObject").objectReferenceValue = witch;
            so.FindProperty("cinematicBroomWitch").objectReferenceValue = broom;
            so.FindProperty("mainCamera").objectReferenceValue = cam;
            so.FindProperty("creditsCanvasGroup").objectReferenceValue = credits;
            so.FindProperty("creditsScrollContent").objectReferenceValue = creditsScroll;
            so.FindProperty("glassShatterSfx").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_GlassShatterEscape.wav");
            so.FindProperty("creditsMusic").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/BGM_Credits_Salem.wav");
            so.ApplyModifiedPropertiesWithoutUndo();
            zone.SetActive(false); // la activa la campana

            return spawn;
        }

        // ------------------------------------------------------------------ //
        //  Jefe: Padre Parris Maligno                                         //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Instancia el jefe del campanario junto al Padre Parris normal.
        /// Usa los sprites del preacher para la forma normal y la carpeta EvilPriest para la maligna.
        /// Si la carpeta EvilPriest no existe, recae en los sprites del preacher con tinte rojo.
        /// </summary>
        private static EvilPriestBossAI BuildEvilPriest(Transform npcs, Transform play,
            SpriteRenderer normalSr, float fatherX)
        {
            GameObject bossGO = new GameObject("EvilPriest_Boss");
            bossGO.transform.SetParent(npcs, false);
            bossGO.transform.position = new Vector3(fatherX, G - 0.02f, 0f);

            var rb = bossGO.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var col = bossGO.AddComponent<BoxCollider2D>();
            col.size   = new Vector2(0.85f, 1.75f);
            col.offset = new Vector2(0f, 0.875f);

            // Renderer de la forma maligna (oculto al inicio)
            GameObject evilSrGO = new GameObject("EvilPriest_Renderer");
            evilSrGO.transform.SetParent(bossGO.transform, false);
            evilSrGO.transform.localPosition = Vector3.zero;
            var evilSr = evilSrGO.AddComponent<SpriteRenderer>();
            evilSr.sortingLayerName = "Gameplay";
            evilSr.sortingOrder     = 5;
            evilSr.enabled          = false;

            // Audio
            var audioSrc = bossGO.AddComponent<AudioSource>();
            audioSrc.spatialBlend = 0f;
            audioSrc.playOnAwake  = false;

            var boss = bossGO.AddComponent<EvilPriestBossAI>();

            // ---- Cargar sprites ----
            // Forma normal: frames idle del preacher
            Sprite[] normalIdles = LoadFrameRange(PreacherArt + "idle_", 0, 11);
            // Frames de transformación: screams del preacher
            Sprite[] transformFrames = LoadFrameRange(PreacherArt + "scream_", 0, 3);
            // Frames de stun: sleep del preacher
            Sprite[] stunFrames = LoadFrameRange(PreacherArt + "sleep_", 0, 2);

            // Forma maligna: si existe carpeta EvilPriest, usa esos sprites;
            // si no, usa los del preacher con tinte oscuro-rojo.
            Sprite[] evilIdles   = LoadFrameRange(EvilPriestArt + "idle_",   0, 3);
            Sprite[] evilRuns    = LoadFrameRange(EvilPriestArt + "run_",    0, 3);
            Sprite[] evilAttacks = LoadFrameRange(EvilPriestArt + "attack_", 0, 3);
            Sprite[] evilThrows  = LoadFrameRange(EvilPriestArt + "throw_",  0, 3);
            Sprite   evilCollapsed = Opt(EvilPriestArt + "collapsed.png");
            Sprite   holyWaterSpr  = Opt(EvilPriestArt + "holy_water.png")
                                     ?? Load(PreacherArt + "action_00.png"); // fallback

            // Si no hay sprites propios, recaer en los del preacher
            if (evilIdles   == null || evilIdles.Length == 0)  evilIdles   = LoadFrameRange(PreacherArt + "walk_",   0, 7);
            if (evilRuns    == null || evilRuns.Length   == 0)  evilRuns    = LoadFrameRange(PreacherArt + "run_",    0, 10);
            if (evilAttacks == null || evilAttacks.Length == 0) evilAttacks = LoadFrameRange(PreacherArt + "action_", 0, 7);
            if (evilThrows  == null || evilThrows.Length  == 0) evilThrows  = LoadFrameRange(PreacherArt + "action_", 0, 3);
            if (evilCollapsed == null) evilCollapsed = Load(PreacherArt + "sleep_02.png");

            // Si recayó en sprites del preacher, aplicar tinte rojo oscuro al evilRenderer
            bool usingFallback = !AssetDatabase.IsValidFolder(EvilPriestArt.TrimEnd('/'));
            if (usingFallback) evilSr.color = new Color(0.75f, 0.15f, 0.15f);

            // Serializar todos los campos
            var so = new SerializedObject(boss);
            so.FindProperty("normalRenderer").objectReferenceValue    = normalSr;
            so.FindProperty("evilRenderer").objectReferenceValue      = evilSr;
            so.FindProperty("audioSource").objectReferenceValue       = audioSrc;
            so.FindProperty("holyWaterSprite").objectReferenceValue   = holyWaterSpr;
            so.FindProperty("evilCollapsedSprite").objectReferenceValue = evilCollapsed;
            SetSpriteArray(so, "normalIdleFrames",    normalIdles);
            SetSpriteArray(so, "evilTransformFrames", transformFrames);
            SetSpriteArray(so, "evilStunFrames",      stunFrames);
            SetSpriteArray(so, "evilIdleFrames",      evilIdles);
            SetSpriteArray(so, "evilRunFrames",       evilRuns);
            SetSpriteArray(so, "evilAttackFrames",    evilAttacks);
            SetSpriteArray(so, "evilThrowFrames",     evilThrows);
            so.ApplyModifiedPropertiesWithoutUndo();

            return boss;
        }

        /// <summary>Carga una secuencia de sprites con nombre base + índice de dos dígitos.</summary>
        private static Sprite[] LoadFrameRange(string basePath, int from, int to)
        {
            var list = new System.Collections.Generic.List<Sprite>();
            for (int i = from; i <= to; i++)
            {
                string path = basePath + i.ToString("D2") + ".png";
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null) list.Add(s);
            }
            return list.Count > 0 ? list.ToArray() : null;
        }

        /// <summary>Escribe un array de sprites en una propiedad serializada.</summary>
        private static void SetSpriteArray(SerializedObject so, string propName, Sprite[] sprites)
        {
            if (sprites == null || sprites.Length == 0) return;
            var prop = so.FindProperty(propName);
            if (prop == null) return;
            prop.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }

        /// <summary>Bruja en escoba para la cinemática final; sin Witch_Broom_Flight.png se arma con Witch_Fly + Witch_Broom.</summary>
        private static GameObject BuildBroomWitch(Transform root, Vector3 pos)
        {
            GameObject go = new GameObject("Cinematic_Broom_Witch");
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            Sprite flight = Opt(NewBroomFlight);
            if (flight != null)
                Fit(go.transform, "Broom_Witch", flight, pos.x, pos.y - 0.9f, 1.9f, "Foreground", 9, Color.white);
            else
            {
                // shortcut: bruja de vuelo sentada sobre la escoba suelta; se sustituye solo al existir Witch_Broom_Flight.png
                // Misma pose que en el juego: la escoba va DETRÁS de la bruja (como BroomVisual, orden menor),
                // a la altura de las rodillas/cadera, sin inclinar; ella queda sentada con las piernas por delante.
                Sprite[] fly = Frames(WitchArt + "Witch_Fly.png", 0);
                Sprite rider = fly != null && fly.Length > 0 ? fly[0] : Load(WitchArt + "Witch_Fly.png");
                Fit(go.transform, "Rider", rider, pos.x + 0.1f, pos.y - 0.62f, 1.55f, "Foreground", 9, Color.white);
                Fit(go.transform, "Broom", Load(WitchArt + "Witch_Broom.png"), pos.x - 0.05f, pos.y - 0.35f, 0.55f, "Foreground", 8, Color.white);
            }
            PointLight(go.transform, "Broom_Moonlight_Light2D", pos, new Color(0.8f, 0.9f, 1f), 1.2f, 3f, false);
            go.SetActive(false);
            return go;
        }

        /// <summary>Lienzo de créditos (oculto): fondo de pergamino, texto que sube y botón para volver al menú.</summary>
        private static void BuildCredits(Transform ui, Level4EndingSequence ending, out CanvasGroup group, out RectTransform scroll)
        {
            GameObject canvasGo = new GameObject("Credits_Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster), typeof(CanvasGroup));
            canvasGo.transform.SetParent(ui, false);
            canvasGo.layer = LayerMask.NameToLayer("UI");
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            group = canvasGo.GetComponent<CanvasGroup>();
            group.alpha = 0f;

            Sprite parchment = Opt(CreditsBg);
            var bg = UiChild(canvasGo.transform, "Credits_Background", Vector2.zero, Vector2.one).gameObject.AddComponent<UnityEngine.UI.Image>();
            bg.sprite = parchment;
            bg.color = parchment != null ? new Color(0.75f, 0.75f, 0.75f, 1f) : new Color(0.02f, 0.02f, 0.04f, 1f);

            scroll = UiChild(canvasGo.transform, "Credits_Scroll", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            scroll.pivot = new Vector2(0.5f, 1f);
            scroll.sizeDelta = new Vector2(1200f, 1100f);
            scroll.anchoredPosition = new Vector2(0f, 360f);
            var text = scroll.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
            text.alignment = TMPro.TextAlignmentOptions.Top;
            text.fontSize = 40f;
            text.color = new Color(0.96f, 0.9f, 0.74f, 1f);
            text.text = "<size=84><b>BRUJAS DE SALEM</b></size>\n<size=34><i>Salem, 1692</i></size>\n\n\n" +
                        "La bruja vertió la poción en el cáliz,\nhizo repicar la Gran Campana\ny escapó volando bajo la luna.\n\n\n" +
                        "<size=46><b>Diseño y desarrollo</b></size>\nEquipo de Brujas de Salem\n\n" +
                        "<size=46><b>Arte</b></size>\nCainos · Pixel Art Platformer - Village Props\nDark Forest · Parallax Forest\nArte del Nivel 4 generado con Antigravity\n\n" +
                        "<size=46><b>Motor</b></size>\nUnity\n\n\n<size=52><b>¡Gracias por jugar!</b></size>";

            RectTransform btnRt = UiChild(canvasGo.transform, "Credits_MenuButton", new Vector2(1f, 0f), new Vector2(1f, 0f));
            btnRt.pivot = new Vector2(1f, 0f);
            btnRt.sizeDelta = new Vector2(380f, 90f);
            btnRt.anchoredPosition = new Vector2(-60f, 50f);
            var btnImg = btnRt.gameObject.AddComponent<UnityEngine.UI.Image>();
            btnImg.color = new Color(0.16f, 0.1f, 0.14f, 0.9f);
            var button = btnRt.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = btnImg;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, ending.ReturnToMainMenu);
            var label = UiChild(btnRt, "Label", Vector2.zero, Vector2.one).gameObject.AddComponent<TMPro.TextMeshProUGUI>();
            label.text = "Volver al menú";
            label.fontSize = 38f;
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.color = new Color(0.96f, 0.9f, 0.74f, 1f);

            canvasGo.SetActive(false);
        }

        private static RectTransform UiChild(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.gameObject.layer = LayerMask.NameToLayer("UI");
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static void WireDoor(CathedralHighWindowEntry door, GameObject from, GameObject to, Transform spawn, Vector2 camMin, Vector2 camMax,
            Light2D global, Color ambient, float intensity, string prompt, string arrival)
        {
            var so = new SerializedObject(door);
            so.FindProperty("interiorSpawnPoint").objectReferenceValue = spawn;
            so.FindProperty("courtyardSection").objectReferenceValue = from;
            so.FindProperty("churchInteriorSection").objectReferenceValue = to;
            so.FindProperty("globalLight").objectReferenceValue = global;
            so.FindProperty("interiorCameraMin").vector2Value = camMin;
            so.FindProperty("interiorCameraMax").vector2Value = camMax;
            so.FindProperty("interiorAmbientColor").colorValue = ambient;
            so.FindProperty("interiorAmbientIntensity").floatValue = intensity;
            so.FindProperty("requireCat").boolValue = false;
            so.FindProperty("catPrompt").stringValue = prompt;
            so.FindProperty("arrivalMessage").stringValue = arrival;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static CathedralHighWindowEntry DoorTrigger(Transform parent, string name, Vector2 center, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;
            return go.AddComponent<CathedralHighWindowEntry>();
        }

        /// <summary>Suelo de duelas como el de la nave: superficie en G, collider macizo y relleno oscuro debajo.</summary>
        private static void PlankFloor(Transform env, string name, float x0, float x1, Sprite planks, Sprite square)
        {
            GameObject floor = new GameObject(name);
            floor.transform.SetParent(env, false);
            floor.layer = _ground;
            floor.transform.position = new Vector3((x0 + x1) * 0.5f, G - 2.085f, 0f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(x1 - x0, 4.17f);
            int n = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / 4f));
            float w = (x1 - x0) / n;
            for (int i = 0; i < n; i++)
                L3.AddFitted(floor.transform, "Floor_Planks", planks, new Vector2(x0 + i * w, G - 1f), new Vector2(x0 + (i + 1) * w + 0.01f, G), "Midground", 0, new Color(0.62f, 0.5f, 0.42f));
            L3.AddFitted(floor.transform, "Floor_Under", square, new Vector2(x0, -4.2f), new Vector2(x1, G - 0.95f), "Midground", 0, new Color(0.08f, 0.06f, 0.05f));
        }

        /// <summary>Losa maciza de un tramo de la torre (superficie en topY, 0.5 de grueso).</summary>
        private static void Slab(Transform env, string name, float x0, float x1, float topY, Sprite rafter, Sprite square)
        {
            if (x1 - x0 < 0.2f) return;
            L3.CreateSolidWall(env, name, new Vector2((x0 + x1) * 0.5f, topY - 0.25f), new Vector2(x1 - x0, 0.5f), _ground, null);
            L3.AddFitted(env, name + "_Under", square, new Vector2(x0, topY - 0.55f), new Vector2(x1, topY - 0.1f), "Midground", 1, new Color(0.1f, 0.08f, 0.07f));
            int n = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / 4f));
            float w = (x1 - x0) / n;
            for (int i = 0; i < n; i++)
                L3.AddFitted(env, name + "_Beam", rafter, new Vector2(x0 + i * w, topY - 0.45f), new Vector2(x0 + (i + 1) * w + 0.01f, topY + 0.05f), "Gameplay", 2, new Color(0.62f, 0.52f, 0.46f));
        }

        /// <summary>Trampilla de una sola dirección: se atraviesa subiendo por la escalera y se pisa al llegar.</summary>
        private static GameObject Trapdoor(Transform env, string name, float x0, float x1, float topY, Sprite planks)
        {
            L3.AddFitted(env, name + "_Visual", planks, new Vector2(x0, topY - 0.25f), new Vector2(x1, topY + 0.04f), "Gameplay", 2, new Color(0.5f, 0.4f, 0.32f));
            return L3.CreateOneWayLedge(env, name, x0, x1, topY, _ground, null, false);
        }

        private static void Balustrade(Transform env, float x0, float x1)
        {
            Sprite tile = Opt(NewBalustrade);
            Sprite rail = tile ?? Named(DF + "decorative_obj", "decorative_obj_18");
            if (rail == null || x1 - x0 < 0.5f) return;
            int n = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / 2.2f));
            float w = (x1 - x0) / n;
            float h = tile != null ? w * Aspect(tile) : 1.1f;
            for (int i = 0; i < n; i++)
                L3.AddFitted(env, "Balustrade", rail, new Vector2(x0 + i * w, G - 0.02f), new Vector2(x0 + (i + 1) * w + 0.01f, G + h), "Midground", 3, tile != null ? Color.white : new Color(0.62f, 0.56f, 0.52f));
        }

        /// <summary>Órgano de tubos centrado en cx. Devuelve la altura de su cornisa pisable.</summary>
        private static float Organ(Transform env, float cx, Sprite square)
        {
            Sprite art = Opt(NewOrgan);
            if (art != null)
                return Fit(env, "Choir_Pipe_Organ", art, cx, G - 0.05f, 6f, "Midground", 2, Color.white).bounds.max.y - 0.12f;

            // shortcut: órgano armado con un estante y tubos rectangulares; se sustituye solo al existir Choir_Pipe_Organ.png
            L3.AddFitted(env, "Organ_Case", Load(Env + "Level2/Interior_Bookshelf.png"), new Vector2(cx - 2.5f, G - 0.05f), new Vector2(cx + 2.5f, 2.6f), "Midground", 2, new Color(0.5f, 0.36f, 0.28f));
            float[] heights = { 2.1f, 2.6f, 3.0f, 3.3f, 3.5f, 3.3f, 3.0f, 2.6f, 2.1f };
            for (int i = 0; i < heights.Length; i++)
            {
                float px = cx - 2.2f + i * 0.55f;
                L3.AddFitted(env, "Organ_Pipe", square, new Vector2(px - 0.2f, 2.6f), new Vector2(px + 0.2f, 2.6f + heights[i]), "Midground", 1, new Color(0.62f, 0.56f, 0.42f));
                L3.AddFitted(env, "Organ_Pipe_Mouth", square, new Vector2(px - 0.12f, 2.9f), new Vector2(px + 0.12f, 3.05f), "Midground", 2, new Color(0.1f, 0.08f, 0.07f));
            }
            L3.AddFitted(env, "Organ_Cornice", Named(DF + "castle_env", "castle_env_22"), new Vector2(cx - 2.6f, 6.15f), new Vector2(cx + 2.6f, 6.5f), "Gameplay", 2, new Color(0.5f, 0.4f, 0.32f));
            return 6.45f;
        }

        private static void GearDecor(Transform parent, string name, Sprite gear, Vector2 center, float size, bool clockwise, float speed, AudioClip tick)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            L3.AddFitted(go.transform, "Gear_Visual", gear, center - Vector2.one * size * 0.5f, center + Vector2.one * size * 0.5f, "Background", -4, new Color(0.62f, 0.55f, 0.42f));
            var spin = go.AddComponent<ClockworkGearPlatform>();
            var so = new SerializedObject(spin);
            so.FindProperty("rotationSpeed").floatValue = speed;
            so.FindProperty("clockwise").boolValue = clockwise;
            if (tick != null) so.FindProperty("tickSfx").objectReferenceValue = tick;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Péndulo con pivote en 'pivot': el contrapeso (trigger) quita un corazón.</summary>
        private static void Pendulum(Transform parent, string name, Vector2 pivot, float length, float phase, Sprite gear, Sprite square)
        {
            GameObject p = new GameObject(name);
            p.transform.SetParent(parent, false);
            p.transform.position = pivot;
            var hazard = p.AddComponent<PendulumHazard>();
            p.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var so = new SerializedObject(hazard);
            so.FindProperty("phase").floatValue = phase;
            so.ApplyModifiedPropertiesWithoutUndo();

            Sprite art = Opt(NewPendulum);
            if (art != null)
            {
                float h = length + 0.75f, w = h / Aspect(art);
                L3.AddFitted(p.transform, "Pendulum_Art", art, new Vector2(pivot.x - w * 0.5f, pivot.y - h + 0.1f), new Vector2(pivot.x + w * 0.5f, pivot.y + 0.1f), "Gameplay", 4, Color.white);
            }
            else
            {
                L3.AddFitted(p.transform, "Pendulum_Rod", square, new Vector2(pivot.x - 0.06f, pivot.y - length), new Vector2(pivot.x + 0.06f, pivot.y), "Gameplay", 4, new Color(0.2f, 0.18f, 0.17f));
                L3.AddFitted(p.transform, "Pendulum_Bob", gear, new Vector2(pivot.x - 0.65f, pivot.y - length - 0.65f), new Vector2(pivot.x + 0.65f, pivot.y - length + 0.65f), "Gameplay", 5, new Color(0.85f, 0.7f, 0.4f));
            }
            GameObject bob = new GameObject("Pendulum_Bob_Hitbox");
            bob.transform.SetParent(p.transform, false);
            bob.transform.localPosition = new Vector3(0f, -length, 0f);
            CircleCollider2D hit = bob.AddComponent<CircleCollider2D>();
            hit.isTrigger = true;
            hit.radius = 0.55f;
            PointLight(p.transform, name + "_Glint", new Vector3(pivot.x, pivot.y - length, 0f), new Color(1f, 0.8f, 0.5f), 0.6f, 1.6f, false);
        }

        /// <summary>Respiradero de vapor: ciclo seguro → aviso → chorro que quita un corazón (Level3EnvironmentalHazard).</summary>
        private static void SteamVent(Transform parent, string name, float cx, float baseY, Sprite fog, Sprite square)
        {
            L3.AddFitted(parent, name + "_Grate", square, new Vector2(cx - 0.7f, baseY - 0.05f), new Vector2(cx + 0.7f, baseY + 0.08f), "Gameplay", 3, new Color(0.18f, 0.16f, 0.15f));
            GameObject v = new GameObject(name);
            v.transform.SetParent(parent, false);
            v.transform.position = new Vector3(cx, baseY + 1.1f, 0f);
            BoxCollider2D col = v.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.1f, 2.1f);
            SpriteRenderer steam = L3.AddFitted(v.transform, "Steam", fog, new Vector2(cx - 0.9f, baseY), new Vector2(cx + 0.9f, baseY + 2.6f), "Gameplay", 6, new Color(1f, 1f, 1f, 0.1f));
            Light2D glow = PointLight(v.transform, name + "_Light2D", new Vector3(cx, baseY + 0.8f, 0f), new Color(0.8f, 0.95f, 1f), 0.3f, 2f, false);
            var hz = v.AddComponent<Level3EnvironmentalHazard>();
            var so = new SerializedObject(hz);
            so.FindProperty("hazardRenderer").objectReferenceValue = steam;
            so.FindProperty("hazardLight").objectReferenceValue = glow;
            so.FindProperty("dormantDuration").floatValue = 2.6f;
            so.FindProperty("warningDuration").floatValue = 0.9f;
            so.FindProperty("activeDuration").floatValue = 1.5f;
            so.FindProperty("dormantColor").colorValue = new Color(0.85f, 0.95f, 1f, 0.06f);
            so.FindProperty("warningColor").colorValue = new Color(0.95f, 1f, 1f, 0.3f);
            so.FindProperty("activeColor").colorValue = new Color(0.95f, 1f, 1f, 0.8f);
            so.FindProperty("damageCause").stringValue = "Vapor del mecanismo del reloj";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Plataforma móvil del mecanismo (Level3MovingPlatform arrastra a la bruja); a, b = superficie pisable.</summary>
        private static void MovingPlatform(Transform parent, string name, Vector2 a, Vector2 b, float width, float speed, Sprite rafter, Sprite gear)
        {
            // El pivote va 0.2 bajo la superficie: Level3MovingPlatform solo lleva al pasajero si sus pies quedan > 0.1 por encima
            Vector3 pa = new Vector3(a.x, a.y - 0.2f, 0f), pb = new Vector3(b.x, b.y - 0.2f, 0f);
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pa;
            go.layer = _ground;
            go.AddComponent<BoxCollider2D>().size = new Vector2(width, 0.4f);
            L3.AddFitted(go.transform, "Platform_Visual", rafter, new Vector2(pa.x - width * 0.5f, pa.y - 0.3f), new Vector2(pa.x + width * 0.5f, pa.y + 0.22f), "Gameplay", 3, new Color(0.72f, 0.6f, 0.48f));
            L3.AddFitted(go.transform, "Platform_Gear", gear, new Vector2(pa.x - 0.45f, pa.y - 1.1f), new Vector2(pa.x + 0.45f, pa.y - 0.2f), "Gameplay", 2, new Color(0.7f, 0.6f, 0.42f));
            Light2D l = PointLight(go.transform, name + "_Light2D", new Vector3(pa.x, pa.y + 0.3f, 0f), new Color(1f, 0.78f, 0.45f), 0.6f, 2f, false);
            go.AddComponent<Level3MovingPlatform>().Configure(pa, pb, speed, 0.8f);
            var so = new SerializedObject(go.GetComponent<Level3MovingPlatform>());
            so.FindProperty("runeGlowColor").colorValue = new Color(1f, 0.78f, 0.45f, 1f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Lantern(Transform env, float x, float baseY)
        {
            Fit(env, "Tower_Candelabra", Named(DF + "decorative_obj", "decorative_obj_3"), x, baseY - 0.05f, 1.5f, "Midground", 3, new Color(0.9f, 0.8f, 0.6f));
            PointLight(env, "Tower_Candelabra_Light2D", new Vector3(x, baseY + 1.6f, 0f), new Color(1f, 0.72f, 0.38f), 1f, 3f, true);
        }

        /// <summary>Esfera del reloj vista desde dentro con manecillas girando; sin arte, un engranaje pálido a contraluz.</summary>
        private static void ClockFace(Transform env, Vector2 center, float size, Sprite gear)
        {
            Sprite face = Opt(NewClockFace);
            L3.AddFitted(env, "Clock_Face", face ?? gear, center - Vector2.one * size * 0.5f, center + Vector2.one * size * 0.5f, "Background", -3,
                face != null ? Color.white : new Color(0.7f, 0.85f, 0.8f, 0.55f));
            PointLight(env, "Clock_Moonlight_Light2D", center, new Color(0.7f, 1f, 0.9f), 1.3f, size * 1.1f, false);
            Sprite[] hands = Frames(NewClockHands, 0, 1);
            for (int i = 0; i < 2; i++)
            {
                float len = size * (i == 0 ? 0.28f : 0.4f);
                GameObject pivot = new GameObject(i == 0 ? "Clock_Hand_Hour" : "Clock_Hand_Minute");
                pivot.transform.SetParent(env, false);
                pivot.transform.position = center;
                Sprite hs = hands != null && hands.Length == 2 ? hands[i] : Load(Env + "Placeholder_Square.png");
                float w = hands != null && hands.Length == 2 ? len / Aspect(hs) : (i == 0 ? 0.22f : 0.14f);
                L3.AddFitted(pivot.transform, "Hand", hs, new Vector2(center.x - w * 0.5f, center.y), new Vector2(center.x + w * 0.5f, center.y + len), "Background", -2,
                    hands != null && hands.Length == 2 ? Color.white : new Color(0.12f, 0.12f, 0.12f));
                pivot.transform.rotation = Quaternion.Euler(0f, 0f, i == 0 ? -110f : -20f);
                var spin = pivot.AddComponent<ClockworkGearPlatform>();
                var so = new SerializedObject(spin);
                so.FindProperty("rotationSpeed").floatValue = i == 0 ? 0.5f : 6f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private static Sprite Load(string path)
        {
            // Hojas en modo Multiple: el corte más grande es el arte principal (los demás son restos del recorte)
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderByDescending(s => s.rect.width * s.rect.height).FirstOrDefault();
        }

        /// <summary>Sprite principal de un asset opcional, o null si todavía no existe.</summary>
        private static Sprite Opt(string path) => System.IO.File.Exists(path) ? Load(path) : null;

        /// <summary>Alto/ancho del área visible del sprite.</summary>
        private static float Aspect(Sprite sp)
        {
            L3.VisibleExtents(sp, out float x0, out float x1, out float y0, out float y1);
            return (y1 - y0) / Mathf.Max(0.01f, x1 - x0);
        }

        /// <summary>Cuadros de una hoja cortada en orden de lectura (izq→der, arriba→abajo).</summary>
        private static void SetFrames(SerializedObject so, string field, Sprite[] frames)
        {
            SerializedProperty p = so.FindProperty(field);
            p.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }

        private static Sprite[] Frames(string path, params int[] indices)
        {
            if (!System.IO.File.Exists(path)) return null;
            string baseName = System.IO.Path.GetFileNameWithoutExtension(path);
            var all = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
            return indices.Select(i => all.TryGetValue($"{baseName}_{i}", out Sprite s) ? s : null).Where(s => s != null).ToArray();
        }

        /// <summary>
        /// Ajustes de importación del arte nuevo: pixel art con filtro Point y 64 ppu; las hojas se cortan en celdas
        /// (SpriteSheetSlicer) solo si aún no están cortadas. El Inquisidor es cel-shaded: conserva filtro Bilinear.
        /// </summary>
        private static void PrepareNewArt()
        {
            foreach (string p in new[] { Garden + "Gothic_Cat_Entry_Window.png", Garden + "Panteon_Iron_Gate.png", NewMausoleum, NewCarpet, NewCreaky, NewGround, NewRuin, NewOrgan, NewStalls, NewBalustrade, NewClockFace, NewPendulum, NewChalicePoisoned })
                Import(p, FilterMode.Point, true);
            foreach (string p in new[] { NewFogStrip, NewSky, NewFar, NewNear, NewBroomFlight, NewShockwave, CreditsBg })
                Import(p, FilterMode.Bilinear, true);
            Slice(NewTombstones, 128, 256, FilterMode.Point);
            Slice(NewInquisitorSheet, 256, 384, FilterMode.Bilinear);
            Slice(NewClockHands, 128, 512, FilterMode.Point);
            Import(NewParrisCollapsed, FilterMode.Bilinear, true, 128f);
        }

        private static void Import(string path, FilterMode filter, bool single = false, float ppu = 64f)
        {
            if (!System.IO.File.Exists(path) || !(AssetImporter.GetAtPath(path) is TextureImporter ti)) return;
            // Una imagen suelta en modo Multiple con cortes viejos (de un archivo reemplazado) no da sprite o da uno equivocado
            bool modeOk = !single || ti.spriteImportMode == SpriteImportMode.Single;
            if (modeOk && ti.textureType == TextureImporterType.Sprite && ti.filterMode == filter && ti.textureCompression == TextureImporterCompression.Uncompressed && Mathf.Approximately(ti.spritePixelsPerUnit, ppu)) return;
            SpriteImportDefaults.Apply(ti);
            if (single) ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = ppu;
            ti.filterMode = filter;
            ti.SaveAndReimport();
        }

        private static void Slice(string path, int cellW, int cellH, FilterMode filter)
        {
            if (!System.IO.File.Exists(path)) return;
            if (AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Count() <= 1) SpriteSheetSlicer.SliceSheet(path, cellW, cellH);
            Import(path, filter);
        }

        private static Sprite Named(string pathNoExt, string name) => L3.PackSprite(pathNoExt, name);

        private static GameObject LoadPrefab(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

        /// <summary>Escalera trepable con visual: el atlas de Cainos viene troceado, así que se usa la escalera completa del Nivel 2 en modo Tiled.</summary>
        private static void Ladder(Transform parent, string name, float cx, float bottomY, float topY, Collider2D topPlatform)
        {
            LadderInteractable li = L3.CreateClimbableLadder(parent, _cainos, name, cx, bottomY, topY, topPlatform);
            if (li.GetComponent<SpriteRenderer>() != null) return;
            SpriteRenderer sr = li.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Load("Assets/Art/Environment/Level2/Cainos_Ladder.png");
            sr.sortingLayerName = "Gameplay";
            sr.sortingOrder = 2;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Adaptive;
            sr.adaptiveModeThreshold = 0.5f;
            sr.size = new Vector2(0.85f, Mathf.Max(1.5f, topY - bottomY) + 0.3f);
        }

        /// <summary>Sprite con la base visible en baseY, centrado en cx y escalado a la altura dada.</summary>
        private static SpriteRenderer Fit(Transform parent, string name, Sprite sp, float cx, float baseY, float height, string layer, int order, Color tint, bool flip = false)
        {
            if (sp == null) { Debug.LogWarning($"[Level4SceneBuilder] Falta sprite para {name}"); return null; }
            L3.VisibleExtents(sp, out float vx0, out float vx1, out float vy0, out float vy1);
            float w = height * (vx1 - vx0) / Mathf.Max(0.01f, vy1 - vy0);
            return L3.AddFitted(parent, name, sp, new Vector2(cx - w * 0.5f, baseY), new Vector2(cx + w * 0.5f, baseY + height), layer, order, tint, flip);
        }

        /// <summary>
        /// Suelo marchito (set seco de DarkForest/env_ground): franja de pasto muerto en mosaico, fila de tierra
        /// agrietada debajo y relleno oscuro. Collider con cara superior exacta en topY.
        /// </summary>
        private static void WitheredGround(Transform parent, string name, float x0, float x1, float topY, int order)
        {
            GameObject g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.layer = _ground;
            g.transform.position = new Vector3((x0 + x1) * 0.5f, topY - 2.085f, 0f);
            g.AddComponent<BoxCollider2D>().size = new Vector2(x1 - x0, 4.17f);

            Sprite custom = Opt(NewGround);
            if (custom != null)
            {
                // Tile propio 4x2u: la superficie pisable está 16 px (0.25u) bajo el borde superior
                int n = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / 4f));
                float w = (x1 - x0) / n;
                for (int i = 0; i < n; i++)
                    L3.AddFitted(g.transform, "Withered_Ground_Tile", custom, new Vector2(x0 + i * w, topY - 1.75f), new Vector2(x0 + (i + 1) * w + 0.01f, topY + 0.25f), "Midground", order + 1, Color.white);
                L3.AddFitted(g.transform, "Dirt_Fill", Load(Env + "Placeholder_Square.png"), new Vector2(x0, topY - 4.2f), new Vector2(x1, topY - 1.7f), "Midground", order, new Color(0.03f, 0.02f, 0.02f, 1f));
                return;
            }

            Sprite strip = Named(DF + "env_ground", "env_ground_32");
            Sprite dirt = Named(DF + "env_ground", "env_ground_40");
            Color dry = new Color(0.86f, 0.78f, 0.7f, 1f);
            const float s = 1.5f; // mismo factor de píxel que el resto del arte DarkForest
            float sw = strip.rect.width / strip.pixelsPerUnit * s, sh = strip.rect.height / strip.pixelsPerUnit * s;
            float dw = dirt.rect.width / dirt.pixelsPerUnit * s;
            for (float x = x0; x < x1; x += sw)
                L3.AddFitted(g.transform, "Withered_Topsoil", strip, new Vector2(x, topY - sh + 0.14f), new Vector2(Mathf.Min(x + sw, x1 + 0.3f), topY + 0.14f), "Midground", order + 1, dry);
            for (float x = x0; x < x1; x += dw)
                L3.AddFitted(g.transform, "Cracked_Dirt", dirt, new Vector2(x, topY - sh - dw + 0.2f), new Vector2(x + dw + 0.01f, topY - sh + 0.2f), "Midground", order, dry);
            L3.AddFitted(g.transform, "Dirt_Fill", Load(Env + "Placeholder_Square.png"), new Vector2(x0, topY - 4.2f), new Vector2(x1, topY - sh - dw + 0.25f), "Midground", order, new Color(0.1f, 0.07f, 0.05f, 1f));
        }

        /// <summary>Raíces secas, espinos y matojos muertos sobre el suelo marchito.</summary>
        private static void WitheredPlants(Transform parent, params float[] xs)
        {
            string[] names = { "env_ground_21", "env_ground_28", "env_ground_24", "env_ground_27" };
            for (int i = 0; i < xs.Length; i++)
            {
                float baseY = (xs[i] > 23f && xs[i] < 34f ? TerraceY : G) - 0.1f;
                Fit(parent, "Withered_Plant", Named(DF + "env_ground", names[i % names.Length]), xs[i], baseY, i % names.Length == 2 ? 0.7f : 0.85f, "Midground", 3, new Color(0.8f, 0.72f, 0.62f), i % 2 == 1);
            }
        }

        /// <summary>Muro de ladrillo en mosaico (sin estirar el pixel art) dentro de [x0,x1]x[y0,y1].</summary>
        private static void BrickWall(Transform parent, string name, float x0, float x1, float y0, float y1, Sprite brick, int order, Color tint, string layer = "Midground")
        {
            if (brick == null) return;
            Transform wall = L3.NewChild(parent, name);
            float tw = brick.rect.width / brick.pixelsPerUnit, th = brick.rect.height / brick.pixelsPerUnit;
            int nx = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / tw)), ny = Mathf.Max(1, Mathf.RoundToInt((y1 - y0) / th));
            float sw = (x1 - x0) / nx, sh = (y1 - y0) / ny;
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < ny; j++)
                    L3.AddFitted(wall, $"Brick_{i}_{j}", brick, new Vector2(x0 + i * sw, y0 + j * sh), new Vector2(x0 + (i + 1) * sw + 0.01f, y0 + (j + 1) * sh + 0.01f), layer, order, tint);
        }

        /// <summary>Mausoleo de fondo con tejado de losa al que se puede subir (repisa de un sentido).</summary>
        private static readonly Color MausoleumTint = new Color(0.72f, 0.82f, 0.76f, 1f);

        private static void Mausoleum(Transform parent, string name, Sprite front, float cx, float baseY, float height)
        {
            Transform m = L3.NewChild(parent, name);
            bool custom = front == Opt(NewMausoleum);
            SpriteRenderer sr = Fit(m, "Front", front, cx, baseY - 0.05f, height + 0.05f, "Midground", 2, custom ? MausoleumTint : NightStone);
            float x0 = sr.bounds.min.x, x1 = sr.bounds.max.x;
            if (custom)
            {
                L3.CreateOneWayLedge(m, "Roof_Ledge", x0 + 0.1f, x1 - 0.1f, baseY + height - 0.08f, _ground, null, false);
                return;
            }
            Fit(m, "Column_L", Named(DF + "env_mon", "env_mon_4"), x0 - 0.2f, baseY - 0.05f, height, "Midground", 3, NightStone);
            Fit(m, "Column_R", Named(DF + "env_mon", "env_mon_5"), x1 + 0.2f, baseY - 0.05f, height, "Midground", 3, NightStone);
            x0 -= 0.5f;
            x1 += 0.5f;
            L3.AddFitted(m, "Roof_Slab", Named(DF + "castle_env", "castle_env_22"), new Vector2(x0 - 0.4f, baseY + height - 0.1f), new Vector2(x1 + 0.4f, baseY + height + 0.4f), "Gameplay", 2, NightStone);
            L3.CreateOneWayLedge(m, "Roof_Ledge", x0 - 0.3f, x1 + 0.3f, baseY + height + 0.38f, _ground, null, false);
        }

        private static void Graves(Transform parent, float baseY, params float[] xs)
        {
            string[] names = { "TX Village Props - Gravestone 01", "TX Village Props - Gravestone 02", "TX Village Props - Gravestone 03", "TX Village Props - Gravestone 04" };
            Sprite[] stones = Frames(NewTombstones, 0, 1, 2, 3, 4, 5);
            if (stones == null || stones.Length == 0) stones = names.Select(n => _cainos.FirstOrDefault(s => s.name == n)).ToArray();
            bool custom = System.IO.File.Exists(NewTombstones);
            for (int i = 0; i < xs.Length; i++)
            {
                Sprite sp = stones[(i + Mathf.RoundToInt(xs[i])) % stones.Length];
                float h = (custom ? 1.3f : 0.9f) + (i % 3) * 0.18f;
                Fit(parent, "Gravestone", sp, xs[i], baseY - 0.08f, h, "Midground", 3, custom ? Color.white : Moss, i % 2 == 1);
            }
        }

        private static void IronRailing(Transform parent, float x0, float x1)
        {
            Sprite rail = Named(DF + "env_mon", "env_mon_14");
            for (float x = x0 + 0.6f; x < x1; x += 1.15f)
                Fit(parent, "Grave_IronRailing", rail, x, G - 0.05f, 0.72f, "Midground", 4, NightStone);
        }

        private static void PlaceCainos(Transform parent, string name, string sprite, float x, float height)
        {
            Fit(parent, name, _cainos.FirstOrDefault(s => s.name == sprite), x, G - 0.05f, height, "Gameplay", 1, Moss);
        }

        private static void Hedge(Transform parent, string name, float cx, float baseY, float width)
        {
            Sprite b1 = _cainos.FirstOrDefault(s => s.name == "TX Village Props - Bush 01");
            Sprite b2 = _cainos.FirstOrDefault(s => s.name == "TX Village Props - Bush 02");
            Sprite b3 = _cainos.FirstOrDefault(s => s.name == "TX Village Props - Bush 03");
            L3.CreateForestHideout(parent, name, cx, baseY, width, 1.6f, null, b1, b2, b3);
            Transform h = parent.Find(name);
            // Seto denso detrás (follaje de fondo) + hojas delanteras teñidas de noche
            for (float x = -width * 0.5f + 0.4f; x <= width * 0.5f - 0.3f; x += 0.75f)
                Fit(h, "Hedge_Back", b1, cx + x, baseY - 0.1f, 1.3f, "Midground", 4, new Color(0.32f, 0.5f, 0.4f));
            foreach (SpriteRenderer sr in h.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.sortingLayerName == "Foreground") sr.color = new Color(0.45f, 0.65f, 0.52f, 0.7f);
        }

        private static void Guard(Transform parent, string name, float xa, float xb, float y)
        {
            Transform[] wps = L3.CreateWaypoints(parent, "Waypoints_" + name, new Vector2(xa, y), new Vector2(xb, y));
            L3.CreateGuard(parent, LoadPrefab("Assets/Prefabs/Guard.prefab"), name, new Vector3((xa + xb) * 0.5f, y, 0f), wps, PatrolMode.PingPong, _groundMask);
        }

        private static Light2D PointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float radius, bool flicker)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            Light2D l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.pointLightInnerRadius = radius * 0.12f;
            l.pointLightOuterRadius = radius;
            l.falloffIntensity = 0.55f;
            if (flicker) go.AddComponent<LanternFlicker>();
            return l;
        }

        /// <summary>Tira continua de niebla tileable entre x0 y x1 (tramos de ~6u pegados), con vaivén en bloque.</summary>
        private static void FogBand(Transform parent, Sprite strip, float x0, float x1, float y, float height, Color color, string layer, int order, float drift)
        {
            GameObject band = new GameObject("Fog_Band");
            band.transform.SetParent(parent, false);
            int n = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / 6f));
            float w = (x1 - x0) / n;
            for (int i = 0; i < n; i++)
            {
                SpriteRenderer sr = L3.AddFitted(band.transform, "Fog_Tile", strip, new Vector2(x0 + i * w, y - height * 0.5f), new Vector2(x0 + (i + 1) * w + 0.02f, y + height * 0.5f), layer, order, color);
            }
            var fd = band.AddComponent<FogDrift>();
            var so = new SerializedObject(fd);
            so.FindProperty("amplitude").floatValue = drift;
            so.FindProperty("speed").floatValue = Random.Range(0.12f, 0.24f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void FogBlob(Transform parent, Sprite fog, Vector3 pos, Vector2 size, Color color, string layer, int order, float drift)
        {
            GameObject go = new GameObject("Fog_Bank");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(size.x / fog.bounds.size.x, size.y / fog.bounds.size.y, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = fog;
            sr.color = color;
            sr.sortingLayerName = layer;
            sr.sortingOrder = order;
            var fd = go.AddComponent<FogDrift>();
            var so = new SerializedObject(fd);
            so.FindProperty("amplitude").floatValue = drift;
            so.FindProperty("speed").floatValue = Random.Range(0.12f, 0.24f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Carpet(Transform parent, string name, float x0, float x1, Sprite square)
        {
            // shortcut: sin Carpet_Velvet_Runner.png la alfombra son rectángulos teñidos; se sustituye solo al existir el archivo
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3((x0 + x1) * 0.5f, G + 0.15f, 0f);
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(x1 - x0, 0.5f);
            go.AddComponent<SilentCarpet>();
            Sprite runner = Opt(NewCarpet);
            if (runner != null)
            {
                // Tira tileable en tramos iguales (~2u); alto según la proporción del arte para no aplastar los píxeles
                int n = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / 2f));
                float w = (x1 - x0) / n, h = w * Aspect(runner);
                for (int i = 0; i < n; i++)
                    L3.AddFitted(go.transform, "Velvet_Runner", runner, new Vector2(x0 + i * w, G + 0.1f - h), new Vector2(x0 + (i + 1) * w + 0.01f, G + 0.1f), "Gameplay", 1, Color.white);
                return;
            }
            L3.AddFitted(go.transform, "Velvet", square, new Vector2(x0, G - 0.06f), new Vector2(x1, G + 0.05f), "Gameplay", 1, new Color(0.45f, 0.06f, 0.09f));
            L3.AddFitted(go.transform, "Gold_Trim", square, new Vector2(x0, G + 0.03f), new Vector2(x1, G + 0.06f), "Gameplay", 1, new Color(0.78f, 0.6f, 0.22f));
        }

        private static void CreakyTile(Transform parent, string name, float x0, float x1, Sprite planks, AudioClip sfx)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3((x0 + x1) * 0.5f, G + 0.2f, 0f);
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(x1 - x0, 0.5f);
            var creak = go.AddComponent<CreakyFloorTile>();
            var so = new SerializedObject(creak);
            so.FindProperty("creakSfx").objectReferenceValue = sfx;
            so.ApplyModifiedPropertiesWithoutUndo();
            Sprite board = Opt(NewCreaky);
            if (board != null)
            {
                L3.AddFitted(go.transform, "Creaky_Floorboard", board, new Vector2(x0, G + 0.04f - (x1 - x0) * Aspect(board)), new Vector2(x1, G + 0.04f), "Gameplay", 1, Color.white);
                return;
            }
            // shortcut: duela floja = tablón aclarado y levantado; se sustituye solo al existir Creaky_Floorboard.png
            SpriteRenderer sr = L3.AddFitted(go.transform, "Loose_Plank", planks, new Vector2(x0, G - 0.3f), new Vector2(x1, G + 0.04f), "Gameplay", 1, new Color(0.95f, 0.78f, 0.55f));
            sr.transform.rotation = Quaternion.Euler(0f, 0f, 2.5f);
        }

        private static void RegisterInBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(s => s.path == Level4Path);
            int idx = scenes.FindIndex(s => s.path == L3.Level3VillagePath);
            var entry = new EditorBuildSettingsScene(Level4Path, true);
            if (idx >= 0) scenes.Insert(idx + 1, entry);
            else scenes.Add(entry);
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
