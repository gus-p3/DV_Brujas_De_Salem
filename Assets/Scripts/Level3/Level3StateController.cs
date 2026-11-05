using System;
using System.Collections.Generic;
using Level2;
using UnityEngine;

namespace Level3
{
    /// <summary>
    /// Fases principales del flujo de juego en el Nivel 3 (El Bosque, la Cabaña de la Bruja y el Regreso a la Iglesia de Salem).
    /// </summary>
    public enum Level3Phase
    {
        /// <summary>
        /// Salir de la aldea hacia la cabaña en el bosque abriendo 2 portones bloqueados
        /// cuyas llaves las portan guardias con brillo especial.
        /// </summary>
        Phase1_EscapeToForestCabins = 0,

        /// <summary>
        /// Entrar o interactuar con el Caldero en la Casa de la Bruja en el bosque
        /// para preparar la Poción Maestra.
        /// </summary>
        Phase2_BrewMasterPotion = 1,

        /// <summary>
        /// Regresar por la ruta hacia la Aldea/Distrito de la Iglesia, recolectar pociones,
        /// leer las pistas (LoreObject), abrir los cofres encadenados (TreasureChest),
        /// obtener la Llave de la Iglesia (church_master_key) y abrir el Portón de la Iglesia.
        /// </summary>
        Phase3_ReturnToVillageAndChurch = 2
    }

    /// <summary>
    /// Singleton por escena que gestiona las 3 fases del Nivel 3, el progreso del Caldero
    /// (Poción Maestra), la lectura de pistas (LoreObject) y los mensajes de objetivo en el HUD.
    /// </summary>
    public class Level3StateController : MonoBehaviour
    {
        private static Level3StateController _instance;

        public static Level3StateController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindFirstObjectByType<Level3StateController>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        public const string ForestGateKey1Id = "forest_gate_key_1";
        public const string ForestGateKey2Id = "forest_gate_key_2";
        public const string ChurchRoofKeyId = "church_roof_key";
        public const string ChurchMasterKeyId = "church_master_key";

        [Header("Estado de Progreso del Nivel 3")]
        [SerializeField] private Level3Phase initialPhase = Level3Phase.Phase1_EscapeToForestCabins;
        [SerializeField] private int totalExpectedClues = 3;
        [SerializeField] private int masterPotionBonusScore = 300;
        [SerializeField] private int masterPotionSleepChargesGrant = 2;

        [Header("Parte del Nivel 3")]
        [Tooltip("Activo en la escena Level3_Village (Parte 2): la Poción Maestra ya está preparada y se empieza en la Fase 3.")]
        [SerializeField] private bool villagePart = false;

        private readonly HashSet<string> readClues = new HashSet<string>();

        public Level3Phase CurrentPhase { get; private set; }
        public bool HasBrewedMasterPotion { get; private set; }
        public int CluesReadCount => readClues.Count;
        public int TotalExpectedClues => Mathf.Max(1, totalExpectedClues);

        /// <summary>True en la Parte 2 (escena Level3_Village); false en la Parte 1 (Bosque y Cabaña).</summary>
        public bool IsVillagePart => villagePart;

        public event Action<Level3Phase> OnPhaseChanged;
        public event Action OnMasterPotionBrewed;
        public event Action<string> OnObjectiveUpdated;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            CurrentPhase = initialPhase;
            HasBrewedMasterPotion = false;
            readClues.Clear();

            if (villagePart)
            {
                // La Poción Maestra se preparó al final de la Parte 1 (escena anterior)
                CurrentPhase = Level3Phase.Phase3_ReturnToVillageAndChurch;
                HasBrewedMasterPotion = true;
            }
        }

        private void Start()
        {
            if (KeyInventory.Instance != null)
            {
                KeyInventory.Instance.OnKeyCollected += HandleKeyCollected;
                KeyInventory.Instance.OnSecretDiscovered += HandleSecretDiscovered;
            }

            BroadcastObjectiveUpdate();
        }

        private void OnDestroy()
        {
            if (KeyInventory.Instance != null)
            {
                KeyInventory.Instance.OnKeyCollected -= HandleKeyCollected;
                KeyInventory.Instance.OnSecretDiscovered -= HandleSecretDiscovered;
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Cambia explícitamente la fase activa del Nivel 3 y notifica al HUD y suscriptores.
        /// </summary>
        public void SetPhase(Level3Phase newPhase, bool showHudBanner = true)
        {
            if (CurrentPhase == newPhase)
            {
                BroadcastObjectiveUpdate();
                return;
            }

            CurrentPhase = newPhase;
            OnPhaseChanged?.Invoke(CurrentPhase);
            BroadcastObjectiveUpdate();

            if (showHudBanner)
            {
                ShowHUDMessage(GetCurrentObjectiveText(), 3.4f);
            }
        }

        /// <summary>
        /// Avanza a la Fase 2 (Preparar la Poción Maestra en la Cabaña de la Bruja) si aún está en la Fase 1.
        /// </summary>
        public void AdvanceToBrewPhase()
        {
            if (CurrentPhase == Level3Phase.Phase1_EscapeToForestCabins && !HasBrewedMasterPotion)
            {
                SetPhase(Level3Phase.Phase2_BrewMasterPotion, showHudBanner: true);
            }
        }

        /// <summary>
        /// Completa la preparación de la Poción Maestra en el Caldero de la Cabaña del Bosque
        /// y avanza automáticamente a la Fase 3 (Regreso a la Aldea y Distrito de la Iglesia).
        /// </summary>
        public void CompletePotionBrewing()
        {
            if (HasBrewedMasterPotion)
            {
                return;
            }

            HasBrewedMasterPotion = true;

            if (GameManager.Instance != null)
            {
                if (masterPotionBonusScore > 0)
                {
                    GameManager.Instance.AddScore(masterPotionBonusScore);
                }

                if (masterPotionSleepChargesGrant > 0)
                {
                    GameManager.Instance.AddSpellCharges(1, 1, masterPotionSleepChargesGrant);
                }
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCheckpointSound();
            }

            OnMasterPotionBrewed?.Invoke();

            CurrentPhase = Level3Phase.Phase3_ReturnToVillageAndChurch;
            OnPhaseChanged?.Invoke(CurrentPhase);
            BroadcastObjectiveUpdate();

            ShowHUDMessage(
                villagePart
                    ? "¡Poción Maestra preparada! Regresa hacia la Aldea y la Iglesia de Salem para romper el sello final."
                    : "¡Poción Maestra preparada! El portón se abre: sigue el Sendero de Regreso hacia la Aldea de Salem.",
                4.0f);
        }

        /// <summary>
        /// Registra la lectura de una pista (LoreObject) y actualiza el contador y el objetivo en el HUD.
        /// </summary>
        public void RegisterClueRead(string clueId)
        {
            if (string.IsNullOrEmpty(clueId))
            {
                return;
            }

            if (!readClues.Add(clueId))
            {
                return;
            }

            BroadcastObjectiveUpdate();

            ShowHUDMessage(
                $"Pista descubierta ({CluesReadCount}/{TotalExpectedClues}): Infíltrate como gato en las casas atrancadas rumbo a la Iglesia.",
                2.8f);
        }

        /// <summary>
        /// Reintento tras "La hoguera te alcanzó…": en lugar de recargar la escena (que devolvía a la bruja a X=0
        /// con el reloj en 00:00), la reaparece en el último checkpoint activado con los corazones llenos.
        /// Llaves, portones abiertos, cofres, pistas, Poción Maestra, puntos y tiempo se conservan tal cual.
        /// </summary>
        public bool RetryFromLastCheckpoint()
        {
            if (GameManager.Instance == null || CheckpointManager.Instance == null) return false;

            Time.timeScale = 1f;
            GameManager.Instance.RestoreAllHearts();
            GameManager.Instance.SetState(GameState.Playing);

            if (FrenzyController.Instance != null) FrenzyController.Instance.StopFrenzy();

            // Guardias de vuelta a su ronda (los dormidos por poción siguen dormidos)
            foreach (EnemySight sight in UnityEngine.Object.FindObjectsByType<EnemySight>(FindObjectsSortMode.None))
            {
                if (sight == null) continue;
                PatrolRoute patrol = sight.GetComponent<PatrolRoute>();
                if (patrol != null && patrol.IsAffectedBySpell) continue;
                sight.ResetState();
                if (patrol != null) patrol.ResetToFirstWaypoint();
            }

            SleepingVillager.ResetAllToSleep();

            CheckpointManager.Instance.RespawnAtCheckpoint();

            // Asegurar munición mínima para no quedar bloqueado tras reaparecer
            if (GameManager.Instance.SleepCharges <= 0)
            {
                GameManager.Instance.AddSpellCharges(0, 0, 2);
            }

            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.UpdateStatsUI();
                hud.ShowMessage("Reapareces en el último punto de control. Tus llaves y avances siguen contigo.", 3.0f);
            }

            BroadcastObjectiveUpdate();
            return true;
        }

        /// <summary>
        /// Devuelve el texto descriptivo del objetivo actual según la fase y el progreso de llaves.
        /// </summary>
        public string GetCurrentObjectiveText()
        {
            KeyInventory inv = KeyInventory.Instance;

            switch (CurrentPhase)
            {
                case Level3Phase.Phase1_EscapeToForestCabins:
                {
                    bool hasKey1 = inv != null && (inv.HasKey(ForestGateKey1Id) || inv.IsDoorUnlocked("forest_gate_1"));
                    bool hasKey2 = inv != null && (inv.HasKey(ForestGateKey2Id) || inv.IsDoorUnlocked("forest_gate_2"));

                    if (!hasKey1)
                    {
                        return "Objetivo: Duerme al guardia brillante con una poción ([Q] o clic) para obtener la Llave del Portón I y escapar al bosque.";
                    }

                    if (!hasKey2)
                    {
                        return "Objetivo: Avanza por el bosque y duerme al segundo guardia brillante para abrir el Portón II hacia la Cabaña.";
                    }

                    return "Objetivo: Cruza el segundo portón del bosque y entra a descansar en la Cabaña de la Bruja.";
                }

                case Level3Phase.Phase2_BrewMasterPotion:
                    return "Objetivo: Interactúa con el Caldero en la Cabaña de la Bruja para preparar la Poción Maestra.";

                case Level3Phase.Phase3_ReturnToVillageAndChurch:
                {
                    if (!villagePart)
                    {
                        return "Objetivo: Cruza el Portón del Sendero de Regreso: la Aldea de Salem te espera.";
                    }

                    bool hasChurchKey = inv != null && inv.HasKey(ChurchMasterKeyId);
                    if (hasChurchKey)
                    {
                        return "Objetivo: ¡Tienes la Llave de la Iglesia! Abre el Portón de la Iglesia de Salem para completar el nivel.";
                    }

                    return $"Objetivo: Regresa al Distrito de la Iglesia, lee las pistas ({CluesReadCount}/{TotalExpectedClues}), abre los cofres encadenados y obtén la Llave de la Iglesia.";
                }

                default:
                    return "Objetivo: Explora el Bosque y la Iglesia de Salem.";
            }
        }

        private void HandleKeyCollected(string keyId)
        {
            if (string.IsNullOrEmpty(keyId)) return;

            if (keyId == ChurchMasterKeyId)
            {
                if (CurrentPhase != Level3Phase.Phase3_ReturnToVillageAndChurch)
                {
                    CurrentPhase = Level3Phase.Phase3_ReturnToVillageAndChurch;
                    OnPhaseChanged?.Invoke(CurrentPhase);
                }

                BroadcastObjectiveUpdate();
                ShowHUDMessage("¡Obtuviste la Llave Maestra de la Iglesia! Dirígete al Portón de la Iglesia.", 3.5f);
                return;
            }

            BroadcastObjectiveUpdate();
        }

        private void HandleSecretDiscovered(string secretId)
        {
            if (!string.IsNullOrEmpty(secretId))
            {
                RegisterClueRead(secretId);
            }
        }

        private void BroadcastObjectiveUpdate()
        {
            OnObjectiveUpdated?.Invoke(GetCurrentObjectiveText());
        }

        private static void ShowHUDMessage(string message, float duration = 2.8f)
        {
            if (string.IsNullOrEmpty(message)) return;

            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage(message, duration);
            }
        }
    }
}
