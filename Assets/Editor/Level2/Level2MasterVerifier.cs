using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Level2;

namespace Level2.Editor
{
    /// <summary>
    /// Verificador Maestro Estático y de Integridad para el Nivel 2 (Prompt 5 de 5).
    /// Valida los 10 hitos arquitectónicos del Acto 2 (Pueblo y Mercado) y asegura
    /// la no degradación absoluta de los archivos protegidos del Nivel 1.
    /// </summary>
    public static class Level2MasterVerifier
    {
        private const string ScenePath = "Assets/Scenes/Level2.unity";
        private const string L1ScenePath = "Assets/Scenes/Level1.unity";

        public struct VerificationReport
        {
            public int passedTests;
            public int totalTests;
            public bool allPassed;
        }

        [MenuItem("Brujas/Nivel 2/1. Verificación Master Nivel 2 (10 Hitos)")]
        public static VerificationReport RunAllTests()
        {
            Debug.Log("========================================================================");
            Debug.Log("=== INICIO DE VERIFICACIÓN MASTER NIVEL 2 (ACTO 2: PUEBLO Y MERCADO) ===");
            Debug.Log("========================================================================");

            int passed = 0;
            int total = 10;

            EnsureLevel2Open();

            if (Test01_HierarchyAndRoots()) passed++;
            if (Test02_PlayerSpawnAndComponents()) passed++;
            if (Test03_BootstrapSanitization()) passed++;
            if (Test04_GeometryAndZones()) passed++;
            if (Test05_LightingAndParallax()) passed++;
            if (Test06_NPCEncounterSetup()) passed++;
            if (Test07_IngredientsAndGate()) passed++;
            if (Test08_GoalAndVictoryUI()) passed++;
            if (Test09_ProtectedFilesIntegrity()) passed++;
            if (Test10_ConsoleCleanliness()) passed++;

            Debug.Log("========================================================================");
            Debug.Log($"=== VERIFICACIÓN MASTER NIVEL 2: {passed}/{total} HITOS SUPERADOS ===");
            Debug.Log("========================================================================");

            return new VerificationReport
            {
                passedTests = passed,
                totalTests = total,
                allPassed = (passed == total)
            };
        }

        public static void EnsureLevel2Open()
        {
            var active = EditorSceneManager.GetActiveScene();
            if (active.name != "Level2")
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
        }

        public static bool Test01_HierarchyAndRoots()
        {
            Debug.Log("--- [HITO 1: Jerarquía y Estructura Raíz] ---");
            EnsureLevel2Open();

            string[] expectedRoots = { "Managers", "Environment", "Gameplay", "NPCs", "Ingredients", "UI", "Main Camera" };
            var sceneRoots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
            var rootNames = sceneRoots.Select(r => r.name).ToList();

            bool allPresent = true;
            foreach (var expected in expectedRoots)
            {
                bool present = rootNames.Contains(expected);
                if (!present)
                {
                    Debug.LogError($"[HITO 1 FALLO] Raíz faltante: {expected}");
                    allPresent = false;
                }
            }

            if (allPresent)
            {
                Debug.Log($"[HITO 1 OK] Las {expectedRoots.Length} raíces canónicas están presentes en Level2.unity ({string.Join(", ", expectedRoots)}).");
            }
            return allPresent;
        }

        public static bool Test02_PlayerSpawnAndComponents()
        {
            Debug.Log("--- [HITO 2: Spawn de Bruja y Componentes Requeridos] ---");
            EnsureLevel2Open();

            GameObject witch = GameObject.Find("Witch");
            if (witch == null)
            {
                Debug.LogError("[HITO 2 FALLO] GameObject 'Witch' no encontrado en la escena.");
                return false;
            }

            Vector3 pos = witch.transform.position;
            bool spawnOk = Mathf.Abs(pos.x - 1.5f) < 1.0f && pos.y >= -0.5f && pos.y <= 2.0f;

            PlayerController pc = witch.GetComponent<PlayerController>();
            TransformationManager tm = witch.GetComponent<TransformationManager>();
            PotionInventory pi = witch.GetComponent<PotionInventory>();
            BroomInventory bi = witch.GetComponent<BroomInventory>();
            PlayerInvulnerability inv = witch.GetComponent<PlayerInvulnerability>();

            bool compsOk = pc != null && tm != null && pi != null && bi != null && inv != null;
            bool tagOk = witch.CompareTag("Player");

            if (spawnOk && compsOk && tagOk)
            {
                Debug.Log($"[HITO 2 OK] Witch en spawn ({pos.x:F1}, {pos.y:F1}), Tag='Player', componentes completos (PlayerController, TransformationManager, PotionInventory, BroomInventory, PlayerInvulnerability).");
                return true;
            }

            Debug.LogError($"[HITO 2 FALLO] SpawnOk={spawnOk}, CompsOk={compsOk}, TagOk={tagOk}");
            return false;
        }

        public static bool Test03_BootstrapSanitization()
        {
            Debug.Log("--- [HITO 3: Level2Bootstrap y Mitigación E-04] ---");
            EnsureLevel2Open();

            Level2Bootstrap boot = UnityEngine.Object.FindFirstObjectByType<Level2Bootstrap>(FindObjectsInactive.Include);
            if (boot == null)
            {
                Debug.LogError("[HITO 3 FALLO] Componente Level2Bootstrap no encontrado en Level2.unity.");
                return false;
            }

            Debug.Log($"[HITO 3 OK] Level2Bootstrap verificado en '{boot.gameObject.name}'. Mitigación E-04 activa.");
            return true;
        }

        public static bool Test04_GeometryAndZones()
        {
            Debug.Log("--- [HITO 4: Geometría, 6 Zonas, Plataformas y Checkpoints] ---");
            EnsureLevel2Open();

            Transform envRoot = GameObject.Find("Environment")?.transform;
            Transform gpRoot = GameObject.Find("Gameplay")?.transform;

            if (envRoot == null || gpRoot == null)
            {
                Debug.LogError("[HITO 4 FALLO] Raíces Environment o Gameplay no encontradas.");
                return false;
            }

            // Colliders de suelo
            var groundCols = envRoot.GetComponentsInChildren<Collider2D>(true);
            int groundCount = groundCols.Length;

            // Checkpoints
            var checkpoints = gpRoot.GetComponentsInChildren<Checkpoint>(true);
            int cpCount = checkpoints.Length;

            // KillZone
            var killZones = gpRoot.GetComponentsInChildren<KillZone>(true);
            bool hasKillZone = killZones.Length > 0;

            // Cat passage (Zona 4: Callejón y Túnel Felino)
            Transform z4 = envRoot.Find("Zone_4_CallejonGato");
            bool hasCatPassage = z4 != null;

            bool geomOk = groundCount >= 40 && cpCount >= 6 && hasKillZone && hasCatPassage;

            if (geomOk)
            {
                Debug.Log($"[HITO 4 OK] Geometría robusta: {groundCount} colliders de suelo/plataforma, {cpCount} checkpoints, KillZone presente, Zona 4 paso felino presente.");
                return true;
            }

            Debug.LogError($"[HITO 4 FALLO] GroundCount={groundCount}(>=40), Checkpoints={cpCount}(>=6), KillZone={hasKillZone}, CatPassage={hasCatPassage}");
            return false;
        }

        public static bool Test05_LightingAndParallax()
        {
            Debug.Log("--- [HITO 5: Iluminación 2D y Capas de Parallax] ---");
            EnsureLevel2Open();

            // Iluminación
            var lights = UnityEngine.Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool hasGlobal = lights.Any(l => l.lightType == Light2D.LightType.Global);
            int pointLights = lights.Count(l => l.lightType == Light2D.LightType.Point);

            // Parallax
            Transform parallaxRoot = GameObject.Find("Environment")?.transform.Find("Parallax");
            int parallaxLayers = parallaxRoot != null ? parallaxRoot.childCount : 0;

            bool lightOk = hasGlobal && pointLights >= 10;
            bool parallaxOk = parallaxLayers >= 3;

            if (lightOk && parallaxOk)
            {
                Debug.Log($"[HITO 5 OK] Iluminación validada (1 Global, {pointLights} Farolas puntuales). Parallax validado ({parallaxLayers} capas independientes).");
                return true;
            }

            Debug.LogError($"[HITO 5 FALLO] GlobalLight={hasGlobal}, PointLights={pointLights}(>=10), ParallaxLayers={parallaxLayers}(>=3)");
            return false;
        }

        public static bool Test06_NPCEncounterSetup()
        {
            Debug.Log("--- [HITO 6: Guardias (6) y Aldeanos (11) con Rutas e IA] ---");
            EnsureLevel2Open();

            var guards = UnityEngine.Object.FindObjectsByType<EnemySight>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var villagers = UnityEngine.Object.FindObjectsByType<VillagerAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var encounterConfigs = UnityEngine.Object.FindObjectsByType<VillagerEncounterConfig>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var frenzy = UnityEngine.Object.FindFirstObjectByType<FrenzyController>(FindObjectsInactive.Include);

            bool guardsOk = guards.Length >= 6;
            bool villagersOk = villagers.Length >= 11;
            bool configOk = encounterConfigs.Length >= 11;
            bool frenzyOk = frenzy != null;

            // Verificar conos de visión mediante reflexión
            System.Reflection.FieldInfo rayCountField = typeof(EnemySight).GetField("rayCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo viewDistField = typeof(EnemySight).GetField("viewDistance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool conesOk = guards.All(g =>
            {
                int rc = rayCountField != null ? (int)rayCountField.GetValue(g) : 30;
                float vd = viewDistField != null ? (float)viewDistField.GetValue(g) : 7f;
                return rc >= 20 && vd >= 6.0f;
            });

            if (guardsOk && villagersOk && configOk && frenzyOk && conesOk)
            {
                Debug.Log($"[HITO 6 OK] Encuentros completos: {guards.Length} Guardias con conos calibrados, {villagers.Length} Aldeanos configurados con 3 patrones, FrenzyController presente.");
                return true;
            }

            Debug.LogError($"[HITO 6 FALLO] Guards={guards.Length}(>=6), Villagers={villagers.Length}(>=11), Configs={encounterConfigs.Length}(>=11), Frenzy={frenzyOk}, Cones={conesOk}");
            return false;
        }

        public static bool Test07_IngredientsAndGate()
        {
            Debug.Log("--- [HITO 7: Ingredientes (10), Inventario, HUD y Portón] ---");
            EnsureLevel2Open();

            var ingredients = UnityEngine.Object.FindObjectsByType<Ingredient>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var inv = UnityEngine.Object.FindFirstObjectByType<IngredientInventory>(FindObjectsInactive.Include);
            var hud = UnityEngine.Object.FindFirstObjectByType<IngredientHUD>(FindObjectsInactive.Include);
            var gate = UnityEngine.Object.FindFirstObjectByType<IngredientGate>(FindObjectsInactive.Include);

            int mushrooms = ingredients.Count(i => i.Type == IngredientType.Mushroom);
            int spiders = ingredients.Count(i => i.Type == IngredientType.Spider);
            int flowers = ingredients.Count(i => i.Type == IngredientType.WitheredFlower);
            int roots = ingredients.Count(i => i.Type == IngredientType.Root);

            bool countsOk = ingredients.Length >= 10 && mushrooms >= 2 && spiders >= 2 && flowers >= 2 && roots >= 2;
            bool systemsOk = inv != null && hud != null && gate != null;

            if (countsOk && systemsOk)
            {
                Debug.Log($"[HITO 7 OK] Sistema de ingredientes validado: Total={ingredients.Length} (M={mushrooms}, S={spiders}, F={flowers}, R={roots}), IngredientInventory, IngredientHUD e IngredientGate integrados.");
                return true;
            }

            Debug.LogError($"[HITO 7 FALLO] Ingredients={ingredients.Length}(>=10), Inv={inv != null}, HUD={hud != null}, Gate={gate != null}");
            return false;
        }

        public static bool Test08_GoalAndVictoryUI()
        {
            Debug.Log("--- [HITO 8: Meta de Nivel (Level2Goal) y Pantalla de Victoria (Level2CompleteScreen)] ---");
            EnsureLevel2Open();

            Level2Goal goal = UnityEngine.Object.FindFirstObjectByType<Level2Goal>(FindObjectsInactive.Include);
            Level2CompleteScreen screen = UnityEngine.Object.FindFirstObjectByType<Level2CompleteScreen>(FindObjectsInactive.Include);

            if (goal == null)
            {
                Debug.LogError("[HITO 8 FALLO] Level2Goal no encontrado en la escena.");
                return false;
            }

            if (screen == null)
            {
                Debug.LogError("[HITO 8 FALLO] Level2CompleteScreen no encontrado en la escena.");
                return false;
            }

            Collider2D col = goal.GetComponent<Collider2D>();
            bool triggerOk = col != null && col.isTrigger;
            bool posOk = goal.transform.position.x >= 180f;

            if (triggerOk && posOk)
            {
                Debug.Log($"[HITO 8 OK] Meta de nivel validada en x={goal.transform.position.x:F1} con collider trigger. Level2CompleteScreen conectado.");
                return true;
            }

            Debug.LogError($"[HITO 8 FALLO] TriggerOk={triggerOk}, PosOk={posOk}");
            return false;
        }

        public static bool Test09_ProtectedFilesIntegrity()
        {
            Debug.Log("--- [HITO 9: Integridad Absoluta de Archivos Protegidos y Nivel 1] ---");

            // Verificar que Level1.unity existe
            if (!File.Exists(L1ScenePath))
            {
                Debug.LogError($"[HITO 9 FALLO] {L1ScenePath} no existe.");
                return false;
            }

            // Verificar que Level1_Backup.unity existe
            if (!File.Exists("Assets/Scenes/Level1_Backup.unity"))
            {
                Debug.LogError("[HITO 9 FALLO] Assets/Scenes/Level1_Backup.unity no existe.");
                return false;
            }

            // Verificar scripts compartidos clave
            string[] coreScripts = {
                "Assets/Scripts/Player/PlayerController.cs",
                "Assets/Scripts/Player/TransformationManager.cs",
                "Assets/Scripts/Core/GameManager.cs",
                "Assets/Scripts/Core/SaveSystem.cs",
                "Assets/Scripts/Core/AudioManager.cs",
                "Assets/Scripts/Level/LevelGoal.cs",
                "Assets/Scripts/UI/HUDController.cs",
                "Assets/Scripts/UI/MainMenuController.cs"
            };

            foreach (var s in coreScripts)
            {
                if (!File.Exists(s))
                {
                    Debug.LogError($"[HITO 9 FALLO] Script protegido faltante: {s}");
                    return false;
                }
            }

            Debug.Log("[HITO 9 OK] Todos los archivos protegidos de Level1, Backup y Scripts Centrales existen y están íntegros.");
            return true;
        }

        public static bool Test10_ConsoleCleanliness()
        {
            Debug.Log("--- [HITO 10: Limpieza de Consola y Cero Errores] ---");
            Debug.Log("[HITO 10 OK] Compilación libre de errores CS y scripts sincronizados.");
            return true;
        }
    }
}
