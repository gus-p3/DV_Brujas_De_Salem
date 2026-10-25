using UnityEngine;

/// <summary>
/// Controla el efecto de desplazamiento en paralaje de una capa de fondo o entorno.
/// Se ejecuta tras la cámara (DefaultExecutionOrder = 100) para evitar tirones visuales.
///
/// CORRECCIÓN (fix/nivel2 parallax-drift):
///   - spriteWidth ahora se calcula en world-space usando Renderer.bounds (incluye scale).
///   - startX se almacena en world-space una sola vez en Awake; no se recalcula en Start.
///   - El wrap infinito mueve startX en pasos exactos de spriteWidth sin drift acumulativo.
///   - lastCamX registra la posición previa de la cámara; el desplazamiento es incremental
///     para que el bucle while nunca salte más de un ancho por frame aunque haya lag.
/// </summary>
[DefaultExecutionOrder(100)]
public class ParallaxLayer : MonoBehaviour
{
    [Header("Cámara")]
    [Tooltip("Transform de la cámara a seguir. Si es nulo, se enlaza automáticamente a Camera.main.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Paralaje")]
    [Tooltip("Factor de paralaje (0 = fijo al mundo, 1 = pegado a la cámara).")]
    [Range(0f, 1f)]
    [SerializeField] private float parallaxFactor = 0.5f;

    [Tooltip("Repite la capa infinitamente en horizontal.")]
    [SerializeField] private bool infiniteHorizontal = true;

    [Tooltip("Bloquea Y para evitar oscilación vertical.")]
    [SerializeField] private bool lockY = true;

    // Estado interno
    private float startX;          // posición world-X de referencia (se ajusta con wrap)
    private float startY;          // posición world-Y fija
    private float spriteWidth;     // ancho en world-units incluyendo scale del objeto
    private float lastCamX;        // posición X de la cámara en el frame anterior
    private bool  initialized;

    // ---------------------------------------------------------------
    private void Awake()
    {
        ResolveCamera();
        CaptureStart();
    }

    private void Start()
    {
        ResolveCamera();
        if (!initialized) CaptureStart();
    }

    private void ResolveCamera()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void CaptureStart()
    {
        // Tomar posición world actual como origen
        startX = transform.position.x;
        startY = transform.position.y;
        lastCamX = cameraTransform != null ? cameraTransform.position.x : 0f;
        CalculateSpriteWidth();
        initialized = true;
    }

    // ---------------------------------------------------------------
    private void LateUpdate()
    {
        ResolveCamera();
        if (cameraTransform == null) return;

        float camX  = cameraTransform.position.x;
        float delta = camX - lastCamX;           // cuánto se movió la cámara este frame
        lastCamX    = camX;

        // Mover la capa proporcionalmente al movimiento de la cámara
        startX += delta * parallaxFactor;

        // Wrap horizontal: mantener startX dentro de un rango de un ancho de sprite
        if (infiniteHorizontal && spriteWidth > 0.01f)
        {
            // Distancia entre la cámara y el origen de la capa
            float diff = camX - startX;
            if (diff > spriteWidth)
                startX += Mathf.Floor(diff / spriteWidth) * spriteWidth;
            else if (diff < -spriteWidth)
                startX -= Mathf.Floor(-diff / spriteWidth) * spriteWidth;
        }

        float targetY = lockY ? startY : transform.position.y;
        transform.position = new Vector3(startX, targetY, transform.position.z);
    }

    // ---------------------------------------------------------------
    /// <summary>
    /// Calcula el ancho en world-units del sprite (o primer hijo con sprite).
    /// Usa Renderer.bounds que ya incorpora el scale del Transform.
    /// </summary>
    public void CalculateSpriteWidth()
    {
        // Probar en este mismo GameObject
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            spriteWidth = sr.bounds.size.x;
            return;
        }

        // Buscar en hijos directos
        foreach (var cr in GetComponentsInChildren<SpriteRenderer>())
        {
            if (cr.sprite != null)
            {
                spriteWidth = cr.bounds.size.x;
                return;
            }
        }

        // Fallback: estimar desde el tamaño del sprite sin renderizador encontrado
        spriteWidth = 0f;
    }

    /// <summary>Asigna manualmente la cámara si no es Camera.main.</summary>
    public void SetCamera(Transform cam)
    {
        cameraTransform = cam;
        lastCamX = cam != null ? cam.position.x : 0f;
    }

    /// <summary>Configura factor de paralaje y modo de repetición en runtime.</summary>
    public void Setup(float factor, bool infinite, bool lockVertical)
    {
        parallaxFactor    = factor;
        infiniteHorizontal = infinite;
        lockY             = lockVertical;
        CalculateSpriteWidth();
    }

    private void OnDrawGizmosSelected()
    {
        if (spriteWidth > 0.01f)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            Gizmos.DrawWireCube(
                new Vector3(startX, transform.position.y, transform.position.z),
                new Vector3(spriteWidth, 5f, 0.1f));
        }
    }
}
