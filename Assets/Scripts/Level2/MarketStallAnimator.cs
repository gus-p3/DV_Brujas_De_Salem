using UnityEngine;

namespace Level2
{
    /// <summary>
    /// Animación procedural desincronizada para los puestos del mercado del Nivel 2.
    /// - Balancea suavemente el toldo visual (rotación y respiración sutil de tela al viento).
    /// - Oscila como péndulo los colgantes del puesto (hierbas, telas, farolillos).
    /// - Cada instancia usa una fase inicial y un multiplicador de velocidad distinto (±12%)
    ///   basado en su posición para que ningún puesto se mueva al unísono.
    /// - Solo modifica transformaciones visuales sin colliders sólidos ni efecto en física o IA.
    /// </summary>
    public class MarketStallAnimator : MonoBehaviour
    {
        [Header("Balanceo del Toldo")]
        [SerializeField] private Transform awningVisual;
        [SerializeField] private float awningSwayAngle = 1.6f;
        [SerializeField] private float awningScalePulse = 0.018f;
        [SerializeField] private float baseSpeed = 1.85f;

        [Header("Colgantes / Mercancía")]
        [SerializeField] private Transform[] hangingItems;
        [SerializeField] private float hangingSwingAngle = 4.8f;

        [Header("Instancia Desincronizada")]
        [SerializeField] private float phaseOffset;
        [SerializeField] private float speedMultiplier = 1.0f;

        private Vector3 awningBaseScale = Vector3.one;
        private Quaternion awningBaseRot = Quaternion.identity;

        public float PhaseOffset => phaseOffset;
        public float SpeedMultiplier => speedMultiplier;
        public Transform AwningVisual => awningVisual;
        public Transform[] HangingItems => hangingItems;

        private void Awake()
        {
            InitializeDesyncFromPosition();
            CacheAwningBase();
        }

        public void Configure(Transform awning, Transform[] pendants, float customPhase, float customSpeedMult)
        {
            awningVisual = awning;
            hangingItems = pendants;
            phaseOffset = customPhase;
            speedMultiplier = Mathf.Clamp(customSpeedMult, 0.85f, 1.15f);
            CacheAwningBase();
        }

        private void InitializeDesyncFromPosition()
        {
            if (Mathf.Approximately(phaseOffset, 0f))
            {
                float seed = Mathf.Abs(transform.position.x * 1.37f + transform.position.y * 2.91f);
                phaseOffset = (seed % 6.28318f) + 0.35f;
                float norm = (Mathf.Sin(seed * 3.17f) + 1f) * 0.5f; // 0..1
                speedMultiplier = Mathf.Lerp(0.89f, 1.11f, norm);
            }
        }

        private void CacheAwningBase()
        {
            if (awningVisual != null)
            {
                awningBaseScale = awningVisual.localScale;
                awningBaseRot = awningVisual.localRotation;
            }
        }

        private void Update()
        {
            float t = Time.time * baseSpeed * speedMultiplier + phaseOffset;

            if (awningVisual != null)
            {
                // Si el toldo tiene un BoxCollider2D directamente, solo aplicamos pulso visual sutil o rotamos si no afecta al suelo
                float sway = Mathf.Sin(t) * awningSwayAngle;
                float pulseY = 1f + Mathf.Sin(t * 1.7f + 0.8f) * awningScalePulse;
                awningVisual.localRotation = awningBaseRot * Quaternion.Euler(0f, 0f, sway * 0.45f);
                awningVisual.localScale = new Vector3(awningBaseScale.x, awningBaseScale.y * pulseY, awningBaseScale.z);
            }

            if (hangingItems != null)
            {
                for (int i = 0; i < hangingItems.Length; i++)
                {
                    Transform h = hangingItems[i];
                    if (h == null) continue;
                    float itemPhase = t * 1.35f + i * 1.4f;
                    float angle = Mathf.Sin(itemPhase) * hangingSwingAngle;
                    h.localRotation = Quaternion.Euler(0f, 0f, angle);
                }
            }
        }
    }
}
