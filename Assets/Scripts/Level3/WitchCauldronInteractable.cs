using System.Collections;
using Level2;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Level3
{
    /// <summary>
    /// Representa el Caldero Místico / Mesa de Alquimia dentro de la Casa de la Bruja en el Bosque (Acto 2).
    /// Implementa Level2.IInteractable y se usa con [E] (la tecla de interactuar del Nivel 3).
    /// - Primera interacción: prepara la Poción Maestra Consagrada, cura toda la vida en HealthManager,
    ///   otorga +3 Pociones de Sueño y +2 de Cegar, llama a Level3StateController.Instance.CompletePotionBrewing()
    ///   y desbloquea automáticamente el Portón del Sendero de Regreso ("return_village_gate").
    /// - Interacciones posteriores: funciona como Estación de Reabastecimiento gratuita si la bruja
    ///   tiene menos de 2 pociones de dormir para evitar cualquier softlock en el Acto 3.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class WitchCauldronInteractable : MonoBehaviour, IInteractable
    {
        public const string ReturnGateId = "return_village_gate";
        public const string BrewedHudMessage = "¡Poción Consagrada lista! El Portón del Sendero de Regreso se abre: síguelo hasta la Aldea de Salem.";

        [Header("Configuración de Alquimia")]
        [SerializeField] private string cauldronId = "witch_cabin_cauldron";
        [SerializeField] private string cauldronDisplayName = "Caldero Místico de la Bruja";
        [SerializeField] private int sleepPotionsGrant = 3;
        [SerializeField] private int blindPotionsGrant = 2;
        [SerializeField] private int minSleepPotionsForRefill = 2;

        [Header("Referencias Opcionales")]
        [SerializeField] private LockedGateInteractable returnVillageGate;
        [SerializeField] private Light2D cauldronGlowLight;
        [SerializeField] private SpriteRenderer cauldronRenderer;

        private Transform auraTransform;
        private SpriteRenderer auraRenderer;
        private readonly Transform[] risingBubbles = new Transform[5];
        private readonly SpriteRenderer[] bubbleRenderers = new SpriteRenderer[5];
        private float lastInteractTime = -10f;

        public string InteractionId => cauldronId;
        public int Priority => 35;
        public bool UsesInteractKey => true;
        public Transform Transform => transform;
        public float PromptHeightOffset => 1.65f;

        public void SetReturnGate(LockedGateInteractable gate)
        {
            returnVillageGate = gate;
        }

        private void Awake()
        {
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
                if (col.size.x < 2.2f || col.size.y < 2.0f)
                {
                    col.size = new Vector2(Mathf.Max(col.size.x, 2.6f), Mathf.Max(col.size.y, 2.4f));
                }
            }

            if (cauldronRenderer == null)
            {
                cauldronRenderer = GetComponent<SpriteRenderer>();
                if (cauldronRenderer == null)
                {
                    cauldronRenderer = GetComponentInChildren<SpriteRenderer>();
                }
            }

            BuildCauldronVisualFX();
        }

        private void Update()
        {
            AnimateCauldronIdleFX();

        }

        public bool CanInteract(GameObject interactor)
        {
            return interactor != null;
        }

        public string GetPromptText(GameObject interactor)
        {
            bool brewed = Level3StateController.Instance != null && Level3StateController.Instance.HasBrewedMasterPotion;
            if (!brewed)
            {
                return $"[E] Preparar Poción Maestra en {cauldronDisplayName}";
            }

            int sleepCount = GameManager.Instance != null ? GameManager.Instance.SleepCharges : 0;
            if (sleepCount < minSleepPotionsForRefill)
            {
                return $"[E] Reabastecer Pociones de Sueño en {cauldronDisplayName}";
            }

            return $"[E] Examinar {cauldronDisplayName} (Poción Maestra lista)";
        }

        public void Interact(GameObject interactor)
        {
            if (interactor == null) return;
            if (Time.unscaledTime < lastInteractTime + 0.35f) return;
            lastInteractTime = Time.unscaledTime;

            TransformationManager tm = interactor.GetComponent<TransformationManager>();
            if (tm != null && tm.IsCatForm)
            {
                ShowHUDMessage("Necesitas tu forma humana [T] para preparar pociones en el caldero.", 2.4f);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayErrorSound();
                return;
            }

            bool alreadyBrewed = Level3StateController.Instance != null && Level3StateController.Instance.HasBrewedMasterPotion;
            if (!alreadyBrewed)
            {
                BrewMasterPotionSequence();
            }
            else
            {
                RefillPotionsIfLow();
            }
        }

        private void BrewMasterPotionSequence()
        {
            // 1. Curación completa en HealthManager / GameManager
            if (HealthManager.Instance != null)
            {
                int missingHearts = Mathf.Max(0, HealthManager.Instance.MaxHearts - HealthManager.Instance.CurrentHearts);
                if (missingHearts > 0)
                {
                    HealthManager.Instance.Curar(missingHearts);
                }
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.GainHeart(GameManager.Instance.MaxHearts);
            }

            // 2. Recarga de +3 pociones de Sueño y +2 de Cegar
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddSpellCharges(blindPotionsGrant, 0, sleepPotionsGrant);
            }

            PotionInventory potInv = Object.FindFirstObjectByType<PotionInventory>();
            if (potInv != null)
            {
                potInv.SelectPotion(PotionType.Sleep);
            }

            // 3. Avanzar estado del Nivel 3
            if (Level3StateController.Instance != null)
            {
                Level3StateController.Instance.CompletePotionBrewing();
            }

            // 4. Desbloquear automáticamente el Portón del Sendero de Regreso ("return_village_gate")
            UnlockReturnVillageGate();

            // 5. Efectos visuales y sonoros de alquimia
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayIngredientPickupSound();
                AudioManager.Instance.PlayHeartHealSound();
            }

            LootPopupVisualizer.ShowPotionReward(transform.position + Vector3.up * 0.6f, PotionType.Sleep, sleepPotionsGrant);
            StartCoroutine(AlchemyBurstRoutine());

            // 6. Mostrar mensaje destacado en el HUD
            ShowHUDMessage(BrewedHudMessage, 4.5f);
        }

        private void RefillPotionsIfLow()
        {
            if (GameManager.Instance == null) return;

            int currentSleep = GameManager.Instance.SleepCharges;
            if (currentSleep < minSleepPotionsForRefill)
            {
                int neededSleep = Mathf.Max(1, minSleepPotionsForRefill + 1 - currentSleep);
                int neededBlind = GameManager.Instance.BlindCharges <= 0 ? 1 : 0;

                GameManager.Instance.AddSpellCharges(neededBlind, 0, neededSleep);

                PotionInventory potInv = Object.FindFirstObjectByType<PotionInventory>();
                if (potInv != null)
                {
                    potInv.SelectPotion(PotionType.Sleep);
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayPotionPickupSound();
                }

                LootPopupVisualizer.ShowPotionReward(transform.position + Vector3.up * 0.5f, PotionType.Sleep, neededSleep);
                StartCoroutine(AlchemyBurstRoutine());
                ShowHUDMessage($"¡Caldero reabastecido! Recibiste +{neededSleep} Pociones de Sueño para tu regreso a la Iglesia.", 3.0f);
            }
            else
            {
                ShowHUDMessage("Tu reserva alquímica está llena. Regresa hacia la Aldea y abre la Iglesia de Salem.", 2.5f);
            }
        }

        private void UnlockReturnVillageGate()
        {
            if (returnVillageGate != null)
            {
                returnVillageGate.UnlockGate(immediate: false);
                return;
            }

            LockedGateInteractable[] gates = Object.FindObjectsByType<LockedGateInteractable>(FindObjectsSortMode.None);
            for (int i = 0; i < gates.Length; i++)
            {
                if (gates[i] != null && (gates[i].GateId == ReturnGateId || gates[i].RequireMasterPotion))
                {
                    returnVillageGate = gates[i];
                    gates[i].UnlockGate(immediate: false);
                }
            }
        }

        private void BuildCauldronVisualFX()
        {
            Transform existingAura = transform.Find("CauldronMysticAura");
            if (existingAura != null)
            {
                auraTransform = existingAura;
                auraRenderer = existingAura.GetComponent<SpriteRenderer>();
                return;
            }

            GameObject auraGo = new GameObject("CauldronMysticAura");
            auraGo.transform.SetParent(transform, false);
            auraGo.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            auraGo.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
            auraTransform = auraGo.transform;

            auraRenderer = auraGo.AddComponent<SpriteRenderer>();
            auraRenderer.sprite = LootPopupVisualizer.GetHaloSprite();
            auraRenderer.color = new Color(0.35f, 0.95f, 0.55f, 0.65f);
            auraRenderer.sortingLayerName = "Gameplay";
            auraRenderer.sortingOrder = 2;

            Sprite starSprite = LootPopupVisualizer.GetStarSprite();
            for (int i = 0; i < risingBubbles.Length; i++)
            {
                GameObject bGo = new GameObject($"MysticBubble_{i}");
                bGo.transform.SetParent(transform, false);
                bGo.transform.localPosition = new Vector3((i - 2) * 0.22f, 0.6f + (i % 3) * 0.25f, 0f);
                bGo.transform.localScale = Vector3.one * 0.28f;

                SpriteRenderer bSr = bGo.AddComponent<SpriteRenderer>();
                bSr.sprite = starSprite;
                bSr.color = (i % 2 == 0)
                    ? new Color(0.35f, 1f, 0.58f, 0.85f)
                    : new Color(0.78f, 0.42f, 1f, 0.85f);
                bSr.sortingLayerName = "Foreground";
                bSr.sortingOrder = 8;

                risingBubbles[i] = bGo.transform;
                bubbleRenderers[i] = bSr;
            }

            if (cauldronGlowLight == null)
            {
                cauldronGlowLight = GetComponentInChildren<Light2D>();
                if (cauldronGlowLight == null)
                {
                    GameObject lightGo = new GameObject("CauldronLight2D");
                    lightGo.transform.SetParent(transform, false);
                    lightGo.transform.localPosition = new Vector3(0f, 0.85f, 0f);
                    cauldronGlowLight = lightGo.AddComponent<Light2D>();
                    cauldronGlowLight.lightType = Light2D.LightType.Point;
                    cauldronGlowLight.color = new Color(0.42f, 0.95f, 0.62f, 1f);
                    cauldronGlowLight.intensity = 1.55f;
                    cauldronGlowLight.pointLightInnerRadius = 0.4f;
                    cauldronGlowLight.pointLightOuterRadius = 3.8f;
                }
            }
        }

        private void AnimateCauldronIdleFX()
        {
            float t = Time.time;
            bool brewed = Level3StateController.Instance != null && Level3StateController.Instance.HasBrewedMasterPotion;

            Color primary = brewed
                ? new Color(0.75f, 0.38f, 1f, 0.72f)
                : new Color(0.32f, 0.95f, 0.55f, 0.68f);

            if (auraTransform != null)
            {
                float s = 1.75f + Mathf.Sin(t * 3.2f) * 0.22f;
                auraTransform.localScale = new Vector3(s, s, 1f);
                auraTransform.localRotation = Quaternion.Euler(0f, 0f, t * 38f);
                if (auraRenderer != null)
                {
                    auraRenderer.color = primary;
                }
            }

            for (int i = 0; i < risingBubbles.Length; i++)
            {
                if (risingBubbles[i] == null) continue;
                float cycle = (t * (0.65f + i * 0.12f) + i * 0.37f) % 1f;
                float x = (i - 2) * 0.22f + Mathf.Sin(t * 3f + i) * 0.08f;
                float y = Mathf.Lerp(0.55f, 1.95f, cycle);
                risingBubbles[i].localPosition = new Vector3(x, y, 0f);
                float scale = Mathf.Sin(cycle * Mathf.PI) * 0.34f;
                risingBubbles[i].localScale = Vector3.one * scale;
            }
        }

        private IEnumerator AlchemyBurstRoutine()
        {
            float duration = 1.1f;
            float elapsed = 0f;
            float baseIntensity = cauldronGlowLight != null ? cauldronGlowLight.intensity : 1.55f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float p = elapsed / duration;
                float flash = Mathf.Sin(p * Mathf.PI);

                if (cauldronGlowLight != null)
                {
                    cauldronGlowLight.color = Color.Lerp(
                        new Color(0.35f, 1f, 0.6f, 1f),
                        new Color(0.82f, 0.38f, 1f, 1f),
                        p);
                    cauldronGlowLight.intensity = baseIntensity + flash * 2.4f;
                    cauldronGlowLight.pointLightOuterRadius = 3.8f + flash * 2.5f;
                }

                yield return null;
            }

            if (cauldronGlowLight != null)
            {
                cauldronGlowLight.intensity = baseIntensity;
                cauldronGlowLight.pointLightOuterRadius = 3.8f;
            }
        }

        private static void ShowHUDMessage(string message, float duration)
        {
            HUDController hud = Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage(message, duration);
            }
        }
    }
}
