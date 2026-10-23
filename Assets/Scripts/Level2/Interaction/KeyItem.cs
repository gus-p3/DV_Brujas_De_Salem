using UnityEngine;

namespace Level2
{
    /// <summary>
    /// Llave recolectable en sótanos, altillos o habitaciones secretas del Nivel 2.
    /// Se puede recoger pulsando E o al pasar sobre ella en forma humana, registrándose
    /// una única vez en KeyInventory.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class KeyItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private string keyId = "key_tavern_chest";
        [SerializeField] private string keyDisplayName = "Llave de Hierro de la Posada";
        [SerializeField] private int scoreValue = 150;
        [SerializeField] private bool collectOnTriggerTouch = true;

        private bool collected;

        public string InteractionId => keyId;
        public int Priority => 19;
        public bool UsesInteractKey => true;
        public Transform Transform => transform;
        public float PromptHeightOffset => 0.9f;

        public string KeyId => keyId;
        public string KeyDisplayName => keyDisplayName;

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void Start()
        {
            if (KeyInventory.Instance != null && KeyInventory.Instance.HasKey(keyId))
            {
                collected = true;
                gameObject.SetActive(false);
            }
        }

        public void Configure(string id, string displayName, int points = 150)
        {
            keyId = id;
            keyDisplayName = displayName;
            scoreValue = points;
        }

        public bool CanInteract(GameObject interactor)
        {
            if (collected || interactor == null) return false;
            return !(KeyInventory.Instance != null && KeyInventory.Instance.HasKey(keyId));
        }

        public string GetPromptText(GameObject interactor)
        {
            if (collected) return string.Empty;
            return $"E — Recoger {keyDisplayName}";
        }

        public void Interact(GameObject interactor)
        {
            TryCollectKey(interactor);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!collectOnTriggerTouch || collected || other == null) return;
            PlayerController pc = other.GetComponentInParent<PlayerController>();
            if (pc != null || other.CompareTag("Player"))
            {
                TryCollectKey(pc != null ? pc.gameObject : other.gameObject);
            }
        }

        private void TryCollectKey(GameObject interactor)
        {
            if (collected || interactor == null) return;

            TransformationManager tm = interactor.GetComponent<TransformationManager>();
            if (tm != null && tm.IsCatForm)
            {
                HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
                if (hud != null) hud.ShowMessage("Necesitas forma humana para recoger esta llave", 1.8f);
                return;
            }

            collected = true;

            if (KeyInventory.Instance != null)
            {
                KeyInventory.Instance.AddKey(keyId, keyDisplayName);
            }

            if (GameManager.Instance != null && scoreValue > 0)
            {
                GameManager.Instance.AddScore(scoreValue);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClickSound();
            }

            gameObject.SetActive(false);
        }
    }
}
