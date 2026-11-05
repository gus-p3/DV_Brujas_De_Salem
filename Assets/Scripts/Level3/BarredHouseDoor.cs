using Level2;
using UnityEngine;

namespace Level3
{
    /// <summary>
    /// Puerta principal atrancada de una casa de la Aldea (Acto 3). Es un bloque sólido que no se abre:
    /// al examinarla con [E] indica por dónde entrar como gato (ventana alta o gatera).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class BarredHouseDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private string doorId = "barred_door";
        [SerializeField] private string houseDisplayName = "Casa de la Aldea";
        [TextArea(2, 3)]
        [SerializeField] private string entryHint = "Entra como Gato [T] por la ventana alta.";

        private float lastMessageTime = -10f;

        public string InteractionId => doorId;
        public int Priority => 8;
        public bool UsesInteractKey => true;
        public Transform Transform => transform;
        public float PromptHeightOffset => 2.2f;

        public void Configure(string id, string displayName, string hint)
        {
            doorId = id;
            houseDisplayName = displayName;
            entryHint = hint;
        }

        private void Awake()
        {
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            if (col != null) col.isTrigger = true;
        }

        public bool CanInteract(GameObject interactor) => interactor != null;

        public string GetPromptText(GameObject interactor)
        {
            return $"[{Level2Controls.InteractKey}] {houseDisplayName}: puerta atrancada";
        }

        public void Interact(GameObject interactor)
        {
            if (Time.unscaledTime < lastMessageTime + 0.5f) return;
            lastMessageTime = Time.unscaledTime;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayLockedSound();
            HUDController hud = Object.FindFirstObjectByType<HUDController>();
            if (hud != null) hud.ShowMessage($"La puerta de {houseDisplayName} está atrancada por dentro. {entryHint}", 3.2f);
        }
    }
}
