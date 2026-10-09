using UnityEngine;

/// <summary>
/// Cámara 2D que sigue a un objetivo con suavizado (SmoothDamp) y límites opcionales del nivel.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("Transform que la cámara sigue (la bruja).")]
    [SerializeField] private Transform target;
    [Tooltip("Desplazamiento respecto al objetivo (u). Z debe ser negativa.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2.5f, -10f);

    [Header("Suavizado")]
    [Tooltip("Tiempo aproximado que tarda la cámara en alcanzar al objetivo (s).")]
    [SerializeField] private float smoothTime = 0.2f;
    [Tooltip("Velocidad máxima de la cámara (u/s).")]
    [SerializeField] private float maxSpeed = 50f;

    [Header("Límites opcionales del nivel")]
    [Tooltip("Si está activo, la vista de la cámara no sale del rectángulo definido abajo.")]
    [SerializeField] private bool useLimits = false;
    [Tooltip("Esquina inferior izquierda del área visible permitida (mundo).")]
    [SerializeField] private Vector2 minLimit = new Vector2(-20f, -1f);
    [Tooltip("Esquina superior derecha del área visible permitida (mundo).")]
    [SerializeField] private Vector2 maxLimit = new Vector2(20f, 13f);

    private Camera cam;
    private Vector3 velocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        // Arranca ya centrada en el objetivo para evitar un barrido inicial
        if (target != null)
        {
            transform.position = ClampToLimits(target.position + offset);
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desired = ClampToLimits(target.position + offset);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime, maxSpeed);
    }

    /// <summary>Asigna el objetivo desde código (por ejemplo al instanciar a la bruja).</summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
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
