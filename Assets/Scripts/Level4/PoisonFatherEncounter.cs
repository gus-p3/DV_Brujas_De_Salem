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
        [Tooltip("Referencia al jefe Padre Parris Maligno. Al envenenar el c\u00e1liz el jefe colapsa.")]
        [SerializeField] private EvilPriestBossAI bossAI;

        [Header("Audio")]
        [SerializeField] private AudioClip poisonSfx;
        [SerializeField] private AudioClip preacherGaspSfx;
        [SerializeField] private AudioClip alarmMusic;

        [Header("UI Prompts")]
        [SerializeField] private string readyPrompt = "[E] Verter la Poción Maestra en el Cáliz del Padre Parris";
        [SerializeField] private string completedPrompt = "¡El cáliz ha sido envenenado! ¡La alarma ha sonado, huye hacia el campanario!";

        [SerializeField] private string gateLockedPrompt = "La reja está cerrada. Se abrirá cuando la alarma suene.";
        [SerializeField] private string gateLockedHud = "La reja del campanario no cede. Envenena el cáliz del Padre Parris con [E] para que suene la alarma y se abra el paso.";

        private bool _isPlayerNear = false;
        private bool _isPoisoned = false;
        private GameObject _gateHint;

        public bool IsPoisoned => _isPoisoned;

        private void Start()
        {
            CreateGateHint();
        }

        /// <summary>Zona de aviso frente a la reja (lado oeste, por donde llega el jugador).</summary>
        private void CreateGateHint()
        {
            if (escapePathBlocker == null) return;
            Collider2D blocker = escapePathBlocker.GetComponentInChildren<Collider2D>(true);
            if (blocker == null) return;

            Bounds b = blocker.bounds;
            _gateHint = new GameObject("Escape_Gate_Hint");
            _gateHint.transform.SetParent(escapePathBlocker.transform.parent, false);
            _gateHint.transform.position = new Vector3(b.min.x - 1.6f, b.center.y, 0f);
            var col = _gateHint.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(3.2f, Mathf.Max(3f, b.size.y));
            _gateHint.AddComponent<EscapeGateHint>().Configure(gateLockedPrompt, gateLockedHud);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isPoisoned) return;
            if (other.CompareTag("Player"))
            {
                _isPlayerNear = true;
                GameManager.Instance?.UnlockMasterPotionUsage(); // solo frente al Padre se puede lanzar/verter
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
                if (!_isPoisoned) GameManager.Instance?.LockMasterPotionUsage();
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
            if (GameManager.Instance != null)
            {
                // Si además se lanzó con E en el mismo cuadro, la carga ya se gastó: no bajar de 0
                GameManager.Instance.MasterCharges = 0;
                GameManager.Instance.LockMasterPotionUsage();
            }
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

            // Colapsar el jefe si ya se había transformado
            bossAI?.Collapse();

            // Colapsar el sprite normal del Padre (por si el jefe no se había transformado aún)
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

            OpenEscapeRoute(hud);
        }

        /// <summary>
        /// Abre la reja del campanario y activa la alarma.
        /// Llamado por PoisonSequence al envenenar el cáliz.
        /// </summary>
        public void OpenEscapeRoute(HUDController hud = null)
        {
            if (escapePathBlocker != null) escapePathBlocker.SetActive(false);
            if (_gateHint != null) _gateHint.SetActive(false);
            if (alarmVisuals != null) alarmVisuals.SetActive(true);

            if (hud == null) hud = FindObjectOfType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage("¡Alarma en la catedral! ¡Sube a la cúpula antes de que te atrapen!", 3.5f);
            }

            if (alarmMusic != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(alarmMusic);
            }
        }
    }
}
