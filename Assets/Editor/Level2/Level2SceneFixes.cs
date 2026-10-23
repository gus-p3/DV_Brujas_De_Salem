using System.Collections.Generic;
using System.Linq;
using System.Text;
using Level2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Correcciones de escena del Nivel 2 (solo actúa con Level2.unity activa; no toca prefabs ni otras escenas):
/// - Escoba: añade Level2BroomGrant para que la bruja llegue con la escoba del Acto 1.
/// - Puestos: los mostradores pasan a plataformas de un solo sentido (se cruzan a pie y se puede subir a ellos).
/// - NPC: saca a los aldeanos que nacen dentro de cajas o carretas y ajusta las rutas de patrulla a su plataforma.
/// - Suelo: recorta el empedrado al ancho de cada collider y pone agua en el canal (antes se veía suelo sobre el hueco).
/// - Fondo: cielo, aldea lejana con campanario y fachadas de madera con ventanas encendidas en paralaje.
/// - Luz: noche cálida y parpadeo sutil en las farolas.
/// - Suelo: delega en Level2GroundingTool.
/// Tools > Nivel 2 > Verificar ruta comprueba con un BoxCast del tamaño de la bruja que la ruta está libre.
/// </summary>
public static class Level2SceneFixes
{
    private const string ArtDir = "Assets/Art/Environment/Level2/";
    private const string FacadesPath = ArtDir + "Placeholder_Village_Facades_Mid.png";
    private const string FacadesGlowPath = ArtDir + "Placeholder_Village_Facades_Mid_Glow.png";
    private const string VillageFarPath = ArtDir + "Env_Village_Mid.png";
    private const string UnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
    private const string CobblePath = ArtDir + "Placeholder_Cobble_Ground.png";
    private const string WaterPath = ArtDir + "Placeholder_Canal_Water.png";

    // Superficie del agua del canal: por debajo de la calle (0.17) para que se lea como un canal y no como suelo
    private const float CanalWaterTopY = -0.05f;
    // Cuánto se hunden en el agua los props que quedan dentro del canal (la carreta que sirve de escalón)
    private const float CanalPropSink = 0.12f;

    // Capa media: la fachada se ve a 0.8 de escala para que asome la aldea lejana y su campanario por encima
    private const float FacadesScale = 0.8f;
    private const float FacadesBottomY = 0.1f;
    private const float VillageFarBottomY = -0.8f;
    private static readonly Color FacadesTint = new Color(0.58f, 0.58f, 0.72f, 1f);

    // Margen para que un guardia no se quede con medio cuerpo fuera de su plataforma
    private const float PlatformMargin = 0.6f;

    [MenuItem("Tools/Nivel 2/Aplicar correcciones de escena")]
    public static void ApplyMenu()
    {
        if (!Level2GroundingTool.IsLevel2Active()) return;
        Debug.Log("[Level2SceneFixes]\n" + ApplyAll());
    }

    [MenuItem("Tools/Nivel 2/Verificar ruta (BoxCast)")]
    public static void VerifyRouteMenu()
    {
        if (!Level2GroundingTool.IsLevel2Active()) return;
        Debug.Log("[Level2SceneFixes] " + VerifyRoute(out _));
    }

    public static string ApplyAll()
    {
        if (!Level2GroundingTool.IsLevel2Active()) return "Level2 no está activa";
        StringBuilder log = new StringBuilder();
        EnsureBroomGrant(log);
        MakeStallCountersOneWay(log);
        FreeTrappedVillagers(log);
        FitPatrolsToPlatforms(log);
        FitGroundArtToColliders(log);
        BuildCanalWater(log);
        BuildBackground(log);
        SetupLighting(log);
        log.AppendLine($"Suelo: {Level2GroundingTool.Align()} objetos ajustados");
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        return log.ToString();
    }

    // ------------------------------------------------------------------ A. Escoba

    private static void EnsureBroomGrant(StringBuilder log)
    {
        if (Object.FindFirstObjectByType<Level2BroomGrant>() != null)
        {
            log.AppendLine("Escoba: Level2BroomGrant ya existe");
            return;
        }
        GameObject managers = GameObject.Find("Managers");
        GameObject go = new GameObject("Level2_BroomGrant");
        Undo.RegisterCreatedObjectUndo(go, "Level2BroomGrant");
        if (managers != null) go.transform.SetParent(managers.transform, false);
        go.AddComponent<Level2BroomGrant>();
        log.AppendLine("Escoba: añadido Managers/Level2_BroomGrant");
    }

    // ------------------------------------------------------------------ B. Puestos

    private static void MakeStallCountersOneWay(StringBuilder log)
    {
        int count = 0;
        foreach (Transform stall in ZoneChildren().Where(t => t.name.StartsWith("Stall_")))
        {
            Transform counter = stall.Find("PostsAndCounter");
            BoxCollider2D col = counter != null ? counter.GetComponent<BoxCollider2D>() : null;
            if (col == null) continue;
            Undo.RecordObject(col, "Mostrador de un solo sentido");
            col.usedByEffector = true;
            PlatformEffector2D eff = counter.GetComponent<PlatformEffector2D>();
            if (eff == null) eff = Undo.AddComponent<PlatformEffector2D>(counter.gameObject);
            // Mismos ajustes que los toldos del nivel
            eff.useOneWay = true;
            eff.useOneWayGrouping = true;
            eff.surfaceArc = 170f;
            eff.rotationalOffset = 0f;
            eff.useSideFriction = false;
            eff.useSideBounce = false;
            count++;
        }
        log.AppendLine($"Puestos: {count} mostradores convertidos en plataformas de un solo sentido");
    }

    // ------------------------------------------------------------------ NPC

    /// <summary>Mueve en X a los aldeanos cuyo collider nace dentro de un obstáculo sólido hasta el hueco libre más cercano.</summary>
    private static void FreeTrappedVillagers(StringBuilder log)
    {
        Physics2D.SyncTransforms();
        foreach (VillagerAI villager in Object.FindObjectsByType<VillagerAI>(FindObjectsSortMode.None))
        {
            BoxCollider2D col = villager.GetComponent<BoxCollider2D>();
            if (col == null || !Overlaps(col, villager.transform, 0f, out string blocker)) continue;

            float start = villager.transform.position.x;
            float? free = null;
            for (float d = 0.5f; d <= 8f && free == null; d += 0.5f)
            {
                foreach (float sign in new[] { -1f, 1f })
                {
                    float dx = d * sign;
                    if (!Overlaps(col, villager.transform, dx, out _) && HasGroundBelow(col, dx)) { free = dx; break; }
                }
            }
            if (free == null)
            {
                log.AppendLine($"NPC: {villager.name} sigue dentro de {blocker}: no hay hueco libre a ±8 u");
                continue;
            }
            Undo.RecordObject(villager.transform, "Sacar aldeano del obstáculo");
            villager.transform.position += new Vector3(free.Value, 0f, 0f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(villager.transform);
            Physics2D.SyncTransforms();
            log.AppendLine($"NPC: {villager.name} estaba dentro de {blocker}; movido de x={start:F2} a x={villager.transform.position.x:F2}");
        }
    }

    private static bool Overlaps(BoxCollider2D col, Transform self, float dx, out string blocker)
    {
        blocker = null;
        Bounds b = col.bounds;
        // Se separa un poco del suelo para no contar la superficie que pisa
        Vector2 center = new Vector2(b.center.x + dx, b.center.y + 0.05f);
        Vector2 size = new Vector2(b.size.x, b.size.y - 0.1f);
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, size, 0f, LayerMask.GetMask("Ground")))
        {
            if (hit.isTrigger || hit.usedByEffector || hit.transform.IsChildOf(self)) continue;
            blocker = hit.name;
            return true;
        }
        return false;
    }

    private static bool HasGroundBelow(BoxCollider2D col, float dx)
    {
        Bounds b = col.bounds;
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(b.center.x + dx, b.min.y + 0.1f), Vector2.down, 0.4f, LayerMask.GetMask("Ground"));
        return hit.collider != null && !hit.collider.isTrigger;
    }

    /// <summary>
    /// Deja a cada guardia y sus waypoints sobre la plataforma que patrulla. Si el guardia nace en el aire (p. ej. junto
    /// a un tejado) se usa la plataforma bajo el centro de su ruta; los waypoints se recortan a sus bordes.
    /// </summary>
    private static void FitPatrolsToPlatforms(StringBuilder log)
    {
        Physics2D.SyncTransforms();
        int ground = LayerMask.GetMask("Ground");
        foreach (PatrolRoute route in Object.FindObjectsByType<PatrolRoute>(FindObjectsSortMode.None))
        {
            BoxCollider2D col = route.GetComponent<BoxCollider2D>();
            Transform[] wps = route.Waypoints?.Where(w => w != null).ToArray();
            if (col == null || wps == null || wps.Length == 0) continue;

            float bottom = col.bounds.min.y;
            Collider2D platform = PlatformAt(col.bounds.center.x, bottom, route.transform, ground);
            if (platform == null)
            {
                float midX = wps.Average(w => w.position.x);
                platform = PlatformAt(midX, bottom, route.transform, ground);
            }
            if (platform == null)
            {
                log.AppendLine($"NPC: {route.name} sin plataforma reconocible bajo su ruta");
                continue;
            }

            Bounds p = platform.bounds;
            float minX = p.min.x + PlatformMargin, maxX = p.max.x - PlatformMargin;
            bool changed = false;
            foreach (Transform wp in wps)
            {
                Vector3 pos = wp.position;
                Vector3 fitted = new Vector3(Mathf.Clamp(pos.x, minX, maxX), p.max.y, pos.z);
                if ((fitted - pos).sqrMagnitude < 1e-6f) continue;
                Undo.RecordObject(wp, "Ajustar waypoint");
                wp.position = fitted;
                changed = true;
            }
            Vector3 rp = route.transform.position;
            float rx = Mathf.Clamp(rp.x, minX, maxX);
            float ry = rp.y + (p.max.y - bottom);
            if (Mathf.Abs(rx - rp.x) > 1e-3f || Mathf.Abs(ry - rp.y) > 1e-3f)
            {
                Undo.RecordObject(route.transform, "Ajustar guardia a su plataforma");
                route.transform.position = new Vector3(rx, ry, rp.z);
                PrefabUtility.RecordPrefabInstancePropertyModifications(route.transform);
                changed = true;
            }
            Physics2D.SyncTransforms();
            if (changed)
                log.AppendLine($"NPC: {route.name} ajustado a {platform.name} (x {minX:F2}–{maxX:F2}, y {p.max.y:F2}); waypoints {string.Join(", ", wps.Select(w => w.position.x.ToString("F2")))}");
        }
    }

    /// <summary>Plataforma (no trigger) cuya cara superior está más cerca de la altura indicada, como mucho a 1.8 u.</summary>
    private static Collider2D PlatformAt(float x, float y, Transform self, int mask)
    {
        Collider2D best = null;
        float bestDist = 1.8f;
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(new Vector2(x, y + 1.8f), Vector2.down, 3.6f, mask))
        {
            if (hit.collider == null || hit.collider.isTrigger || hit.collider.transform.IsChildOf(self)) continue;
            float d = Mathf.Abs(hit.collider.bounds.max.y - y);
            if (d < bestDist) { bestDist = d; best = hit.collider; }
        }
        return best;
    }

    // ------------------------------------------------------------------ C. Suelo y canal

    /// <summary>
    /// Sustituye las baldosas sueltas de cada tramo de suelo por un único sprite en mosaico del ancho exacto de su
    /// collider. Antes las baldosas sobresalían varios metros y tapaban el hueco del canal: se veía suelo sobre la
    /// zona de muerte.
    /// </summary>
    private static void FitGroundArtToColliders(StringBuilder log)
    {
        SetMeshType(CobblePath, SpriteMeshType.FullRect); // el modo mosaico necesita malla rectangular
        GameObject gameplay = GameObject.Find("Gameplay");
        if (gameplay == null) return;
        int groundLayer = LayerMask.NameToLayer("Ground");
        int strips = 0;
        foreach (Transform strip in gameplay.transform)
        {
            BoxCollider2D col = strip.GetComponent<BoxCollider2D>();
            if (!strip.name.StartsWith("Ground_") || strip.gameObject.layer != groundLayer || col == null) continue;
            SpriteRenderer[] tiles = strip.Cast<Transform>().Where(t => t.name.StartsWith("Tile_"))
                .Select(t => t.GetComponent<SpriteRenderer>()).Where(sr => sr != null && sr.sprite != null).ToArray();
            Transform existing = strip.Find("Tiles");
            SpriteRenderer reference = tiles.Length > 0 ? tiles[0] : existing != null ? existing.GetComponent<SpriteRenderer>() : null;
            if (reference == null) continue;

            Sprite sprite = reference.sprite;
            SpriteRenderer tiled = existing != null ? existing.GetComponent<SpriteRenderer>() : null;
            if (tiled == null)
            {
                GameObject go = new GameObject("Tiles");
                Undo.RegisterCreatedObjectUndo(go, "Suelo en mosaico");
                go.transform.SetParent(strip, false);
                tiled = go.AddComponent<SpriteRenderer>();
            }
            Undo.RecordObject(tiled, "Suelo en mosaico");
            Undo.RecordObject(tiled.transform, "Suelo en mosaico");
            tiled.sprite = sprite;
            tiled.sharedMaterial = reference.sharedMaterial;
            tiled.sortingLayerID = reference.sortingLayerID;
            tiled.sortingOrder = reference.sortingOrder;
            tiled.drawMode = SpriteDrawMode.Tiled;
            tiled.tileMode = SpriteTileMode.Continuous;

            Bounds b = col.bounds;
            Vector2 pivot01 = new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);
            tiled.size = new Vector2(b.size.x, sprite.bounds.size.y);
            // Misma altura que las baldosas originales; en X, el área dibujada empieza y acaba con el collider
            tiled.transform.position = new Vector3(b.min.x + pivot01.x * b.size.x, reference.transform.position.y, reference.transform.position.z);
            tiled.transform.localScale = Vector3.one;

            foreach (SpriteRenderer t in tiles) Undo.DestroyObjectImmediate(t.gameObject);
            strips++;
        }
        Physics2D.SyncTransforms();
        log.AppendLine($"Suelo: {strips} tramos con el empedrado recortado a su collider");
    }

    /// <summary>
    /// Agua en el hueco entre tramos de suelo donde hay una zona de muerte (el canal de la plaza), por debajo de la
    /// calle. Los props que quedan dentro del canal se hunden ligeramente en el agua en lugar de flotar sobre la nada.
    /// </summary>
    private static void BuildCanalWater(StringBuilder log)
    {
        SetMeshType(WaterPath, SpriteMeshType.FullRect);
        ConfigureSprite(WaterPath, SpriteMeshType.FullRect, SpriteAlignment.Center);
        Sprite water = AssetDatabase.LoadAssetAtPath<Sprite>(WaterPath);
        GameObject gameplay = GameObject.Find("Gameplay");
        Transform zone = GameObject.Find("Environment")?.transform.Find("Zone_3_PlazaCentral");
        if (water == null || gameplay == null || zone == null)
        {
            log.AppendLine("Canal: falta el sprite de agua o la zona de la plaza");
            return;
        }

        int groundLayer = LayerMask.NameToLayer("Ground");
        List<Bounds> strips = gameplay.transform.Cast<Transform>()
            .Where(t => t.name.StartsWith("Ground_") && t.gameObject.layer == groundLayer)
            .Select(t => t.GetComponent<BoxCollider2D>()).Where(c => c != null).Select(c => c.bounds).ToList();

        foreach (Transform kz in gameplay.transform.Cast<Transform>().Where(t => t.GetComponent<KillZone>() != null))
        {
            Bounds k = kz.GetComponent<Collider2D>().bounds;
            // Hueco: del tramo que acaba dentro de la zona de muerte al que empieza dentro de ella
            float left = strips.Where(s => s.max.x > k.min.x - 1f && s.max.x < k.max.x).Select(s => s.max.x).DefaultIfEmpty(float.NaN).Max();
            float right = strips.Where(s => s.min.x < k.max.x + 1f && s.min.x > k.min.x).Select(s => s.min.x).DefaultIfEmpty(float.NaN).Min();
            if (float.IsNaN(left) || float.IsNaN(right) || right <= left) continue;

            string name = "Canal_Water_" + kz.name;
            Transform existing = zone.Find(name);
            GameObject go = existing != null ? existing.gameObject : new GameObject(name);
            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(go, "Agua del canal");
                go.transform.SetParent(zone, false);
            }
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = Undo.AddComponent<SpriteRenderer>(go);
            Undo.RecordObject(sr, "Agua del canal");
            Undo.RecordObject(go.transform, "Agua del canal");
            sr.sprite = water;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.sortingLayerName = "Gameplay";
            sr.sortingOrder = -1; // detrás del empedrado y de los props
            float width = right - left + 0.4f; // se mete un poco bajo los bordes del empedrado
            sr.size = new Vector2(width, water.bounds.size.y);
            go.transform.position = new Vector3((left + right) * 0.5f, CanalWaterTopY - water.bounds.size.y * 0.5f, 0f);
            go.transform.localScale = Vector3.one;

            // Props apoyados dentro del canal: base visible un poco bajo la superficie del agua
            foreach (Transform prop in ZoneChildren())
            {
                SpriteRenderer psr = prop.GetComponent<SpriteRenderer>();
                if (psr == null || psr.sprite == null || prop == go.transform) continue;
                float cx = psr.bounds.center.x;
                if (cx <= left || cx >= right || psr.bounds.min.y > 2f || !prop.name.StartsWith("Market_")) continue;
                if (!Level2GroundingTool.TryOpaqueBottom(psr, out float bottom)) continue;
                float dy = (CanalWaterTopY - CanalPropSink) - bottom;
                if (Mathf.Abs(dy) < 0.001f) continue;
                Undo.RecordObject(prop, "Prop en el canal");
                prop.position += new Vector3(0f, dy, 0f);
                log.AppendLine($"Canal: {prop.name} hundido en el agua (base {bottom:F2} → {bottom + dy:F2})");
            }
            log.AppendLine($"Canal: agua entre x={left:F2} y x={right:F2}, superficie y={CanalWaterTopY:F2}");
        }
        Physics2D.SyncTransforms();
    }

    private static void SetMeshType(string path, SpriteMeshType meshType)
    {
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        TextureImporterSettings s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        if (s.spriteMeshType == meshType) return;
        s.spriteMeshType = meshType;
        ti.SetTextureSettings(s);
        ti.SaveAndReimport();
    }

    // ------------------------------------------------------------------ D. Fondo

    private static void BuildBackground(StringBuilder log)
    {
        ConfigureSprite(FacadesPath, SpriteMeshType.FullRect);
        ConfigureSprite(FacadesGlowPath, SpriteMeshType.FullRect);
        ConfigureSprite(VillageFarPath, SpriteMeshType.FullRect);
        Sprite facades = AssetDatabase.LoadAssetAtPath<Sprite>(FacadesPath);
        Sprite facadesGlow = AssetDatabase.LoadAssetAtPath<Sprite>(FacadesGlowPath);
        Sprite villageFar = AssetDatabase.LoadAssetAtPath<Sprite>(VillageFarPath);
        Material unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
        if (facades == null || facadesGlow == null || villageFar == null)
        {
            log.AppendLine("Fondo: faltan sprites; ejecuta ArtSource/Environment/Level2/generate_level2_background.py");
            return;
        }

        Transform parallax = GameObject.Find("Environment")?.transform.Find("Parallax");
        if (parallax == null)
        {
            log.AppendLine("Fondo: no se encontró Environment/Parallax");
            return;
        }

        // El bosque de pinos es del Acto 1: en el pueblo lo sustituyen las fachadas
        Transform trees = parallax.Find("Trees_Mid");
        if (trees != null) Undo.DestroyObjectImmediate(trees.gameObject);

        // Lejano: la aldea con el campanario (reutiliza el arte existente, ahora importado como un único sprite)
        Transform far = parallax.Find("Village_Mid") ?? parallax.Find("Village_Far");
        BuildLayer(parallax, far, "Village_Far", villageFar, null, null, Color.white, 1f, VillageFarBottomY, "Background", 1, 0.8f);

        // Medio: fachadas con entramado, tejados de paja y tejamanil; las ventanas brillan con material sin luz.
        // La base se oscurece hacia el azul de la noche para que puestos, NPC y conos de visión destaquen delante
        Transform mid = parallax.Find("Facades_Mid");
        BuildLayer(parallax, mid, "Facades_Mid", facades, facadesGlow, unlit, FacadesTint, FacadesScale, FacadesBottomY, "Midground", 0, 0.5f);

        log.AppendLine("Fondo: Sky_Far (0.9) + Village_Far (0.8) + Facades_Mid (0.5, ventanas sin luz); Trees_Mid retirado");
    }

    private static Transform BuildLayer(Transform parent, Transform existing, string name, Sprite sprite, Sprite glow, Material glowMaterial,
        Color tint, float scale, float bottomY, string sortingLayer, int order, float factor)
    {
        GameObject layer;
        if (existing != null)
        {
            layer = existing.gameObject;
            Undo.RecordObject(layer, "Capa de paralaje");
            layer.name = name;
            for (int i = layer.transform.childCount - 1; i >= 0; i--) Undo.DestroyObjectImmediate(layer.transform.GetChild(i).gameObject);
        }
        else
        {
            layer = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(layer, "Capa de paralaje");
            layer.transform.SetParent(parent, false);
        }
        layer.SetActive(true);
        layer.transform.localPosition = new Vector3(0f, bottomY, 0f);
        layer.transform.localScale = Vector3.one;

        float width = sprite.bounds.size.x * scale;
        string[] copies = { "Copy_Left", "Copy_Center", "Copy_Right" };
        for (int i = 0; i < 3; i++)
        {
            GameObject copy = new GameObject(copies[i]);
            Undo.RegisterCreatedObjectUndo(copy, "Copia de paralaje");
            copy.transform.SetParent(layer.transform, false);
            copy.transform.localPosition = new Vector3((i - 1) * width, 0f, 0f);
            copy.transform.localScale = new Vector3(scale, scale, 1f);
            SpriteRenderer sr = copy.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = tint;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;

            if (glow != null)
            {
                GameObject g = new GameObject("Glow");
                Undo.RegisterCreatedObjectUndo(g, "Ventanas encendidas");
                g.transform.SetParent(copy.transform, false);
                SpriteRenderer gsr = g.AddComponent<SpriteRenderer>();
                gsr.sprite = glow;
                if (glowMaterial != null) gsr.sharedMaterial = glowMaterial;
                gsr.sortingLayerName = sortingLayer;
                gsr.sortingOrder = order + 1;
            }
        }

        ParallaxLayer pl = layer.GetComponent<ParallaxLayer>();
        if (pl == null) pl = Undo.AddComponent<ParallaxLayer>(layer);
        SerializedObject so = new SerializedObject(pl);
        so.FindProperty("parallaxFactor").floatValue = factor;
        so.FindProperty("infiniteHorizontal").boolValue = true;
        so.FindProperty("lockY").boolValue = true;
        so.ApplyModifiedProperties();
        return layer.transform;
    }

    private static void ConfigureSprite(string path, SpriteMeshType meshType, SpriteAlignment alignment = SpriteAlignment.BottomCenter)
    {
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        bool dirty = ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single ||
                     !Mathf.Approximately(ti.spritePixelsPerUnit, 64f);
        TextureImporterSettings s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        dirty |= s.spriteAlignment != (int)alignment || s.spriteMeshType != meshType || ti.maxTextureSize < 2048;
        if (!dirty) return;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 64f;
        ti.maxTextureSize = Mathf.Max(ti.maxTextureSize, 2048);
        ti.ReadTextureSettings(s);
        s.spriteAlignment = (int)alignment;
        s.spriteMeshType = meshType;
        s.spriteExtrude = 0;
        ti.SetTextureSettings(s);
        ti.SaveAndReimport();
    }

    // ------------------------------------------------------------------ Luz

    private static void SetupLighting(StringBuilder log)
    {
        GameObject global = GameObject.Find("Global Light 2D");
        Light2D gl = global != null ? global.GetComponent<Light2D>() : null;
        if (gl != null && gl.lightType == Light2D.LightType.Global)
        {
            Undo.RecordObject(gl, "Luz global de noche cálida");
            gl.color = new Color(0.46f, 0.43f, 0.62f, 1f);
            gl.intensity = 0.62f;
        }

        int flickers = 0;
        foreach (Light2D l in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (l.name != "Light2D_Lantern" || l.GetComponent<LanternFlicker>() != null) continue;
            Undo.AddComponent<LanternFlicker>(l.gameObject);
            flickers++;
        }
        log.AppendLine($"Luz: global de noche cálida (0.46, 0.43, 0.62) × 0.62; parpadeo en {flickers} farolas");
    }

    // ------------------------------------------------------------------ Verificación de ruta

    /// <summary>
    /// Barre con un BoxCast del tamaño de la bruja (a ras de suelo) desde el spawn hasta la salida. Los mostradores y
    /// toldos de un solo sentido no cuentan; los NPC no chocan con la bruja (capas Player/Enemy sin colisión).
    /// Cajas, barriles y carretas son obstáculos saltables si su altura no supera el salto.
    /// </summary>
    public static string VerifyRoute(out int failures)
    {
        failures = 0;
        Physics2D.SyncTransforms();
        StringBuilder sb = new StringBuilder();
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        BoxCollider2D pcol = player != null ? player.GetComponent<BoxCollider2D>() : null;
        GameObject goal = GameObject.Find("Goal_Exit_Marker");
        if (pcol == null || goal == null)
        {
            failures = 1;
            return "Ruta: falta la bruja o Goal_Exit_Marker";
        }

        float jump = new SerializedObject(player).FindProperty("jumpHeight").floatValue;
        int playerLayer = player.gameObject.layer;
        int mask = 0;
        for (int l = 0; l < 32; l++) if (!Physics2D.GetIgnoreLayerCollision(playerLayer, l) && l != playerLayer) mask |= 1 << l;

        Vector2 size = pcol.size;
        float groundTop = pcol.bounds.min.y;
        Vector2 origin = new Vector2(pcol.bounds.center.x, groundTop + size.y * 0.5f + 0.03f);
        float distance = goal.transform.position.x - origin.x;
        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, size - new Vector2(0f, 0.06f), 0f, Vector2.right, distance, mask);

        sb.AppendLine($"Verificar ruta: BoxCast {size.x:F2}×{size.y:F2} desde x={origin.x:F1} hasta la salida x={goal.transform.position.x:F1} (salto {jump:F2} u)");
        HashSet<Collider2D> seen = new HashSet<Collider2D>();
        foreach (RaycastHit2D h in hits.OrderBy(h => h.point.x))
        {
            Collider2D c = h.collider;
            if (c == null || c.isTrigger || !seen.Add(c)) continue;
            if (c.usedByEffector && c.GetComponent<PlatformEffector2D>() is PlatformEffector2D e && e.useOneWay) continue;
            Bounds b = c.bounds;
            float height = b.max.y - groundTop;
            string path = PathOf(c.transform);
            string verdict;
            if (path.Contains("Stall_"))
            {
                verdict = "FALLO: puesto sólido en la ruta";
                failures++;
            }
            else if (c.name == "Alley_LowCeiling")
                verdict = "intencional: paso de gato (1.05 u); alternativa humana por el techo del callejón o volando";
            else if (path.Contains("IngredientGate"))
                verdict = "intencional: portón de salida, se abre con los ingredientes";
            else if (height <= jump - 0.2f)
                verdict = $"obstáculo saltable (alto {height:F2} u ≤ salto {jump:F2} u)";
            else
            {
                verdict = $"FALLO: obstáculo de {height:F2} u, más alto que el salto";
                failures++;
            }
            sb.AppendLine($"  x={b.min.x,7:F2}–{b.max.x,7:F2}  {path,-55} {verdict}");
        }

        // Huecos sin suelo a ras de calle (el canal): se informan con lo que hay encima para cruzarlos
        int groundMask = LayerMask.GetMask("Ground");
        float gapStart = float.NaN;
        for (float x = origin.x; x <= goal.transform.position.x; x += 0.25f)
        {
            RaycastHit2D down = Physics2D.Raycast(new Vector2(x, groundTop + 0.3f), Vector2.down, 1f, groundMask);
            bool solid = down.collider != null && !down.collider.isTrigger;
            if (!solid && float.IsNaN(gapStart)) gapStart = x;
            if (solid && !float.IsNaN(gapStart))
            {
                Collider2D over = Physics2D.OverlapArea(new Vector2(gapStart, groundTop + 1.5f), new Vector2(x, groundTop + jump + 1.2f), groundMask);
                string via = over != null ? $"plataforma {over.name} encima; también volando" : "solo volando";
                sb.AppendLine($"  x={gapStart,7:F2}–{x,7:F2}  hueco de {x - gapStart:F2} u sin suelo: se cruza por {via}");
                gapStart = float.NaN;
            }
        }
        sb.AppendLine(failures == 0 ? "Ruta: OK, ningún puesto ni NPC fijo cierra el paso" : $"Ruta: {failures} bloqueo(s)");
        return sb.ToString();
    }

    // ------------------------------------------------------------------ Utilidades

    private static IEnumerable<Transform> ZoneChildren()
    {
        GameObject env = GameObject.Find("Environment");
        if (env == null) yield break;
        foreach (Transform zone in env.transform)
        {
            if (!zone.name.StartsWith("Zone_")) continue;
            foreach (Transform t in zone) yield return t;
        }
    }

    private static string PathOf(Transform t)
    {
        return t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    }
}
