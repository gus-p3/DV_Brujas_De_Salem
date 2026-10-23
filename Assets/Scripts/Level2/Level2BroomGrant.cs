using UnityEngine;

namespace Level2
{
    /// <summary>
    /// Entrega la escoba completa al entrar al Nivel 2: la bruja ya la reunió en el Acto 1 y aquí no hay piezas.
    /// - Usa solo la API pública de BroomInventory (Collect con las 3 piezas), que dispara el mismo flujo que en el
    ///   Nivel 1: CanFly, aviso en el HUD, tutorial de vuelo y OnBroomCompleted.
    /// - Se ejecuta después de Level2Bootstrap, que revoca el vuelo al entrar (CanFly = false), y lo vuelve a habilitar.
    /// Aislamiento: BroomInventory vive en la instancia de la bruja de esta escena, se reinicia en su Awake y no se
    /// guarda en PlayerPrefs ni en estado estático persistente, así que el Nivel 1 sigue empezando con 0/3 piezas.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class Level2BroomGrant : MonoBehaviour
    {
        private void Start()
        {
            Grant();
        }

        /// <summary>Completa la escoba (si falta) y habilita el vuelo de la bruja de esta escena.</summary>
        public void Grant()
        {
            BroomInventory broom = BroomInventory.Instance;
            if (broom != null && !broom.IsComplete)
            {
                broom.Collect(BroomPart.Branch);
                broom.Collect(BroomPart.Straw);
                broom.Collect(BroomPart.Rope);
            }

            // Sin BroomInventory el permiso depende solo de este campo; con él, deshace el CanFly = false del Bootstrap
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                player.CanFly = true;
            }
        }
    }
}
