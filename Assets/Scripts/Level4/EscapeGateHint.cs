using UnityEngine;

namespace Salem.Level4
{
    /// <summary>
    /// Aviso junto a la reja del campanario (piso 4): explica por qué está cerrada y cómo abrirla.
    /// Lo crea PoisonFatherEncounter en tiempo de ejecución y se apaga al sonar la alarma.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class EscapeGateHint : MonoBehaviour
    {
        private string _prompt;
        private string _hudMessage;
        private float _nextHudTime;
        private bool _playerInside;

        public void Configure(string prompt, string hudMessage)
        {
            _prompt = prompt;
            _hudMessage = hudMessage;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _playerInside = true;
            Level2.InteractionPromptUI.Instance?.ShowPrompt(_prompt, transform, 1.8f);

            // Mensaje en el HUD sólo de vez en cuando para no saturar si el jugador va y viene
            if (!string.IsNullOrEmpty(_hudMessage) && Time.time >= _nextHudTime)
            {
                _nextHudTime = Time.time + 12f;
                FindFirstObjectByType<HUDController>()?.ShowMessage(_hudMessage, 4f);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _playerInside = false;
            Level2.InteractionPromptUI.Instance?.HidePrompt();
        }

        private void OnDisable()
        {
            if (_playerInside) Level2.InteractionPromptUI.Instance?.HidePrompt();
            _playerInside = false;
        }
    }
}
