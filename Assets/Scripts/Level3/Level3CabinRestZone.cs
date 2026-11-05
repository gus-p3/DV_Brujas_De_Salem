using UnityEngine;

namespace Level3
{
    /// <summary>
    /// Zona de descanso de la Cabaña de la Bruja (Parte 1 del Nivel 3). La primera vez que la bruja entra:
    /// fundido "Llegaste a la cabaña. Descansa…", corazones restaurados y aviso para preparar la Poción Maestra.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Level3CabinRestZone : MonoBehaviour
    {
        [SerializeField] private string restKicker = "NIVEL 3  ·  PARTE 1";
        [SerializeField] private string restTitle = "Llegaste a la cabaña";
        [TextArea(2, 3)]
        [SerializeField] private string restSubtitle = "Descansa junto al fuego… Tus corazones se restauran.";
        [TextArea(2, 3)]
        [SerializeField] private string afterRestMessage = "Descansaste en tu cabaña. Prepara la Poción Maestra en el Caldero con [E].";

        private bool used;

        public bool HasRested => used;

        private void Awake()
        {
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (used || other == null) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            used = true;

            Level3StateController state = Level3StateController.Instance;
            if (state != null) state.AdvanceToBrewPhase();

            Level3ChapterCard card = Level3ChapterCard.Instance;
            if (card != null)
            {
                card.PlayRest(restKicker, restTitle, restSubtitle, Rest, ShowAfterMessage);
            }
            else
            {
                Rest();
                ShowAfterMessage();
            }
        }

        private static void Rest()
        {
            if (GameManager.Instance != null) GameManager.Instance.RestoreAllHearts();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayCheckpointSound();
        }

        private void ShowAfterMessage()
        {
            HUDController hud = Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.UpdateStatsUI();
                hud.ShowMessage(afterRestMessage, 3.6f);
            }
        }
    }
}
