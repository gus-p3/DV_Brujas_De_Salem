using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Estados de comportamiento del aldeano colonial.
/// </summary>
public enum VillagerState
{
    Wander,
    Notice,
    Scream,
    Frenzy,
    Cooldown,
    Disabled
}

/// <summary>
/// Patrones erráticos de patrulla/vagar del aldeano.
/// </summary>
public enum WanderPattern
{
    WalkAndReturn,     // Camina X metros, pausa, regresa
    LookAround,        // Camina a un punto, mira a ambos lados, continúa
    ShortStops         // Pasos cortos y paradas frecuentes
}

/// <summary>
/// IA del Aldeano colonial de Salem.
/// Vaga erráticamente, detecta a la bruja si está en forma humana,
/// alerta a los guardias en un radio de 14 u, desencadena el ataque frenético
/// y responde a las pociones mágicas implementando ISpellTarget.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class VillagerAI : MonoBehaviour, ISpellTarget
{
    [Header("Movimiento")]
    [Tooltip("Velocidad normal de paseo errático (u/s).")]
    [SerializeField] private float walkSpeed = 1.8f;
    [Tooltip("Velocidad al correr en frenesí (u/s).")]
    [SerializeField] private float frenzySpeed = 4.2f;

    [Header("Detección de Bordes")]
    [Tooltip("Máscara del suelo para no caer por precipicios.")]
    [SerializeField] private LayerMask groundMask;
    [Tooltip("Distancia frontal para chequear si hay suelo.")]
    [SerializeField] private float edgeCheckOffset = 0.5f;
    [Tooltip("Largo del rayo hacia abajo.")]
    [SerializeField] private float edgeCheckRayDistance = 1.2f;

    [Header("Visión")]
    [Tooltip("Distancia máxima de visión frontal (u).")]
    [SerializeField] private float viewDistance = 6.5f;
    [Tooltip("Capas donde se detecta a la bruja.")]
    [SerializeField] private LayerMask targetMask;
    [Tooltip("Capas de obstáculos que bloquean la visión (paredes/suelo).")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("Tiempos de Reacción")]
    [Tooltip("Tiempo en Notice antes de comenzar a gritar (s).")]
    [SerializeField] private float noticeDuration = 0.5f;
    [Tooltip("Duración de la animación y grito de alerta (s).")]
    [SerializeField] private float screamDuration = 2.0f;
    [Tooltip("Radio de alerta en el que despierta a los guardias (u).")]
    [SerializeField] private float guardAlertRadius = 14f;
    [Tooltip("Tiempo de enfriamiento tras terminar el frenesí (s).")]
    [SerializeField] private float cooldownDuration = 5f;

    [Header("Referencias Visuales")]
    [Tooltip("Transform que se escala en X para voltear.")]
    [SerializeField] private Transform visualTransform;
    [Tooltip("Animator del aldeano.")]
    [SerializeField] private Animator animator;
    [Tooltip("SpriteRenderer para el icono de exclamación '!' sobre la cabeza.")]
    [SerializeField] private SpriteRenderer noticeIconRenderer;
    [Tooltip("SpriteRenderer para los iconos de hechizos sobre la cabeza.")]
    [SerializeField] private SpriteRenderer statusIconRenderer;

    [Header("Audio")]
    [Tooltip("Sonido de grito al alertar.")]
    [SerializeField] private AudioClip screamSound;

    // Parámetros de Animator
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsScreamingHash = Animator.StringToHash("IsScreaming");
    private static readonly int IsFrenzyHash = Animator.StringToHash("IsFrenzy");

    private Rigidbody2D rb;
    private VillagerState currentState = VillagerState.Wander;
    private WanderPattern currentPattern = WanderPattern.WalkAndReturn;
    private bool facingRight = true;
    private Transform playerTarget;
    private IDetectable playerDetectable;

    // Control de patrones de Wander
    private float patternTimer = 0f;
    private float moveDir = 1f;
    private int lookAroundStep = 0;

    // Control de Hechizos
    private enum VillagerSpellType { None, Blind, Hypnotize, Sleep }
    private VillagerSpellType activeSpellType = VillagerSpellType.None;
    private Coroutine activeSpellCoroutine;

    /// <summary>
    /// Evento estático disparado cuando un aldeano percibe que la bruja se aproxima.
    /// Utilizado por TutorialManager para mostrar el cuadro de diálogo 'tip_aldeano'.
    /// </summary>
    public static event Action OnWitchApproaching;

    public bool IsAffectedBySpell => activeSpellType != VillagerSpellType.None;
    public VillagerState CurrentState => currentState;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (visualTransform == null)
        {
            Transform v = transform.Find("Visual");
            visualTransform = v != null ? v : transform;
        }
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (groundMask.value == 0)
        {
            groundMask = 1 << LayerMask.NameToLayer("Ground");
        }
        if (targetMask.value == 0)
        {
            targetMask = 1 << LayerMask.NameToLayer("Player");
        }
        if (obstacleMask.value == 0)
        {
            obstacleMask = 1 << LayerMask.NameToLayer("Ground");
        }

        EnsureIconRenderers();
    }

    private void EnsureIconRenderers()
    {
        if (noticeIconRenderer == null)
        {
            Transform tNotice = transform.Find("NoticeIcon");
            if (tNotice != null) noticeIconRenderer = tNotice.GetComponent<SpriteRenderer>();
            else
            {
                GameObject g = new GameObject("NoticeIcon");
                g.transform.SetParent(transform, false);
                g.transform.localPosition = new Vector3(0f, 2.3f, 0f);
                noticeIconRenderer = g.AddComponent<SpriteRenderer>();
                noticeIconRenderer.sortingLayerName = "Default";
                noticeIconRenderer.sortingOrder = 15;
                noticeIconRenderer.enabled = false;
            }
        }

        if (statusIconRenderer == null)
        {
            Transform tStatus = transform.Find("StatusIcon");
            if (tStatus != null) statusIconRenderer = tStatus.GetComponent<SpriteRenderer>();
            else
            {
                GameObject g = new GameObject("StatusIcon");
                g.transform.SetParent(transform, false);
                g.transform.localPosition = new Vector3(0f, 2.3f, 0f);
                statusIconRenderer = g.AddComponent<SpriteRenderer>();
                statusIconRenderer.sortingLayerName = "Default";
                statusIconRenderer.sortingOrder = 16;
                statusIconRenderer.enabled = false;
            }
        }
    }

    private void Start()
    {
        FindPlayer();
        SelectRandomPattern();
    }

    private void FindPlayer()
    {
        PlayerController pc = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            playerTarget = pc.transform;
            playerDetectable = pc.GetComponent<IDetectable>();
        }
    }

    private void Update()
    {
        UpdateAnimator();

        if (currentState == VillagerState.Disabled || IsAffectedBySpell)
        {
            return;
        }

        if (currentState == VillagerState.Wander)
        {
            CheckVision();
        }
    }

    private void FixedUpdate()
    {
        if (currentState == VillagerState.Disabled)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (IsAffectedBySpell)
        {
            HandleSpellMovement();
            return;
        }

        switch (currentState)
        {
            case VillagerState.Wander:
                HandleWanderMovement();
                break;
            case VillagerState.Notice:
            case VillagerState.Scream:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;
            case VillagerState.Frenzy:
                HandleFrenzyMovement();
                break;
            case VillagerState.Cooldown:
                HandleCooldownMovement();
                break;
        }
    }

    // ----------------------------------------------------------------- Visión y Alerta

    private void CheckVision()
    {
        if (playerTarget == null)
        {
            FindPlayer();
            if (playerTarget == null) return;
        }

        // Solo detecta si la bruja es detectable (no en gato, no en maizal/escondite)
        if (playerDetectable != null && !playerDetectable.IsDetectable)
        {
            return;
        }

        float diffX = playerTarget.position.x - transform.position.x;
        bool isAhead = (facingRight && diffX > 0f) || (!facingRight && diffX < 0f);
        float distance = Vector2.Distance(transform.position, playerTarget.position);

        if (distance <= viewDistance && isAhead)
        {
            // Comprobar si hay obstáculo bloqueando la visión
            Vector2 origin = (Vector2)transform.position + Vector2.up * 1.2f;
            Vector2 targetPoint = (Vector2)playerTarget.position + Vector2.up * 0.8f;
            Vector2 dir = (targetPoint - origin).normalized;
            RaycastHit2D hitWall = Physics2D.Raycast(origin, dir, distance, obstacleMask);

            if (hitWall.collider == null)
            {
                // Disparar evento de aproximación para el tutorial
                OnWitchApproaching?.Invoke();
                StartCoroutine(NoticeRoutine());
            }
        }
    }

    private IEnumerator NoticeRoutine()
    {
        currentState = VillagerState.Notice;
        FaceDirection(Mathf.Sign(playerTarget.position.x - transform.position.x));

        // Mostrar signo "!"
        if (noticeIconRenderer != null)
        {
            noticeIconRenderer.enabled = true;
        }

        float elapsed = 0f;
        while (elapsed < noticeDuration)
        {
            elapsed += Time.deltaTime;
            // Si la bruja se transformó en gato o se escondió durante el notice, cancelar
            if (playerDetectable != null && !playerDetectable.IsDetectable)
            {
                if (noticeIconRenderer != null) noticeIconRenderer.enabled = false;
                currentState = VillagerState.Wander;
                yield break;
            }
            yield return null;
        }

        if (noticeIconRenderer != null) noticeIconRenderer.enabled = false;
        StartCoroutine(ScreamRoutine());
    }

    private IEnumerator ScreamRoutine()
    {
        currentState = VillagerState.Scream;

        if (AudioManager.Instance != null && screamSound != null)
        {
            AudioManager.Instance.PlaySFX(screamSound);
        }

        // Alertar a guardias en radio de 14 u
        AlertNearbyGuards();

        yield return new WaitForSeconds(screamDuration);

        // Desencadenar el frenesí general a través de FrenzyController
        FrenzyController frenzy = UnityEngine.Object.FindFirstObjectByType<FrenzyController>();
        if (frenzy != null && playerTarget != null)
        {
            frenzy.TriggerFrenzy(playerTarget);
        }
        else
        {
            EnterFrenzy();
        }
    }

    private void AlertNearbyGuards()
    {
        EnemySight[] allGuards = UnityEngine.Object.FindObjectsByType<EnemySight>(FindObjectsSortMode.None);
        foreach (EnemySight guard in allGuards)
        {
            if (guard == null || guard.IsSuppressed) continue;
            float dist = Vector2.Distance(transform.position, guard.transform.position);
            if (dist <= guardAlertRadius && playerTarget != null)
            {
                // Poner al guardia en persecución hacia la posición de la bruja
                PatrolRoute route = guard.GetComponent<PatrolRoute>();
                if (route != null && !route.IsAffectedBySpell)
                {
                    // Asignar estado de alerta directamente en su EnemySight
                    // Al disparar el evento Alert, el guardia persigue
                    var method = typeof(EnemySight).GetMethod("SetState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (method != null)
                    {
                        method.Invoke(guard, new object[] { AlertState.Alert });
                    }
                }
            }
        }
    }

    public void EnterFrenzy()
    {
        if (IsAffectedBySpell) return;
        currentState = VillagerState.Frenzy;
    }

    public void EndFrenzy()
    {
        if (currentState == VillagerState.Frenzy)
        {
            StartCoroutine(CooldownRoutine());
        }
    }

    private IEnumerator CooldownRoutine()
    {
        currentState = VillagerState.Cooldown;
        yield return new WaitForSeconds(cooldownDuration);
        currentState = VillagerState.Wander;
        SelectRandomPattern();
    }

    // ----------------------------------------------------------------- Movimiento Errático

    private void SelectRandomPattern()
    {
        currentPattern = (WanderPattern)UnityEngine.Random.Range(0, 3);
        patternTimer = UnityEngine.Random.Range(2.5f, 4.5f);
        moveDir = UnityEngine.Random.value > 0.5f ? 1f : -1f;
        lookAroundStep = 0;
    }

    private void HandleWanderMovement()
    {
        patternTimer -= Time.fixedDeltaTime;

        switch (currentPattern)
        {
            case WanderPattern.WalkAndReturn:
                if (!HasGroundAhead(moveDir) || patternTimer <= 0f)
                {
                    moveDir = -moveDir;
                    patternTimer = UnityEngine.Random.Range(2f, 4f);
                }
                MoveHorizontal(moveDir, walkSpeed);
                break;

            case WanderPattern.LookAround:
                if (lookAroundStep == 0) // Caminando
                {
                    if (!HasGroundAhead(moveDir) || patternTimer <= 0f)
                    {
                        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                        lookAroundStep = 1;
                        patternTimer = 0.8f;
                    }
                    else
                    {
                        MoveHorizontal(moveDir, walkSpeed);
                    }
                }
                else if (lookAroundStep == 1) // Mirando a un lado
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    if (patternTimer <= 0f)
                    {
                        FaceDirection(-moveDir);
                        lookAroundStep = 2;
                        patternTimer = 0.8f;
                    }
                }
                else if (lookAroundStep == 2) // Mirando al otro lado
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    if (patternTimer <= 0f)
                    {
                        moveDir = -moveDir;
                        SelectRandomPattern();
                    }
                }
                break;

            case WanderPattern.ShortStops:
                if (patternTimer > 1.2f)
                {
                    if (!HasGroundAhead(moveDir)) moveDir = -moveDir;
                    MoveHorizontal(moveDir, walkSpeed * 0.8f);
                }
                else if (patternTimer > 0f)
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }
                else
                {
                    SelectRandomPattern();
                }
                break;
        }
    }

    private void HandleFrenzyMovement()
    {
        if (playerTarget == null)
        {
            FindPlayer();
            if (playerTarget == null) return;
        }

        float diffX = playerTarget.position.x - transform.position.x;
        float dir = Mathf.Sign(diffX);

        if (!HasGroundAhead(dir))
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            FaceDirection(dir);
            return;
        }

        MoveHorizontal(dir, frenzySpeed);
    }

    private void HandleCooldownMovement()
    {
        // Camina lento hacia adelante mientras se recupera
        if (!HasGroundAhead(moveDir))
        {
            moveDir = -moveDir;
        }
        MoveHorizontal(moveDir, walkSpeed * 0.5f);
    }

    private void MoveHorizontal(float dirX, float speed)
    {
        FaceDirection(dirX);
        rb.linearVelocity = new Vector2(dirX * speed, rb.linearVelocity.y);
    }

    private void FaceDirection(float dirX)
    {
        if (Mathf.Abs(dirX) < 0.01f) return;
        bool wantRight = dirX > 0f;
        if (wantRight != facingRight)
        {
            facingRight = wantRight;
            if (visualTransform != null)
            {
                Vector3 scale = visualTransform.localScale;
                scale.x = Mathf.Abs(scale.x) * (facingRight ? 1f : -1f);
                visualTransform.localScale = scale;
            }
        }
    }

    private bool HasGroundAhead(float dirX)
    {
        Vector2 checkPos = new Vector2(transform.position.x + dirX * edgeCheckOffset, transform.position.y);
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, edgeCheckRayDistance, groundMask);
        return hit.collider != null;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        float speed = Mathf.Abs(rb.linearVelocity.x);
        animator.SetFloat(SpeedHash, speed);
        animator.SetBool(IsScreamingHash, currentState == VillagerState.Scream);
        animator.SetBool(IsFrenzyHash, currentState == VillagerState.Frenzy);
    }

    // ----------------------------------------------------------------- Hechizos (ISpellTarget)

    public void ApplyBlind(float seconds)
    {
        StartSpell(VillagerSpellType.Blind, seconds > 0f ? seconds : 5f);
    }

    public void ApplyHypnotize(float seconds, int direction)
    {
        moveDir = direction != 0 ? Mathf.Sign(direction) : (facingRight ? 1f : -1f);
        FaceDirection(moveDir);
        StartSpell(VillagerSpellType.Hypnotize, seconds > 0f ? seconds : 4f);
    }

    public void ApplySleep(float seconds)
    {
        StartSpell(VillagerSpellType.Sleep, seconds > 0f ? seconds : 6f);
    }

    private void StartSpell(VillagerSpellType type, float duration)
    {
        if (activeSpellCoroutine != null)
        {
            StopCoroutine(activeSpellCoroutine);
        }

        activeSpellType = type;
        if (noticeIconRenderer != null) noticeIconRenderer.enabled = false;

        // Cargar y mostrar icono correspondiente
        Sprite icon = null;
#if UNITY_EDITOR
        string iconName = type switch
        {
            VillagerSpellType.Blind => "Icon_Blinded",
            VillagerSpellType.Hypnotize => "Icon_Spiral",
            VillagerSpellType.Sleep => "Icon_Zzz",
            _ => null
        };
        if (iconName != null)
        {
            icon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Effects/{iconName}.png");
        }
#endif
        if (statusIconRenderer != null)
        {
            statusIconRenderer.sprite = icon;
            statusIconRenderer.enabled = icon != null;
        }

        activeSpellCoroutine = StartCoroutine(SpellDurationRoutine(duration));
    }

    private IEnumerator SpellDurationRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        activeSpellType = VillagerSpellType.None;
        activeSpellCoroutine = null;
        if (statusIconRenderer != null) statusIconRenderer.enabled = false;

        currentState = VillagerState.Wander;
        SelectRandomPattern();
    }

    private void HandleSpellMovement()
    {
        switch (activeSpellType)
        {
            case VillagerSpellType.Sleep:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;
            case VillagerSpellType.Blind:
                // Camina confundido y muy lento
                if (!HasGroundAhead(moveDir)) moveDir = -moveDir;
                MoveHorizontal(moveDir, walkSpeed * 0.35f);
                break;
            case VillagerSpellType.Hypnotize:
                // Camina en la dirección fija impuesta
                if (!HasGroundAhead(moveDir))
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }
                else
                {
                    MoveHorizontal(moveDir, walkSpeed * 0.8f);
                }
                break;
        }
    }
}
