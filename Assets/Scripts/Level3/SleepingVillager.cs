using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Level3
{
    /// <summary>
    /// Aldeano dormido dentro de una casa de la Aldea de Salem (Acto 3).
    /// Mide el ruido que hace la bruja mientras está dentro de su casa:
    /// - Forma humana dentro de la casa: ruido muy alto (y volver a humana ahí dentro es un golpe de ruido).
    /// - Caminar rápido (sin sigilo) o saltar: ruido moderado por segundo / por salto.
    /// - Gato caminando con sigilo (Shift): casi silencio; el ruido acumulado se va disipando.
    /// Si el medidor se llena, el aldeano despierta gritando "¡BRUJA!": la bruja pierde un corazón y es
    /// devuelta al último punto de control (y, si estaba en forma humana, arranca el Frenesí de Aldeanos).
    /// Al rato vuelve a dormirse para que la casa pueda intentarse de nuevo.
    /// </summary>
    public class SleepingVillager : MonoBehaviour
    {
        private static readonly List<SleepingVillager> All = new List<SleepingVillager>();

        [Header("Casa que vigila")]
        [SerializeField] private Level3VillageHouse house;

        [Header("Ruido")]
        [SerializeField] private float decayPerSecond = 0.22f;
        [SerializeField] private float humanNoisePerSecond = 1.6f;
        [SerializeField] private float transformToHumanNoise = 0.7f;
        [SerializeField] private float fastWalkNoisePerSecond = 0.55f;
        [SerializeField] private float jumpNoise = 0.38f;
        [Tooltip("Fracción de la velocidad máxima a partir de la cual el paso cuenta como 'caminar rápido'.")]
        [SerializeField] private float fastWalkSpeedFraction = 0.55f;
        [SerializeField] private float backToSleepSeconds = 4f;

        [Header("Visuales")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private TextMeshPro zzzText;
        [SerializeField] private Transform noiseBarFill;
        [SerializeField] private SpriteRenderer noiseBarFillRenderer;
        [Tooltip("Raíz de la barra de ruido (fondo + relleno + etiqueta); se oculta mientras no hace falta.")]
        [SerializeField] private GameObject noiseBarRoot;

        private float noise;
        private bool awake;
        private float awakeTimer;
        private bool wasGrounded = true;
        private PlayerController player;
        private Rigidbody2D playerRb;
        private TransformationManager transformation;
        private Vector3 zzzBasePos;

        public float Noise01 => noise;
        public bool IsAwake => awake;
        public Level3VillageHouse House => house;

        public void Configure(Level3VillageHouse watchedHouse, SpriteRenderer body, TextMeshPro zzz, Transform barFill, SpriteRenderer barFillRenderer, GameObject barRoot = null)
        {
            house = watchedHouse;
            bodyRenderer = body;
            zzzText = zzz;
            noiseBarFill = barFill;
            noiseBarFillRenderer = barFillRenderer;
            noiseBarRoot = barRoot;
        }

        /// <summary>Duerme de nuevo a todos los aldeanos con el medidor a cero (reintento desde checkpoint).</summary>
        public static void ResetAllToSleep()
        {
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i] != null) All[i].FallAsleep();
            }
        }

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
            if (transformation != null) transformation.OnTransformed -= HandleTransformed;
        }

        private void Start()
        {
            if (zzzText != null) zzzBasePos = zzzText.transform.localPosition;
            BindPlayer();
            FallAsleep();
        }

        private void BindPlayer()
        {
            if (player != null) return;
            player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null) return;
            playerRb = player.GetComponent<Rigidbody2D>();
            transformation = player.GetComponent<TransformationManager>();
            if (transformation != null)
            {
                transformation.OnTransformed -= HandleTransformed;
                transformation.OnTransformed += HandleTransformed;
            }
        }

        private void HandleTransformed(bool isCat)
        {
            // Volver a forma humana dentro de la casa hace muchísimo ruido
            if (!isCat && !awake && house != null && house.PlayerInside)
            {
                AddNoise(transformToHumanNoise);
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            BindPlayer();

            if (awake)
            {
                awakeTimer -= Time.deltaTime;
                if (awakeTimer <= 0f) FallAsleep();
                return;
            }

            float dt = Time.deltaTime;
            bool inside = house != null && house.PlayerInside && player != null;
            if (inside)
            {
                bool isCat = transformation != null && transformation.IsCatForm;
                if (!isCat)
                {
                    AddNoise(humanNoisePerSecond * dt);
                }
                else if (playerRb != null)
                {
                    float maxSpeed = Mathf.Max(0.1f, player.MaxMoveSpeed);
                    float fraction = Mathf.Abs(playerRb.linearVelocity.x) / maxSpeed;
                    if (player.IsGrounded && !player.IsSneaking && fraction > fastWalkSpeedFraction)
                    {
                        AddNoise(fastWalkNoisePerSecond * dt);
                    }
                }

                // Salto brusco: despegar del suelo con impulso hacia arriba
                if (playerRb != null)
                {
                    bool grounded = player.IsGrounded;
                    if (wasGrounded && !grounded && playerRb.linearVelocity.y > 2.5f)
                    {
                        AddNoise(jumpNoise);
                    }
                    wasGrounded = grounded;
                }
            }
            else
            {
                wasGrounded = true;
            }

            noise = Mathf.Max(0f, noise - decayPerSecond * dt);
            UpdateVisuals();
        }

        private void AddNoise(float amount)
        {
            if (awake) return;
            noise = Mathf.Clamp01(noise + amount);
            if (noise >= 1f) WakeUp();
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (zzzText != null)
            {
                zzzText.gameObject.SetActive(!awake);
                float bob = Mathf.Sin(Time.time * 2.2f) * 0.08f;
                zzzText.transform.localPosition = zzzBasePos + new Vector3(0f, bob, 0f);
                zzzText.color = Color.Lerp(new Color(0.85f, 0.9f, 1f, 0.95f), new Color(1f, 0.25f, 0.2f, 1f), noise);
                zzzText.fontSize = Mathf.Lerp(3.2f, 4.4f, noise);
            }

            if (noiseBarFill != null)
            {
                noiseBarFill.localScale = new Vector3(Mathf.Max(0.001f, noise), 1f, 1f);
            }

            if (noiseBarFillRenderer != null)
            {
                noiseBarFillRenderer.color = Color.Lerp(new Color(0.45f, 0.85f, 0.55f, 0.95f), new Color(1f, 0.25f, 0.2f, 1f), noise);
            }

            GameObject bar = noiseBarRoot != null
                ? noiseBarRoot
                : (noiseBarFillRenderer != null ? noiseBarFillRenderer.transform.parent.gameObject : null);
            if (bar != null)
            {
                bar.SetActive(!awake && (noise > 0.02f || (house != null && house.PlayerInside)));
            }
        }

        private void WakeUp()
        {
            if (awake) return;
            awake = true;
            awakeTimer = backToSleepSeconds;

            if (bodyRenderer != null)
            {
                // Se incorpora en la cama, alarmado
                bodyRenderer.transform.localRotation = Quaternion.identity;
                bodyRenderer.color = new Color(1f, 0.82f, 0.78f, 1f);
            }
            if (zzzText != null)
            {
                zzzText.gameObject.SetActive(true);
                zzzText.text = "¡BRUJA!";
                zzzText.color = new Color(1f, 0.3f, 0.2f, 1f);
                zzzText.fontSize = 4.6f;
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayFrenzyAlarmSound();

            HUDController hud = Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage("¡El aldeano despertó y grita \"¡BRUJA!\"! Dentro de las casas muévete como gato con [Shift] y sin saltar.", 3.6f);
            }

            bool human = transformation != null && !transformation.IsCatForm;
            if (human && FrenzyController.Instance != null && player != null)
            {
                FrenzyController.Instance.TriggerFrenzy(player.transform);
            }

            if (GameManager.Instance != null && GameManager.Instance.LoseHeart(1, "Aldeano despierto"))
            {
                if (GameManager.Instance.CurrentHearts > 0 && CheckpointManager.Instance != null)
                {
                    CheckpointManager.Instance.RespawnAtCheckpoint();
                }
            }
        }

        private void FallAsleep()
        {
            awake = false;
            noise = 0f;
            if (bodyRenderer != null)
            {
                bodyRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                bodyRenderer.color = Color.white;
            }
            if (zzzText != null)
            {
                zzzText.text = "Z z z";
            }
            UpdateVisuals();
        }
    }
}
