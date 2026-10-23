using UnityEngine;

/// <summary>
/// Animación procedural del rig por piezas de la bruja (personalidad: curiosa y astuta).
/// Capas combinadas con pesos suavizados: idle, caminar/correr (fase según distancia recorrida para que
/// los pies no patinen), salto (anticipación, subida, caída, aterrizaje), vuelo, lanzar hechizo, daño y muerte.
/// Encima: física secundaria con resortes (coleta, bufanda, capucha, falda), parpadeo aleatorio y apoyo
/// de pies en el suelo. Documentación: Docs/Arte/Direccion_de_Arte.md.
/// </summary>
[DisallowMultipleComponent]
public class WitchRigAnimator : MonoBehaviour
{
    private const float Tau = Mathf.PI * 2f;

    [Header("Fuente de movimiento (se buscan en los padres si están vacías)")]
    [SerializeField] private PlayerController player;
    [SerializeField] private Rigidbody2D body;

    [Header("Escala")]
    [Tooltip("Píxeles por unidad de las piezas (del JSON del rig).")]
    [SerializeField] private float pixelsPerUnit = 650f;
    [Tooltip("Escala uniforme del rig respecto a su padre.")]
    [SerializeField] private float visualScale = 1f;

    [Header("Locomoción")]
    [SerializeField] private float walkSpeed = 2.4f;
    [SerializeField] private float runSpeed = 6f;
    [Tooltip("Distancia recorrida por ciclo completo (2 pasos) al caminar (u).")]
    [SerializeField] private float walkStride = 1.15f;
    [Tooltip("Distancia recorrida por ciclo completo (2 pasos) al correr (u).")]
    [SerializeField] private float runStride = 2.4f;

    [Header("Variación por instancia")]
    [Tooltip("Variación aleatoria del tempo de idle/respiración (±).")]
    [SerializeField] private float tempoJitter = 0.06f;

    [Header("Caras (asignadas por el constructor)")]
    [SerializeField] private Sprite eyeOpen;
    [SerializeField] private Sprite eyeHalf;
    [SerializeField] private Sprite eyeClosed;
    [SerializeField] private Sprite mouthSmile;
    [SerializeField] private Sprite mouthO;

    [Header("Efectos")]
    [SerializeField] private bool landingDust = true;
    [SerializeField] private Color dustColor = new Color(0.62f, 0.52f, 0.40f, 0.8f);

    [Header("Modo demo (escena de prueba)")]
    [Tooltip("Ignora al PlayerController y usa los valores de abajo.")]
    public bool demoMode;
    public Vector2 demoVelocity;
    public bool demoGrounded = true;
    public bool demoFlying;
    public bool demoFacingRight = true;

    private enum B
    {
        Root, Hips, Torso, Neck, Head, HoodInner, Hood, HairTail, Eye, Mouth, Scarf, ScarfTail, Skirt,
        ArmUF, ArmLF, ArmUB, ArmLB, LegUF, LegLF, LegUB, LegLB, Count
    }

    private static readonly string[] BoneNames =
    {
        "root", "hips", "torso", "neck", "head", "hood_inner", "hood", "hair_tail", "eye", "mouth", "scarf",
        "scarf_tail", "robe_skirt", "arm_upper_front", "arm_lower_front", "arm_upper_back", "arm_lower_back",
        "leg_upper_front", "leg_lower_front", "leg_upper_back", "leg_lower_back"
    };

    private struct P
    {
        public float rot, dx, dy, sx, sy; // sx/sy son desviaciones respecto a 1
    }

    private struct Spring
    {
        public float x, v;

        public float Step(float target, float dt, float freq, float damping)
        {
            // Subpasos para estabilidad con frames largos
            int n = Mathf.Max(1, Mathf.CeilToInt(dt / 0.008f));
            float h = dt / n;
            float w = Tau * freq;
            for (int i = 0; i < n; i++)
            {
                float a = w * w * (target - x) - 2f * damping * w * v;
                v += a * h;
                x += v * h;
            }
            return x;
        }
    }

    private Transform[] bones;
    private Vector3[] restPos;
    private float[] restRot;
    private Vector3[] restScale;
    private float[] parentScale;
    private readonly P[] pose = new P[(int)B.Count];
    private Transform tipFront, tipBack;
    private SpriteRenderer eyeRenderer, mouthRenderer;
    private bool ready;

    // Estado
    private float time, phase, tempo = 1f;
    private float moveBlend, runBlend, airBlend, flyBlend, groundBlend = 1f;
    private bool wasGrounded = true;
    private float airTime, lastVy;
    private float landTimer = -1f, landStrength, takeoffTimer = -1f;
    private float castTimer = -1f, hurtTimer = -1f, dieTimer = -1f;
    private float nextBlink, blinkTimer = -1f;
    private Spring hair, scarf, hood, skirt;
    private float prevVx;
    private bool facingRight = true;
    private ParticleSystem dust;

    private const float CastDuration = 0.5f;
    private const float HurtDuration = 0.45f;
    private const float LandDuration = 0.3f;
    private const float TakeoffDuration = 0.1f;

    /// <summary>Orientación del rig (la escala X se invierte al mirar a la izquierda).</summary>
    public bool FacingRight
    {
        get => facingRight;
        set => facingRight = value;
    }

    public bool IsDying => dieTimer >= 0f;

    // ----------------------------------------------------------------- Configuración

    public void Configure(float ppu, float scale)
    {
        pixelsPerUnit = ppu;
        visualScale = scale;
    }

    public void SetFaceSprites(Sprite open, Sprite half, Sprite closed, Sprite smile, Sprite o)
    {
        eyeOpen = open;
        eyeHalf = half;
        eyeClosed = closed;
        mouthSmile = smile;
        mouthO = o;
    }

    /// <summary>Inicializa el rig fuera de Play (herramientas de editor).</summary>
    public void EnsureInitialized()
    {
        if (!ready) Awake();
    }

    /// <summary>Activa o desactiva el polvo de aterrizaje (las capturas de QA lo desactivan).</summary>
    public bool LandingDust
    {
        get => landingDust;
        set => landingDust = value;
    }

    private void Awake()
    {
        if (player == null) player = GetComponentInParent<PlayerController>();
        if (body == null) body = GetComponentInParent<Rigidbody2D>();
        CacheBones();
        tempo = 1f + Random.Range(-tempoJitter, tempoJitter);
        time = Random.Range(0f, 10f);
        nextBlink = time + Random.Range(1f, 3f);
    }

    private void CacheBones()
    {
        int n = (int)B.Count;
        bones = new Transform[n];
        restPos = new Vector3[n];
        restRot = new float[n];
        restScale = new Vector3[n];
        parentScale = new float[n];

        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < n; i++)
        {
            foreach (Transform t in all)
            {
                if (t.name == BoneNames[i]) { bones[i] = t; break; }
            }
            if (bones[i] == null)
            {
                Debug.LogWarning($"[WitchRigAnimator] Falta el hueso '{BoneNames[i]}'.");
                return;
            }
            restPos[i] = bones[i].localPosition;
            restRot[i] = bones[i].localEulerAngles.z;
            restScale[i] = bones[i].localScale;

            float s = 1f;
            for (Transform p = bones[i].parent; p != null && p != transform; p = p.parent) s *= p.localScale.x;
            parentScale[i] = s;
        }

        foreach (Transform t in all)
        {
            if (t.name == "leg_lower_front_tip") tipFront = t;
            else if (t.name == "leg_lower_back_tip") tipBack = t;
        }
        eyeRenderer = bones[(int)B.Eye].GetComponent<SpriteRenderer>();
        mouthRenderer = bones[(int)B.Mouth].GetComponent<SpriteRenderer>();
        ready = true;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHeartLost += TriggerHurt;
            GameManager.Instance.OnStateChanged += HandleStateChanged;
        }
        PotionInventory inv = GetComponentInParent<PotionInventory>();
        if (inv != null) inv.OnPotionThrown += TriggerCast;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHeartLost -= TriggerHurt;
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }
        PotionInventory inv = GetComponentInParent<PotionInventory>();
        if (inv != null) inv.OnPotionThrown -= TriggerCast;
    }

    private void HandleStateChanged(GameState state)
    {
        if (state == GameState.GameOver) TriggerDie();
        else if (state == GameState.Playing) ResetDeath();
    }

    // ----------------------------------------------------------------- Disparadores

    public void TriggerCast() => castTimer = 0f;
    public void TriggerHurt()
    {
        if (IsDying) return;
        hurtTimer = 0f;
        CameraFollow.Shake(0.22f, 0.3f);
    }
    public void TriggerDie() { if (!IsDying) dieTimer = 0f; }
    public void ResetDeath() { dieTimer = -1f; hurtTimer = -1f; }

    public void TriggerLand(float impactSpeed)
    {
        landTimer = 0f;
        landStrength = Mathf.Clamp01(impactSpeed / 14f) * 0.8f + 0.2f;
        // Caídas fuertes sacuden ligeramente la cámara
        if (impactSpeed > 11f) CameraFollow.Shake(0.06f + 0.06f * Mathf.InverseLerp(11f, 22f, impactSpeed), 0.18f);
        if (landingDust) EmitDust(Mathf.RoundToInt(Mathf.Lerp(4, 12, landStrength)));
    }

    // ----------------------------------------------------------------- Bucle

    private void LateUpdate()
    {
        // Game Over pone timeScale = 0: la animación de muerte usa tiempo sin escalar para poder verse
        Tick(IsDying ? Mathf.Min(Time.unscaledDeltaTime, 0.05f) : Time.deltaTime);
    }

    /// <summary>Avanza la animación dt segundos (lo usa LateUpdate y las herramientas de QA).</summary>
    public void Tick(float dt)
    {
        if (!ready || dt <= 0f) return;

        ReadInput(out float vx, out float vy, out bool grounded, out bool flying);
        time += dt * tempo;
        float vForward = vx * (facingRight ? 1f : -1f);

        // Eventos de contacto
        if (grounded && !wasGrounded && !flying && airTime > 0.12f) TriggerLand(-lastVy);
        if (!grounded && wasGrounded && vy > 0.5f) takeoffTimer = 0f;
        airTime = grounded ? 0f : airTime + dt;
        wasGrounded = grounded;
        lastVy = vy;

        // Pesos de capa (suavizados: nada cambia de golpe)
        float speed = Mathf.Abs(vx);
        moveBlend = Approach(moveBlend, grounded ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.1f, walkSpeed * 0.6f, speed)) : moveBlend, 10f, dt);
        runBlend = Approach(runBlend, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(walkSpeed, runSpeed, speed)), 6f, dt);
        airBlend = Approach(airBlend, !grounded && !flying ? 1f : 0f, 22f, dt);
        flyBlend = Approach(flyBlend, flying ? 1f : 0f, 7f, dt);
        if (grounded) phase += speed * dt / Mathf.Lerp(walkStride, runStride, runBlend) * Tau;

        System.Array.Clear(pose, 0, pose.Length);
        float ground = (1f - airBlend) * (1f - flyBlend);

        AddIdle((1f - moveBlend) * ground);
        AddLocomotion(moveBlend * ground);
        AddAir(airBlend * (1f - flyBlend), vy);
        AddFly(flyBlend);
        AddTakeoff(dt);
        AddLanding(dt);
        AddCast(dt);
        AddHurt(dt);
        AddSecondary(dt, vForward, vy);
        float dieWeight = AddDie(dt);

        Apply();

        groundBlend = Approach(groundBlend, ground * (1f - dieWeight), 18f, dt);
        GroundFeet(groundBlend);

        UpdateFace(dt);
        transform.localScale = new Vector3(visualScale * (facingRight ? 1f : -1f), visualScale, 1f);
        prevVx = vx;
    }

    private void ReadInput(out float vx, out float vy, out bool grounded, out bool flying)
    {
        if (demoMode || player == null)
        {
            vx = demoVelocity.x;
            vy = demoVelocity.y;
            grounded = demoGrounded;
            flying = demoFlying;
            if (demoMode) facingRight = demoFacingRight;
            return;
        }
        Vector2 v = body != null ? body.linearVelocity : Vector2.zero;
        vx = v.x;
        vy = v.y;
        grounded = player.IsGrounded;
        flying = player.IsFlying;
    }

    // ----------------------------------------------------------------- Capas

    private void AddIdle(float w)
    {
        if (w <= 0f) return;
        float t = time;
        float breath = Wave(t, 2.6f, 0f);
        float look = Wave(t, 5.2f, 0.6f);
        float peek = Ease((Wave(t, 7.8f, 0f) + 1f) * 0.5f); // se asoma con curiosidad

        float shift = Wave(t, 6.4f, 1.3f); // cambio de peso lento entre piernas

        Add(B.Hips, w, dy: 2.5f * breath, dx: 3f * shift);
        Add(B.Torso, w, rot: -1.5f * peek + 1.2f * shift, sx: -0.01f * breath, sy: 0.022f * breath);
        Add(B.Neck, w, dy: -3.5f * breath);
        Add(B.Head, w, rot: 3f * look - 3.5f * peek);
        Add(B.LegUF, w, rot: 1.5f * shift);
        Add(B.LegUB, w, rot: 1.5f * shift);
        Add(B.ArmUF, w, rot: 3.5f * Wave(t, 2.6f, -0.6f) + 3f * peek);
        Add(B.ArmLF, w, rot: 5f * Wave(t, 2.6f, -1.1f) + 6f * peek);
        Add(B.ArmUB, w, rot: 3f * Wave(t, 2.6f, -0.8f));
        Add(B.ArmLB, w, rot: 4.5f * Wave(t, 2.6f, -1.3f));
        Add(B.Skirt, w, rot: 1.2f * Wave(t, 2.6f, -0.9f) - 0.8f * shift);
    }

    private void AddLocomotion(float w)
    {
        if (w <= 0f) return;
        float r = runBlend;
        float s = Mathf.Sin(phase), c = Mathf.Cos(phase);
        float bob = Mathf.Cos(2f * phase);
        float contact = Mathf.Max(0f, -bob);

        float thighAmp = Mathf.Lerp(24f, 38f, r);
        float kneeMax = Mathf.Lerp(28f, 75f, r);
        float armAmp = Mathf.Lerp(18f, 34f, r);
        float elbow = Mathf.Lerp(12f, 72f, r);

        Add(B.Root, w, sx: Mathf.Lerp(0.018f, 0.03f, r) * contact, sy: -Mathf.Lerp(0.025f, 0.045f, r) * contact);
        Add(B.Torso, w, rot: Mathf.Lerp(-4f, -11f, r) + 1.2f * Mathf.Sin(2f * phase + 0.6f));
        Add(B.Neck, w, dy: 1.5f * bob);
        Add(B.Head, w, rot: Mathf.Lerp(2f, 6f, r) - 1.5f * Mathf.Sin(2f * phase + 1f));
        Add(B.LegUF, w, rot: thighAmp * s);
        Add(B.LegLF, w, rot: -kneeMax * Ease(Mathf.Max(0f, c)) - 4f);
        Add(B.LegUB, w, rot: -thighAmp * s);
        Add(B.LegLB, w, rot: -kneeMax * Ease(Mathf.Max(0f, -c)) - 4f);
        Add(B.ArmUF, w, rot: -armAmp * s);
        Add(B.ArmLF, w, rot: elbow + 8f * Mathf.Sin(phase - 0.5f));
        Add(B.ArmUB, w, rot: armAmp * s);
        Add(B.ArmLB, w, rot: elbow - 8f * Mathf.Sin(phase - 0.5f));
        Add(B.Skirt, w, rot: Mathf.Lerp(2.5f, 4f, r) * Mathf.Sin(2f * phase - 0.8f) - Mathf.Lerp(1.5f, 4f, r));
    }

    private void AddAir(float w, float vy)
    {
        if (w <= 0f) return;
        float up = Mathf.Clamp01(vy / 6f);
        float down = Mathf.Clamp01(-vy / 9f);
        float apex = 1f - Mathf.Max(up, down);

        Add(B.Root, w, sx: -0.04f * up, sy: 0.07f * up);
        Add(B.Torso, w, rot: -3f * up + 2f * down);
        Add(B.Head, w, rot: 3f * up - 4f * down);
        Add(B.LegUF, w, rot: 58f * up + 46f * apex + 12f * down);
        Add(B.LegLF, w, rot: -88f * up - 76f * apex - 12f * down);
        Add(B.LegUB, w, rot: 36f * up + 30f * apex - 8f * down);
        Add(B.LegLB, w, rot: -82f * up - 70f * apex - 18f * down);
        Add(B.ArmUF, w, rot: 55f * up + 30f * apex + 85f * down);
        Add(B.ArmLF, w, rot: 25f * up + 15f * apex + 30f * down);
        Add(B.ArmUB, w, rot: 35f * up + 25f * apex + 100f * down);
        Add(B.ArmLB, w, rot: 20f * up + 10f * apex + 25f * down);
    }

    private void AddFly(float w)
    {
        if (w <= 0f) return;
        float bob = Wave(time, 1.4f, 0f);
        Add(B.Root, w, dy: 12f * bob, rot: 2.5f * Wave(time, 2.8f, 0.4f));
        Add(B.Torso, w, rot: -8f + 3f * Wave(time, 1.4f, -0.5f));
        Add(B.Head, w, rot: 4f + 3f * Wave(time, 2.8f, -0.4f));
        Add(B.LegUF, w, rot: 72f);
        Add(B.LegLF, w, rot: -92f + 4f * Wave(time, 1.4f, -1f));
        Add(B.LegUB, w, rot: 66f);
        Add(B.LegLB, w, rot: -86f + 4f * Wave(time, 1.4f, -1.4f));
        Add(B.ArmUF, w, rot: 52f);
        Add(B.ArmLF, w, rot: 28f);
        Add(B.ArmUB, w, rot: 48f);
        Add(B.ArmLB, w, rot: 24f);
        Add(B.Skirt, w, rot: -6f);
    }

    private void AddTakeoff(float dt)
    {
        if (takeoffTimer < 0f) return;
        takeoffTimer += dt;
        float k = takeoffTimer / TakeoffDuration;
        if (k >= 1f) { takeoffTimer = -1f; return; }
        // Anticipación: pequeño agachado justo al despegar antes del estiramiento
        float e = Mathf.Sin(k * Mathf.PI);
        Add(B.Root, 1f, sx: 0.06f * e, sy: -0.1f * e);
    }

    private void AddLanding(float dt)
    {
        if (landTimer < 0f) return;
        landTimer += dt;
        float k = landTimer / LandDuration;
        if (k >= 1f) { landTimer = -1f; return; }
        // Impacto inmediato y recuperación suave
        float e = landStrength * (1f - Ease(k));
        Add(B.Root, 1f, sx: 0.09f * e, sy: -0.15f * e);
        Add(B.Torso, 1f, rot: -9f * e);
        Add(B.Head, 1f, rot: -4f * e);
        Add(B.LegUF, 1f, rot: 30f * e);
        Add(B.LegLF, 1f, rot: -55f * e);
        Add(B.LegUB, 1f, rot: 22f * e);
        Add(B.LegLB, 1f, rot: -45f * e);
        Add(B.ArmUF, 1f, rot: 25f * e);
        Add(B.ArmUB, 1f, rot: 20f * e);
    }

    private void AddCast(float dt)
    {
        if (castTimer < 0f) return;
        castTimer += dt;
        float k = castTimer / CastDuration;
        if (k >= 1f) { castTimer = -1f; return; }

        // Anticipación (brazo atrás) -> lanzamiento rápido -> follow-through y regreso
        float wind = k < 0.22f ? Ease(k / 0.22f) : 0f;
        float thr = k < 0.22f ? 0f : (k < 0.42f ? BackOut((k - 0.22f) / 0.2f) : 1f - Ease((k - 0.42f) / 0.58f));
        Add(B.ArmUF, 1f, rot: -75f * wind + 115f * thr);
        Add(B.ArmLF, 1f, rot: 40f * wind - 5f * thr);
        Add(B.Torso, 1f, rot: 5f * wind - 9f * thr);
        Add(B.Head, 1f, rot: 3f * wind - 4f * thr);
        Add(B.ArmUB, 1f, rot: 15f * wind - 25f * thr);
        Add(B.Root, 1f, sx: 0.03f * thr, sy: -0.04f * thr);
    }

    private void AddHurt(float dt)
    {
        if (hurtTimer < 0f) return;
        hurtTimer += dt;
        float k = hurtTimer / HurtDuration;
        if (k >= 1f) { hurtTimer = -1f; return; }
        float e = k < 0.12f ? EaseOut(k / 0.12f) : 1f - Ease((k - 0.12f) / 0.88f);
        Add(B.Root, 1f, dx: -14f * e, sx: -0.04f * e, sy: -0.06f * e);
        Add(B.Torso, 1f, rot: 14f * e);
        Add(B.Head, 1f, rot: 12f * e);
        Add(B.ArmUF, 1f, rot: 35f * e);
        Add(B.ArmLF, 1f, rot: 30f * e);
        Add(B.ArmUB, 1f, rot: 50f * e);
    }

    private float AddDie(float dt)
    {
        if (dieTimer < 0f) return 0f;
        dieTimer += dt;
        float t = dieTimer;
        // 1) Retroceso  2) Rodillas ceden  3) Desplome hacia delante con rebote
        float recoil = t < 0.35f ? Mathf.Sin(Mathf.Clamp01(t / 0.35f) * Mathf.PI) : 0f;
        float buckle = Ease(Mathf.Clamp01((t - 0.2f) / 0.4f));
        float fall = Mathf.Clamp01((t - 0.5f) / 0.7f);
        // Caída con aceleración (ease-in) y rebote al tocar el suelo
        float fallAngle = -84f * FallWithBounce(fall);

        Add(B.Torso, 1f, rot: 12f * recoil - 18f * buckle);
        Add(B.Head, 1f, rot: 10f * recoil - 10f * buckle);
        Add(B.LegUF, 1f, rot: 45f * buckle);
        Add(B.LegLF, 1f, rot: -80f * buckle);
        Add(B.LegUB, 1f, rot: 35f * buckle);
        Add(B.LegLB, 1f, rot: -70f * buckle);
        Add(B.ArmUF, 1f, rot: 40f * recoil + 70f * Ease(fall));
        Add(B.ArmUB, 1f, rot: 30f * recoil + 80f * Ease(fall));
        // Al caer se eleva un poco la raíz para que cabeza y capucha descansen sobre el suelo
        Add(B.Root, 1f, rot: fallAngle, dy: -95f * Ease(fall));
        // El apoyo de pies se mantiene mientras ceden las rodillas y se libera durante el desplome
        return Mathf.Clamp01(fall * 4f);
    }

    private void AddSecondary(float dt, float vForward, float vy)
    {
        float headRot = pose[(int)B.Head].rot;
        float torsoRot = pose[(int)B.Torso].rot;
        float accel = Mathf.Clamp((Mathf.Abs(vForward) - Mathf.Abs(prevVx)) / Mathf.Max(dt, 1e-4f), -40f, 40f);

        // El avance y la caída levantan (negativo) coleta y bufanda; la subida las empuja hacia abajo
        float trail = Mathf.Clamp(vForward, -2f, runSpeed) * 2.6f;
        float fallLift = Mathf.Max(0f, -vy) * 2.2f;
        float riseDrop = Mathf.Max(0f, vy) * 1.4f;
        float sway = Wave(time, 2.9f, -1.2f);

        float hairTarget = -(headRot + torsoRot) * 1.2f - trail - fallLift + riseDrop + 4f * sway;
        float scarfTarget = -(headRot + torsoRot) * 0.8f - trail * 1.3f - fallLift * 1.2f + riseDrop + 4.5f * Wave(time, 3.1f, -1.6f);
        float hoodTarget = -headRot * 0.25f - Mathf.Clamp(trail * 0.2f, -3f, 3f) - Mathf.Min(fallLift * 0.3f, 4f);
        float skirtTarget = -Mathf.Clamp(accel * 0.12f, -6f, 6f) - Mathf.Min(fallLift * 0.6f, 6f);

        Add(B.HairTail, 1f, rot: Mathf.Clamp(hair.Step(hairTarget, dt, 3.2f, 0.35f), -45f, 25f));
        Add(B.ScarfTail, 1f, rot: Mathf.Clamp(scarf.Step(scarfTarget, dt, 2.6f, 0.3f), -55f, 25f));
        float h = Mathf.Clamp(hood.Step(hoodTarget, dt, 4f, 0.4f), -6f, 6f);
        Add(B.Hood, 1f, rot: h);
        Add(B.HoodInner, 1f, rot: h);
        Add(B.Skirt, 1f, rot: skirt.Step(skirtTarget, dt, 3.5f, 0.45f));
    }

    // ----------------------------------------------------------------- Aplicación

    private void Apply()
    {
        for (int i = 0; i < (int)B.Count; i++)
        {
            Transform t = bones[i];
            P p = pose[i];
            float k = 1f / (pixelsPerUnit * parentScale[i]);
            t.localPosition = restPos[i] + new Vector3(p.dx * k, -p.dy * k, 0f);
            t.localRotation = Quaternion.Euler(0f, 0f, restRot[i] + p.rot);
            t.localScale = new Vector3(restScale[i].x * (1f + p.sx), restScale[i].y * (1f + p.sy), 1f);
        }
    }

    private void GroundFeet(float weight)
    {
        if (weight <= 0.001f || tipFront == null || tipBack == null) return;
        // Pie más bajo apoyado en y = 0 del rig (pies del collider)
        float a = transform.InverseTransformPoint(tipFront.position).y;
        float b = transform.InverseTransformPoint(tipBack.position).y;
        float lowest = Mathf.Min(a, b);
        bones[(int)B.Root].localPosition += new Vector3(0f, -lowest * weight, 0f);
    }

    private void UpdateFace(float dt)
    {
        if (eyeRenderer != null && eyeOpen != null)
        {
            bool forcedClosed = IsDying && dieTimer > 0.5f || hurtTimer >= 0f && hurtTimer < HurtDuration * 0.6f;
            if (blinkTimer < 0f && time >= nextBlink) blinkTimer = 0f;
            Sprite eye = eyeOpen;
            if (blinkTimer >= 0f)
            {
                blinkTimer += dt;
                float k = blinkTimer / 0.16f;
                if (k >= 1f)
                {
                    blinkTimer = -1f;
                    nextBlink = time + (Random.value < 0.2f ? 0.2f : Random.Range(2f, 5f));
                }
                else
                {
                    eye = k < 0.25f || k > 0.75f ? eyeHalf : eyeClosed;
                }
            }
            if (forcedClosed) eye = eyeClosed;
            else if (castTimer >= 0f && castTimer < CastDuration * 0.35f) eye = eyeHalf; // apunta
            eyeRenderer.sprite = eye != null ? eye : eyeOpen;
        }

        if (mouthRenderer != null && mouthSmile != null)
        {
            bool open = hurtTimer >= 0f || IsDying || castTimer >= CastDuration * 0.2f || airBlend > 0.6f && lastVy < -6f;
            mouthRenderer.sprite = open && mouthO != null ? mouthO : mouthSmile;
        }
    }

    private void EmitDust(int count)
    {
        if (dust == null) dust = CreateDust();
        if (dust == null) return;
        dust.transform.position = transform.position;
        dust.Emit(count);
    }

    private ParticleSystem CreateDust()
    {
        GameObject go = new GameObject("LandingDust");
        go.transform.SetParent(transform.parent, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startColor = dustColor;
        main.gravityModifier = -0.05f;
        main.maxParticles = 64;

        ParticleSystem.EmissionModule em = ps.emission;
        em.enabled = false;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 80f;
        shape.radius = 0.15f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.4f));
        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sortingLayerName = "Gameplay";
        r.sortingOrder = 20;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            r.material = new Material(shader) { mainTexture = SoftCircle() };
        }
        return ps;
    }

    private static Texture2D SoftCircle()
    {
        const int n = 32;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f)) / (n * 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d)));
            }
        }
        tex.Apply();
        return tex;
    }

    // ----------------------------------------------------------------- Utilidades

    private void Add(B b, float w, float rot = 0f, float dx = 0f, float dy = 0f, float sx = 0f, float sy = 0f)
    {
        if (w <= 0f) return;
        ref P p = ref pose[(int)b];
        p.rot += rot * w;
        p.dx += dx * w;
        p.dy += dy * w;
        p.sx += sx * w;
        p.sy += sy * w;
    }

    private static float Wave(float t, float period, float phase) => Mathf.Sin(Tau * (t / period) + phase);
    private static float Ease(float x) { x = Mathf.Clamp01(x); return 0.5f - 0.5f * Mathf.Cos(Mathf.PI * x); }
    private static float EaseOut(float x) { x = Mathf.Clamp01(x); return 1f - (1f - x) * (1f - x); }

    private static float FallWithBounce(float x)
    {
        // 0..0.7: caída acelerada; 0.7..1: rebote pequeño y asentamiento
        if (x < 0.7f) { float u = x / 0.7f; return u * u; }
        float b = (x - 0.7f) / 0.3f;
        return 1f - 0.12f * Mathf.Sin(b * Mathf.PI) * (1f - b * 0.3f);
    }

    private static float BackOut(float x)
    {
        // Sobrepasa un poco y asienta (rebote del desplome)
        x = Mathf.Clamp01(x);
        const float c = 1.4f;
        float u = x - 1f;
        return 1f + (c + 1f) * u * u * u + c * u * u;
    }

    private static float Approach(float current, float target, float speed, float dt)
    {
        return Mathf.Lerp(current, target, 1f - Mathf.Exp(-speed * dt));
    }
}
