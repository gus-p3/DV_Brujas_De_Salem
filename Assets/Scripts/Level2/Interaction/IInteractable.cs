using UnityEngine;

namespace Level2
{
    /// <summary>
    /// Contrato común reutilizable para todos los objetos interactivos de la aldea en el Nivel 2:
    /// puertas de casas, escaleras, cofres, llaves, grimorios y mecanismos.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Identificador único de la interacción.</summary>
        string InteractionId { get; }

        /// <summary>Prioridad cuando hay varios objetos cercanos (mayor valor = mayor prioridad).</summary>
        int Priority { get; }

        /// <summary>
        /// Indica si este objeto se activa con la tecla de interacción (E).
        /// Si es false (por ejemplo, escaleras con W/S), muestra el aviso contextual pero no consume la tecla E.
        /// </summary>
        bool UsesInteractKey { get; }

        /// <summary>Transform del objeto para posicionar indicadores visuales en el mundo.</summary>
        Transform Transform { get; }

        /// <summary>Altura adicional sobre el objeto donde mostrar el aviso contextual.</summary>
        float PromptHeightOffset { get; }

        /// <summary>Indica si el jugador puede interactuar en este momento.</summary>
        bool CanInteract(GameObject interactor);

        /// <summary>Texto contextual que debe mostrarse en el HUD (ej. "E — Entrar", "W / ↑ — Subir").</summary>
        string GetPromptText(GameObject interactor);

        /// <summary>Ejecuta la acción de interacción.</summary>
        void Interact(GameObject interactor);
    }
}
