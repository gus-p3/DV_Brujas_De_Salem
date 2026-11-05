using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Level3
{
    /// <summary>
    /// Reliquia mágica oculta en áreas secretas, rutas aéreas o catacumbas del Nivel 3.
    /// Incentiva la exploración y el uso avanzado de la Escoba y la transformación en Gato.
    /// Otorga puntuación adicional, sonido de descubrimiento y un aviso en el HUD.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Level3SecretRelic : MonoBehaviour
    {
        public static int TotalRelicsInLevel { get; private set; }
        public static int CollectedRelicsCount { get; private set; }

        [Header("Información de la Reliquia")]
        [SerializeField] private string relicId = "relic_grimoire";
        [SerializeField] private string relicName = "Grimorio Olvidado de Salem";
        [SerializeField] private int scoreValue = 350;

        [Header("Visuales")]
        [SerializeField] private Sprite relicSprite;
        [SerializeField] private Color glowColor = new Color(0.95f, 0.78f, 0.25f, 1f);
        [SerializeField] private float bobbingSpeed = 2.4f;
        [SerializeField] private float bobbingHeight = 0.18f;

        private bool isCollected = false;
        private Vector3 startPos;
        private SpriteRenderer spriteRenderer;
        private Light2D relicLight;

        public string RelicId => relicId;
        public string RelicName => relicName;
        public int ScoreValue => scoreValue;
        public bool IsCollected => isCollected;

        public event Action<Level3SecretRelic> OnRelicCollected;

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            relicLight = GetComponentInChildren<Light2D>();
            startPos = transform.position;

            TotalRelicsInLevel++;
        }

        private void OnDestroy()
        {
            TotalRelicsInLevel = Mathf.Max(0, TotalRelicsInLevel - 1);
        }

        public void Configure(string id, string name, int score, Sprite sprite = null, Color? customGlow = null)
        {
            relicId = id;
            relicName = name;
            scoreValue = score;
            if (sprite != null)
            {
                relicSprite = sprite;
                if (spriteRenderer != null) spriteRenderer.sprite = sprite;
            }
            if (customGlow.HasValue)
            {
                glowColor = customGlow.Value;
                if (relicLight != null) relicLight.color = glowColor;
            }
        }

        private void Update()
        {
            if (isCollected) return;

            // Suave oscilación flotante mística
            float yOffset = Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
            transform.position = startPos + new Vector3(0f, yOffset, 0f);

            if (relicLight != null)
            {
                relicLight.intensity = 0.9f + 0.35f * Mathf.Sin(Time.time * (bobbingSpeed * 1.5f));
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryCollect(other);
        }

        private void TryCollect(Collider2D other)
        {
            if (isCollected || other == null) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;

            isCollected = true;
            CollectedRelicsCount++;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(scoreValue);
            }

            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage($"★ ¡RELIQUIA ENCONTRADA: {relicName.ToUpper()}! (+{scoreValue} pts)", 3.2f);
            }

            if (AudioManager.Instance != null && AudioManager.Instance.potionPickupClip != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.potionPickupClip);
            }

            OnRelicCollected?.Invoke(this);

            gameObject.SetActive(false);
        }
    }
}
