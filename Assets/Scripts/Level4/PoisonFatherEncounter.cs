using System.Collections;
using UnityEngine;

namespace Salem.Level4
{
    public class PoisonFatherEncounter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer chaliceRenderer;
        [SerializeField] private Sprite poisonedChaliceSprite;
        [SerializeField] private SpriteRenderer preacherRenderer;
        [SerializeField] private Sprite preacherCollapsedSprite;
        [SerializeField] private GameObject escapePathBlocker;
        [SerializeField] private GameObject alarmVisuals;

        [Header("Audio")]
        [SerializeField] private AudioClip poisonSfx;
        [SerializeField] private AudioClip preacherGaspSfx;
        [SerializeField] private AudioClip alarmMusic;

        [Header("UI Prompts")]
        [SerializeField] private string readyPrompt = "[E] Verter la Poción Maestra en el Cáliz del Padre Parris";
        [SerializeField] private string completedPrompt = "¡El cáliz ha sido envenenado! ¡La alarma ha sonado, huye hacia el campanario!";

        private bool _isPlayerNear = false;
        private bool _isPoisoned = false;

        public bool IsPoisoned => _isPoisoned;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isPoisoned) return;
            if (other.CompareTag("Player"))
            {
                _isPlayerNear = true;
                if (Level2.InteractionPromptUI.Instance != null)
                {
                    Level2.InteractionPromptUI.Instance.ShowPrompt(readyPrompt, transform, 1.2f);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _isPlayerNear = false;
                if (Level2.InteractionPromptUI.Instance != null)
                {
                    Level2.InteractionPromptUI.Instance.HidePrompt();
                }
            }
        }

        private void Update()
        {
            if (!_isPlayerNear || _isPoisoned) return;

            if (Input.GetKeyDown(KeyCode.E))
            {
                StartCoroutine(PoisonSequence());
            }
        }

        private IEnumerator PoisonSequence()
        {
            _isPoisoned = true;
            if (Level2.InteractionPromptUI.Instance != null)
            {
                Level2.InteractionPromptUI.Instance.HidePrompt();
            }

            // Play poison liquid sound
            if (poisonSfx != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(poisonSfx);
            }

            if (chaliceRenderer != null && poisonedChaliceSprite != null)
            {
                chaliceRenderer.sprite = poisonedChaliceSprite;
            }

            yield return new WaitForSeconds(1.2f);

            // Preacher collapses
            if (preacherRenderer != null && preacherCollapsedSprite != null)
            {
                preacherRenderer.sprite = preacherCollapsedSprite;
            }

            if (preacherGaspSfx != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(preacherGaspSfx);
            }

            var hud = FindObjectOfType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage("Padre Parris: '¡Maldición... la poción... GUARDIAAS!'", 3.0f);
            }

            yield return new WaitForSeconds(2.0f);

            // Trigger alarm, open escape climb route
            if (escapePathBlocker != null) escapePathBlocker.SetActive(false);
            if (alarmVisuals != null) alarmVisuals.SetActive(true);

            if (hud != null)
            {
                hud.ShowMessage("¡Alarma en la catedral! ¡Sube a la cúpula antes de que te atrapen!", 3.5f);
            }

            // Switch to escape music
            if (alarmMusic != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(alarmMusic);
            }
        }
    }
}
