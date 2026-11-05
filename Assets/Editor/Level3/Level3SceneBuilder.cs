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
using UnityEngine.UI;

namespace Level3.Editor
{
    /// <summary>
    /// Generador automatizado e idempotente del Nivel 3, dividido en 2 escenas:
    /// - Parte 1 (Assets/Scenes/Level3.unity): Acto 1 Sendero del Bosque (X 0..115) y Acto 2 Cabaña de la Bruja
    ///   (X 112..158) con descanso al entrar, Caldero de la Poción Maestra y salida por el Portón del Sendero de Regreso.
    /// - Parte 2 (Assets/Scenes/Level3_Village.unity): Acto 3 la Aldea de Salem y su Iglesia (X 141..266).
    /// Ambas comparten la infraestructura base (Cámara, Luces 2D, Managers, HUD, Level3Bootstrap,
    /// Level3StateController, KeyInventory, InteractionPromptUI y tarjeta de capítulo).
    /// </summary>
    public static class Level3SceneBuilder
    {
        public const string Level3Path = "Assets/Scenes/Level3.unity";
        public const string Level3VillagePath = "Assets/Scenes/Level3_Village.unity";

        // Límites de cada parte (X del mundo; la Parte 2 conserva las coordenadas originales del Acto 3)
        private const float Part1SpawnX = 2f;
        private const float Part1EastWallX = 226f;
        private const float Part1ExitX = 218f;
        private const float Part2SpawnX = 145.6f;
        private const float Part2WestWallX = 142.5f;
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
        private const string FringeSpritePath = EnvPath + "Env_Ground_Fringe.png";
        private const string Level2ArtPath = EnvPath + "Level2/";
        private const string PlaceholderSquarePath = EnvPath + "Placeholder_Square.png";
        private const string FogSpritePath = EnvPath + "Level3/FX_Level3_SoftFog.png";
        private const string DarkForestPath = EnvPath + "DarkForest/";
        private const string ParallaxForestPath = EnvPath + "ParallaxForest/";
        private const string CainosAtlasPath = "Assets/Cainos/Pixel Art Platformer - Village Props/Texture/TX Village Props.png";
        private const string CainosChestAtlasPath = "Assets/Cainos/Pixel Art Platformer - Village Props/Texture/TX Chest Animation.png";
        private const string VillagerPreacherPrefabPath = "Assets/Prefabs/VillagerPreacher.prefab";

        [MenuItem("Tools/Level 3/Build Complete Level 3 Scene")]
        public static void BuildCompleteLevel3Scene()
        {
            BuildLevel3Part(1);
            BuildLevel3Part(2);
            RegisterLevel3InBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(Level3Path, OpenSceneMode.Single);
        }

        [MenuItem("Tools/Level 3/Build Part 1 (Forest + Cabin)")]
        public static void BuildLevel3Part1Only()
        {
            BuildLevel3Part(1);
            RegisterLevel3InBuildSettings();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Level 3/Build Part 2 (Village)")]
        public static void BuildLevel3Part2Only()
        {
            BuildLevel3Part(2);
            RegisterLevel3InBuildSettings();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Construye y guarda una de las 2 escenas del Nivel 3 (1 = Bosque y Cabaña, 2 = Aldea).</summary>
        private static void BuildLevel3Part(int part)
        {
            bool village = part == 2;
            string scenePath = village ? Level3VillagePath : Level3Path;
            var log = new StringBuilder();
            log.AppendLine(village
                ? "=== CONSTRUCCIÓN AUTOMATIZADA DE LEVEL 3 — PARTE 2 (LA ALDEA DE SALEM) ==="
                : "=== CONSTRUCCIÓN AUTOMATIZADA DE LEVEL 3 — PARTE 1 (BOSQUE Y CABAÑA) ===");

            ProjectSetup.Configure();
            EnsurePixelArtImport(DarkForestPath);
            EnsurePixelArtImport(ParallaxForestPath);

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
                witch.transform.position = new Vector3(village ? Part2SpawnX : Part1SpawnX, 1f, 0f);

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
            SetupLevel3ManagersAndUI(roots["Managers"], roots["UI"], camCopy, witch, village);

            // ------------------------------------------------------------------
            // 4. Fondo Parallax de Bosque Nocturno
            // ------------------------------------------------------------------
            BuildForestParallax(roots["Background"], village);

            if (!village)
            {
                // ------------------------------------------------------------------
                // 5. Parte 1 — Acto 1: El Sendero del Bosque (X: 0 a X: 115)
                // ------------------------------------------------------------------
                BuildAct1ForestTrail(roots["Environment"], roots["Gameplay"], roots["NPCs"], log);

                // ------------------------------------------------------------------
                // 6. Parte 1 — Acto 2: El Claro, Cabaña (X: 112..146) y Desfiladero de las Sombras (X: 146..226)
                // ------------------------------------------------------------------
                BuildAct2WitchCabinAndClearing(roots["Environment"], roots["Gameplay"], roots["NPCs"], log);

                // Árboles retorcidos, rocas, ruinas, hierba y flores distintos en cada bioma
                DecorateForestBiomes(roots["Environment"]);
            }
            else
            {
                // ------------------------------------------------------------------
                // 7. Parte 2 — Acto 3: La Aldea de Salem y su Iglesia (X: 141 a X: 266)
                // ------------------------------------------------------------------
                BuildAct3ChurchDistrict(roots["Environment"], roots["Gameplay"], roots["NPCs"], log);
            }

            // ------------------------------------------------------------------
            // 8. Conectar CaptureListener a todos los guardias creados
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
            EditorSceneManager.SaveScene(l3, scenePath);

            Debug.Log($"[Level3SceneBuilder] Escena {scenePath} construida exitosamente.\n" + log);
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

            PotionInventory potionInv = witch.GetComponent<PotionInventory>();
            if (potionInv == null)
            {
                potionInv = witch.AddComponent<PotionInventory>();
            }
            GameObject masterProjPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PotionProjectile_Master.prefab");
            if (masterProjPrefab != null)
            {
                SerializedObject soPInv = new SerializedObject(potionInv);
                soPInv.FindProperty("masterPotionPrefab").objectReferenceValue = masterProjPrefab;
                soPInv.ApplyModifiedPropertiesWithoutUndo();
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

        private static void SetupLevel3ManagersAndUI(Transform managersRoot, Transform uiRoot, GameObject camCopy, GameObject witch, bool village)
        {
            // Level3Bootstrap
            GameObject bootGo = new GameObject("Level3_Bootstrap");
            bootGo.transform.SetParent(managersRoot, false);
            bootGo.AddComponent<Level3Bootstrap>();

            // Level3StateController
            GameObject stateGo = new GameObject("Level3_StateController");
            stateGo.transform.SetParent(managersRoot, false);
            Level3StateController stateCtrl = stateGo.AddComponent<Level3StateController>();
            var soState = new SerializedObject(stateCtrl);
            soState.FindProperty("villagePart").boolValue = village;
            soState.ApplyModifiedPropertiesWithoutUndo();

            // KeyInventory
            GameObject keyInvGo = new GameObject("KeyInventory");
            keyInvGo.transform.SetParent(managersRoot, false);
            keyInvGo.AddComponent<KeyInventory>();

            // InteractionPromptUI
            GameObject promptGo = new GameObject("InteractionPrompt_Canvas");
            promptGo.transform.SetParent(uiRoot, false);
            InteractionPromptUI promptUI = promptGo.AddComponent<InteractionPromptUI>();
            promptUI.EnsureUIHierarchy();

            // Panel de Objetivos del Nivel 3 (fase, portones, pistas, cofres, llaves y Poción Maestra)
            GameObject objectiveHudGo = new GameObject("Level3ObjectiveHUD_Canvas", typeof(RectTransform));
            objectiveHudGo.transform.SetParent(uiRoot, false);
            objectiveHudGo.layer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 0;
            Level3ObjectiveHUD objectiveHud = objectiveHudGo.AddComponent<Level3ObjectiveHUD>();
            objectiveHud.EnsureUIHierarchy();

            // Lector de notas: pergamino con pausa y "[Espacio] Cerrar"
            BuildLevel3LoreReader(uiRoot);

            // Tarjeta de capítulo: descanso en la cabaña / fin de la Parte 1 / presentación de la Parte 2
            GameObject cardGo = new GameObject("Level3ChapterCard_Canvas", typeof(RectTransform));
            cardGo.transform.SetParent(uiRoot, false);
            cardGo.layer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 0;
            Level3ChapterCard card = cardGo.AddComponent<Level3ChapterCard>();
            card.Configure(
                village,
                "NIVEL 3  ·  PARTE 2",
                "La Aldea de Salem",
                "Descansaste en la cabaña y la Poción Maestra está lista. La aldea duerme: infíltrate como Gato [T] en las casas atrancadas.");
            card.EnsureUIHierarchy();
            CanvasGroup cardGroup = cardGo.GetComponent<CanvasGroup>();
            if (cardGroup != null) cardGroup.alpha = 0f; // invisible en el editor; en juego decide Awake()

            HUDController hudCtrl = uiRoot.GetComponentInChildren<HUDController>(true);
            if (hudCtrl != null)
            {
                hudCtrl.EnsureFourPotionSlots();
            }

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
                camCopy.transform.position = new Vector3(village ? Part2SpawnX : Part1SpawnX, 2.2f, -10f);
                CameraFollow follow = camCopy.GetComponent<CameraFollow>();
                if (follow != null)
                {
                    follow.SetTarget(witch.transform);
                    var so = new SerializedObject(follow);
                    var minProp = so.FindProperty("minLimit");
                    var maxProp = so.FindProperty("maxLimit");
                    var useLimitsProp = so.FindProperty("useLimits");
                    if (useLimitsProp != null) useLimitsProp.boolValue = true;
                    if (minProp != null) minProp.vector2Value = village ? new Vector2(Part2WestWallX - 1f, -1f) : new Vector2(-2f, -1f);
                    if (maxProp != null) maxProp.vector2Value = village ? new Vector2(268f, 18f) : new Vector2(Part1EastWallX - 0.5f, 18f);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void BuildForestParallax(Transform bgRoot, bool villagePart)
        {
            var village = new List<SpriteRenderer>();

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

            // Parte 1: 3 biomas de bosque distintos (packs DarkForest y ParallaxForest) en lugar de un único fondo repetido
            var zones = new List<Level3BiomeBackdrop.BackdropZone>();
            if (!villagePart)
            {
                BuildForestBiomeLayers(bgRoot, zones);
            }

            // Fase 2: silueta de la Aldea de Salem (se funde con el bosque al salir de la cabaña)
            Sprite villageFar = LoadFirstSprite(Level2ArtPath + "Level2_Village_Far.png");
            if (villageFar != null)
            {
                GameObject farObj = new GameObject("Village_Far");
                farObj.transform.SetParent(bgRoot, false);
                farObj.transform.position = new Vector3(0f, 3.3f, 0f);
                farObj.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
                ParallaxUtility.SetupThreeCopies(farObj, villageFar, "Background", villagePart ? 1 : 14);
                ParallaxLayer farPl = farObj.GetComponent<ParallaxLayer>();
                if (farPl != null) farPl.Setup(0.82f, true, true);
                foreach (SpriteRenderer sr in farObj.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    sr.color = new Color(0.46f, 0.46f, 0.66f, 1f);
                    village.Add(sr);
                }
            }

            Sprite villageMid = LoadFirstSprite(Level2ArtPath + "Env_Village_Mid.png");
            if (villageMid != null)
            {
                GameObject midObj = new GameObject("Village_Mid");
                midObj.transform.SetParent(bgRoot, false);
                midObj.transform.position = new Vector3(0f, -0.7f, 0f);
                midObj.transform.localScale = new Vector3(0.78f, 0.78f, 1f);
                ParallaxUtility.SetupThreeCopies(midObj, villageMid, "Background", villagePart ? 3 : 15);
                ParallaxLayer midPl = midObj.GetComponent<ParallaxLayer>();
                if (midPl != null) midPl.Setup(0.55f, true, true);
                foreach (SpriteRenderer sr in midObj.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    sr.color = new Color(0.6f, 0.58f, 0.78f, 1f);
                    village.Add(sr);
                }
            }

            // Parte 1: el bosque empieza a fundirse con la silueta de la aldea al final del Sendero de Regreso.
            // Parte 2: solo la aldea, sin fundido.
            if (!villagePart)
            {
                zones.Add(new Level3BiomeBackdrop.BackdropZone
                {
                    name = "Aldea de Salem (silueta)",
                    renderers = village.ToArray(),
                    fadeInStartX = 175f,
                    fadeInEndX = 226f,
                    fadeOut = false
                });
                Level3BiomeBackdrop backdrop = bgRoot.gameObject.AddComponent<Level3BiomeBackdrop>();
                backdrop.ConfigureZones(zones.ToArray());
            }
        }

        // =========================================================================
        // BIOMAS DEL BOSQUE (PARTE 1): packs DarkForest (Szadi art) y ParallaxForest
        // =========================================================================

        /// <summary>Los packs son pixel art: filtro Point y sin compresión para que no se vean borrosos al escalarlos.</summary>
        private static void EnsurePixelArtImport(string folder)
        {
            string dir = folder.TrimEnd('/');
            if (!AssetDatabase.IsValidFolder(dir)) return;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                if (ti.filterMode == FilterMode.Point && ti.textureCompression == TextureImporterCompression.Uncompressed && !ti.mipmapEnabled) continue;
                ti.filterMode = FilterMode.Point;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }
        }

        private static Sprite PackSprite(string pathNoExt, string spriteName = null)
        {
            Sprite[] all = AssetDatabase.LoadAllAssetsAtPath(pathNoExt + ".png").OfType<Sprite>().ToArray();
            if (all.Length == 0) return null;
            return string.IsNullOrEmpty(spriteName) ? all[0] : all.FirstOrDefault(sp => sp.name == spriteName);
        }

        /// <summary>Franja de paralaje infinita (3 copias) teñida; devuelve sus renderers para el fundido por zona.</summary>
        private static List<SpriteRenderer> AddParallaxBand(Transform bgRoot, string name, Sprite sp, float centerY, float scale, int order, float factor, Color tint)
        {
            var list = new List<SpriteRenderer>();
            if (sp == null) return list;
            GameObject go = new GameObject(name);
            go.transform.SetParent(bgRoot, false);
            go.transform.position = new Vector3(0f, centerY, 0f);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            ParallaxUtility.SetupThreeCopies(go, sp, "Background", order);
            ParallaxLayer pl = go.GetComponent<ParallaxLayer>();
            if (pl != null) pl.Setup(factor, true, true);
            foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.color = tint;
                list.Add(sr);
            }
            return list;
        }

        /// <summary>Silueta lejana única (sin repetir) que aparece en worldX cuando la cámara está en anchorCamX.</summary>
        private static SpriteRenderer AddParallaxLandmark(Transform bgRoot, string name, Sprite sp, float anchorCamX, float worldX, float y, float scale, int order, float factor, Color tint)
        {
            if (sp == null) return null;
            GameObject go = new GameObject(name);
            go.transform.SetParent(bgRoot, false);
            go.transform.position = new Vector3(worldX - anchorCamX * factor, y, 0f);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.color = tint;
            sr.sortingLayerName = "Background";
            sr.sortingOrder = order;
            ParallaxLayer pl = go.AddComponent<ParallaxLayer>();
            pl.Setup(factor, false, true);
            return sr;
        }

        /// <summary>
        /// 3 biomas con transición suave según la cámara:
        /// - Bosque Dorado (X 0..50): ParallaxForest v1, troncos otoñales en 3 capas.
        /// - Bosque Muerto (X 44..116, barranco y bastión): siluetas retorcidas de DarkForest y colina-cementerio flotante.
        /// - Claro de la Cabaña (X 106..158): ParallaxForest v2, haces de luz de luna entre árboles rojizos.
        /// </summary>
        private static void BuildForestBiomeLayers(Transform bgRoot, List<Level3BiomeBackdrop.BackdropZone> zones)
        {
            const float fullScale = 4.3f; // 272x160 px a 64 ppu -> 18.3 x 10.75 u, cubre toda la vista

            // --- Bosque Dorado ---
            var golden = new List<SpriteRenderer>();
            golden.AddRange(AddParallaxBand(bgRoot, "Biome_Golden_Back", PackSprite(ParallaxForestPath + "v1/parallax-forest-back-trees"), 4.2f, fullScale, 2, 0.88f, new Color(0.80f, 0.62f, 0.62f, 1f)));
            golden.AddRange(AddParallaxBand(bgRoot, "Biome_Golden_Middle", PackSprite(ParallaxForestPath + "v1/parallax-forest-middle-trees"), 4.2f, fullScale, 3, 0.72f, new Color(0.72f, 0.58f, 0.64f, 1f)));
            golden.AddRange(AddParallaxBand(bgRoot, "Biome_Golden_Front", PackSprite(ParallaxForestPath + "v1/parallax-forest-front-trees"), 4.0f, fullScale, 4, 0.5f, new Color(0.58f, 0.52f, 0.66f, 1f)));
            zones.Add(new Level3BiomeBackdrop.BackdropZone
            {
                name = "Bosque Dorado",
                renderers = golden.ToArray(),
                fadeIn = false,
                fadeOutStartX = 44f,
                fadeOutEndX = 54f
            });

            // --- Bosque Muerto ---
            var dead = new List<SpriteRenderer>();
            SpriteRenderer graveyard = AddParallaxLandmark(bgRoot, "Biome_Dead_GraveyardHill",
                PackSprite(DarkForestPath + "top_far_bgrnd", "top_far_bgrnd_1"), 88f, 91f, 7.5f, 1.35f, 5, 0.9f, new Color(0.12f, 0.09f, 0.19f, 0.85f));
            if (graveyard != null) dead.Add(graveyard);
            dead.AddRange(AddParallaxBand(bgRoot, "Biome_Dead_Ridge", PackSprite(DarkForestPath + "top_far_bgrnd", "top_far_bgrnd_0"), 1.5f, 2.2f, 6, 0.8f, new Color(0.15f, 0.12f, 0.22f, 1f)));
            dead.AddRange(AddParallaxBand(bgRoot, "Biome_Dead_TreesFar", PackSprite(DarkForestPath + "bgrd_tree1"), 2.6f, 1.45f, 7, 0.68f, new Color(0.22f, 0.17f, 0.30f, 1f)));
            dead.AddRange(AddParallaxBand(bgRoot, "Biome_Dead_TreesMid", PackSprite(DarkForestPath + "bgrd_tree3"), 2.7f, 1.55f, 8, 0.52f, new Color(0.17f, 0.13f, 0.24f, 0.85f)));
            zones.Add(new Level3BiomeBackdrop.BackdropZone
            {
                name = "Bosque Muerto",
                renderers = dead.ToArray(),
                fadeInStartX = 44f,
                fadeInEndX = 54f,
                fadeOutStartX = 106f,
                fadeOutEndX = 116f
            });

            // --- Claro de la Cabaña ---
            var clearing = new List<SpriteRenderer>();
            clearing.AddRange(AddParallaxBand(bgRoot, "Biome_Clearing_Back", PackSprite(ParallaxForestPath + "v2/back"), 4.4f, fullScale, 10, 0.88f, new Color(0.84f, 0.68f, 0.80f, 1f)));
            clearing.AddRange(AddParallaxBand(bgRoot, "Biome_Clearing_Middle", PackSprite(ParallaxForestPath + "v2/middle"), 4.3f, fullScale, 11, 0.7f, new Color(0.68f, 0.56f, 0.72f, 1f)));
            clearing.AddRange(AddParallaxBand(bgRoot, "Biome_Clearing_Front", PackSprite(ParallaxForestPath + "v2/front"), 4.1f, fullScale, 12, 0.5f, new Color(0.40f, 0.36f, 0.50f, 1f)));
            zones.Add(new Level3BiomeBackdrop.BackdropZone
            {
                name = "Claro de la Cabaña",
                renderers = clearing.ToArray(),
                fadeInStartX = 106f,
                fadeInEndX = 116f,
                fadeOutStartX = 146f,
                fadeOutEndX = 158f
            });
        }

        /// <summary>Decoración sin colisión apoyada en el suelo (fondo de la capa Midground, detrás del jugador).</summary>
        private static SpriteRenderer PlaceForestDecor(Transform parent, string name, Sprite sp, float x, float scale, bool flip, int order, Color tint, float sink)
        {
            if (sp == null) return null;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.flipX = flip;
            sr.color = tint;
            sr.sortingLayerName = "Midground";
            sr.sortingOrder = order;
            float bottomOffset = (sp.pivot.y / sp.pixelsPerUnit) * scale;
            go.transform.position = new Vector3(x, StreetY - sink + bottomOffset, 0f);
            return sr;
        }

        private static void DecorateForestBiomes(Transform envRoot)
        {
            Transform root = NewChild(envRoot, "Forest_Biome_Decor");
            string trees = DarkForestPath + "env_trees";
            string rocks = DarkForestPath + "env_rock";
            string ruins = DarkForestPath + "env_mon";
            string tufts = DarkForestPath + "top_far_bgrnd";

            Color treeTint = new Color(1f, 0.92f, 0.95f, 1f);
            Color rockTint = new Color(1f, 0.94f, 0.96f, 1f);
            Color ruinTint = new Color(0.74f, 0.68f, 0.86f, 1f); // algo más oscuras: decorado de fondo, no plataformas

            // Árboles retorcidos (cada bioma con su mezcla, tamaños y orientación distintos)
            var treeSpots = new (string sprite, float x, float scale, bool flip)[]
            {
                // Bosque Dorado
                ("env_trees_6", 9.5f, 1.9f, false), ("env_trees_1", 21.6f, 1.7f, true), ("env_trees_5", 28.6f, 1.6f, false),
                // Bosque Muerto
                ("env_trees_0", 72.6f, 1.45f, false), ("env_trees_6", 79.6f, 1.8f, true), ("env_trees_1", 92.4f, 1.9f, false), ("env_trees_5", 103.6f, 1.7f, true),
                // Claro de la Cabaña
                ("env_trees_5", 113.0f, 1.5f, false), ("env_trees_6", 140.6f, 1.7f, true), ("env_trees_1", 155.6f, 1.7f, true),
            };
            foreach (var t in treeSpots)
            {
                PlaceForestDecor(root, $"TwistedTree_{t.x:F0}", PackSprite(trees, t.sprite), t.x, t.scale, t.flip, -2, treeTint, 0.12f);
            }

            // Rocas y ruinas del Bosque Muerto (un antiguo santuario derrumbado tras el bastión)
            PlaceForestDecor(root, "Rock_Boulder_24", PackSprite(rocks, "env_rock_13"), 24.8f, 0.7f, false, -1, rockTint, 0.1f);
            PlaceForestDecor(root, "Rock_Wide_70", PackSprite(rocks, "env_rock_14"), 70.4f, 1.1f, false, -1, rockTint, 0.12f);
            PlaceForestDecor(root, "Rock_Boulder_88", PackSprite(rocks, "env_rock_12"), 88.4f, 0.95f, true, -1, rockTint, 0.1f);
            PlaceForestDecor(root, "Rock_Boulder_107", PackSprite(rocks, "env_rock_13"), 107.4f, 0.85f, true, -1, rockTint, 0.1f);
            PlaceForestDecor(root, "Ruin_Shrine_97", PackSprite(ruins, "env_mon_0"), 97.4f, 1.2f, false, -3, ruinTint, 0.2f);
            PlaceForestDecor(root, "Ruin_Arch_84", PackSprite(ruins, "env_mon_10"), 84.2f, 1.6f, false, -3, ruinTint, 0.1f);
            PlaceForestDecor(root, "Ruin_Column_86", PackSprite(ruins, "env_mon_8"), 86.3f, 1.5f, false, -3, ruinTint, 0.1f);

            // Luz de luna suave para que la corteza y la piedra se lean (sin ella eran manchas negras)
            void Glow(string name, float x, float y, Color c, float radius, float intensity)
            {
                GameObject lg = new GameObject(name);
                lg.transform.SetParent(root, false);
                lg.transform.position = new Vector3(x, y, 0f);
                Light2D l = lg.AddComponent<Light2D>();
                l.lightType = Light2D.LightType.Point;
                l.color = c;
                l.intensity = intensity;
                l.pointLightOuterRadius = radius;
                l.pointLightInnerRadius = radius * 0.25f;
                l.falloffIntensity = 0.7f;
            }

            Color moon = new Color(0.72f, 0.66f, 1f, 1f);
            Color amber = new Color(1f, 0.74f, 0.42f, 1f);
            Glow("BiomeGlow_Golden_A", 10f, 3.2f, amber, 6f, 0.55f);
            Glow("BiomeGlow_Golden_B", 25f, 3.2f, amber, 6f, 0.5f);
            Glow("BiomeGlow_Dead_A", 73f, 3.5f, moon, 6.5f, 0.6f);
            Glow("BiomeGlow_Dead_Ruins", 90f, 4.2f, moon, 9f, 0.75f);
            Glow("BiomeGlow_Dead_B", 104f, 3.5f, moon, 6.5f, 0.6f);
            Glow("BiomeGlow_Clearing_A", 113f, 3.2f, amber, 5.5f, 0.5f);
            Glow("BiomeGlow_Clearing_B", 155f, 3.2f, amber, 6f, 0.5f);

            // Hierba y flores: matas secas en el Bosque Muerto, hierba alta y flores otoñales en los otros biomas
            var rng = new System.Random(1692);
            string[] bladeNames = { "top_far_bgrnd_10", "top_far_bgrnd_11", "top_far_bgrnd_12", "top_far_bgrnd_13", "top_far_bgrnd_16", "top_far_bgrnd_17", "top_far_bgrnd_18", "top_far_bgrnd_19" };
            string[] flowerNames = { "top_far_bgrnd_24", "top_far_bgrnd_25", "top_far_bgrnd_27", "top_far_bgrnd_30", "top_far_bgrnd_31", "top_far_bgrnd_34" };
            string[] dryNames = { "env_trees_2", "env_trees_3", "env_trees_4", "env_trees_7", "env_trees_8" };

            void Scatter(float from, float to, float minStep, float maxStep, bool dry)
            {
                float x = from;
                int i = 0;
                while (x < to)
                {
                    bool insideCabin = x > 123.5f && x < 136.5f;
                    if (!insideCabin)
                    {
                        if (dry)
                        {
                            Sprite sp = PackSprite(trees, dryNames[rng.Next(dryNames.Length)]);
                            PlaceForestDecor(root, $"DryGrass_{x:F1}", sp, x, 0.9f + (float)rng.NextDouble() * 0.4f, rng.Next(2) == 0, 1, new Color(0.62f, 0.56f, 0.50f, 1f), 0.08f);
                        }
                        else
                        {
                            bool flower = rng.NextDouble() < 0.4;
                            string[] pool = flower ? flowerNames : bladeNames;
                            Sprite sp = PackSprite(tufts, pool[rng.Next(pool.Length)]);
                            PlaceForestDecor(root, (flower ? "Flower_" : "GrassBlade_") + $"{x:F1}", sp, x, 1.6f + (float)rng.NextDouble() * 0.5f, rng.Next(2) == 0, 1,
                                flower ? new Color(0.95f, 0.82f, 0.78f, 1f) : new Color(0.86f, 0.80f, 0.70f, 1f), 0.04f);
                        }
                        i++;
                    }
                    x += minStep + (float)rng.NextDouble() * (maxStep - minStep);
                }
            }

            Scatter(1.5f, 33f, 1.1f, 2.3f, false);
            Scatter(69.5f, 109f, 2.4f, 4.2f, true);
            Scatter(112.5f, 157f, 1.2f, 2.4f, false);
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
            // SUELO PRINCIPAL DEL ACTO 1, ACTO 2 Y ACTO 3:
            // - Tramo A: X: -6 a X: 52 (58u de largo, superficie en Y: 0.17)
            // - Hueco del Barranco del Río Oscuro: X: 52 a X: 68 (16u de ancho)
            // - Tramo B: X: 68 a X: 170 (102u de largo, superficie en Y: 0.17, cubre Bastión + Cabaña)
            // - Foso de Ciénaga Oscura del Desfiladero: X: 170 a X: 184 (14u de ancho)
            // - Tramo C: X: 184 a X: 228 (44u de largo, superficie en Y: 0.17, rampa y salida a la aldea)
            // ------------------------------------------------------------------
            CreateGroundSegment(act1Env, "Ground_Forest_West", -6f, 58f, groundLayer, groundSprite);
            CreateGroundSegment(act1Env, "Ground_Forest_East", 68f, 102f, groundLayer, groundSprite);
            CreateGroundSegment(act1Env, "Ground_ShadowGorge_Rampart", 184f, 44f, groundLayer, groundSprite);

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
                "El bosque hacia tu cabaña está vigilado. Duerme a los guardias con AURA DORADA con una Poción de Sueño ([Q] o clic) y toma su llave con [E].");

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

            // Ruta Aérea Alta de las Copas (X: 10..32, Y: 5.2..7.5) con Reliquia Secreta #1
            BuildAct1HighCanopyRoute(act1Env, act1Gameplay, act1Pickups, groundLayer, logASprite, logCSprite, potBlindPrefab, heartPrefab);

            log.AppendLine("  - Tramo 1 (X: 0..34): Spawn (2,1), Sign inicial, 2 Pociones Sleep (X:6, X:10), Hideout (X:16), Aldeano, Guardia Portador #1 (X:18..28), Portón #1 (X:34) y Ruta Aérea Alta con Reliquia #1 (Grimorio).");

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
                "Muro de raíces bajas: transfórmate en Gato [T] para cruzar por debajo. En el Barranco del Río Oscuro vuela con tu Escoba.");

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
                "Bastión del Bosque: vuela a la plataforma superior, duerme al Guardia Dorado con una poción ([Q] o clic) y toma su llave con [E].");

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
        // CONSTRUCCIÓN DEL ACTO 2: EL CLARO Y LA CASA DE LA BRUJA (X: 112 a X: 226)
        // =========================================================================
        private static void BuildAct2WitchCabinAndClearing(Transform envRoot, Transform gameplayRoot, Transform npcsRoot, StringBuilder log)
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            LayerMask groundMask = 1 << groundLayer;

            Sprite logASprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_A.png");
            Sprite logBSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_B.png");
            Sprite logCSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_C.png");
            Sprite signSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Sign.png");
            Sprite gateSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Goal_Gate.png");

            GameObject checkpointPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CheckpointPrefabPath);
            GameObject potSleepPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PotionSleepPath);
            GameObject potBlindPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PotionBlindPath);
            GameObject heartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeartPickupPath);
            GameObject guardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GuardPrefabPath);
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
                "Claro de la Cabaña: prepara la Poción Maestra en el Caldero con [E]. El portón de salida no se abrirá hasta que la poción esté lista.");

            // Faroles mágicos con LanternFlicker (tonos violeta y ámbar)
            CreateMysticLantern(act2Env, cainosSprites, "MysticLantern_Violet_1", new Vector3(114.2f, 1.35f, 0f), new Color(0.75f, 0.38f, 1f, 1f));
            CreateMysticLantern(act2Env, cainosSprites, "MysticLantern_Amber_1", new Vector3(122.2f, 1.35f, 0f), new Color(1f, 0.78f, 0.28f, 1f));
            CreateMysticLantern(act2Env, cainosSprites, "MysticLantern_Amber_2", new Vector3(137.8f, 1.35f, 0f), new Color(1f, 0.78f, 0.28f, 1f));
            CreateMysticLantern(act2Env, cainosSprites, "MysticLantern_Violet_2", new Vector3(143.2f, 1.35f, 0f), new Color(0.75f, 0.38f, 1f, 1f));

            // 2. Pabellón / Casa de la Bruja en X: 124 a 136 (centro X: 128..130)
            GameObject cabinRoot = new GameObject("WitchCabin_Pavilion");
            cabinRoot.transform.SetParent(act2Env, false);
            cabinRoot.transform.position = new Vector3(130f, 0.17f, 0f);

            // Pared de fondo de tablones del santuario (X: 124 a 136): antes eran troncos estirados 17u que tapaban el suelo
            Sprite cabinTimber = LoadFirstSprite(Level2ArtPath + "Interior_Wall_Timber.png");
            for (int i = 0; i < 3; i++)
            {
                AddFitted(cabinRoot.transform, $"Cabin_BackWall_{i}", cabinTimber,
                    new Vector2(124f + i * 4f, 0.17f), new Vector2(128f + i * 4f, 4.6f),
                    "Midground", 1, new Color(0.42f, 0.32f, 0.46f, 1f));
            }

            // Pilares de madera en los extremos del porche (X: 124 y X: 136)
            PlaceCainosSprite(cabinRoot.transform, cainosSprites, "Cabin_Pillar_West", "TX Village Props - Stall Tent Pillar", new Vector3(124.2f, 1.65f, 0f), 1.35f, true);
            PlaceCainosSprite(cabinRoot.transform, cainosSprites, "Cabin_Pillar_East", "TX Village Props - Stall Tent Pillar", new Vector3(135.8f, 1.65f, 0f), 1.35f, true);

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
                roofSeg.transform.localScale = new Vector3(1.3f, 0.6f, 1f);
                SpriteRenderer rSr = roofSeg.AddComponent<SpriteRenderer>();
                rSr.sprite = logCSprite;
                rSr.color = new Color(0.38f, 0.25f, 0.34f, 1f);
                rSr.sortingLayerName = "Gameplay";
                rSr.sortingOrder = 3;
            }

            // Repisas interiores de alquimia (Y: 2.35) y mesas laterales
            CreateOneWayPlatform(cabinRoot.transform, "Cabin_AlchemyShelf_Left", logASprite, new Vector2(126.5f, 2.4f), 0.9f, groundLayer);
            CreateOneWayPlatform(cabinRoot.transform, "Cabin_AlchemyShelf_Right", logASprite, new Vector2(133.5f, 2.4f), 0.9f, groundLayer);

            PlaceCainosSprite(cabinRoot.transform, cainosSprites, "AlchemyTable_Left", "TX Village Props - Stall Table", new Vector3(126.6f, 0.72f, 0f), 1.0f, true);
            PlaceCainosSprite(cabinRoot.transform, cainosSprites, "AlchemyTable_Right", "TX Village Props - Stall Table", new Vector3(133.4f, 0.72f, 0f), 1.0f, true);
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

            // El caldero se apoya en el suelo de la cabaña (antes flotaba)
            SnapToGround(cauldronGo, 0.05f);

            // 5. Estaciones de Reabastecimiento de Pociones (PotionRefillStation) en la Cabaña y ruta de regreso
            Sprite sleepFlaskSprite = null;
            if (potSleepPrefab != null)
            {
                SpriteRenderer pfSr = potSleepPrefab.GetComponentInChildren<SpriteRenderer>();
                if (pfSr != null) sleepFlaskSprite = pfSr.sprite;
            }

            CreatePotionRefillStation(act2Gameplay, "PotionRefillStation_CabinPorch", new Vector3(134.8f, 0.72f, 0f),
                "refill_cabin_porch", "Estante Alquímico de la Cabaña", PotionType.Sleep, 2, sleepFlaskSprite);

            // 6. Descanso al entrar a la cabaña: "Llegaste a la cabaña. Descansa…" (corazones restaurados)
            GameObject restGo = new GameObject("Cabin_RestZone");
            restGo.transform.SetParent(act2Gameplay, false);
            restGo.transform.position = new Vector3(130f, 2.4f, 0f);
            BoxCollider2D restCol = restGo.AddComponent<BoxCollider2D>();
            restCol.isTrigger = true;
            restCol.size = new Vector2(11f, 4.4f);
            restGo.AddComponent<Level3CabinRestZone>();

            // 7. Desfiladero de las Sombras y Reto de Regreso a la Aldea (X: 146 a 226)
            BuildAct2ShadowGorge(
                act2Env,
                act2Gameplay,
                npcsRoot,
                groundLayer,
                groundMask,
                logASprite,
                logBSprite,
                logCSprite,
                signSprite,
                checkpointPrefab,
                potSleepPrefab,
                potBlindPrefab,
                heartPrefab,
                guardPrefab,
                cainosSprites);

            log.AppendLine("  - Acto 2 (X: 112..226): Claro seguro con Checkpoint (X:116), descanso en la Casa de la Bruja (X:124..136), WitchCauldronInteractable (X:130), Portón de Regreso (X:146), Desfiladero de las Sombras (X:146..226) con Checkpoints (X:152, X:203), Plataforma Móvil sobre ciénaga, Géiser ambiental, Guardia Élite, túnel de gato con pociones, Reliquia #3 (Cáliz), Mecanismo de Rastra de Salem y salida en X:218.");
        }

        // =========================================================================
        // CONSTRUCCIÓN DEL DESFILADERO DE LAS SOMBRAS (PARTE 1 — FINAL EXPANDIDO, X: 146 a X: 226)
        // =========================================================================
        private static void BuildAct2ShadowGorge(
            Transform envParent,
            Transform gameplayParent,
            Transform npcsParent,
            int groundLayer,
            LayerMask groundMask,
            Sprite logASprite,
            Sprite logBSprite,
            Sprite logCSprite,
            Sprite signSprite,
            GameObject checkpointPrefab,
            GameObject potSleepPrefab,
            GameObject potBlindPrefab,
            GameObject heartPrefab,
            GameObject guardPrefab,
            Sprite[] cainosSprites)
        {
            Transform gorgeEnv = new GameObject("Act2_ShadowGorge_Env").transform;
            gorgeEnv.SetParent(envParent, false);

            Transform gorgeGameplay = new GameObject("Act2_ShadowGorge_Gameplay").transform;
            gorgeGameplay.SetParent(gameplayParent, false);

            Transform gorgePickups = new GameObject("Act2_ShadowGorge_Pickups").transform;
            gorgePickups.SetParent(gorgeGameplay, false);

            // 1. Checkpoint 5 al entrar al Desfiladero (X: 152, Y: 0.17)
            CreateCheckpoint(gorgeGameplay, checkpointPrefab, "Checkpoint_Act2_ShadowGorge", new Vector3(152f, 0.17f, 0f));

            // 2. Cartel del Desfiladero
            CreateWorldSign(
                gorgeGameplay,
                "Sign_Act2_ShadowGorge",
                new Vector2(154.5f, 0.9f),
                signSprite,
                "Desfiladero de las Sombras: La Inquisición patrulla el camino a Salem. Vuela sobre la ciénaga con tu Escoba o infíltrate con forma de Gato.");

            // 3. Pociones de apoyo antes del foso
            InstantiatePickup(gorgePickups, potSleepPrefab, "PotionPickup_Gorge_Sleep", new Vector3(158f, 0.72f, 0f));
            InstantiatePickup(gorgePickups, potBlindPrefab, "PotionPickup_Gorge_Blind", new Vector3(164f, 0.72f, 0f));

            // 4. Gran Foso de Ciénaga Oscura / Pantano de Espinas (X: 170 a 184)
            GameObject kzGorge = new GameObject("KillZone_ShadowSwamp");
            kzGorge.transform.SetParent(gorgeGameplay, false);
            kzGorge.transform.position = new Vector3(177f, -4.5f, 0f);
            BoxCollider2D kzGCol = kzGorge.AddComponent<BoxCollider2D>();
            kzGCol.isTrigger = true;
            kzGCol.size = new Vector2(16f, 2.5f);
            kzGorge.AddComponent<KillZone>();

            // Peligro ambiental: Géiser de Vaho en la orilla oeste del foso
            GameObject hazSwamp = new GameObject("EnvironmentalHazard_SwampGeyser");
            hazSwamp.transform.SetParent(gorgeGameplay, false);
            hazSwamp.transform.position = new Vector3(168.8f, 0.35f, 0f);
            BoxCollider2D hazSCol = hazSwamp.AddComponent<BoxCollider2D>();
            hazSCol.isTrigger = true;
            hazSCol.size = new Vector2(2.0f, 2.2f);
            SpriteRenderer hazSSr = hazSwamp.AddComponent<SpriteRenderer>();
            hazSSr.sprite = EnsureSoftFogSprite();
            hazSSr.color = new Color(0.7f, 0.2f, 0.85f, 0.8f);
            hazSSr.sortingLayerName = "Gameplay";
            hazSSr.sortingOrder = 6;
            Light2D hazSLight = new GameObject("HazardLight").AddComponent<Light2D>();
            hazSLight.transform.SetParent(hazSwamp.transform, false);
            hazSLight.lightType = Light2D.LightType.Point;
            hazSLight.color = new Color(0.7f, 0.2f, 0.85f, 1f);
            hazSLight.intensity = 1.1f;
            hazSLight.pointLightOuterRadius = 3.0f;
            hazSwamp.AddComponent<Level3EnvironmentalHazard>();

            // Plataformas aéreas sobre el foso de ciénaga
            CreateOneWayPlatform(gorgeEnv, "Ledge_Swamp_West", logASprite, new Vector2(168f, 3.2f), 1.3f, groundLayer);
            CreateOneWayPlatform(gorgeEnv, "Ledge_Swamp_HighSpire", logCSprite, new Vector2(176f, 5.8f), 1.5f, groundLayer);
            CreateOneWayPlatform(gorgeEnv, "Ledge_Swamp_East", logASprite, new Vector2(184f, 3.2f), 1.3f, groundLayer);

            // Plataforma Móvil sobre el pantano (oscila entre X: 172 y 180, Y: 2.4 a 3.6)
            GameObject movSwamp = new GameObject("MovingPlatform_ShadowSwamp");
            movSwamp.transform.SetParent(gorgeEnv, false);
            movSwamp.layer = groundLayer;
            movSwamp.transform.position = new Vector3(172f, 2.4f, 0f);
            BoxCollider2D movSCol = movSwamp.AddComponent<BoxCollider2D>();
            movSCol.size = new Vector2(3.6f, 0.45f);
            movSCol.usedByEffector = true;
            PlatformEffector2D movSEff = movSwamp.AddComponent<PlatformEffector2D>();
            movSEff.useOneWay = true;
            movSEff.surfaceArc = 170f;
            SpriteRenderer movSSr = movSwamp.AddComponent<SpriteRenderer>();
            movSSr.sprite = logCSprite;
            movSSr.color = new Color(0.85f, 0.7f, 1f, 1f);
            movSSr.sortingLayerName = "Gameplay";
            movSSr.sortingOrder = 2;
            Light2D movSLight = new GameObject("Glow").AddComponent<Light2D>();
            movSLight.transform.SetParent(movSwamp.transform, false);
            movSLight.lightType = Light2D.LightType.Point;
            movSLight.color = new Color(0.8f, 0.4f, 1f, 1f);
            movSLight.intensity = 1.0f;
            movSLight.pointLightOuterRadius = 3f;
            Level3MovingPlatform movSComp = movSwamp.AddComponent<Level3MovingPlatform>();
            movSComp.Configure(new Vector3(172f, 2.4f, 0f), new Vector3(180f, 3.6f, 0f), 2.4f, 0.75f);

            // 5. Bastión y Rampart del Desfiladero (X: 184 a 202)
            // Piso elevado de la muralla (Y: 3.8)
            GameObject rampartFloor = new GameObject("Rampart_Upper_Floor");
            rampartFloor.transform.SetParent(gorgeEnv, false);
            rampartFloor.layer = groundLayer;
            rampartFloor.transform.position = new Vector3(193f, 3.8f, 0f);
            BoxCollider2D rampartCol = rampartFloor.AddComponent<BoxCollider2D>();
            rampartCol.size = new Vector2(16.5f, 0.45f);
            rampartCol.usedByEffector = true;
            PlatformEffector2D rampartEff = rampartFloor.AddComponent<PlatformEffector2D>();
            rampartEff.useOneWay = true;
            rampartEff.surfaceArc = 170f;

            for (int i = 0; i < 4; i++)
            {
                GameObject tile = new GameObject($"Rampart_Visual_{i}");
                tile.transform.SetParent(rampartFloor.transform, false);
                tile.transform.localPosition = new Vector3(-6f + i * 4.0f, 0f, 0f);
                tile.transform.localScale = new Vector3(1.25f, 0.55f, 1f);
                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = logCSprite;
                sr.sortingLayerName = "Gameplay";
                sr.sortingOrder = 2;
            }

            // Guardia Élite en la muralla (X: 188 a 198, Y: 3.82)
            Transform[] wpElite = CreateWaypoints(npcsParent, "Waypoints_Guard_ShadowElite", new Vector2(188f, 3.82f), new Vector2(198f, 3.82f));
            CreateGuard(npcsParent, guardPrefab, "Guard_ShadowGorge_Elite", new Vector3(193f, 3.82f, 0f), wpElite, PatrolMode.PingPong, groundMask);

            // Túnel bajo para Gato (X: 186 a 198, Y: 0.17 a 1.15)
            AddSolidBox(gorgeEnv, "Underpass_Roof_Solid", new Vector2(186f, 1.35f), new Vector2(198f, 2.4f), groundLayer, new Color(0.28f, 0.2f, 0.15f, 1f));
            CreateCatOnlyTrigger(gorgeGameplay, "CatOnlyPassage_ShadowEntry", new Vector3(185.5f, 0.95f, 0f),
                "Paso estrecho: Infíltrate en forma de Gato [T] para pasar bajo la muralla.");
            CreateCatOnlyTrigger(gorgeGameplay, "CatOnlyPassage_ShadowExit", new Vector3(198.5f, 0.95f, 0f),
                "Paso estrecho: Infíltrate en forma de Gato [T] para pasar bajo la muralla.");

            // Recompensas dentro del túnel de infiltración
            InstantiatePickup(gorgePickups, potSleepPrefab, "PotionPickup_Underpass_Sleep", new Vector3(190f, 0.58f, 0f));
            InstantiatePickup(gorgePickups, heartPrefab, "HeartPickup_Underpass", new Vector3(194f, 0.58f, 0f));
            ShrinkPickupTriggers(gorgePickups, "PotionPickup_Underpass_Sleep", "HeartPickup_Underpass");

            // 6. Risco Alto y Reliquia Secreta #3: Cáliz de Plata del Aquelarre (X: 192, Y: 7.2)
            CreateOneWayPlatform(gorgeEnv, "Ledge_HighSpire_Relic", logCSprite, new Vector2(192f, 7.2f), 1.4f, groundLayer);
            Sprite chaliceSprite = LoadFirstSprite(Level2ArtPath + "Interactive_Chest_Closed.png");
            GameObject relic3Go = new GameObject("SecretRelic_Chalice");
            relic3Go.transform.SetParent(gorgeGameplay, false);
            relic3Go.transform.position = new Vector3(192f, 7.85f, 0f);
            BoxCollider2D relic3Col = relic3Go.AddComponent<BoxCollider2D>();
            relic3Col.isTrigger = true;
            relic3Col.size = new Vector2(1.2f, 1.2f);
            SpriteRenderer relic3Sr = relic3Go.AddComponent<SpriteRenderer>();
            relic3Sr.sprite = chaliceSprite;
            relic3Sr.color = new Color(0.9f, 0.75f, 1f, 1f);
            relic3Sr.sortingLayerName = "Gameplay";
            relic3Sr.sortingOrder = 5;
            Light2D relic3Light = new GameObject("RelicLight").AddComponent<Light2D>();
            relic3Light.transform.SetParent(relic3Go.transform, false);
            relic3Light.lightType = Light2D.LightType.Point;
            relic3Light.color = new Color(0.85f, 0.65f, 1f, 1f);
            relic3Light.intensity = 1.2f;
            relic3Light.pointLightOuterRadius = 3f;
            Level3SecretRelic relic3 = relic3Go.AddComponent<Level3SecretRelic>();
            relic3.Configure("relic_chalice", "Cáliz de Plata del Aquelarre", 400, chaliceSprite, new Color(0.85f, 0.65f, 1f, 1f));

            // 7. Checkpoint 6 en la aproximación final (X: 203, Y: 0.17)
            CreateCheckpoint(gorgeGameplay, checkpointPrefab, "Checkpoint_Act2_FinalApproach", new Vector3(203f, 0.17f, 0f));

            // 8. Mecanismo de Salida: Runa y Rastra de Salem (X: 206 y X: 211)
            GameObject finalGateGo = new GameObject("MechanismGate_SalemApproach");
            finalGateGo.transform.SetParent(gorgeEnv, false);
            finalGateGo.layer = groundLayer;
            finalGateGo.transform.position = new Vector3(211f, 2.2f, 0f);
            BoxCollider2D finalGateCol = finalGateGo.AddComponent<BoxCollider2D>();
            finalGateCol.size = new Vector2(0.9f, 4.0f);
            SpriteRenderer finalGateSr = finalGateGo.AddComponent<SpriteRenderer>();
            finalGateSr.sprite = logBSprite;
            finalGateSr.color = new Color(0.5f, 0.35f, 0.65f, 1f);
            finalGateSr.sortingLayerName = "Gameplay";
            finalGateSr.sortingOrder = 3;
            Level3MechanismGate finalGate = finalGateGo.AddComponent<Level3MechanismGate>();
            finalGate.Configure("gate_salem_approach", "Rastra del Portal de Salem", finalGateGo.transform, finalGateCol, 4.0f);

            Sprite leverSprite = FindAtlasSprite(cainosSprites, "TX Village Props - Lever 01") ?? logASprite;
            GameObject finalSwitchGo = new GameObject("RuneSwitch_SalemPortal");
            finalSwitchGo.transform.SetParent(gorgeGameplay, false);
            finalSwitchGo.transform.position = new Vector3(205.5f, 0.85f, 0f);
            BoxCollider2D finalSwCol = finalSwitchGo.AddComponent<BoxCollider2D>();
            finalSwCol.isTrigger = true;
            finalSwCol.size = new Vector2(1.8f, 1.8f);
            SpriteRenderer finalSwSr = finalSwitchGo.AddComponent<SpriteRenderer>();
            finalSwSr.sprite = leverSprite;
            finalSwSr.sortingLayerName = "Gameplay";
            finalSwSr.sortingOrder = 4;
            Light2D finalSwLight = new GameObject("SwitchLight").AddComponent<Light2D>();
            finalSwLight.transform.SetParent(finalSwitchGo.transform, false);
            finalSwLight.lightType = Light2D.LightType.Point;
            finalSwLight.color = new Color(0.3f, 1f, 0.7f, 1f);
            finalSwLight.intensity = 1.0f;
            finalSwLight.pointLightOuterRadius = 2.5f;
            Level3RuneSwitch finalSw = finalSwitchGo.AddComponent<Level3RuneSwitch>();
            finalSw.Configure("switch_salem_portal", "Mecanismo del Portal", finalGate);
            finalGate.AddRequiredSwitch(finalSw);

            // 9. Portal de Salida hacia la Aldea (Level3_Village) en Part1ExitX (218)
            GameObject exitGo = new GameObject("Part1_Exit_ToVillage");
            exitGo.transform.SetParent(gorgeGameplay, false);
            exitGo.transform.position = new Vector3(Part1ExitX, 3f, 0f);
            BoxCollider2D exitCol = exitGo.AddComponent<BoxCollider2D>();
            exitCol.isTrigger = true;
            exitCol.size = new Vector2(2.0f, 6.5f);
            exitGo.AddComponent<Level3PartExit>();

            Light2D exitGlow = new GameObject("ExitGlow").AddComponent<Light2D>();
            exitGlow.transform.SetParent(exitGo.transform, false);
            exitGlow.transform.localPosition = new Vector3(1.5f, -1.4f, 0f);
            exitGlow.lightType = Light2D.LightType.Point;
            exitGlow.color = new Color(1f, 0.82f, 0.45f, 1f);
            exitGlow.intensity = 1.3f;
            exitGlow.pointLightOuterRadius = 6f;

            // Portal visual: dos pilares místicos enmarcando la salida
            PlaceCainosSprite(gorgeEnv, cainosSprites, "PortalPillar_West", "TX Village Props - Stall Tent Pillar", new Vector3(Part1ExitX - 1.2f, 1.8f, 0f), 1.4f, true);
            PlaceCainosSprite(gorgeEnv, cainosSprites, "PortalPillar_East", "TX Village Props - Stall Tent Pillar", new Vector3(Part1ExitX + 1.2f, 1.8f, 0f), 1.4f, true);

            // 10. Muro Límite Este en Part1EastWallX (226)
            CreateSolidWall(envParent, "Boundary_EastTrailWall", new Vector2(Part1EastWallX, 6f), new Vector2(1.5f, 14f), groundLayer, null);
        }

        // =========================================================================
        // CONSTRUCCIÓN DEL ACTO 3 (FASE 2): LA ALDEA DE SALEM Y SU IGLESIA (X: 146 a X: 266)
        // =========================================================================
        private static void BuildAct3ChurchDistrict(Transform envRoot, Transform gameplayRoot, Transform npcsRoot, StringBuilder log)
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            LayerMask groundMask = 1 << groundLayer;

            Sprite logBSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_B.png");
            Sprite logCSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Platform_Log_C.png");
            Sprite gateSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Goal_Gate.png");
            Sprite signSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Sign.png");

            Sprite cornCluster = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Corn_Cluster.png");
            Sprite stalkA = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Corn_Stalk_A.png");
            Sprite stalkB = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Corn_Stalk_B.png");
            Sprite stalkC = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Corn_Stalk_C.png");

            GameObject guardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GuardPrefabPath);
            GameObject villagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VillagerPrefabPath);
            GameObject villagerFPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VillagerFPrefabPath);
            GameObject villagerPreacherPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VillagerPreacherPrefabPath);
            GameObject checkpointPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CheckpointPrefabPath);

            GameObject potSleepPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PotionSleepPath);
            GameObject potBlindPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PotionBlindPath);
            GameObject potHypnoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PotionHypnotizePath);
            GameObject heartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeartPickupPath);

            Sprite[] cainosSprites = AssetDatabase.LoadAllAssetsAtPath(CainosAtlasPath).OfType<Sprite>().ToArray();
            Sprite[] chestSprites = AssetDatabase.LoadAllAssetsAtPath(CainosChestAtlasPath).OfType<Sprite>().ToArray();

            Sprite sleepFlaskSprite = null;
            if (potSleepPrefab != null)
            {
                SpriteRenderer pfSr = potSleepPrefab.GetComponentInChildren<SpriteRenderer>();
                if (pfSr != null) sleepFlaskSprite = pfSr.sprite;
            }

            Transform act3Env = new GameObject("Act3_ChurchDistrict_Env").transform;
            act3Env.SetParent(envRoot, false);

            Transform act3Gameplay = new GameObject("Act3_ChurchDistrict_Gameplay").transform;
            act3Gameplay.SetParent(gameplayRoot, false);

            Transform act3Pickups = new GameObject("Act3_Pickups").transform;
            act3Pickups.SetParent(act3Gameplay, false);

            Transform act3Npcs = new GameObject("Act3_NPCs").transform;
            act3Npcs.SetParent(npcsRoot, false);

            // Suelo de la Aldea (X: 138 a 270) y muros límite: oeste (llegada desde el Sendero) y este tras la Iglesia en X: 266
            Sprite groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GroundSpritePath);
            CreateGroundSegment(act3Env, "Ground_Village", 138f, 132f, groundLayer, groundSprite);
            CreateSolidWall(act3Env, "Boundary_WestTrailWall", new Vector2(Part2WestWallX, 6f), new Vector2(1.5f, 14f), groundLayer, null);
            CreateSolidWall(act3Env, "Boundary_EastChurchWall", new Vector2(266f, 6f), new Vector2(1.5f, 14f), groundLayer, null);

            CreatePotionRefillStation(act3Gameplay, "PotionRefillStation_ReturnTrail", new Vector3(150.5f, 0.72f, 0f),
                "refill_return_trail_1", "Alijo del Sendero de Regreso", PotionType.Sleep, 2, sleepFlaskSprite);

            // ------------------------------------------------------------------
            // TRAMO 3.1: ENTRADA A LA ALDEA Y CASA DEL SACRISTÁN (X: 146 a 170)
            // ------------------------------------------------------------------
            CreateWorldSign(
                act3Gameplay,
                "Sign_Act3_VillageEntry",
                new Vector2(148.9f, 0.9f),
                signSprite,
                "Aldea de Salem: las casas están atrancadas. Entra como Gato [T] por ventanas altas, gateras o conductos. Dentro mantén [Shift] para no despertar a nadie.");

            CreateCheckpoint(act3Gameplay, checkpointPrefab, "Checkpoint_Act3_VillageRear", new Vector3(151.8f, 0.17f, 0f));

            CreateIlluminatedLoreStand(
                act3Gameplay,
                cainosSprites,
                "Lore_Clue_1_Sacristan",
                new Vector3(154.4f, 0.17f, 0f),
                "clue_sacristan_1",
                "Nota del Sacristán I",
                "Por orden del Juez escondí la Llave de la Cripta en el Cofre de Madera de mi casa, junto a la cama. Atranqué la puerta: solo un gato podría colarse por la ventana alta, trepando por las cajas. Y que nadie haga ruido: duermo con un ojo abierto.");

            InstantiatePickup(act3Pickups, potSleepPrefab, "PotionPickup_Act3_Rear_Sleep", new Vector3(156.0f, 0.65f, 0f));

            var sacristan = new VillageHouseSpec
            {
                Id = "sacristan",
                Name = "Casa del Sacristán",
                X0 = 160f,
                X1 = 170f,
                WestEntry = HouseOpening.HighWindow,
                EastExit = HouseOpening.CatFlap,
                FacadeSlices = new[] { "houses_sprite.png_3", "houses_sprite.png_19" },
                EntryHint = "Entra como Gato [T]: trepa por las cajas hasta la ventana alta.",
                BedX = 164.8f,
                ChestX = 168.3f,
                ChestObjectName = "Chest_Act3_1_Wooden",
                ChestId = "chest_act3_crypt_wooden",
                ChestName = "Cofre de Madera del Sacristán",
                ChestKind = "Wooden",
                ChestNeedsKey = false,
                RewardKeyId = "crypt_chest_key",
                RewardKeyName = "Llave de la Cripta",
            };
            sacristan.Decor.Add(new HouseDecor("Interior_Bookshelf.png", 162.5f, 1.5f, 2.0f));
            sacristan.Decor.Add(new HouseDecor("Interior_Fireplace.png", 166.7f, 1.8f, 2.2f));
            sacristan.Decor.Add(new HouseDecor("Interior_Window_Shutters.png", 164.8f, 1.1f, 0.95f, 2.35f));
            BuildInfiltrationHouse(sacristan, act3Env, act3Gameplay, act3Npcs, groundLayer, cainosSprites, chestSprites, villagerPrefab);

            // ------------------------------------------------------------------
            // TRAMO 3.2: CALLE DEL CAMPANERO, PISTA #2 Y GUARDIA DEL BALCÓN (X: 170 a 194)
            // ------------------------------------------------------------------
            CreateCheckpoint(act3Gameplay, checkpointPrefab, "Checkpoint_Act3_CryptYard", new Vector3(171.4f, 0.17f, 0f));
            InstantiatePickup(act3Pickups, potSleepPrefab, "PotionPickup_Chest1_Sleep_1", new Vector3(172.4f, 0.65f, 0f));

            CreateIlluminatedLoreStand(
                act3Gameplay,
                cainosSprites,
                "Lore_Clue_2_CaptainDiary",
                new Vector3(173.8f, 0.17f, 0f),
                "clue_captain_diary_2",
                "Diario del Capitán de la Guardia",
                "El guardia de la linterna dorada vigila el balcón y el tejado de la Casa del Campanero: él lleva la Llave de la Reja del Atrio. Dentro de la casa, junto al campanero dormido, está el Cofre de Hierro; se abre con la Llave de la Cripta y guarda la Llave del Cofre del Inquisidor. Solo se entra por la gatera.");

            CreateForestHideout(act3Env, "Hideout_CryptYard_1", 176.8f, 0f, 3.0f, 3.4f, cornCluster, stalkA, stalkB, stalkC);

            Transform[] wpRearGuard = CreateWaypoints(act3Npcs, "Waypoints_Guard_RearEntry", new Vector2(174.6f, 0.17f), new Vector2(179.2f, 0.17f));
            CreateGuard(act3Npcs, guardPrefab, "Guard_Act3_RearEntry", new Vector3(177f, 0.17f, 0f), wpRearGuard, PatrolMode.PingPong, groundMask);

            var campanero = new VillageHouseSpec
            {
                Id = "campanero",
                Name = "Casa del Campanero",
                X0 = 182f,
                X1 = 194f,
                WestEntry = HouseOpening.CatFlap,
                EastExit = HouseOpening.CatFlap,
                FacadeSlices = new[] { "houses_sprite.png_7", "houses_sprite.png_17" },
                FacadeTint = new Color(0.84f, 0.82f, 0.9f, 1f),
                EntryHint = "Entra como Gato [T] por la gatera de la pared.",
                BedX = 187.6f,
                ChestX = 191.5f,
                ChestObjectName = "Chest_Act3_2_Iron",
                ChestId = "chest_act3_belfry_iron",
                ChestName = "Cofre de Hierro del Campanero",
                ChestKind = "Iron",
                ChestNeedsKey = true,
                NeedKeyId = "crypt_chest_key",
                NeedKeyName = "Llave de la Cripta",
                RewardKeyId = "inquisitor_chest_key",
                RewardKeyName = "Llave del Cofre del Inquisidor",
            };
            campanero.Decor.Add(new HouseDecor("Interior_Table_Chairs.png", 184.7f, 2.1f, 1.25f));
            campanero.Decor.Add(new HouseDecor("Interior_Bookshelf.png", 189.8f, 1.5f, 2.0f));
            campanero.Decor.Add(new HouseDecor("Interior_Window_Shutters.png", 185.4f, 1.1f, 0.95f, 2.35f));
            BuildInfiltrationHouse(campanero, act3Env, act3Gameplay, act3Npcs, groundLayer, cainosSprites, chestSprites, villagerPrefab);

            // Balcón y escalera hasta el tejado del Campanero: el Guardia Portador patrulla arriba
            Sprite balconySprite = LoadFirstSprite(Level2ArtPath + "Village_Balcony_Platform.png");
            GameObject balcony = CreateOneWayLedge(act3Env, "Campanero_Balcony", 179.9f, 181.85f, HouseRoofTop, groundLayer, balconySprite, true);
            CreateClimbableLadder(act3Gameplay, cainosSprites, "Ladder_CampaneroBalcony", 180.5f, 0.17f, HouseRoofTop, balcony.GetComponent<Collider2D>());

            Transform[] wpBalconyCarrier = CreateWaypoints(act3Npcs, "Waypoints_KeyCarrier_3_Balcony", new Vector2(182.6f, HouseRoofTop + 0.02f), new Vector2(193.4f, HouseRoofTop + 0.02f));
            GameObject guardCarrier3 = CreateGuard(act3Npcs, guardPrefab, "Guard_KeyCarrier_3_Balcony", new Vector3(188f, HouseRoofTop + 0.02f, 0f), wpBalconyCarrier, PatrolMode.PingPong, groundMask);
            if (guardCarrier3 != null)
            {
                KeyCarrierGuard kc3 = guardCarrier3.AddComponent<KeyCarrierGuard>();
                kc3.Configure(
                    "sanctuary_gate_key",
                    "Llave de la Reja del Atrio",
                    new Color(1f, 0.86f, 0.18f, 0.95f),
                    autoCollect: false);
                EditorUtility.SetDirty(kc3);
            }

            // ------------------------------------------------------------------
            // TRAMO 3.3: PLAZA DEL MERCADO Y REJA DEL ATRIO (X: 194 a 212)
            // ------------------------------------------------------------------
            CreatePotionRefillStation(act3Gameplay, "PotionRefillStation_Act3_Rear", new Vector3(195.9f, 0.72f, 0f),
                "refill_act3_rear", "Alijo de los Linderos", PotionType.Sleep, 2, sleepFlaskSprite);
            InstantiatePickup(act3Pickups, potSleepPrefab, "PotionPickup_Chest1_Sleep_2", new Vector3(198.4f, 0.65f, 0f));
            InstantiatePickup(act3Pickups, potHypnoPrefab, "PotionPickup_Act3_Rear_Hypno", new Vector3(205.2f, 0.65f, 0f));

            CreateVillager(act3Npcs, villagerFPrefab != null ? villagerFPrefab : villagerPrefab, "Villager_Act3_RearEntry", new Vector3(199.4f, 0.17f, 0f), WanderPattern.WalkAndReturn, 2.4f);
            CreateForestHideout(act3Env, "Hideout_CryptYard_2", 202.6f, 0f, 3.4f, 3.5f, cornCluster, stalkA, stalkB, stalkC);

            Transform[] wpCryptLow2 = CreateWaypoints(act3Npcs, "Waypoints_Guard_CryptLow2", new Vector2(197.6f, 0.17f), new Vector2(206.2f, 0.17f));
            CreateGuard(act3Npcs, guardPrefab, "Guard_CryptYard_Low2", new Vector3(201.8f, 0.17f, 0f), wpCryptLow2, PatrolMode.PingPong, groundMask);

            CreateLockedGateWithCanopyBlocker(
                act3Gameplay,
                "Gate_Act3_Sanctuary",
                new Vector3(212f, 0.17f, 0f),
                "sanctuary_atrium_gate",
                "Reja del Atrio de la Iglesia",
                "sanctuary_gate_key",
                "Llave de la Reja del Atrio",
                requireMasterPotion: false,
                advanceCabinPhase: false,
                gateSprite,
                groundLayer);

            // ------------------------------------------------------------------
            // TRAMO 3.4: EL ATRIO, CASA DEL INQUISIDOR Y LA IGLESIA (X: 212 a 266)
            // ------------------------------------------------------------------
            CreateCheckpoint(act3Gameplay, checkpointPrefab, "Checkpoint_Act3_ChurchAtrium", new Vector3(214.6f, 0.17f, 0f));

            CreateIlluminatedLoreStand(
                act3Gameplay,
                cainosSprites,
                "Lore_Clue_3_PriestConfession",
                new Vector3(217.8f, 0.17f, 0f),
                "clue_priest_confession_3",
                "Confesión del Párroco",
                "La Llave Maestra de la Iglesia de Salem descansa en el Cofre Dorado de la Casa del Inquisidor; solo la Llave del Inquisidor lo abre. Él duerme con el sueño ligero: entra como gato por el conducto de ventilación y camina despacio.");

            var inquisidor = new VillageHouseSpec
            {
                Id = "inquisidor",
                Name = "Casa del Inquisidor",
                X0 = 221.5f,
                X1 = 234.5f,
                WestEntry = HouseOpening.Duct,
                EastExit = HouseOpening.CatFlap,
                WestWallThickness = 2.2f,
                FacadeSlices = new[] { "houses_sprite.png_2", "houses_sprite.png_4", "houses_sprite.png_10" },
                FacadeTint = new Color(0.8f, 0.8f, 0.92f, 1f),
                EntryHint = "Entra como Gato [T] por el conducto de ventilación junto a la puerta.",
                BedX = 228.6f,
                ChestX = 232.6f,
                ChestObjectName = "Chest_Act3_3_Golden",
                ChestId = "chest_act3_sanctuary_golden",
                ChestName = "Cofre Dorado del Inquisidor",
                ChestKind = "Golden",
                ChestNeedsKey = true,
                NeedKeyId = "inquisitor_chest_key",
                NeedKeyName = "Llave del Cofre del Inquisidor",
                RewardKeyId = Level3StateController.ChurchMasterKeyId,
                RewardKeyName = "Llave Maestra de la Iglesia",
            };
            inquisidor.Decor.Add(new HouseDecor("Interior_Fireplace.png", 225.4f, 1.8f, 2.2f));
            inquisidor.Decor.Add(new HouseDecor("Interior_Wardrobe.png", 230.8f, 1.2f, 2.1f));
            inquisidor.Decor.Add(new HouseDecor("Interior_Window_Shutters.png", 227.2f, 1.1f, 0.95f, 2.35f));
            BuildInfiltrationHouse(inquisidor, act3Env, act3Gameplay, act3Npcs, groundLayer, cainosSprites, chestSprites, villagerPrefab);

            CreateCheckpoint(act3Gameplay, checkpointPrefab, "Checkpoint_Act3_AtriumEast", new Vector3(236.0f, 0.17f, 0f));
            CreatePotionRefillStation(act3Gameplay, "PotionRefillStation_Act3_Atrium", new Vector3(237.6f, 0.72f, 0f),
                "refill_act3_atrium", "Alijo del Atrio", PotionType.Sleep, 2, sleepFlaskSprite);

            CreateForestHideout(act3Env, "Hideout_Atrium_West", 240.2f, 0f, 3.6f, 3.6f, cornCluster, stalkA, stalkB, stalkC);
            CreateForestHideout(act3Env, "Hideout_Atrium_East", 247.4f, 0f, 3.6f, 3.6f, cornCluster, stalkA, stalkB, stalkC);

            Transform[] wpAtrium1 = CreateWaypoints(act3Npcs, "Waypoints_Guard_Atrium1", new Vector2(238.6f, 0.17f), new Vector2(244.0f, 0.17f));
            CreateGuard(act3Npcs, guardPrefab, "Guard_Atrium_1", new Vector3(241f, 0.17f, 0f), wpAtrium1, PatrolMode.PingPong, groundMask);

            Transform[] wpAtrium2 = CreateWaypoints(act3Npcs, "Waypoints_Guard_Atrium2", new Vector2(245.0f, 0.17f), new Vector2(249.8f, 0.17f));
            CreateGuard(act3Npcs, guardPrefab, "Guard_Atrium_2", new Vector3(247.5f, 0.17f, 0f), wpAtrium2, PatrolMode.PingPong, groundMask);

            CreateVillager(act3Npcs, villagerPreacherPrefab != null ? villagerPreacherPrefab : villagerPrefab,
                "Villager_Atrium_Preacher", new Vector3(242.6f, 0.17f, 0f), WanderPattern.LookAround, 2.6f);

            InstantiatePickup(act3Pickups, potBlindPrefab, "PotionPickup_Atrium_Blind", new Vector3(239.4f, 0.65f, 0f));
            InstantiatePickup(act3Pickups, potSleepPrefab, "PotionPickup_Atrium_Sleep", new Vector3(243.2f, 0.65f, 0f));
            InstantiatePickup(act3Pickups, potHypnoPrefab, "PotionPickup_Atrium_Hypno", new Vector3(246.0f, 0.65f, 0f));
            InstantiatePickup(act3Pickups, heartPrefab, "HeartPickup_Atrium", new Vector3(250.0f, 0.65f, 0f));

            // La Iglesia de Salem (X: 250 a 260) y ChurchDoorGoal en X: 255
            BuildSalemChurchMonumentAndGoal(act3Env, act3Gameplay, cainosSprites, logBSprite, logCSprite, gateSprite);

            DecorateVillageStreet(act3Env, cainosSprites);

            log.AppendLine("  - Acto 3 (X: 146..266): Aldea de Salem con 3 casas atrancadas (Sacristán X:160..170 ventana alta, Campanero X:182..194 gatera + guardia portador en el tejado, Inquisidor X:221.5..234.5 conducto), aldeanos dormidos con medidor de ruido, 3 cofres encadenados dentro de las casas, Reja del Atrio (X:212) e Iglesia (X:255).");
        }

        // =========================================================================
        // CASAS DE INFILTRACIÓN DE LA ALDEA (solo se entra como gato)
        // =========================================================================
        private enum HouseOpening { HighWindow, CatFlap, Duct }

        private const float StreetY = 0.17f;
        private const float HouseWallThickness = 0.5f;
        private const float HouseCeilingY = 3.6f;
        private const float HouseRoofThickness = 0.45f;
        private const float HouseRoofTop = HouseCeilingY + HouseRoofThickness;
        private const float HouseFacadeHeight = 6.6f;
        private const float CatGapHeight = 0.9f;
        private const float HighWindowSillY = 2.37f;

        private struct HouseDecor
        {
            public string File;
            public float CenterX;
            public float Width;
            public float Height;
            public float BottomY;

            public HouseDecor(string file, float centerX, float width, float height, float bottomY = -1f)
            {
                File = file;
                CenterX = centerX;
                Width = width;
                Height = height;
                BottomY = bottomY;
            }
        }

        private sealed class VillageHouseSpec
        {
            public string Id;
            public string Name;
            public float X0;
            public float X1;
            public HouseOpening WestEntry = HouseOpening.CatFlap;
            public HouseOpening EastExit = HouseOpening.CatFlap;
            public float WestWallThickness = HouseWallThickness;
            public string[] FacadeSlices;
            public Color FacadeTint = new Color(0.86f, 0.84f, 0.92f, 1f);
            public string EntryHint;
            public float BedX;
            public float ChestX;
            public string ChestObjectName;
            public string ChestId;
            public string ChestName;
            public string ChestKind;
            public bool ChestNeedsKey;
            public string NeedKeyId = string.Empty;
            public string NeedKeyName = string.Empty;
            public string RewardKeyId;
            public string RewardKeyName;
            public readonly List<HouseDecor> Decor = new List<HouseDecor>();
        }

        private static void OpeningSpan(HouseOpening opening, out float bottom, out float top)
        {
            bottom = opening == HouseOpening.HighWindow ? HighWindowSillY : StreetY;
            top = bottom + CatGapHeight;
        }

        /// <summary>
        /// Casa cerrada de la Aldea: techo y paredes sólidos con un único hueco de 0.9u por lado (la bruja humana
        /// mide 1.6u, el gato 0.5u), fachada que se desvanece al entrar, aldeano dormido con medidor de ruido,
        /// cofre con llave que el gato puede abrir y puerta principal atrancada que solo da la pista de entrada.
        /// </summary>
        private static Level3VillageHouse BuildInfiltrationHouse(
            VillageHouseSpec s,
            Transform envParent,
            Transform gameplayParent,
            Transform npcsParent,
            int groundLayer,
            Sprite[] cainos,
            Sprite[] chestAtlas,
            GameObject villagerPrefab)
        {
            const float g = StreetY;
            float westT = Mathf.Max(HouseWallThickness, s.WestWallThickness);
            float innerMinX = s.X0 + westT;
            float innerMaxX = s.X1 - HouseWallThickness;
            float centerX = (s.X0 + s.X1) * 0.5f;
            Color wood = new Color(0.19f, 0.12f, 0.09f, 1f);
            Color hole = new Color(0.03f, 0.02f, 0.04f, 0.94f);

            GameObject root = new GameObject("House_" + s.Id);
            root.transform.SetParent(envParent, false);
            root.transform.position = new Vector3(centerX, g, 0f);
            Transform structure = NewChild(root.transform, "Structure");
            Transform interior = NewChild(root.transform, "Interior");
            Transform facadeRoot = NewChild(root.transform, "Facade");

            // ---- Estructura física
            AddSolidBox(structure, "Roof_Solid", new Vector2(s.X0 - 0.2f, HouseCeilingY), new Vector2(s.X1 + 0.2f, HouseRoofTop), groundLayer, wood);
            OpeningSpan(s.WestEntry, out float wb, out float wt);
            OpeningSpan(s.EastExit, out float eb, out float et);
            BuildWallWithGap(structure, "WestWall", s.X0, innerMinX, wb, wt, groundLayer, wood);
            BuildWallWithGap(structure, "EastWall", innerMaxX, s.X1, eb, et, groundLayer, wood);

            if (s.WestEntry == HouseOpening.HighWindow)
            {
                // Cajas para trepar hasta la ventana (fuera) y repisa para bajar sin saltar (dentro)
                Sprite crate = FindAtlasSprite(cainos, "TX Village Props - Crate Large");
                CreateOneWayLedge(structure, "Crate_Step_Low", s.X0 - 2.45f, s.X0 - 1.35f, g + 1.1f, groundLayer, null, false);
                AddFitted(structure, "Crate_Step_Low_Visual", crate, new Vector2(s.X0 - 2.45f, g - 0.02f), new Vector2(s.X0 - 1.35f, g + 1.1f), "Gameplay", 3, Color.white);
                CreateOneWayLedge(structure, "Crate_Step_High", s.X0 - 1.15f, s.X0 - 0.02f, HighWindowSillY, groundLayer, null, false);
                AddFitted(structure, "Crate_Step_High_Visual_A", crate, new Vector2(s.X0 - 1.15f, g - 0.02f), new Vector2(s.X0 - 0.02f, g + 1.1f), "Gameplay", 3, Color.white);
                AddFitted(structure, "Crate_Step_High_Visual_B", crate, new Vector2(s.X0 - 1.12f, g + 1.1f), new Vector2(s.X0 - 0.05f, HighWindowSillY), "Gameplay", 3, new Color(0.9f, 0.88f, 0.85f, 1f));
                CreateOneWayLedge(structure, "Interior_WindowShelf", innerMinX, innerMinX + 1.3f, HighWindowSillY, groundLayer, null, true);
            }

            // ---- Interior (se ve al entrar: la fachada se vuelve transparente)
            Sprite timber = LoadFirstSprite(Level2ArtPath + "Interior_Wall_Timber.png");
            float innerW = innerMaxX - innerMinX;
            float innerH = HouseCeilingY - g;
            int tiles = Mathf.Max(1, Mathf.RoundToInt(innerW / innerH));
            float tileW = innerW / tiles;
            for (int i = 0; i < tiles; i++)
            {
                AddFitted(interior, $"Interior_Wall_{i}", timber,
                    new Vector2(innerMinX + i * tileW, g), new Vector2(innerMinX + (i + 1) * tileW, HouseCeilingY),
                    "Midground", 4, new Color(0.5f, 0.42f, 0.38f, 1f));
            }

            foreach (HouseDecor d in s.Decor)
            {
                Sprite sp = LoadFirstSprite(Level2ArtPath + d.File);
                float bottom = d.BottomY >= 0f ? d.BottomY : g - 0.02f;
                AddFitted(interior, "Decor_" + Path.GetFileNameWithoutExtension(d.File), sp,
                    new Vector2(d.CenterX - d.Width * 0.5f, bottom), new Vector2(d.CenterX + d.Width * 0.5f, bottom + d.Height),
                    "Midground", 6, new Color(0.85f, 0.8f, 0.78f, 1f));
            }

            Sprite bed = LoadFirstSprite(Level2ArtPath + "Interior_Bed.png");
            AddFitted(interior, "Interior_Bed", bed, new Vector2(s.BedX - 1.25f, g - 0.02f), new Vector2(s.BedX + 1.25f, g + 1.28f), "Gameplay", 0, Color.white);

            GameObject candle = new GameObject("Interior_CandleLight2D");
            candle.transform.SetParent(interior, false);
            candle.transform.position = new Vector3(centerX, HouseCeilingY - 0.9f, 0f);
            Light2D candleLight = candle.AddComponent<Light2D>();
            candleLight.lightType = Light2D.LightType.Point;
            candleLight.color = new Color(1f, 0.72f, 0.42f, 1f);
            candleLight.intensity = 0.95f;
            candleLight.pointLightInnerRadius = 0.6f;
            candleLight.pointLightOuterRadius = innerW * 0.65f;
            candle.AddComponent<LanternFlicker>();

            // ---- Fachada (se funde al entrar la bruja)
            // Orden de dibujo (capa Gameplay): interior 0-1 < muro de fondo 2 < fachadas 3 < puerta y huecos 4 < bruja 5
            var facade = new List<SpriteRenderer>();
            SpriteRenderer backing = AddRect(facadeRoot, "Facade_Backing", new Vector2(s.X0 - 0.2f, g - 0.05f), new Vector2(s.X1 + 0.2f, HouseRoofTop + 0.05f),
                "Gameplay", 2, new Color(0.25f, 0.17f, 0.14f, 1f));
            if (backing != null) facade.Add(backing);

            Sprite[] slices = AssetDatabase.LoadAllAssetsAtPath(Level2ArtPath + "houses_sprite_clean.png").OfType<Sprite>().ToArray();
            int n = s.FacadeSlices != null ? s.FacadeSlices.Length : 0;
            float sliceW = n > 0 ? (s.X1 - s.X0 + 0.4f) / n : 0f;
            for (int i = 0; i < n; i++)
            {
                Sprite sp = slices.FirstOrDefault(x => x != null && x.name == s.FacadeSlices[i]);
                // Cada fachada se ensancha un 20% para que sus paredes se toquen (el arte trae arbustos y faroles a los lados)
                float cx = s.X0 - 0.2f + (i + 0.5f) * sliceW;
                float half = sliceW * 0.6f;
                SpriteRenderer fr = AddFitted(facadeRoot, $"Facade_{i}", sp,
                    new Vector2(cx - half, g - 0.1f), new Vector2(cx + half, g - 0.1f + HouseFacadeHeight),
                    "Gameplay", 3, s.FacadeTint);
                if (fr != null) facade.Add(fr);
            }

            float doorX = s.WestEntry == HouseOpening.HighWindow ? s.X0 + 0.6f : innerMinX + 0.9f;
            Sprite doorSp = LoadFirstSprite(Level2ArtPath + "Village_Door_Locked.png");
            SpriteRenderer doorSr = AddFitted(facadeRoot, "Facade_BarredDoor", doorSp,
                new Vector2(doorX - 0.55f, g - 0.03f), new Vector2(doorX + 0.55f, g + 1.9f), "Gameplay", 4, new Color(0.92f, 0.86f, 0.8f, 1f));
            if (doorSr != null) facade.Add(doorSr);
            SpriteRenderer planks = AddFitted(facadeRoot, "Facade_DoorPlanks", FindAtlasSprite(cainos, "TX Village Props - Barricade"),
                new Vector2(doorX - 0.75f, g - 0.03f), new Vector2(doorX + 0.75f, g + 1.15f), "Gameplay", 5, Color.white);
            if (planks != null) facade.Add(planks);

            BuildOpeningVisual(facadeRoot, cainos, s.WestEntry, s.X0, innerMinX, wb, wt, hole);
            BuildOpeningVisual(facadeRoot, cainos, s.EastExit, innerMaxX, s.X1, eb, et, hole);

            Level3VillageHouse house = root.AddComponent<Level3VillageHouse>();
            house.Configure(s.Id, s.Name, new Vector2(innerMinX, g - 0.3f), new Vector2(innerMaxX, HouseCeilingY), facade.ToArray());

            // ---- Puerta principal atrancada: [E] solo explica por dónde entrar
            GameObject doorGo = new GameObject("BarredDoor_" + s.Id);
            doorGo.transform.SetParent(gameplayParent, false);
            doorGo.transform.position = new Vector3(s.X0 - 1.0f, g, 0f);
            BoxCollider2D doorCol = doorGo.AddComponent<BoxCollider2D>();
            doorCol.isTrigger = true;
            doorCol.size = new Vector2(2.1f, 2.4f);
            doorCol.offset = new Vector2(0f, 1.2f);
            BarredHouseDoor barred = doorGo.AddComponent<BarredHouseDoor>();
            barred.Configure("barred_door_" + s.Id, s.Name, s.EntryHint);

            // ---- Pasos solo para gato (aviso a la bruja humana y bloqueo de la retransformación)
            CreateCatOnlyTrigger(gameplayParent, $"CatOnlyPassage_{s.Id}_West", new Vector3((s.X0 + innerMinX) * 0.5f, (wb + wt) * 0.5f, 0f),
                $"{s.Name}: el hueco es demasiado estrecho para una bruja. Pulsa [T] y entra como Gato.");
            CreateCatOnlyTrigger(gameplayParent, $"CatOnlyPassage_{s.Id}_East", new Vector3((innerMaxX + s.X1) * 0.5f, (eb + et) * 0.5f, 0f),
                $"{s.Name}: el hueco es demasiado estrecho para una bruja. Pulsa [T] y entra como Gato.");

            // ---- Aldeano dormido y cofre con llave
            BuildSleepingVillager(npcsParent, s, house, villagerPrefab);

            CreateLevel3TreasureChest(
                gameplayParent,
                chestAtlas,
                s.ChestObjectName,
                new Vector3(s.ChestX, g + 0.5f, 0f),
                s.ChestId,
                s.ChestName,
                s.ChestKind,
                s.ChestNeedsKey,
                s.NeedKeyId,
                s.NeedKeyName,
                s.RewardKeyId,
                s.RewardKeyName,
                catCanOpen: true,
                sortingLayer: "Gameplay",
                sortingOrder: 1);

            return house;
        }

        private static void BuildWallWithGap(Transform parent, string name, float minX, float maxX, float gapBottom, float gapTop, int groundLayer, Color color)
        {
            if (gapBottom - StreetY > 0.01f)
            {
                AddSolidBox(parent, name + "_Lower", new Vector2(minX, StreetY - 0.05f), new Vector2(maxX, gapBottom), groundLayer, color);
            }
            if (HouseCeilingY - gapTop > 0.01f)
            {
                AddSolidBox(parent, name + "_Upper", new Vector2(minX, gapTop), new Vector2(maxX, HouseCeilingY), groundLayer, color);
            }
        }

        private static void BuildOpeningVisual(Transform facadeRoot, Sprite[] cainos, HouseOpening opening, float minX, float maxX, float bottom, float top, Color hole)
        {
            // Huecos oscuros sobre la fachada (orden 4: por encima del arte de la casa y por debajo de la bruja)
            switch (opening)
            {
                case HouseOpening.HighWindow:
                    AddRect(facadeRoot, "Window_Hole", new Vector2(minX - 0.08f, bottom), new Vector2(maxX + 0.08f, top - 0.02f), "Gameplay", 4, hole);
                    break;
                case HouseOpening.CatFlap:
                    AddRect(facadeRoot, "CatFlap_Hole", new Vector2(minX - 0.05f, bottom - 0.04f), new Vector2(maxX + 0.05f, top - 0.1f), "Gameplay", 4, hole);
                    break;
                case HouseOpening.Duct:
                    AddRect(facadeRoot, "Duct_Hole", new Vector2(minX - 0.04f, bottom - 0.04f), new Vector2(maxX + 0.04f, top - 0.06f), "Gameplay", 4, hole);
                    break;
            }
        }

        private static SleepingVillager BuildSleepingVillager(Transform parent, VillageHouseSpec s, Level3VillageHouse house, GameObject villagerPrefab)
        {
            const float g = StreetY;
            GameObject root = new GameObject("SleepingVillager_" + s.Id);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(s.BedX, g, 0f);

            Sprite bodySp = null;
            if (villagerPrefab != null)
            {
                SpriteRenderer pr = villagerPrefab.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r => r.sprite != null);
                if (pr != null) bodySp = pr.sprite;
            }

            // Cuerpo tumbado en la cama (girado 90°): al despertar se incorpora
            const float bodyScale = 0.85f;
            GameObject body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(bodyScale, bodyScale, 1f);
            body.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            SpriteRenderer bodySr = body.AddComponent<SpriteRenderer>();
            bodySr.sprite = bodySp;
            bodySr.sortingLayerName = "Gameplay";
            bodySr.sortingOrder = 1;
            if (bodySp != null)
            {
                VisibleExtents(bodySp, out float bx0, out float bx1, out float by0, out float by1);
                body.transform.localPosition = new Vector3((by0 + by1) * 0.5f * bodyScale, 0.78f - bx0 * bodyScale, 0f);
            }

            TextMeshPro zzz = AddWorldText(root.transform, "Zzz", new Vector3(0.75f, 1.75f, 0f), "Z z z", 3.2f,
                new Color(0.85f, 0.9f, 1f, 0.95f), "Gameplay", 1, new Vector2(2.4f, 0.8f));
            zzz.fontStyle = FontStyles.Bold;

            // Medidor de ruido: el relleno escala desde su borde izquierdo
            GameObject bar = new GameObject("NoiseBar");
            bar.transform.SetParent(root.transform, false);
            bar.transform.localPosition = new Vector3(0f, 2.35f, 0f);
            AddRect(bar.transform, "Bg", new Vector2(s.BedX - 0.8f, g + 2.27f), new Vector2(s.BedX + 0.8f, g + 2.43f), "Gameplay", 0, new Color(0.06f, 0.04f, 0.06f, 0.85f));
            GameObject pivot = new GameObject("FillPivot");
            pivot.transform.SetParent(bar.transform, false);
            pivot.transform.position = new Vector3(s.BedX - 0.76f, g + 2.35f, 0f);
            SpriteRenderer fill = AddRect(pivot.transform, "Fill", new Vector2(s.BedX - 0.76f, g + 2.3f), new Vector2(s.BedX + 0.76f, g + 2.4f), "Gameplay", 1, new Color(0.45f, 0.85f, 0.55f, 0.95f));
            AddWorldText(bar.transform, "Label", new Vector3(0f, 0.24f, 0f), "ruido", 1.6f, new Color(0.9f, 0.9f, 1f, 0.85f), "Gameplay", 1, new Vector2(1.6f, 0.4f));

            SleepingVillager sv = root.AddComponent<SleepingVillager>();
            sv.Configure(house, bodySr, zzz, pivot.transform, fill, bar);
            return sv;
        }

        private static void DecorateVillageStreet(Transform envParent, Sprite[] cainos)
        {
            Transform deco = new GameObject("Act3_Village_Decor").transform;
            deco.SetParent(envParent, false);

            CreateMysticLantern(deco, cainos, "VillageLamp_Entry", new Vector3(157.0f, 1f, 0f), new Color(1f, 0.78f, 0.35f, 1f));
            CreateMysticLantern(deco, cainos, "VillageLamp_Market_1", new Vector3(197.3f, 1f, 0f), new Color(1f, 0.78f, 0.35f, 1f));
            CreateMysticLantern(deco, cainos, "VillageLamp_Market_2", new Vector3(208.6f, 1f, 0f), new Color(1f, 0.78f, 0.35f, 1f));
            CreateMysticLantern(deco, cainos, "VillageLamp_Atrium", new Vector3(235.2f, 1f, 0f), new Color(1f, 0.85f, 0.4f, 1f));

            PlaceCainosSprite(deco, cainos, "Village_Barrel_1", "TX Village Props - Barrel", new Vector3(170.7f, 0.5f, 0f), 1.0f, true);
            PlaceCainosSprite(deco, cainos, "Village_Crate_Balcony", "TX Village Props - Crate Small", new Vector3(181.4f, 0.8f, 0f), 0.85f, true);
            PlaceCainosSprite(deco, cainos, "Village_Pumpkin_1", "TX Village Props - Pumpkin 02", new Vector3(200.4f, 0.5f, 0f), 1.0f, true);
            PlaceCainosSprite(deco, cainos, "Village_Barrel_2", "TX Village Props - Barrel", new Vector3(207.4f, 0.5f, 0f), 1.0f, true);
            PlaceCainosSprite(deco, cainos, "Village_Pumpkin_2", "TX Village Props - Pumpkin 01", new Vector3(251.0f, 0.5f, 0f), 0.9f, true);
        }

        private static void BuildSalemChurchMonumentAndGoal(
            Transform envParent,
            Transform gameplayParent,
            Sprite[] cainosSprites,
            Sprite wallSprite,
            Sprite roofSprite,
            Sprite doorSprite)
        {
            GameObject churchRoot = new GameObject("Salem_Church_Monument");
            churchRoot.transform.SetParent(envParent, false);
            churchRoot.transform.position = new Vector3(255f, 0.17f, 0f);

            // Nave de piedra (casa grande de la aldea teñida de piedra) y campanario sobre ella
            Sprite[] slices = AssetDatabase.LoadAllAssetsAtPath(Level2ArtPath + "houses_sprite_clean.png").OfType<Sprite>().ToArray();
            Sprite naveSp = slices.FirstOrDefault(s => s != null && s.name == "houses_sprite.png_2");
            Sprite towerSp = slices.FirstOrDefault(s => s != null && s.name == "houses_sprite.png_18");
            Color stone = new Color(0.66f, 0.65f, 0.78f, 1f);
            AddFitted(churchRoot.transform, "Church_StoneNave", naveSp, new Vector2(249.8f, 0.07f), new Vector2(260.2f, 10.6f), "Midground", 2, stone);
            AddFitted(churchRoot.transform, "Church_BellTower", towerSp, new Vector2(253.2f, 8.4f), new Vector2(256.8f, 15.2f), "Midground", 1, stone);
            PlaceCainosSprite(churchRoot.transform, cainosSprites, "Church_Cross_Top", "TX Village Props - Gravestone 02", new Vector3(255.0f, 15.0f, 0f), 1.1f);

            // Columnas del pórtico (apoyadas en el suelo)
            PlaceCainosSprite(churchRoot.transform, cainosSprites, "Church_Pillar_L", "TX Village Props - Stall Tent Pillar", new Vector3(252.9f, 1.85f, 0f), 1.2f, true);
            PlaceCainosSprite(churchRoot.transform, cainosSprites, "Church_Pillar_R", "TX Village Props - Stall Tent Pillar", new Vector3(257.1f, 1.85f, 0f), 1.2f, true);

            // Faroles consagrados a ambos lados del pórtico
            CreateMysticLantern(churchRoot.transform, cainosSprites, "Church_Lantern_Left", new Vector3(251.5f, 1.35f, 0f), new Color(1f, 0.85f, 0.35f, 1f));
            CreateMysticLantern(churchRoot.transform, cainosSprites, "Church_Lantern_Right", new Vector3(258.5f, 1.35f, 0f), new Color(1f, 0.85f, 0.35f, 1f));

            // Gran Puerta de la Iglesia (ChurchDoorGoal) en X: 255, Y: 0.17
            GameObject doorGo = new GameObject("Church_MainDoor_Goal");
            doorGo.transform.SetParent(gameplayParent, false);
            doorGo.transform.position = new Vector3(255f, 0.17f, 0f);

            Sprite wingSp = LoadFirstSprite(Level2ArtPath + "Village_Door_Locked.png");
            if (wingSp == null) wingSp = doorSprite;
            Color wingColor = new Color(0.78f, 0.62f, 0.45f, 1f);

            SpriteRenderer lwSr = AddFitted(doorGo.transform, "LeftDoorWing", wingSp, new Vector2(253.7f, 0.13f), new Vector2(255f, 3.1f), "Gameplay", 3, wingColor);
            SpriteRenderer rwSr = AddFitted(doorGo.transform, "RightDoorWing", wingSp, new Vector2(255f, 0.13f), new Vector2(256.3f, 3.1f), "Gameplay", 3, wingColor, true);

            GameObject lightGo = new GameObject("SanctuaryPortal_Light2D");
            lightGo.transform.SetParent(doorGo.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 2.0f, 0f);
            Light2D portalLight = lightGo.AddComponent<Light2D>();
            portalLight.lightType = Light2D.LightType.Point;
            portalLight.color = new Color(1f, 0.90f, 0.45f, 1f);
            portalLight.intensity = 1.65f;
            portalLight.pointLightOuterRadius = 5.0f;

            BoxCollider2D goalTrigger = doorGo.AddComponent<BoxCollider2D>();
            goalTrigger.isTrigger = true;
            goalTrigger.size = new Vector2(3.6f, 4.4f);
            goalTrigger.offset = new Vector2(0f, 2.2f);

            ChurchDoorGoal goal = doorGo.AddComponent<ChurchDoorGoal>();
            var soGoal = new SerializedObject(goal);
            soGoal.FindProperty("doorRenderer").objectReferenceValue = lwSr;
            soGoal.FindProperty("leftDoorWing").objectReferenceValue = lwSr != null ? lwSr.transform : null;
            soGoal.FindProperty("rightDoorWing").objectReferenceValue = rwSr != null ? rwSr.transform : null;
            soGoal.FindProperty("sanctuaryPortalLight").objectReferenceValue = portalLight;
            soGoal.ApplyModifiedPropertiesWithoutUndo();
        }

        public static LoreObject CreateIlluminatedLoreStand(
            Transform parent,
            Sprite[] cainosAtlas,
            string objectName,
            Vector3 pos,
            string loreId,
            string title,
            string message)
        {
            GameObject loreGo = new GameObject(objectName);
            loreGo.transform.SetParent(parent, false);
            loreGo.transform.position = pos;

            Sprite standSp = cainosAtlas.FirstOrDefault(s => s != null && s.name == "TX Village Props - Billboard");
            Sprite scrollSp = cainosAtlas.FirstOrDefault(s => s != null && s.name == "TX Village Props - Billboard Noti 01");

            SpriteRenderer sr = loreGo.AddComponent<SpriteRenderer>();
            sr.sprite = standSp != null ? standSp : LootPopupVisualizer.GetStarSprite();
            sr.sortingLayerName = "Gameplay";
            sr.sortingOrder = 3;

            if (scrollSp != null)
            {
                GameObject pageGo = new GameObject("LoreScroll_Visual");
                pageGo.transform.SetParent(loreGo.transform, false);
                pageGo.transform.localPosition = new Vector3(0f, 1.15f, 0f);
                pageGo.transform.localScale = Vector3.one * 1.15f;
                SpriteRenderer pSr = pageGo.AddComponent<SpriteRenderer>();
                pSr.sprite = scrollSp;
                pSr.color = new Color(1f, 0.95f, 0.75f, 1f);
                pSr.sortingLayerName = "Gameplay";
                pSr.sortingOrder = 4;
            }

            GameObject lightGo = new GameObject("Lore_Light2D");
            lightGo.transform.SetParent(loreGo.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            Light2D l2d = lightGo.AddComponent<Light2D>();
            l2d.lightType = Light2D.LightType.Point;
            l2d.color = new Color(0.55f, 0.88f, 1f, 1f);
            l2d.intensity = 1.25f;
            l2d.pointLightOuterRadius = 2.8f;

            PickupGlint glint = loreGo.AddComponent<PickupGlint>();
            glint.SetColor(new Color(0.55f, 0.88f, 1f, 1f));

            BoxCollider2D col = loreGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.0f, 2.2f);
            col.offset = new Vector2(0f, 1.1f);

            LoreObject lore = loreGo.AddComponent<LoreObject>();
            lore.Configure(loreId, title, message, 125);

            // El tablón apoya en el suelo (antes flotaba ~0.7u)
            SnapToGround(loreGo, 0.04f);
            return lore;
        }

        public static TreasureChest CreateLevel3TreasureChest(
            Transform parent,
            Sprite[] chestAtlas,
            string objectName,
            Vector3 pos,
            string chestId,
            string displayName,
            string chestKind,
            bool requiresKey,
            string requiredKeyId,
            string requiredKeyName,
            string rewardKeyId,
            string rewardKeyName,
            bool catCanOpen = false,
            string sortingLayer = "Gameplay",
            int sortingOrder = 4)
        {
            GameObject chestGo = new GameObject(objectName);
            chestGo.transform.SetParent(parent, false);
            chestGo.transform.position = pos;

            Sprite[] frames = new Sprite[7];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = chestAtlas.FirstOrDefault(s => s != null && s.name == $"TX Chest Animation {chestKind} {i}");
            }
            Sprite closedSp = frames[0];
            Sprite openSp = frames[6];

            SpriteRenderer sr = chestGo.AddComponent<SpriteRenderer>();
            sr.sprite = closedSp;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = sortingOrder;

            BoxCollider2D col = chestGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.1f, 1.8f);

            PickupGlint glint = chestGo.AddComponent<PickupGlint>();
            glint.SetColor(new Color(1f, 0.88f, 0.30f, 1f));

            GameObject lightGo = new GameObject("Chest_Light2D");
            lightGo.transform.SetParent(chestGo.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            Light2D l2d = lightGo.AddComponent<Light2D>();
            l2d.lightType = Light2D.LightType.Point;
            l2d.color = new Color(1f, 0.85f, 0.32f, 1f);
            l2d.intensity = 1.2f;
            l2d.pointLightOuterRadius = 2.6f;

            TreasureChest chest = chestGo.AddComponent<TreasureChest>();
            chest.Configure(chestId, displayName, closedSp, openSp, requiresKey, requiredKeyId, requiredKeyName);
            chest.ConfigureReward(
                ChestRewardType.Key,
                amount: 1,
                score: 250,
                grantedKeyId: rewardKeyId,
                grantedKeyName: rewardKeyName);

            // Animación de apertura (7 fotogramas) y la llave que sale flotando del cofre
            Sprite keySprite = LoadFirstSprite(Level2ArtPath + "Interactive_Key.png");
            chest.ConfigurePresentation(catCanOpen, frames.Where(f => f != null).ToArray(), keySprite);

            SnapToGround(chestGo, 0.03f);
            return chest;
        }

        public static LadderInteractable CreateClimbableLadder(
            Transform parent,
            Sprite[] cainosAtlas,
            string objectName,
            float centerX,
            float bottomY,
            float topY,
            Collider2D topPlatform)
        {
            GameObject ladderGo = new GameObject(objectName);
            ladderGo.transform.SetParent(parent, false);
            float height = Mathf.Max(1.5f, topY - bottomY);
            ladderGo.transform.position = new Vector3(centerX, bottomY + height * 0.5f, 0f);

            Sprite ladderSp = cainosAtlas.FirstOrDefault(s => s != null && s.name == "TX Village Props - Ladder 01");
            if (ladderSp != null)
            {
                SpriteRenderer sr = ladderGo.AddComponent<SpriteRenderer>();
                sr.sprite = ladderSp;
                sr.sortingLayerName = "Gameplay";
                sr.sortingOrder = 2;
                float nativeH = Mathf.Max(0.5f, ladderSp.bounds.size.y);
                ladderGo.transform.localScale = new Vector3(1f, height / nativeH, 1f);
            }

            BoxCollider2D col = ladderGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            LadderInteractable ladder = ladderGo.AddComponent<LadderInteractable>();
            ladder.Configure(objectName.ToLowerInvariant(), bottomY, topY, 1.1f, topPlatform);
            return ladder;
        }

        public static void CreateMysticLantern(Transform parent, Sprite[] cainosAtlas, string name, Vector3 pos, Color lightColor)
        {
            GameObject lanternGo = new GameObject(name);
            lanternGo.transform.SetParent(parent, false);
            lanternGo.transform.position = pos;
            lanternGo.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

            Sprite pillarSp = cainosAtlas.FirstOrDefault(s => s != null && s.name == "TX Village Props - Road Lamp Pillar");
            Sprite supportSp = cainosAtlas.FirstOrDefault(s => s != null && s.name == "TX Village Props - Road Lamp Support");
            Sprite lampSp = cainosAtlas.FirstOrDefault(s => s != null && s.name == "TX Village Props - Road Lamp Light On");
            float top = 2.9f;
            if (pillarSp != null)
            {
                SpriteRenderer sr = lanternGo.AddComponent<SpriteRenderer>();
                sr.sprite = pillarSp;
                sr.sortingLayerName = "Midground";
                sr.sortingOrder = 2;
                VisibleExtents(pillarSp, out _, out _, out _, out float maxY);
                top = maxY;
            }

            // Brazo y farol colgante: antes solo había un poste y la luz flotaba en el aire
            if (supportSp != null)
            {
                GameObject support = new GameObject("Lamp_Support");
                support.transform.SetParent(lanternGo.transform, false);
                support.transform.localPosition = new Vector3(0f, top - 0.12f, 0f);
                SpriteRenderer ssr = support.AddComponent<SpriteRenderer>();
                ssr.sprite = supportSp;
                ssr.sortingLayerName = "Midground";
                ssr.sortingOrder = 2;
            }

            Vector3 lampLocal = new Vector3(0.82f, top - 0.45f, 0f);
            if (lampSp != null)
            {
                GameObject lamp = new GameObject("Lamp_Light");
                lamp.transform.SetParent(lanternGo.transform, false);
                lamp.transform.localPosition = lampLocal;
                SpriteRenderer lsr = lamp.AddComponent<SpriteRenderer>();
                lsr.sprite = lampSp;
                lsr.sortingLayerName = "Midground";
                lsr.sortingOrder = 3;
            }

            GameObject lightChild = new GameObject("MysticLantern_Light2D");
            lightChild.transform.SetParent(lanternGo.transform, false);
            lightChild.transform.localPosition = lampLocal + new Vector3(0f, -0.3f, 0f);

            Light2D l2d = lightChild.AddComponent<Light2D>();
            l2d.lightType = Light2D.LightType.Point;
            l2d.color = lightColor;
            l2d.intensity = 1.45f;
            l2d.pointLightInnerRadius = 0.4f;
            l2d.pointLightOuterRadius = 4.5f;
            l2d.falloffIntensity = 0.5f;

            lightChild.AddComponent<LanternFlicker>();

            // El poste se apoya en el suelo (antes flotaba más de 1u)
            SnapToGround(lanternGo, 0.05f);
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

        private static void BuildAct1HighCanopyRoute(
            Transform envParent,
            Transform gameplayParent,
            Transform pickupsParent,
            int groundLayer,
            Sprite logASprite,
            Sprite logCSprite,
            GameObject potBlindPrefab,
            GameObject heartPrefab)
        {
            Transform canopyRoot = new GameObject("Act1_HighCanopy_Route").transform;
            canopyRoot.SetParent(envParent, false);

            // 1. Plataforma copa oeste (X: 14.5, Y: 5.2)
            CreateOneWayPlatform(canopyRoot, "Platform_Canopy_West1", logASprite, new Vector2(14.5f, 5.2f), 1.35f, groundLayer);
            InstantiatePickup(pickupsParent, potBlindPrefab, "PotionPickup_Canopy_Blind", new Vector3(14.5f, 5.8f, 0f));

            // 2. Plataforma copa central alta (X: 22.0, Y: 7.0) con Reliquia Secreta #1
            CreateOneWayPlatform(canopyRoot, "Platform_Canopy_CenterHigh", logCSprite, new Vector2(22.0f, 7.0f), 1.55f, groundLayer);
            Sprite loreBookSprite = LoadFirstSprite(Level2ArtPath + "Interactive_LoreBook.png");
            GameObject relic1Go = new GameObject("SecretRelic_Grimoire");
            relic1Go.transform.SetParent(gameplayParent, false);
            relic1Go.transform.position = new Vector3(22.0f, 7.65f, 0f);
            BoxCollider2D relic1Col = relic1Go.AddComponent<BoxCollider2D>();
            relic1Col.isTrigger = true;
            relic1Col.size = new Vector2(1.2f, 1.2f);
            SpriteRenderer relic1Sr = relic1Go.AddComponent<SpriteRenderer>();
            relic1Sr.sprite = loreBookSprite;
            relic1Sr.sortingLayerName = "Gameplay";
            relic1Sr.sortingOrder = 5;
            Light2D relic1Light = new GameObject("RelicLight").AddComponent<Light2D>();
            relic1Light.transform.SetParent(relic1Go.transform, false);
            relic1Light.lightType = Light2D.LightType.Point;
            relic1Light.color = new Color(0.95f, 0.78f, 0.25f, 1f);
            relic1Light.intensity = 1.1f;
            relic1Light.pointLightOuterRadius = 3f;
            Level3SecretRelic relic1 = relic1Go.AddComponent<Level3SecretRelic>();
            relic1.Configure("relic_grimoire", "Grimorio Olvidado de Salem", 350, loreBookSprite, new Color(0.95f, 0.78f, 0.25f, 1f));

            // 3. Plataforma copa este (X: 29.5, Y: 5.5)
            CreateOneWayPlatform(canopyRoot, "Platform_Canopy_East1", logASprite, new Vector2(29.5f, 5.5f), 1.35f, groundLayer);
            InstantiatePickup(pickupsParent, heartPrefab, "HeartPickup_Canopy_West", new Vector3(29.5f, 6.05f, 0f));
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

            // Bloque físico del tronco caído:
            // - Cara inferior en Y = 1.325: deja 1.15u de hueco (el gato mide 0.56u con su collider; la bruja 1.6u).
            // - Cara superior en Y = 2.575: piso caminable del guardia.
            const float roofBottom = 1.325f;
            const float roofTop = 2.575f;
            GameObject ceilingGo = new GameObject("Tunnel_Roof_Solid");
            ceilingGo.transform.SetParent(tunnelRoot.transform, false);
            ceilingGo.layer = groundLayer;
            ceilingGo.transform.position = new Vector3(centerX, (roofBottom + roofTop) * 0.5f, 0f);

            BoxCollider2D ceilingCol = ceilingGo.AddComponent<BoxCollider2D>();
            ceilingCol.size = new Vector2(width, roofTop - roofBottom);
            ceilingCol.offset = Vector2.zero;

            // Visual ajustado al collider: dos troncos cuyo borde visible coincide con las caras físicas
            if (logSprite != null)
            {
                VisibleExtents(logSprite, out float vx0, out float vx1, out float vy0, out float vy1);
                const int visualSegments = 2;
                float segW = width / visualSegments;
                float sx = (segW + 0.35f) / Mathf.Max(0.01f, vx1 - vx0);
                float sy = (roofTop - roofBottom + 0.2f) / Mathf.Max(0.01f, vy1 - vy0);
                float centerY = (roofBottom + roofTop) * 0.5f;
                for (int i = 0; i < visualSegments; i++)
                {
                    GameObject vis = new GameObject($"TrunkRoof_Visual_{i}");
                    vis.transform.SetParent(ceilingGo.transform, false);
                    vis.transform.localScale = new Vector3(sx, sy, 1f);
                    float wantCenterX = -width * 0.5f + segW * (i + 0.5f);
                    vis.transform.localPosition = new Vector3(wantCenterX - (vx0 + vx1) * 0.5f * sx, (roofTop + 0.1f - centerY) - vy1 * sy, 0f);
                    SpriteRenderer sr = vis.AddComponent<SpriteRenderer>();
                    sr.sprite = logSprite;
                    sr.flipX = i % 2 == 1;
                    sr.color = new Color(0.55f, 0.42f, 0.30f, 1f);
                    sr.sortingLayerName = "Gameplay";
                    sr.sortingOrder = 2;
                }
            }

            // Sombra translúcida del interior del tronco (no tiene collider)
            AddRect(tunnelRoot.transform, "Tunnel_Inner_Shadow", new Vector2(xStart, StreetY), new Vector2(xEnd, roofBottom),
                "Midground", 3, new Color(0.08f, 0.05f, 0.04f, 0.55f));

            // Triggers CatOnlyPassage en la entrada y salida
            CreateCatOnlyTrigger(gameplayParent, "CatOnlyPassage_Trunk_Entry", new Vector3(xStart - 0.35f, 1.05f, 0f),
                "Raíces bajas: Pulsa [T] para transformarte en Gato y pasar por debajo.");
            CreateCatOnlyTrigger(gameplayParent, "CatOnlyPassage_Trunk_Exit", new Vector3(xEnd + 0.35f, 1.05f, 0f),
                "Raíces bajas: Pulsa [T] para transformarte en Gato y pasar por debajo.");

            // Recompensas dentro del túnel de gato (sus colliders son triggers y no tocan el techo)
            InstantiatePickup(pickupsParent, potSleepPrefab, "PotionPickup_Tunnel_Sleep", new Vector3(43f, 0.6f, 0f));
            InstantiatePickup(pickupsParent, heartPrefab, "HeartPickup_Tunnel", new Vector3(45f, 0.6f, 0f));
            InstantiatePickup(pickupsParent, potBlindPrefab, "PotionPickup_Tunnel_Blind", new Vector3(47f, 0.6f, 0f));
            ShrinkPickupTriggers(pickupsParent, "PotionPickup_Tunnel_Sleep", "HeartPickup_Tunnel", "PotionPickup_Tunnel_Blind");

            // Reliquia Secreta #2: Talismán de Hécate oculto dentro del tronco hueco (accesible solo como Gato)
            Sprite relicSprite2 = LoadFirstSprite(Level2ArtPath + "Interactive_Key.png");
            GameObject relic2Go = new GameObject("SecretRelic_Talisman");
            relic2Go.transform.SetParent(gameplayParent, false);
            relic2Go.transform.position = new Vector3(46f, 0.72f, 0f);
            BoxCollider2D relic2Col = relic2Go.AddComponent<BoxCollider2D>();
            relic2Col.isTrigger = true;
            relic2Col.size = new Vector2(1.1f, 1.1f);
            SpriteRenderer relic2Sr = relic2Go.AddComponent<SpriteRenderer>();
            relic2Sr.sprite = relicSprite2;
            relic2Sr.color = new Color(0.4f, 1f, 0.8f, 1f);
            relic2Sr.sortingLayerName = "Gameplay";
            relic2Sr.sortingOrder = 5;
            Light2D relic2Light = new GameObject("RelicLight").AddComponent<Light2D>();
            relic2Light.transform.SetParent(relic2Go.transform, false);
            relic2Light.lightType = Light2D.LightType.Point;
            relic2Light.color = new Color(0.35f, 0.95f, 0.75f, 1f);
            relic2Light.intensity = 1.1f;
            relic2Light.pointLightOuterRadius = 2.5f;
            Level3SecretRelic relic2 = relic2Go.AddComponent<Level3SecretRelic>();
            relic2.Configure("relic_talisman", "Talismán de Hécate", 350, relicSprite2, new Color(0.35f, 0.95f, 0.75f, 1f));

            // Guardia patrullando sobre el tronco (X: 41 a 49)
            Transform[] wpRoof = CreateWaypoints(npcsParent, "Waypoints_Guard_TrunkRoof", new Vector2(41.2f, roofTop + 0.02f), new Vector2(48.8f, roofTop + 0.02f));
            CreateGuard(npcsParent, guardPrefab, "Guard_TrunkRoof", new Vector3(45f, roofTop + 0.02f, 0f), wpRoof, PatrolMode.PingPong, groundMask);
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

            // 6. Plataforma Móvil Rúnica sobre el abismo (oscila entre X: 56.5 y 63.5 en Y: 2.8)
            GameObject movPlatGo = new GameObject("MovingPlatform_DarkRiver");
            movPlatGo.transform.SetParent(envParent, false);
            movPlatGo.layer = groundLayer;
            movPlatGo.transform.position = new Vector3(56.5f, 2.8f, 0f);

            BoxCollider2D movCol = movPlatGo.AddComponent<BoxCollider2D>();
            movCol.size = new Vector2(3.6f, 0.45f);
            movCol.usedByEffector = true;
            PlatformEffector2D movEff = movPlatGo.AddComponent<PlatformEffector2D>();
            movEff.useOneWay = true;
            movEff.surfaceArc = 170f;

            SpriteRenderer movSr = movPlatGo.AddComponent<SpriteRenderer>();
            movSr.sprite = logCSprite;
            movSr.color = new Color(0.72f, 0.85f, 1f, 1f);
            movSr.sortingLayerName = "Gameplay";
            movSr.sortingOrder = 2;

            Light2D movLight = new GameObject("RuneLight").AddComponent<Light2D>();
            movLight.transform.SetParent(movPlatGo.transform, false);
            movLight.lightType = Light2D.LightType.Point;
            movLight.color = new Color(0.4f, 0.85f, 1f, 1f);
            movLight.intensity = 1.0f;
            movLight.pointLightOuterRadius = 3f;

            Level3MovingPlatform movComp = movPlatGo.AddComponent<Level3MovingPlatform>();
            movComp.Configure(new Vector3(56.5f, 2.8f, 0f), new Vector3(63.5f, 2.8f, 0f), 2.2f, 0.7f);

            // 7. Peligro ambiental: Géiser de vaho tóxico al pie del acantilado oeste (X: 53.6, Y: 0.25)
            GameObject hazardGo = new GameObject("EnvironmentalHazard_RavineGeyser");
            hazardGo.transform.SetParent(gameplayParent, false);
            hazardGo.transform.position = new Vector3(53.6f, 0.35f, 0f);
            BoxCollider2D hazCol = hazardGo.AddComponent<BoxCollider2D>();
            hazCol.isTrigger = true;
            hazCol.size = new Vector2(2.2f, 2.4f);

            SpriteRenderer hazSr = hazardGo.AddComponent<SpriteRenderer>();
            hazSr.sprite = EnsureSoftFogSprite();
            hazSr.color = new Color(0.55f, 0.15f, 0.85f, 0.8f);
            hazSr.sortingLayerName = "Gameplay";
            hazSr.sortingOrder = 6;

            Light2D hazLight = new GameObject("HazardLight").AddComponent<Light2D>();
            hazLight.transform.SetParent(hazardGo.transform, false);
            hazLight.lightType = Light2D.LightType.Point;
            hazLight.color = new Color(0.6f, 0.2f, 0.9f, 1f);
            hazLight.intensity = 1.2f;
            hazLight.pointLightOuterRadius = 3.2f;

            hazardGo.AddComponent<Level3EnvironmentalHazard>();

            // 8. Peligro visible: acantilados, vallas rotas, niebla violeta y cartel
            DecorateRavineDanger(envParent, ravineStartX, ravineEndX);
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
                "Nicho estrecho: Usa forma de Gato [T] para recoger las pociones ocultas.");

            // Recargas de PotionPickup (Sleep) ocultas tras el CatOnlyPassage lateral en X: 82
            InstantiatePickup(pickupsParent, potSleepPrefab, "PotionPickup_Bastion_Sleep_1", new Vector3(81.6f, 0.58f, 0f));
            InstantiatePickup(pickupsParent, potSleepPrefab, "PotionPickup_Bastion_Sleep_2", new Vector3(83.2f, 0.58f, 0f));

            // Mecanismo del Acueducto: Compuerta Rúnica y Palancas (abre el paso directo bajo el bastión)
            Sprite[] cainos = AssetDatabase.LoadAllAssetsAtPath(CainosAtlasPath).OfType<Sprite>().ToArray();
            Sprite leverSprite = FindAtlasSprite(cainos, "TX Village Props - Lever 01") ?? logASprite;

            GameObject mechGateGo = new GameObject("MechanismGate_BastionArch");
            mechGateGo.transform.SetParent(bastionRoot.transform, false);
            mechGateGo.layer = groundLayer;
            mechGateGo.transform.position = new Vector3(84.8f, 2.0f, 0f);

            BoxCollider2D gateCol = mechGateGo.AddComponent<BoxCollider2D>();
            gateCol.size = new Vector2(0.8f, 3.6f);

            SpriteRenderer gateSr = mechGateGo.AddComponent<SpriteRenderer>();
            gateSr.sprite = logBSprite;
            gateSr.color = new Color(0.35f, 0.45f, 0.55f, 1f);
            gateSr.sortingLayerName = "Gameplay";
            gateSr.sortingOrder = 3;

            Level3MechanismGate mechGate = mechGateGo.AddComponent<Level3MechanismGate>();
            mechGate.Configure("gate_bastion_arch", "Compuerta del Acueducto", mechGateGo.transform, gateCol, 3.6f);

            // Interruptor de Runa #1: En las ruinas superiores (X: 84f, Y: 5.6f) - requiere volar con escoba
            GameObject switch1Go = new GameObject("RuneSwitch_BastionHigh");
            switch1Go.transform.SetParent(gameplayParent, false);
            switch1Go.transform.position = new Vector3(84f, 5.6f, 0f);
            BoxCollider2D sw1Col = switch1Go.AddComponent<BoxCollider2D>();
            sw1Col.isTrigger = true;
            sw1Col.size = new Vector2(1.8f, 1.8f);
            SpriteRenderer sw1Sr = switch1Go.AddComponent<SpriteRenderer>();
            sw1Sr.sprite = leverSprite;
            sw1Sr.sortingLayerName = "Gameplay";
            sw1Sr.sortingOrder = 4;
            Light2D sw1Light = new GameObject("SwitchLight").AddComponent<Light2D>();
            sw1Light.transform.SetParent(switch1Go.transform, false);
            sw1Light.lightType = Light2D.LightType.Point;
            sw1Light.color = new Color(0.3f, 1f, 0.7f, 1f);
            sw1Light.intensity = 0.9f;
            sw1Light.pointLightOuterRadius = 2.5f;

            Level3RuneSwitch sw1 = switch1Go.AddComponent<Level3RuneSwitch>();
            sw1.Configure("switch_bastion_high", "Runa de la Torre", mechGate);
            mechGate.AddRequiredSwitch(sw1);

            // Interruptor de Runa #2: En el nicho de gato (X: 81.2f, Y: 0.75f) - accesible como gato
            GameObject switch2Go = new GameObject("RuneSwitch_BastionCat");
            switch2Go.transform.SetParent(gameplayParent, false);
            switch2Go.transform.position = new Vector3(81.2f, 0.75f, 0f);
            BoxCollider2D sw2Col = switch2Go.AddComponent<BoxCollider2D>();
            sw2Col.isTrigger = true;
            sw2Col.size = new Vector2(1.6f, 1.6f);
            SpriteRenderer sw2Sr = switch2Go.AddComponent<SpriteRenderer>();
            sw2Sr.sprite = leverSprite;
            sw2Sr.sortingLayerName = "Gameplay";
            sw2Sr.sortingOrder = 4;
            Light2D sw2Light = new GameObject("SwitchLight").AddComponent<Light2D>();
            sw2Light.transform.SetParent(switch2Go.transform, false);
            sw2Light.lightType = Light2D.LightType.Point;
            sw2Light.color = new Color(0.3f, 1f, 0.7f, 1f);
            sw2Light.intensity = 0.9f;
            sw2Light.pointLightOuterRadius = 2.2f;

            Level3RuneSwitch sw2 = switch2Go.AddComponent<Level3RuneSwitch>();
            sw2.Configure("switch_bastion_cat", "Runa de las Raíces", mechGate);
            mechGate.AddRequiredSwitch(sw2);

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
                tile.transform.localScale = new Vector3(1.25f, 0.55f, 1f);
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

            // GroundStrip alinea la línea de pasto denso con la superficie física y dibuja la franja delantera:
            // antes las baldosas quedaban ~0.28u por debajo y todo parecía flotar sobre el pasto.
            Sprite fringe = AssetDatabase.LoadAssetAtPath<Sprite>(FringeSpritePath);
            GroundStrip strip = g.AddComponent<GroundStrip>();
            strip.Configure(sprite, fringe);
            Physics2D.SyncTransforms();
            strip.Rebuild();
        }

        public static GameObject CreateOneWayPlatform(Transform parent, string name, Sprite sprite, Vector2 pos, float scaleX, int layer, float visualThickness = 0.5f)
        {
            GameObject plat = new GameObject(name);
            plat.transform.SetParent(parent, false);
            plat.layer = layer;
            plat.transform.position = pos;

            // Collider sin escalar: cara superior en pos.y + 0.27 (igual que antes)
            float w = sprite != null ? (sprite.rect.width / sprite.pixelsPerUnit) : 3.5f;
            const float targetTop = 0.27f;
            const float colThickness = 0.35f;
            BoxCollider2D col = plat.AddComponent<BoxCollider2D>();
            col.size = new Vector2(w * scaleX * 0.92f, colThickness);
            col.offset = new Vector2(0f, targetTop - (colThickness * 0.5f));
            col.usedByEffector = true;

            PlatformEffector2D effector = plat.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.useOneWayGrouping = true;
            effector.surfaceArc = 170f;

            // Tronco visual mucho más delgado (antes medía ~2u de alto) con su borde superior sobre el collider
            if (sprite != null)
            {
                VisibleExtents(sprite, out float vx0, out float vx1, out float vy0, out float vy1);
                float sy = visualThickness / Mathf.Max(0.05f, vy1 - vy0);
                GameObject vis = new GameObject("Visual");
                vis.transform.SetParent(plat.transform, false);
                vis.transform.localScale = new Vector3(scaleX, sy, 1f);
                vis.transform.localPosition = new Vector3(-(vx0 + vx1) * 0.5f * scaleX, targetTop + 0.06f - vy1 * sy, 0f);
                SpriteRenderer sr = vis.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingLayerName = "Gameplay";
                sr.sortingOrder = 2;
            }

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
            barsVisual.transform.localPosition = new Vector3(0f, 2.21f, 0f);
            barsVisual.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

            SpriteRenderer sr = barsVisual.AddComponent<SpriteRenderer>();
            sr.sprite = gateSprite;
            sr.color = new Color(0.85f, 0.78f, 0.62f, 1f);
            sr.sortingLayerName = "Gameplay";
            sr.sortingOrder = 3;

            // Luz cálida sobre el portón para que destaque en el bosque
            GameObject gateLightGo = new GameObject("Gate_LanternLight2D");
            gateLightGo.transform.SetParent(gateGo.transform, false);
            gateLightGo.transform.localPosition = new Vector3(0f, 2.85f, 0f);
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
            soGate.FindProperty("lockedOverlay").objectReferenceValue = BuildGateLockedOverlay(gateGo.transform);
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

        /// <summary>
        /// El arte del portón muestra las hojas abiertas: mientras está cerrado se cubre el hueco con dos puertas
        /// con cadena y candado (LockedGateInteractable lo oculta al abrirse).
        /// </summary>
        private static GameObject BuildGateLockedOverlay(Transform gate)
        {
            GameObject overlay = new GameObject("Gate_LockedOverlay");
            overlay.transform.SetParent(gate, false);
            Sprite door = LoadFirstSprite(Level2ArtPath + "Village_Door_Locked.png");
            float baseX = gate.position.x;
            float baseY = gate.position.y;
            Color tint = new Color(0.9f, 0.84f, 0.74f, 1f);
            AddFitted(overlay.transform, "LockedWing_Left", door, new Vector2(baseX - 1.2f, baseY - 0.05f), new Vector2(baseX, baseY + 2.6f), "Gameplay", 4, tint);
            AddFitted(overlay.transform, "LockedWing_Right", door, new Vector2(baseX, baseY - 0.05f), new Vector2(baseX + 1.2f, baseY + 2.6f), "Gameplay", 4, tint, true);
            return overlay;
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

            bool elevated = baseY > 0.5f;
            if (clusterSprite != null)
            {
                GameObject cl = new GameObject("Cluster_Bg");
                cl.transform.SetParent(cf.transform, false);
                float clusterSy = elevated ? height / 8f : 0.75f;
                cl.transform.localPosition = new Vector3(0f, elevated ? -0.1f + 4f * clusterSy : height * 0.55f, 0f);
                cl.transform.localScale = new Vector3(Mathf.Clamp(width / 6f, 0.55f, 1.2f), clusterSy, 1f);
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
                if (elevated)
                {
                    // Sobre una plataforma: el tallo arranca en la base y no cuelga por debajo del piso
                    float sy = height * 1.1f / 8f;
                    st.transform.localPosition = new Vector3(lx, -0.1f + 4f * sy, 0f);
                    st.transform.localScale = new Vector3(0.6f, sy, 1f);
                }
                else
                {
                    st.transform.localPosition = new Vector3(lx, height * 0.5f, 0f);
                    st.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
                }
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

            // Cartel un 38% más pequeño (antes medía 3x4u) y apoyado en el suelo
            const float signScale = 0.62f;
            float signTop = 2.0f;
            if (signSprite != null)
            {
                GameObject vis = new GameObject("Sign_Visual");
                vis.transform.SetParent(signObj.transform, false);
                vis.transform.localScale = new Vector3(signScale, signScale, 1f);
                SpriteRenderer sr = vis.AddComponent<SpriteRenderer>();
                sr.sprite = signSprite;
                sr.sortingLayerName = "Midground";
                sr.sortingOrder = 2;
                VisibleExtents(signSprite, out _, out _, out _, out float maxY);
                signTop = maxY * signScale;
            }

            GameObject textObj = new GameObject("Sign_Text");
            textObj.transform.SetParent(signObj.transform, false);
            textObj.transform.localPosition = new Vector3(0f, signTop + 0.75f, 0f);

            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 2.4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.96f, 0.92f, 0.78f);
            tmp.sortingLayerID = SortingLayer.NameToID("Gameplay");
            tmp.sortingOrder = 10;

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(4.2f, 1.6f);

            WorldSign ws = signObj.AddComponent<WorldSign>();
            var so = new SerializedObject(ws);
            so.FindProperty("textMesh").objectReferenceValue = tmp;
            so.FindProperty("message").stringValue = text;
            so.FindProperty("detectionRadius").floatValue = 2.6f;
            so.FindProperty("messageDuration").floatValue = 3.5f;
            so.ApplyModifiedPropertiesWithoutUndo();

            SnapToGround(signObj, 0.06f);
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

            // Todos los props se apoyan en el suelo con GroundAnchor (antes flotaban con alturas fijas)
            CreateMysticLantern(decoRoot, cainosSprites, "Lamp_Start", new Vector3(3.2f, 1.35f, 0f), new Color(1f, 0.8f, 0.38f, 1f));
            PlaceCainosSprite(decoRoot, cainosSprites, "Pumpkins_Start", "TX Village Props - Pumpkin 01", new Vector3(8.0f, 0.55f, 0f), 1.0f, true);
            PlaceCainosSprite(decoRoot, cainosSprites, "Pumpkins_Start_Small", "TX Village Props - Pumpkin 02", new Vector3(8.75f, 0.55f, 0f), 1.0f, true);
            PlaceCainosSprite(decoRoot, cainosSprites, "Barrel_Gate1", "TX Village Props - Barrel", new Vector3(30.4f, 0.65f, 0f), 1.0f, true);
            PlaceCainosSprite(decoRoot, cainosSprites, "Gravestone_Ravine", "TX Village Props - Gravestone 03", new Vector3(50.8f, 0.65f, 0f), 1.0f, true);
            PlaceCainosSprite(decoRoot, cainosSprites, "Scarecrow_Bastion", "TX Village Props - Scarecrow", new Vector3(72.5f, 0.95f, 0f), 1.0f, true);
            PlaceCainosSprite(decoRoot, cainosSprites, "Crate_Bastion", "TX Village Props - Crate Large", new Vector3(99.0f, 0.72f, 0f), 0.9f, true);
        }

        private static GameObject PlaceCainosSprite(Transform parent, Sprite[] atlas, string objName, string spriteName, Vector3 pos, float scale, bool ground = false)
        {
            Sprite sp = atlas.FirstOrDefault(s => s != null && s.name == spriteName);
            if (sp == null) return null;

            GameObject go = new GameObject(objName);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingLayerName = "Midground";
            sr.sortingOrder = 2;

            if (ground) SnapToGround(go, 0.04f);
            return go;
        }

        // =========================================================================
        // HELPERS DE ARTE, SUELO Y UI DEL NIVEL 3
        // =========================================================================

        private static Transform NewChild(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Sprite LoadFirstSprite(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        }

        private static Sprite FindAtlasSprite(Sprite[] atlas, string name)
        {
            return atlas != null ? atlas.FirstOrDefault(s => s != null && s.name == name) : null;
        }

        /// <summary>Límites del área opaca del sprite (malla ajustada), en unidades locales respecto al pivote.</summary>
        private static void VisibleExtents(Sprite sp, out float minX, out float maxX, out float minY, out float maxY)
        {
            minX = minY = float.MaxValue;
            maxX = maxY = float.MinValue;
            Vector2[] verts = sp != null ? sp.vertices : null;
            if (verts == null || verts.Length == 0)
            {
                Bounds b = sp != null ? sp.bounds : new Bounds(Vector3.zero, Vector3.one);
                minX = b.min.x;
                maxX = b.max.x;
                minY = b.min.y;
                maxY = b.max.y;
                return;
            }
            foreach (Vector2 v in verts)
            {
                minX = Mathf.Min(minX, v.x);
                maxX = Mathf.Max(maxX, v.x);
                minY = Mathf.Min(minY, v.y);
                maxY = Mathf.Max(maxY, v.y);
            }
        }

        /// <summary>Crea un sprite cuyo área visible ocupa exactamente el rectángulo [min, max] de mundo.</summary>
        private static SpriteRenderer AddFitted(Transform parent, string name, Sprite sp, Vector2 min, Vector2 max, string layer, int order, Color color, bool flipX = false)
        {
            if (sp == null) return null;
            VisibleExtents(sp, out float vx0, out float vx1, out float vy0, out float vy1);
            float sx = (max.x - min.x) / Mathf.Max(0.01f, vx1 - vx0);
            float sy = (max.y - min.y) / Mathf.Max(0.01f, vy1 - vy0);
            float cx = (vx0 + vx1) * 0.5f * (flipX ? -1f : 1f);

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(sx, sy, 1f);
            go.transform.position = new Vector3((min.x + max.x) * 0.5f - cx * sx, min.y - vy0 * sy, 0f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.flipX = flipX;
            sr.color = color;
            sr.sortingLayerName = layer;
            sr.sortingOrder = order;
            return sr;
        }

        private static SpriteRenderer AddRect(Transform parent, string name, Vector2 min, Vector2 max, string layer, int order, Color color)
        {
            Sprite square = LoadFirstSprite(PlaceholderSquarePath);
            return AddFitted(parent, name, square, min, max, layer, order, color);
        }

        private static GameObject AddSolidBox(Transform parent, string name, Vector2 min, Vector2 max, int layer, Color visualColor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = layer;
            go.transform.position = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, 0f);
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = max - min;
            if (visualColor.a > 0f)
            {
                AddRect(go.transform, "Visual", min, max, "Midground", 7, visualColor);
            }
            return go;
        }

        /// <summary>Repisa de un solo sentido con collider sin escalar y cara superior exacta en <paramref name="topY"/>.</summary>
        private static GameObject CreateOneWayLedge(Transform parent, string name, float minX, float maxX, float topY, int layer, Sprite sprite, bool drawVisual)
        {
            const float thickness = 0.35f;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = layer;
            go.transform.position = new Vector3((minX + maxX) * 0.5f, topY - thickness * 0.5f, 0f);

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(maxX - minX, thickness);
            col.usedByEffector = true;
            PlatformEffector2D effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.useOneWayGrouping = true;
            effector.surfaceArc = 170f;

            if (drawVisual)
            {
                if (sprite != null)
                {
                    AddFitted(go.transform, "Visual", sprite, new Vector2(minX - 0.05f, topY - 0.55f), new Vector2(maxX + 0.05f, topY + 0.06f), "Gameplay", 2, Color.white);
                }
                else
                {
                    AddRect(go.transform, "Visual", new Vector2(minX, topY - 0.16f), new Vector2(maxX, topY), "Midground", 7, new Color(0.34f, 0.22f, 0.13f, 1f));
                }
            }
            return go;
        }

        /// <summary>Apoya la base visible del objeto sobre la superficie Ground más cercana (y lo mantiene así al jugar).</summary>
        private static GroundAnchor SnapToGround(GameObject go, float sink)
        {
            GroundAnchor anchor = go.GetComponent<GroundAnchor>();
            if (anchor == null) anchor = go.AddComponent<GroundAnchor>();
            anchor.Sink = sink;

            // Fijar el sprite de referencia: en juego se crean auras/brillos hijos más grandes que no deben contar
            SpriteRenderer main = go.GetComponent<SpriteRenderer>();
            if (main == null)
            {
                float best = -1f;
                foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr.sprite == null) continue;
                    float area = sr.bounds.size.x * sr.bounds.size.y;
                    if (area > best)
                    {
                        best = area;
                        main = sr;
                    }
                }
            }
            if (main != null)
            {
                var so = new SerializedObject(anchor);
                SerializedProperty targetProp = so.FindProperty("target");
                if (targetProp != null) targetProp.objectReferenceValue = main;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            anchor.Snap();
            return anchor;
        }

        private static TextMeshPro AddWorldText(Transform parent, string name, Vector3 localPos, string text, float size, Color color, string layer, int order, Vector2 box)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.sortingLayerID = SortingLayer.NameToID(layer);
            tmp.sortingOrder = order;
            go.GetComponent<RectTransform>().sizeDelta = box;
            return tmp;
        }

        /// <summary>Reduce el trigger de recogida de los objetos dentro del túnel para que no rocen el techo.</summary>
        private static void ShrinkPickupTriggers(Transform parent, params string[] names)
        {
            foreach (string n in names)
            {
                Transform t = parent.Find(n);
                if (t == null) continue;
                foreach (Collider2D c in t.GetComponentsInChildren<Collider2D>(true))
                {
                    c.isTrigger = true;
                    if (c is BoxCollider2D box)
                    {
                        Vector3 s = c.transform.lossyScale;
                        box.size = new Vector2(1.0f / Mathf.Max(0.01f, Mathf.Abs(s.x)), 0.9f / Mathf.Max(0.01f, Mathf.Abs(s.y)));
                    }
                }
            }
        }

        /// <summary>Textura de niebla suave (se genera una sola vez como asset para que la escena la guarde).</summary>
        private static Sprite EnsureSoftFogSprite()
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(FogSpritePath);
            if (existing != null) return existing;

            Directory.CreateDirectory(Path.GetDirectoryName(FogSpritePath));
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - r) * (x - r) + (y - r) * (y - r)) / r;
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            File.WriteAllBytes(FogSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(FogSpritePath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(FogSpritePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.spritePixelsPerUnit = 64f;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(FogSpritePath);
        }

        /// <summary>
        /// Barranco del Río Oscuro: bordes de acantilado, vallas y estacas rotas, niebla violeta y un cartel de
        /// peligro para que el vacío se lea a simple vista (antes parecía suelo normal).
        /// </summary>
        private static void DecorateRavineDanger(Transform envParent, float x0, float x1)
        {
            Sprite[] cainos = AssetDatabase.LoadAllAssetsAtPath(CainosAtlasPath).OfType<Sprite>().ToArray();
            Transform root = new GameObject("Ravine_DangerDressing").transform;
            root.SetParent(envParent, false);

            // Paredes del acantilado y oscuridad del fondo
            Color cliff = new Color(0.09f, 0.07f, 0.11f, 1f);
            AddRect(root, "Cliff_Wall_West", new Vector2(x0 - 0.45f, -7f), new Vector2(x0 + 0.08f, StreetY - 0.05f), "Gameplay", 0, cliff);
            AddRect(root, "Cliff_Wall_East", new Vector2(x1 - 0.08f, -7f), new Vector2(x1 + 0.45f, StreetY - 0.05f), "Gameplay", 0, cliff);
            AddRect(root, "Abyss_Dark", new Vector2(x0, -7f), new Vector2(x1, -0.6f), "Midground", 1, new Color(0.05f, 0.03f, 0.08f, 0.85f));

            // Rocas del filo (sin sobresalir sobre el hueco)
            Sprite rockA = AssetDatabase.LoadAssetAtPath<Sprite>(EnvPath + "Env_Ledge_Rock_A.png");
            AddFitted(root, "Cliff_Lip_West", rockA, new Vector2(x0 - 3.0f, -1.6f), new Vector2(x0 + 0.08f, StreetY + 0.12f), "Gameplay", 1, new Color(0.62f, 0.56f, 0.7f, 1f));
            AddFitted(root, "Cliff_Lip_East", rockA, new Vector2(x1 - 0.08f, -1.6f), new Vector2(x1 + 3.0f, StreetY + 0.12f), "Gameplay", 1, new Color(0.62f, 0.56f, 0.7f, 1f), true);
            Sprite rock = FindAtlasSprite(cainos, "TX Village Props - Rock 02");
            for (int i = 0; i < 4; i++)
            {
                float y = -1.0f - i * 1.1f;
                float jitter = (i % 2 == 0) ? 0.1f : -0.15f;
                AddFitted(root, $"Cliff_Rock_W{i}", rock, new Vector2(x0 - 0.9f + jitter, y - 0.7f), new Vector2(x0 + 0.25f + jitter, y), "Gameplay", 1, new Color(0.45f, 0.4f, 0.52f, 1f));
                AddFitted(root, $"Cliff_Rock_E{i}", rock, new Vector2(x1 - 0.25f - jitter, y - 0.7f), new Vector2(x1 + 0.9f - jitter, y), "Gameplay", 1, new Color(0.45f, 0.4f, 0.52f, 1f), true);
            }

            // Vallas y estacas rotas al borde
            Sprite pillar = FindAtlasSprite(cainos, "TX Village Props - Fence 01 Pillar 01");
            Sprite pillar2 = FindAtlasSprite(cainos, "TX Village Props - Fence 02 Pillar");
            Sprite rail = FindAtlasSprite(cainos, "TX Village Props - Fence 01 Rail 01");
            PlaceRotated(root, "BrokenFence_W_Post1", pillar, new Vector3(x0 - 1.7f, StreetY - 0.05f, 0f), -6f, 1.1f);
            PlaceRotated(root, "BrokenFence_W_Post2", pillar, new Vector3(x0 - 0.45f, StreetY - 0.08f, 0f), 24f, 1.1f);
            PlaceRotated(root, "BrokenFence_W_Rail", rail, new Vector3(x0 - 1.05f, StreetY + 0.55f, 0f), -17f, 1.1f);
            PlaceRotated(root, "BrokenFence_W_Fallen", rail, new Vector3(x0 + 0.35f, StreetY - 0.55f, 0f), 68f, 1.0f);
            PlaceRotated(root, "BrokenStake_E1", pillar2, new Vector3(x1 + 0.45f, StreetY - 0.08f, 0f), -28f, 1.15f);
            PlaceRotated(root, "BrokenStake_E2", pillar2, new Vector3(x1 + 1.35f, StreetY - 0.05f, 0f), 9f, 1.05f);
            PlaceRotated(root, "BrokenFence_E_Rail", rail, new Vector3(x1 + 0.95f, StreetY + 0.42f, 0f), 21f, 1.1f);

            // Niebla violeta que sube del abismo
            Sprite fog = EnsureSoftFogSprite();
            int puffs = Mathf.Max(5, Mathf.RoundToInt((x1 - x0) / 2.2f));
            for (int i = 0; i < puffs; i++)
            {
                float t = (i + 0.5f) / puffs;
                float x = Mathf.Lerp(x0 + 0.6f, x1 - 0.6f, t);
                float y = -1.4f + Mathf.Sin(i * 1.7f) * 0.45f;
                SpriteRenderer f = AddFitted(root, $"Ravine_Fog_{i}", fog, new Vector2(x - 2.2f, y - 1.0f), new Vector2(x + 2.2f, y + 1.0f),
                    "Gameplay", 6, new Color(0.58f, 0.28f, 0.92f, 0.5f));
                if (f != null && i % 2 == 0)
                {
                    AddFitted(root, $"Ravine_FogHigh_{i}", fog, new Vector2(x - 1.6f, -0.65f), new Vector2(x + 1.6f, 0.35f),
                        "Foreground", 2, new Color(0.7f, 0.42f, 1f, 0.28f));
                }
            }

            GameObject glowGo = new GameObject("Ravine_VioletGlow_Light2D");
            glowGo.transform.SetParent(root, false);
            glowGo.transform.position = new Vector3((x0 + x1) * 0.5f, -2.2f, 0f);
            Light2D glow = glowGo.AddComponent<Light2D>();
            glow.lightType = Light2D.LightType.Point;
            glow.color = new Color(0.62f, 0.25f, 0.95f, 1f);
            glow.intensity = 1.5f;
            glow.pointLightInnerRadius = 1.5f;
            glow.pointLightOuterRadius = (x1 - x0) * 0.6f;
            glowGo.AddComponent<LanternFlicker>();

            // Cartel de peligro
            GameObject warn = PlaceCainosSprite(root, cainos, "Ravine_WarningSign", "TX Village Props - Road Sign 02", new Vector3(x0 - 2.6f, 0.5f, 0f), 1.0f, true);
            if (warn != null)
            {
                TextMeshPro t = AddWorldText(warn.transform, "Warning_Text", new Vector3(0f, 2.1f, 0f), "¡PELIGRO!\nBarranco: vuela con la escoba [F]", 2.2f,
                    new Color(1f, 0.62f, 0.88f, 1f), "Gameplay", 10, new Vector2(3.6f, 1.2f));
                t.fontStyle = FontStyles.Bold;
            }
        }

        private static void PlaceRotated(Transform parent, string name, Sprite sp, Vector3 basePos, float angle, float scale)
        {
            if (sp == null) return;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = basePos;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.color = new Color(0.72f, 0.62f, 0.58f, 1f);
            sr.sortingLayerName = "Gameplay";
            sr.sortingOrder = 2;
        }

        /// <summary>
        /// Lector de notas del Nivel 3: pergamino a pantalla completa con el juego en pausa (Time.timeScale = 0).
        /// Las pistas (LoreObject) lo usan automáticamente si existe en la escena.
        /// </summary>
        private static void BuildLevel3LoreReader(Transform uiRoot)
        {
            Sprite parchment = LoadFirstSprite(Level2ArtPath + "UI_Lore_Parchment.png");

            GameObject root = new GameObject("LoreReader_Canvas", typeof(RectTransform));
            root.transform.SetParent(uiRoot, false);
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0) root.layer = uiLayer;
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            RectTransform panel = NewUiChild(root.transform, "Panel");
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            panel.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.02f, 0.01f, 0.62f);

            RectTransform paper = NewUiChild(panel, "Parchment");
            paper.anchorMin = paper.anchorMax = paper.pivot = new Vector2(0.5f, 0.5f);
            paper.sizeDelta = new Vector2(1120f, 840f);
            paper.anchoredPosition = Vector2.zero;
            Image paperImg = paper.gameObject.AddComponent<Image>();
            paperImg.sprite = parchment;
            paperImg.preserveAspect = true;
            paperImg.raycastTarget = false;

            TextMeshProUGUI title = LoreText(paper, "Title", new Vector2(0.14f, 0.76f), new Vector2(0.86f, 0.88f), 48f, FontStyles.Bold);
            TextMeshProUGUI body = LoreText(paper, "Body", new Vector2(0.14f, 0.24f), new Vector2(0.86f, 0.74f), 34f, FontStyles.Italic);
            TextMeshProUGUI hint = LoreText(paper, "Hint", new Vector2(0.14f, 0.12f), new Vector2(0.86f, 0.2f), 26f, FontStyles.Bold);
            hint.text = "[Espacio] Cerrar";
            hint.color = new Color32(92, 58, 30, 255);
            body.enableAutoSizing = true;
            body.fontSizeMin = 22f;
            body.fontSizeMax = 34f;

            Level2LoreReader reader = root.AddComponent<Level2LoreReader>();
            reader.SetupReferences(panel.gameObject, title, body);
            panel.gameObject.SetActive(false);
        }

        private static RectTransform NewUiChild(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            return (RectTransform)go.transform;
        }

        private static TextMeshProUGUI LoreText(Transform parent, string name, Vector2 min, Vector2 max, float size, FontStyles style)
        {
            RectTransform rt = NewUiChild(parent, name);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.color = new Color32(48, 26, 10, 255);
            tmp.raycastTarget = false;
            return tmp;
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

            // Eliminar entradas duplicadas previas de Level3 / Level3_Village si existieran
            scenes.RemoveAll(s => s.path == Level3Path || s.path == Level3VillagePath);

            int level2Index = scenes.FindIndex(s => s.path == Level2Path);
            EditorBuildSettingsScene level3Entry = new EditorBuildSettingsScene(Level3Path, true);
            EditorBuildSettingsScene villageEntry = new EditorBuildSettingsScene(Level3VillagePath, true);

            if (level2Index >= 0)
            {
                scenes.Insert(level2Index + 1, level3Entry);
                scenes.Insert(level2Index + 2, villageEntry);
            }
            else
            {
                scenes.Add(level3Entry);
                scenes.Add(villageEntry);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
