using UnityEngine;

/// <summary>
/// Controla el efecto de desplazamiento en paralaje de una capa de fondo o entorno.
/// Se ejecuta tras la cámara (DefaultExecutionOrder = 100) para evitar tirones visuales.
/// </summary>
[DefaultExecutionOrder(100)]
public class ParallaxLayer : MonoBehaviour
{
    [Header("Cámara")]
    [Tooltip("Transform de la cámara a seguir. Si es nulo, se enlaza automáticamente a Camera.main.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Paralaje")]
    [Tooltip("Factor de paralaje (0.0 = primer plano se mueve con el mundo, 1.0 = fondo infinitamente lejano pegado a la cámara).")]
    [Range(0f, 1f)]
    [SerializeField] private float parallaxFactor = 0.5f;

    [Tooltip("Permite que la capa se desplace y repita infinitamente en el eje horizontal.")]
    [SerializeField] private bool infiniteHorizontal = true;

    [Tooltip("Bloquea la posición vertical para que el fondo no oscile al saltar.")]
    [SerializeField] private bool lockY = true;

    private float startX;
    private float startY;
    private float spriteWidth;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        startX = transform.position.x;
        startY = transform.position.y;

        CalculateSpriteWidth();
    }

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (spriteWidth <= 0.01f)
        {
            CalculateSpriteWidth();
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            else
            {
                return;
            }
        }

        float camX = cameraTransform.position.x;
        float distance = camX * parallaxFactor;
        float temp = camX * (1f - parallaxFactor);

        if (infiniteHorizontal && spriteWidth > 0.01f)
        {
            while (temp > startX + spriteWidth)
            {
                startX += spriteWidth;
            }
            while (temp < startX - spriteWidth)
            {
                startX -= spriteWidth;
            }
        }

        float targetX = startX + distance;
        float targetY = lockY ? startY : startY + (cameraTransform.position.y * parallaxFactor);

        transform.position = new Vector3(targetX, targetY, transform.position.z);
    }

    /// <summary>
    /// Calcula el ancho del sprite ya sea en el objeto actual o en sus hijos.
    /// </summary>
    public void CalculateSpriteWidth()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            spriteWidth = sr.bounds.size.x;
            return;
        }

        // Buscar en hijos
        SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var cr in childRenderers)
        {
            if (cr.gameObject != gameObject && cr.sprite != null)
            {
                spriteWidth = cr.bounds.size.x;
                return;
            }
        }
    }

    /// <summary>
    /// Asigna manualmente la cámara si no es Camera.main.
    /// </summary>
    public void SetCamera(Transform cam)
    {
        cameraTransform = cam;
    }

    /// <summary>
    /// Configura el factor de paralaje y ancho de repetición.
    /// </summary>
    public void Setup(float factor, bool infinite, bool lockVertical)
    {
        parallaxFactor = factor;
        infiniteHorizontal = infinite;
        lockY = lockVertical;
        CalculateSpriteWidth();
    }

    private void OnDrawGizmosSelected()
    {
        if (spriteWidth > 0.01f)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            Gizmos.DrawWireCube(new Vector3(startX, transform.position.y, transform.position.z), new Vector3(spriteWidth, 5f, 0.1f));
        }
    }
}
