using System.Collections;
using UnityEngine;

namespace Salem.Level4
{
    [RequireComponent(typeof(Collider2D))]
    public class GreatBellRopeTrigger : MonoBehaviour
    {
        [Header("Bell Elements")]
        [SerializeField] private Transform bellTransform;
        [SerializeField] private AudioClip bellTollSfx;
        [SerializeField] private SpriteRenderer shockwaveRenderer;
        [SerializeField] private GameObject escapeWindowZone;

        [Header("State")]
        [SerializeField] private bool hasTolled = false;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

            if (shockwaveRenderer != null)
            {
                shockwaveRenderer.gameObject.SetActive(false);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasTolled) return;

            if (other.CompareTag("Player"))
            {
                var tm = other.GetComponent<TransformationManager>();
                bool isCat = tm != null && tm.IsCatForm;

                // User requested: in cat form she jumps onto obstacles to ring the bell
                if (isCat)
                {
                    StartCoroutine(TollBellRoutine());
                }
                else
                {
                    var hud = FindObjectOfType<HUDController>();
                    if (hud != null)
                    {
                        hud.ShowMessage("¡Salta en forma de gato para impulsarte y hacer repicar la Gran Campana!", 2.5f);
                    }
                }
            }
        }

        private IEnumerator TollBellRoutine()
        {
            hasTolled = true;

            // Play bell sound
            if (bellTollSfx != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(bellTollSfx);
            }

            // Stun all inquisitors and guards
            var allInquisitors = FindObjectsOfType<InquisitorGuardAI>();
            foreach (var inq in allInquisitors)
            {
                inq.ApplyStun(8.0f);
            }

            var allGuards = FindObjectsOfType<VillagerAI>();
            foreach (var g in allGuards)
            {
                g.ApplySleep(8.0f);
            }

            // Shockwave expansion animation
            if (shockwaveRenderer != null)
            {
                shockwaveRenderer.gameObject.SetActive(true);
                shockwaveRenderer.transform.localScale = Vector3.zero;
                float elapsed = 0f;
                float duration = 1.2f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    shockwaveRenderer.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 6.0f, t);
                    Color c = shockwaveRenderer.color;
                    c.a = Mathf.Lerp(1.0f, 0f, t);
                    shockwaveRenderer.color = c;
                    yield return null;
                }
                shockwaveRenderer.gameObject.SetActive(false);
            }

            // Bell swing oscillation
            if (bellTransform != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    bellTransform.localRotation = Quaternion.Euler(0, 0, 18f);
                    yield return new WaitForSeconds(0.12f);
                    bellTransform.localRotation = Quaternion.Euler(0, 0, -18f);
                    yield return new WaitForSeconds(0.12f);
                }
                bellTransform.localRotation = Quaternion.identity;
            }

            var hud = FindObjectOfType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage("¡El estruendo ensordeció a los guardias! ¡La ventana de la torre está rota, SALTA Y ESCAPA!", 3.5f);
            }

            if (escapeWindowZone != null)
            {
                escapeWindowZone.SetActive(true);
            }
        }
    }
}
