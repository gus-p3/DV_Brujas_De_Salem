using System;
using UnityEngine;

/// <summary>
/// Gestor de salud del jugador. Proporciona una interfaz unificada para recibir daño (Lesionar),
/// consultar corazones y activar eventos de muerte y derrota (GameOverPanel).
/// Se sincroniza directamente con GameManager.
/// </summary>
public class HealthManager : MonoBehaviour
{
    private static HealthManager instance;
    public static HealthManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = UnityEngine.Object.FindFirstObjectByType<HealthManager>();
                if (instance == null && Application.isPlaying)
                {
                    GameObject go = new GameObject("HealthManager");
                    instance = go.AddComponent<HealthManager>();
                }
            }
            return instance;
        }
    }

    public int CurrentHearts => GameManager.Instance != null ? GameManager.Instance.CurrentHearts : 0;
    public int MaxHearts => GameManager.Instance != null ? GameManager.Instance.MaxHearts : 3;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Aplica daño reduciendo corazones. Si la salud baja a 0, dispara el evento de muerte y GameOverPanel.
    /// </summary>
    public bool Lesionar(int amount = 1, string source = "Daño")
    {
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.LoseHeart(amount, source);
        }
        return false;
    }

    public bool Curar(int amount = 1)
    {
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.GainHeart(amount);
        }
        return false;
    }
}
