using Level2;
using UnityEngine;

namespace Level3
{
    /// <summary>
    /// Componente de arranque y estabilización del Nivel 3 (El Bosque, la Cabaña de la Bruja y la Iglesia de Salem).
    /// Garantiza:
    /// - Time.timeScale = 1f.
    /// - Limpieza de estados residuales (fuerza forma humana al iniciar, cancela invulnerabilidad y frenesí).
    /// - Fija el esquema de controles del nivel: [Q] interactúa y [E] o clic izquierdo lanzan pociones.
    /// - Asegura que el jugador tenga PlayerInteractor, Level2Controls, LadderClimber, KeyInventory,
    ///   BroomInventory con escoba desbloqueada (GrantBroom()) y PotionInventory inicializado con al menos
    ///   3 pociones de dormir (Sleep) al empezar el nivel para que nunca quede bloqueado.
    /// </summary>
    [DefaultExecutionOrder(-600)]
    public class Level3Bootstrap : MonoBehaviour
    {
        public static Level3Bootstrap Instance { get; private set; }

        [Header("Configuración de Arranque del Nivel 3")]
        [SerializeField] private bool autoStartGameManager = true;
        [SerializeField] private bool forceHumanFormOnLoad = true;
        [SerializeField] private bool playLevelMusic = true;
        [SerializeField] private int minimumStartingSleepPotions = 3;
        [SerializeField] private int minimumStartingBlindPotions = 1;
        [SerializeField] private int minimumStartingHypnotizePotions = 1;

        /// <summary>Esquema de controles del Nivel 3: [Q] interactúa, [E] o clic izquierdo lanzan la poción.</summary>
        public const KeyCode InteractKey = KeyCode.Q;
        public const KeyCode ThrowPotionKey = KeyCode.E;

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

            Time.timeScale = 1f;

            // Teclas separadas por completo: E nunca lanza (Level2Controls suprime PotionInventory ese frame)
            // y Q/clic nunca interactúan, aunque haya un cartel, nota o alijo al lado.
            Level2Controls.SetSceneScheme(InteractKey, ThrowPotionKey);
        }

        private void Start()
        {
            InitializeLevel3State();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Level2Controls.ResetScheme();
            }
        }

        /// <summary>
        /// Ejecuta toda la inicialización, limpieza de estado y provisión de componentes e inventarios del Nivel 3.
        /// </summary>
        public void InitializeLevel3State()
        {
            // 1. Asegurar escala de tiempo activa
            Time.timeScale = 1f;

            // 2. Asegurar que exista KeyInventory limpio para la sesión y Level3StateController
            EnsureSceneSingletons();

            // 3. Localizar referencias del jugador
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            TransformationManager tm = Object.FindFirstObjectByType<TransformationManager>();
            PlayerInvulnerability inv = Object.FindFirstObjectByType<PlayerInvulnerability>();

            // 4. Forzar forma humana al iniciar y limpiar estado de escondite
            if (tm != null && forceHumanFormOnLoad)
            {
                if (tm.IsCatForm)
                {
                    tm.ResetToHumanForm();
                }
                tm.IsInHideout = false;
            }

            // 5. Asegurar componentes requeridos en el jugador y desbloquear escoba
            if (player != null)
            {
                LadderClimber climber = player.GetComponent<LadderClimber>();
                if (climber == null)
                {
                    climber = player.gameObject.AddComponent<LadderClimber>();
                }
                else
                {
                    climber.StopClimbing(restoreVelocityZero: true);
                }

                if (player.GetComponent<LadderClimbAnimator>() == null)
                {
                    player.gameObject.AddComponent<LadderClimbAnimator>();
                }

                if (player.GetComponent<PlayerInteractor>() == null)
                {
                    player.gameObject.AddComponent<PlayerInteractor>();
                }

                if (player.GetComponent<Level2Controls>() == null)
                {
                    player.gameObject.AddComponent<Level2Controls>();
                }

                PotionInventory potionInventory = player.GetComponent<PotionInventory>();
                if (potionInventory == null)
                {
                    potionInventory = player.gameObject.AddComponent<PotionInventory>();
                }

                BroomInventory broomInventory = player.GetComponent<BroomInventory>();
                if (broomInventory == null)
                {
                    broomInventory = Object.FindFirstObjectByType<BroomInventory>();
                }
                if (broomInventory == null)
                {
                    broomInventory = player.gameObject.AddComponent<BroomInventory>();
                }

                broomInventory.GrantBroom();

                player.ResetFallReference();
                player.ResetPhysics(Vector2.zero);
                player.CancelFlight();
                player.CanFly = true;
            }

            // 6. Asegurar que el jugador empiece con al menos 3 pociones de dormir (Sleep) para no quedar bloqueado
            EnsureMinimumPotions();

            // 7. Cancelar invulnerabilidad residual y detener frenesí
            if (inv != null)
            {
                inv.CancelInvulnerability();
            }

            if (FrenzyController.Instance != null)
            {
                FrenzyController.Instance.StopFrenzy();
            }

            // 8. Iniciar GameManager en estado Playing
            if (autoStartGameManager && GameManager.Instance != null)
            {
                if (GameManager.Instance.CurrentState != GameState.Playing)
                {
                    GameManager.Instance.SetState(GameState.Playing);
                }
            }

            // 9. Cerrar paneles de tutorial residuales si estaban abiertos
            TutorialPanel tutPanel = Object.FindFirstObjectByType<TutorialPanel>(FindObjectsInactive.Include);
            if (tutPanel != null && tutPanel.gameObject.activeSelf)
            {
                tutPanel.gameObject.SetActive(false);
            }

            // 10. Iniciar música del nivel
            if (playLevelMusic && AudioManager.Instance != null)
            {
                AudioClip clip = AudioManager.Instance.level3MusicClip != null ? AudioManager.Instance.level3MusicClip : AudioManager.Instance.levelMusicClip;
                if (clip != null) AudioManager.Instance.PlayMusic(clip);
            }

            Debug.Log("[Level3Bootstrap] Escena Level3 inicializada con escoba activa, controles y pociones de sueño garantizadas.");
        }

        private void EnsureSceneSingletons()
        {
            if (KeyInventory.Instance == null)
            {
                GameObject keyInvGo = new GameObject("KeyInventory");
                keyInvGo.AddComponent<KeyInventory>();
            }
            else
            {
                KeyInventory.Instance.ResetExplorationProgress();
            }

            if (Level3StateController.Instance == null)
            {
                GameObject stateGo = new GameObject("Level3StateController");
                stateGo.AddComponent<Level3StateController>();
            }
        }

        private void EnsureMinimumPotions()
        {
            if (GameManager.Instance == null) return;

            int addBlind = Mathf.Max(0, minimumStartingBlindPotions - GameManager.Instance.BlindCharges);
            int addHypnotize = Mathf.Max(0, minimumStartingHypnotizePotions - GameManager.Instance.HypnotizeCharges);
            int addSleep = Mathf.Max(0, minimumStartingSleepPotions - GameManager.Instance.SleepCharges);

            if (addBlind > 0 || addHypnotize > 0 || addSleep > 0)
            {
                GameManager.Instance.AddSpellCharges(addBlind, addHypnotize, addSleep);
            }

            PotionInventory potionInv = Object.FindFirstObjectByType<PotionInventory>();
            if (potionInv != null)
            {
                potionInv.SelectPotion(PotionType.Sleep);
            }

            HUDController hud = Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.UpdateStatsUI();
            }
        }
    }
}
