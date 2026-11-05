using System.Collections.Generic;
using System.Linq;
using System.Text;
using Level2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Level3.Editor
{
    /// <summary>
    /// Verificador Maestro del Nivel 3.
    /// Menú: Tools/Level 3/Run Level 3 Master Audit
    /// Abre Assets/Scenes/Level3.unity y audita automáticamente infraestructura, portadores de llave,
    /// cerraduras (sin llaves huérfanas), caldero, pistas, cofres encadenados, pasajes de gato,
    /// retos de vuelo sobre KillZone, checkpoints, grounding por raycast y la progresión Level2 -> Level3.
    /// El reporte final debe arrojar 100% PASS con 0 errores y 0 warnings.
    /// </summary>
    public static class Level3MasterVerifier
    {
        public const string Level3ScenePath = "Assets/Scenes/Level3.unity";
        public const string Level2ScenePath = "Assets/Scenes/Level2.unity";

        private static readonly string[] ExpectedCarrierKeys = { "forest_gate_key_1", "forest_gate_key_2", "sanctuary_gate_key" };

        private static readonly Dictionary<string, string> ExpectedGates = new Dictionary<string, string>
        {
            { "forest_gate_1", "forest_gate_key_1" },
            { "forest_gate_2", "forest_gate_key_2" },
            { WitchCauldronInteractable.ReturnGateId, string.Empty }, // se abre con la Poción Maestra (caldero)
            { "sanctuary_atrium_gate", "sanctuary_gate_key" },
        };

        private static readonly string[] ExpectedChestChain = { "crypt_chest_key", "inquisitor_chest_key", "church_master_key" };

        public class AuditReport
        {
            public int Passed;
            public int Errors;
            public int Warnings;
            public readonly StringBuilder Log = new StringBuilder();
            public int Total => Passed + Errors + Warnings;
            public bool IsPerfect => Errors == 0 && Warnings == 0 && Passed > 0;
        }

        [MenuItem("Tools/Level 3/Run Level 3 Master Audit")]
        public static void RunFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            RunAudit();
        }

        /// <summary>Ejecuta la auditoría completa y devuelve el reporte (también lo imprime en consola).</summary>
        public static AuditReport RunAudit()
        {
            var r = new AuditReport();
            r.Log.AppendLine("=================== LEVEL 3 MASTER AUDIT ===================");

            Scene scene = EditorSceneManager.OpenScene(Level3ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Fail(r, $"No se pudo abrir {Level3ScenePath}");
                return Finish(r);
            }
            Physics2D.SyncTransforms();

            try
            {
                Section(r, "1. Infraestructura del jugador, managers y HUD");
                AuditInfrastructure(r);

                Section(r, "2. Guardias portadores de llave (KeyCarrierGuard)");
                AuditKeyCarriers(r);

                Section(r, "3. Cerraduras (LockedGateInteractable / ChurchDoorGoal) y llaves huérfanas");
                AuditLocksAndKeys(r);

                Section(r, "4. Caldero, pistas (LoreObject) y cofres encadenados (TreasureChest)");
                AuditCauldronCluesChests(r);

                Section(r, "5. Pasajes de gato, retos de vuelo sobre KillZone y checkpoints");
                AuditTraversal(r);

                Section(r, "6. Auditoría de Grounding (raycast hacia abajo sobre layer Ground)");
                AuditGrounding(r);
            }
            finally
            {
                // Descartar cualquier cambio temporal (p. ej. visuales del aura generados para la verificación)
                EditorSceneManager.OpenScene(Level3ScenePath, OpenSceneMode.Single);
            }

            Section(r, "7. Progresión Level2 -> Level3 y Build Settings");
            AuditProgression(r);

            return Finish(r);
        }

        // ------------------------------------------------------------------
        // 1. Infraestructura
        // ------------------------------------------------------------------
        private static void AuditInfrastructure(AuditReport r)
        {
            RequireSingle<PlayerController>(r, "PlayerController");
            RequireSingle<Level3Bootstrap>(r, "Level3Bootstrap");
            RequireSingle<Level3StateController>(r, "Level3StateController");
            RequireSingle<Level3ObjectiveHUD>(r, "Level3ObjectiveHUD");
            RequireSingle<KeyInventory>(r, "KeyInventory");
            RequireSingle<PotionInventory>(r, "PotionInventory");
            RequireSingle<BroomInventory>(r, "BroomInventory");
            RequireSingle<HUDController>(r, "HUDController");
            RequireSingle<GameManager>(r, "GameManager");

            PlayerController pc = Find<PlayerController>().FirstOrDefault();
            if (pc != null)
            {
                Check(r, pc.GetComponent<PotionInventory>() != null && pc.GetComponent<BroomInventory>() != null,
                    "PotionInventory y BroomInventory viven en la Bruja",
                    "PotionInventory/BroomInventory no están en el GameObject del jugador");
                Check(r, pc.GetComponent<PlayerInteractor>() != null, "PlayerInteractor presente en la Bruja",
                    "Falta PlayerInteractor en la Bruja");
            }

            HUDController hud = Find<HUDController>().FirstOrDefault();
            if (hud != null)
            {
                Check(r, hud.levelCompletePanel != null && hud.levelCompletePanel.transform.Find("WinTitle") != null,
                    "LevelCompletePanel con WinTitle listo para el mensaje de victoria final",
                    "HUDController sin LevelCompletePanel/WinTitle para la victoria del Nivel 3");
            }

            Level3ObjectiveHUD objHud = Find<Level3ObjectiveHUD>().FirstOrDefault();
            if (objHud != null)
            {
                Transform panel = objHud.transform.Find(Level3ObjectiveHUD.PanelName);
                bool ok = objHud.GetComponent<Canvas>() != null && panel != null &&
                          panel.Find("Inner/HeaderText") != null && panel.Find("Inner/ObjectiveText") != null &&
                          panel.Find("Inner/StatusText") != null && panel.Find("Inner/KeyChipsRow") != null;
                Check(r, ok, "Level3ObjectiveHUD con Canvas, cabecera, objetivo, chips de llaves y estado",
                    "Level3ObjectiveHUD con jerarquía UI incompleta");
            }
        }

        // ------------------------------------------------------------------
        // 2. Portadores de llave
        // ------------------------------------------------------------------
        private static void AuditKeyCarriers(AuditReport r)
        {
            KeyCarrierGuard[] carriers = Find<KeyCarrierGuard>();
            Check(r, carriers.Length == 3, $"3 KeyCarrierGuard encontrados", $"Se esperaban 3 KeyCarrierGuard, hay {carriers.Length}");

            var ids = carriers.Select(c => c.KeyId).ToList();
            Check(r, ids.Distinct().Count() == ids.Count, "keyId de portadores únicos", "keyId duplicados entre portadores: " + string.Join(", ", ids));

            foreach (string expected in ExpectedCarrierKeys)
            {
                Check(r, ids.Contains(expected), $"Portador con keyId '{expected}'", $"Ningún portador lleva '{expected}'");
            }

            foreach (KeyCarrierGuard c in carriers)
            {
                bool isGuard = c.GetComponent<PatrolRoute>() != null || c.GetComponent<VillagerAI>() != null;
                Check(r, isGuard, $"'{c.name}' es un NPC dormible (PatrolRoute/VillagerAI)", $"'{c.name}' no tiene PatrolRoute ni VillagerAI");

                c.EnsureAuraVisuals();
                Transform aura = c.transform.Find("KeyCarrierAura");
                bool auraOk = c.enabled && c.gameObject.activeInHierarchy && c.HasActiveAura && c.AuraColor.a >= 0.5f &&
                              aura != null && aura.GetComponentInChildren<Light2D>(true) != null;
                Check(r, auraOk, $"Aura dorada activa en '{c.name}' ({c.KeyId})", $"Aura inactiva o incompleta en '{c.name}'");
            }
        }

        // ------------------------------------------------------------------
        // 3. Cerraduras y llaves huérfanas
        // ------------------------------------------------------------------
        private static void AuditLocksAndKeys(AuditReport r)
        {
            HashSet<string> obtainable = CollectObtainableKeys();
            r.Log.AppendLine("    Llaves obtenibles en escena: " + string.Join(", ", obtainable.OrderBy(k => k)));

            LockedGateInteractable[] gates = Find<LockedGateInteractable>();
            Check(r, gates.Length == 4, "4 LockedGateInteractable encontrados", $"Se esperaban 4 LockedGateInteractable, hay {gates.Length}");

            foreach (KeyValuePair<string, string> kv in ExpectedGates)
            {
                LockedGateInteractable g = gates.FirstOrDefault(x => x.GateId == kv.Key);
                if (g == null)
                {
                    Fail(r, $"Falta el portón '{kv.Key}'");
                    continue;
                }

                if (string.IsNullOrEmpty(kv.Value))
                {
                    bool cauldron = Find<WitchCauldronInteractable>().Length > 0;
                    Check(r, g.RequireMasterPotion && cauldron,
                        $"Portón '{kv.Key}' se abre con la Poción Maestra (caldero presente)",
                        $"Portón '{kv.Key}' no está ligado a la Poción Maestra o falta el caldero");
                }
                else
                {
                    Check(r, g.RequiredKeyId == kv.Value, $"Portón '{kv.Key}' requiere '{kv.Value}'",
                        $"Portón '{kv.Key}' requiere '{g.RequiredKeyId}' (esperado '{kv.Value}')");
                }
            }

            ChurchDoorGoal[] doors = Find<ChurchDoorGoal>();
            Check(r, doors.Length == 1, "1 ChurchDoorGoal (Puerta de la Iglesia)", $"Se esperaba 1 ChurchDoorGoal, hay {doors.Length}");
            foreach (ChurchDoorGoal d in doors)
            {
                string req = new SerializedObject(d).FindProperty("requiredKeyId").stringValue;
                Check(r, req == ChurchDoorGoal.RequiredMasterKeyId, $"ChurchDoorGoal requiere '{req}'", $"ChurchDoorGoal requiere '{req}' (esperado church_master_key)");
                Check(r, Mathf.Abs(d.transform.position.x - 255f) <= 1.5f, $"ChurchDoorGoal en X={d.transform.position.x:F1}", $"ChurchDoorGoal fuera de X≈255 ({d.transform.position.x:F1})");
            }

            // Toda cerradura con llave debe tener su llave obtenible
            var required = new List<KeyValuePair<string, string>>();
            foreach (LockedGateInteractable g in gates)
            {
                if (!string.IsNullOrEmpty(g.RequiredKeyId)) required.Add(new KeyValuePair<string, string>($"Portón {g.GateId}", g.RequiredKeyId));
            }
            foreach (TreasureChest c in Find<TreasureChest>())
            {
                if (c.RequiresKey && !string.IsNullOrEmpty(c.RequiredKeyId)) required.Add(new KeyValuePair<string, string>($"Cofre {c.ChestId}", c.RequiredKeyId));
            }
            foreach (ChurchDoorGoal d in doors)
            {
                required.Add(new KeyValuePair<string, string>("Puerta de la Iglesia", new SerializedObject(d).FindProperty("requiredKeyId").stringValue));
            }

            int orphans = 0;
            foreach (KeyValuePair<string, string> req in required)
            {
                if (!obtainable.Contains(req.Value))
                {
                    orphans++;
                    Fail(r, $"Llave HUÉRFANA: {req.Key} requiere '{req.Value}' y no es obtenible en la escena");
                }
            }
            Check(r, orphans == 0, $"0 llaves huérfanas ({required.Count} cerraduras verificadas)", $"{orphans} llaves huérfanas");
        }

        private static HashSet<string> CollectObtainableKeys()
        {
            var keys = new HashSet<string>();
            foreach (KeyCarrierGuard c in Find<KeyCarrierGuard>()) if (!string.IsNullOrEmpty(c.KeyId)) keys.Add(c.KeyId);
            foreach (KeyItem k in Find<KeyItem>()) if (!string.IsNullOrEmpty(k.KeyId)) keys.Add(k.KeyId);
            foreach (TreasureChest c in Find<TreasureChest>())
            {
                string rk = GetChestRewardKey(c);
                if (!string.IsNullOrEmpty(rk)) keys.Add(rk);
            }
            return keys;
        }

        private static string GetChestRewardKey(TreasureChest c)
        {
            var so = new SerializedObject(c);
            SerializedProperty type = so.FindProperty("primaryReward");
            SerializedProperty key = so.FindProperty("rewardKeyId");
            if (type == null || key == null) return string.Empty;
            return type.enumValueIndex == (int)ChestRewardType.Key || !string.IsNullOrEmpty(key.stringValue) ? key.stringValue : string.Empty;
        }

        // ------------------------------------------------------------------
        // 4. Caldero, pistas y cofres
        // ------------------------------------------------------------------
        private static void AuditCauldronCluesChests(AuditReport r)
        {
            WitchCauldronInteractable[] cauldrons = Find<WitchCauldronInteractable>();
            Check(r, cauldrons.Length == 1, "1 WitchCauldronInteractable en la Cabaña", $"Se esperaba 1 caldero, hay {cauldrons.Length}");

            LoreObject[] lore = Find<LoreObject>();
            Check(r, lore.Length == 3, "3 pistas (LoreObject)", $"Se esperaban 3 LoreObject, hay {lore.Length}");
            var loreIds = lore.Select(l => l.InteractionId).ToList();
            Check(r, loreIds.Distinct().Count() == loreIds.Count, "Pistas con IDs únicos", "IDs de pistas duplicados");

            TreasureChest[] chests = Find<TreasureChest>();
            Check(r, chests.Length == 3, "3 cofres (TreasureChest)", $"Se esperaban 3 TreasureChest, hay {chests.Length}");

            // Cadena: (sin llave) -> crypt_chest_key -> inquisitor_chest_key -> church_master_key
            TreasureChest first = chests.FirstOrDefault(c => !c.RequiresKey);
            Check(r, first != null && GetChestRewardKey(first) == ExpectedChestChain[0],
                $"Cofre #1 sin llave otorga '{ExpectedChestChain[0]}'", "Cofre #1 (sin llave) no otorga crypt_chest_key");

            for (int i = 1; i < ExpectedChestChain.Length; i++)
            {
                string needs = ExpectedChestChain[i - 1];
                string gives = ExpectedChestChain[i];
                TreasureChest c = chests.FirstOrDefault(x => x.RequiresKey && x.RequiredKeyId == needs);
                Check(r, c != null && GetChestRewardKey(c) == gives,
                    $"Cofre #{i + 1} requiere '{needs}' y otorga '{gives}'",
                    $"Cadena rota: no hay cofre que requiera '{needs}' y otorgue '{gives}'");
            }
        }

        // ------------------------------------------------------------------
        // 5. Pasajes de gato, vuelo, checkpoints
        // ------------------------------------------------------------------
        private static void AuditTraversal(AuditReport r)
        {
            CatOnlyPassage[] cats = Find<CatOnlyPassage>();
            Check(r, cats.Length >= 2, $"{cats.Length} CatOnlyPassage (mínimo 2)", $"Solo {cats.Length} CatOnlyPassage (mínimo 2)");

            Checkpoint[] cps = Find<Checkpoint>();
            Check(r, cps.Length >= 4, $"{cps.Length} Checkpoints (mínimo 4)", $"Solo {cps.Length} Checkpoints (mínimo 4)");

            KillZone[] kills = Find<KillZone>();
            Check(r, kills.Length >= 1, $"{kills.Length} KillZone(s) de reto de vuelo", "No hay KillZone para retos de vuelo");

            int groundMask = GroundMask();
            foreach (KillZone kz in kills)
            {
                Collider2D kzCol = kz.GetComponent<Collider2D>();
                Bounds b = kzCol != null ? kzCol.bounds : new Bounds(kz.transform.position, Vector3.one);

                // Debe existir un vacío real (sin suelo al nivel de camino) sobre el centro de la KillZone
                Collider2D floor = Physics2D.OverlapPoint(new Vector2(b.center.x, 0f), groundMask);
                Check(r, floor == null, $"'{kz.name}': vacío real sobre la KillZone (X {b.min.x:F0}..{b.max.x:F0})",
                    $"'{kz.name}': hay suelo '{(floor != null ? floor.name : "")}' tapando la KillZone");

                // Debe haber al menos un apoyo aéreo (copa/plataforma) sobre el barranco para el reto de escoba
                bool aerial = Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None).Any(c =>
                    !c.isTrigger && ((1 << c.gameObject.layer) & groundMask) != 0 &&
                    c.bounds.min.y > 1.5f && c.bounds.center.x > b.min.x && c.bounds.center.x < b.max.x);
                Check(r, aerial, $"'{kz.name}': plataformas aéreas para cruzar volando",
                    $"'{kz.name}': sin plataformas aéreas sobre el barranco");
            }
        }

        // ------------------------------------------------------------------
        // 6. Grounding
        // ------------------------------------------------------------------
        private struct GroundTarget
        {
            public Component Comp;
            public string Category;
            public float MaxGap;
        }

        private static void AuditGrounding(AuditReport r)
        {
            var targets = new List<GroundTarget>();
            void Add<T>(string category, float maxGap) where T : Component
            {
                foreach (T c in Find<T>()) targets.Add(new GroundTarget { Comp = c, Category = category, MaxGap = maxGap });
            }

            Add<PatrolRoute>("Guardia", 0.6f);
            Add<VillagerAI>("Aldeano", 0.6f);
            Add<TreasureChest>("Cofre", 0.6f);
            Add<LoreObject>("Pista", 0.8f);
            Add<LockedGateInteractable>("Portón", 0.6f);
            Add<ChurchDoorGoal>("Puerta Iglesia", 0.8f);
            Add<WitchCauldronInteractable>("Caldero", 0.6f);
            Add<PotionRefillStation>("Estación de Pociones", 0.8f);
            Add<Checkpoint>("Checkpoint", 0.8f);
            Add<PotionPickup>("Poción", 2.2f);
            Add<HeartPickup>("Corazón", 2.2f);

            // Evitar duplicados (p. ej. guardia con PatrolRoute y VillagerAI)
            var seen = new HashSet<GameObject>();
            int groundMask = GroundMask();
            int fails = 0;
            foreach (GroundTarget t in targets)
            {
                if (!seen.Add(t.Comp.gameObject)) continue;

                float footY = GetFootY(t.Comp);
                Vector2 origin = new Vector2(t.Comp.transform.position.x, footY + 0.5f);
                RaycastHit2D hit = RaycastGroundIgnoringSelf(origin, 0.5f + t.MaxGap + 0.05f, groundMask, t.Comp.transform);

                if (hit.collider == null)
                {
                    fails++;
                    Fail(r, $"[{t.Category}] '{t.Comp.name}' en ({t.Comp.transform.position.x:F1}, {footY:F2}) NO tiene suelo Ground debajo (≤{t.MaxGap:F1}u)");
                }
                else
                {
                    float gap = hit.distance - 0.5f;
                    if (gap < -0.45f)
                    {
                        fails++;
                        Fail(r, $"[{t.Category}] '{t.Comp.name}' está hundido {(-gap):F2}u dentro de '{hit.collider.name}'");
                    }
                    else
                    {
                        r.Passed++;
                    }
                }
            }

            int checkedCount = seen.Count;
            r.Log.AppendLine($"    Objetos auditados por raycast: {checkedCount} — fallos: {fails}");
            Check(r, fails == 0, $"Grounding 100%: {checkedCount} objetos apoyados sobre suelo/plataformas Ground",
                $"Grounding con {fails} objetos flotando/hundidos");
        }

        private static float GetFootY(Component comp)
        {
            // Usar el collider sólido (no trigger) más bajo si existe; si no, el pivote del transform.
            Collider2D best = null;
            foreach (Collider2D c in comp.GetComponents<Collider2D>())
            {
                if (c.isTrigger || !c.enabled) continue;
                if (best == null || c.bounds.min.y < best.bounds.min.y) best = c;
            }

            if (comp is PatrolRoute || comp is VillagerAI)
            {
                if (best != null) return best.bounds.min.y;
            }
            return comp.transform.position.y;
        }

        private static RaycastHit2D RaycastGroundIgnoringSelf(Vector2 origin, float distance, int mask, Transform self)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, distance, mask);
            foreach (RaycastHit2D h in hits.OrderBy(h => h.distance))
            {
                if (h.collider == null || h.collider.isTrigger) continue;
                if (h.collider.transform == self || h.collider.transform.IsChildOf(self)) continue;
                return h;
            }
            return default;
        }

        // ------------------------------------------------------------------
        // 7. Progresión
        // ------------------------------------------------------------------
        private static void AuditProgression(AuditReport r)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            int i2 = System.Array.FindIndex(scenes, s => s.path == Level2ScenePath);
            int i3 = System.Array.FindIndex(scenes, s => s.path == Level3ScenePath);
            Check(r, i3 >= 0 && scenes[i3].enabled, "Level3.unity registrada y habilitada en Build Settings", "Level3.unity no está habilitada en Build Settings");
            Check(r, i2 >= 0 && i3 == i2 + 1, "Level3 justo después de Level2 en Build Settings", $"Orden incorrecto (Level2={i2}, Level3={i3})");

            // Comprobación de assets en disco sin abrir la escena aditiva para evitar conflicto de Global Light 2D en URP
            string l2Text = System.IO.File.ReadAllText(Level2ScenePath);
            bool hasNextL3 = l2Text.Contains("nextSceneName: Level3");
            Check(r, hasNextL3, "nextSceneName serializado en Level2.unity = 'Level3'",
                "nextSceneName serializado en Level2.unity no es 'Level3'");
            Check(r, Level2CompleteScreen.DefaultNextSceneName == "Level3",
                "Level2CompleteScreen.DefaultNextSceneName == 'Level3'",
                "DefaultNextSceneName no es 'Level3'");
        }

        // ------------------------------------------------------------------
        // Utilidades
        // ------------------------------------------------------------------
        private static int GroundMask()
        {
            int layer = LayerMask.NameToLayer("Ground");
            return layer >= 0 ? 1 << layer : 0;
        }

        private static T[] Find<T>() where T : Component =>
            Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OrderBy(c => c.transform.position.x).ToArray();

        private static void RequireSingle<T>(AuditReport r, string label) where T : Component
        {
            int n = Find<T>().Length;
            Check(r, n >= 1, $"{label} presente ({n})", $"Falta {label} en la escena");
        }

        private static void Section(AuditReport r, string title) => r.Log.AppendLine($"\n--- {title} ---");

        private static void Check(AuditReport r, bool ok, string passMsg, string failMsg)
        {
            if (ok)
            {
                r.Passed++;
                r.Log.AppendLine("  [PASS] " + passMsg);
            }
            else
            {
                Fail(r, failMsg);
            }
        }

        private static void Fail(AuditReport r, string msg)
        {
            r.Errors++;
            r.Log.AppendLine("  [FAIL] " + msg);
        }

        private static AuditReport Finish(AuditReport r)
        {
            float pct = r.Total > 0 ? 100f * r.Passed / r.Total : 0f;
            r.Log.AppendLine("\n============================================================");
            r.Log.AppendLine($"RESULTADO: {pct:F1}% PASS — {r.Passed} PASS, {r.Errors} errores, {r.Warnings} warnings");
            r.Log.AppendLine(r.IsPerfect ? "ESTADO: ✅ 100% PASS — Nivel 3 listo." : "ESTADO: ❌ Hay problemas por corregir.");

            if (r.IsPerfect) Debug.Log("[Level3MasterVerifier]\n" + r.Log);
            else Debug.LogError("[Level3MasterVerifier]\n" + r.Log);
            return r;
        }
    }
}
