using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Level2
{
    /// <summary>
    /// Centinela de la Inquisición / Guardián de Cripta que protege llaves o cofres en el Nivel 2.
    /// Implementa <see cref="ISpellTarget"/> para exigir obligatoriamente el uso de una poción
    /// (Cegar [1], Hipnotizar [2] o Dormir [3] + lanzar con [E] o Clic) antes de poder pasar
    /// hacia la llave o el cofre.
    /// No añade componentes EnemySight ni VillagerAI adicionales para mantener intactos los
    /// conteos exactos de patrullas exteriores verificados por Level2StealthVerifier.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class ArcaneSentinelObstacle : MonoBehaviour, ISpellTarget
    {
        [Header("Configuración del Reto de Poción")]
        [SerializeField] private string sentinelName = "Centinela de la Cripta";
        [SerializeField] private float neutralizedDuration = 18f;
        [SerializeField] private BoxCollider2D solidBlockerCollider;
        [SerializeField] private BoxCollider2D warningTriggerCollider;

        [Header("Visuales y Animación")]
        [SerializeField] private SpriteRenderer characterRenderer;
        [SerializeField] private VillagerAnimationSet animationSet;
        [SerializeField] private SpriteRenderer statusIconRenderer;
        [SerializeField] private Sprite blindIcon;
        [SerializeField] private Sprite hypnotizeIcon;
        [SerializeField] private Sprite sleepIcon;

        private enum SentinelState
        {
            Guarding,
            Blinded,
            Hypnotized,
            Sleeping
        }

        private SentinelState state = SentinelState.Guarding;
        private float stateTimer = 0f;
        private float animTimer = 0f;
        private int frameIndex = 0;
        private float lastWarningTime = -10f;
        private Vector3 baseVisualLocalPos;
        private Vector3 baseVisualLocalScale = Vector3.one;
        private Transform bannerTransform;
        private TextMeshProUGUI bannerText;
        private Image bannerBg;
        private SpriteRenderer auraRenderer;

        public bool IsNeutralized => state != SentinelState.Guarding;
        public bool IsAffectedBySpell => IsNeutralized;

        private void Awake()
        {
            if (characterRenderer != null)
            {
                Transform vTr = characterRenderer.transform;
                vTr.localScale = new Vector3(Mathf.Abs(vTr.localScale.x), Mathf.Abs(vTr.localScale.y), 1f);
                baseVisualLocalPos = vTr.localPosition;
                baseVisualLocalScale = vTr.localScale;
            }
        }

        private void Start()
        {
            if (characterRenderer != null)
            {
                baseVisualLocalPos = characterRenderer.transform.localPosition;
                baseVisualLocalScale = characterRenderer.transform.localScale;
            }

            if (Application.isPlaying)
            {
                EnsureWorldBannerAndAura();
            }
            UpdateVisualState();
        }

        public void Configure(
            string displayTitle,
            BoxCollider2D blocker,
            BoxCollider2D trigger,
            SpriteRenderer charSr,
            VillagerAnimationSet animSet,
            SpriteRenderer statusSr,
            Sprite iconBlind,
            Sprite iconHypno,
            Sprite iconSleep)
        {
            sentinelName = displayTitle;
            solidBlockerCollider = blocker;
            warningTriggerCollider = trigger;
            characterRenderer = charSr;
            animationSet = animSet;
            statusIconRenderer = statusSr;
            blindIcon = iconBlind;
            hypnotizeIcon = iconHypno;
            sleepIcon = iconSleep;
        }

        private void EnsureWorldBannerAndAura()
        {
            if (transform.Find("SentinelAura") == null)
            {
                GameObject auraGo = new GameObject("SentinelAura");
                auraGo.transform.SetParent(transform, false);
                auraGo.transform.localPosition = new Vector3(0f, 0.85f, 0f);
                auraGo.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
                auraRenderer = auraGo.AddComponent<SpriteRenderer>();
                auraRenderer.sprite = LootPopupVisualizer.GetHaloSprite();
                auraRenderer.color = new Color(0.95f, 0.22f, 0.18f, 0.38f);
                auraRenderer.sortingLayerName = "Characters";
                auraRenderer.sortingOrder = 4;
            }

            Transform existingBanner = transform.Find("SentinelBannerCanvas");
            if (existingBanner == null)
            {
                GameObject canvasGo = new GameObject("SentinelBannerCanvas", typeof(RectTransform));
                canvasGo.transform.SetParent(transform, false);
                canvasGo.transform.localPosition = new Vector3(0f, 2.35f, 0f);
                canvasGo.transform.localScale = Vector3.one * 0.0115f;

                Canvas c = canvasGo.AddComponent<Canvas>();
                c.renderMode = RenderMode.WorldSpace;
                c.sortingLayerName = "Foreground";
                c.sortingOrder = 45;

                RectTransform crt = (RectTransform)canvasGo.transform;
                crt.sizeDelta = new Vector2(440f, 48f);

                GameObject bgGo = new GameObject("Bg", typeof(RectTransform));
                bgGo.transform.SetParent(canvasGo.transform, false);
                RectTransform bgRt = (RectTransform)bgGo.transform;
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.sizeDelta = Vector2.zero;
                bannerBg = bgGo.AddComponent<Image>();
                bannerBg.color = new Color(0.18f, 0.04f, 0.06f, 0.90f);
                bannerBg.raycastTarget = false;

                Outline ol = bgGo.AddComponent<Outline>();
                ol.effectColor = new Color(1f, 0.35f, 0.25f, 1f);
                ol.effectDistance = new Vector2(2f, -2f);

                GameObject txtGo = new GameObject("Text", typeof(RectTransform));
                txtGo.transform.SetParent(canvasGo.transform, false);
                RectTransform txtRt = (RectTransform)txtGo.transform;
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.sizeDelta = Vector2.zero;

                bannerText = txtGo.AddComponent<TextMeshProUGUI>();
                bannerText.text = "GUARDIÁN: Usa Poción (1/2/3 + E)";
                bannerText.fontSize = 19.5f;
                bannerText.fontStyle = FontStyles.Bold;
                bannerText.color = new Color(1f, 0.88f, 0.72f, 1f);
                bannerText.alignment = TextAlignmentOptions.Center;
                bannerText.textWrappingMode = TextWrappingModes.NoWrap;
                bannerText.raycastTarget = false;

                bannerTransform = canvasGo.transform;
            }
            else
            {
                bannerTransform = existingBanner;
                RectTransform crt = bannerTransform as RectTransform;
                if (crt != null) crt.sizeDelta = new Vector2(440f, 48f);
                bannerBg = existingBanner.GetComponentInChildren<Image>(true);
                bannerText = existingBanner.GetComponentInChildren<TextMeshProUGUI>(true);
                if (bannerText != null) bannerText.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        private void Update()
        {
            if (state != SentinelState.Guarding)
            {
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    state = SentinelState.Guarding;
                    UpdateVisualState();
                }
            }

            AnimateSprite();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying) return;
            AnimateSprite();
            KeepBannerUpright();
        }

        private void KeepBannerUpright()
        {
            if (bannerTransform == null)
            {
                bannerTransform = transform.Find("SentinelBannerCanvas");
            }
            if (bannerTransform != null)
            {
                float parentSignX = transform.lossyScale.x < 0f ? -1f : 1f;
                bannerTransform.localScale = new Vector3(0.0115f * parentSignX, 0.0115f, 0.0115f);
                bannerTransform.rotation = Quaternion.identity;
            }
        }

        private void AnimateSprite()
        {
            if (characterRenderer == null || animationSet == null) return;

            Sprite[] idleFrames = animationSet.idle != null ? animationSet.idle.frames : null;
            Sprite[] walkFrames = (animationSet.walk != null && !animationSet.walk.IsEmpty) ? animationSet.walk.frames : idleFrames;
            Sprite[] blindFrames = (animationSet.blind != null && !animationSet.blind.IsEmpty) ? animationSet.blind.frames : idleFrames;
            Sprite[] hypnoFrames = (animationSet.hypnotize != null && !animationSet.hypnotize.IsEmpty) ? animationSet.hypnotize.frames : blindFrames;
            Sprite[] sleepFrames = (animationSet.sleep != null && !animationSet.sleep.IsEmpty) ? animationSet.sleep.frames : idleFrames;

            Sprite[] clip = state switch
            {
                SentinelState.Blinded => blindFrames,
                SentinelState.Hypnotized => hypnoFrames,
                SentinelState.Sleeping => sleepFrames,
                _ => walkFrames
            };

            float fps = state == SentinelState.Guarding ? 11f : 7.5f;
            if (clip != null && clip.Length > 0)
            {
                animTimer += Time.deltaTime;
                if (animTimer >= 1f / fps)
                {
                    animTimer = 0f;
                    frameIndex = (frameIndex + 1) % clip.Length;
                }
                characterRenderer.sprite = clip[frameIndex % clip.Length];
            }

            if (!Application.isPlaying) return;

            float t = Time.time;
            Transform vTr = characterRenderer.transform;
            characterRenderer.flipX = true;

            if (state == SentinelState.Guarding)
            {
                // Guardia en postura intimidante vigilando el corredor (sin invertir localScale.x para no espejar el Canvas hijo)
                float patrolStep = Mathf.Sin(t * 2.4f) * 0.16f;
                float bounce = Mathf.Abs(Mathf.Cos(t * 4.8f)) * 0.045f;
                vTr.localPosition = baseVisualLocalPos + new Vector3(patrolStep, bounce, 0f);
                float sx = Mathf.Abs(baseVisualLocalScale.x) * (1f - bounce * 0.4f);
                float sy = Mathf.Abs(baseVisualLocalScale.y) * (1f + bounce * 0.6f);
                vTr.localScale = new Vector3(sx, sy, 1f);
                vTr.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 4.8f) * 2.5f);
            }
            else if (state == SentinelState.Sleeping)
            {
                float breath = Mathf.Sin(t * 2.0f) * 0.045f;
                vTr.localPosition = baseVisualLocalPos;
                vTr.localScale = new Vector3(Mathf.Abs(baseVisualLocalScale.x) * (1f - breath), Mathf.Abs(baseVisualLocalScale.y) * (0.92f + breath), 1f);
                vTr.localRotation = Quaternion.Euler(0f, 0f, 6f);
            }
            else
            {
                // Mareado o hipnotizado
                float sway = Mathf.Sin(t * 4.2f) * 7.5f;
                vTr.localPosition = baseVisualLocalPos + new Vector3(Mathf.Sin(t * 2.1f) * 0.04f, 0f, 0f);
                vTr.localScale = new Vector3(Mathf.Abs(baseVisualLocalScale.x), Mathf.Abs(baseVisualLocalScale.y), 1f);
                vTr.localRotation = Quaternion.Euler(0f, 0f, sway);
            }

            if (bannerTransform != null)
            {
                float signX = transform.lossyScale.x < 0f ? -1f : 1f;
                bannerTransform.localScale = new Vector3(0.0115f * signX, 0.0115f, 0.0115f);
                bannerTransform.rotation = Quaternion.identity;
            }

            if (statusIconRenderer != null && statusIconRenderer.enabled)
            {
                statusIconRenderer.transform.localPosition = new Vector3(0f, 1.95f + Mathf.Sin(t * 4.5f) * 0.08f, 0f);
                if (state == SentinelState.Hypnotized)
                {
                    statusIconRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, -t * 150f);
                }
                else
                {
                    statusIconRenderer.transform.localRotation = Quaternion.identity;
                }
            }

            if (bannerTransform != null && state != SentinelState.Guarding)
            {
                bannerText.text = $"PASO LIBRE ({Mathf.CeilToInt(stateTimer)}s)";
            }
        }

        // --- Implementación de ISpellTarget ---
        public void ApplyBlind(float duration)
        {
            Neutralize(SentinelState.Blinded, Mathf.Max(duration, neutralizedDuration), "cegado");
        }

        public void ApplyHypnotize(float duration, int direction)
        {
            Neutralize(SentinelState.Hypnotized, Mathf.Max(duration, neutralizedDuration), "hipnotizado");
        }

        public void ApplySleep(float duration)
        {
            Neutralize(SentinelState.Sleeping, Mathf.Max(duration, neutralizedDuration), "dormido");
        }

        private void Neutralize(SentinelState newState, float duration, string verb)
        {
            state = newState;
            stateTimer = duration;
            frameIndex = 0;
            UpdateVisualState();

            HUDController hud = Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage($"¡{sentinelName} {verb} con tu poción! El paso hacia la llave está libre.", 3.0f);
            }
        }

        private void UpdateVisualState()
        {
            bool guarding = state == SentinelState.Guarding;
            if (solidBlockerCollider != null)
            {
                solidBlockerCollider.enabled = guarding;
            }

            if (statusIconRenderer != null)
            {
                statusIconRenderer.enabled = !guarding;
                statusIconRenderer.sprite = state switch
                {
                    SentinelState.Blinded => blindIcon,
                    SentinelState.Hypnotized => hypnotizeIcon,
                    SentinelState.Sleeping => sleepIcon,
                    _ => null
                };
            }

            if (auraRenderer != null)
            {
                auraRenderer.color = guarding
                    ? new Color(0.95f, 0.22f, 0.18f, 0.42f)
                    : new Color(0.32f, 0.92f, 0.55f, 0.35f);
            }

            if (bannerBg != null && bannerText != null)
            {
                if (guarding)
                {
                    bannerBg.color = new Color(0.18f, 0.04f, 0.06f, 0.90f);
                    bannerText.color = new Color(1f, 0.88f, 0.72f, 1f);
                    bannerText.text = "GUARDIÁN: Usa Poción (1/2/3 + E)";
                }
                else
                {
                    bannerBg.color = new Color(0.05f, 0.18f, 0.09f, 0.90f);
                    bannerText.color = new Color(0.55f, 1f, 0.68f, 1f);
                    bannerText.text = "PASO LIBRE";
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CheckWarning(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            CheckWarning(other);
        }

        private void CheckWarning(Collider2D other)
        {
            if (state != SentinelState.Guarding) return;
            PlayerController pc = other != null ? other.GetComponentInParent<PlayerController>() : null;
            if (pc == null) return;

            if (Time.unscaledTime - lastWarningTime < 2.6f) return;
            lastWarningTime = Time.unscaledTime;

            HUDController hud = Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage($"¡{sentinelName} bloquea el paso! Arroja una Poción (teclas 1/2/3 y pulsa E) para neutralizarlo.", 2.8f);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayGuardAlertSound();
            }
        }
    }
}
