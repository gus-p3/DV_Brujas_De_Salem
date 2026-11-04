using System;
using System.Collections;
using Level2;
using UnityEngine;

namespace Level3
{
    /// <summary>
    /// Portón físico bloqueado en el Nivel 3 que implementa Level2.IInteractable.
    /// Cuenta con un BoxCollider2D sólido que bloquea el paso del jugador en X y un
    /// BoxCollider2D trigger más ancho para la interacción con E/Q o apertura automática al acercarse con la llave.
    /// Soporta requerir una llave de KeyInventory y opcionalmente haber preparado la Poción Maestra en la Cabaña.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class LockedGateInteractable : MonoBehaviour, IInteractable
    {
        [Header("Identificación y Requisitos")]
        [SerializeField] private string gateId = "forest_gate_1";
        [SerializeField] private string gateDisplayName = "Portón del Bosque I";
        [SerializeField] private string requiredKeyId = "forest_gate_key_1";
        [SerializeField] private string requiredKeyDisplayName = "Llave del Portón del Bosque I";
        [SerializeField] private bool requireMasterPotion = false;
        [SerializeField] private bool autoOpenOnApproach = true;
        [SerializeField] private bool advanceToCabinPhaseOnOpen = false;
        [SerializeField] private bool completeLevelOnOpen = false;

        [Header("Colisionadores")]
        [SerializeField] private BoxCollider2D solidBlockerCollider;
        [SerializeField] private BoxCollider2D interactionTriggerCollider;

        [Header("Visuales y Animación de Apertura")]
        [SerializeField] private SpriteRenderer gateRenderer;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private Transform slidingBarsTransform;
        [SerializeField] private float openSlideHeight = 2.6f;
        [SerializeField] private float openAnimationDuration = 0.55f;

        private bool isUnlocked;
        private bool isAnimatingOpen;
        private float lastLockedMessageTime = -10f;
        private Vector3 initialBarsLocalPosition;

        public string InteractionId => $"gate_{gateId}";
        public int Priority => 28;
        public bool UsesInteractKey => true;
        public Transform Transform => transform;
        public float PromptHeightOffset => 1.85f;

        public string GateId => gateId;
        public string GateDisplayName => gateDisplayName;
        public string RequiredKeyId => requiredKeyId;
        public string RequiredKeyDisplayName => requiredKeyDisplayName;
        public bool RequireMasterPotion => requireMasterPotion;
        public bool IsUnlocked => isUnlocked;

        public event Action<LockedGateInteractable> OnGateUnlocked;

        /// <summary>
        /// Configura el portón programáticamente desde constructores de escena.
        /// </summary>
        public void Configure(
            string id,
            string displayName,
            string keyId,
            string keyName,
            bool needsMasterPotion = false,
            bool advanceCabinPhase = false,
            bool completesLevel = false,
            Sprite closed = null,
            Sprite open = null)
        {
            gateId = id;
            gateDisplayName = displayName;
            requiredKeyId = keyId;
            requiredKeyDisplayName = keyName;
            requireMasterPotion = needsMasterPotion;
            advanceToCabinPhaseOnOpen = advanceCabinPhase;
            completeLevelOnOpen = completesLevel;

            if (closed != null) closedSprite = closed;
            if (open != null) openSprite = open;

            EnsureCollidersAndVisuals();
        }

        private void Awake()
        {
            EnsureCollidersAndVisuals();

            Transform bars = slidingBarsTransform != null ? slidingBarsTransform : transform;
            initialBarsLocalPosition = bars.localPosition;
        }

        private void Start()
        {
            if (KeyInventory.Instance != null && KeyInventory.Instance.IsDoorUnlocked(gateId))
            {
                UnlockGate(immediate: true);
            }
        }

        private void Update()
        {
            if (isUnlocked || isAnimatingOpen) return;

            // Soporte adicional para abrir o consultar con tecla E cuando el jugador está frente al portón
            if (Input.GetKeyDown(KeyCode.E))
            {
                PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
                if (player != null && Vector2.Distance(transform.position, player.transform.position) <= 2.3f)
                {
                    Interact(player.gameObject);
                }
            }
        }

        private void EnsureCollidersAndVisuals()
        {
            if (gateRenderer == null)
            {
                gateRenderer = GetComponent<SpriteRenderer>();
                if (gateRenderer == null)
                {
                    gateRenderer = GetComponentInChildren<SpriteRenderer>();
                }
            }

            BoxCollider2D[] colliders = GetComponents<BoxCollider2D>();
            if (solidBlockerCollider == null && colliders.Length > 0)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (!colliders[i].isTrigger)
                    {
                        solidBlockerCollider = colliders[i];
                        break;
                    }
                }

                if (solidBlockerCollider == null)
                {
                    solidBlockerCollider = colliders[0];
                    solidBlockerCollider.isTrigger = false;
                }
            }

            if (solidBlockerCollider == null)
            {
                solidBlockerCollider = gameObject.AddComponent<BoxCollider2D>();
                solidBlockerCollider.isTrigger = false;
                solidBlockerCollider.size = new Vector2(0.85f, 3.2f);
                solidBlockerCollider.offset = new Vector2(0f, 1.6f);
            }

            if (interactionTriggerCollider == null)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != solidBlockerCollider && colliders[i].isTrigger)
                    {
                        interactionTriggerCollider = colliders[i];
                        break;
                    }
                }

                if (interactionTriggerCollider == null)
                {
                    interactionTriggerCollider = gameObject.AddComponent<BoxCollider2D>();
                }
            }

            interactionTriggerCollider.isTrigger = true;
            interactionTriggerCollider.size = new Vector2(
                Mathf.Max(solidBlockerCollider.size.x + 2.2f, 2.8f),
                Mathf.Max(solidBlockerCollider.size.y, 3.0f));
            interactionTriggerCollider.offset = solidBlockerCollider.offset;

            if (gateRenderer != null && closedSprite != null && !isUnlocked)
            {
                gateRenderer.sprite = closedSprite;
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            return !isUnlocked && !isAnimatingOpen && interactor != null;
        }

        public string GetPromptText(GameObject interactor)
        {
            if (isUnlocked) return string.Empty;

            if (requireMasterPotion && Level3StateController.Instance != null && !Level3StateController.Instance.HasBrewedMasterPotion)
            {
                return "Cerrado: Prepara primero la Poción Maestra en el Caldero de la Cabaña";
            }

            if (HasRequiredKey())
            {
                return $"[E / Q] Abrir {gateDisplayName} ({requiredKeyDisplayName})";
            }

            return GetMissingKeyMessage();
        }

        public void Interact(GameObject interactor)
        {
            if (isUnlocked || isAnimatingOpen || interactor == null) return;

            TransformationManager tm = interactor.GetComponent<TransformationManager>();
            if (tm != null && tm.IsCatForm)
            {
                ShowHUDFeedback("Necesitas forma humana [F] para abrir el portón.");
                if (AudioManager.Instance != null) AudioManager.Instance.PlayErrorSound();
                return;
            }

            if (requireMasterPotion && Level3StateController.Instance != null && !Level3StateController.Instance.HasBrewedMasterPotion)
            {
                ShowHUDFeedback("Cerrado: Primero debes preparar la Poción Maestra en el Caldero de la Cabaña.");
                if (AudioManager.Instance != null) AudioManager.Instance.PlayLockedSound();
                return;
            }

            if (!HasRequiredKey())
            {
                ShowHUDFeedback(GetMissingKeyMessage());
                if (AudioManager.Instance != null) AudioManager.Instance.PlayLockedSound();
                return;
            }

            UnlockGate(immediate: false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isUnlocked || isAnimatingOpen || other == null) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null && !other.CompareTag("Player")) return;

            TransformationManager tm = player != null ? player.GetComponent<TransformationManager>() : null;
            bool isCat = tm != null && tm.IsCatForm;

            if (requireMasterPotion && Level3StateController.Instance != null && !Level3StateController.Instance.HasBrewedMasterPotion)
            {
                TryShowThrottledLockedWarning("Cerrado: Primero debes preparar la Poción Maestra en el Caldero de la Cabaña.");
                return;
            }

            if (HasRequiredKey())
            {
                if (autoOpenOnApproach && !isCat)
                {
                    UnlockGate(immediate: false);
                }
            }
            else
            {
                TryShowThrottledLockedWarning(GetMissingKeyMessage());
            }
        }

        private bool HasRequiredKey()
        {
            if (string.IsNullOrEmpty(requiredKeyId))
            {
                return true;
            }

            return KeyInventory.Instance != null && KeyInventory.Instance.HasKey(requiredKeyId);
        }

        private string GetMissingKeyMessage()
        {
            return $"Cerrado: Necesitas {requiredKeyDisplayName} (Duerme al guardia brillante con una poción [Q])";
        }

        private void TryShowThrottledLockedWarning(string message)
        {
            if (Time.unscaledTime - lastLockedMessageTime < 2.2f) return;
            lastLockedMessageTime = Time.unscaledTime;

            ShowHUDFeedback(message);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLockedSound();
            }
        }

        /// <summary>
        /// Desbloquea y abre el portón, desactivando el collider sólido y guardando el estado en KeyInventory.
        /// </summary>
        public void UnlockGate(bool immediate = false)
        {
            if (isUnlocked) return;
            isUnlocked = true;

            if (KeyInventory.Instance != null)
            {
                KeyInventory.Instance.MarkDoorUnlocked(gateId);
            }

            if (solidBlockerCollider != null)
            {
                solidBlockerCollider.enabled = false;
            }

            if (advanceToCabinPhaseOnOpen && Level3StateController.Instance != null)
            {
                Level3StateController.Instance.AdvanceToBrewPhase();
            }

            if (immediate || !Application.isPlaying || !gameObject.activeInHierarchy)
            {
                ApplyOpenVisualsImmediate();
            }
            else
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayGateUnlockSound();
                }

                ShowHUDFeedback($"¡{gateDisplayName} abierto con {requiredKeyDisplayName}!");
                StartCoroutine(OpenGateRoutine());
            }

            OnGateUnlocked?.Invoke(this);

            if (completeLevelOnOpen && !immediate && GameManager.Instance != null)
            {
                GameManager.Instance.CompleteLevel();
            }
        }

        private void ApplyOpenVisualsImmediate()
        {
            if (gateRenderer != null && openSprite != null)
            {
                gateRenderer.sprite = openSprite;
            }
            else
            {
                Transform target = slidingBarsTransform != null ? slidingBarsTransform : transform;
                target.localPosition = initialBarsLocalPosition + new Vector3(0f, openSlideHeight, 0f);
                if (gateRenderer != null)
                {
                    Color c = gateRenderer.color;
                    c.a = 0.35f;
                    gateRenderer.color = c;
                }
            }
        }

        private IEnumerator OpenGateRoutine()
        {
            isAnimatingOpen = true;

            if (gateRenderer != null && openSprite != null)
            {
                gateRenderer.sprite = openSprite;
            }

            Transform target = slidingBarsTransform != null ? slidingBarsTransform : (openSprite == null ? transform : null);
            Vector3 startPos = target != null ? target.localPosition : Vector3.zero;
            Vector3 endPos = startPos + new Vector3(0f, openSlideHeight, 0f);

            Color startColor = gateRenderer != null ? gateRenderer.color : Color.white;
            float elapsed = 0f;
            float dur = Mathf.Max(0.1f, openAnimationDuration);

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                float smooth = Mathf.SmoothStep(0f, 1f, t);

                if (target != null)
                {
                    target.localPosition = Vector3.Lerp(startPos, endPos, smooth);
                }

                if (gateRenderer != null && openSprite == null)
                {
                    Color c = startColor;
                    c.a = Mathf.Lerp(startColor.a, 0.32f, smooth);
                    gateRenderer.color = c;
                }

                yield return null;
            }

            isAnimatingOpen = false;
        }

        private static void ShowHUDFeedback(string message)
        {
            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage(message, 2.8f);
            }
        }
    }
}
