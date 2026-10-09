using System;
using UnityEngine;

/// <summary>
/// Controlador de la bruja: movimiento horizontal suave, salto con altura variable,
/// coyote time, jump buffer, detección de suelo con OverlapBox y vuelo con gravedad cero.
/// Implementa IDetectable para que los guardias puedan verla.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour, IDetectable
{
    [Header("Movimiento horizontal")]
    [Tooltip("Velocidad horizontal máxima en unidades por segundo.")]
    [SerializeField] private float moveSpeed = 6f;
    [Tooltip("Aceleración al pulsar una dirección (u/s²).")]
    [SerializeField] private float acceleration = 45f;
    [Tooltip("Desaceleración al soltar la dirección (u/s²).")]
    [SerializeField] private float deceleration = 55f;
    [Tooltip("Multiplicador del control horizontal mientras está en el aire (0..1).")]
    [Range(0f, 1f)]
    [SerializeField] private float airControlMultiplier = 0.85f;

    [Header("Salto")]
    [Tooltip("Altura máxima del salto en unidades de mundo (manteniendo el botón).")]
    [SerializeField] private float jumpHeight = 2.8f;
    [Tooltip("Factor que se aplica a la velocidad vertical al soltar el botón mientras sube.")]
    [Range(0f, 1f)]
    [SerializeField] private float jumpCutMultiplier = 0.45f;
    [Tooltip("Gravedad extra al caer para que el salto se sienta ágil.")]
    [SerializeField] private float fallGravityMultiplier = 1.5f;
    [Tooltip("Velocidad máxima de caída (u/s).")]
    [SerializeField] private float maxFallSpeed = 22f;
    [Tooltip("Tiempo extra para saltar tras abandonar una plataforma (s).")]
    [SerializeField] private float coyoteTime = 0.1f;
    [Tooltip("Tiempo que se recuerda un salto pulsado antes de tocar el suelo (s).")]
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Detección de suelo")]
    [Tooltip("Capas que cuentan como suelo (Ground).")]
    [SerializeField] private LayerMask groundMask;
    [Tooltip("Alto de la caja de detección bajo los pies (u).")]
    [SerializeField] private float groundCheckHeight = 0.1f;
    [Tooltip("Ancho de la caja respecto al ancho del collider (0..1) para no detectar paredes.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float groundCheckWidthScale = 0.9f;
    [Tooltip("Separación vertical entre la caja y la base del collider (u).")]
    [SerializeField] private float groundCheckSkin = 0.02f;
    [Tooltip("Velocidad vertical máxima hacia arriba para considerar que está en el suelo (evita saltos infinitos).")]
    [SerializeField] private float groundedMaxUpwardSpeed = 0.1f;

    [Header("Físicas")]
    [Tooltip("Material sin fricción para que no se pegue a las paredes. Si está vacío se crea uno en tiempo de ejecución.")]
    [SerializeField] private PhysicsMaterial2D noFrictionMaterial;

    [Header("Vuelo (gravedad cero)")]
    [Tooltip("Habilita el vuelo. Se activará al reunir las 3 partes de la escoba; expuesto para pruebas.")]
    [SerializeField] private bool canFly = false;
    [Tooltip("Tecla para activar/desactivar el vuelo.")]
    [SerializeField] private KeyCode flightKey = KeyCode.F;
    [Tooltip("Botón alterno (Input Manager) para activar/desactivar el vuelo.")]
    [SerializeField] private string flightAltButton = "Fire2";
    [Tooltip("Velocidad vertical de vuelo (u/s).")]
    [SerializeField] private float flySpeed = 5f;
    [Tooltip("Velocidad horizontal de vuelo (u/s).")]
    [SerializeField] private float flyHorizontalSpeed = 6f;
    [Tooltip("Aceleración de la velocidad al volar (u/s²).")]
    [SerializeField] private float flyAcceleration = 28f;
    [Tooltip("Duración máxima del vuelo con la energía llena (s).")]
    [SerializeField] private float maxFlightDuration = 4f;
    [Tooltip("Energía (0..1) que se recarga por segundo al estar en el suelo.")]
    [SerializeField] private float energyRechargePerSecond = 0.6f;
    [Tooltip("Energía mínima (0..1) necesaria para iniciar el vuelo.")]
    [Range(0f, 1f)]
    [SerializeField] private float minEnergyToStartFlight = 0.05f;
    [Tooltip("Factor de la velocidad vertical al terminar el vuelo (amortigua el impulso).")]
    [Range(0f, 1f)]
    [SerializeField] private float flightExitVerticalDamping = 0.5f;
    [Tooltip("Entrada vertical (negativa) a partir de la cual aterriza al estar tocando suelo.")]
    [SerializeField] private float landingInputThreshold = -0.1f;

    [Header("Animación y orientación")]
    [SerializeField] private Animator animator;
    [Tooltip("SpriteRenderer del personaje (el sprite mira a la derecha por defecto).")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Entrada mínima para considerar un cambio de dirección.")]
    [SerializeField] private float flipDeadZone = 0.01f;

    [Header("Detección (IDetectable)")]
    [Tooltip("Otro componente (p. ej. el gato) puede cambiarlo con SetDetectable.")]
    [SerializeField] private bool isDetectable = true;
    [Tooltip("Punto que usan los guardias para medir distancia. Si está vacío se usa el transform.")]
    [SerializeField] private Transform detectionPoint;

    [Header("Entrada programática (pruebas y cinemáticas)")]
    [Tooltip("Si está activo se ignora el teclado y se usa SetScriptedInput.")]
    [SerializeField] private bool useScriptedInput = false;

    // Nombres de los parámetros del Animator (hash para evitar strings en cada frame)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");
    private static readonly int IsFlyingHash = Animator.StringToHash("IsFlying");

    // Nombres de los ejes y botones del Input Manager
    private const string HorizontalAxis = "Horizontal";
    private const string VerticalAxis = "Vertical";
    private const string JumpButton = "Jump";

    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;

    private float baseGravityScale;
    private float moveInput;
    private float verticalInput;
    private bool facingRight = true;

    private bool isGrounded;
    private bool isJumping;
    private bool jumpReleased;
    private float coyoteCounter;
    private float jumpBufferCounter;

    private bool isFlying;
    private float flightEnergy = 1f;

    // Entrada programática
    private bool scriptedJumpHeld;

    /// <summary>Se dispara cuando el vuelo empieza (true) o termina (false).</summary>
    public event Action<bool> OnFlightChanged;

    /// <summary>Se dispara cuando cambia la energía de vuelo, normalizada de 0 a 1.</summary>
    public event Action<float> OnFlightEnergyChanged;

    /// <summary>Permite volar. Se habilita al reunir las 3 partes de la escoba.</summary>
    public bool CanFly
    {
        get => canFly;
        set
        {
            canFly = value;
            if (!canFly && isFlying)
            {
                StopFlight();
            }
        }
    }

    public bool IsFlying => isFlying;
    public bool IsGrounded => isGrounded;
    public float FlightEnergy01 => flightEnergy;

    // --- Implementación de IDetectable ---
    public bool IsDetectable 
    {
        get
        {
            WitchDetectable wd = GetComponent<WitchDetectable>();
            return wd != null ? wd.IsDetectable : isDetectable;
        }
    }
    
    public Transform DetectionPoint 
    {
        get
        {
            WitchDetectable wd = GetComponent<WitchDetectable>();
            if (wd != null && wd.DetectionPoint != transform) return wd.DetectionPoint;
            return detectionPoint != null ? detectionPoint : transform;
        }
    }

    /// <summary>Permite que otro componente (por ejemplo el gato en el maizal) cambie la detectabilidad.</summary>
    public void SetDetectable(bool value)
    {
        isDetectable = value;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        baseGravityScale = rb.gravityScale;

        // Colisiones coherentes: continuas, interpoladas y sin rotación
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Material sin fricción para que no se pegue a las paredes
        if (noFrictionMaterial == null)
        {
            noFrictionMaterial = new PhysicsMaterial2D("Witch_NoFriction_Runtime")
            {
                friction = 0f,
                bounciness = 0f
            };
        }
        boxCollider.sharedMaterial = noFrictionMaterial;
    }

    private void Start()
    {
        // Estado de suelo inicial para que el Animator no arranque en Jump durante un frame
        CheckGround();
        OnFlightEnergyChanged?.Invoke(flightEnergy);
    }

    private void OnDisable()
    {
        // Si se desactiva en pleno vuelo, se restaura la gravedad original
        if (isFlying && rb != null)
        {
            rb.gravityScale = baseGravityScale;
            isFlying = false;
        }
    }

    private void Update()
    {
        ReadInput();

        // Si se revoca CanFly desde el Inspector mientras vuela, se cancela el vuelo
        if (!canFly && isFlying)
        {
            StopFlight();
        }

        UpdateFacing();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        CheckGround();
        UpdateTimers();

        if (isFlying)
        {
            HandleFlight();
        }
        else
        {
            HandleHorizontalMovement();
            HandleJump();
            ApplyFallGravity();
            RechargeFlightEnergy();
        }
    }

    // ----------------------------------------------------------------- Entrada

    private void ReadInput()
    {
        if (useScriptedInput)
        {
            return; // moveInput, verticalInput y el salto los fija SetScriptedInput
        }

        moveInput = Input.GetAxisRaw(HorizontalAxis);
        verticalInput = Input.GetAxisRaw(VerticalAxis);

        if (Input.GetButtonDown(JumpButton))
        {
            jumpBufferCounter = jumpBufferTime;
        }

        if (Input.GetButtonUp(JumpButton))
        {
            jumpReleased = true;
        }

        if (Input.GetKeyDown(flightKey) || (!string.IsNullOrEmpty(flightAltButton) && Input.GetButtonDown(flightAltButton)))
        {
            ToggleFlight();
        }
    }

    /// <summary>Fija la entrada manualmente (pruebas automáticas y cinemáticas). Requiere useScriptedInput.</summary>
    public void SetScriptedInput(float horizontal, float vertical)
    {
        moveInput = horizontal;
        verticalInput = vertical;
    }

    /// <summary>Simula pulsar el botón de salto (requiere useScriptedInput).</summary>
    public void ScriptedJumpPressed()
    {
        scriptedJumpHeld = true;
        jumpBufferCounter = jumpBufferTime;
    }

    /// <summary>Simula soltar el botón de salto (requiere useScriptedInput).</summary>
    public void ScriptedJumpReleased()
    {
        if (scriptedJumpHeld)
        {
            jumpReleased = true;
        }
        scriptedJumpHeld = false;
    }

    /// <summary>Activa el modo de entrada programática.</summary>
    public void EnableScriptedInput(bool enabledValue)
    {
        useScriptedInput = enabledValue;
        moveInput = 0f;
        verticalInput = 0f;
    }

    /// <summary>Alterna el vuelo (equivale a pulsar F).</summary>
    public void ToggleFlight()
    {
        if (isFlying)
        {
            StopFlight();
        }
        else if (canFly && flightEnergy >= minEnergyToStartFlight)
        {
            StartFlight();
        }
    }

    // ------------------------------------------------------------------ Suelo

    private void CheckGround()
    {
        GetGroundCheckBox(out Vector2 center, out Vector2 size);
        bool overlapping = Physics2D.OverlapBox(center, size, 0f, groundMask) != null;

        // Si va subiendo rápido no cuenta como suelo: evita volver a saltar en pleno impulso
        isGrounded = overlapping && rb.linearVelocity.y <= groundedMaxUpwardSpeed;
    }

    private void GetGroundCheckBox(out Vector2 center, out Vector2 size)
    {
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();

        Bounds bounds = boxCollider.bounds;
        size = new Vector2(bounds.size.x * groundCheckWidthScale, groundCheckHeight);
        center = new Vector2(bounds.center.x, bounds.min.y - groundCheckSkin - groundCheckHeight * 0.5f);
    }

    private void UpdateTimers()
    {
        coyoteCounter = isGrounded ? coyoteTime : coyoteCounter - Time.fixedDeltaTime;
        jumpBufferCounter -= Time.fixedDeltaTime;
    }

    // ------------------------------------------------------------- Movimiento

    private void HandleHorizontalMovement()
    {
        float targetSpeed = moveInput * moveSpeed;
        float rate = Mathf.Abs(moveInput) > flipDeadZone ? acceleration : deceleration;
        if (!isGrounded)
        {
            rate *= airControlMultiplier;
        }

        Vector2 velocity = rb.linearVelocity;
        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, rate * Time.fixedDeltaTime);
        rb.linearVelocity = velocity;
    }

    private void HandleJump()
    {
        Vector2 velocity = rb.linearVelocity;

        // Salto: requiere salto recordado (buffer) y estar en suelo o dentro del coyote time
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            float gravity = Mathf.Abs(Physics2D.gravity.y) * baseGravityScale;
            velocity.y = Mathf.Sqrt(2f * gravity * jumpHeight);
            isJumping = true;
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        // Altura variable: soltar el botón corta el salto mientras sube
        if (jumpReleased)
        {
            if (isJumping && velocity.y > 0f)
            {
                velocity.y *= jumpCutMultiplier;
            }
            jumpReleased = false;
        }

        if (velocity.y <= 0f)
        {
            isJumping = false;
        }

        rb.linearVelocity = velocity;
    }

    private void ApplyFallGravity()
    {
        Vector2 velocity = rb.linearVelocity;

        if (velocity.y < 0f)
        {
            velocity.y += Physics2D.gravity.y * baseGravityScale * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
            velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
        }

        rb.linearVelocity = velocity;
    }

    // ------------------------------------------------------------------ Vuelo

    private void StartFlight()
    {
        isFlying = true;
        isJumping = false;
        rb.gravityScale = 0f;
        Vector2 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;
        OnFlightChanged?.Invoke(true);
    }

    private void StopFlight()
    {
        if (!isFlying)
        {
            return;
        }

        isFlying = false;
        rb.gravityScale = baseGravityScale;
        Vector2 velocity = rb.linearVelocity;
        velocity.y *= flightExitVerticalDamping;
        rb.linearVelocity = velocity;
        OnFlightChanged?.Invoke(false);
    }

    private void HandleFlight()
    {
        // Aterrizaje voluntario: tocando suelo y pulsando hacia abajo
        if (isGrounded && verticalInput < landingInputThreshold)
        {
            StopFlight();
            return;
        }

        // Consumo de energía
        flightEnergy = Mathf.Max(0f, flightEnergy - Time.fixedDeltaTime / maxFlightDuration);
        OnFlightEnergyChanged?.Invoke(flightEnergy);

        if (flightEnergy <= 0f)
        {
            StopFlight();
            return;
        }

        Vector2 target = new Vector2(moveInput * flyHorizontalSpeed, verticalInput * flySpeed);
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, target, flyAcceleration * Time.fixedDeltaTime);
    }

    private void RechargeFlightEnergy()
    {
        if (!isGrounded || flightEnergy >= 1f)
        {
            return;
        }

        flightEnergy = Mathf.Min(1f, flightEnergy + energyRechargePerSecond * Time.fixedDeltaTime);
        OnFlightEnergyChanged?.Invoke(flightEnergy);
    }

    // ------------------------------------------------- Orientación y animación

    private void UpdateFacing()
    {
        if (Mathf.Abs(moveInput) > flipDeadZone)
        {
            facingRight = moveInput > 0f;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !facingRight;
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        Vector2 velocity = rb.linearVelocity;
        animator.SetFloat(SpeedHash, Mathf.Abs(velocity.x));
        animator.SetBool(IsGroundedHash, isGrounded);
        animator.SetFloat(VerticalVelocityHash, velocity.y);
        animator.SetBool(IsFlyingHash, isFlying);
    }

    // ----------------------------------------------------------------- Gizmos

    private void OnDrawGizmos()
    {
        GetGroundCheckBox(out Vector2 center, out Vector2 size);
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(center, size);
    }
}
