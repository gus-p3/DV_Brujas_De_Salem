using UnityEngine;
using UnityEngine.SceneManagement;

namespace Level2
{
    /// <summary>
    /// Componente de arranque y estabilización del Nivel 2 (Acto 2 — Pueblo y Mercado).
    /// Garantiza la limpieza absoluta de estado al entrar a la escena:
    /// - Corrige el error E-04 del GDD (gato bloqueado al cambiar de nivel):
    ///   fuerza el retorno inmediato a forma humana, restaura dimensiones del collider,
    ///   escala y resetea velocidad física.
    /// - Asegura Time.timeScale = 1.0f (sin pausas arrastradas).
    /// - Limpia estados de invulnerabilidad, frenesí de aldeanos y diálogos de tutorial abiertos.
    /// - Inicia la música de nivel si existe en AudioManager.
    /// - Al salir de la escena (OnDestroy), limpia referencias estáticas para no contaminar Level1.
    /// </summary>
    public class Level2Bootstrap : MonoBehaviour
    {
        public static Level2Bootstrap Instance { get; private set; }

        [Header("Configuración de Arranque")]
        [Tooltip("Si está activo, inicia automáticamente el nivel en GameManager si existe.")]
        [SerializeField] private bool autoStartGameManager = true;

        [Tooltip("Si está activo, fuerza el retorno a forma humana al cargar la escena.")]
        [SerializeField] private bool forceHumanFormOnLoad = true;

        [Tooltip("Si está activo, inicia la reproducción de música ambiental de nivel.")]
        [SerializeField] private bool playLevelMusic = true;

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

            // En Awake solo lo que no depende de otros componentes: el orden de Awake entre objetos no está
            // garantizado y, si la bruja aún no se inicializó, ResetPhysics fijaría su gravedad a 0 de forma
            // permanente (PlayerController toma la gravedad base en su propio Awake). La limpieza completa va en Start.
            Time.timeScale = 1f;
        }

        private void Start()
        {
            CleanSceneEntryState();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Aplica la sanitización completa de estado para prevenir arrastres entre niveles.
        /// </summary>
        public void CleanSceneEntryState()
        {
            // 1. Restaurar escala de tiempo
            Time.timeScale = 1f;

            // 2. Localizar referencias del jugador
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            TransformationManager tm = Object.FindFirstObjectByType<TransformationManager>();
            PlayerInvulnerability inv = Object.FindFirstObjectByType<PlayerInvulnerability>();

            // 3. Mitigación E-04 (GDD): forzar retorno a forma humana y físicas estándar
            if (tm != null && forceHumanFormOnLoad)
            {
                if (tm.IsCatForm)
                {
                    tm.ResetToHumanForm();
                    Debug.Log("[Level2Bootstrap] Mitigación E-04: Forma de gato reseteada a forma humana al entrar.");
                }
                tm.IsInHideout = false;
            }

            if (player != null)
            {
                player.ResetFallReference();
                player.ResetPhysics(Vector2.zero);
                player.CanFly = false;
            }

            // 4. Cancelar invulnerabilidad residual
            if (inv != null)
            {
                inv.CancelInvulnerability();
            }

            // 5. Frenesí de aldeanos en reposo
            if (FrenzyController.Instance != null)
            {
                FrenzyController.Instance.StopFrenzy();
            }

            // 6. Iniciar GameManager en estado Playing si existe
            if (autoStartGameManager && GameManager.Instance != null)
            {
                if (GameManager.Instance.CurrentState != GameState.Playing)
                {
                    GameManager.Instance.SetState(GameState.Playing);
                }
            }

            // 7. Asegurar que paneles emergentes no estén abiertos
            TutorialPanel tutPanel = Object.FindFirstObjectByType<TutorialPanel>(FindObjectsInactive.Include);
            if (tutPanel != null && tutPanel.gameObject.activeSelf)
            {
                tutPanel.gameObject.SetActive(false);
            }

            // 8. Música ambiental del mercado
            if (playLevelMusic && AudioManager.Instance != null && AudioManager.Instance.levelMusicClip != null)
            {
                AudioManager.Instance.PlayMusic(AudioManager.Instance.levelMusicClip);
            }

            Debug.Log("[Level2Bootstrap] Escena Level2 inicializada con estado limpio y sanitizado.");
        }
    }
}
