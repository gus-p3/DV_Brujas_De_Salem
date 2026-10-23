using System;
using UnityEngine;

namespace Level2
{
    public enum IngredientType
    {
        Mushroom,
        Spider,
        WitheredFlower,
        Root
    }

    /// <summary>
    /// Componente recolectable para los ingredientes de poción del Nivel 2 (Acto 2).
    /// Reglas del GDD (§3.2, §3.4):
    /// - Solo puede recolectarse en forma humana (TransformationManager.IsCatForm == false).
    /// - En forma de gato no se recolecta y se avisa al jugador mediante banner en pantalla.
    /// - Al recolectarse, notifica a IngredientInventory, suma puntos y se destruye.
    /// - Flota suavemente (vaivén sinusoidal) en su posición inicial.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Ingredient : MonoBehaviour
    {
        [Header("Identificación y Tipo")]
        [Tooltip("Tipo de ingrediente recolectable.")]
        [SerializeField] private IngredientType ingredientType = IngredientType.Mushroom;

        [Tooltip("Identificador único del ingrediente en la escena para evitar doble recolección.")]
        [SerializeField] private string ingredientId = "ing_default";

        [Tooltip("Puntos otorgados al GameManager al recoger este ingrediente.")]
        [SerializeField] private int scoreValue = 150;

        [Header("Efectos Audiovisuales")]
        [Tooltip("Efecto de sonido al recolectar.")]
        [SerializeField] private AudioClip pickupSound;

        [Tooltip("Efecto de partículas instanciado al recolectar.")]
        [SerializeField] private GameObject pickupParticles;

        [Header("Animación de Flotación (Bobbing)")]
        [Tooltip("Amplitud vertical de flotación.")]
        [SerializeField] private float bobAmplitude = 0.05f;

        [Tooltip("Velocidad de oscilación.")]
        [SerializeField] private float bobSpeed = 3.0f;

        [Header("Avisos y Feedback")]
        [Tooltip("Tiempo de enfriamiento entre avisos al intentar recolectar en forma de gato.")]
        [SerializeField] private float catWarningCooldown = 2.0f;

        public IngredientType Type => ingredientType;
        public string Id => ingredientId;
        public int ScoreValue => scoreValue;

        private Collider2D triggerCollider;
        private Vector3 initialPosition;
        private float timeOffset;
        private float lastWarningTime = -10f;
        private bool isCollected = false;

        private void Awake()
        {
            triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }

            initialPosition = transform.position;
            timeOffset = UnityEngine.Random.Range(0f, 6.28f);

            // Capa Collectible si existe, sin interferir con la matriz global
            int colLayer = LayerMask.NameToLayer("Collectible");
            if (colLayer >= 0)
            {
                gameObject.layer = colLayer;
            }
        }

        private void Start()
        {
            // Si el ingrediente ya fue recolectado en esta sesión, desactivarlo inmediatamente
            if (IngredientInventory.Instance != null && IngredientInventory.Instance.IsCollected(ingredientId))
            {
                isCollected = true;
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (isCollected) return;

            // Vaivén vertical suave
            float newY = initialPosition.y + Mathf.Sin((Time.time + timeOffset) * bobSpeed) * bobAmplitude;
            transform.position = new Vector3(initialPosition.x, newY, initialPosition.z);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryCollect(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryCollect(other);
        }

        private void TryCollect(Collider2D other)
        {
            if (isCollected) return;

            PlayerController player = other != null ? other.GetComponentInParent<PlayerController>() : null;
            if (player == null && (other == null || !other.CompareTag("Player"))) return;

            TransformationManager tm = other.GetComponentInParent<TransformationManager>();
            if (tm == null)
            {
                tm = UnityEngine.Object.FindFirstObjectByType<TransformationManager>();
            }

            // GDD §3.2: en forma de gato la bruja NO puede recolectar ingredientes
            if (tm != null && tm.IsCatForm)
            {
                NotifyCatFormBlocked();
                return;
            }

            // En forma humana: recolección exitosa
            ExecuteCollection();
        }

        private void NotifyCatFormBlocked()
        {
            if (Time.unscaledTime - lastWarningTime < catWarningCooldown) return;
            lastWarningTime = Time.unscaledTime;

            string warning = "Necesitas forma humana para recoger esto";

            // Intentar notificar por el HUDController existente si está disponible
            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage(warning, 2.0f);
            }

            // También notificar al HUD específico de ingredientes si existe
            if (IngredientHUD.Instance != null)
            {
                IngredientHUD.Instance.ShowWarning(warning);
            }

            // Audio sutil de rechazo / aviso
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClickSound();
            }
        }

        private void ExecuteCollection()
        {
            isCollected = true;

            // Registrar en el inventario dedicado de ingredientes
            if (IngredientInventory.Instance != null)
            {
                IngredientInventory.Instance.Collect(this);
            }

            // Añadir puntuación al GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(scoreValue);
            }

            // Efecto de sonido
            if (AudioManager.Instance != null)
            {
                if (pickupSound != null)
                {
                    AudioManager.Instance.PlaySFX(pickupSound);
                }
                else
                {
                    AudioManager.Instance.PlayButtonClickSound();
                }
            }

            // Partículas
            if (pickupParticles != null)
            {
                Instantiate(pickupParticles, transform.position, Quaternion.identity);
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
#else
            Destroy(gameObject);
#endif
        }

        /// <summary>
        /// Permite inicializar programáticamente identificador y tipo desde builders o pruebas.
        /// </summary>
        public void Configure(string id, IngredientType type, int score = 150)
        {
            ingredientId = id;
            ingredientType = type;
            scoreValue = score;
        }
    }
}
