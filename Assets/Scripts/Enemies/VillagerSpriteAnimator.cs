using UnityEngine;

/// <summary>
/// Reproduce las animaciones cuadro a cuadro de un aldeano según el estado de su VillagerAI:
/// paseo (idle/caminar con la cadencia ajustada a la velocidad), aviso, grito, frenesí, hechizos
/// y una acción propia que hace de vez en cuando al quedarse quieto.
/// Cada instancia tiene un tempo y unos tiempos ligeramente distintos para que no se muevan al unísono.
/// Va en el objeto "Visual" (el que VillagerAI voltea en X).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class VillagerSpriteAnimator : MonoBehaviour
{
    [SerializeField] private VillagerAnimationSet animations;

    [Header("Variación por instancia")]
    [Tooltip("Variación aleatoria del tempo de las animaciones (±).")]
    [SerializeField] private float tempoJitter = 0.08f;
    [Tooltip("Segundos quieto (mín./máx.) antes de hacer su acción propia.")]
    [SerializeField] private Vector2 actionInterval = new Vector2(5f, 11f);

    [Header("Respiración en idle")]
    [SerializeField] private float breathAmount = 0.012f;
    [SerializeField] private float breathPeriod = 2.8f;

    private VillagerAI ai;
    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private VillagerAnimationSet.Clip current;
    private int frame;
    private float frameTimer;
    private float tempo = 1f;
    private float stillTime;
    private float nextAction;
    private bool playingAction;
    private float breathOffset;

    public VillagerAnimationSet Animations
    {
        get => animations;
        set => animations = value;
    }

    private void Awake()
    {
        ai = GetComponentInParent<VillagerAI>();
        body = GetComponentInParent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        tempo = 1f + Random.Range(-tempoJitter, tempoJitter);
        breathOffset = Random.value * breathPeriod;
        nextAction = Random.Range(actionInterval.x, actionInterval.y);
        spriteRenderer.sortingLayerName = "Gameplay";
        spriteRenderer.sortingOrder = 4;
    }

    private void Start()
    {
        if (animations == null || ai == null) return;
        // Los iconos de aviso y hechizo se colocan a la altura de la cabeza de este personaje
        foreach (string iconName in new[] { "NoticeIcon", "StatusIcon" })
        {
            Transform icon = ai.transform.Find(iconName);
            if (icon != null) icon.localPosition = new Vector3(0f, animations.iconHeight, 0f);
        }
        Play(animations.idle);
        frame = Random.Range(0, Mathf.Max(1, animations.idle.frames.Length));
    }

    private void Update()
    {
        if (animations == null || ai == null) return;

        float speed = body != null ? Mathf.Abs(body.linearVelocity.x) : 0f;
        VillagerAnimationSet.Clip target = ChooseClip(speed);
        if (target != current) Play(target);
        if (current == null || current.IsEmpty) return;

        float fps = current.fps * tempo;
        if (current == animations.walk && ai.WalkSpeed > 0.01f)
        {
            // La cadencia sigue a la velocidad para que los pies no patinen
            fps *= Mathf.Clamp(speed / ai.WalkSpeed, 0.6f, 2.2f);
        }

        frameTimer += Time.deltaTime * fps;
        while (frameTimer >= 1f)
        {
            frameTimer -= 1f;
            frame++;
            if (frame >= current.frames.Length)
            {
                if (current.loop)
                {
                    frame = 0;
                }
                else
                {
                    frame = current.frames.Length - 1;
                    if (playingAction && current == animations.action) EndAction();
                }
            }
        }
        spriteRenderer.sprite = current.frames[Mathf.Clamp(frame, 0, current.frames.Length - 1)];

        // Respiración sutil en idle (solo escala Y; VillagerAI controla el signo de X para voltear)
        Vector3 s = transform.localScale;
        s.y = current == animations.idle ? 1f + breathAmount * Mathf.Sin((Time.time + breathOffset) * Mathf.PI * 2f / breathPeriod) : 1f;
        transform.localScale = s;
    }

    private VillagerAnimationSet.Clip ChooseClip(float speed)
    {
        switch (ai.ActiveSpell)
        {
            case VillagerAI.VillagerSpellType.Blind: return Fallback(animations.blind);
            case VillagerAI.VillagerSpellType.Hypnotize: return Fallback(animations.hypnotize);
            case VillagerAI.VillagerSpellType.Sleep: return Fallback(animations.sleep);
        }

        switch (ai.CurrentState)
        {
            case VillagerState.Notice:
                CancelAction();
                return Fallback(animations.notice);
            case VillagerState.Scream:
                CancelAction();
                return Fallback(animations.scream);
            case VillagerState.Frenzy:
                CancelAction();
                return Fallback(animations.run);
            case VillagerState.Cooldown:
                CancelAction();
                return speed > 0.1f ? animations.walk : animations.idle;
        }

        // Paseo: caminar, idle o, tras un rato quieto, su acción propia
        if (speed > 0.15f)
        {
            CancelAction();
            stillTime = 0f;
            return animations.walk;
        }

        if (playingAction) return animations.action;
        stillTime += Time.deltaTime;
        if (stillTime >= nextAction && !animations.action.IsEmpty)
        {
            playingAction = true;
            return animations.action;
        }
        return animations.idle;
    }

    private VillagerAnimationSet.Clip Fallback(VillagerAnimationSet.Clip clip)
    {
        return clip == null || clip.IsEmpty ? animations.idle : clip;
    }

    private void Play(VillagerAnimationSet.Clip clip)
    {
        current = clip;
        frame = 0;
        frameTimer = 0f;
    }

    private void EndAction()
    {
        playingAction = false;
        stillTime = 0f;
        nextAction = Random.Range(actionInterval.x, actionInterval.y);
    }

    private void CancelAction()
    {
        if (playingAction) EndAction();
    }
}
