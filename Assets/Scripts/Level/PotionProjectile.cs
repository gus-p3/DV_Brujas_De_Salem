using UnityEngine;

/// <summary>
/// Tipo de poción arrojable y su efecto asociado.
/// </summary>
public enum PotionType
{
    Blind = 0,
    Hypnotize = 1,
    Sleep = 2
}

/// <summary>
/// Proyectil de poción lanzado en arco por la bruja.
/// Al impactar contra el suelo o un enemigo, genera un área de salpicadura de radio 1.6 u
/// y aplica el efecto mágico correspondiente a todos los ISpellTarget alcanzados.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PotionProjectile : MonoBehaviour
{
    [Header("Configuración de Poción")]
    [Tooltip("Tipo de poción mágica que representa este proyectil.")]
    [SerializeField] private PotionType potionType = PotionType.Blind;
    [Tooltip("Duración del efecto aplicado en segundos.")]
    [SerializeField] private float effectDuration = 5f;
    [Tooltip("Radio del área de efecto de salpicadura al colisionar (u).")]
    [SerializeField] private float splashRadius = 1.6f;

    [Header("Físicas de Lanzamiento")]
    [Tooltip("Velocidad horizontal de lanzamiento.")]
    [SerializeField] private float horizontalSpeed = 7f;
    [Tooltip("Velocidad vertical inicial del arco.")]
    [SerializeField] private float verticalSpeed = 6f;
    [Tooltip("Velocidad angular de rotación en vuelo (grados/s).")]
    [SerializeField] private float rotationSpeed = 360f;

    [Header("Efectos Visuales")]
    [Tooltip("Prefab del efecto visual de salpicadura.")]
    [SerializeField] private GameObject splashPrefab;
    [Tooltip("Color de la salpicadura en caso de dibujado de partículas o gizmos.")]
    [SerializeField] private Color splashColor = Color.white;

    private Rigidbody2D rb;
    private bool hasImpacted = false;
    private int launchDirection = 1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 2.2f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    /// <summary>
    /// Inicializa la trayectoria parabólica del proyectil según la dirección orientada por la bruja.
    /// </summary>
    public void Launch(int facingDirection)
    {
        launchDirection = facingDirection >= 0 ? 1 : -1;
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        rb.linearVelocity = new Vector2(launchDirection * horizontalSpeed, verticalSpeed);
        rb.angularVelocity = -launchDirection * rotationSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleImpact(collision.contacts.Length > 0 ? collision.contacts[0].point : (Vector2)transform.position);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger) return;
        HandleImpact(transform.position);
    }

    private void HandleImpact(Vector2 impactPoint)
    {
        if (hasImpacted) return;
        hasImpacted = true;

        SpawnSplashEffect(impactPoint);
        ApplySplashDamage(impactPoint);

        Destroy(gameObject);
    }

    private void SpawnSplashEffect(Vector2 position)
    {
        if (splashPrefab != null)
        {
            Instantiate(splashPrefab, position, Quaternion.identity);
            return;
        }

        // Si no hay prefab asignado, se genera un GameObject temporal con SpriteRenderer animado
        GameObject splashObj = new GameObject("SplashEffect_" + potionType);
        splashObj.transform.position = position;
        SpriteRenderer sr = splashObj.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 15;

        // Cargar los 4 frames de la salpicadura
        string spriteName = potionType switch
        {
            PotionType.Blind => "Splash_Blind",
            PotionType.Hypnotize => "Splash_Hypnotize",
            PotionType.Sleep => "Splash_Sleep",
            _ => "Splash_Blind"
        };

        Sprite[] frames = Resources.FindObjectsOfTypeAll<Sprite>();
        System.Collections.Generic.List<Sprite> animFrames = new System.Collections.Generic.List<Sprite>();
        foreach (Sprite s in frames)
        {
            if (s.name.StartsWith(spriteName + "_") || s.name == spriteName)
            {
                animFrames.Add(s);
            }
        }
        animFrames.Sort((a, b) => string.Compare(a.name, b.name));

        SplashAnimator animator = splashObj.AddComponent<SplashAnimator>();
        animator.Initialize(animFrames.ToArray(), 12f);
    }

    private void ApplySplashDamage(Vector2 center)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, splashRadius);
        System.Collections.Generic.HashSet<ISpellTarget> affectedTargets = new System.Collections.Generic.HashSet<ISpellTarget>();

        foreach (Collider2D hit in hits)
        {
            ISpellTarget target = hit.GetComponentInParent<ISpellTarget>();
            if (target != null && !affectedTargets.Contains(target))
            {
                affectedTargets.Add(target);

                switch (potionType)
                {
                    case PotionType.Blind:
                        target.ApplyBlind(effectDuration > 0f ? effectDuration : 5f);
                        break;

                    case PotionType.Hypnotize:
                        target.ApplyHypnotize(effectDuration > 0f ? effectDuration : 4f, launchDirection);
                        break;

                    case PotionType.Sleep:
                        target.ApplySleep(effectDuration > 0f ? effectDuration : 6f);
                        break;
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = splashColor;
        Gizmos.DrawWireSphere(transform.position, splashRadius);
    }
}

/// <summary>
/// Reproduce la animación de salpicadura de 4 fotogramas y destruye el GameObject al finalizar.
/// </summary>
public class SplashAnimator : MonoBehaviour
{
    private Sprite[] frames;
    private float fps = 12f;
    private SpriteRenderer sr;
    private float timer = 0f;
    private int currentFrame = 0;

    public void Initialize(Sprite[] splashFrames, float animationFps)
    {
        frames = splashFrames;
        fps = animationFps;
        sr = GetComponent<SpriteRenderer>();
        if (frames != null && frames.Length > 0 && sr != null)
        {
            sr.sprite = frames[0];
        }
        else
        {
            Destroy(gameObject, 0.4f);
        }
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0 || sr == null) return;

        timer += Time.deltaTime;
        if (timer >= 1f / fps)
        {
            timer = 0f;
            currentFrame++;
            if (currentFrame < frames.Length)
            {
                sr.sprite = frames[currentFrame];
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
