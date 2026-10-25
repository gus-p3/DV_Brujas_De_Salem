using System.Collections.Generic;
using UnityEngine;

namespace Level2
{
    /// <summary>
    /// Detector y controlador de interacciones cercanas para la bruja en el Nivel 2.
    /// - Detecta objetos que implementan IInteractable (puertas, escaleras, cofres, llaves, grimorios).
    /// - Selecciona un único foco prioritario y actualiza InteractionPromptUI sin superponer mensajes.
    /// - Evita activaciones repetidas al mantener pulsada la tecla E mediante GetKeyDown y enfriamiento.
    /// - Suprime temporalmente el lanzamiento de pociones con la tecla E mientras exista una interacción
    ///   contextual activa de tecla E, evitando desperdiciar pociones frente a una puerta o cofre.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Detección de Interacciones")]
        [Tooltip("Tamaño de la caja de detección alrededor del personaje.")]
        [SerializeField] private Vector2 detectionBoxSize = new Vector2(1.45f, 1.85f);
        [Tooltip("Desplazamiento vertical del centro de detección.")]
        [SerializeField] private Vector2 detectionBoxOffset = new Vector2(0f, 0.85f);
        [Tooltip("Tecla principal para interactuar con puertas, cofres y objetos del Nivel 2 (Q para no colisionar con E de lanzar pociones).")]
        [SerializeField] private KeyCode interactKey = KeyCode.Q;
        [Tooltip("Enfriamiento mínimo entre interacciones (s) para evitar dobles activaciones.")]
        [SerializeField] private float interactionCooldown = 0.35f;

        private readonly Collider2D[] overlapBuffer = new Collider2D[20];
        private readonly List<IInteractable> candidates = new List<IInteractable>(8);

        private PotionInventory potionInventory;
        private IInteractable currentFocus;
        private float lastInteractTime = -10f;
        private bool potionInventorySuppressed;

        public IInteractable CurrentFocus => currentFocus;
        public KeyCode InteractKey => interactKey;

        private void Awake()
        {
            interactKey = Level2Controls.InteractKey;
            potionInventory = GetComponent<PotionInventory>();
            if (GetComponent<Level2Controls>() == null)
            {
                gameObject.AddComponent<Level2Controls>();
            }
        }

        private void OnDisable()
        {
            RestorePotionInventoryIfSuppressed();
            if (InteractionPromptUI.Instance != null)
            {
                InteractionPromptUI.Instance.HidePrompt();
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                ClearFocus();
                return;
            }

            if ((TutorialManager.Instance != null && TutorialManager.Instance.IsOpen) || Level2LoreReader.IsOpen)
            {
                ClearFocus();
                return;
            }

            if (InteriorTransitionManager.Instance != null && InteriorTransitionManager.Instance.IsTransitioning)
            {
                ClearFocus();
                return;
            }

            RefreshFocus();

            // En el Nivel 2 la tecla de interacción es exclusivamente Level2Controls.InteractKey (Q),
            // mientras que E lanza la poción seleccionada (controlado por Level2Controls).
            if (currentFocus != null && currentFocus.UsesInteractKey)
            {
                if (Level2Controls.TryConsumeInteractDown(interactionCooldown) && Time.unscaledTime >= lastInteractTime + interactionCooldown)
                {
                    TriggerCurrentInteraction();
                }
            }
        }

        /// <summary>
        /// Simula la pulsación de una tecla sobre el interactor (para pruebas automatizadas Q vs E).
        /// Devuelve true solo si la tecla coincide con Level2Controls.InteractKey (Q) y ejecuta la interacción.
        /// </summary>
        public bool TryProcessKey(KeyCode pressedKey, bool ignoreCooldown = false)
        {
            if (!Level2Controls.IsInteractKey(pressedKey)) return false;
            if (!ignoreCooldown && Time.unscaledTime < lastInteractTime + interactionCooldown) return false;
            Level2Controls.ConsumeInteractNow();
            return TriggerCurrentInteraction();
        }

        /// <summary>
        /// Escanea los colliders cercanos sin generar basura en memoria y elige el mejor IInteractable.
        /// </summary>
        public IInteractable RefreshFocus()
        {
            candidates.Clear();
            Vector2 center = (Vector2)transform.position + detectionBoxOffset;
            ContactFilter2D filter = ContactFilter2D.noFilter;
            filter.useTriggers = true;
            int count = Physics2D.OverlapBox(center, detectionBoxSize, 0f, filter, overlapBuffer);

            for (int i = 0; i < count; i++)
            {
                Collider2D col = overlapBuffer[i];
                if (col == null || !col.gameObject.activeInHierarchy) continue;

                IInteractable interactable = col.GetComponent<IInteractable>();
                if (interactable == null)
                {
                    interactable = col.GetComponentInParent<IInteractable>();
                }

                if (interactable != null && interactable.CanInteract(gameObject) && !candidates.Contains(interactable))
                {
                    candidates.Add(interactable);
                }
            }

            IInteractable best = null;
            int bestPriority = int.MinValue;
            float bestDistSq = float.MaxValue;
            Vector3 myPos = transform.position + (Vector3)detectionBoxOffset;

            for (int i = 0; i < candidates.Count; i++)
            {
                IInteractable c = candidates[i];
                if (c == null || c.Transform == null) continue;

                int prio = c.Priority;
                float distSq = (c.Transform.position - myPos).sqrMagnitude;

                if (prio > bestPriority || (prio == bestPriority && distSq < bestDistSq))
                {
                    best = c;
                    bestPriority = prio;
                    bestDistSq = distSq;
                }
            }

            currentFocus = best;
            UpdatePromptDisplay();
            return currentFocus;
        }

        /// <summary>
        /// Ejecuta la interacción con el objeto actualmente en foco (si existe y está disponible).
        /// </summary>
        public bool TriggerCurrentInteraction()
        {
            if (currentFocus == null)
            {
                RefreshFocus();
            }

            if (currentFocus == null || !currentFocus.CanInteract(gameObject))
            {
                return false;
            }

            lastInteractTime = Time.unscaledTime;
            IInteractable target = currentFocus;
            target.Interact(gameObject);
            RefreshFocus();
            return true;
        }

        private void UpdatePromptDisplay()
        {
            if (InteractionPromptUI.Instance == null) return;

            if (currentFocus == null)
            {
                InteractionPromptUI.Instance.HidePrompt();
                return;
            }

            string text = currentFocus.GetPromptText(gameObject);
            if (string.IsNullOrEmpty(text))
            {
                InteractionPromptUI.Instance.HidePrompt();
            }
            else
            {
                InteractionPromptUI.Instance.ShowPrompt(text, currentFocus.Transform, currentFocus.PromptHeightOffset);
            }
        }

        private void ClearFocus()
        {
            currentFocus = null;
            RestorePotionInventoryIfSuppressed();
            if (InteractionPromptUI.Instance != null)
            {
                InteractionPromptUI.Instance.HidePrompt();
            }
        }

        private void SetPotionThrowSuppressed(bool suppress)
        {
            if (potionInventory == null)
            {
                potionInventory = GetComponent<PotionInventory>();
                if (potionInventory == null) return;
            }

            if (suppress && !potionInventorySuppressed)
            {
                potionInventory.enabled = false;
                potionInventorySuppressed = true;
            }
            else if (!suppress && potionInventorySuppressed)
            {
                potionInventory.enabled = true;
                potionInventorySuppressed = false;
            }
        }

        private void RestorePotionInventoryIfSuppressed()
        {
            if (potionInventorySuppressed && potionInventory != null)
            {
                potionInventory.enabled = true;
            }
            potionInventorySuppressed = false;
        }

        private void ForwardPotionSelectionKeys()
        {
            if (potionInventory == null) return;
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                potionInventory.SelectPotion(PotionType.Blind);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                potionInventory.SelectPotion(PotionType.Hypnotize);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                potionInventory.SelectPotion(PotionType.Sleep);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.5f);
            Gizmos.DrawWireCube(transform.position + (Vector3)detectionBoxOffset, detectionBoxSize);
        }
    }
}
