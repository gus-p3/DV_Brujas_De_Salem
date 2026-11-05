using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Level3
{
    /// <summary>
    /// Peligro ambiental rítmico (géiser de vaho tóxico o foso de espinas de las ciénagas) en el Nivel 3.
    /// Alterna entre fase inactiva (segura) y erupción activa con aviso visual previo.
    /// Si la bruja lo toca durante la fase activa, sufre daño de 1 corazón respetando la invulnerabilidad estándar.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Level3EnvironmentalHazard : MonoBehaviour
    {
        [Header("Tiempos de Ciclo")]
        [Tooltip("Duración de la fase segura/inactiva (segundos).")]
        [SerializeField] private float dormantDuration = 3.5f;
        [Tooltip("Duración del aviso previo (burbujeo/destello) antes de erupcionar.")]
        [SerializeField] private float warningDuration = 1.0f;
        [Tooltip("Duración de la erupción activa con daño.")]
        [SerializeField] private float activeDuration = 2.2f;

        [Header("Efectos Visuales")]
        [SerializeField] private SpriteRenderer hazardRenderer;
        [SerializeField] private Light2D hazardLight;
        [SerializeField] private Color dormantColor = new Color(0.2f, 0.45f, 0.25f, 0.35f);
        [SerializeField] private Color warningColor = new Color(0.85f, 0.65f, 0.15f, 0.8f);
        [SerializeField] private Color activeColor = new Color(0.6f, 0.15f, 0.85f, 0.95f);

        [Header("Daño y Empuje")]
        [SerializeField] private int damageAmount = 1;
        [SerializeField] private string damageCause = "Vaho Tóxico de las Ciénagas";

        private enum HazardPhase
        {
            Dormant,
            Warning,
            Active
        }

        private HazardPhase currentPhase = HazardPhase.Dormant;
        private float phaseTimer = 0f;
        private BoxCollider2D triggerCol;

        public bool IsDangerous => currentPhase == HazardPhase.Active;

        private void Awake()
        {
            triggerCol = GetComponent<BoxCollider2D>();
            triggerCol.isTrigger = true;

            if (hazardRenderer == null) hazardRenderer = GetComponentInChildren<SpriteRenderer>();
            if (hazardLight == null) hazardLight = GetComponentInChildren<Light2D>();

            phaseTimer = dormantDuration;
            ApplyVisualPhase();
        }

        private void Update()
        {
            phaseTimer -= Time.deltaTime;
            if (phaseTimer <= 0f)
            {
                AdvancePhase();
            }

            if (hazardLight != null)
            {
                float pulse = currentPhase == HazardPhase.Active
                    ? 1.2f + 0.3f * Mathf.Sin(Time.time * 8f)
                    : (currentPhase == HazardPhase.Warning ? 0.7f + 0.3f * Mathf.Sin(Time.time * 12f) : 0.25f);
                hazardLight.intensity = pulse;
            }
        }

        private void AdvancePhase()
        {
            switch (currentPhase)
            {
                case HazardPhase.Dormant:
                    currentPhase = HazardPhase.Warning;
                    phaseTimer = warningDuration;
                    break;
                case HazardPhase.Warning:
                    currentPhase = HazardPhase.Active;
                    phaseTimer = activeDuration;
                    break;
                case HazardPhase.Active:
                    currentPhase = HazardPhase.Dormant;
                    phaseTimer = dormantDuration;
                    break;
            }
            ApplyVisualPhase();
        }

        private void ApplyVisualPhase()
        {
            Color targetCol = currentPhase switch
            {
                HazardPhase.Active => activeColor,
                HazardPhase.Warning => warningColor,
                _ => dormantColor
            };

            if (hazardRenderer != null)
            {
                hazardRenderer.color = targetCol;
            }

            if (hazardLight != null)
            {
                hazardLight.color = targetCol;
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (currentPhase != HazardPhase.Active) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;

            PlayerInvulnerability inv = player.GetComponent<PlayerInvulnerability>();
            if (inv != null && inv.IsInvulnerable) return;

            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                GameManager.Instance.LoseHeart(damageAmount, damageCause);
            }
        }
    }
}
