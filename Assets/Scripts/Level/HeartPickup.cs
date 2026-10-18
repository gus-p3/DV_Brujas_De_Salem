using UnityEngine;

/// <summary>
/// Ítem coleccionable en el mundo que recupera un corazón si la bruja no tiene
/// la vida completa. Presenta flotación suave y aviso en pantalla si ya está llena.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HeartPickup : MonoBehaviour
{
    [Header("Flotación Visual")]
    [Tooltip("Amplitud del movimiento vertical suave (u).")]
    [SerializeField] private float bobAmplitude = 0.04f;

    [Tooltip("Velocidad de oscilación senoidal (ciclos/seg).")]
    [SerializeField] private float bobSpeed = 2.5f;

    [Header("Efectos")]
    [Tooltip("Partículas instanciadas al ser recogido.")]
    [SerializeField] private ParticleSystem pickupParticles;

    [Tooltip("Clip de audio al recogerse (si está vacío usa el de AudioManager).")]
    [SerializeField] private AudioClip pickupSound;

    [Header("Avisos")]
    [Tooltip("Tiempo mínimo (s) entre repeticiones del aviso 'Corazones al máximo'.")]
    [SerializeField] private float fullHealthMessageCooldown = 3.0f;

    private static float lastFullMessageTime = -10f;
    private Vector3 initialPosition;
    private bool isCollected = false;

    private void Start()
    {
        if (bobAmplitude > 0.05f) bobAmplitude = 0.04f;

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Foreground";
            sr.sortingOrder = 10;
        }

        Vector3 pos = transform.position;
        if (pos.y > 0.35f && pos.y < 1.25f)
        {
            pos.y = 0.42f;
            transform.position = pos;
        }
        initialPosition = transform.position;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Update()
    {
        // Flotación suave senoidal
        float newY = initialPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.position = new Vector3(initialPosition.x, newY, initialPosition.z);
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
