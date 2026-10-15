using System.Collections.Generic;

/// <summary>
/// Tabla central y única de mapeo de controles para textos, tutoriales y avisos en pantalla.
/// Permite resolver plantillas con marcadores como {Transformar}, {Volar}, etc., garantizando
/// coherencia y facilitando la posterior adaptación a controles táctiles o gamepad.
/// </summary>
public static class ControlsMap
{
    public const string Mover = "A / D o ← / →";
    public const string Saltar = "Espacio";
    public const string Transformar = "T";
    public const string Volar = "F";
    public const string VolarSubirBajar = "W / S o ↑ / ↓";
    public const string ElegirPocion = "1, 2 o 3";
    public const string LanzarPocion = "E o clic izquierdo";
    public const string Continuar = "Enter o clic";
    public const string Pausa = "Esc";

    private static readonly Dictionary<string, string> Replacements = new Dictionary<string, string>
    {
        { "{Mover}", Mover },
        { "{Saltar}", Saltar },
        { "{Transformar}", Transformar },
        { "{Volar}", Volar },
        { "{VolarSubirBajar}", VolarSubirBajar },
        { "{ElegirPoción}", ElegirPocion },
        { "{ElegirPocion}", ElegirPocion },
        { "{LanzarPoción}", LanzarPocion },
        { "{LanzarPocion}", LanzarPocion },
        { "{Continuar}", Continuar },
        { "{Pausa}", Pausa }
    };

    /// <summary>
    /// Reemplaza todos los marcadores entre llaves {Accion} por su cadena de control asignada.
    /// </summary>
    public static string Resolve(string plantilla)
    {
        if (string.IsNullOrEmpty(plantilla)) return string.Empty;

        string result = plantilla;
        foreach (var pair in Replacements)
        {
            result = result.Replace(pair.Key, pair.Value);
        }
        return result;
    }
}
