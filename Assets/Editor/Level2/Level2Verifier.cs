using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Suite de verificación automatizada para el escenario del Nivel 2 (Acto 2 — Pueblo y Mercado).
/// Comprueba geometría sólida, densidad de plataformas, 3 capas de paralaje, pasajes felinos,
/// KillZones, Checkpoints, marcadores de integración y alturas críticas de salto.
/// </summary>
public static class Level2Verifier
{
    private const string OutReportPath = "Logs/Level2_Stage_Verification.txt";

    [MenuItem("Brujas/Nivel 2/Verificar Escenario Pueblo y Mercado")]
    public static void RunVerification()
    {
        var sb = new StringBuilder();
        sb.AppendLine("==========================================================================");
        sb.AppendLine("=== VERIFICACIÓN DE ESCENARIO: NIVEL 2 (ACTO 2 — PUEBLO Y MERCADO) ===");
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

        // 1. Witch y Spawn inicial
        GameObject witch = GameObject.Find("Witch");
        Assert("Spawn de Bruja", witch != null, witch != null ? $"Bruja en ({witch.transform.position.x:F2}, {witch.transform.position.y:F2})" : "Witch no encontrada");

        // 2. Portón de Salida y Marcador Trigger
        GameObject exitMarker = GameObject.Find("Goal_Exit_Marker");
        BoxCollider2D exitCol = exitMarker != null ? exitMarker.GetComponent<BoxCollider2D>() : null;
        Assert("Marcador de Salida", exitCol != null && exitCol.isTrigger, exitCol != null ? $"Trigger en x={exitMarker.transform.position.x:F1}, size={exitCol.size}" : "No encontrado o sin trigger");

        // 3. Checkpoints (mínimo 1 por zona = 6)
        Checkpoint[] checkpoints = Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert("Checkpoints por Zona", checkpoints.Length >= 6, $"Encontrados {checkpoints.Length} checkpoints (esperado >= 6)");

        // 4. KillZones en brechas
        KillZone[] killZones = Object.FindObjectsByType<KillZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool allKzTriggers = killZones.All(kz => kz.GetComponent<BoxCollider2D>() != null && kz.GetComponent<BoxCollider2D>().isTrigger);
        Assert("KillZones con Triggers", killZones.Length >= 1 && allKzTriggers, $"Encontradas {killZones.Length} KillZones con colliders trigger");

        // 5. Capa Ground en todas las plataformas y suelos
        int groundLayer = LayerMask.NameToLayer("Ground");
        var allColliders = Object.FindObjectsByType<BoxCollider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int solidGroundCount = 0;
        int wrongLayerCount = 0;
        foreach (var col in allColliders)
        {
            if (!col.isTrigger)
            {
                if (col.gameObject.layer == groundLayer) solidGroundCount++;
                else if (col.name != "Witch" && !col.transform.IsChildOf(witch?.transform)) wrongLayerCount++;
            }
        }
        Assert("Capas de Plataformas Sólidas", wrongLayerCount == 0, $"Sólidos en Ground: {solidGroundCount}, Fuera de Ground: {wrongLayerCount}");

        // 6. Pasaje Solo Gato (CatOnlyPassage) y Techo Físico
        CatOnlyPassage[] catPassages = Object.FindObjectsByType<CatOnlyPassage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        GameObject ceiling = GameObject.Find("Alley_LowCeiling");
        BoxCollider2D ceilCol = ceiling != null ? ceiling.GetComponent<BoxCollider2D>() : null;
        float clearance = ceilCol != null ? (ceiling.transform.position.y - ceilCol.size.y * 0.5f) - 0.17f : 99f;
        bool catPassageValid = catPassages.Length >= 2 && clearance <= 1.25f && clearance >= 0.8f;
        Assert("Pasaje Exclusivo para Gato", catPassageValid, $"Triggers: {catPassages.Length}, Altura libre: {clearance:F2}u (humana 1.66u bloqueada, gato 0.56u pasa)");

        // 7. Escondites de Sigilo (HideoutZone)
        HideoutZone[] hideouts = Object.FindObjectsByType<HideoutZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert("Zonas de Ocultación / Sombras", hideouts.Length >= 3, $"Encontrados {hideouts.Length} escondites HideoutZone");

        // 8. Paralaje (Mínimo 3 capas)
        ParallaxLayer[] parallaxLayers = Object.FindObjectsByType<ParallaxLayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert("Paralaje >= 3 Capas", parallaxLayers.Length >= 3, $"Encontradas {parallaxLayers.Length} capas de paralaje ({string.Join(", ", parallaxLayers.Select(p => p.name))})");

        // 9. Densidad de Plataformas vs Nivel 1
        // Level 1 tiene 7 plataformas en 232u (~3.0 por 100u).
        // Contar plataformas elevadas (toldos, techos, carretas, cajas) con superficie y > 0.5u
        int elevatedPlatforms = allColliders.Count(c => !c.isTrigger && c.gameObject.layer == groundLayer && c.bounds.max.y >= 0.5f && !c.name.StartsWith("Ground_"));
        float levelLength = 185f;
        float densityPer100 = (elevatedPlatforms / levelLength) * 100f;
        Assert("Densidad de Plataformas Nivel 2", elevatedPlatforms >= 20 && densityPer100 > 10f, $"Plataformas: {elevatedPlatforms} en {levelLength}u -> {densityPer100:F1} plataformas/100u (Level 1: 3.0/100u)");

        // 10. Marcadores de Integración (NPCs e Ingredientes)
        string[] expectedNpcMarkers = {
            "Guard_Spawn_Z1", "Waypoint_Z1_A", "Waypoint_Z1_B",
            "Guard_Spawn_Z2", "Waypoint_Z2_A", "Waypoint_Z2_B", "Villager_Spawn_Z2",
            "Guard_Spawn_Z3_1", "Waypoint_Z3_1A", "Waypoint_Z3_1B", "Guard_Spawn_Z3_2", "Waypoint_Z3_2A", "Waypoint_Z3_2B", "Villager_Spawn_Z3",
            "Guard_Spawn_Z4_Roof", "Waypoint_Z4_A", "Waypoint_Z4_B",
            "Guard_Spawn_Z5_1", "Waypoint_Z5_1A", "Waypoint_Z5_1B", "Guard_Spawn_Z5_2", "Waypoint_Z5_2A", "Waypoint_Z5_2B", "Villager_Spawn_Z5_1", "Villager_Spawn_Z5_2",
            "Guard_Spawn_Z6", "Waypoint_Z6_A", "Waypoint_Z6_B"
        };
        int foundNpcMarkers = expectedNpcMarkers.Count(m => GameObject.Find(m) != null);
        Assert("Marcadores de Spawn y Waypoints de NPCs", foundNpcMarkers == expectedNpcMarkers.Length, $"Encontrados {foundNpcMarkers}/{expectedNpcMarkers.Length} marcadores");

        string[] expectedIngMarkers = {
            "Ingredient_Spawn_Z1_Mushroom",
            "Ingredient_Spawn_Z2_Spider",
            "Ingredient_Spawn_Z3_Flower",
            "Ingredient_Spawn_Z4_Root",
            "Ingredient_Spawn_Z5_BonusMushroom"
        };
        int foundIngMarkers = expectedIngMarkers.Count(m => GameObject.Find(m) != null);
        Assert("Marcadores de Ingredientes de Poción", foundIngMarkers == expectedIngMarkers.Length, $"Encontrados {foundIngMarkers}/{expectedIngMarkers.Length} marcadores");

        // 11. Iluminación URP 2D
        Light2D[] allLights = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int lanternLights = allLights.Count(l => l.lightType == Light2D.LightType.Point && l.name.Contains("Lantern"));
        Assert("Iluminación de Farolas URP 2D", lanternLights >= 8, $"Encontradas {lanternLights} farolas encendidas con PointLight2D");

        // 12. Límites de Cámara
        Camera cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        CameraFollow cf = cam != null ? cam.GetComponent<CameraFollow>() : null;
        SerializedObject soCf = cf != null ? new SerializedObject(cf) : null;
        Vector2 maxLimit = soCf != null ? soCf.FindProperty("maxLimit").vector2Value : Vector2.zero;
        Assert("Límites de CameraFollow", maxLimit.x >= 185f, $"maxLimit.x = {maxLimit.x} (cubre el nivel hasta la meta)");

        sb.AppendLine("--------------------------------------------------------------------------");
        sb.AppendLine($"RESUMEN: {passCount} pruebas PASADAS, {failCount} pruebas FALLIDAS");
        sb.AppendLine("==========================================================================");

        Directory.CreateDirectory("Logs");
        File.WriteAllText(OutReportPath, sb.ToString());
        Debug.Log("[Level2Verifier]\n" + sb);
    }
}
