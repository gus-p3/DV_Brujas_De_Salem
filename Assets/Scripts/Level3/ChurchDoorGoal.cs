using System;
using System.Collections;
using Level2;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Level3
{
    /// <summary>
    /// La gran puerta doble de la Iglesia de Salem al final del Nivel 3 (X: 255).
    /// Implementa Level2.IInteractable y responde a [E] o [Q].
    /// Verifica dos condiciones antes de abrirse:
    /// 1. Level3StateController.Instance.HasBrewedMasterPotion == true
    /// 2. KeyInventory.Instance.HasKey("church_master_key") == true
    /// Al cumplirse ambas, abre las puertas, reproduce la fanfarria de victoria y completa el nivel.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class ChurchDoorGoal : MonoBehaviour, IInteractable
    {
        public const string RequiredMasterKeyId = "church_master_key";
        public const string LockedChurchMessage = "La Iglesia está cerrada con llave. Busca las pistas y abre los cofres del atrio para hallar la Llave de la Iglesia";

        [Header("Identificación y Requisitos")]
        [SerializeField] private string doorId = "salem_church_main_door";
        [SerializeField] private string doorDisplayName = "Gran Puerta de la Iglesia de Salem";
        [SerializeField] private string requiredKeyId = RequiredMasterKeyId;
        [SerializeField] private string requiredKeyDisplayName = "Llave Maestra de la Iglesia";
        [SerializeField] private int completionBonusScore = 500;

        [Header("Visuales de la Puerta Doble")]
        [SerializeField] private SpriteRenderer doorRenderer;
        [SerializeField] private Sprite closedDoorSprite;
        [SerializeField] private Sprite openDoorSprite;
        [SerializeField] private Transform leftDoorWing;
        [SerializeField] private Transform rightDoorWing;
        [SerializeField] private Light2D sanctuaryPortalLight;

        private bool isOpened;
        private float lastInteractTime = -10f;

        public string InteractionId => doorId;
        public int Priority => 40;
        public bool UsesInteractKey => true;
        public Transform Transform => transform;
        public float PromptHeightOffset => 2.2f;

        public bool IsOpened => isOpened;
        public event Action OnChurchDoorOpened;

        private void Awake()
        {
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
                if (col.size.x < 2.6f || col.size.y < 3.6f)
                {
                    col.size = new Vector2(Mathf.Max(col.size.x, 3.2f), Mathf.Max(col.size.y, 4.2f));
                }
            }

            if (doorRenderer == null)
            {
                doorRenderer = GetComponent<SpriteRenderer>();
                if (doorRenderer == null)
                {
                    doorRenderer = GetComponentInChildren<SpriteRenderer>();
                }
            }
        }

        private void Update()
        {
            if (isOpened) return;

            if (Input.GetKeyDown(KeyCode.E) && Time.unscaledTime >= lastInteractTime + 0.4f)
            {
                PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
                if (player != null && Vector2.Distance(transform.position, player.transform.position) <= 2.8f)
                {
                    Interact(player.gameObject);
                }
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            return !isOpened && interactor != null;
        }

        public string GetPromptText(GameObject interactor)
        {
            if (isOpened) return string.Empty;

            if (AreVictoryConditionsMet(out _))
            {
                return $"[E / Q] Abrir {doorDisplayName} con {requiredKeyDisplayName}";
            }

            return LockedChurchMessage;
        }

        public void Interact(GameObject interactor)
        {
            if (isOpened || interactor == null) return;
            if (Time.unscaledTime < lastInteractTime + 0.35f) return;
            lastInteractTime = Time.unscaledTime;

            TransformationManager tm = interactor.GetComponent<TransformationManager>();
            if (tm != null && tm.IsCatForm)
            {
                ShowHUDMessage("Necesitas tu forma humana [F] para abrir las puertas de la Iglesia.", 2.4f);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayErrorSound();
                return;
            }

            if (!AreVictoryConditionsMet(out string missingReason))
            {
                ShowHUDMessage(missingReason, 3.5f);
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayLockedSound();
                }
                return;
            }

            OpenChurchAndCompleteLevel();
        }

        public bool AreVictoryConditionsMet(out string missingMessage)
        {
            bool hasPotion = Level3StateController.Instance != null && Level3StateController.Instance.HasBrewedMasterPotion;
            bool hasKey = KeyInventory.Instance != null && KeyInventory.Instance.HasKey(requiredKeyId);

            if (!hasPotion && !hasKey)
            {
                missingMessage = $"{LockedChurchMessage} (Además debes preparar la Poción Maestra en la Cabaña del Bosque).";
                return false;
            }

            if (!hasPotion)
            {
                missingMessage = "Tienes la Llave de la Iglesia, pero aún necesitas preparar la Poción Maestra en el Caldero de la Cabaña del Bosque.";
                return false;
            }

            if (!hasKey)
            {
                missingMessage = LockedChurchMessage;
                return false;
            }

            missingMessage = string.Empty;
            return true;
        }

        private void OpenChurchAndCompleteLevel()
        {
            if (isOpened) return;
            isOpened = true;

            if (KeyInventory.Instance != null)
            {
                KeyInventory.Instance.MarkDoorUnlocked(doorId);
            }

            if (doorRenderer != null && openDoorSprite != null)
            {
                doorRenderer.sprite = openDoorSprite;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayGateUnlockSound();
                AudioManager.Instance.PlayChestOpenSound();
            }

            if (GameManager.Instance != null && completionBonusScore > 0)
            {
                GameManager.Instance.AddScore(completionBonusScore);
            }

            ShowHUDMessage("¡Las puertas de la Iglesia de Salem se abren! El sello inquisitorial ha sido quebrado.", 4.0f);
            OnChurchDoorOpened?.Invoke();

            if (Application.isPlaying && gameObject.activeInHierarchy)
            {
                StartCoroutine(OpenWingsAndCompleteRoutine());
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteLevel();
            }
        }

        private IEnumerator OpenWingsAndCompleteRoutine()
        {
            float duration = 0.65f;
            float elapsed = 0f;

            Vector3 leftStart = leftDoorWing != null ? leftDoorWing.localPosition : Vector3.zero;
            Vector3 rightStart = rightDoorWing != null ? rightDoorWing.localPosition : Vector3.zero;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

                if (leftDoorWing != null)
                {
                    leftDoorWing.localPosition = leftStart + new Vector3(-0.85f * t, 0f, 0f);
                }

                if (rightDoorWing != null)
                {
                    rightDoorWing.localPosition = rightStart + new Vector3(0.85f * t, 0f, 0f);
                }

                if (sanctuaryPortalLight != null)
                {
                    sanctuaryPortalLight.intensity = Mathf.Lerp(1.2f, 3.2f, t);
                    sanctuaryPortalLight.pointLightOuterRadius = Mathf.Lerp(4.0f, 8.5f, t);
                }

                yield return null;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteLevel();
            }
        }

        private static void ShowHUDMessage(string message, float duration)
        {
            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage(message, duration);
            }
        }
    }
}
