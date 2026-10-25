using UnityEngine;

namespace Level2
{
    /// <summary>
    /// Punto de interés narrativo (grimorio, diario, actas del juicio, recetario)
    /// examinable con la tecla E dentro de las casas explorables del Nivel 2.
    /// Otorga una bonificación única de exploración la primera vez que se examina.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class LoreObject : MonoBehaviour, IInteractable
    {
        [SerializeField] private string loreId = "lore_default";
        [SerializeField] private string objectTitle = "Grimorio Antiguo";
        [TextArea(2, 4)]
        [SerializeField] private string loreMessage = "«En las raíces y flores del mercado reside la llave del sello eclesiástico...»";
        [SerializeField] private int firstReadBonusScore = 100;

        public string InteractionId => loreId;
        public int Priority => 12;
        public bool UsesInteractKey => true;
        public Transform Transform => transform;
        public float PromptHeightOffset => 1.15f;

        private void Awake()
        {
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            if (col != null) col.isTrigger = true;
        }

        public void Configure(string id, string title, string message, int bonusScore = 100)
        {
            loreId = id;
            objectTitle = title;
            loreMessage = message;
            firstReadBonusScore = bonusScore;
        }

        public bool CanInteract(GameObject interactor)
        {
            return interactor != null;
        }

        public string GetPromptText(GameObject interactor)
        {
            return $"Q: Examinar {objectTitle}";
        }

        public void Interact(GameObject interactor)
        {
            if (interactor != null)
            {
                TransformationManager tm = interactor.GetComponent<TransformationManager>();
                if (tm != null && tm.IsCatForm)
                {
                    Level2CatPickupGuard.NotifyCatBlocked(forceShow: true);
                    return;
                }
            }

            bool firstTime = KeyInventory.Instance != null && KeyInventory.Instance.MarkSecretDiscovered(loreId);
            if (firstTime && GameManager.Instance != null && firstReadBonusScore > 0)
            {
                GameManager.Instance.AddScore(firstReadBonusScore);
            }

            string suffix = firstTime && firstReadBonusScore > 0 ? $" (+{firstReadBonusScore} pts)" : "";
            if (Level2LoreReader.Instance != null)
            {
                Level2LoreReader.Instance.Show(objectTitle, loreMessage + (suffix.Length > 0 ? "\n\n<size=75%>" + suffix.Trim() + "</size>" : ""));
            }
            else
            {
                HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
                if (hud != null) hud.ShowMessage($"{objectTitle}: {loreMessage}{suffix}", 3.2f);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClickSound();
            }
        }
    }
}
