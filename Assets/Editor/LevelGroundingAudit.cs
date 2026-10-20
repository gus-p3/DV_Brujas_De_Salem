using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Audita el apoyo en el suelo de un nivel: para cada personaje, objeto y prop compara la base visible
/// de su sprite (píxel opaco más bajo, a partir de la malla ajustada del sprite) con la superficie física
/// (collider de la capa Ground) que tiene justo debajo. Reporta lo que flota o se hunde demasiado.
/// </summary>
public static class LevelGroundingAudit
{
    // Tolerancias (u): más de esto por encima del suelo se ve flotando; más por debajo se ve hundido
    public const float FloatTolerance = 0.03f;
    public const float SinkTolerance = 0.12f;

    [MenuItem("Tools/Nivel/Auditar apoyo en el suelo")]
    public static void Run()
    {
        List<Entry> entries = Collect();
        StringBuilder sb = new StringBuilder();
        int bad = 0;
        foreach (Entry e in entries.OrderBy(e => e.x))
        {
            string flag = e.gap > FloatTolerance ? "FLOTA" : e.gap < -SinkTolerance ? "HUNDIDO" : "ok";
            if (flag != "ok") bad++;
            sb.AppendLine($"{flag,-8} {e.path,-48} x={e.x,7:F2} base={e.visualBottom,6:F3} suelo={e.surface,6:F3} hueco={e.gap,6:F3}");
        }
        Debug.Log($"[LevelGroundingAudit] {entries.Count} objetos, {bad} con problemas\n{sb}");
    }

    public struct Entry
    {
        public string path;
        public float x, visualBottom, surface, gap;
    }

    public static List<Entry> Collect()
    {
        List<Entry> list = new List<Entry>();
        Physics2D.SyncTransforms();
        int groundMask = LayerMask.GetMask("Ground");
        foreach (SpriteRenderer sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (!ShouldAudit(sr)) continue;
            if (!TryVisualBottom(sr, out float bottom, out float centerX)) continue;
            // Se busca el suelo desde un poco por encima de la base visible
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(centerX, bottom + 0.6f), Vector2.down, 3f, groundMask);
            if (hit.collider == null) continue;
            list.Add(new Entry
            {
                path = PathOf(sr.transform),
                x = centerX,
                visualBottom = bottom,
                surface = hit.point.y,
                gap = bottom - hit.point.y
            });
        }
        return list;
    }

    /// <summary>Objetos que deben tocar el suelo: personajes (raíz con Rigidbody2D) y props con GroundAnchor o conocidos.</summary>
    private static bool ShouldAudit(SpriteRenderer sr)
    {
        if (!sr.enabled || !sr.gameObject.activeInHierarchy || sr.sprite == null) return false;
        if (sr.GetComponentInParent<GroundAnchor>() != null) return true;
        if (sr.GetComponentInParent<VillagerAI>() != null || sr.GetComponentInParent<PatrolRoute>() != null) return sr.name == "Visual";
        if (sr.GetComponentInParent<PlayerController>() != null) return false; // la bruja se audita aparte (rig)
        string n = sr.sprite.name;
        return n.StartsWith("Env_Sign") || n.StartsWith("Env_Checkpoint") || n.StartsWith("Env_Goal") ||
               n.StartsWith("Env_Corn") || n.StartsWith("PotionItem") || n.StartsWith("Item_");
    }

    /// <summary>Base visible: el vértice más bajo de la malla ajustada del sprite, en mundo.</summary>
    public static bool TryVisualBottom(SpriteRenderer sr, out float bottom, out float centerX)
    {
        bottom = GroundAnchor.VisualBottom(sr, out centerX);
        return true;
    }

    private static string PathOf(Transform t)
    {
        string p = t.name;
        for (Transform c = t.parent; c != null && c.parent != null; c = c.parent) p = c.name + "/" + p;
        return p;
    }
}
