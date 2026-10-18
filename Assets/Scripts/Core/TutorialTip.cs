using System;
using UnityEngine;

/// <summary>
/// Definición de un consejo o instrucción contextual de tutorial.
/// </summary>
[Serializable]
public class TutorialTip
{
    [Tooltip("Identificador único del tip (p. ej. 'tip_corazones').")]
    public string id;

    [Tooltip("Título del cuadro del tutorial.")]
    public string title;

    [Tooltip("Cuerpo del mensaje. Puede contener marcadores como {Transformar}, {Volar}, etc.")]
    [TextArea(3, 6)]
    public string body;

    [Tooltip("Icono opcional que acompaña al texto en el cuadro.")]
    public Sprite icon;

    [Tooltip("Obsoleto: los tips ya no congelan el juego (se muestran como banner con cierre automático). Se conserva por compatibilidad con datos serializados.")]
    public bool pauseGame = true;

    public TutorialTip() { }

    public TutorialTip(string id, string title, string body, Sprite icon = null, bool pauseGame = true)
    {
        this.id = id;
        this.title = title;
        this.body = body;
        this.icon = icon;
        this.pauseGame = pauseGame;
    }
}
