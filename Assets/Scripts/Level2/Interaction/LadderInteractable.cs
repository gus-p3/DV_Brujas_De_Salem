using System.Collections.Generic;
using UnityEngine;

namespace Level2
{
    /// <summary>
    /// Componente de escalera (de mano o interior de madera) que permite subir y bajar entre pisos.
    /// Define los límites verticales (BottomY y TopY) y las plataformas superiores de un solo sentido
    /// que la bruja puede atravesar limpiamente durante el ascenso o descenso.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class LadderInteractable : MonoBehaviour, IInteractable
    {
        [Header("Identificación")]
        [SerializeField] private string ladderId = "ladder_default";
        [SerializeField] private int priority = 5;

        [Header("Geometría de Escalada")]
        [Tooltip("Altura Y de la base de la escalera en coordenadas de mundo (si autoCalculateBounds está desactivado).")]
        [SerializeField] private float bottomY = 0.17f;
        [Tooltip("Altura Y de la superficie del piso superior al que llega la escalera.")]
        [SerializeField] private float topY = 3.8f;
        [Tooltip("Si está activo, calcula bottomY y topY a partir del BoxCollider2D al inicializar.")]
        [SerializeField] private bool autoCalculateBounds = false;
        [Tooltip("Margen superior del trigger por encima de topY para detectar al jugador cuando pisa el piso superior.")]
        [SerializeField] private float topStandTriggerMargin = 0.75f;

        [Header("Plataformas Superiores Asociadas")]
        [Tooltip("Plataformas (suelos de segundo piso/balcón) que la bruja puede atravesar al subir o bajar por esta escalera.")]
        [SerializeField] private List<Collider2D> passThroughPlatforms = new List<Collider2D>();

        private BoxCollider2D triggerCollider;

        public string InteractionId => ladderId;
        public int Priority => priority;
        public bool UsesInteractKey => false; // Se usa con W / S (↑ / ↓)
        public Transform Transform => transform;
        public float PromptHeightOffset => 1.1f;

        public float BottomY => bottomY;
        public float TopY => topY;
        public float CenterX => transform.position.x;
        public IReadOnlyList<Collider2D> PassThroughPlatforms => passThroughPlatforms;

        private void Awake()
        {
            EnsureColliderSetup();
        }

        public void EnsureColliderSetup()
        {
            if (triggerCollider == null)
            {
                triggerCollider = GetComponent<BoxCollider2D>();
            }
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
                if (autoCalculateBounds)
                {
                    Bounds b = triggerCollider.bounds;
                    bottomY = b.min.y;
                    topY = b.max.y - topStandTriggerMargin;
                }
            }
            AutoDetectPassThroughPlatforms();
        }

        /// <summary>
        /// Configura los parámetros de la escalera desde constructores de escena o código.
        /// Ajusta automáticamente el BoxCollider2D para cubrir desde bottomY hasta topY + margen superior.
        /// </summary>
        public void Configure(string id, float bottomWorldY, float topWorldY, float width = 0.95f, Collider2D topPlatform = null)
        {
            ladderId = id;
            bottomY = bottomWorldY;
            topY = topWorldY;
            autoCalculateBounds = false;

            if (triggerCollider == null)
            {
                triggerCollider = GetComponent<BoxCollider2D>();
            }
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
                float totalHeight = Mathf.Max(1.0f, (topY - bottomY) + topStandTriggerMargin);
                float centerWorldY = bottomY + totalHeight * 0.5f;
                triggerCollider.size = new Vector2(width, totalHeight);
                triggerCollider.offset = new Vector2(0f, centerWorldY - transform.position.y);
            }

            if (topPlatform != null && !passThroughPlatforms.Contains(topPlatform))
            {
                passThroughPlatforms.Add(topPlatform);
            }
            AutoDetectPassThroughPlatforms();
        }

        /// <summary>
        /// Detecta automáticamente cualquier plataforma en la capa Ground cerca del extremo superior (topY)
        /// para permitir bajar desde el piso superior sin quedar bloqueado.
        /// </summary>
        public void AutoDetectPassThroughPlatforms()
        {
            passThroughPlatforms.RemoveAll(c => c == null);
            int groundMask = LayerMask.GetMask("Ground");
            if (groundMask == 0) groundMask = ~0;

            Vector2 checkCenter = new Vector2(CenterX, topY - 0.1f);
            Vector2 checkSize = new Vector2(1.2f, 0.7f);
            Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, checkSize, 0f, groundMask);
            foreach (Collider2D hit in hits)
            {
                if (hit == null || hit.isTrigger || hit == triggerCollider) continue;
                // No ignorar nunca el suelo principal de la calle (Ground_Z*) ni paredes altas
                if (hit.name.StartsWith("Ground_Z") || hit.bounds.size.y > 1.2f) continue;
                if (!passThroughPlatforms.Contains(hit))
                {
                    passThroughPlatforms.Add(hit);
                }
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            if (interactor == null) return false;
            return true;
        }

        public string GetPromptText(GameObject interactor)
        {
            if (interactor == null) return "W / ↑ — Subir  |  S / ↓ — Bajar";

            LadderClimber climber = interactor.GetComponent<LadderClimber>();
            if (climber != null && climber.IsClimbing && climber.CurrentLadder == this)
            {
                return "W / S — Subir / Bajar   |   Espacio — Saltar";
            }

            BoxCollider2D col = interactor.GetComponent<BoxCollider2D>();
            float feetY = col != null ? col.bounds.min.y : interactor.transform.position.y;

            if (feetY >= topY - 0.35f)
            {
                return "S / ↓ — Bajar escalera";
            }
            if (feetY <= bottomY + 0.45f)
            {
                return "W / ↑ — Subir escalera";
            }
            return "W / ↑ — Subir   |   S / ↓ — Bajar";
        }

        public void Interact(GameObject interactor)
        {
            // La escalada se inicia mediante W/S en LadderClimber, pero si se invoca Interact directamente,
            // posiciona al jugador en la escalera.
            if (interactor == null) return;
            LadderClimber climber = interactor.GetComponent<LadderClimber>();
            if (climber != null && !climber.IsClimbing)
            {
                climber.StartClimbing(this, 0f);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.95f, 0.72f, 0.22f, 0.85f);
            Vector3 b = new Vector3(CenterX, bottomY, 0f);
            Vector3 t = new Vector3(CenterX, topY, 0f);
            Gizmos.DrawLine(b + Vector3.left * 0.35f, b + Vector3.right * 0.35f);
            Gizmos.DrawLine(t + Vector3.left * 0.35f, t + Vector3.right * 0.35f);
            Gizmos.DrawLine(b + Vector3.left * 0.25f, t + Vector3.left * 0.25f);
            Gizmos.DrawLine(b + Vector3.right * 0.25f, t + Vector3.right * 0.25f);
        }
    }
}
