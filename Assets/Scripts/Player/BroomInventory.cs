using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona el inventario de las 3 partes de la escoba (rama, paja y cuerda).
/// Persiste en memoria durante la partida y se reinicia al recargar la escena.
/// Al completar las 3 piezas, desbloquea CanFly en PlayerController y muestra aviso.
/// </summary>
public class BroomInventory : MonoBehaviour
{
    public static BroomInventory Instance { get; private set; }

    [Header("Efectos")]
    [Tooltip("Sistema de partículas al completar la escoba.")]
    [SerializeField] private ParticleSystem completionParticles;

    private readonly HashSet<BroomPart> collectedParts = new HashSet<BroomPart>();

    /// <summary>
    /// Se dispara cuando se recolecta una nueva parte. Pasa la parte y el total acumulado.
    /// </summary>
    public event Action<BroomPart, int> OnPartCollected;

    /// <summary>
    /// Se dispara una sola vez al reunir las 3 partes requeridas.
    /// </summary>
    public event Action OnBroomCompleted;

    /// <summary>Indica si las 3 partes de la escoba han sido reunidas.</summary>
    public bool IsComplete => collectedParts.Count >= 3;

    /// <summary>Cantidad actual de partes recolectadas (0 a 3).</summary>
    public int CollectedCount => collectedParts.Count;

    public bool HasPart(BroomPart part) => collectedParts.Contains(part);

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

        ResetInventory();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Registra la recolección de una pieza de escoba.
    /// </summary>
    public void Collect(BroomPart part)
    {
        if (collectedParts.Contains(part)) return;

        collectedParts.Add(part);
        OnPartCollected?.Invoke(part, collectedParts.Count);

        if (IsComplete)
        {
            HandleBroomCompleted();
        }
    }

    private void HandleBroomCompleted()
    {
        PlayerController pc = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            pc.CanFly = true;
        }

        HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
        if (hud != null)
        {
            hud.ShowMessage("¡Escoba lista! Pulsa F para volar", 3.0f);
            hud.SetFlightSliderVisible(true);
        }

        if (completionParticles != null)
        {
            completionParticles.Play();
        }

        OnBroomCompleted?.Invoke();
    }

    /// <summary>
    /// Reinicia las partes recolectadas (llamado al iniciar/reiniciar nivel).
    /// </summary>
    public void ResetInventory()
    {
        collectedParts.Clear();
    }
}
