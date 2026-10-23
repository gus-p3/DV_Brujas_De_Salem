using System;
using UnityEngine;

namespace Level2
{
    /// <summary>
    /// Portón de salida condicionado del Nivel 2 (Acto 2).
    /// Bloquea el paso hacia la salida (LevelGoal / Goal_Exit_Marker) hasta que
    /// la bruja reúna todos los ingredientes requeridos (IngredientInventory.AreAllRequiredCollected).
    /// Proporciona:
    /// - Colisionador bloqueante sólido mientras esté cerrado.
    /// - Trigger de advertencia con aviso en HUD si se aproxima sin los ingredientes.
    /// - Apertura automática (desactivación de bloqueo + cambio de sprite + partículas + SFX)
    ///   al cumplirse los requisitos.
    /// - Evento OnGateOpened y propiedad IsGateOpen para la integración de la transición en el Prompt 5.
    /// - Capacidad de restaurarse/reabrirse tras una reaparición de checkpoint.
    /// </summary>
    public class IngredientGate : MonoBehaviour
    {
        public static IngredientGate Instance { get; private set; }

        [Header("Colisionadores")]
        [Tooltip("Colisionador físico sólido que bloquea el paso mientras el portón está cerrado.")]
        [SerializeField] private Collider2D solidBlockerCollider;

        [Tooltip("Trigger frontal de advertencia al acercarse con el portón cerrado.")]
        [SerializeField] private Collider2D warningTriggerCollider;

        [Header("Representación Visual")]
        [SerializeField] private SpriteRenderer gateRenderer;
        [SerializeField] private Sprite closedGateSprite;
        [SerializeField] private Sprite openGateSprite;

        [Header("Efectos Audiovisuales")]
        [SerializeField] private ParticleSystem openParticles;
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip lockedSound;

        [Header("Mensajes de HUD")]
        [SerializeField] private string lockedMessage = "El portón está cerrado por un sello mágico. Aún te faltan ingredientes.";
        [SerializeField] private string openedMessage = "¡Sello roto! El portón hacia la iglesia está abierto.";
        [SerializeField] private float messageCooldown = 2.0f;

        public bool IsGateOpen { get; private set; } = false;
        public Collider2D SolidBlocker => solidBlockerCollider;
        public event Action OnGateOpened;

        private float lastWarningTime = -10f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (warningTriggerCollider != null)
            {
                warningTriggerCollider.isTrigger = true;
            }
        }

        private void Start()
        {
            if (IngredientInventory.Instance != null)
            {
                IngredientInventory.Instance.OnAllRequiredCollected += HandleAllRequiredCollected;

                // Si al iniciar (ej. reaparición de checkpoint tras haber completado) ya se tienen todos:
                if (IngredientInventory.Instance.AreAllRequiredCollected)
                {
                    OpenGate(immediate: true);
                }
                else
                {
                    CloseGate();
                }
            }
            else
            {
                CloseGate();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (IngredientInventory.Instance != null)
            {
                IngredientInventory.Instance.OnAllRequiredCollected -= HandleAllRequiredCollected;
            }
        }

        private void HandleAllRequiredCollected()
        {
            OpenGate(immediate: false);
        }

        /// <summary>
        /// Abre el portón, desactiva la colisión física y activa efectos de éxito.
        /// </summary>
        public void OpenGate(bool immediate = false)
        {
            if (IsGateOpen) return;

            IsGateOpen = true;

            if (solidBlockerCollider != null)
            {
                solidBlockerCollider.enabled = false;
            }

            if (gateRenderer != null && openGateSprite != null)
            {
                gateRenderer.sprite = openGateSprite;
            }

            if (!immediate)
            {
                if (openParticles != null)
                {
                    openParticles.Play();
                }

                if (AudioManager.Instance != null)
                {
                    if (openSound != null) AudioManager.Instance.PlaySFX(openSound);
                    else AudioManager.Instance.PlayButtonClickSound();
                }

                ShowHUDMessage(openedMessage, 3.5f);
            }

            Debug.Log("[IngredientGate] ¡Portón del Acto 2 abierto exitosamente!");
            OnGateOpened?.Invoke();
        }

        /// <summary>
        /// Cierra el portón y asegura el bloqueo físico.
        /// </summary>
        public void CloseGate()
        {
            IsGateOpen = false;

            if (solidBlockerCollider != null)
            {
                solidBlockerCollider.enabled = true;
            }

            if (gateRenderer != null && closedGateSprite != null)
            {
                gateRenderer.sprite = closedGateSprite;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CheckWarning(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            CheckWarning(other);
        }

        private void CheckWarning(Collider2D other)
        {
            if (IsGateOpen) return;

            PlayerController pc = other.GetComponentInParent<PlayerController>();
            if (pc == null && !other.CompareTag("Player")) return;

            if (Time.unscaledTime - lastWarningTime < messageCooldown) return;
            lastWarningTime = Time.unscaledTime;

            ShowHUDMessage(lockedMessage, 2.5f);

            if (AudioManager.Instance != null)
            {
                if (lockedSound != null) AudioManager.Instance.PlaySFX(lockedSound);
                else AudioManager.Instance.PlayButtonClickSound();
            }
        }

        private void ShowHUDMessage(string message, float duration)
        {
            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage(message, duration);
            }

            if (IngredientHUD.Instance != null)
            {
                IngredientHUD.Instance.ShowWarning(message, duration);
            }
        }

        /// <summary>
        /// Asigna referencias programáticamente desde builders o verificadores.
        /// </summary>
        public void SetupComponents(
            Collider2D blocker, Collider2D trigger,
            SpriteRenderer renderer, Sprite closed, Sprite open)
        {
            solidBlockerCollider = blocker;
            warningTriggerCollider = trigger;
            gateRenderer = renderer;
            closedGateSprite = closed;
            openGateSprite = open;
        }
    }
}
