using Level2;
using UnityEngine;

namespace Level3
{
    /// <summary>
    /// Trigger e interactable en el mundo colocado en el Cartel Gótico Nocturno al final de Level3_Village.
    /// Responde tanto a la entrada física (trigger collider) como a la interacción con la tecla [E].
    /// Inicia la secuencia narrativa y visual de finalización del Nivel 3.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Level3VillageEndingTrigger : MonoBehaviour, IInteractable
    {
        [Header("Identificación")]
        [SerializeField] private string triggerId = "level3_village_end_gothic_sign";
        [SerializeField] private string promptMessage = "[E] Entrar a los Terrenos de la Catedral Gótica";

        private BoxCollider2D boxCol;
        private bool hasTriggered = false;

        public string InteractionId => triggerId;
        public int Priority => 50;
        public bool UsesInteractKey => true;
        public Transform Transform => transform;
        public float PromptHeightOffset => 2.4f;

        private void Awake()
        {
            boxCol = GetComponent<BoxCollider2D>();
            if (boxCol != null)
            {
                boxCol.isTrigger = true;
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            if (hasTriggered) return false;
            if (interactor == null) return false;
            Level3VillageEndingCutscene cutscene = Level3VillageEndingCutscene.Instance;
            if (cutscene != null && cutscene.IsCompleted) return false;
            return true;
        }

        public string GetPromptText(GameObject interactor)
        {
            if (hasTriggered) return string.Empty;
            return promptMessage;
        }

        public void Interact(GameObject interactor)
        {
            if (hasTriggered) return;
            hasTriggered = true;
            TriggerEnding();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasTriggered || other == null) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;

            hasTriggered = true;
            TriggerEnding();
        }

        private void TriggerEnding()
        {
            Level3VillageEndingCutscene cutscene = Level3VillageEndingCutscene.Instance;
            if (cutscene != null)
            {
                cutscene.TriggerEndingSequence();
            }
            else
            {
                Debug.LogWarning("[Level3VillageEndingTrigger] No se halló instancia de Level3VillageEndingCutscene en escena.");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CompleteLevel();
                }
            }
        }
    }
}
