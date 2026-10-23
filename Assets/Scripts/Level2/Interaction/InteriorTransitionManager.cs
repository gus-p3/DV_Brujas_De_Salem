using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Level2
{
    /// <summary>
    /// Gestiona las transiciones entre el exterior de la aldea y los interiores de las casas.
    /// - Conserva íntegramente la salud, el inventario de pociones, los ingredientes, las llaves,
    ///   el progreso de misión, el temporizador y la orientación de la bruja.
    /// - Ajusta los límites de CameraFollow al entrar y salir de las casas, con SnapToTarget().
    /// - Oculta el paralaje exterior mientras el jugador explora un interior cerrado para una
    ///   lectura visual limpia de las habitaciones y pisos.
    /// - Ejecuta un fundido suave en Play Mode y soporta ejecución síncrona para pruebas.
    /// </summary>
    public class InteriorTransitionManager : MonoBehaviour
    {
        private static InteriorTransitionManager _instance;
        public static InteriorTransitionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindFirstObjectByType<InteriorTransitionManager>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Límites Exteriores por Defecto")]
        [SerializeField] private Vector2 exteriorMinLimit = new Vector2(-5f, -2.5f);
        [SerializeField] private Vector2 exteriorMaxLimit = new Vector2(195f, 16f);
        [SerializeField] private Vector3 exteriorCameraOffset = new Vector3(0f, 1.2f, -10f);

        [Header("Transición Visual")]
        [SerializeField] private float fadeDuration = 0.16f;
        [SerializeField] private Image fadeOverlayImage;

        private CameraFollow cameraFollow;
        private Transform parallaxRoot;
        private InteriorZone currentInterior;
        private bool isTransitioning;

        // Campos cacheados de CameraFollow y PlayerController para ajuste no invasivo
        private FieldInfo camMinField;
        private FieldInfo camMaxField;
        private FieldInfo camOffsetField;
        private FieldInfo camUseLimitsField;
        private FieldInfo playerFacingField;

        public InteriorZone CurrentInterior => currentInterior;
        public bool IsInsideHouse => currentInterior != null;
        public bool IsTransitioning => isTransitioning;

        public event Action<InteriorZone> OnInteriorChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            CacheReflectionFields();
            ResolveSceneReferences();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void CacheReflectionFields()
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            camMinField = typeof(CameraFollow).GetField("minLimit", flags);
            camMaxField = typeof(CameraFollow).GetField("maxLimit", flags);
            camOffsetField = typeof(CameraFollow).GetField("offset", flags);
            camUseLimitsField = typeof(CameraFollow).GetField("useLimits", flags);
            playerFacingField = typeof(PlayerController).GetField("facingRight", flags);
        }

        private void ResolveSceneReferences()
        {
            if (cameraFollow == null)
            {
                cameraFollow = UnityEngine.Object.FindFirstObjectByType<CameraFollow>();
                if (cameraFollow != null)
                {
                    exteriorMinLimit = cameraFollow.MinLimit;
                    exteriorMaxLimit = cameraFollow.MaxLimit;
                }
            }

            if (parallaxRoot == null)
            {
                GameObject env = GameObject.Find("Environment");
                if (env != null)
                {
                    parallaxRoot = env.transform.Find("Parallax");
                }
            }
        }

        /// <summary>
        /// Ejecuta el cruce por una puerta hacia su puerta de destino asociada.
        /// </summary>
        public void TransitionThroughDoor(HouseDoor sourceDoor, GameObject player, bool immediate = false)
        {
            if (sourceDoor == null || sourceDoor.TargetDoor == null || player == null)
            {
                Debug.LogWarning("[InteriorTransitionManager] Puerta de origen, destino o jugador nulos.");
                return;
            }

            if (isTransitioning && !immediate) return;

            if (immediate || !Application.isPlaying || fadeDuration <= 0.01f)
            {
                PerformTeleportAndCameraSync(sourceDoor, sourceDoor.TargetDoor, player);
            }
            else
            {
                StartCoroutine(FadeTransitionRoutine(sourceDoor, sourceDoor.TargetDoor, player));
            }
        }

        private IEnumerator FadeTransitionRoutine(HouseDoor source, HouseDoor destination, GameObject player)
        {
            isTransitioning = true;
            EnsureFadeOverlay();

            if (fadeOverlayImage != null)
            {
                fadeOverlayImage.gameObject.SetActive(true);
                float t = 0f;
                while (t < fadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    SetFadeAlpha(Mathf.Clamp01(t / fadeDuration));
                    yield return null;
                }
                SetFadeAlpha(1f);
            }

            PerformTeleportAndCameraSync(source, destination, player);
            yield return new WaitForSecondsRealtime(0.04f);

            if (fadeOverlayImage != null)
            {
                float t = 0f;
                while (t < fadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    SetFadeAlpha(1f - Mathf.Clamp01(t / fadeDuration));
                    yield return null;
                }
                SetFadeAlpha(0f);
                fadeOverlayImage.gameObject.SetActive(false);
            }

            isTransitioning = false;
        }

        public void PerformTeleportAndCameraSync(HouseDoor source, HouseDoor destination, GameObject player)
        {
            CacheReflectionFields();
            ResolveSceneReferences();

            // 1. Detener escalada o vuelo activos y limpiar velocidades residuales
            LadderClimber climber = player.GetComponent<LadderClimber>();
            if (climber != null && climber.IsClimbing)
            {
                climber.StopClimbing(restoreVelocityZero: true);
            }

            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.CancelFlight();
                pc.ResetFallReference();
                pc.ResetPhysics(Vector2.zero);
            }

            // 2. Calcular posición segura de aparición en el suelo de la puerta de destino
            Vector3 targetPos = destination.GetSafeSpawnPosition(player);
            player.transform.position = targetPos;

            // 3. Conservar o aplicar orientación
            if (pc != null && destination.OverrideFacingOnExit != 0)
            {
                bool faceRight = destination.OverrideFacingOnExit > 0;
                if (playerFacingField != null)
                {
                    playerFacingField.SetValue(pc, faceRight);
                }
                SpriteRenderer sr = pc.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.flipX = !faceRight;
                WitchRigAnimator rig = pc.GetComponentInChildren<WitchRigAnimator>();
                if (rig != null) rig.FacingRight = faceRight;
            }

            Physics2D.SyncTransforms();

            // 4. Actualizar estado de interior y límites de cámara
            currentInterior = destination.OwningInterior;

            if (parallaxRoot != null)
            {
                parallaxRoot.gameObject.SetActive(currentInterior == null);
            }

            ApplyCameraBounds(currentInterior);

            // 5. Mostrar banner de ubicación y efecto de sonido
            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                if (currentInterior != null && !string.IsNullOrEmpty(currentInterior.HouseDisplayName))
                {
                    hud.ShowMessage(currentInterior.HouseDisplayName, 1.8f);
                }
                else if (source != null && source.OwningInterior != null)
                {
                    hud.ShowMessage("Calles de Salem", 1.4f);
                }
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClickSound();
            }

            OnInteriorChanged?.Invoke(currentInterior);
        }

        public void ApplyCameraBounds(InteriorZone interior)
        {
            ResolveSceneReferences();
            if (cameraFollow == null) return;

            Vector2 min = interior != null ? interior.CameraMinLimit : exteriorMinLimit;
            Vector2 max = interior != null ? interior.CameraMaxLimit : exteriorMaxLimit;
            Vector3 off = interior != null ? interior.CameraOffset : exteriorCameraOffset;

            if (camUseLimitsField != null) camUseLimitsField.SetValue(cameraFollow, true);
            if (camMinField != null) camMinField.SetValue(cameraFollow, min);
            if (camMaxField != null) camMaxField.SetValue(cameraFollow, max);
            if (camOffsetField != null) camOffsetField.SetValue(cameraFollow, off);

            cameraFollow.SnapToTarget();
        }

        private void EnsureFadeOverlay()
        {
            if (fadeOverlayImage != null) return;

            Canvas promptCanvas = InteractionPromptUI.Instance != null
                ? InteractionPromptUI.Instance.GetComponent<Canvas>()
                : UnityEngine.Object.FindFirstObjectByType<Canvas>();

            if (promptCanvas == null) return;

            Transform existing = promptCanvas.transform.Find("DoorTransitionFade");
            if (existing != null)
            {
                fadeOverlayImage = existing.GetComponent<Image>();
                return;
            }

            GameObject fadeObj = new GameObject("DoorTransitionFade");
            fadeObj.transform.SetParent(promptCanvas.transform, false);
            fadeObj.transform.SetAsLastSibling();

            RectTransform rt = fadeObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            fadeOverlayImage = fadeObj.AddComponent<Image>();
            fadeOverlayImage.color = new Color(0.04f, 0.03f, 0.05f, 0f);
            fadeOverlayImage.raycastTarget = false;
            fadeObj.SetActive(false);
        }

        private void SetFadeAlpha(float alpha)
        {
            if (fadeOverlayImage == null) return;
            Color c = fadeOverlayImage.color;
            c.a = alpha;
            fadeOverlayImage.color = c;
        }
    }
}
