using UnityEngine;
using UnityEngine.SceneManagement;

namespace Level3
{
    /// <summary>
    /// Final de la Parte 1 del Nivel 3: tras cruzar el Portón del Sendero de Regreso (abierto por la Poción Maestra),
    /// muestra "Fin de la Parte 1" y carga la escena de la Aldea (Level3_Village) conservando puntos, reloj y pociones.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Level3PartExit : MonoBehaviour
    {
        public const string VillageSceneName = "Level3_Village";

        [SerializeField] private string nextSceneName = VillageSceneName;
        [SerializeField] private bool requireMasterPotion = true;
        [SerializeField] private string outroKicker = "FIN DE LA PARTE 1";
        [SerializeField] private string outroTitle = "Rumbo a la Aldea de Salem";
        [TextArea(2, 3)]
        [SerializeField] private string outroSubtitle = "Con la Poción Maestra en el morral, la bruja vuelve a la aldea dormida…";

        private bool triggered;
        private float lastHintTime = -10f;

        public string NextSceneName => nextSceneName;

        private void Awake()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other) => TryExit(other);
        private void OnTriggerStay2D(Collider2D other) => TryExit(other);

        private void TryExit(Collider2D other)
        {
            if (triggered || other == null) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            Level3StateController state = Level3StateController.Instance;
            if (requireMasterPotion && (state == null || !state.HasBrewedMasterPotion))
            {
                if (Time.unscaledTime > lastHintTime + 3f)
                {
                    lastHintTime = Time.unscaledTime;
                    HUDController hud = Object.FindFirstObjectByType<HUDController>();
                    if (hud != null) hud.ShowMessage("Primero prepara la Poción Maestra en el Caldero de la cabaña [E].", 2.6f);
                }
                return;
            }

            triggered = true;
            Level3ChapterCard card = Level3ChapterCard.Instance;
            if (card != null)
            {
                card.PlayOutroAndLoad(outroKicker, outroTitle, outroSubtitle, nextSceneName, CarryProgress);
            }
            else
            {
                CarryProgress();
                SceneManager.LoadScene(nextSceneName);
            }
        }

        private static void CarryProgress()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            gm.CarryRunIntoNextScene();
            // Desbloquea la Parte 2 para "Continuar" desde el menú principal
            SaveSystem.SaveLevelProgress(SceneManager.GetActiveScene().buildIndex, gm.Score);
        }
    }
}
