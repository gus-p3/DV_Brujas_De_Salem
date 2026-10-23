using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Herramienta de suelo del Nivel 2 (independiente del verificador del Nivel 1).
/// Mide la base visible real de cada sprite (fila opaca más baja del PNG, no el rectángulo del sprite) y la compara
/// con la superficie física de la capa Ground que tiene debajo:
/// - Suelo: sube o baja las baldosas de empedrado para que la cara superior de las piedras coincida con el collider.
/// - Props: farolas, cajas, barriles, carretas, puestos, checkpoints y portones apoyan su base en la superficie.
/// - NPC: la raíz (collider) se apoya en su plataforma, los waypoints quedan a esa altura y el hijo "Visual" se
///   desplaza por instancia para que los pies del sprite toquen el suelo (sin cambiar pivotes compartidos).
/// Solo actúa con Level2.unity abierta como escena activa.
/// </summary>
public static class Level2GroundingTool
{
    public const string ScenePath = "Assets/Scenes/Level2.unity";
    public const float Tolerance = 0.02f;

    private const float SearchDistance = 2.5f;
    private const float AlphaThreshold = 0.5f;

    // Hijos directos de las zonas de Environment (y de Gameplay) que deben pisar el suelo
    private static readonly string[] PropPrefixes =
    {
        "Market_Lantern", "Crates_Stack", "Barrel", "Market_Cart", "Market_Crate", "Stall_", "Goal_Gate_Visual",
        "Checkpoint_", "IngredientGate"
    };

    public enum Kind { Suelo, Prop, NPC }

    public struct Entry
    {
        public Kind kind;
        public string path;
        public float bottom;
        public float surface;
        public float Gap => bottom - surface;
        public bool Ok => Mathf.Abs(Gap) <= Tolerance;
    }

    [MenuItem("Tools/Nivel 2/Alinear al suelo")]
    public static void AlignMenu()
    {
        if (!IsLevel2Active()) return;
        int moved = Align();
        Debug.Log($"[Level2GroundingTool] Objetos ajustados: {moved}\n{BuildReport(out _)}");
    }

    [MenuItem("Tools/Nivel 2/Verificar suelo")]
    public static void VerifyMenu()
    {
        if (!IsLevel2Active()) return;
        Debug.Log("[Level2GroundingTool] " + BuildReport(out _));
    }

    public static bool IsLevel2Active()
    {
        if (SceneManager.GetActiveScene().path == ScenePath) return true;
        Debug.LogError("[Level2GroundingTool] Abre Assets/Scenes/Level2.unity como escena activa: la herramienta no toca otras escenas.");
        return false;
    }

    // ------------------------------------------------------------------ Alinear

    /// <summary>Aplica el ajuste de suelo, props y NPC. Devuelve cuántos objetos movió.</summary>
    public static int Align()
    {
        if (!IsLevel2Active()) return 0;
        int moved = 0;

        // 1. Arte del suelo: la cara superior de las piedras coincide con el borde superior del collider
        foreach (BoxCollider2D ground in GroundStrips())
        {
            float top = ground.bounds.max.y;
            foreach (SpriteRenderer tile in ground.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!TrySurfaceTop(tile, out float surfaceTop)) continue;
                if (MoveY(tile.transform, top - surfaceTop)) moved++;
            }
        }
        Physics2D.SyncTransforms();

        // 2. Props, de abajo arriba: un puesto apoyado sobre cajas se ajusta después de ellas
        foreach (Transform prop in Props().OrderBy(p => BottomOf(p)))
        {
            SpriteRenderer sr = ReferenceRenderer(prop);
            if (sr == null || !TryOpaqueBottom(sr, out float bottom)) continue;
            if (!TryFindSurface(sr.bounds.center.x, bottom, prop, out float surface)) continue;
            if (MoveY(prop, surface - bottom)) moved++;
            Physics2D.SyncTransforms();
        }

        // 3. NPC: collider sobre su plataforma, waypoints a esa altura y pies del sprite en el suelo
        foreach (Component npc in Npcs())
        {
            BoxCollider2D col = npc.GetComponent<BoxCollider2D>();
            if (col == null) continue;
            if (TryFindSurface(col.bounds.center.x, col.bounds.min.y, npc.transform, out float surface))
            {
                if (MoveY(npc.transform, surface - col.bounds.min.y)) moved++;
                Physics2D.SyncTransforms();
            }

            PatrolRoute route = npc.GetComponent<PatrolRoute>();
            if (route != null && route.Waypoints != null)
            {
                foreach (Transform wp in route.Waypoints.Where(w => w != null))
                {
                    if (TryFindSurface(wp.position.x, col.bounds.min.y, npc.transform, out float wpSurface) && MoveY(wp, wpSurface - wp.position.y)) moved++;
                }
            }

            SpriteRenderer visual = VisualOf(npc);
            if (visual != null && TryOpaqueBottom(visual, out float feet) && MoveY(visual.transform, col.bounds.min.y - feet)) moved++;
        }
        Physics2D.SyncTransforms();
        return moved;
    }

    private static bool MoveY(Transform t, float dy)
    {
        if (Mathf.Abs(dy) < 0.0005f) return false;
        Undo.RecordObject(t, "Alinear al suelo (Nivel 2)");
        Vector3 p = t.position;
        p.y += dy;
        t.position = p;
        PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        return true;
    }

    // ------------------------------------------------------------------ Reporte

    public static List<Entry> Collect()
    {
        Physics2D.SyncTransforms();
        List<Entry> list = new List<Entry>();

        foreach (BoxCollider2D ground in GroundStrips())
        {
            foreach (SpriteRenderer tile in ground.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!TrySurfaceTop(tile, out float surfaceTop)) continue;
                list.Add(new Entry { kind = Kind.Suelo, path = PathOf(tile.transform), bottom = surfaceTop, surface = ground.bounds.max.y });
            }
        }

        foreach (Transform prop in Props())
        {
            SpriteRenderer sr = ReferenceRenderer(prop);
            if (sr == null || !TryOpaqueBottom(sr, out float bottom)) continue;
            if (!TryFindSurface(sr.bounds.center.x, bottom, prop, out float surface)) continue;
            list.Add(new Entry { kind = Kind.Prop, path = PathOf(prop), bottom = bottom, surface = surface });
        }

        foreach (Component npc in Npcs())
        {
            BoxCollider2D col = npc.GetComponent<BoxCollider2D>();
            SpriteRenderer visual = VisualOf(npc);
            if (col == null || visual == null || !TryOpaqueBottom(visual, out float feet)) continue;
            if (!TryFindSurface(col.bounds.center.x, col.bounds.min.y, npc.transform, out float surface)) continue;
            list.Add(new Entry { kind = Kind.NPC, path = PathOf(npc.transform), bottom = feet, surface = surface });
        }
        return list;
    }

    public static string BuildReport(out int outOfTolerance)
    {
        List<Entry> entries = Collect();
        outOfTolerance = entries.Count(e => !e.Ok);
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Verificar suelo (Nivel 2): {entries.Count} elementos, {outOfTolerance} fuera de tolerancia (±{Tolerance} u)");
        foreach (Entry e in entries.OrderBy(e => e.kind).ThenBy(e => e.path))
        {
            string flag = e.Ok ? "ok" : e.Gap > 0f ? "FLOTA" : "HUNDIDO";
            sb.AppendLine($"{flag,-8} {e.kind,-6} {e.path,-60} base={e.bottom,7:F3} suelo={e.surface,7:F3} hueco={e.Gap,7:F3}");
        }
        return sb.ToString();
    }

    // ------------------------------------------------------------------ Selección

    private static IEnumerable<BoxCollider2D> GroundStrips()
    {
        GameObject gameplay = GameObject.Find("Gameplay");
        if (gameplay == null) yield break;
        int groundLayer = LayerMask.NameToLayer("Ground");
        foreach (Transform t in gameplay.transform)
        {
            if (!t.name.StartsWith("Ground_") || t.gameObject.layer != groundLayer) continue;
            BoxCollider2D col = t.GetComponent<BoxCollider2D>();
            if (col != null && !col.isTrigger) yield return col;
        }
    }

    private static IEnumerable<Transform> Props()
    {
        List<Transform> roots = new List<Transform>();
        GameObject env = GameObject.Find("Environment");
        if (env != null)
        {
            foreach (Transform zone in env.transform)
            {
                if (!zone.name.StartsWith("Zone_")) continue;
                roots.AddRange(zone.Cast<Transform>());
            }
        }
        GameObject gameplay = GameObject.Find("Gameplay");
        if (gameplay != null) roots.AddRange(gameplay.transform.Cast<Transform>());
        return roots.Where(t => PropPrefixes.Any(p => t.name.StartsWith(p)) && ReferenceRenderer(t) != null);
    }

    private static IEnumerable<Component> Npcs()
    {
        HashSet<GameObject> seen = new HashSet<GameObject>();
        foreach (PatrolRoute p in Object.FindObjectsByType<PatrolRoute>(FindObjectsSortMode.None))
            if (seen.Add(p.gameObject)) yield return p;
        foreach (VillagerAI v in Object.FindObjectsByType<VillagerAI>(FindObjectsSortMode.None))
            if (seen.Add(v.gameObject)) yield return v;
    }

    /// <summary>Sprite cuya base se apoya: el poste del puesto, o el sprite propio del prop.</summary>
    private static SpriteRenderer ReferenceRenderer(Transform prop)
    {
        if (prop.name.StartsWith("Stall_"))
        {
            Transform posts = prop.Find("PostsAndCounter");
            return posts != null ? posts.GetComponent<SpriteRenderer>() : null;
        }
        SpriteRenderer sr = prop.GetComponent<SpriteRenderer>();
        return sr != null && sr.sprite != null ? sr : null;
    }

    private static SpriteRenderer VisualOf(Component npc)
    {
        Transform v = npc.transform.Find("Visual");
        SpriteRenderer sr = v != null ? v.GetComponent<SpriteRenderer>() : null;
        return sr != null && sr.sprite != null ? sr : null;
    }

    private static float BottomOf(Transform prop)
    {
        SpriteRenderer sr = ReferenceRenderer(prop);
        return sr != null && TryOpaqueBottom(sr, out float b) ? b : prop.position.y;
    }

    // ------------------------------------------------------------------ Física

    /// <summary>
    /// Superficie de Ground más cercana a la base (arriba o abajo), ignorando triggers y los colliders del propio
    /// objeto. Así un prop sobre un tejado se apoya en el tejado y uno semienterrado sube a su superficie.
    /// </summary>
    public static bool TryFindSurface(float x, float bottom, Transform self, out float surface)
    {
        surface = float.NaN;
        int mask = LayerMask.GetMask("Ground");
        RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(x, bottom + SearchDistance), Vector2.down, SearchDistance * 2f, mask);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger) continue;
            if (self != null && hit.collider.transform.IsChildOf(self)) continue;
            float s = hit.distance <= 0f ? hit.collider.bounds.max.y : hit.point.y;
            if (float.IsNaN(surface) || Mathf.Abs(s - bottom) < Mathf.Abs(surface - bottom)) surface = s;
        }
        return !float.IsNaN(surface);
    }

    // ------------------------------------------------------------------ Píxeles

    private struct Rows
    {
        public int bottom;   // fila opaca más baja (desde abajo del rect del sprite)
        public int surface;  // fila superior con más de la mitad de la anchura opaca (cara de apoyo del suelo)
    }

    private static readonly Dictionary<Sprite, Rows> rowCache = new Dictionary<Sprite, Rows>();
    private static readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();

    /// <summary>Base visible del sprite en mundo (borde inferior de la fila opaca más baja).</summary>
    public static bool TryOpaqueBottom(SpriteRenderer sr, out float worldY)
    {
        worldY = 0f;
        if (!TryRows(sr.sprite, out Rows rows)) return false;
        worldY = RowToWorldY(sr, rows.bottom);
        return true;
    }

    /// <summary>Cara superior del suelo en mundo (borde superior de la primera fila mayormente opaca).</summary>
    public static bool TrySurfaceTop(SpriteRenderer sr, out float worldY)
    {
        worldY = 0f;
        if (sr.sprite == null || !TryRows(sr.sprite, out Rows rows) || rows.surface < 0) return false;
        worldY = RowToWorldY(sr, rows.surface + 1);
        return true;
    }

    private static float RowToWorldY(SpriteRenderer sr, int row)
    {
        Sprite s = sr.sprite;
        float localY = (row - s.pivot.y) / s.pixelsPerUnit;
        if (sr.flipY) localY = -localY;
        return sr.transform.TransformPoint(new Vector3(0f, localY, 0f)).y;
    }

    private static bool TryRows(Sprite sprite, out Rows rows)
    {
        rows = default;
        if (sprite == null) return false;
        if (rowCache.TryGetValue(sprite, out rows)) return true;

        Texture2D tex = ReadableCopy(sprite.texture);
        if (tex == null) return false;
        // El PNG puede ser mayor que la textura importada (Max Size): se escala el rect del sprite
        float scale = (float)tex.width / sprite.texture.width;
        Rect r = sprite.rect;
        int x0 = Mathf.FloorToInt(r.xMin * scale), x1 = Mathf.CeilToInt(r.xMax * scale);
        int y0 = Mathf.FloorToInt(r.yMin * scale), y1 = Mathf.CeilToInt(r.yMax * scale);
        Color32[] px = tex.GetPixels32();
        byte threshold = (byte)(AlphaThreshold * 255f);

        int bottom = -1, surface = -1;
        for (int y = y0; y < y1 && bottom < 0; y++)
            for (int x = x0; x < x1; x++)
                if (px[y * tex.width + x].a > threshold) { bottom = y; break; }
        for (int y = y1 - 1; y >= y0 && surface < 0; y--)
        {
            int opaque = 0;
            for (int x = x0; x < x1; x++) if (px[y * tex.width + x].a > threshold) opaque++;
            if (opaque * 2 > x1 - x0) surface = y;
        }
        if (bottom < 0) return false;

        rows = new Rows
        {
            bottom = Mathf.RoundToInt((bottom - y0) / scale),
            surface = surface < 0 ? -1 : Mathf.RoundToInt((surface - y0) / scale)
        };
        rowCache[sprite] = rows;
        return true;
    }

    private static Texture2D ReadableCopy(Texture2D source)
    {
        string path = AssetDatabase.GetAssetPath(source);
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
        if (textureCache.TryGetValue(path, out Texture2D cached) && cached != null) return cached;
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(File.ReadAllBytes(path))) return null;
        textureCache[path] = tex;
        return tex;
    }

    private static string PathOf(Transform t)
    {
        return t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    }
}
