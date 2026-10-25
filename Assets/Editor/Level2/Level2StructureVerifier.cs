#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Level2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Verificador automatico de estructuras del Nivel 2 (Menu: Tools > Nivel 2 > Verificar estructuras).
/// Comprueba:
/// 1) Puertas, casas y props apoyados sobre suelo real (tolerancia <= 0.02u).
/// 2) Escaleras conectadas en ambos extremos (BottomY y TopY <= 0.02u) y asociadas a casa/interior.
/// 3) Cero referencias al sprite de casa antiguo en Level2.unity.
/// </summary>
public static class Level2StructureVerifier
{
    private const float TOLERANCE = 0.02f;

    [MenuItem("Tools/Nivel 2/Verificar estructuras")]
    public static int VerifyAllStructures()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.path != "Assets/Scenes/Level2.unity")
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Level2.unity", OpenSceneMode.Single);
        }
        Physics2D.SyncTransforms();

        List<string> findings = new List<string>();
        int groundMask = 1 << LayerMask.NameToLayer("Ground");

        // 1. Verificar todas las Casas (Decorativas, Estructurales e Interactivas) en Village_Exterior_Structures
        Transform extRoot = GameObject.Find("Environment/Village_Exterior_Structures")?.transform;
        List<SpriteRenderer> houseRenderers = new List<SpriteRenderer>();
        int checkedHouses = 0;
        if (extRoot != null)
        {
            foreach (Transform child in extRoot)
            {
                if (!child.gameObject.activeInHierarchy) continue;
                if (!child.name.StartsWith("House")) continue;

                SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                if (sr == null || sr.sprite == null)
                {
                    findings.Add($"[CASA SIN SPRITE] {child.name} no tiene Sprite asignado.");
                    continue;
                }
                houseRenderers.Add(sr);
                checkedHouses++;
                float minY = sr.bounds.min.y;
                float groundY = Level2CompleteFixer.RaycastGroundY(child.position.x, 0.17f);
                float diff = Mathf.Abs(minY - groundY);
                if (diff > TOLERANCE)
                {
                    findings.Add($"[CASA DESALINEADA] {child.name} minY={minY:F4}, suelo={groundY:F4} (diff={diff:F4}u > {TOLERANCE}u)");
                }
            }
        }

        // 2. Verificar todas las Puertas (HouseDoor)
        HouseDoor[] doors = Object.FindObjectsByType<HouseDoor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int checkedDoors = 0;
        foreach (HouseDoor door in doors)
        {
            checkedDoors++;
            SpriteRenderer sr = door.GetComponent<SpriteRenderer>();
            float minY = sr != null && sr.sprite != null ? sr.bounds.min.y : door.transform.position.y;
            float surfaceY = Level2CompleteFixer.FindSurfaceBelow(door.transform.position.x, minY + 0.8f);
            float diff = Mathf.Abs(minY - surfaceY);
            if (diff > TOLERANCE)
            {
                findings.Add($"[PUERTA FLOTANDO/HUNDIDA] {door.name} minY={minY:F4}, superficie={surfaceY:F4} (diff={diff:F4}u > {TOLERANCE}u)");
            }

            if (door.transform.position.y < 15f)
            {
                bool hasBackingHouse = false;
                foreach (SpriteRenderer hsr in houseRenderers)
                {
                    if (door.transform.position.x >= hsr.bounds.min.x - 1.2f && door.transform.position.x <= hsr.bounds.max.x + 1.2f)
                    {
                        hasBackingHouse = true;
                        break;
                    }
                }
                if (!hasBackingHouse)
                {
                    findings.Add($"[PUERTA SIN CASA] {door.name} en x={door.transform.position.x:F2} no tiene fachada de casa detras.");
                }
            }
        }

        // 3. Verificar todas las Escaleras (LadderInteractable)
        LadderInteractable[] ladders = Object.FindObjectsByType<LadderInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int checkedLadders = 0;
        foreach (LadderInteractable ladder in ladders)
        {
            checkedLadders++;
            float cx = ladder.CenterX;
            float bY = ladder.BottomY;
            float tY = ladder.TopY;

            float bottomSurface = FindNearestFloorSurfaceAt(cx, bY, groundMask);
            float topSurface = FindNearestFloorSurfaceAt(cx, tY, groundMask);

            float bDiff = Mathf.Abs(bY - bottomSurface);
            float tDiff = Mathf.Abs(tY - topSurface);

            if (bDiff > TOLERANCE)
            {
                findings.Add($"[ESCALERA BASE DESCONECTADA] {ladder.name} BottomY={bY:F4}, piso={bottomSurface:F4} (diff={bDiff:F4}u > {TOLERANCE}u)");
            }
            if (tDiff > TOLERANCE)
            {
                findings.Add($"[ESCALERA TOPE DESCONECTADO] {ladder.name} TopY={tY:F4}, piso={topSurface:F4} (diff={tDiff:F4}u > {TOLERANCE}u)");
            }

            if (ladder.transform.position.y < 15f)
            {
                bool associatedWithHouse = false;
                foreach (SpriteRenderer hsr in houseRenderers)
                {
                    if (cx >= hsr.bounds.min.x - 1.8f && cx <= hsr.bounds.max.x + 1.8f)
                    {
                        associatedWithHouse = true;
                        break;
                    }
                }
                if (!associatedWithHouse)
                {
                    findings.Add($"[ESCALERA HUERFANA] {ladder.name} en x={cx:F2} no pertenece a ninguna fachada de casa.");
                }
            }
        }

        // 4. Verificar que NO queden sprites de casas antiguas en Level2.unity
        SpriteRenderer[] allSrs = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (SpriteRenderer sr in allSrs)
        {
            if (sr == null || sr.sprite == null) continue;
            string spName = sr.sprite.name;
            if (spName.Contains("Village_House_Facade") || spName.Contains("Placeholder_Village_Facades") || spName.Contains("Env_Village_Mid"))
            {
                findings.Add($"[SPRITE CASA ANTIGUO] {GetPath(sr.transform)} aun usa sprite antiguo '{spName}'.");
            }
        }

        if (findings.Count == 0)
        {
            Debug.Log($"[Level2StructureVerifier] EXITO: 0 hallazgos. Casas verificadas: {checkedHouses}, Puertas verificadas: {checkedDoors}, Escaleras verificadas: {checkedLadders}. Todas dentro de tolerancia <= {TOLERANCE}u y con el arte nuevo.");
        }
        else
        {
            Debug.LogError($"[Level2StructureVerifier] FALLO con {findings.Count} hallazgos:\n - " + string.Join("\n - ", findings));
        }

        return findings.Count;
    }

    private static float FindNearestFloorSurfaceAt(float x, float targetY, int groundMask)
    {
        float[] probeOffsets = { 0f, -0.5f, 0.5f, -1.0f, 1.0f };
        float bestY = -999f;
        float minDiff = 999f;
        foreach (float ox in probeOffsets)
        {
            Collider2D[] cols = Physics2D.OverlapBoxAll(new Vector2(x + ox, targetY - 0.15f), new Vector2(0.8f, 0.8f), 0f, groundMask);
            foreach (Collider2D c in cols)
            {
                if (c != null && !c.isTrigger)
                {
                    float top = c.bounds.max.y;
                    float d = Mathf.Abs(targetY - top);
                    if (d < minDiff)
                    {
                        minDiff = d;
                        bestY = top;
                    }
                }
            }
        }
        return bestY;
    }

    private static string GetPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            p = t.name + "/" + p;
        }
        return p;
    }
}
#endif
