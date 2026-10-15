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
public class PatrolRoute : MonoBehaviour
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

    [Header("Depuración")]
    [Tooltip("Habilita registros de depuración en consola.")]
    [SerializeField] private bool debugLogs = false;

    // Hashes de parámetros del Animator
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsAlertHash = Animator.StringToHash("IsAlert");

    private Rigidbody2D rb;
    private PatrolState currentState = PatrolState.Patrolling;
    private int currentWaypointIndex = 0;
    private int patrolDirection = 1; // 1: adelante, -1: atrás (PingPong)
    private float waitTimer = 0f;
    private Transform playerTarget;
    private bool facingRight = true;

    public PatrolState CurrentState => currentState;
    public Transform[] Waypoints => waypoints;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (enemySight == null) enemySight = GetComponent<EnemySight>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (visualTransform == null) visualTransform = transform;

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
        if (enemySight != null)
        {
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
        
        // Verificar borde de plataforma antes de avanzar
        if (!HasGroundAhead(dirX))
        {
            // No hay suelo hacia el waypoint, iniciar espera y cambiar de dirección
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            currentState = PatrolState.Waiting;
            waitTimer = waitTimeAtPoint;
            AdvanceWaypoint();
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
            Vector3 scale = visualTransform.localScale;
            scale.x = Mathf.Abs(scale.x) * (facingRight ? 1f : -1f);
            visualTransform.localScale = scale;
        }
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
        TriggerCapture();
    }

    private void TriggerCapture()
    {
        currentState = PatrolState.Disabled;
        rb.linearVelocity = Vector2.zero;
        if (debugLogs)
        {
            Debug.Log($"[PatrolRoute] {name}: ¡Objetivo capturado! Delegando resolución al sistema de captura.", this);
        }
    }

    /// <summary>
    /// Restablece la posición del guardia al primer waypoint y reinicia su estado de patrulla a Calma.
    /// </summary>
    public void ResetToFirstWaypoint()
    {
        if (waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
        {
            Vector3 startPos = waypoints[0].position;
            transform.position = new Vector3(startPos.x, transform.position.y, transform.position.z);
        }

        currentWaypointIndex = 0;
        patrolDirection = 1;
        waitTimer = 0f;
        currentState = PatrolState.Patrolling;
        rb.linearVelocity = Vector2.zero;

        if (enemySight != null)
        {
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

    // ----------------------------------------------------------------- Hechizos Futuros

    /// <summary>
    /// Deshabilita temporalmente la visión y movimiento del guardia.
    /// </summary>
    public void Blind(float seconds)
    {
        if (debugLogs) Debug.Log($"[PatrolRoute] {name}: Blind({seconds}) recibido.", this);
    }

    /// <summary>
    /// Obliga al guardia a desplazarse temporalmente en una dirección indicada.
    /// </summary>
    public void Hypnotize(float seconds, int direction)
    {
        if (debugLogs) Debug.Log($"[PatrolRoute] {name}: Hypnotize({seconds}, dir: {direction}) recibido.", this);
    }

    /// <summary>
    /// Duerme al guardia durante un intervalo temporal.
    /// </summary>
    public void Sleep(float seconds)
    {
        if (debugLogs) Debug.Log($"[PatrolRoute] {name}: Sleep({seconds}) recibido.", this);
    }

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
