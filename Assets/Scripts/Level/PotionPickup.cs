using System;
using UnityEngine;

/// <summary>
/// Ítem recolectable de poción en el mundo.
/// Al entrar en contacto con la bruja suma 1 carga al inventario en GameManager,
/// siempre que no se haya alcanzado el tope máximo (3 cargas por tipo).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PotionPickup : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Tipo de poción que otorga este recolectable.")]
    [SerializeField] private PotionType potionType = PotionType.Blind;
    [Tooltip("Cantidad de cargas que otorga al recogerlo.")]
    [SerializeField] private int amount = 1;
    [Tooltip("Límite máximo de cargas permitido para este tipo.")]
    [SerializeField] private int maxCharges = 3;

    [Header("Animación de Flotación")]
    [Tooltip("Amplitud de la oscilación vertical (u).")]
    [SerializeField] private float bobbingAmplitude = 0.04f;
    [Tooltip("Frecuencia de la oscilación vertical (ciclos/s).")]
    [SerializeField] private float bobbingFrequency = 2.5f;

    [Header("Audio")]
    [Tooltip("Clip de sonido al recolectar.")]
    [SerializeField] private AudioClip collectSound;

    /// <summary>
    /// Evento global disparado al recolectar una poción en el mundo.
    /// Útil para la escucha de tutoriales y objetivos de nivel.
    /// </summary>
    public static event Action<PotionType> OnCollected;

    private Vector3 initialPosition;
    private Collider2D triggerCollider;

    public PotionType Type => potionType;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;

        if (bobbingAmplitude > 0.05f) bobbingAmplitude = 0.04f;

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
    }

    private void Update()
    {
        // Movimiento de flotación vertical sutil asentado sobre el suelo
        float newY = initialPosition.y + Mathf.Sin(Time.time * bobbingFrequency) * bobbingAmplitude;
        transform.position = new Vector3(initialPosition.x, newY, initialPosition.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        if (GameManager.Instance == null) return;

        // Comprobar si ya se tiene el tope máximo de cargas
        int currentCharges = potionType switch
        {
            PotionType.Blind => GameManager.Instance.BlindCharges,
            PotionType.Hypnotize => GameManager.Instance.HypnotizeCharges,
            PotionType.Sleep => GameManager.Instance.SleepCharges,
            _ => 0
        };

        if (currentCharges >= maxCharges)
        {
            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage($"¡Llevas el máximo de pociones de {GetPotionName(potionType)}! ({maxCharges})", 2f);
            }
            return;
        }

        // Agregar cargas al GameManager
        switch (potionType)
        {
            case PotionType.Blind:
                GameManager.Instance.AddSpellCharges(amount, 0, 0);
                break;
            case PotionType.Hypnotize:
                GameManager.Instance.AddSpellCharges(0, amount, 0);
                break;
            case PotionType.Sleep:
                GameManager.Instance.AddSpellCharges(0, 0, amount);
                break;
        }

        if (AudioManager.Instance != null && collectSound != null)
        {
            AudioManager.Instance.PlaySFX(collectSound);
        }

        OnCollected?.Invoke(potionType);

        Destroy(gameObject);
    }

    private string GetPotionName(PotionType type) => type switch
    {
        PotionType.Blind => "Ceguera",
        PotionType.Hypnotize => "Hipnosis",
        PotionType.Sleep => "Sueño",
        _ => "Magia"
    };
}
