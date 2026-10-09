#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// TEMPORAL (no se versiona): recorre la escena Test_Bruja en modo Play con entrada programática,
/// comprueba caminar, saltar, colisiones, vuelo y estados del guardia, guarda capturas y resultados.
/// </summary>
public class SmokeTestRunner : MonoBehaviour
{
    private const string FlagPath = "Temp/run_smoke.flag";
    private const string ResultsPath = "Temp/smoke_results.txt";
    private const string ShotsDir = "C:/Users/brand/.gemini/antigravity/brain/4516ea65-716b-4682-bd15-bffa27cc1a5c/scratch/shots/";

    private PlayerController pc;
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private EnemySight sight;
    private MeshRenderer cone;
    private Camera cam;
    private readonly StringBuilder log = new StringBuilder();
    private int failures;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (!File.Exists(FlagPath)) return;
        File.Delete(FlagPath);
        new GameObject("SmokeTestRunner").AddComponent<SmokeTestRunner>();
    }

    private void Check(string name, bool ok, string detail = "")
    {
        if (!ok) failures++;
        log.AppendLine($"[{(ok ? "OK " : "FALLA")}] {name} {detail}");
    }

    private string State()
    {
        foreach (string n in new[] { "Idle", "Walk", "Jump", "Fly" })
            if (anim.GetCurrentAnimatorStateInfo(0).IsName(n)) return n;
        return "?";
    }

    private void Teleport(Vector2 p)
    {
        rb.position = p;
        rb.linearVelocity = Vector2.zero;
        pc.transform.position = p;
        Physics2D.SyncTransforms();
    }

    private IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        Directory.CreateDirectory(ShotsDir);
        Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(ShotsDir + name + ".png", tex.EncodeToPNG());
        Destroy(tex);
    }

    private Color ConeColor() => cone.sharedMaterial.color;

    private IEnumerator Start()
    {
        yield return StartCoroutine(Run());
        log.AppendLine(failures == 0 ? "RESULTADO: TODO OK" : $"RESULTADO: {failures} FALLAS");
        File.WriteAllText(ResultsPath, log.ToString());
        UnityEditor.EditorApplication.Exit(failures == 0 ? 0 : 1);
    }

    private IEnumerator Run()
    {
        pc = FindFirstObjectByType<PlayerController>();
        rb = pc.GetComponent<Rigidbody2D>();
        anim = pc.GetComponentInChildren<Animator>();
        sr = pc.GetComponentInChildren<SpriteRenderer>();
        sight = FindFirstObjectByType<EnemySight>();
        cone = sight.transform.Find("ViewCone").GetComponent<MeshRenderer>();
        cam = Camera.main;
        pc.EnableScriptedInput(true);

        int flightEvents = 0; bool lastFlight = false; float lastEnergy = 1f;
        pc.OnFlightChanged += f => { flightEvents++; lastFlight = f; };
        pc.OnFlightEnergyChanged += e => lastEnergy = e;
        var states = new System.Collections.Generic.List<AlertState>();
        sight.OnAlertStateChanged += s => states.Add(s);
        bool captured = false;
        sight.OnTargetCaptured += () => captured = true;

        // --- Ajustes del proyecto
        Check("Tag Player", pc.CompareTag("Player"));
        Check("Layer Player", LayerMask.LayerToName(pc.gameObject.layer) == "Player");
        Check("RB Continuous+Interpolate", rb.collisionDetectionMode == CollisionDetectionMode2D.Continuous && rb.interpolation == RigidbodyInterpolation2D.Interpolate);
        Check("Collider sin friccion", pc.GetComponent<BoxCollider2D>().sharedMaterial != null && pc.GetComponent<BoxCollider2D>().sharedMaterial.friction == 0f);
        Check("CanFly activo en escena de prueba", pc.CanFly);
        Check("IDetectable", ((IDetectable)pc).IsDetectable && ((IDetectable)pc).DetectionPoint != null);
        Check("Sprite PPU 64", sr.sprite != null && Mathf.Approximately(sr.sprite.pixelsPerUnit, 64f), sr.sprite ? sr.sprite.name : "null");

        // --- Reposo / suelo
        yield return new WaitForSeconds(1.2f);
        Check("En suelo al inicio", pc.IsGrounded, $"y={rb.position.y:F3}");
        Check("Idle al inicio", State() == "Idle", State());
        yield return Shot("01_idle");

        // --- Caminar
        float x0 = rb.position.x;
        pc.SetScriptedInput(1f, 0f);
        yield return new WaitForSeconds(1.0f);
        Check("Camina a la derecha", rb.position.x - x0 > 3f, $"dx={rb.position.x - x0:F2}");
        Check("Animator Walk", State() == "Walk" && sr.sprite.name.StartsWith("Witch_Walk"), State() + " " + sr.sprite.name);
        Check("Sin flip al ir a la derecha", !sr.flipX);
        yield return Shot("02_walk");
        pc.SetScriptedInput(-1f, 0f);
        yield return new WaitForSeconds(0.5f);
        Check("Flip al ir a la izquierda", sr.flipX);
        pc.SetScriptedInput(0f, 0f);
        yield return new WaitForSeconds(1.0f);
        Check("Desacelera y vuelve a Idle", Mathf.Abs(rb.linearVelocity.x) < 0.05f && State() == "Idle", $"vx={rb.linearVelocity.x:F3} {State()}");

        // --- Salto completo (mantener) y corto (tap)
        Teleport(new Vector2(-14f, 0.05f));
        yield return new WaitForSeconds(0.8f);
        float groundY = rb.position.y;
        pc.ScriptedJumpPressed();
        float apexHold = groundY; bool sawJumpState = false; bool wasAirborne = false;
        for (float t = 0; t < 1.5f; t += Time.deltaTime)
        {
            yield return null;
            apexHold = Mathf.Max(apexHold, rb.position.y);
            if (State() == "Jump") sawJumpState = true;
            if (!pc.IsGrounded) wasAirborne = true;
        }
        pc.ScriptedJumpReleased();
        yield return new WaitForSeconds(0.5f);
        Check("Salto completo ~2.8u", apexHold - groundY > 2.4f && apexHold - groundY < 3.3f, $"altura={apexHold - groundY:F2}");
        Check("Animator Jump durante el salto", sawJumpState);
        Check("Vuelve a suelo", pc.IsGrounded && wasAirborne);

        groundY = rb.position.y;
        pc.ScriptedJumpPressed();
        yield return new WaitForSeconds(0.08f);
        pc.ScriptedJumpReleased();
        float apexTap = groundY;
        for (float t = 0; t < 1f; t += Time.deltaTime) { yield return null; apexTap = Mathf.Max(apexTap, rb.position.y); }
        Check("Salto corto < salto completo (altura variable)", apexTap - groundY < (apexHold - groundY) * 0.7f, $"corto={apexTap - groundY:F2}");

        // --- Sin saltos infinitos: pulsar salto 2 veces en el aire no sube mas
        yield return new WaitForSeconds(0.6f);
        pc.ScriptedJumpPressed(); yield return new WaitForSeconds(0.05f); pc.ScriptedJumpReleased();
        yield return new WaitForSeconds(0.25f);
        float yBefore = rb.position.y;
        pc.ScriptedJumpPressed(); // en el aire: no debe impulsar
        float vyAfter = rb.linearVelocity.y;
        yield return new WaitForSeconds(0.3f);
        pc.ScriptedJumpReleased();
        Check("Sin doble salto / salto infinito", rb.linearVelocity.y < vyAfter + 0.01f && !pc.IsGrounded || rb.position.y < yBefore + 1.0f, $"y={rb.position.y:F2} antes={yBefore:F2}");
        yield return new WaitForSeconds(1.2f);

        // --- Coyote time y buffer
        Teleport(new Vector2(-16.5f, 0.05f)); // cerca del borde izquierdo del suelo (x=-20)
        yield return new WaitForSeconds(0.5f);

        // --- Plataforma baja: no atraviesa (debajo) y se puede subir (desde el costado)
        Teleport(new Vector2(-6f, 0.05f)); // justo debajo de Platform_Low (borde inferior 1.8u)
        yield return new WaitForSeconds(0.4f);
        float peakUnder = 0f;
        pc.ScriptedJumpPressed();
        for (float t = 0; t < 0.8f; t += Time.deltaTime) { yield return null; peakUnder = Mathf.Max(peakUnder, rb.position.y); }
        pc.ScriptedJumpReleased();
        Check("No atraviesa plataforma desde abajo", peakUnder + 1.6f < 2.0f + 0.05f, $"pies max={peakUnder:F2} (cabeza={peakUnder + 1.6f:F2}, plataforma abajo=2.0)");
        yield return new WaitForSeconds(0.8f);

        Teleport(new Vector2(-10.5f, 0.05f));
        yield return new WaitForSeconds(0.4f);
        pc.SetScriptedInput(1f, 0f);
        pc.ScriptedJumpPressed();
        yield return new WaitForSeconds(0.9f);
        pc.ScriptedJumpReleased();
        pc.SetScriptedInput(0f, 0f);
        yield return new WaitForSeconds(0.6f);
        Check("Sube a Platform_Low sin atravesarla", pc.IsGrounded && Mathf.Abs(rb.position.y - 2.2f) < 0.1f && rb.position.x > -8f && rb.position.x < -4f, $"pos=({rb.position.x:F2},{rb.position.y:F2})");
        yield return Shot("03_plataforma");

        // --- Caida desde altura: no tunelea el suelo
        Teleport(new Vector2(-14f, 25f));
        float minY = 99f;
        for (float t = 0; t < 3f; t += Time.deltaTime) { yield return null; minY = Mathf.Min(minY, rb.position.y); }
        Check("Caida larga no atraviesa el suelo", minY > -0.05f && Mathf.Abs(rb.position.y - 0f) < 0.1f, $"minY={minY:F3} y={rb.position.y:F3}");

        // --- Vuelo con gravedad cero
        Teleport(new Vector2(-14f, 0.05f));
        yield return new WaitForSeconds(0.5f);
        pc.ToggleFlight();
        yield return new WaitForSeconds(0.2f);
        Check("Vuelo activo, gravityScale = 0", pc.IsFlying && rb.gravityScale == 0f, $"gs={rb.gravityScale}");
        Check("Evento OnFlightChanged(true)", flightEvents == 1 && lastFlight);
        pc.SetScriptedInput(0f, 1f);
        yield return new WaitForSeconds(0.8f);
        Check("Sube con eje Vertical", rb.position.y > 2.5f, $"y={rb.position.y:F2}");
        Check("Animator Fly", State() == "Fly", State());
        yield return Shot("04_fly");
        pc.SetScriptedInput(0f, 0f);
        yield return new WaitForSeconds(0.4f);
        float yHover = rb.position.y;
        yield return new WaitForSeconds(0.6f);
        Check("Flota sin caer (gravedad cero)", Mathf.Abs(rb.position.y - yHover) < 0.1f, $"dy={rb.position.y - yHover:F3}");
        Check("Energia se consume", lastEnergy < 1f && lastEnergy > 0f, $"energia={lastEnergy:F2}");
        float eBefore = lastEnergy;
        pc.ToggleFlight();
        Check("Segunda F: vuelo off + gravedad restaurada", !pc.IsFlying && Mathf.Approximately(rb.gravityScale, 3f) && flightEvents == 2 && !lastFlight, $"gs={rb.gravityScale}");
        yield return new WaitForSeconds(0.3f);
        Check("Cae tras volar (Jump)", State() == "Jump" || pc.IsGrounded, State());
        yield return new WaitForSeconds(2.0f);
        Check("Aterriza y recarga energia", pc.IsGrounded && lastEnergy > eBefore, $"energia={lastEnergy:F2}");

        // Agotar energia
        yield return new WaitForSeconds(2.5f);
        pc.ToggleFlight();
        pc.SetScriptedInput(0f, 0.3f);
        yield return new WaitForSeconds(5.0f);
        Check("Energia agotada termina el vuelo y restaura gravedad", !pc.IsFlying && Mathf.Approximately(rb.gravityScale, 3f) && lastEnergy <= 0.001f, $"energia={lastEnergy:F3} gs={rb.gravityScale}");
        pc.SetScriptedInput(0f, 0f);
        yield return new WaitForSeconds(2.5f);

        // CanFly = false impide volar
        pc.CanFly = false; pc.ToggleFlight();
        Check("CanFly=false impide volar", !pc.IsFlying);
        pc.CanFly = true;

        // --- Guardia
        sight.ResetToCalm();
        states.Clear();
        Teleport(new Vector2(-14f, 0.05f));
        yield return new WaitForSeconds(1.0f);
        Color calm = ConeColor();
        Check("Cono en Calm, amarillo calido, alfa >= .35", sight.CurrentState == AlertState.Calm && calm.r > 0.9f && calm.g > 0.75f && calm.b < 0.5f && calm.a >= 0.35f, calm.ToString());
        yield return Shot("05_calm");

        Teleport(new Vector2(8f, 0.05f));
        yield return new WaitForSeconds(0.6f);
        Color susp = ConeColor();
        Check("Entra al cono: Suspicion y cono naranja", sight.CurrentState == AlertState.Suspicion && susp.g > 0.4f && susp.g < 0.7f && susp.a >= 0.35f, $"{sight.CurrentState} {susp}");
        yield return Shot("06_suspicion");
        yield return new WaitForSeconds(1.2f);
        Color alert = ConeColor();
        Check("Sigue visible: Alert y cono rojo", sight.CurrentState == AlertState.Alert && alert.g < 0.25f && alert.a >= 0.35f, $"{sight.CurrentState} {alert}");
        yield return Shot("07_alert");

        // Capture
        Teleport(new Vector2(11f, 0.05f));
        yield return new WaitForSeconds(0.5f);
        Check("Distancia < captureDistance: Capture + evento", sight.CurrentState == AlertState.Capture && captured, $"{sight.CurrentState} capt={captured}");
        string order = string.Join(">", states);
        Check("Orden de estados Suspicion>Alert>Capture", order == "Suspicion>Alert>Capture", order);

        // Perder objetivo -> Calm
        sight.ResetToCalm(); states.Clear();
        Teleport(new Vector2(8f, 0.05f));
        yield return new WaitForSeconds(0.5f);
        Teleport(new Vector2(-14f, 0.05f));
        yield return new WaitForSeconds(0.5f);
        Check("Pierde al objetivo, aun no vuelve a Calm", sight.CurrentState != AlertState.Calm, sight.CurrentState.ToString());
        yield return new WaitForSeconds(2.3f);
        Check("Tras lostTargetDelay vuelve a Calm", sight.CurrentState == AlertState.Calm, sight.CurrentState.ToString());

        // IsDetectable = false -> no ve
        sight.ResetToCalm();
        Teleport(new Vector2(8f, 0.05f));
        pc.SetDetectable(false);
        yield return new WaitForSeconds(0.8f);
        Check("IsDetectable=false: el guardia no la ve", sight.CurrentState == AlertState.Calm && !sight.IsTargetVisible, sight.CurrentState.ToString());
        pc.SetDetectable(true);

        // Pared bloquea la vista
        GameObject wall = new GameObject("TempWall");
        wall.layer = LayerMask.NameToLayer("Ground");
        wall.transform.position = new Vector3(10f, 2f, 0f);
        wall.transform.localScale = new Vector3(0.5f, 6f, 1f);
        wall.AddComponent<BoxCollider2D>();
        Physics2D.SyncTransforms();
        sight.ResetToCalm();
        Teleport(new Vector2(8f, 0.05f));
        yield return new WaitForSeconds(0.8f);
        Check("Pared entre guardia y bruja: no ve a traves", sight.CurrentState == AlertState.Calm && !sight.IsTargetVisible, sight.CurrentState.ToString());
        Destroy(wall);
        yield return new WaitForSeconds(0.3f);
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(0.8f);
        Check("Sin pared vuelve a verla", sight.IsTargetVisible, sight.CurrentState.ToString());

        // --- Camara
        Teleport(new Vector2(-14f, 0.05f));
        yield return new WaitForSeconds(2f);
        Check("Camara ortografica sigue a la bruja", cam.orthographic && Mathf.Abs(cam.transform.position.x - rb.position.x) < 1.5f, $"cam={cam.transform.position} witch={rb.position}");
        Check("Layer collision Player-Enemy ignorada", Physics2D.GetIgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy")));
    }
}
#endif
