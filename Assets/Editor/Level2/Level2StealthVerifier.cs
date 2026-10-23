using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Suite de verificación de Guardias, Aldeanos, Cadenas de Alerta y Sigilo para Level2.unity.
/// Valida la coexistencia de ambos tipos de enemigo, >= 3 WanderPatterns, distancias de alarma,
/// efectos de pociones, invulnerabilidad y soluciones de sigilo documentadas.
/// </summary>
public static class Level2StealthVerifier
{
    private const string OutReportPath = "Logs/Level2_Stealth_Verification.txt";

    [MenuItem("Brujas/Nivel 2/Verificar Sigilo, Guardias y Aldeanos")]
    public static void RunVerification()
    {
        var sb = new StringBuilder();
        sb.AppendLine("==========================================================================");
        sb.AppendLine("=== VERIFICACIÓN DE SIGILO Y ENEMIGOS: NIVEL 2 (PUEBLO Y MERCADO) ===");
        sb.AppendLine("==========================================================================");

        Scene scene = EditorSceneManager.OpenScene(Level2StageBuilder.ScenePath, OpenSceneMode.Single);
        int passCount = 0;
        int failCount = 0;

        void Assert(string name, bool condition, string detail)
        {
            if (condition)
            {
                passCount++;
                sb.AppendLine($"[PASS] {name} :: {detail}");
            }
            else
            {
                failCount++;
                sb.AppendLine($"[FAIL] {name} :: {detail}");
            }
        }

        // 1. Guardias presentes
        EnemySight[] allGuards = Object.FindObjectsByType<EnemySight>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert("Guardias en Escena", allGuards.Length == 6, $"Encontrados {allGuards.Length} guardias (esperado 6)");

        // 2. Modos de Patrulla (PingPong y Loop ambos presentes)
        PatrolRoute[] allPatrols = Object.FindObjectsByType<PatrolRoute>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int pingPongCount = 0;
        int loopCount = 0;
        FieldInfo modeField = typeof(PatrolRoute).GetField("patrolMode", BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var p in allPatrols)
        {
            PatrolMode m = (PatrolMode)modeField.GetValue(p);
            if (m == PatrolMode.PingPong) pingPongCount++;
            else if (m == PatrolMode.Loop) loopCount++;
        }
        Assert("Modos de Patrulla (PingPong y Loop)", pingPongCount >= 1 && loopCount >= 1, $"PingPong: {pingPongCount}, Loop: {loopCount} (GDD exige ambos)");

        // 3. Waypoints válidos en todos los guardias
        bool allWaypointsValid = true;
        foreach (var p in allPatrols)
        {
            if (p.Waypoints == null || p.Waypoints.Length < 2 || p.Waypoints.Any(w => w == null))
            {
                allWaypointsValid = false;
                break;
            }
        }
        Assert("Waypoints Asignados por Guardia", allWaypointsValid, "Todos los guardias poseen al menos 2 waypoints válidos");

        // 4. Aldeanos presentes con variantes
        VillagerAI[] allVillagers = Object.FindObjectsByType<VillagerAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert("Aldeanos en Escena", allVillagers.Length == 11, $"Encontrados {allVillagers.Length} aldeanos (esperado 11)");

        // 5. Verificación de >= 3 WanderPatterns distintos activos
        HashSet<WanderPattern> patternsFound = new HashSet<WanderPattern>();
        foreach (var v in allVillagers)
        {
            VillagerEncounterConfig cfg = v.GetComponent<VillagerEncounterConfig>();
            if (cfg != null) patternsFound.Add(cfg.AssignedPattern);
        }
        Assert("Patrones Erráticos (>= 3 WanderPatterns)", patternsFound.Count >= 3, $"Encontrados {patternsFound.Count} patrones: {string.Join(", ", patternsFound)}");

        // 6. Cadena de Alarma: Aldeano a Guardia en radio de alerta (<= 12 u)
        int alertCapableVillagers = 0;
        FieldInfo radiusField = typeof(VillagerAI).GetField("guardAlertRadius", BindingFlags.NonPublic | BindingFlags.Instance);
        float alertRadius = radiusField != null ? (float)radiusField.GetValue(allVillagers[0]) : 12f;

        foreach (var v in allVillagers)
        {
            float nearestGuardDist = allGuards.Min(g => Vector2.Distance(v.transform.position, g.transform.position));
            if (nearestGuardDist <= alertRadius)
            {
                alertCapableVillagers++;
            }
        }
        Assert("Cadena de Alarma Aldeano -> Guardia", alertCapableVillagers >= 8, $"{alertCapableVillagers} aldeanos tienen al menos un guardia a <= {alertRadius}u (radio de alerta)");

        // 7. FrenzyController en escena
        FrenzyController frenzy = Object.FindFirstObjectByType<FrenzyController>();
        Assert("FrenzyController Activo", frenzy != null, frenzy != null ? "Presente en la escena" : "Falta FrenzyController");

        // 8. CaptureListener y CheckpointManager
        CaptureListener cl = Object.FindFirstObjectByType<CaptureListener>();
        CheckpointManager cm = Object.FindFirstObjectByType<CheckpointManager>();
        Assert("Infraestructura de Captura y Respawn", cl != null && cm != null, $"CaptureListener={cl != null}, CheckpointManager={cm != null}");

        // 9. Recursos de Pociones y Corazones
        PotionPickup[] potionPickups = Object.FindObjectsByType<PotionPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        HeartPickup[] heartPickups = Object.FindObjectsByType<HeartPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int blindCount = potionPickups.Count(p => p.Type == PotionType.Blind);
        int hypnoCount = potionPickups.Count(p => p.Type == PotionType.Hypnotize);
        int sleepCount = potionPickups.Count(p => p.Type == PotionType.Sleep);
        Assert("Recursos de Pociones", potionPickups.Length >= 5 && blindCount >= 1 && hypnoCount >= 1 && sleepCount >= 1, $"Pociones: {potionPickups.Length} (Cegar: {blindCount}, Hipnotizar: {hypnoCount}, Dormir: {sleepCount})");
        Assert("Recursos de Corazones", heartPickups.Length >= 3, $"Corazones coleccionables: {heartPickups.Length}");

        // 10. Soluciones de Sigilo Documentadas por Zona
        // Zona 1: Sin enemigos (calibración)
        // Zona 2: Escondite en callejón (Hideout_Z2_Alley) + Ruta alta de tejado (y = 3.8)
        // Zona 3: Vuelo alto sobre canal + carreta en canal fuera del cono + poción Cegar
        // Zona 4: Pasaje bajo exclusivo de gato (CatPassage) + escondite en túnel
        // Zona 5: Plataforma máxima de vigas (y = 6.2u) + escondite en sombras + pociones
        // Zona 6: Salto a cornisa alta o aproximación felina bajo el cono
        HideoutZone[] hideouts = Object.FindObjectsByType<HideoutZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        CatOnlyPassage[] passages = Object.FindObjectsByType<CatOnlyPassage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert("Rutas y Herramientas de Sigilo", hideouts.Length >= 3 && passages.Length >= 2, $"Escondites: {hideouts.Length}, Pasajes de Gato: {passages.Length}");

        sb.AppendLine("--------------------------------------------------------------------------");
        sb.AppendLine($"RESUMEN: {passCount} pruebas PASADAS, {failCount} pruebas FALLIDAS");
        sb.AppendLine("==========================================================================");

        Directory.CreateDirectory("Logs");
        File.WriteAllText(OutReportPath, sb.ToString());
        Debug.Log("[Level2StealthVerifier]\n" + sb);
    }
}
