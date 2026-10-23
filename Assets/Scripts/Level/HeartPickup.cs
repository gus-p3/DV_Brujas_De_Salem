using UnityEngine;

/// <summary>
/// Ítem coleccionable en el mundo que recupera un corazón si la bruja no tiene
/// la vida completa. Descansa sobre el suelo (GroundAnchor) con un brillo y avisa en pantalla si la vida ya está llena.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HeartPickup : MonoBehaviour
{
    [Header("Efectos")]
    [Tooltip("Partículas instanciadas al ser recogido.")]
    [SerializeField] private ParticleSystem pickupParticles;

    [Tooltip("Clip de audio al recogerse (si está vacío usa el de AudioManager).")]
    [SerializeField] private AudioClip pickupSound;

    [Header("Avisos")]
    [Tooltip("Tiempo mínimo (s) entre repeticiones del aviso 'Corazones al máximo'.")]
    [SerializeField] private float fullHealthMessageCooldown = 3.0f;

    private static float lastFullMessageTime = -10f;
    private bool isCollected = false;

    private void Start()
    {
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Foreground";
            sr.sortingOrder = 10;
        }

        // La altura la fija GroundAnchor; el brillo sustituye a la antigua flotación
        PickupGlint glint = GetComponent<PickupGlint>();
        if (glint == null) glint = gameObject.AddComponent<PickupGlint>();
        glint.SetColor(new Color(1f, 0.45f, 0.4f, 1f));
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null && !other.CompareTag("Player")) return;

        if (GameManager.Instance == null) return;

        // Comprobar si los corazones están al máximo
        if (GameManager.Instance.CurrentHearts >= GameManager.Instance.MaxHearts)
        {
            if (Time.unscaledTime - lastFullMessageTime >= fullHealthMessageCooldown)
            {
                lastFullMessageTime = Time.unscaledTime;
                HUDController hud = Object.FindFirstObjectByType<HUDController>();
                if (hud != null)
                {
                    hud.ShowMessage("Corazones al máximo", 2.0f);
                }
            }
            return;
        }

        // Recoger corazón
        isCollected = true;
        bool gained = GameManager.Instance.GainHeart(1);
        if (!gained)
        {
            isCollected = false;
            return;
        }

        // Suma 0 puntos como especifica la regla A8
        GameManager.Instance.AddScore(0);

        // Disparar tip de tutorial la primera vez (Requisito C8)
        if (TutorialManager.Instance != null && !TutorialManager.Instance.HasSeen("tip_corazon_recogido"))
        {
            TutorialManager.Instance.ShowTip(new TutorialTip(
                "tip_corazon_recogido",
                "Corazón Recogido",
                "¡Recuperaste un corazón! Los corazones caídos te permiten sanar heridas antes de que la hoguera te alcance.",
                null,
                true
            ));
        }

        // Partículas
        if (pickupParticles != null)
        {
            pickupParticles.transform.SetParent(null);
            pickupParticles.Play();
            Destroy(pickupParticles.gameObject, 1.5f);
        }

        // Sonido
        if (AudioManager.Instance != null)
        {
            AudioClip clip = pickupSound != null ? pickupSound : AudioManager.Instance.heartHealClip;
            AudioManager.Instance.PlaySFX(clip);
        }

        Destroy(gameObject);
    }
}
