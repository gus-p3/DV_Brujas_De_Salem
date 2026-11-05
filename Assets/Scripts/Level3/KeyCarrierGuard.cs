using System.Collections;
using Level2;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Level3
{
    /// <summary>
    /// Componente que se añade a un Guardia (PatrolRoute) o Aldeano (VillagerAI) para convertirlo
    /// en un Portador de Llave con Brillo Especial claramente distinguible a simple vista.
    /// Al caer dormido (Sleep), apaga su aura dorada y suelta la llave física (KeyItem) con un salto
    /// parabólico o la entrega directamente a KeyInventory.
    /// </summary>
    public class KeyCarrierGuard : MonoBehaviour
    {
        [Header("Configuración de la Llave Portada")]
        [SerializeField] private string keyId = "forest_gate_key_1";
        [SerializeField] private string keyDisplayName = "Llave del Portón del Bosque I";
        [SerializeField] private Color auraColor = new Color(1f, 0.85f, 0.2f, 0.9f);
        [SerializeField] private bool autoCollectOnSleep = false;
        [SerializeField] private int keyScoreValue = 150;

        [Header("Ajustes del Salto de Llave y Recolección")]
        [SerializeField] private float dropHorizontalOffset = 0.85f;
        [SerializeField] private float dropArcHeight = 0.95f;
        [SerializeField] private float dropDuration = 0.42f;
        [SerializeField] private float immediatePickupDistance = 2.5f;

        private PatrolRoute patrolRoute;
        private VillagerAI villagerAI;
        private bool hasDroppedKey;

        // Referencias a los elementos visuales generados programáticamente
        private GameObject auraRootGo;
        private SpriteRenderer auraCircleRenderer;
        private SpriteRenderer outerRingRenderer;
        private Transform orbitContainer;
        private readonly SpriteRenderer[] orbitSparkRenderers = new SpriteRenderer[4];
        private Transform floatingKeyBadge;
        private SpriteRenderer floatingKeyRenderer;
        private Light2D carrierLight2D;

        private static Sprite cachedSoftCircleSprite;
        private static Sprite cachedRingSprite;
        private static Sprite cachedProceduralKeySprite;

        public string KeyId => keyId;
        public string KeyDisplayName => keyDisplayName;
        public Color AuraColor => auraColor;
        public bool AutoCollectOnSleep => autoCollectOnSleep;
        public bool HasDroppedKey => hasDroppedKey;

        /// <summary>True si el aura dorada (halo + luz 2D) está construida y visible.</summary>
        public bool HasActiveAura =>
            auraRootGo != null && auraRootGo.activeInHierarchy &&
            auraCircleRenderer != null && auraCircleRenderer.enabled && auraCircleRenderer.sprite != null &&
            carrierLight2D != null && carrierLight2D.enabled;

        /// <summary>
        /// Garantiza que los visuales del aura existan (usado por herramientas de auditoría en Editor).
        /// </summary>
        public void EnsureAuraVisuals()
        {
            BuildSpecialGlowVisuals();
            ApplyAuraColorToVisuals();
        }

        /// <summary>
        /// Configura programáticamente el portador de llave.
        /// </summary>
        public void Configure(string id, string displayName, Color? customAuraColor = null, bool autoCollect = false)
        {
            keyId = id;
            keyDisplayName = displayName;
            if (customAuraColor.HasValue)
            {
                auraColor = customAuraColor.Value;
            }
            autoCollectOnSleep = autoCollect;

            ApplyAuraColorToVisuals();
        }

        private void Awake()
        {
            patrolRoute = GetComponent<PatrolRoute>();
            villagerAI = GetComponent<VillagerAI>();

            BuildSpecialGlowVisuals();
        }

        private void Start()
        {
            if (patrolRoute == null) patrolRoute = GetComponent<PatrolRoute>();
            if (villagerAI == null) villagerAI = GetComponent<VillagerAI>();

            if (KeyInventory.Instance != null && KeyInventory.Instance.HasKey(keyId))
            {
                hasDroppedKey = true;
                SetSpecialGlowActive(false);
            }
        }

        private void Update()
        {
            if (!hasDroppedKey)
            {
                AnimateSpecialGlow();

                if (IsTargetAsleep())
                {
                    DropOrGrantKeyOnSleep();
                }
            }
        }

        private bool IsTargetAsleep()
        {
            if (patrolRoute != null && patrolRoute.ActiveSpell == PatrolRoute.SpellEffectType.Sleep)
            {
                return true;
            }

            if (villagerAI != null && villagerAI.ActiveSpell == VillagerAI.VillagerSpellType.Sleep)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Construye el hijo "KeyCarrierAura" con SpriteRenderer circular suave en Gameplay order 3,
        /// anillo de destellos orbitales, PickupGlint y el icono de llave flotante junto al guardia.
        /// </summary>
        private void BuildSpecialGlowVisuals()
        {
            Transform existing = transform.Find("KeyCarrierAura");
            if (existing != null)
            {
                auraRootGo = existing.gameObject;
                auraCircleRenderer = auraRootGo.GetComponent<SpriteRenderer>();

                // Re-cachear referencias de los hijos visuales si el aura ya existía
                Transform ring = existing.Find("KeyCarrierHaloRing");
                if (ring != null) outerRingRenderer = ring.GetComponent<SpriteRenderer>();
                orbitContainer = existing.Find("KeyCarrierOrbitSparks");
                if (orbitContainer != null)
                {
                    for (int i = 0; i < orbitSparkRenderers.Length && i < orbitContainer.childCount; i++)
                    {
                        orbitSparkRenderers[i] = orbitContainer.GetChild(i).GetComponent<SpriteRenderer>();
                    }
                }
                floatingKeyBadge = existing.Find("FloatingKeyBadge");
                if (floatingKeyBadge != null) floatingKeyRenderer = floatingKeyBadge.GetComponent<SpriteRenderer>();
                Transform lightTr = existing.Find("KeyCarrierLight2D");
                if (lightTr != null) carrierLight2D = lightTr.GetComponent<Light2D>();
                return;
            }

            // 1. Nodo raíz "KeyCarrierAura" con SpriteRenderer procedural circular suave
            auraRootGo = new GameObject("KeyCarrierAura");
            auraRootGo.transform.SetParent(transform, false);
            auraRootGo.transform.localPosition = new Vector3(0f, 0.92f, 0f);
            auraRootGo.transform.localScale = new Vector3(2.4f, 2.6f, 1f);

            auraCircleRenderer = auraRootGo.AddComponent<SpriteRenderer>();
            auraCircleRenderer.sprite = GetOrCreateSoftCircleSprite();
            auraCircleRenderer.color = auraColor;
            auraCircleRenderer.sortingLayerName = "Gameplay";
            auraCircleRenderer.sortingOrder = 3;

            // Añadir PickupGlint al aura para destellos estelares periódicos
            PickupGlint glint = auraRootGo.AddComponent<PickupGlint>();
            glint.SetColor(auraColor);

            // 2. Halo anular exterior sutil
            GameObject ringGo = new GameObject("KeyCarrierHaloRing");
            ringGo.transform.SetParent(auraRootGo.transform, false);
            ringGo.transform.localPosition = Vector3.zero;
            ringGo.transform.localScale = Vector3.one * 0.88f;

            outerRingRenderer = ringGo.AddComponent<SpriteRenderer>();
            outerRingRenderer.sprite = GetOrCreateRingSprite();
            outerRingRenderer.color = new Color(1f, 0.95f, 0.55f, 0.75f);
            outerRingRenderer.sortingLayerName = "Gameplay";
            outerRingRenderer.sortingOrder = 3;

            // 3. Anillo de partículas / destellos orbitales (4 estrellas girando alrededor del guardia)
            GameObject orbitGo = new GameObject("KeyCarrierOrbitSparks");
            orbitGo.transform.SetParent(auraRootGo.transform, false);
            orbitGo.transform.localPosition = Vector3.zero;
            orbitContainer = orbitGo.transform;

            Sprite starSprite = LootPopupVisualizer.GetStarSprite();
            for (int i = 0; i < orbitSparkRenderers.Length; i++)
            {
                GameObject sparkGo = new GameObject($"OrbitSpark_{i}");
                sparkGo.transform.SetParent(orbitContainer, false);
                float angleRad = i * Mathf.PI * 0.5f;
                sparkGo.transform.localPosition = new Vector3(Mathf.Cos(angleRad) * 0.42f, Mathf.Sin(angleRad) * 0.42f, 0f);
                sparkGo.transform.localScale = Vector3.one * 0.24f;

                SpriteRenderer sSr = sparkGo.AddComponent<SpriteRenderer>();
                sSr.sprite = starSprite;
                sSr.color = new Color(1f, 0.96f, 0.68f, 0.92f);
                sSr.sortingLayerName = "Gameplay";
                sSr.sortingOrder = 5;
                orbitSparkRenderers[i] = sSr;
            }

            // 4. Pequeño icono de llave dorada flotando junto a la cintura/cabeza del guardia
            GameObject badgeGo = new GameObject("FloatingKeyBadge");
            badgeGo.transform.SetParent(auraRootGo.transform, false);
            badgeGo.transform.localPosition = new Vector3(0.32f, 0.42f, 0f);
            badgeGo.transform.localScale = Vector3.one * 0.34f;
            floatingKeyBadge = badgeGo.transform;

            floatingKeyRenderer = badgeGo.AddComponent<SpriteRenderer>();
            floatingKeyRenderer.sprite = GetOrCreateKeyBadgeSprite();
            floatingKeyRenderer.color = new Color(1f, 0.92f, 0.28f, 1f);
            floatingKeyRenderer.sortingLayerName = "Gameplay";
            floatingKeyRenderer.sortingOrder = 14;

            // Sombra/borde para que el icono de llave resalte sobre cualquier fondo
            GameObject badgeRimGo = new GameObject("BadgeRim");
            badgeRimGo.transform.SetParent(badgeGo.transform, false);
            badgeRimGo.transform.localPosition = Vector3.zero;
            badgeRimGo.transform.localScale = Vector3.one * 1.18f;
            SpriteRenderer rimSr = badgeRimGo.AddComponent<SpriteRenderer>();
            rimSr.sprite = floatingKeyRenderer.sprite;
            rimSr.color = new Color(0.08f, 0.05f, 0.01f, 0.85f);
            rimSr.sortingLayerName = "Gameplay";
            rimSr.sortingOrder = 13;

            // 5. Luz puntual 2D cálida para destacar al guardia portador en zonas oscuras del bosque
            GameObject lightGo = new GameObject("KeyCarrierLight2D");
            lightGo.transform.SetParent(auraRootGo.transform, false);
            lightGo.transform.localPosition = Vector3.zero;
            carrierLight2D = lightGo.AddComponent<Light2D>();
            carrierLight2D.lightType = Light2D.LightType.Point;
            carrierLight2D.color = new Color(auraColor.r, auraColor.g, auraColor.b, 1f);
            carrierLight2D.intensity = 1.35f;
            carrierLight2D.pointLightInnerRadius = 0.4f;
            carrierLight2D.pointLightOuterRadius = 2.8f;
            carrierLight2D.falloffIntensity = 0.5f;
        }

        private void ApplyAuraColorToVisuals()
        {
            if (auraCircleRenderer != null)
            {
                auraCircleRenderer.color = auraColor;
            }

            if (carrierLight2D != null)
            {
                carrierLight2D.color = new Color(auraColor.r, auraColor.g, auraColor.b, 1f);
            }

            if (auraRootGo != null)
            {
                PickupGlint glint = auraRootGo.GetComponent<PickupGlint>();
                if (glint != null) glint.SetColor(auraColor);
            }
        }

        private void AnimateSpecialGlow()
        {
            if (auraRootGo == null || !auraRootGo.activeSelf) return;

            float t = Time.time;

            // Pulso suave de escala y opacidad del halo detrás del guardia
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 3.6f);
            float scaleX = Mathf.Lerp(2.15f, 2.65f, pulse);
            float scaleY = Mathf.Lerp(2.35f, 2.85f, pulse);
            auraRootGo.transform.localScale = new Vector3(scaleX, scaleY, 1f);

            if (auraCircleRenderer != null)
            {
                Color c = auraColor;
                c.a = Mathf.Lerp(0.45f, auraColor.a, pulse);
                auraCircleRenderer.color = c;
            }

            if (outerRingRenderer != null)
            {
                outerRingRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, -t * 48f);
                Color rc = outerRingRenderer.color;
                rc.a = Mathf.Lerp(0.35f, 0.82f, 1f - pulse);
                outerRingRenderer.color = rc;
            }

            if (orbitContainer != null)
            {
                orbitContainer.localRotation = Quaternion.Euler(0f, 0f, t * 85f);
                for (int i = 0; i < orbitSparkRenderers.Length; i++)
                {
                    if (orbitSparkRenderers[i] == null) continue;
                    float sparkPulse = 0.20f + 0.08f * Mathf.Sin(t * 6f + i * 1.5f);
                    orbitSparkRenderers[i].transform.localScale = Vector3.one * sparkPulse;
                    orbitSparkRenderers[i].transform.localRotation = Quaternion.Euler(0f, 0f, -t * 160f);
                }
            }

            if (floatingKeyBadge != null)
            {
                float bob = Mathf.Sin(t * 4.2f) * 0.07f;
                floatingKeyBadge.localPosition = new Vector3(0.32f, 0.42f + bob, 0f);
                floatingKeyBadge.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 3.0f) * 14f);
            }
        }

        private void SetSpecialGlowActive(bool active)
        {
            if (auraRootGo != null)
            {
                auraRootGo.SetActive(active);
            }
        }

        /// <summary>
        /// Se ejecuta exactamente una vez cuando el guardia cae dormido por una Poción de Sueño.
        /// </summary>
        private void DropOrGrantKeyOnSleep()
        {
            if (hasDroppedKey) return;
            hasDroppedKey = true;

            // Apagar el brillo dorado del guardia para indicar que ya soltó la llave
            SetSpecialGlowActive(false);

            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage($"¡El guardia cayó dormido y soltó: {keyDisplayName}!", 3.2f);
            }

            PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            TransformationManager tm = player != null ? player.GetComponent<TransformationManager>() : null;
            bool playerCloseEnough = player != null &&
                                     Vector2.Distance(transform.position, player.transform.position) <= immediatePickupDistance &&
                                     (tm == null || !tm.IsCatForm);

            if (autoCollectOnSleep || playerCloseEnough)
            {
                GrantKeyDirectly(transform.position + Vector3.up * 0.9f);
            }
            else
            {
                SpawnPhysicalDroppedKey();
            }
        }

        private void GrantKeyDirectly(Vector3 visualOrigin)
        {
            EnsureKeyInventoryExists();

            if (KeyInventory.Instance != null && KeyInventory.Instance.AddKey(keyId, keyDisplayName))
            {
                if (GameManager.Instance != null && keyScoreValue > 0)
                {
                    GameManager.Instance.AddScore(keyScoreValue);
                }

                Sprite keySpr = GetOrCreateKeyBadgeSprite();
                LootPopupVisualizer.ShowKeyReward(visualOrigin, keyDisplayName, keySpr);

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayKeyPickupSound();
                }
            }
        }

        private void SpawnPhysicalDroppedKey()
        {
            EnsureKeyInventoryExists();

            Vector3 startPos = transform.position + new Vector3(0f, 0.95f, 0f);
            PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            float dirToPlayer = 1f;
            if (player != null)
            {
                float diffX = player.transform.position.x - transform.position.x;
                if (Mathf.Abs(diffX) > 0.1f)
                {
                    dirToPlayer = Mathf.Sign(diffX);
                }
            }

            Vector3 targetPos = new Vector3(
                transform.position.x + dirToPlayer * dropHorizontalOffset,
                transform.position.y + 0.42f,
                transform.position.z);

            GameObject keyGo = new GameObject($"DroppedKey_{keyId}");
            keyGo.transform.position = targetPos;
            keyGo.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

            SpriteRenderer sr = keyGo.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateKeyBadgeSprite();
            sr.color = new Color(1f, 0.92f, 0.28f, 1f);
            sr.sortingLayerName = "Foreground";
            sr.sortingOrder = 20;

            BoxCollider2D col = keyGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.45f, 1.45f);

            KeyItem keyItem = keyGo.AddComponent<KeyItem>();
            keyItem.Configure(keyId, keyDisplayName, keyScoreValue);

            PickupGlint glint = keyGo.GetComponent<PickupGlint>();
            if (glint == null) glint = keyGo.AddComponent<PickupGlint>();
            glint.SetColor(auraColor);

            // Añadir colector por proximidad/tecla E además de la interacción Q nativa de KeyItem
            DroppedKeyQuickPickup quickPickup = keyGo.AddComponent<DroppedKeyQuickPickup>();
            quickPickup.Initialize(keyItem, startPos, targetPos, dropArcHeight, dropDuration);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayKeyPickupSound();
            }
        }

        private static void EnsureKeyInventoryExists()
        {
            if (KeyInventory.Instance == null)
            {
                GameObject invGo = new GameObject("KeyInventory_Runtime");
                invGo.AddComponent<KeyInventory>();
            }
        }

        // --- Generación Procedural de Sprites para el Brillo Especial ---

        private static Sprite GetOrCreateSoftCircleSprite()
        {
            if (cachedSoftCircleSprite != null) return cachedSoftCircleSprite;

            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float maxRadius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist01 = Vector2.Distance(new Vector2(x, y), center) / maxRadius;
                    float falloff = Mathf.Clamp01(1f - dist01);
                    float alpha = falloff * falloff * 0.85f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            cachedSoftCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
            return cachedSoftCircleSprite;
        }

        private static Sprite GetOrCreateRingSprite()
        {
            if (cachedRingSprite != null) return cachedRingSprite;

            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float maxRadius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float r = Vector2.Distance(new Vector2(x, y), center) / maxRadius;
                    float ring = Mathf.Exp(-Mathf.Pow((r - 0.78f) * 9.0f, 2f));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(ring)));
                }
            }

            tex.Apply();
            cachedRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
            return cachedRingSprite;
        }

        public static Sprite GetOrCreateKeyBadgeSprite()
        {
            if (cachedProceduralKeySprite != null) return cachedProceduralKeySprite;

            Sprite[] loaded = Resources.FindObjectsOfTypeAll<Sprite>();
            for (int i = 0; i < loaded.Length; i++)
            {
                if (loaded[i] != null && loaded[i].name.Contains("Interactive_Key"))
                {
                    cachedProceduralKeySprite = loaded[i];
                    return cachedProceduralKeySprite;
                }
            }

            // Respaldo procedural de silueta de llave dorada nítida (32x32)
            const int w = 32;
            const int h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color clear = Color.clear;
            Color gold = new Color(1f, 0.88f, 0.22f, 1f);
            Color highlight = new Color(1f, 0.98f, 0.72f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, clear);
                }
            }

            // Cabeza circular de la llave (izquierda)
            Vector2 headCenter = new Vector2(9f, 16f);
            for (int y = 8; y <= 24; y++)
            {
                for (int x = 2; x <= 16; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), headCenter);
                    if (d <= 6.5f && d >= 2.6f)
                    {
                        tex.SetPixel(x, y, y > 16 ? highlight : gold);
                    }
                }
            }

            // Tallo horizontal de la llave
            for (int x = 15; x <= 29; x++)
            {
                for (int y = 14; y <= 17; y++)
                {
                    tex.SetPixel(x, y, y == 17 ? highlight : gold);
                }
            }

            // Dientes de la llave (derecha abajo)
            for (int y = 9; y <= 14; y++)
            {
                tex.SetPixel(23, y, gold);
                tex.SetPixel(24, y, gold);
                tex.SetPixel(27, y, gold);
                tex.SetPixel(28, y, gold);
            }

            tex.Apply();
            cachedProceduralKeySprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
            return cachedProceduralKeySprite;
        }
    }

    /// <summary>
    /// Componente auxiliar para animar el salto parabólico inicial de la llave soltada por el guardia
    /// y permitir recogerla al pasar cerca en forma humana o presionando E o Q.
    /// </summary>
    public class DroppedKeyQuickPickup : MonoBehaviour
    {
        private KeyItem keyItem;
        private Vector3 startWorldPos;
        private Vector3 targetWorldPos;
        private float arcHeight;
        private float duration;
        private float elapsed;
        private bool landingCompleted;

        public void Initialize(KeyItem item, Vector3 fromPos, Vector3 toPos, float height, float arcDuration)
        {
            keyItem = item;
            startWorldPos = fromPos;
            targetWorldPos = toPos;
            arcHeight = Mathf.Max(0.2f, height);
            duration = Mathf.Max(0.1f, arcDuration);
            elapsed = 0f;
            landingCompleted = false;
            transform.position = startWorldPos;
        }

        private void Update()
        {
            if (keyItem == null)
            {
                Destroy(this);
                return;
            }

            if (!landingCompleted)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 linear = Vector3.Lerp(startWorldPos, targetWorldPos, t);
                float parabola = 4f * t * (1f - t) * arcHeight;
                transform.position = linear + new Vector3(0f, parabola, 0f);

                if (t >= 1f)
                {
                    landingCompleted = true;
                    transform.position = targetWorldPos;
                }
                return;
            }

            // Permitir recoger con tecla E además de Q o al pasar sobre la llave en forma humana
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                float dist = Vector2.Distance(transform.position, player.transform.position);
                if (dist <= 1.65f)
                {
                    TransformationManager tm = player.GetComponent<TransformationManager>();
                    bool isHuman = tm == null || !tm.IsCatForm;
                    if (isHuman && (dist <= 1.15f || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Q)))
                    {
                        keyItem.Interact(player.gameObject);
                    }
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!landingCompleted || keyItem == null || other == null) return;

            PlayerController pc = other.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                TransformationManager tm = pc.GetComponent<TransformationManager>();
                if (tm == null || !tm.IsCatForm)
                {
                    keyItem.Interact(pc.gameObject);
                }
            }
        }
    }
}
