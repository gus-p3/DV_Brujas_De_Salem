using System;
using UnityEngine;

/// <summary>
/// Animaciones cuadro a cuadro de un aldeano (fotogramas exportados desde ArtSource/Villagers).
/// Las genera Tools > Aldeanos > Importar animaciones.
/// </summary>
[CreateAssetMenu(menuName = "Brujas de Salem/Animaciones de aldeano", fileName = "VillagerAnimationSet")]
public class VillagerAnimationSet : ScriptableObject
{
    [Serializable]
    public class Clip
    {
        public Sprite[] frames = Array.Empty<Sprite>();
        [Tooltip("Fotogramas por segundo.")]
        public float fps = 12f;
        [Tooltip("Si es falso, se queda en el último fotograma.")]
        public bool loop = true;

        public bool IsEmpty => frames == null || frames.Length == 0;
    }

    public Clip idle = new Clip { fps = 8f };
    public Clip walk = new Clip { fps = 12f };
    public Clip run = new Clip { fps = 14f };
    public Clip notice = new Clip { fps = 10f, loop = false };
    public Clip scream = new Clip { fps = 10f };
    public Clip blind = new Clip { fps = 6f };
    public Clip hypnotize = new Clip { fps = 5f };
    public Clip sleep = new Clip { fps = 2f };
    public Clip action = new Clip { fps = 8f, loop = false };

    [Header("Ajustes")]
    [Tooltip("Altura (u) de los iconos '!' y de hechizo sobre la cabeza.")]
    public float iconHeight = 2.15f;
}
