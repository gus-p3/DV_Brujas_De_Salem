using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Level2
{
    /// <summary>
    /// Llave recolectable oculta en los interiores o sótanos de las casas de Salem.
    /// Incluye animación continua de levitación, rotación suave, halo dorado brillante,
    /// luz puntual propia (Light2D) y un indicador flotante en el mundo ("LLAVE ▼")
    /// para que sea claramente visible en sótanos y estancias oscuras.
    /// Al recogerla con Q (en forma humana), dispara la animación de recompensa (LootPopupVisualizer)
    /// y el sonido de tintineo de llave.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class KeyItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private string keyId = "key_default";
        [SerializeField] private string keyDisplayName = "Llave Oxidada";
        [SerializeField] private int scoreValue = 125;

        [Header("Animación Visual de Alta Visibilidad")]
        [SerializeField] private float bobAmplitude = 0.14f;
        [SerializeField] private float bobFrequency = 3.2f;
        [SerializeField] private float pulseScaleAmount = 0.14f;
        [SerializeField] private float tiltAngle = 14f;

        private Vector3 baseLocalPos;
        private Vector3 baseLocalScale;
        private Transform haloTransform;
        private Transform beaconTransform;
        private SpriteRenderer keyRenderer;

        public string InteractionId => $"pickup_{keyId}";
        public int Priority => 30;
        public bool UsesInteractKey => true;
        public Transform Transform => transform;
        public float PromptHeightOffset => 1.25f;

        public string KeyId => keyId;
        public string KeyDisplayName => keyDisplayName;

        private void Awake()
        {
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
                // Ampliar ligeramente el trigger para que sea cómodo de recoger
                if (col.size.x < 1.1f || col.size.y < 1.1f)
                {
                    col.size = new Vector2(Mathf.Max(col.size.x, 1.25f), Mathf.Max(col.size.y, 1.25f));
                }
            }

            keyRenderer = GetComponent<SpriteRenderer>();
            if (keyRenderer != null)
            {
                keyRenderer.sortingLayerName = "Foreground";
                keyRenderer.sortingOrder = Mathf.Max(keyRenderer.sortingOrder, 18);
            }

            // Garantizar escala mínima visible para que la llave nunca se vea diminuta
            if (transform.localScale.x < 0.72f)
            {
                transform.localScale = new Vector3(0.78f, 0.78f, 1f);
            }

            baseLocalPos = transform.localPosition;
            baseLocalScale = transform.localScale;
        }

        private void Start()
        {
            if (KeyInventory.Instance != null && KeyInventory.Instance.HasKey(keyId))
            {
                gameObject.SetActive(false);
                return;
            }

            baseLocalPos = transform.localPosition;
            baseLocalScale = transform.localScale;

            if (Application.isPlaying)
            {
                SetupHighVisibilityVisuals();
            }
        }

        private void SetupHighVisibilityVisuals()
        {
            // 1. Brillo estelar continuo (PickupGlint)
            PickupGlint glint = GetComponent<PickupGlint>();
            if (glint == null) glint = gameObject.AddComponent<PickupGlint>();
            glint.SetColor(new Color(1f, 0.88f, 0.22f, 1f));

            // 2. Halo dorado giratorio detrás de la llave
            if (transform.Find("KeyGoldenHalo") == null)
            {
                GameObject haloGo = new GameObject("KeyGoldenHalo");
                haloGo.transform.SetParent(transform, false);
                haloGo.transform.localPosition = Vector3.zero;
                haloGo.transform.localScale = Vector3.one * 1.65f;

                SpriteRenderer hSr = haloGo.AddComponent<SpriteRenderer>();
                hSr.sprite = LootPopupVisualizer.GetHaloSprite();
                hSr.color = new Color(1f, 0.85f, 0.18f, 0.78f);
                hSr.sortingLayerName = "Foreground";
                hSr.sortingOrder = (keyRenderer != null ? keyRenderer.sortingOrder : 18) - 1;
                haloTransform = haloGo.transform;

                // Segunda estrella giratoria concéntrica
                GameObject starGo = new GameObject("KeyStarFlare");
                starGo.transform.SetParent(haloGo.transform, false);
                starGo.transform.localScale = Vector3.one * 0.9f;
                SpriteRenderer sSr = starGo.AddComponent<SpriteRenderer>();
                sSr.sprite = LootPopupVisualizer.GetStarSprite();
                sSr.color = new Color(1f, 0.98f, 0.75f, 0.85f);
                sSr.sortingLayerName = "Foreground";
                sSr.sortingOrder = (keyRenderer != null ? keyRenderer.sortingOrder : 18) - 1;
            }

            // 3. Luz puntual cálida 2D para que la llave ilumine el sótano alrededor
            if (GetComponentInChildren<Light2D>() == null)
            {
                GameObject lightGo = new GameObject("KeySpotLight2D");
                lightGo.transform.SetParent(transform, false);
                lightGo.transform.localPosition = Vector3.zero;
                Light2D l2d = lightGo.AddComponent<Light2D>();
                l2d.lightType = Light2D.LightType.Point;
                l2d.color = new Color(1f, 0.86f, 0.32f, 1f);
                l2d.intensity = 1.65f;
                l2d.pointLightInnerRadius = 0.35f;
                l2d.pointLightOuterRadius = 3.2f;
                l2d.falloffIntensity = 0.5f;
            }

            // 4. Indicador flotante sobre la llave ("LLAVE ▼") para identificarla al instante
            if (transform.Find("KeyBeaconCanvas") == null)
            {
                GameObject canvasGo = new GameObject("KeyBeaconCanvas", typeof(RectTransform));
                canvasGo.transform.SetParent(transform, false);
                canvasGo.transform.localPosition = new Vector3(0f, 0.82f, 0f);
                canvasGo.transform.localScale = Vector3.one * 0.013f;

                Canvas c = canvasGo.AddComponent<Canvas>();
                c.renderMode = RenderMode.WorldSpace;
                c.sortingLayerName = "Foreground";
                c.sortingOrder = 45;

                RectTransform crt = (RectTransform)canvasGo.transform;
                crt.sizeDelta = new Vector2(180f, 46f);

                GameObject bgGo = new GameObject("Bg", typeof(RectTransform));
                bgGo.transform.SetParent(canvasGo.transform, false);
                RectTransform bgRt = (RectTransform)bgGo.transform;
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.sizeDelta = Vector2.zero;
                Image bgImg = bgGo.AddComponent<Image>();
                bgImg.color = new Color(0.14f, 0.09f, 0.02f, 0.90f);
                bgImg.raycastTarget = false;

                Outline ol = bgGo.AddComponent<Outline>();
                ol.effectColor = new Color(1f, 0.84f, 0.18f, 1f);
                ol.effectDistance = new Vector2(2f, -2f);

                GameObject txtGo = new GameObject("Text", typeof(RectTransform));
                txtGo.transform.SetParent(canvasGo.transform, false);
                RectTransform txtRt = (RectTransform)txtGo.transform;
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.sizeDelta = Vector2.zero;

                TextMeshProUGUI tmp = txtGo.AddComponent<TextMeshProUGUI>();
                tmp.text = "LLAVE ▼";
                tmp.fontSize = 22f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = new Color(1f, 0.92f, 0.35f, 1f);
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;

                beaconTransform = canvasGo.transform;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            float t = Time.time;

            // Levitación suave + pulso de escala + balanceo de llave
            float bobOffset = Mathf.Sin(t * bobFrequency) * bobAmplitude;
            transform.localPosition = baseLocalPos + new Vector3(0f, bobOffset, 0f);

            float scalePulse = 1f + Mathf.Sin(t * (bobFrequency * 1.4f)) * pulseScaleAmount;
            transform.localScale = baseLocalScale * scalePulse;

            float zRot = Mathf.Sin(t * 2.4f) * tiltAngle;
            transform.localRotation = Quaternion.Euler(0f, 0f, zRot);

            if (haloTransform != null)
            {
                haloTransform.localRotation = Quaternion.Euler(0f, 0f, t * 75f);
            }

            if (beaconTransform != null)
            {
                // Mantener la etiqueta horizontal aunque la llave se balancee
                beaconTransform.rotation = Quaternion.identity;
                beaconTransform.localPosition = new Vector3(0f, 0.82f + Mathf.Sin(t * 5f) * 0.08f, 0f);
            }
        }

        public void Configure(string id, string displayName, int score = 125)
        {
            keyId = id;
            keyDisplayName = displayName;
            scoreValue = score;
        }

        public bool CanInteract(GameObject interactor)
        {
            if (interactor == null) return false;
            return !KeyInventory.Instance.HasKey(keyId);
        }

        public string GetPromptText(GameObject interactor)
        {
            return $"Q: Recoger {keyDisplayName}";
        }

        public void Interact(GameObject interactor)
        {
            if (interactor != null)
            {
                TransformationManager tm = interactor.GetComponent<TransformationManager>();
                if (tm != null && tm.IsCatForm)
                {
                    Level2CatPickupGuard.NotifyCatBlocked(forceShow: true);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayErrorSound();
                    return;
                }
            }

            if (KeyInventory.Instance != null && KeyInventory.Instance.AddKey(keyId, keyDisplayName))
            {
                if (GameManager.Instance != null && scoreValue > 0)
                {
                    GameManager.Instance.AddScore(scoreValue);
                }

                // Animación visual emergente de recompensa (icono de llave + etiqueta + tarjeta HUD)
                Sprite keySpr = keyRenderer != null ? keyRenderer.sprite : null;
                LootPopupVisualizer.ShowKeyReward(transform.position, keyDisplayName, keySpr);

                HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
                if (hud != null)
                {
                    hud.ShowMessage($"Obtenido: {keyDisplayName}", 2.6f);
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayKeyPickupSound();
                }

                // Actualizar el indicador de llaves en el HUD del Nivel 2 si existe
                Level2ChestGuideHUD guide = UnityEngine.Object.FindFirstObjectByType<Level2ChestGuideHUD>();
                if (guide != null) guide.Refresh();

                if (Application.isPlaying) Destroy(gameObject);
                else gameObject.SetActive(false);
            }
        }
    }
}
