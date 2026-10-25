using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Level2;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Verificador propio del Nivel 2 (independiente de los del Nivel 1): puestos, puertas, postes y textos de cofres.
/// Reporta 0 hallazgos cuando:
/// - ningún puesto se empalma con otro (paso libre ≥1.0 u en calle) ni tapa casas interactivas, puertas, escaleras,
///   checkpoints, cofres, letreros o el punto de inicio de un aldeano;
/// - todos los puestos tienen postes, mostrador, toldo, mercancía y colgante (y techo físico alineado en la calle);
/// - postes, props, cofres y escaleras apoyan en su superficie (±0.02 u);
/// - las puertas miden 1.8–2.2 u, apoyan en su suelo y no sobresalen de su fachada;
/// - cada puesto se anima con fase y velocidad propias;
/// - existen el aviso inicial, la guía del HUD, el letrero de calle y el letrero de cada casa con cofre (y ninguno en
///   casas sin cofre), y ningún texto pide E para abrir o interactuar.
/// </summary>
public static class Level2StallsVerifier
{
    private const string ScenePath = "Assets/Scenes/Level2.unity";
    private const float Tolerance = 0.02f;
    private const float StreetGap = 1.0f;
    private const float SignDoorRadius = 3.0f;

    private static readonly string[] PropPrefixes = { "Market_Lantern", "Crates_Stack", "Barrel", "Market_Cart", "Goal_Gate_Visual" };
    private static readonly string[] StallPieces = { "PostsAndCounter", "Elevated_CanopyPosts", "Awning_Sway_Visual", "Hanging_Lantern_Visual", "Counter_Wares_Visual", "Hanging_Wares_Pivot" };

    private static readonly Regex EToInteract = new Regex(
        @"\bE\b\s*[:—\-]?\s*(para\s+)?(abrir|entrar|interactuar|recoger|examinar)|(abr|entr|interactu|recog|examin)\w*\s+(con|pulsando|presionando)\s+(la\s+tecla\s+)?E\b",
        RegexOptions.IgnoreCase);

    private class Stall
    {
        public Transform root; public bool elevated; public Bounds bounds; public List<SpriteRenderer> renderers;
        public string Name => root.name;
    }

    [MenuItem("Tools/Nivel 2/Verificar puestos y puertas")]
    public static void VerifyMenu()
    {
        int findings = Run(out string report);
        if (findings == 0) Debug.Log(report); else Debug.LogError(report);
    }

    public static int Run(out string report)
    {
        StringBuilder sb = new StringBuilder();
        if (SceneManager.GetActiveScene().path != ScenePath)
        {
            report = "[Level2StallsVerifier] Abre Assets/Scenes/Level2.unity como escena activa.";
            return 1;
        }
        Physics2D.SyncTransforms();
        List<string> findings = new List<string>();
        List<Stall> stalls = CollectStalls();

        CheckCompleteness(stalls, findings);
        CheckOverlaps(stalls, findings);
        CheckGrounding(stalls, findings);
        CheckDoors(findings, sb);
        CheckAnimation(stalls, findings);
        CheckTexts(findings);

        Level2SceneFixes.VerifyRoute(out int routeFailures);
        if (routeFailures > 0) findings.Add($"[RUTA] El BoxCast humano (0.6×1.6) de spawn a salida encontró {routeFailures} bloqueo(s) no saltables");

        StringBuilder head = new StringBuilder();
        head.AppendLine($"[Level2StallsVerifier] {(findings.Count == 0 ? "OK" : "FALLO")}: {findings.Count} hallazgos — {stalls.Count} puestos ({stalls.Count(s => !s.elevated)} de calle, {stalls.Count(s => s.elevated)} elevados)");
        foreach (string f in findings) head.AppendLine(" - " + f);
        head.AppendLine("Puestos:");
        foreach (Stall s in stalls.OrderBy(s => s.bounds.min.x))
            head.AppendLine($"   {s.Name,-20} {(s.elevated ? "elevado" : "calle  ")} x[{s.bounds.min.x,7:F2},{s.bounds.max.x,7:F2}] y[{s.bounds.min.y,5:F2},{s.bounds.max.y,5:F2}]");
        head.Append(sb);
        report = head.ToString();
        return findings.Count;
    }

    // ------------------------------------------------------------------ Recolección

    private static List<Stall> CollectStalls()
    {
        List<Stall> list = new List<Stall>();
        foreach (Transform zone in GameObject.Find("Environment").transform)
        {
            if (!zone.name.StartsWith("Zone_")) continue;
            foreach (Transform t in zone)
            {
                bool street = t.name.StartsWith("Stall_");
                bool elevated = t.Find("Elevated_CanopyPosts") != null;
                if (!street && !elevated) continue;
                List<SpriteRenderer> rs = StallPieces.Select(p => t.Find(p)).Where(p => p != null)
                    .SelectMany(p => p.GetComponentsInChildren<SpriteRenderer>()).Where(r => r.enabled && r.sprite != null).ToList();
                if (rs.Count == 0) { list.Add(new Stall { root = t, elevated = elevated, bounds = new Bounds(t.position, Vector3.zero), renderers = rs }); continue; }
                Bounds b = rs[0].bounds;
                foreach (SpriteRenderer r in rs) b.Encapsulate(r.bounds);
                list.Add(new Stall { root = t, elevated = elevated, bounds = b, renderers = rs });
            }
        }
        return list;
    }

    // ------------------------------------------------------------------ Completitud

    private static void CheckCompleteness(List<Stall> stalls, List<string> findings)
    {
        foreach (Stall s in stalls)
        {
            List<string> missing = new List<string>();
            if (!HasVisual(s.root, s.elevated ? "Elevated_CanopyPosts" : "PostsAndCounter")) missing.Add("postes/mostrador");
            if (!HasVisual(s.root, "Awning_Sway_Visual")) missing.Add("toldo");
            if (!HasVisual(s.root, "Counter_Wares_Visual")) missing.Add("mercancía del mostrador");
            if (s.root.Find("Hanging_Wares_Pivot")?.GetComponentInChildren<SpriteRenderer>() == null) missing.Add("colgante");
            if (!HasVisual(s.root, "Hanging_Lantern_Visual")) missing.Add("farol");
            if (!s.elevated)
            {
                Collider2D roof = s.root.Find("AwningRoof")?.GetComponent<Collider2D>();
                SpriteRenderer awning = s.root.Find("Awning_Sway_Visual")?.GetComponent<SpriteRenderer>();
                if (roof == null) missing.Add("techo físico del toldo");
                else if (awning != null && (Mathf.Abs(roof.bounds.center.x - awning.bounds.center.x) > 0.25f || roof.bounds.max.y > awning.bounds.max.y + Tolerance || roof.bounds.max.y < awning.bounds.min.y))
                    missing.Add("techo físico desalineado con el toldo visible");
                Collider2D counter = s.root.Find("PostsAndCounter")?.GetComponent<Collider2D>();
                if (counter == null) missing.Add("mostrador físico");
                int ground = LayerMask.NameToLayer("Ground");
                if ((counter != null && counter.gameObject.layer != ground) || (roof != null && roof.gameObject.layer != ground))
                    missing.Add("mostrador o techo fuera de la capa Ground");
            }
            if (missing.Count > 0) findings.Add($"[PUESTO INCOMPLETO] {s.Name}: falta {string.Join(", ", missing)}");
        }
    }

    private static bool HasVisual(Transform root, string child)
    {
        SpriteRenderer sr = root.Find(child)?.GetComponent<SpriteRenderer>();
        return sr != null && sr.enabled && sr.sprite != null;
    }

    // ------------------------------------------------------------------ Empalmes

    private static void CheckOverlaps(List<Stall> stalls, List<string> findings)
    {
        for (int i = 0; i < stalls.Count; i++)
        {
            for (int j = i + 1; j < stalls.Count; j++)
            {
                Stall a = stalls[i], b = stalls[j];
                if (!RangesOverlap(a.bounds.min.y, a.bounds.max.y, b.bounds.min.y, b.bounds.max.y, 0f)) continue;
                float gap = Mathf.Max(a.bounds.min.x, b.bounds.min.x) - Mathf.Min(a.bounds.max.x, b.bounds.max.x);
                float needed = !a.elevated && !b.elevated ? StreetGap : 0.5f;
                if (gap < needed - 0.001f) findings.Add($"[PUESTOS EMPALMADOS] {a.Name} y {b.Name}: separación {gap:F2} u < {needed:F1} u");
            }
        }

        List<HouseDoor> extDoors = Object.FindObjectsByType<HouseDoor>(FindObjectsSortMode.None).Where(d => d.OwningInterior == null).ToList();
        List<SpriteRenderer> houses = InteractiveHouses(extDoors);
        List<(string name, Bounds b)> blockers = new List<(string, Bounds)>();
        blockers.AddRange(extDoors.Select(d => (d.name, d.GetComponent<SpriteRenderer>().bounds)));
        blockers.AddRange(Object.FindObjectsByType<LadderInteractable>(FindObjectsSortMode.None)
            .Select(l => l.GetComponent<SpriteRenderer>()).Where(r => r != null).Select(r => (r.name, Shrink(r.bounds, 0.05f))));
        foreach (Checkpoint c in Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None))
        {
            SpriteRenderer[] rs = c.GetComponentsInChildren<SpriteRenderer>();
            if (rs.Length > 0) blockers.Add((c.name, Encapsulate(rs)));
        }
        blockers.AddRange(Object.FindObjectsByType<Level2InfoSign>(FindObjectsSortMode.None).Select(s => (s.name, SignBounds(s))));

        foreach (Stall s in stalls)
        {
            foreach (var (name, b) in blockers)
                if (Intersects(s.bounds, b)) findings.Add($"[PUESTO TAPA] {s.Name} se solapa con {name}");

            if (!s.elevated)
            {
                foreach (SpriteRenderer house in houses)
                    if (Intersects(s.bounds, house.bounds)) findings.Add($"[PUESTO SOBRE CASA] {s.Name} se empalma con la casa interactiva {house.name}");

                foreach (VillagerAI v in Object.FindObjectsByType<VillagerAI>(FindObjectsSortMode.None))
                {
                    float x = v.transform.position.x;
                    if (Mathf.Abs(v.transform.position.y - 0.17f) < 1f && x + 0.5f > s.bounds.min.x && x - 0.5f < s.bounds.max.x)
                        findings.Add($"[PUESTO SOBRE NPC] {s.Name} ocupa el punto de inicio de {v.name} (x={x:F2})");
                }
            }

            foreach (Level2Chest chest in Object.FindObjectsByType<Level2Chest>(FindObjectsSortMode.None))
            {
                SpriteRenderer cr = chest.GetComponent<SpriteRenderer>();
                foreach (SpriteRenderer r in s.renderers)
                {
                    if (r.sortingLayerID != cr.sortingLayerID || r.sortingOrder < cr.sortingOrder) continue;
                    if (OverlapDepth(r.bounds, cr.bounds) > 0.1f) { findings.Add($"[PUESTO TAPA COFRE] {r.name} de {s.Name} tapa {chest.name}"); break; }
                }
            }
        }
    }

    private static List<SpriteRenderer> InteractiveHouses(List<HouseDoor> extDoors)
    {
        Transform ext = GameObject.Find("Environment/Village_Exterior_Structures").transform;
        return ext.Cast<Transform>().Where(t => t.name.StartsWith("House"))
            .Select(t => t.GetComponent<SpriteRenderer>()).Where(r => r != null)
            .Where(r => extDoors.Any(d => d.transform.position.y < 3f && r.bounds.min.x < d.transform.position.x && d.transform.position.x < r.bounds.max.x))
            .ToList();
    }

    // ------------------------------------------------------------------ Suelo

    private static void CheckGrounding(List<Stall> stalls, List<string> findings)
    {
        foreach (Stall s in stalls)
        {
            SpriteRenderer posts = s.root.Find(s.elevated ? "Elevated_CanopyPosts" : "PostsAndCounter")?.GetComponent<SpriteRenderer>();
            if (posts == null) continue;
            if (s.elevated)
            {
                Collider2D platform = s.root.GetComponent<Collider2D>();
                if (platform != null && Level2GroundingTool.TryOpaqueBottom(posts, out float b) && Mathf.Abs(b - platform.bounds.max.y) > Tolerance)
                    findings.Add($"[POSTE FLOTANDO/HUNDIDO] {s.Name}: postes en y={b:F3}, plataforma y={platform.bounds.max.y:F3}");
            }
            else CheckBase($"postes de {s.Name}", posts, s.root, findings);

            SpriteRenderer lantern = s.root.Find("Hanging_Lantern_Visual")?.GetComponent<SpriteRenderer>();
            if (lantern != null && lantern.bounds.max.y < posts.bounds.min.y) findings.Add($"[FAROL SUELTO] {s.Name}: el farol no está sobre el mostrador");
        }

        foreach (Transform zone in GameObject.Find("Environment").transform)
        {
            if (!zone.name.StartsWith("Zone_")) continue;
            foreach (Transform t in zone)
                if (PropPrefixes.Any(p => t.name.StartsWith(p)) && t.GetComponent<SpriteRenderer>() != null)
                    CheckBase($"{zone.name}/{t.name} (x={t.position.x:F1})", t.GetComponent<SpriteRenderer>(), t, findings);
            Transform sign = zone.Find("Sign_Z1_ChestsStreet");
            if (sign != null) CheckBase(sign.name, sign.GetComponent<SpriteRenderer>(), sign, findings);
        }

        foreach (Level2Chest chest in Object.FindObjectsByType<Level2Chest>(FindObjectsSortMode.None))
            CheckBase(chest.name, chest.GetComponent<SpriteRenderer>(), chest.transform, findings);

        foreach (LadderInteractable ladder in Object.FindObjectsByType<LadderInteractable>(FindObjectsSortMode.None)
                     .Where(l => l.transform.parent != null && l.transform.parent.name == "Village_Exterior_Structures"))
        {
            SerializedObject so = new SerializedObject(ladder);
            float top = so.FindProperty("topY").floatValue;
            if (!Level2GroundingTool.TryFindSurface(ladder.transform.position.x, top, ladder.transform, out float surface)) continue;
            if (Mathf.Abs(top - surface) > Tolerance) findings.Add($"[ESCALERA DESCONECTADA] {ladder.name}: tope y={top:F3}, piso y={surface:F3}");
        }
    }

    private static void CheckBase(string label, SpriteRenderer sr, Transform self, List<string> findings)
    {
        if (sr == null || !Level2GroundingTool.TryOpaqueBottom(sr, out float bottom)) return;
        if (!Level2GroundingTool.TryFindSurface(sr.bounds.center.x, bottom, self, out float surface)) return;
        float gap = bottom - surface;
        if (Mathf.Abs(gap) > Tolerance) findings.Add($"[{(gap > 0 ? "FLOTA" : "HUNDIDO")}] {label}: base y={bottom:F3}, superficie y={surface:F3} ({gap:+0.000;-0.000} u)");
    }

    // ------------------------------------------------------------------ Puertas

    private static void CheckDoors(List<string> findings, StringBuilder table)
    {
        table.AppendLine("Puertas:");
        Transform ext = GameObject.Find("Environment/Village_Exterior_Structures").transform;
        List<SpriteRenderer> facades = ext.Cast<Transform>().Where(t => t.name.StartsWith("House"))
            .Select(t => t.GetComponent<SpriteRenderer>()).Where(r => r != null).ToList();

        foreach (HouseDoor door in Object.FindObjectsByType<HouseDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(d => d.transform.position.x))
        {
            SpriteRenderer sr = door.GetComponent<SpriteRenderer>();
            float h = sr.bounds.size.y;
            if (h < 1.8f - Tolerance || h > 2.2f + Tolerance) findings.Add($"[PUERTA DESPROPORCIONADA] {door.name}: alto {h:F2} u (esperado 1.8–2.2 u)");

            Level2GroundingTool.TryOpaqueBottom(sr, out float bottom);
            if (Mathf.Abs(bottom - door.FloorSurfaceY) > Tolerance) findings.Add($"[PUERTA FLOTANDO/HUNDIDA] {door.name}: base y={bottom:F3}, piso y={door.FloorSurfaceY:F3}");
            if (Level2GroundingTool.TryFindSurface(sr.bounds.center.x, bottom, door.transform, out float surface) && Mathf.Abs(surface - door.FloorSurfaceY) > Tolerance)
                findings.Add($"[PUERTA SIN SUELO] {door.name}: floorSurfaceY={door.FloorSurfaceY:F3} pero la superficie real es y={surface:F3}");

            BoxCollider2D trigger = door.GetComponent<BoxCollider2D>();
            if (trigger != null && (Mathf.Abs(trigger.bounds.center.x - sr.bounds.center.x) > 0.1f || trigger.bounds.min.y > bottom + 0.1f))
                findings.Add($"[PUERTA TRIGGER DESPLAZADO] {door.name}: el trigger no coincide con la puerta visible");

            string house = "-";
            if (door.OwningInterior == null)
            {
                SpriteRenderer facade = facades.Where(f => f.bounds.min.x <= sr.bounds.center.x && sr.bounds.center.x <= f.bounds.max.x)
                    .OrderBy(f => f.sortingOrder < -3 ? 1 : 0).FirstOrDefault();
                if (facade == null) findings.Add($"[PUERTA SIN CASA] {door.name} no está sobre ninguna fachada");
                else
                {
                    house = facade.name;
                    if (sr.bounds.min.x < facade.bounds.min.x || sr.bounds.max.x > facade.bounds.max.x || sr.bounds.max.y > facade.bounds.max.y)
                        findings.Add($"[PUERTA SOBRESALE] {door.name} sobresale de {facade.name}");
                }
            }
            table.AppendLine($"   {door.name,-28} alto {h:F2} ancho {sr.bounds.size.x:F2} base {bottom:F3} x={sr.bounds.center.x:F2} casa {house}");
        }
    }

    // ------------------------------------------------------------------ Animación

    private static void CheckAnimation(List<Stall> stalls, List<string> findings)
    {
        List<(string name, float phase)> phases = new List<(string, float)>();
        foreach (Stall s in stalls)
        {
            MarketStallAnimator anim = s.root.GetComponent<MarketStallAnimator>();
            if (anim == null || anim.AwningVisual == null) { findings.Add($"[SIN ANIMACIÓN] {s.Name} no balancea su toldo"); continue; }
            if (anim.HangingItems == null || anim.HangingItems.Length == 0 || anim.HangingItems.Any(h => h == null))
                findings.Add($"[SIN ANIMACIÓN] {s.Name}: colgantes sin asignar");
            if (anim.SpeedMultiplier < 0.89f || anim.SpeedMultiplier > 1.11f)
                findings.Add($"[ANIMACIÓN] {s.Name}: velocidad ×{anim.SpeedMultiplier:F2} fuera de ±10 %");
            phases.Add((s.Name, Mathf.Repeat(anim.PhaseOffset, 2f * Mathf.PI)));
        }
        for (int i = 0; i < phases.Count; i++)
            for (int j = i + 1; j < phases.Count; j++)
            {
                float d = Mathf.Abs(phases[i].phase - phases[j].phase);
                d = Mathf.Min(d, 2f * Mathf.PI - d);
                if (d < 0.1f) findings.Add($"[ANIMACIÓN SINCRONIZADA] {phases[i].name} y {phases[j].name} se mueven al unísono");
            }
    }

    // ------------------------------------------------------------------ Textos

    private static void CheckTexts(List<string> findings)
    {
        List<(string where, string text)> texts = new List<(string, string)>();

        TutorialTrigger tut = Object.FindObjectsByType<TutorialTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(t => t.TipId == "tip_ingredientes_mercado");
        if (tut == null) findings.Add("[TEXTO] Falta el aviso inicial de cofres");
        else
        {
            SerializedObject so = new SerializedObject(tut);
            string title = so.FindProperty("tipTitle").stringValue, body = so.FindProperty("tipBody").stringValue;
            texts.Add(("aviso inicial", title + " " + body));
            if (title != "Busca en los cofres" || !body.Contains("cofres") || !body.Contains("pulsa Q") || !body.Contains("forma humana"))
                findings.Add("[TEXTO] El aviso inicial no explica cofres, tecla Q y forma humana");
            if (tut.transform.position.x > 8f) findings.Add($"[TEXTO] El aviso inicial está lejos del spawn (x={tut.transform.position.x:F1})");
        }

        if (Object.FindFirstObjectByType<Level2ChestGuideHUD>() == null) findings.Add("[TEXTO] Falta la guía de cofres en el HUD propio");
        texts.Add(("HUD", Level2ChestGuideHUD.GuideText));
        texts.Add(("HUD", Level2ChestGuideHUD.EmptyChestMessage));

        Level2InfoSign[] signs = Object.FindObjectsByType<Level2InfoSign>(FindObjectsSortMode.None);
        texts.AddRange(signs.Select(s => (s.name, s.Message)));
        if (!signs.Any(s => s.Message.Contains("cofres de la calle")))
            findings.Add("[TEXTO] No hay letrero en la calle que diga que los cofres de la calle guardan objetos");

        // Casa con cofre ⇔ letrero junto a su puerta de calle
        List<HouseDoor> doors = Object.FindObjectsByType<HouseDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
        foreach (HouseDoor ext in doors.Where(d => d.OwningInterior == null && d.FloorSurfaceY < 1f))
        {
            // Puertas selladas (cuarentena, santuario) no llevan a una casa explorable
            SerializedObject so = new SerializedObject(ext);
            bool sealedDoor = so.FindProperty("permanentlyBarred").boolValue || so.FindProperty("requiresAllIngredients").boolValue;
            InteriorZone interior = ext.TargetDoor != null && !sealedDoor ? ext.TargetDoor.OwningInterior : null;
            bool hasChest = interior != null && interior.GetComponentsInChildren<Level2Chest>(true).Length > 0;
            bool hasSign = signs.Any(s => s.Message.Contains("cofre") && !s.Message.Contains("calle") && Vector2.Distance(s.transform.position, ext.transform.position) <= SignDoorRadius);
            if (hasChest && !hasSign) findings.Add($"[TEXTO] {ext.name} lleva a una casa con cofre y no tiene letrero");
            if (!hasChest && hasSign) findings.Add($"[TEXTO] {ext.name} tiene letrero de cofre pero su casa no guarda ninguno");
        }

        IngredientGate gate = Object.FindFirstObjectByType<IngredientGate>(FindObjectsInactive.Include);
        if (gate != null)
        {
            string locked = new SerializedObject(gate).FindProperty("lockedMessage").stringValue;
            texts.Add(("salida", locked));
            if (!locked.Contains("cofres de las casas y de la calle")) findings.Add("[TEXTO] El aviso de salida no recuerda buscar en los cofres");
        }
        Level2Goal goal = Object.FindFirstObjectByType<Level2Goal>(FindObjectsInactive.Include);
        if (goal != null)
        {
            string missing = new SerializedObject(goal).FindProperty("missingIngredientsMessage").stringValue;
            texts.Add(("meta", missing));
            if (!missing.Contains("cofres de las casas y de la calle")) findings.Add("[TEXTO] El aviso de la meta sin ingredientes no recuerda buscar en los cofres");
        }

        foreach (HouseDoor d in doors) texts.Add((d.name, d.GetPromptText(null)));
        GameObject witch = GameObject.Find("Gameplay/Witch");
        foreach (Level2Chest c in Object.FindObjectsByType<Level2Chest>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string prompt = c.GetPromptText(witch);
            texts.Add((c.name, prompt));
            if (!c.HasBeenOpened && !c.RequiresKey && prompt != "Q: Abrir cofre") findings.Add($"[TEXTO] {c.name} no muestra 'Q: Abrir cofre' (muestra '{prompt}')");
            if (c.GetComponent<Level2DoorIndicator>() == null) findings.Add($"[TEXTO] {c.name} no tiene indicador al acercarse");
        }
        texts.Add(("controles", Level2Controls.ControlsGuideBanner));
        texts.Add(("controles", Level2Controls.PotionUsageText));
        texts.Add(("gato", Level2Controls.CatChestBlockedMessage));

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        foreach (var (where, text) in texts.Where(t => !string.IsNullOrEmpty(t.text)))
        {
            if (EToInteract.IsMatch(text)) findings.Add($"[TECLA E] '{text}' ({where}) pide E para abrir o interactuar");
            if (font != null && !font.HasCharacters(text, out uint[] missing, true, false))
                findings.Add($"[GLIFOS] '{text}' ({where}) sin glifos para: {new string(missing.Distinct().Select(c => (char)c).ToArray())}");
        }
    }

    // ------------------------------------------------------------------ Geometría

    private static bool RangesOverlap(float a0, float a1, float b0, float b1, float margin) => a0 < b1 + margin && b0 < a1 + margin;

    private static bool Intersects(Bounds a, Bounds b) => OverlapDepth(a, b) > 0.001f;

    private static float OverlapDepth(Bounds a, Bounds b)
    {
        float dx = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
        float dy = Mathf.Min(a.max.y, b.max.y) - Mathf.Max(a.min.y, b.min.y);
        return Mathf.Min(dx, dy);
    }

    private static Bounds Shrink(Bounds b, float m) { b.Expand(-2f * m); return b; }

    private static Bounds Encapsulate(SpriteRenderer[] rs)
    {
        Bounds b = rs[0].bounds;
        foreach (SpriteRenderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    private static Bounds SignBounds(Level2InfoSign s) => new Bounds(s.transform.position, new Vector3(s.PanelSize.x, s.PanelSize.y, 0f));
}
