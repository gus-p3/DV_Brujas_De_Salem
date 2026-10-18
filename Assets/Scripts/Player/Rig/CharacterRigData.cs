using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Definición de un rig por piezas leída desde JSON (ArtSource/&lt;Personaje&gt;/*_rig.json).
/// Espacio de ensamblaje en px de la hoja: x a la derecha, y hacia abajo, origen en el suelo entre los pies.
/// </summary>
[Serializable]
public class CharacterRigData
{
    public string character;
    public string spriteDir = "parts";
    public float ppu = 100f;
    public List<RigSpriteDef> extraSprites = new List<RigSpriteDef>();
    public List<RigBoneDef> bones = new List<RigBoneDef>();

    public static CharacterRigData FromJson(string json)
    {
        CharacterRigData data = JsonUtility.FromJson<CharacterRigData>(json);
        foreach (RigBoneDef b in data.bones)
        {
            if (b.scale <= 0f) b.scale = 1f;
            if (string.IsNullOrEmpty(b.parent)) b.parent = null;
        }
        return data;
    }

    public RigBoneDef Find(string name)
    {
        return bones.Find(b => b.name == name);
    }

    /// <summary>Pivote en px (desde arriba a la izquierda) de un sprite, sea de hueso o extra.</summary>
    public bool TryGetPivot(string spriteName, out Vector2 pivotPx)
    {
        foreach (RigBoneDef b in bones)
        {
            if (b.sprite == spriteName && b.pivot != null && b.pivot.Length == 2)
            {
                pivotPx = new Vector2(b.pivot[0], b.pivot[1]);
                return true;
            }
        }
        foreach (RigSpriteDef s in extraSprites)
        {
            if (s.name == spriteName && s.pivot != null && s.pivot.Length == 2)
            {
                pivotPx = new Vector2(s.pivot[0], s.pivot[1]);
                return true;
            }
        }
        pivotPx = Vector2.zero;
        return false;
    }
}

[Serializable]
public class RigBoneDef
{
    public string name;
    public string parent;
    public float[] pos;
    public string sprite;
    public float[] pivot;
    public float[] tip;
    public int order;
    public float scale = 1f;
    public float rot;
}

[Serializable]
public class RigSpriteDef
{
    public string name;
    public float[] pivot;
}
