using UnityEngine;

namespace Level2
{
    /// <summary>
    /// Guardia anti-recolección en forma de gato para todos los pickups del Nivel 2
    /// (Ingredient, PotionPickup, HeartPickup, Collectible, KeyItem).
    /// Cumple GDD §3.2 sin modificar scripts compartidos del Nivel 1:
    /// - Mientras TransformationManager.IsCatForm == true, desactiva el Collider2D trigger y
    ///   los componentes de recogida compartidos para impedir que se consuman en forma felina.
    /// - Si el gato pasa por encima del pickup, muestra el aviso "Necesitas forma humana para recoger esto"
    ///   con enfriamiento para no spamear.
    /// - Al volver a forma humana (incluso estando parado sobre el objeto o al reaparecer),
    ///   reactiva inmediatamente el trigger permitiendo su recogida normal.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class Level2CatPickupGuard : MonoBehaviour
    {
        public const string CatBlockedMessage = "Necesitas forma humana para recoger esto";

        [SerializeField] private Vector2 catDetectionSize = new Vector2(0.95f, 0.95f);

        private Collider2D[] pickupColliders;
        private MonoBehaviour[] guardedScripts;
        private TransformationManager cachedTm;
        private BoxCollider2D cachedPlayerCol;
        private bool collidersSuppressed;
        private static float lastGlobalWarningTime = -10f;
        private static string lastWarningShown = string.Empty;

        // Sin recarga de dominio el estático sobrevive entre partidas y el reloj vuelve a 0
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { lastGlobalWarningTime = -10f; lastWarningShown = string.Empty; }

        public static string LastWarningShown => lastWarningShown;
        public bool IsSuppressedForCat => collidersSuppressed;

        private void Awake()
        {
            CacheComponents();
        }

        private void OnEnable()
        {
            CacheComponents();
            BindTransformationManager();
            SyncCatState();
        }

        private void OnDisable()
        {
            if (cachedTm != null)
            {
                cachedTm.OnTransformed -= HandleTransformed;
            }
            RestorePickupComponents();
        }

        private void CacheComponents()
        {
            pickupColliders = GetComponents<Collider2D>();

            var list = new System.Collections.Generic.List<MonoBehaviour>(4);
            foreach (MonoBehaviour mb in GetComponents<MonoBehaviour>())
            {
                if (mb == null || mb == this) continue;
                string typeName = mb.GetType().Name;
                if (typeName == "PotionPickup" || typeName == "HeartPickup" || typeName == "Collectible")
                {
                    list.Add(mb);
                }
            }
            guardedScripts = list.ToArray();
        }

        private void BindTransformationManager()
        {
            if (cachedTm == null)
            {
                cachedTm = Object.FindFirstObjectByType<TransformationManager>();
            }
            if (cachedTm != null)
            {
                cachedTm.OnTransformed -= HandleTransformed;
                cachedTm.OnTransformed += HandleTransformed;
                if (cachedPlayerCol == null)
                {
                    cachedPlayerCol = cachedTm.GetComponent<BoxCollider2D>();
                }
            }
        }

        private void HandleTransformed(bool isCat)
        {
            ApplySuppression(isCat);
        }

        private void Update()
        {
            SyncCatState();

            if (collidersSuppressed && cachedTm != null && cachedTm.IsCatForm)
            {
                if (IsCatOverlappingPickup(cachedTm.gameObject))
                {
                    NotifyCatBlocked();
                }
            }
        }

        private void FixedUpdate()
        {
            SyncCatState();
        }

        public void SyncCatState()
        {
            if (cachedTm == null)
            {
                BindTransformationManager();
            }

            bool isCat = cachedTm != null && cachedTm.IsCatForm;
            ApplySuppression(isCat);
        }

        private void ApplySuppression(bool suppress)
        {
            if (suppress && !collidersSuppressed)
            {
                collidersSuppressed = true;
                if (guardedScripts != null)
                {
                    for (int i = 0; i < guardedScripts.Length; i++)
                    {
                        if (guardedScripts[i] != null) guardedScripts[i].enabled = false;
                    }
                }
                if (pickupColliders != null)
                {
                    for (int i = 0; i < pickupColliders.Length; i++)
                    {
                        if (pickupColliders[i] != null) pickupColliders[i].enabled = false;
                    }
                }
            }
            else if (!suppress && collidersSuppressed)
            {
                RestorePickupComponents();
            }
        }

        private void RestorePickupComponents()
        {
            collidersSuppressed = false;
            if (guardedScripts != null)
            {
                for (int i = 0; i < guardedScripts.Length; i++)
                {
                    if (guardedScripts[i] != null) guardedScripts[i].enabled = true;
                }
            }
            if (pickupColliders != null)
            {
                for (int i = 0; i < pickupColliders.Length; i++)
                {
                    if (pickupColliders[i] != null)
                    {
                        pickupColliders[i].enabled = false;
                        pickupColliders[i].enabled = true;
                    }
                }
            }
        }

        public bool IsCatOverlappingPickup(GameObject player)
        {
            if (player == null) return false;
            Vector2 playerCenter = player.transform.position;
            if (cachedPlayerCol == null) cachedPlayerCol = player.GetComponent<BoxCollider2D>();
            if (cachedPlayerCol != null)
            {
                playerCenter = cachedPlayerCol.bounds.center;
            }

            Vector2 myPos = transform.position;
            float dx = Mathf.Abs(playerCenter.x - myPos.x);
            float dy = Mathf.Abs(playerCenter.y - myPos.y);
            return dx <= catDetectionSize.x * 0.65f && dy <= catDetectionSize.y * 0.65f;
        }

        /// <summary>
        /// Evalúa un intento de recolección sobre este pickup (usado también por verificadores).
        /// Devuelve false si está bloqueado por forma de gato, true si puede recogerse en forma humana.
        /// </summary>
        public bool EvaluatePickupAttempt(GameObject player)
        {
            if (player == null) return false;
            TransformationManager tm = player.GetComponent<TransformationManager>();
            if (tm == null) tm = cachedTm;

            bool isCat = tm != null && tm.IsCatForm;
            ApplySuppression(isCat);

            if (isCat)
            {
                NotifyCatBlocked(forceShow: true);
                return false;
            }
            return true;
        }

        public static void NotifyCatBlocked(bool forceShow = false)
        {
            if (!forceShow && Time.unscaledTime - lastGlobalWarningTime < 2.0f) return;
            lastGlobalWarningTime = Time.unscaledTime;
            lastWarningShown = CatBlockedMessage;

            HUDController hud = Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage(CatBlockedMessage, 2.0f);
            }

            if (IngredientHUD.Instance != null)
            {
                IngredientHUD.Instance.ShowWarning(CatBlockedMessage);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClickSound();
            }
        }
    }
}
