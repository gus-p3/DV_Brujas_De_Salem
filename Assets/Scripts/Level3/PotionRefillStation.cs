using Level2;
using UnityEngine;

namespace Level3
{
    /// <summary>
    /// Pequeños alijos o estantes de pociones repartidos en el camino de regreso que reaparecen
    /// tras 25 segundos si el jugador se queda sin pociones (o bajo de cargas) en el inventario,
    /// garantizando que siempre tenga munición para los retos de dormir guardias.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class PotionRefillStation : MonoBehaviour, IInteractable
    {
        [Header("Configuración del Alijo de Pociones")]
        [SerializeField] private string stationId = "refill_station_1";
        [SerializeField] private string stationDisplayName = "Alijo Alquímico de Reserva";
        [SerializeField] private PotionType grantedPotionType = PotionType.Sleep;
        [SerializeField] private int grantAmount = 2;
        [SerializeField] private int maxAllowedBeforeRefill = 2;
        [SerializeField] private float respawnDelaySeconds = 25f;

        [Header("Visuales")]
        [SerializeField] private SpriteRenderer flaskRenderer;

        private bool isAvailable = true;
        private float cooldownTimer;
        private Vector3 baseFlaskLocalPos;

        public string InteractionId => $"refill_{stationId}";
        public int Priority => 22;
        public bool UsesInteractKey => true;
        public Transform Transform => transform;
        public float PromptHeightOffset => 1.3f;

        public bool IsAvailable => isAvailable;

        public void Configure(string id, string displayName, PotionType type = PotionType.Sleep, int amount = 2, float respawnSeconds = 25f)
        {
            stationId = id;
            stationDisplayName = displayName;
            grantedPotionType = type;
            grantAmount = Mathf.Max(1, amount);
            respawnDelaySeconds = Mathf.Max(5f, respawnSeconds);
        }

        private void Awake()
        {
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
                if (col.size.x < 1.4f || col.size.y < 1.4f)
                {
                    col.size = new Vector2(1.6f, 1.6f);
                }
            }

            if (flaskRenderer == null)
            {
                Transform f = transform.Find("FlaskVisual");
                if (f != null) flaskRenderer = f.GetComponent<SpriteRenderer>();
                if (flaskRenderer == null) flaskRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (flaskRenderer != null)
            {
                baseFlaskLocalPos = flaskRenderer.transform.localPosition;
            }

            PickupGlint glint = GetComponent<PickupGlint>();
            if (glint == null) glint = gameObject.AddComponent<PickupGlint>();
            glint.SetColor(new Color(0.45f, 0.72f, 1f, 1f));
        }

        private void Update()
        {
            if (isAvailable)
            {
                if (flaskRenderer != null)
                {
                    float bob = Mathf.Sin(Time.time * 3.2f) * 0.08f;
                    flaskRenderer.transform.localPosition = baseFlaskLocalPos + new Vector3(0f, bob, 0f);
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    PlayerController player = Object.FindFirstObjectByType<PlayerController>();
                    if (player != null && Vector2.Distance(transform.position, player.transform.position) <= 1.85f)
                    {
                        Interact(player.gameObject);
                    }
                }
                return;
            }

            // En enfriamiento: avanza más rápido si el jugador se quedó con 0 pociones
            float speedMultiplier = GetCurrentCharges() <= 0 ? 2.5f : 1f;
            cooldownTimer -= Time.deltaTime * speedMultiplier;
            if (cooldownTimer <= 0f)
            {
                SetStationAvailable(true);
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            return isAvailable && interactor != null;
        }

        public string GetPromptText(GameObject interactor)
        {
            if (!isAvailable)
            {
                return $"{stationDisplayName} (Reaparece en {Mathf.CeilToInt(cooldownTimer)}s)";
            }

            return $"[E / Q] Tomar +{grantAmount} Poción de {GetPotionLabel(grantedPotionType)} ({stationDisplayName})";
        }

        public void Interact(GameObject interactor)
        {
            if (!isAvailable || interactor == null) return;

            TransformationManager tm = interactor.GetComponent<TransformationManager>();
            if (tm != null && tm.IsCatForm)
            {
                return;
            }

            TryGrantRefill();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isAvailable || other == null) return;

            PlayerController pc = other.GetComponentInParent<PlayerController>();
            if (pc == null && !other.CompareTag("Player")) return;

            TransformationManager tm = pc != null ? pc.GetComponent<TransformationManager>() : null;
            if (tm != null && tm.IsCatForm) return;

            // Si el jugador tiene pocas cargas de esta poción, la recoge automáticamente al pasar
            if (GetCurrentCharges() < maxAllowedBeforeRefill)
            {
                TryGrantRefill();
            }
        }

        private void TryGrantRefill()
        {
            if (GameManager.Instance == null) return;

            int current = GetCurrentCharges();
            if (current >= 3)
            {
                HUDController hud = Object.FindFirstObjectByType<HUDController>();
                if (hud != null)
                {
                    hud.ShowMessage($"Ya llevas el máximo de Pociones de {GetPotionLabel(grantedPotionType)}.", 2.0f);
                }
                return;
            }

            int add = Mathf.Clamp(grantAmount, 1, 3 - current);
            switch (grantedPotionType)
            {
                case PotionType.Blind:
                    GameManager.Instance.AddSpellCharges(add, 0, 0);
                    break;
                case PotionType.Hypnotize:
                    GameManager.Instance.AddSpellCharges(0, add, 0);
                    break;
                default:
                    GameManager.Instance.AddSpellCharges(0, 0, add);
                    break;
            }

            PotionInventory inv = Object.FindFirstObjectByType<PotionInventory>();
            if (inv != null && grantedPotionType == PotionType.Sleep)
            {
                inv.SelectPotion(PotionType.Sleep);
            }

            Sprite spr = flaskRenderer != null ? flaskRenderer.sprite : null;
            LootPopupVisualizer.ShowPotionReward(transform.position + Vector3.up * 0.4f, grantedPotionType, add, spr);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPotionPickupSound();
            }

            SetStationAvailable(false);
        }

        private void SetStationAvailable(bool available)
        {
            isAvailable = available;
            cooldownTimer = available ? 0f : respawnDelaySeconds;

            if (flaskRenderer != null)
            {
                flaskRenderer.enabled = available;
            }
        }

        private int GetCurrentCharges()
        {
            if (GameManager.Instance == null) return 0;
            return grantedPotionType switch
            {
                PotionType.Blind => GameManager.Instance.BlindCharges,
                PotionType.Hypnotize => GameManager.Instance.HypnotizeCharges,
                _ => GameManager.Instance.SleepCharges
            };
        }

        private static string GetPotionLabel(PotionType type) => type switch
        {
            PotionType.Blind => "Ceguera",
            PotionType.Hypnotize => "Hipnosis",
            _ => "Sueño"
        };
    }
}
