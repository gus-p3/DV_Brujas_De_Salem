using System;
using UnityEngine;

/// <summary>
/// Modo de patrulla entre los waypoints asignados.
/// </summary>
public enum PatrolMode
{
    PingPong,
    Loop
}

/// <summary>
/// Estados internos de comportamiento del guardia.
/// </summary>
public enum PatrolState
{
    Patrolling,
    Waiting,
    Chasing,
    Returning,
    Disabled
}

/// <summary>
/// Controlador de patrulla y persecución para el guardia en plano 2D.
/// Se integra con EnemySight para reaccionar a sospecha y alerta, controla
/// la animación y previene caídas al borde de plataformas con detección por Raycast.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PatrolRoute : MonoBehaviour, ISpellTarget
{
    [Header("Ruta de patrulla")]
    [Tooltip("Puntos de patrulla. Solo se utiliza la coordenada X para el movimiento en plano.")]
    [SerializeField] private Transform[] waypoints;
    [Tooltip("Modo de recorrido entre waypoints: PingPong (ida y vuelta) o Loop (cíclico).")]
    [SerializeField] private PatrolMode patrolMode = PatrolMode.PingPong;
    [Tooltip("Velocidad al patrullar normalmente (u/s).")]
    [SerializeField] private float walkSpeed = 2.2f;
    [Tooltip("Velocidad al perseguir a la bruja en estado de alerta (u/s).")]
    [SerializeField] private float chaseSpeed = 3.8f;
    [Tooltip("Tiempo de espera en cada punto antes de continuar (s).")]
    [SerializeField] private float waitTimeAtPoint = 1.5f;
    [Tooltip("Distancia horizontal mínima para considerar alcanzado un waypoint (u).")]
    [SerializeField] private float arrivalTolerance = 0.25f;

    [Header("Detección de bordes y suelo")]
    [Tooltip("Capa de suelo para evitar caer de plataformas.")]
    [SerializeField] private LayerMask groundMask;
    [Tooltip("Distancia horizontal frontal para comprobar si hay suelo delante.")]
    [SerializeField] private float edgeCheckOffset = 0.5f;
    [Tooltip("Longitud del rayo hacia abajo para detectar el suelo.")]
    [SerializeField] private float edgeCheckRayDistance = 1.2f;

    [Header("Captura")]
    [Tooltip("Distancia de captura directa si el guardia alcanza al objetivo.")]
    [SerializeField] private float captureDistance = 1.2f;

    [Header("Referencias")]
    [Tooltip("Componente EnemySight del guardia.")]
    [SerializeField] private EnemySight enemySight;
    [Tooltip("Animator del guardia.")]
    [SerializeField] private Animator animator;
    [Tooltip("Transform visual que se escala en X para voltear.")]
    [SerializeField] private Transform visualTransform;

    [Header("Efectos de Pociones (ISpellTarget)")]
    [Tooltip("Duración por defecto del efecto Cegar (s).")]
    [SerializeField] private float blindDefaultDuration = 5f;
    [Tooltip("Duración por defecto del efecto Hipnotizar (s).")]
    [SerializeField] private float hypnotizeDefaultDuration = 4f;
    [Tooltip("Duración por defecto del efecto Dormir (s).")]
    [SerializeField] private float sleepDefaultDuration = 6f;
    [Tooltip("Renderer para mostrar iconos de estado sobre la cabeza.")]
    [SerializeField] private SpriteRenderer statusIconRenderer;
    [Tooltip("Sprite de icono para el efecto Cegar (ojos tapados).")]
    [SerializeField] private Sprite blindIconSprite;
    [Tooltip("Sprite de icono para el efecto Hipnotizar (espiral).")]
    [SerializeField] private Sprite hypnotizeIconSprite;
    [Tooltip("Sprite de icono para el efecto Dormir (Zzz).")]
    [SerializeField] private Sprite sleepIconSprite;

    [Header("Depuración")]
    [Tooltip("Habilita registros de depuración en consola.")]
    [SerializeField] private bool debugLogs = false;

    // Hashes de parámetros del Animator
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsAlertHash = Animator.StringToHash("IsAlert");

    private enum SpellEffectType { None, Blind, Hypnotize, Sleep }

    private Rigidbody2D rb;
    private PatrolState currentState = PatrolState.Patrolling;
    private int currentWaypointIndex = 0;
    private int patrolDirection = 1; // 1: adelante, -1: atrás (PingPong)
    private float waitTimer = 0f;
    private Transform playerTarget;
    private bool facingRight = true;

    private Coroutine activeSpellCoroutine;
    private SpellEffectType activeSpellType = SpellEffectType.None;
    private float blindWaitTimer = 0f;
    private int hypnotizeDirection = 1;

    public bool IsAffectedBySpell => activeSpellType != SpellEffectType.None;
    public PatrolState CurrentState => currentState;
    public Transform[] Waypoints => waypoints;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (enemySight == null) enemySight = GetComponent<EnemySight>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (visualTransform == null) visualTransform = transform;

        if (statusIconRenderer == null)
        {
            Transform existingIcon = transform.Find("StatusIcon");
            if (existingIcon != null)
            {
                statusIconRenderer = existingIcon.GetComponent<SpriteRenderer>();
            }
            else
            {
                GameObject iconGo = new GameObject("StatusIcon");
                iconGo.transform.SetParent(transform);
                iconGo.transform.localPosition = new Vector3(0f, 2.2f, 0f);
                statusIconRenderer = iconGo.AddComponent<SpriteRenderer>();
                statusIconRenderer.sortingLayerName = "Default";
                statusIconRenderer.sortingOrder = 10;
                statusIconRenderer.enabled = false;
            }
        }

        // Capa Ground por defecto si no se asignó
        if (groundMask.value == 0)
        {
            groundMask = 1 << LayerMask.NameToLayer("Ground");
        }

        // Justificación Rigidbody2D Dynamic:
        // Permite respetar la gravedad natural y caminar sólidamente sobre plataformas y suelo
        // continuo sin requerir simulación de colisión manual, utilizando linearVelocity.x.
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Start()
    {
        FindPlayerTarget();
    }

    private void OnEnable()
    {
        if (enemySight != null)
        {
            enemySight.OnAlertStateChanged += HandleAlertStateChanged;
            enemySight.OnTargetCaptured += HandleTargetCaptured;
        }
    }

    private void OnDisable()
    {
        if (activeSpellCoroutine != null)
        {
            StopCoroutine(activeSpellCoroutine);
            activeSpellCoroutine = null;
        }
        activeSpellType = SpellEffectType.None;
        if (statusIconRenderer != null) statusIconRenderer.enabled = false;

        if (enemySight != null)
        {
            enemySight.SetVisionSuppressed(false);
            enemySight.OnAlertStateChanged -= HandleAlertStateChanged;
            enemySight.OnTargetCaptured -= HandleTargetCaptured;
        }
    }

    private void Update()
    {
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (currentState == PatrolState.Disabled)
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
            case PatrolState.Patrolling:
                HandlePatrolling();
                break;

            case PatrolState.Waiting:
                HandleWaiting();
                break;

            case PatrolState.Chasing:
                HandleChasing();
                break;

            case PatrolState.Returning:
                HandleReturning();
                break;
        }
    }

    private void HandleSpellMovement()
    {
        switch (activeSpellType)
        {
            case SpellEffectType.Sleep:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;

            case SpellEffectType.Blind:
                if (blindWaitTimer > 0f)
                {
                    blindWaitTimer -= Time.fixedDeltaTime;
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }
                else
                {
                    float dir = facingRight ? 1f : -1f;
                    if (!HasGroundAhead(dir))
                    {
                        FaceDirection(-dir);
                        dir = -dir;
                    }
                    MoveHorizontal(dir, walkSpeed * 0.45f);
                }
                break;

            case SpellEffectType.Hypnotize:
                if (!HasGroundAhead(hypnotizeDirection))
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }
                else
                {
                    MoveHorizontal(hypnotizeDirection, walkSpeed * 0.9f);
                }
                break;
        }
    }

    // ----------------------------------------------------------------- Comportamiento

    private void HandlePatrolling()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        Transform targetPoint = waypoints[currentWaypointIndex];
        if (targetPoint == null) return;

        float diffX = targetPoint.position.x - transform.position.x;
        if (Mathf.Abs(diffX) <= arrivalTolerance)
        {
            // Llegó al waypoint: iniciar espera
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            currentState = PatrolState.Waiting;
            waitTimer = waitTimeAtPoint;
            return;
        }

        float dirX = Mathf.Sign(diffX);
        
        // Verificar borde de plataforma o choque con muro antes de avanzar
        if (!HasGroundAhead(dirX) || HasWallAhead(dirX))
        {
            // No hay suelo o hay muro hacia el waypoint: iniciar espera y cambiar de dirección
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            currentState = PatrolState.Waiting;
            waitTimer = waitTimeAtPoint;
            AdvanceWaypoint();
            FaceDirection(-dirX);
            return;
        }

        MoveHorizontal(dirX, walkSpeed);
    }

    private void HandleWaiting()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        waitTimer -= Time.fixedDeltaTime;

        if (waitTimer <= 0f)
        {
            AdvanceWaypoint();
            currentState = PatrolState.Patrolling;
        }
    }

    private void AdvanceWaypoint()
    {
        if (waypoints == null || waypoints.Length <= 1) return;

        if (patrolMode == PatrolMode.Loop)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
        else // PingPong
        {
            currentWaypointIndex += patrolDirection;
            if (currentWaypointIndex >= waypoints.Length)
            {
                currentWaypointIndex = waypoints.Length - 2;
                patrolDirection = -1;
            }
            else if (currentWaypointIndex < 0)
            {
                currentWaypointIndex = 1;
                patrolDirection = 1;
            }
        }
    }

    private void HandleChasing()
    {
        if (playerTarget == null)
        {
            FindPlayerTarget();
            if (playerTarget == null)
            {
                ReturnToPatrol();
                return;
            }
        }

        float diffX = playerTarget.position.x - transform.position.x;
        float dirX = Mathf.Sign(diffX);

        // Comprobar captura por proximidad
        float distToPlayer = Vector2.Distance(transform.position, playerTarget.position);
        if (distToPlayer <= captureDistance)
        {
            TriggerCapture();
            return;
        }

        // Si hay un precipicio delante, detenerse en el borde para no caer
        if (!HasGroundAhead(dirX))
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            FaceDirection(dirX);
            return;
        }

        MoveHorizontal(dirX, chaseSpeed);
    }

    private void HandleReturning()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            currentState = PatrolState.Patrolling;
            return;
        }

        Transform targetPoint = waypoints[currentWaypointIndex];
        if (targetPoint == null) return;

        float diffX = targetPoint.position.x - transform.position.x;
        if (Mathf.Abs(diffX) <= arrivalTolerance)
        {
            currentState = PatrolState.Patrolling;
            return;
        }

        float dirX = Mathf.Sign(diffX);
        if (!HasGroundAhead(dirX))
        {
            // Si el camino de regreso está bloqueado, se reanuda la patrulla desde aquí
            currentState = PatrolState.Patrolling;
            return;
        }

        MoveHorizontal(dirX, walkSpeed);
    }

    private void ReturnToPatrol()
    {
        // Encontrar el waypoint más cercano
        if (waypoints != null && waypoints.Length > 0)
        {
            int bestIdx = 0;
            float bestDist = float.MaxValue;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                float d = Mathf.Abs(waypoints[i].position.x - transform.position.x);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestIdx = i;
                }
            }
            currentWaypointIndex = bestIdx;
        }

        currentState = PatrolState.Returning;
    }

    // ----------------------------------------------------------------- Movimiento y Física

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

            // Escala local en X para orientar visual y cono de visión
            if (visualTransform != null)
            {
                Vector3 scale = visualTransform.localScale;
                scale.x = Mathf.Abs(scale.x) * (facingRight ? 1f : -1f);
                visualTransform.localScale = scale;
            }

            // Sincronizar posición y orientación del farol
            Transform lantern = transform.Find("LanternOrigin");
            if (lantern != null)
            {
                Vector3 lp = lantern.localPosition;
                lp.x = Mathf.Abs(lp.x) * (facingRight ? 1f : -1f);
                lantern.localPosition = lp;
            }
        }
    }

    private bool HasWallAhead(float dirX)
    {
        Vector2 checkOrigin = (Vector2)transform.position + Vector2.up * 0.5f;
        RaycastHit2D hit = Physics2D.Raycast(checkOrigin, Vector2.right * dirX, 0.8f, groundMask);
        return hit.collider != null && !hit.collider.isTrigger;
    }

    private bool HasGroundAhead(float dirX)
    {
        Vector2 checkPos = new Vector2(transform.position.x + dirX * edgeCheckOffset, transform.position.y);
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, edgeCheckRayDistance, groundMask);
        return hit.collider != null;
    }

    private void FindPlayerTarget()
    {
        PlayerController pc = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            playerTarget = pc.transform;
        }
    }

    // ----------------------------------------------------------------- Reacciones a EnemySight

    private void HandleAlertStateChanged(AlertState state)
    {
        if (IsAffectedBySpell) return;

        switch (state)
        {
            case AlertState.Suspicion:
                // Se detiene y mira hacia el objetivo durante la sospecha
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                if (playerTarget != null)
                {
                    FaceDirection(Mathf.Sign(playerTarget.position.x - transform.position.x));
                }
                break;

            case AlertState.Alert:
                currentState = PatrolState.Chasing;
                break;

            case AlertState.Calm:
                if (currentState == PatrolState.Chasing)
                {
                    ReturnToPatrol();
                }
                break;

            case AlertState.Capture:
                TriggerCapture();
                break;
        }
    }

    private void HandleTargetCaptured()
    {
        if (IsAffectedBySpell) return;
        TriggerCapture();
    }

    private void TriggerCapture()
    {
        if (IsAffectedBySpell) return;

        currentState = PatrolState.Disabled;
        rb.linearVelocity = Vector2.zero;
        if (debugLogs)
        {
            Debug.Log($"[PatrolRoute] {name}: ¡Objetivo capturado! Delegando resolución al sistema de captura.", this);
        }

        if (enemySight != null && enemySight.CurrentState != AlertState.Capture)
        {
            enemySight.TriggerCaptureFromPatrol();
        }
    }

    /// <summary>
    /// Restablece la posición del guardia al primer waypoint y reinicia su estado de patrulla a Calma.
    /// </summary>
    public void ResetToFirstWaypoint()
    {
        if (activeSpellCoroutine != null)
        {
            StopCoroutine(activeSpellCoroutine);
            activeSpellCoroutine = null;
        }
        activeSpellType = SpellEffectType.None;

        if (statusIconRenderer != null)
        {
            statusIconRenderer.enabled = false;
        }

        if (waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
        {
            Vector3 startPos = waypoints[0].position;
            transform.position = new Vector3(startPos.x, transform.position.y, transform.position.z);
        }

        currentWaypointIndex = 0;
        patrolDirection = 1;
        waitTimer = 0f;
        currentState = PatrolState.Patrolling;
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (enemySight != null)
        {
            enemySight.SetVisionSuppressed(false);
            enemySight.ResetState();
        }

        UpdateAnimator();
        if (debugLogs)
        {
            Debug.Log($"[PatrolRoute] {name}: reiniciado al primer waypoint.", this);
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        float speed = Mathf.Abs(rb.linearVelocity.x);
        animator.SetFloat(SpeedHash, speed);

        bool isAlert = currentState == PatrolState.Chasing || (enemySight != null && enemySight.CurrentState == AlertState.Alert);
        animator.SetBool(IsAlertHash, isAlert);
    }

    // ----------------------------------------------------------------- Hechizos (ISpellTarget)

    /// <summary>
    /// Aplica ceguera al guardia: el cono se apaga a gris, no detecta, se detiene brevemente y camina lento.
    /// </summary>
    public void ApplyBlind(float seconds)
    {
        float dur = seconds > 0f ? seconds : blindDefaultDuration;
        StartSpell(SpellEffectType.Blind, dur, 0);
    }

    /// <summary>
    /// Aplica hipnosis al guardia: camina en una dirección fija (-1 o +1) sin detectar.
    /// </summary>
    public void ApplyHypnotize(float seconds, int direction)
    {
        float dur = seconds > 0f ? seconds : hypnotizeDefaultDuration;
        int dir = direction != 0 ? (int)Mathf.Sign(direction) : (facingRight ? 1 : -1);
        StartSpell(SpellEffectType.Hypnotize, dur, dir);
    }

    /// <summary>
    /// Aplica sueño al guardia: se detiene por completo con icono Zzz y cono apagado.
    /// </summary>
    public void ApplySleep(float seconds)
    {
        float dur = seconds > 0f ? seconds : sleepDefaultDuration;
        StartSpell(SpellEffectType.Sleep, dur, 0);
    }

    // Métodos previos conservados para compatibilidad
    public void Blind(float seconds) => ApplyBlind(seconds);
    public void Hypnotize(float seconds, int direction) => ApplyHypnotize(seconds, direction);
    public void Sleep(float seconds) => ApplySleep(seconds);

    private void StartSpell(SpellEffectType type, float duration, int dir)
    {
#if UNITY_EDITOR
        AutoAssignSprites();
#endif
        if (activeSpellCoroutine != null)
        {
            StopCoroutine(activeSpellCoroutine);
            activeSpellCoroutine = null;
        }

        activeSpellType = type;
        hypnotizeDirection = dir;

        if (rb == null) rb = GetComponent<Rigidbody2D>();

        if (enemySight != null)
        {
            switch (type)
            {
                case SpellEffectType.Blind:
                    blindWaitTimer = 0.8f;
                    enemySight.SetVisionSuppressed(true, new Color(0.55f, 0.55f, 0.55f, 0.45f));
                    SetStatusIcon(blindIconSprite);
                    break;

                case SpellEffectType.Hypnotize:
                    FaceDirection(dir);
                    enemySight.SetVisionSuppressed(true, new Color(0.7f, 0.35f, 0.95f, 0.45f));
                    SetStatusIcon(hypnotizeIconSprite);
                    break;

                case SpellEffectType.Sleep:
                    if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    enemySight.SetVisionSuppressed(true, null);
                    SetStatusIcon(sleepIconSprite);
                    break;
            }
        }

        if (Application.isPlaying)
        {
            activeSpellCoroutine = StartCoroutine(SpellDurationRoutine(duration));
        }
        if (debugLogs) Debug.Log($"[PatrolRoute] {name}: Hechizo {type} aplicado por {duration} s.", this);
    }

    private System.Collections.IEnumerator SpellDurationRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        EndSpellEffect();
    }

    private void EndSpellEffect()
    {
        activeSpellType = SpellEffectType.None;
        activeSpellCoroutine = null;

        if (statusIconRenderer != null)
        {
            statusIconRenderer.enabled = false;
        }

        if (enemySight != null)
        {
            enemySight.SetVisionSuppressed(false);
            enemySight.ResetToCalm();
        }

        currentState = PatrolState.Patrolling;
        ReturnToPatrol();
    }

    private void SetStatusIcon(Sprite icon)
    {
        if (statusIconRenderer == null) return;
        if (icon != null)
        {
            statusIconRenderer.sprite = icon;
            statusIconRenderer.enabled = true;
        }
        else
        {
            statusIconRenderer.enabled = false;
        }
    }

#if UNITY_EDITOR
    private void Reset()
    {
        AutoAssignSprites();
    }

    private void AutoAssignSprites()
    {
        if (blindIconSprite == null)
            blindIconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/Icon_Blinded.png");
        if (hypnotizeIconSprite == null)
            hypnotizeIconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/Icon_Spiral.png");
        if (sleepIconSprite == null)
            sleepIconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/Icon_Zzz.png");
    }
#endif

    // ----------------------------------------------------------------- Gizmos

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            Vector3 pos = waypoints[i].position;
            Gizmos.DrawWireSphere(pos, 0.35f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(pos + Vector3.up * 0.45f, $"WP {i}");
#endif

            // Conectar con el siguiente
            if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(pos, waypoints[i + 1].position);
            }
            else if (patrolMode == PatrolMode.Loop && waypoints[0] != null)
            {
                Gizmos.color = new Color(0f, 0.8f, 1f, 0.5f);
                Gizmos.DrawLine(pos, waypoints[0].position);
            }
        }
    }
}
