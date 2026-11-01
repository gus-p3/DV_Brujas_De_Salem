using UnityEngine;

/// <summary>
/// Reproductor de sprites 2D por estado para Aldeanos (VillagerAI) y Guardias (PatrolRoute + EnemySight).
/// Se monta en el hijo "Visual" junto al SpriteRenderer y selecciona automáticamente el clip
/// de <see cref="VillagerAnimationSet"/> en cada LateUpdate sin requerir Animator Controller.
///
/// Incluye animación secundaria procedural fluida en tiempo de ejecución:
/// - Transición elástica suave al girar de dirección (sin perder el signo de localScale.x).
/// - Rebote de zancada, inclinación dinámica según velocidad y squash-and-stretch al caminar y correr.
/// - Salto de sobresalto en Notice (!), temblor de alarma en Scream, vaivén de mareo en Blind,
///   balanceo hipnótico en Hypnotize y respiración profunda en Sleep.
/// - Animación flotante/pulsante para los iconos de estado (NoticeIcon y StatusIcon) y partículas de polvo al correr.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class VillagerSpriteAnimator : MonoBehaviour
{
    public enum AnimState
    {
        Idle,
        Walk,
        Run,
        Notice,
        Scream,
        Blind,
        Hypnotize,
        Sleep
    }

    [SerializeField] private VillagerAnimationSet animationSet;
    [SerializeField] private float idleFps = 6.5f;
    [SerializeField] private float walkFps = 14.5f;
    [SerializeField] private float runFps = 20.0f;
    [SerializeField] private float statusFps = 8.5f;

    [Header("Movimiento Secundario Procedural")]
    [SerializeField] private bool enableProceduralFluidity = true;
    [SerializeField] private float walkBounceHeight = 0.042f;
    [SerializeField] private float runBounceHeight = 0.082f;
    [SerializeField] private float walkLeanDegrees = 3.5f;
    [SerializeField] private float runLeanDegrees = 8.0f;

    private SpriteRenderer sr;
    private VillagerAI villagerAI;
    private PatrolRoute patrolRoute;
    private EnemySight enemySight;
    private Rigidbody2D parentRb;

    private Transform noticeIconTransform;
    private Transform statusIconTransform;
    private Vector3 noticeIconBasePos;
    private Vector3 statusIconBasePos;
    private Vector3 noticeIconBaseScale = Vector3.one;
    private Vector3 statusIconBaseScale = Vector3.one;

    private AnimState currentState = AnimState.Idle;
    private float frameTimer;
    private int frameIndex;
    private Vector2 lastParentPos;
    private bool hasLastPos;

    // Estado base del transform "Visual" para aplicar animación secundaria aditiva sin deriva
    private Vector3 baseLocalPos;
    private Vector3 baseAbsScale = Vector3.one;
    private bool baseTransformCaptured;
    private int lastFacingSign = 1;
    private float turnTransitionTimer;
    private float stateEnterTime;
    private float randomPhaseOffset;
    private float lastDustSpawnTime = -10f;

    public VillagerAnimationSet AnimationSet
    {
        get => animationSet;
        set
        {
            animationSet = value;
            ApplyCurrentFrame(force: true);
        }
    }

    public VillagerAnimationSet Animations
    {
        get => animationSet;
        set
        {
            animationSet = value;
            ApplyCurrentFrame(force: true);
        }
    }

    public void SetAnimationSet(VillagerAnimationSet set)
    {
        Animations = set;
    }

    public AnimState CurrentState => currentState;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        villagerAI = GetComponentInParent<VillagerAI>();
        patrolRoute = GetComponentInParent<PatrolRoute>();
        enemySight = GetComponentInParent<EnemySight>();
        parentRb = GetComponentInParent<Rigidbody2D>();

        // Actualizar FPS antiguos serializados en prefabs para aprovechar los 12 fotogramas con total fluidez
        if (idleFps < 5f) idleFps = 6.5f;
        if (walkFps < 12f) walkFps = 14.5f;
        if (runFps < 17f) runFps = 20.0f;
        if (statusFps < 7f) statusFps = 8.5f;

        randomPhaseOffset = UnityEngine.Random.Range(0f, 6.28f);
        CaptureBaseTransform();
        CacheStatusIcons();
    }

    private void Start()
    {
        CaptureBaseTransform();
        CacheStatusIcons();
        if ( transform.parent != null )
        {
            lastParentPos = transform.parent.position;
            hasLastPos = true;
        }
        ApplyCurrentFrame(force: true);
    }

    private void CaptureBaseTransform()
    {
        if (baseTransformCaptured) return;
        baseLocalPos = transform.localPosition;
        Vector3 s = transform.localScale;
        baseAbsScale = new Vector3(
            Mathf.Abs(s.x) > 0.001f ? Mathf.Abs(s.x) : 1f,
            Mathf.Abs(s.y) > 0.001f ? Mathf.Abs(s.y) : 1f,
            Mathf.Abs(s.z) > 0.001f ? Mathf.Abs(s.z) : 1f);
        lastFacingSign = s.x >= 0f ? 1 : -1;
        baseTransformCaptured = true;
    }

    private void CacheStatusIcons()
    {
        if (transform.parent == null) return;
        if (noticeIconTransform == null)
        {
            noticeIconTransform = transform.parent.Find("NoticeIcon");
            if (noticeIconTransform != null)
            {
                noticeIconBasePos = noticeIconTransform.localPosition;
                noticeIconBaseScale = noticeIconTransform.localScale;
            }
        }
        if (statusIconTransform == null)
        {
            statusIconTransform = transform.parent.Find("StatusIcon");
            if (statusIconTransform != null)
            {
                statusIconBasePos = statusIconTransform.localPosition;
                statusIconBaseScale = statusIconTransform.localScale;
            }
        }
    }

    private void LateUpdate()
    {
        if (animationSet == null || sr == null) return;

        float speed = MeasureParentSpeed();
        AnimState next = ResolveState(speed);
        if (next != currentState)
        {
            currentState = next;
            frameIndex = 0;
            frameTimer = 0f;
            stateEnterTime = Time.time;
            ApplyCurrentFrame(force: true);
        }

        Sprite[] clip = GetClip(currentState);
        if (clip == null || clip.Length == 0) return;

        float fps = GetFps(currentState, speed);
        if (clip.Length > 1 && fps > 0f)
        {
            frameTimer += Time.deltaTime;
            float step = 1f / fps;
            if (frameTimer >= step)
            {
                int advance = Mathf.Max(1, Mathf.FloorToInt(frameTimer / step));
                frameTimer -= advance * step;
                frameIndex = (frameIndex + advance) % clip.Length;
                sr.sprite = clip[frameIndex];
            }
        }
        else if (sr.sprite != clip[0])
        {
            sr.sprite = clip[0];
        }

        if (Application.isPlaying && enableProceduralFluidity)
        {
            ApplyProceduralSecondaryMotion(clip.Length, speed);
            AnimateOverheadIcons();
        }
    }

    private float MeasureParentSpeed()
    {
        float speed = 0f;
        if (parentRb != null)
        {
            speed = Mathf.Abs(parentRb.linearVelocity.x);
        }

        if (transform.parent != null)
        {
            Vector2 currentPos = transform.parent.position;
            if (hasLastPos && Time.deltaTime > 0.0001f)
            {
                float transformSpeed = Mathf.Abs(currentPos.x - lastParentPos.x) / Time.deltaTime;
                if (transformSpeed > speed) speed = transformSpeed;
            }
            lastParentPos = currentPos;
            hasLastPos = true;
        }
        return speed;
    }

    private AnimState ResolveState(float horizontalSpeed)
    {
        // 1. Aldeano (VillagerAI)
        if (villagerAI != null)
        {
            if (villagerAI.IsAffectedBySpell)
            {
                switch (villagerAI.ActiveSpell)
                {
                    case VillagerAI.VillagerSpellType.Blind: return AnimState.Blind;
                    case VillagerAI.VillagerSpellType.Hypnotize: return AnimState.Hypnotize;
                    case VillagerAI.VillagerSpellType.Sleep: return AnimState.Sleep;
                }
            }

            switch (villagerAI.CurrentState)
            {
                case VillagerState.Notice: return AnimState.Notice;
                case VillagerState.Scream: return AnimState.Scream;
                case VillagerState.Frenzy: return AnimState.Run;
                case VillagerState.Cooldown: return horizontalSpeed > 0.1f ? AnimState.Run : AnimState.Idle;
                case VillagerState.Wander: return horizontalSpeed > 0.1f ? AnimState.Walk : AnimState.Idle;
            }
        }

        // 2. Guardia (PatrolRoute + EnemySight)
        if (patrolRoute != null)
        {
            if (patrolRoute.IsAffectedBySpell)
            {
                switch (patrolRoute.ActiveSpell)
                {
                    case PatrolRoute.SpellEffectType.Blind: return AnimState.Blind;
                    case PatrolRoute.SpellEffectType.Hypnotize: return AnimState.Hypnotize;
                    case PatrolRoute.SpellEffectType.Sleep: return AnimState.Sleep;
                }
            }

            if (enemySight != null)
            {
                if (enemySight.CurrentState == AlertState.Alert)
                    return horizontalSpeed > 0.1f ? AnimState.Run : AnimState.Notice;
                if (enemySight.CurrentState == AlertState.Suspicion && horizontalSpeed <= 0.1f)
                    return AnimState.Notice;
            }

            return horizontalSpeed > 0.1f ? AnimState.Walk : AnimState.Idle;
        }

        return AnimState.Idle;
    }

    private void ApplyProceduralSecondaryMotion(int clipLength, float speed)
    {
        CaptureBaseTransform();

        // Detectar si VillagerAI o PatrolRoute acaba de invertir el signo de localScale.x
        int currentFacingSign = transform.localScale.x >= 0f ? 1 : -1;
        if (currentFacingSign != lastFacingSign)
        {
            lastFacingSign = currentFacingSign;
            turnTransitionTimer = 0.14f;
        }

        float turnSquashX = 1f;
        float turnStretchY = 1f;
        if (turnTransitionTimer > 0f)
        {
            turnTransitionTimer = Mathf.Max(0f, turnTransitionTimer - Time.deltaTime);
            float p = 1f - (turnTransitionTimer / 0.14f);
            // Curva elástica rápida al girar
            turnSquashX = p < 0.45f
                ? Mathf.Lerp(0.58f, 1.08f, p / 0.45f)
                : Mathf.Lerp(1.08f, 1.0f, (p - 0.45f) / 0.55f);
            turnStretchY = p < 0.45f
                ? Mathf.Lerp(1.10f, 0.96f, p / 0.45f)
                : Mathf.Lerp(0.96f, 1.0f, (p - 0.45f) / 0.55f);
        }

        float t = Time.time + randomPhaseOffset;
        float stateAge = Time.time - stateEnterTime;

        Vector3 posOffset = Vector3.zero;
        float scaleXMult = 1f;
        float scaleYMult = 1f;
        float tiltZ = 0f;

        switch (currentState)
        {
            case AnimState.Idle:
            {
                float breath = Mathf.Sin(t * 2.8f);
                scaleYMult = 1f + breath * 0.024f;
                scaleXMult = 1f - breath * 0.014f;
                tiltZ = Mathf.Sin(t * 1.4f) * 0.8f;
                break;
            }

            case AnimState.Walk:
            {
                float cycleProgress = clipLength > 0 ? ((frameIndex + frameTimer * walkFps) / clipLength) : (t * 2f);
                float stepWave = Mathf.Abs(Mathf.Sin(cycleProgress * Mathf.PI * 2f));
                posOffset.y = stepWave * walkBounceHeight;
                scaleYMult = 1f + (stepWave - 0.5f) * 0.055f;
                scaleXMult = 1f - (stepWave - 0.5f) * 0.035f;
                float speedFactor = Mathf.Clamp01(speed / 2.2f);
                tiltZ = -currentFacingSign * walkLeanDegrees * speedFactor + Mathf.Sin(cycleProgress * Mathf.PI * 2f) * 1.6f;
                break;
            }

            case AnimState.Run:
            {
                float cycleProgress = clipLength > 0 ? ((frameIndex + frameTimer * runFps) / clipLength) : (t * 3.5f);
                float stepWave = Mathf.Abs(Mathf.Sin(cycleProgress * Mathf.PI * 2f));
                posOffset.y = stepWave * runBounceHeight;
                scaleYMult = 1f + (stepWave - 0.5f) * 0.11f;
                scaleXMult = 1f - (stepWave - 0.5f) * 0.07f;
                tiltZ = -currentFacingSign * runLeanDegrees + Mathf.Sin(cycleProgress * Mathf.PI * 4f) * 2.5f;

                // Generar pequeña nube de polvo en los pies en cada apoyo de carrera
                if (stepWave < 0.18f && Time.time - lastDustSpawnTime > 0.24f)
                {
                    lastDustSpawnTime = Time.time;
                    SpawnFootstepDust(currentFacingSign);
                }
                break;
            }

            case AnimState.Notice:
            {
                // Sobresalto elástico al detectar a la bruja
                if (stateAge < 0.28f)
                {
                    float p = stateAge / 0.28f;
                    posOffset.y = Mathf.Sin(p * Mathf.PI) * 0.16f;
                    scaleYMult = 1f + Mathf.Sin(p * Mathf.PI) * 0.18f;
                    scaleXMult = 1f - Mathf.Sin(p * Mathf.PI) * 0.10f;
                }
                else
                {
                    float alertPulse = Mathf.Sin(t * 10f);
                    scaleYMult = 1f + alertPulse * 0.035f;
                    scaleXMult = 1f - alertPulse * 0.02f;
                }
                break;
            }

            case AnimState.Scream:
            {
                // Temblor intenso de alarma y expansión al gritar
                posOffset.x = Mathf.Sin(Time.time * 48f) * 0.032f;
                posOffset.y = Mathf.Abs(Mathf.Sin(Time.time * 12f)) * 0.045f;
                float shoutPulse = Mathf.Sin(Time.time * 14f);
                scaleYMult = 1.04f + shoutPulse * 0.065f;
                scaleXMult = 0.98f - shoutPulse * 0.04f;
                tiltZ = Mathf.Sin(Time.time * 22f) * 3.5f;
                break;
            }

            case AnimState.Blind:
            {
                // Vaivén de mareo al estar cegado
                tiltZ = Mathf.Sin(t * 4.5f) * 7.0f;
                posOffset.x = Mathf.Sin(t * 2.25f) * 0.035f;
                scaleYMult = 0.95f + Mathf.Sin(t * 4.5f) * 0.03f;
                scaleXMult = 1.03f;
                break;
            }

            case AnimState.Hypnotize:
            {
                // Balanceo hipnótico de péndulo
                tiltZ = Mathf.Sin(t * 3.2f) * 6.5f;
                posOffset.y = Mathf.Abs(Mathf.Sin(t * 3.2f)) * 0.038f;
                scaleYMult = 1f + Mathf.Sin(t * 6.4f) * 0.035f;
                break;
            }

            case AnimState.Sleep:
            {
                // Respiración lenta y profunda al dormir
                float deepBreath = Mathf.Sin(t * 1.9f);
                scaleYMult = 0.93f + deepBreath * 0.045f;
                scaleXMult = 1.05f - deepBreath * 0.035f;
                tiltZ = currentFacingSign * 4.5f;
                break;
            }
        }

        transform.localPosition = baseLocalPos + posOffset;
        transform.localScale = new Vector3(
            baseAbsScale.x * scaleXMult * turnSquashX * currentFacingSign,
            baseAbsScale.y * scaleYMult * turnStretchY,
            baseAbsScale.z);
        transform.localRotation = Quaternion.Euler(0f, 0f, tiltZ);
    }

    private void AnimateOverheadIcons()
    {
        CacheStatusIcons();

        if (noticeIconTransform != null && noticeIconTransform.gameObject.activeSelf)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 14f) * 0.18f;
            noticeIconTransform.localScale = noticeIconBaseScale * pulse;
            noticeIconTransform.localPosition = noticeIconBasePos + new Vector3(0f, Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 0.10f, 0f);
        }

        if (statusIconTransform != null && statusIconTransform.gameObject.activeSelf)
        {
            float bob = Mathf.Sin(Time.time * 4.5f) * 0.08f;
            statusIconTransform.localPosition = statusIconBasePos + new Vector3(0f, bob, 0f);

            if (currentState == AnimState.Hypnotize)
            {
                statusIconTransform.localRotation = Quaternion.Euler(0f, 0f, -Time.time * 160f);
                statusIconTransform.localScale = statusIconBaseScale * (1f + Mathf.Sin(Time.time * 6f) * 0.12f);
            }
            else if (currentState == AnimState.Sleep)
            {
                float s = 1f + Mathf.Sin(Time.time * 2.5f) * 0.16f;
                statusIconTransform.localScale = statusIconBaseScale * s;
                statusIconTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2.5f) * 10f);
            }
            else
            {
                statusIconTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 7f) * 12f);
                statusIconTransform.localScale = statusIconBaseScale * (1f + Mathf.Sin(Time.time * 8f) * 0.12f);
            }
        }
    }

    private void SpawnFootstepDust(int facingSign)
    {
        if (LootPopupVisualizer.GetHaloSprite() == null) return;
        GameObject dust = new GameObject("VillagerStepDust");
        dust.transform.position = transform.position + new Vector3(-facingSign * 0.18f, 0.08f, 0f);
        dust.transform.localScale = Vector3.one * 0.28f;

        SpriteRenderer dsr = dust.AddComponent<SpriteRenderer>();
        dsr.sprite = LootPopupVisualizer.GetHaloSprite();
        dsr.color = new Color(0.78f, 0.72f, 0.62f, 0.45f);
        dsr.sortingLayerName = sr != null ? sr.sortingLayerName : "Characters";
        dsr.sortingOrder = (sr != null ? sr.sortingOrder : 5) - 1;

        Destroy(dust, 0.26f);
    }

    private Sprite[] GetClip(AnimState state)
    {
        switch (state)
        {
            case AnimState.Walk:      return NonEmpty(animationSet.walk, animationSet.idle);
            case AnimState.Run:       return NonEmpty(animationSet.run, animationSet.walk);
            case AnimState.Notice:    return NonEmpty(animationSet.notice, animationSet.idle);
            case AnimState.Scream:    return NonEmpty(animationSet.scream, animationSet.notice);
            case AnimState.Blind:     return NonEmpty(animationSet.blind, animationSet.idle);
            case AnimState.Hypnotize: return NonEmpty(animationSet.hypnotize, animationSet.blind);
            case AnimState.Sleep:     return NonEmpty(animationSet.sleep, animationSet.idle);
            default:                  return animationSet.idle != null ? animationSet.idle.frames : null;
        }
    }

    private static Sprite[] NonEmpty(VillagerAnimationSet.Clip primary, VillagerAnimationSet.Clip fallback)
    {
        if (primary != null && primary.frames != null && primary.frames.Length > 0 && primary.frames[0] != null)
            return primary.frames;
        return fallback != null ? fallback.frames : null;
    }

    private float GetFps(AnimState state, float speed)
    {
        switch (state)
        {
            case AnimState.Walk:
            {
                float speedScale = speed > 0.1f ? Mathf.Clamp(speed / 1.9f, 0.78f, 1.35f) : 1f;
                return walkFps * speedScale;
            }
            case AnimState.Run:
            {
                float speedScale = speed > 0.1f ? Mathf.Clamp(speed / 3.8f, 0.85f, 1.35f) : 1f;
                return runFps * speedScale;
            }
            case AnimState.Idle:
                return idleFps;
            default:
                return statusFps;
        }
    }

    private void ApplyCurrentFrame(bool force)
    {
        if (animationSet == null || sr == null) return;
        Sprite[] clip = GetClip(currentState);
        if (clip != null && clip.Length > 0 && clip[0] != null)
        {
            frameIndex = Mathf.Clamp(frameIndex, 0, clip.Length - 1);
            if (force || sr.sprite == null)
            {
                sr.sprite = clip[frameIndex];
            }
        }
    }
}
