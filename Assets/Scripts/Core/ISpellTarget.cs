/// <summary>
/// Interfaz para cualquier entidad del juego susceptible de recibir los efectos
/// mágicos de las pociones arrojables (Cegar, Hipnotizar y Dormir).
/// </summary>
public interface ISpellTarget
{
    /// <summary>
    /// Aplica el efecto de ceguera: desactiva la visión y reduce la velocidad de movimiento.
    /// </summary>
    void ApplyBlind(float seconds);

    /// <summary>
    /// Aplica el efecto de hipnosis: obliga al objetivo a caminar en la dirección indicada (-1 o 1).
    /// </summary>
    void ApplyHypnotize(float seconds, int direction);

    /// <summary>
    /// Aplica el efecto de sueño: inmoviliza por completo al objetivo y apaga su percepción.
    /// </summary>
    void ApplySleep(float seconds);

    /// <summary>
    /// Indica si el objetivo se encuentra actualmente bajo la influencia de algún hechizo.
    /// </summary>
    bool IsAffectedBySpell { get; }
}
