using UnityEngine;

/// <summary>
/// Cámara 2D de plataformas (estilo Hollow Knight) con límites opcionales del nivel:
/// - Anticipación horizontal: se adelanta hacia donde mira y se mueve la bruja, para ver lo que viene.
/// - Zona muerta vertical: los saltos pequeños no mueven la cámara; sigue la altura del último suelo
///   pisado y solo acompaña al subir mucho o al caer por debajo de ese suelo (rápido, para ver dónde cae).
/// - Temblor de pantalla (Shake) para impactos, con ruido suave en lugar de saltos aleatorios.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("Transform que la cámara sigue (la bruja).")]
    [SerializeField] private Transform target;
    [Tooltip("Desplazamiento respecto al objetivo (u). Z debe ser negativa.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2.4f, -10f);

    [Header("Suavizado")]
    [Tooltip("Tiempo aproximado que tarda la cámara en alcanzar al objetivo en horizontal (s).")]
    [SerializeField] private float smoothTime = 0.18f;
    [Tooltip("Tiempo de suavizado vertical (s).")]
    [SerializeField] private float verticalSmoothTime = 0.28f;
    [Tooltip("Tiempo de suavizado vertical al caer (s): más rápido para ver el aterrizaje.")]
    [SerializeField] private float fallSmoothTime = 0.1f;
    [Tooltip("Velocidad máxima de la cámara (u/s).")]
    [SerializeField] private float maxSpeed = 50f;

    [Header("Anticipación horizontal")]
    [Tooltip("Distancia que la cámara se adelanta hacia donde mira la bruja (u).")]
    [SerializeField] private float lookAhead = 1.8f;
    [Tooltip("Tiempo que tarda la anticipación en cambiar de lado (s).")]
    [SerializeField] private float lookAheadSmoothTime = 0.6f;

    [Header("Zona muerta vertical")]
    [Tooltip("Altura sobre el último suelo que puede subir la bruja sin que la cámara la siga (u).")]
    [SerializeField] private float verticalDeadZone = 2.2f;

    [Header("Límites opcionales del nivel")]
    [Tooltip("Si está activo, la vista de la cámara no sale del rectángulo definido abajo.")]
    [SerializeField] private bool useLimits = false;
    [Tooltip("Esquina inferior izquierda del área visible permitida (mundo).")]
    [SerializeField] private Vector2 minLimit = new Vector2(-20f, -1.6f);
    [Tooltip("Esquina superior derecha del área visible permitida (mundo).")]
    [SerializeField] private Vector2 maxLimit = new Vector2(235f, 15f);

    private static CameraFollow instance;

    private Camera cam;
    private PlayerController player;
    private Rigidbody2D body;
    private float velocityX, velocityY;
    private float lookAheadCurrent, lookAheadVelocity;
    private float groundY;
    private Vector3 basePosition;

    private float shakeTime, shakeDuration, shakeAmplitude;
    private float shakeSeed;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        instance = this;
        shakeSeed = Random.value * 100f;
    }

    private void Start()
    {
        ResolvePlayer();
        // Arranca ya centrada en el objetivo para evitar un barrido inicial
        if (target != null)
        {
            groundY = target.position.y;
            lookAheadCurrent = player != null ? player.FacingDirection * lookAhead : 0f;
            basePosition = ClampToLimits(new Vector3(target.position.x + lookAheadCurrent, groundY, 0f) + offset);
            transform.position = basePosition;
        }
    }

    private void ResolvePlayer()
    {
        if (target == null) return;
        player = target.GetComponent<PlayerController>();
        body = target.GetComponent<Rigidbody2D>();
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (player == null && body == null) ResolvePlayer();

        float dt = Time.deltaTime;
        Vector3 p = target.position;

        // Anticipación: hacia donde mira; algo más si además se desplaza rápido en esa dirección
        float facing = player != null ? player.FacingDirection : 1f;
        float vx = body != null ? body.linearVelocity.x : 0f;
        float ahead = facing * lookAhead * Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(Mathf.Abs(vx) / 5f));
        lookAheadCurrent = Mathf.SmoothDamp(lookAheadCurrent, ahead, ref lookAheadVelocity, lookAheadSmoothTime);

        // Altura de referencia: el último suelo pisado (o la bruja si vuela, sube mucho o cae por debajo)
        bool grounded = player == null || player.IsGrounded;
        bool flying = player != null && player.IsFlying;
        if (grounded || flying) groundY = p.y;
        else if (p.y > groundY + verticalDeadZone) groundY = p.y - verticalDeadZone;
        else if (p.y < groundY) groundY = p.y;
        bool falling = body != null && body.linearVelocity.y < -1f && p.y < groundY + 0.01f;

        Vector3 desired = ClampToLimits(new Vector3(p.x + lookAheadCurrent, groundY, 0f) + offset);
        basePosition.x = Mathf.SmoothDamp(basePosition.x, desired.x, ref velocityX, smoothTime, maxSpeed, dt);
        basePosition.y = Mathf.SmoothDamp(basePosition.y, desired.y, ref velocityY, falling ? fallSmoothTime : verticalSmoothTime, maxSpeed, dt);
        basePosition.z = desired.z;

        transform.position = basePosition + ShakeOffset(Time.unscaledDeltaTime);
    }

    public Vector2 MinLimit => minLimit;
    public Vector2 MaxLimit => maxLimit;

    /// <summary>Hace temblar la cámara activa. La intensidad se combina con un temblor en curso (no se suma).</summary>
    public static void Shake(float amplitude, float duration)
    {
        if (instance == null) return;
        float remaining = instance.shakeTime < instance.shakeDuration
            ? instance.shakeAmplitude * (1f - instance.shakeTime / instance.shakeDuration)
            : 0f;
        if (remaining > amplitude) return;
        instance.shakeAmplitude = amplitude;
        instance.shakeDuration = duration;
        instance.shakeTime = 0f;
    }

    private Vector3 ShakeOffset(float dt)
    {
        if (shakeTime >= shakeDuration) return Vector3.zero;
        shakeTime += dt;
        float k = 1f - Mathf.Clamp01(shakeTime / shakeDuration);
        float a = shakeAmplitude * k * k;
        float t = Time.unscaledTime * 28f;
        return new Vector3((Mathf.PerlinNoise(shakeSeed, t) - 0.5f) * 2f * a, (Mathf.PerlinNoise(t, shakeSeed) - 0.5f) * 2f * a, 0f);
    }

    /// <summary>Centra la cámara de inmediato en el objetivo sin suavizado (para reapariciones).</summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        velocityX = velocityY = lookAheadVelocity = 0f;
        groundY = target.position.y;
        lookAheadCurrent = player != null ? player.FacingDirection * lookAhead : 0f;
        basePosition = ClampToLimits(new Vector3(target.position.x + lookAheadCurrent, groundY, 0f) + offset);
        transform.position = basePosition;
    }

    /// <summary>Asigna el objetivo desde código (por ejemplo al instanciar a la bruja).</summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        ResolvePlayer();
    }

    /// <summary>Restringe la posición para que el área visible quede dentro de los límites.</summary>
    private Vector3 ClampToLimits(Vector3 position)
    {
        if (!useLimits || cam == null)
        {
            return position;
        }

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        // Si el nivel es más pequeño que la vista, se centra en vez de invertir los límites
        float minX = minLimit.x + halfWidth;
        float maxX = maxLimit.x - halfWidth;
        float minY = minLimit.y + halfHeight;
        float maxY = maxLimit.y - halfHeight;

        position.x = minX > maxX ? (minLimit.x + maxLimit.x) * 0.5f : Mathf.Clamp(position.x, minX, maxX);
        position.y = minY > maxY ? (minLimit.y + maxLimit.y) * 0.5f : Mathf.Clamp(position.y, minY, maxY);
        return position;
    }

    private void OnDrawGizmosSelected()
    {
        if (!useLimits)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Vector3 center = (minLimit + maxLimit) * 0.5f;
        Vector3 size = maxLimit - minLimit;
        Gizmos.DrawWireCube(center, size);
    }
}
