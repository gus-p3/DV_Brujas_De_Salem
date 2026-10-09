using UnityEngine;

/// <summary>
/// Contrato de detección entre la bruja (y su forma de gato) y los enemigos.
/// Cualquier objeto que pueda ser visto por un guardia debe implementar esta interfaz.
/// IMPORTANTE: es el contrato con el resto del equipo, no se debe cambiar su firma.
/// </summary>
public interface IDetectable
{
    /// <summary>Indica si el objeto puede ser detectado en este momento.</summary>
    bool IsDetectable { get; }

    /// <summary>Punto del mundo que usan los enemigos para medir distancias al objetivo.</summary>
    Transform DetectionPoint { get; }
}
