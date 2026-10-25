using UnityEngine;

namespace Level2
{
    /// <summary>
    /// Componente de puerta interactiva para las casas de la aldea del siglo XVII.
    /// Conecta puertas exteriores (calle o balcones de segundo piso) con puertas interiores,
    /// soporta puertas abiertas, puertas que requieren llave, puertas desbloqueables por misión
    /// y puertas selladas de ambientación.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class HouseDoor : MonoBehaviour, IInteractable
    {
        [Header("Identificación y Destino")]
        [SerializeField] private string doorId = "door_default";
        [SerializeField] private string actionLabel = "Entrar";
        [SerializeField] private HouseDoor targetDoor;
        [SerializeField] private InteriorZone owningInterior;
        [Tooltip("Altura Y de la superficie del suelo donde deben apoyarse los pies al aparecer frente a esta puerta.")]
        [SerializeField] private float floorSurfaceY = 0.17f;
        [Tooltip("Desplazamiento horizontal respecto a la puerta al aparecer.")]
        [SerializeField] private float spawnOffsetX = 0f;
        [Tooltip("Orientación forzada al salir por esta puerta (-1 izquierda, 0 mantener actual, +1 derecha).")]
        [SerializeField] private int overrideFacingOnExit = 0;

        [Header("Condiciones de Bloqueo")]
        [SerializeField] private bool isLocked = false;
        [SerializeField] private string requiredKeyId = "";
        [SerializeField] private string requiredKeyDisplayName = "Llave Antigua";
        [SerializeField] private bool requiresAllIngredients = false;
        [SerializeField] private bool permanentlyBarred = false;
        [SerializeField] private string barredMessage = "Esta puerta está tapiada desde dentro por la cuarentena.";

        [Header("Visuales")]
        [SerializeField] private SpriteRenderer doorRenderer;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private Sprite lockedSprite;

        public string InteractionId => doorId;
        public int Priority => 20;
        public bool UsesInteractKey => true;
        public Transform Transform => transform;
        public float PromptHeightOffset => 1.45f;

        public string DoorId => doorId;
        public HouseDoor TargetDoor { get => targetDoor; set => targetDoor = value; }
        public InteriorZone OwningInterior { get => owningInterior; set => owningInterior = value; }
        public float FloorSurfaceY { get => floorSurfaceY; set => floorSurfaceY = value; }
        public int OverrideFacingOnExit => overrideFacingOnExit;
        public bool IsLocked => CheckCurrentlyLocked();

        private void Awake()
        {
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            if (col != null) col.isTrigger = true;
            if (doorRenderer == null) doorRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            if (KeyInventory.Instance != null && KeyInventory.Instance.IsDoorUnlocked(doorId))
            {
                isLocked = false;
            }
            UpdateVisualState();
        }

        public void Configure(
            string id,
            string promptAction,
             float surfaceY,
            InteriorZone interior = null,
            Sprite closed = null,
            Sprite open = null,
            Sprite locked = null)
        {
            doorId = id;
            actionLabel = promptAction;
            floorSurfaceY = surfaceY;
            owningInterior = interior;
            closedSprite = closed;
            openSprite = open;
            lockedSprite = locked;

            if (doorRenderer == null) doorRenderer = GetComponent<SpriteRenderer>();
            UpdateVisualState();
        }

        public void ConfigureLock(bool locked, string keyId = "", string keyName = "", bool needsIngredients = false, bool barred = false, string customBarredMsg = "")
        {
            isLocked = locked;
            requiredKeyId = keyId;
            requiredKeyDisplayName = keyName;
            requiresAllIngredients = needsIngredients;
            permanentlyBarred = barred;
            if (!string.IsNullOrEmpty(customBarredMsg)) barredMessage = customBarredMsg;
            UpdateVisualState();
        }

        public bool CanInteract(GameObject interactor)
        {
            if (interactor == null) return false;
            TransformationManager tm = interactor.GetComponent<TransformationManager>();
            // Tanto en forma humana como felina puede acercarse a ver el aviso, aunque las puertas las abre la bruja
            return true;
        }

        private static float lastGlobalDoorUseTime = -10f;
        private const float DoorUseCooldown = 0.35f;

        public string GetPromptText(GameObject interactor)
        {
            if (permanentlyBarred)
            {
                return $"Q: Examinar puerta ({actionLabel})";
            }

            if (requiresAllIngredients && (IngredientInventory.Instance == null || !IngredientInventory.Instance.AreAllRequiredCollected))
            {
                return $"Q: {actionLabel} (Sello de Ingredientes)";
            }

            if (isLocked)
            {
                if (!string.IsNullOrEmpty(requiredKeyId) && KeyInventory.Instance != null && KeyInventory.Instance.HasKey(requiredKeyId))
                {
                    return $"Q: Desbloquear con {requiredKeyDisplayName}";
                }
                return $"Q: Cerrada (Requiere {requiredKeyDisplayName})";
            }

            return $"Q: {actionLabel}";
        }

        public void Interact(GameObject interactor)
        {
            if (interactor == null) return;

            if (Application.isPlaying && Time.unscaledTime - lastGlobalDoorUseTime < DoorUseCooldown)
            {
                return;
            }

            TransformationManager tm = interactor.GetComponent<TransformationManager>();
            // Si está en el exterior y la puerta está cerrada con llave o sellada, requiere forma humana;
            // desde el interior siempre puede salir para nunca quedar atrapado en forma de gato.
            if (tm != null && tm.IsCatForm && owningInterior == null && (isLocked || permanentlyBarred || requiresAllIngredients))
            {
                ShowFeedback("Necesitas forma humana para abrir esta puerta");
                return;
            }

            if (permanentlyBarred)
            {
                ShowFeedback(barredMessage);
                return;
            }

            if (requiresAllIngredients && (IngredientInventory.Instance == null || !IngredientInventory.Instance.AreAllRequiredCollected))
            {
                int current = IngredientInventory.Instance != null ? IngredientInventory.Instance.TotalCollected : 0;
                int req = IngredientInventory.Instance != null ? IngredientInventory.Instance.TotalRequired : 8;
                ShowFeedback($"Sello arcano activo ({current}/{req}). Revisa los cofres de las casas y de la calle.");
                return;
            }

            if (isLocked)
            {
                if (!string.IsNullOrEmpty(requiredKeyId) && KeyInventory.Instance != null && KeyInventory.Instance.HasKey(requiredKeyId))
                {
                    UnlockDoor();
                    ShowFeedback($"Puerta desbloqueada con {requiredKeyDisplayName}");
                }
                else
                {
                    ShowFeedback($"Cerrada con llave. Necesitas: {requiredKeyDisplayName}");
                    return;
                }
            }

            if (targetDoor == null)
            {
                ShowFeedback("La puerta no conduce a ningún sitio accesible.");
                return;
            }

            lastGlobalDoorUseTime = Time.unscaledTime;

            // Abrir visualmente ambas puertas conectadas
            SetOpenVisual(true);
            targetDoor.SetOpenVisual(true);

            if (InteriorTransitionManager.Instance != null)
            {
                InteriorTransitionManager.Instance.TransitionThroughDoor(this, interactor, immediate: !Application.isPlaying);
            }
            else
            {
                interactor.transform.position = targetDoor.GetSafeSpawnPosition(interactor);
                Physics2D.SyncTransforms();
            }
        }

        public void UnlockDoor()
        {
            isLocked = false;
            if (KeyInventory.Instance != null)
            {
                KeyInventory.Instance.MarkDoorUnlocked(doorId);
            }
            if (targetDoor != null && targetDoor.isLocked)
            {
                targetDoor.isLocked = false;
                if (KeyInventory.Instance != null)
                {
                    KeyInventory.Instance.MarkDoorUnlocked(targetDoor.doorId);
                }
                targetDoor.UpdateVisualState();
            }
            UpdateVisualState();
        }

        public Vector3 GetSafeSpawnPosition(GameObject player)
        {
            float feetOffset = 0.056f;
            if (player != null)
            {
                BoxCollider2D box = player.GetComponent<BoxCollider2D>();
                if (box != null)
                {
                    feetOffset = box.offset.y - box.size.y * 0.5f;
                }
            }

            float targetY = floorSurfaceY - feetOffset + 0.02f;
            return new Vector3(transform.position.x + spawnOffsetX, targetY, 0f);
        }

        public void SetOpenVisual(bool opened)
        {
            if (doorRenderer == null) doorRenderer = GetComponent<SpriteRenderer>();
            if (doorRenderer == null) return;

            if (CheckCurrentlyLocked() && lockedSprite != null)
            {
                doorRenderer.sprite = lockedSprite;
            }
            else if (opened && openSprite != null)
            {
                doorRenderer.sprite = openSprite;
            }
            else if (closedSprite != null)
            {
                doorRenderer.sprite = closedSprite;
            }
        }

        public void UpdateVisualState()
        {
            SetOpenVisual(!CheckCurrentlyLocked() && openSprite != null && targetDoor != null && OwningInterior != null);
        }

        private bool CheckCurrentlyLocked()
        {
            if (permanentlyBarred) return true;
            if (requiresAllIngredients && (IngredientInventory.Instance == null || !IngredientInventory.Instance.AreAllRequiredCollected))
            {
                return true;
            }
            return isLocked;
        }

        private static void ShowFeedback(string message)
        {
            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage(message, 2.0f);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClickSound();
            }
        }
    }
}
