using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador del ataque frenético de la multitud de aldeanos.
/// Cuando un aldeano grita, recluta hasta 5 aldeanos en un radio de 12 u
/// para perseguir a la bruja durante 8 s. Aplica daño de 1 corazón al contacto
/// respetando invulnerabilidad, y finaliza por tiempo, distancia (>20 u)
/// o si la bruja se camufla (gato / escondite).
/// </summary>
public class FrenzyController : MonoBehaviour
{
    public static FrenzyController Instance { get; private set; }

    [Header("Configuración del Frenesí")]
    [Tooltip("Máximo número de aldeanos que se unen a la persecución.")]
    [SerializeField] private int maxFrenzyVillagers = 5;
    [Tooltip("Radio para reclutar aldeanos cercanos (u).")]
    [SerializeField] private float recruitmentRadius = 12f;
    [Tooltip("Duración máxima del frenesí (s).")]
    [SerializeField] private float frenzyDuration = 8f;
    [Tooltip("Distancia de escape para que los aldeanos pierdan a la bruja (u).")]
    [SerializeField] private float escapeDistance = 20f;
    [Tooltip("Distancia mínima para golpear a la bruja y causar daño (u).")]
    [SerializeField] private float attackContactDistance = 1.0f;

    [Header("Efectos")]
    [Tooltip("Sonido de multitud enfurecida.")]
    [SerializeField] private AudioClip mobSound;

    /// <summary>Se dispara al comenzar el ataque frenético.</summary>
    public static event Action OnFrenzyStarted;
    /// <summary>Se dispara al finalizar el ataque frenético.</summary>
    public static event Action OnFrenzyEnded;

    private readonly List<VillagerAI> activeFrenzyVillagers = new List<VillagerAI>();
    private Coroutine frenzyCoroutine;
    private Transform targetWitch;
    private IDetectable witchDetectable;
    private bool isFrenzyActive = false;

    public bool IsFrenzyActive => isFrenzyActive;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    private void Start()
    {
        FindWitch();
    }

    private void FindWitch()
    {
        PlayerController pc = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            targetWitch = pc.transform;
            witchDetectable = pc.GetComponent<IDetectable>();
        }
    }

    private void Update()
    {
        if (!isFrenzyActive) return;

        if (targetWitch == null)
        {
            FindWitch();
            if (targetWitch == null)
            {
                StopFrenzy();
                return;
            }
        }

        // Condición de finalización 2: La bruja se transformó en gato o se escondió en maizal
        if (witchDetectable != null && !witchDetectable.IsDetectable)
        {
            StopFrenzy();
            return;
        }

        // Comprobar contacto y daño con la bruja
        CheckVillagerDamage();
    }

    private void CheckVillagerDamage()
    {
        if (targetWitch == null || GameManager.Instance == null) return;

        for (int i = activeFrenzyVillagers.Count - 1; i >= 0; i--)
        {
            VillagerAI v = activeFrenzyVillagers[i];
            if (v == null)
            {
                activeFrenzyVillagers.RemoveAt(i);
                continue;
            }

            // Si el aldeano fue afectado por un hechizo, queda fuera del frenesí
            if (v.IsAffectedBySpell)
            {
                activeFrenzyVillagers.RemoveAt(i);
                continue;
            }

            float dist = Vector2.Distance(v.transform.position, targetWitch.position);

            // Condición de finalización 3: Distancia mayor a 20 u
            if (dist > escapeDistance)
            {
                v.EndFrenzy();
                activeFrenzyVillagers.RemoveAt(i);
                continue;
            }

            // Contacto de ataque: 1 corazón respetando invulnerabilidad
            if (dist <= attackContactDistance)
            {
                GameManager.Instance.LoseHeart(1, "Aldeano");
                // Breve retroceso o fin del ataque tras impactar
                v.EndFrenzy();
                activeFrenzyVillagers.RemoveAt(i);
            }
        }

        if (activeFrenzyVillagers.Count == 0)
        {
            StopFrenzy();
        }
    }

    /// <summary>
    /// Activa el ataque frenético reclutando aldeanos en el radio especificado.
    /// </summary>
    public void TriggerFrenzy(Transform witch)
    {
        targetWitch = witch;
        if (witch != null) witchDetectable = witch.GetComponent<IDetectable>();

        if (isFrenzyActive)
        {
            // Renovar tiempo si ya estaba activo
            if (frenzyCoroutine != null) StopCoroutine(frenzyCoroutine);
            frenzyCoroutine = StartCoroutine(FrenzyTimerRoutine());
            return;
        }

        activeFrenzyVillagers.Clear();
        VillagerAI[] allVillagers = UnityEngine.Object.FindObjectsByType<VillagerAI>(FindObjectsSortMode.None);

        int count = 0;
        foreach (VillagerAI v in allVillagers)
        {
            if (v == null || v.IsAffectedBySpell || v.CurrentState == VillagerState.Disabled) continue;

            float dist = Vector2.Distance(v.transform.position, targetWitch.position);
            if (dist <= recruitmentRadius)
            {
                v.EnterFrenzy();
                activeFrenzyVillagers.Add(v);
                count++;
                if (count >= maxFrenzyVillagers) break;
            }
        }

        if (activeFrenzyVillagers.Count > 0)
        {
            isFrenzyActive = true;
            OnFrenzyStarted?.Invoke();

            if (AudioManager.Instance != null && mobSound != null)
            {
                AudioManager.Instance.PlaySFX(mobSound);
            }

            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.SetFrenzyVisuals(true);
            }

            if (frenzyCoroutine != null) StopCoroutine(frenzyCoroutine);
            frenzyCoroutine = StartCoroutine(FrenzyTimerRoutine());
        }
    }

    private IEnumerator FrenzyTimerRoutine()
    {
        yield return new WaitForSeconds(frenzyDuration);
        StopFrenzy();
    }

    public void StopFrenzy()
    {
        if (!isFrenzyActive) return;
        isFrenzyActive = false;

        if (frenzyCoroutine != null)
        {
            StopCoroutine(frenzyCoroutine);
            frenzyCoroutine = null;
        }

        foreach (VillagerAI v in activeFrenzyVillagers)
        {
            if (v != null) v.EndFrenzy();
        }
        activeFrenzyVillagers.Clear();

        HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
        if (hud != null)
        {
            hud.SetFrenzyVisuals(false);
        }

        OnFrenzyEnded?.Invoke();
    }
}
