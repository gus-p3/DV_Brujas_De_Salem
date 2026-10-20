using System;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(PlayerController), typeof(BoxCollider2D))]
public class TransformationManager : MonoBehaviour
{
    [Header("Settings")]
    public float transformCooldown = 0.75f;
    public ParticleSystem smokeEffect;
    public float catSpeedMultiplier = 1.3f;
    public LayerMask obstacleMask;

    [Header("Human Form")]
    public RuntimeAnimatorController humanController;
    public Vector2 humanColliderSize = new Vector2(0.6f, 1.6f);
    public Vector2 humanColliderOffset = new Vector2(0f, 0.856f);
    [Tooltip("Escala uniforme del objeto Visual en forma humana (referencia de tamaño del juego).")]
    public float humanVisualScale = 0.45f;
    [Tooltip("Desplazamiento vertical local del Visual en forma humana (hunde los pies en el pasto).")]
    public float humanVisualOffsetY = -0.10f;

    [Header("Cat Form")]
    public RuntimeAnimatorController catController;
    public Vector2 catColliderSize = new Vector2(0.8f, 0.5f);
    public Vector2 catColliderOffset = new Vector2(0f, 0.306f);
    [Tooltip("Escala del Visual en forma de gato (~40-45% de la altura de la bruja).")]
    public float catVisualScale = 0.34f;
    [Tooltip("Desplazamiento vertical local del Visual en forma de gato.")]
    public float catVisualOffsetY = -0.09f;

    [Header("Límite de la forma de gato")]
    [Tooltip("Segundos que dura la forma de gato con la energía llena.")]
    public float catMaxDuration = 8f;
    [Tooltip("Energía (0..1) que se recupera por segundo en forma humana.")]
    public float catRechargePerSecond = 0.15f;
    [Tooltip("Energía mínima (0..1) necesaria para convertirse en gato.")]
    public float catMinEnergyToTransform = 0.25f;
    [Tooltip("Por debajo de esta energía (0..1) se avisa de que el hechizo se acaba.")]
    public float catLowEnergyWarning = 0.3f;

    [Header("Bloqueo de Transformación")]
    [Tooltip("Mensaje mostrado cuando no hay espacio para volver a forma humana.")]
    public string blockedMessage = "Espacio muy estrecho para transformarse";

    public bool IsCatForm { get; private set; }
    public bool IsInHideout { get; set; }

    public bool CanCollect => true;
    public bool CanCastSpells => !IsCatForm;

    public event Action<bool> OnTransformed;

    /// <summary>Energía de la forma de gato (0..1). Se dispara cada vez que cambia.</summary>
    public event Action<float> OnCatEnergyChanged;
    public float CatEnergy01 => catEnergy;

    private float catEnergy = 1f;
    private bool lowEnergyWarned;
    private bool exhausted;

    private float cooldownTimer;
    private PlayerController pc;
    private BoxCollider2D boxCol;
    private Animator anim;
    
    private float originalMoveSpeed;
    private FieldInfo moveSpeedField;

    private void Awake()
    {
        EnsureComponents();
    }

    private void EnsureComponents()
    {
        if (pc == null) pc = GetComponent<PlayerController>();
        if (boxCol == null) boxCol = GetComponent<BoxCollider2D>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
        
        // Guardar la velocidad original por reflexión para no alterar la firma pública
        if (moveSpeedField == null)
        {
            moveSpeedField = typeof(PlayerController).GetField("moveSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
            if (moveSpeedField != null && pc != null)
            {
                originalMoveSpeed = (float)moveSpeedField.GetValue(pc);
            }
        }
    }

    private void Start()
    {
        EnsureComponents();
        if (humanController == null && anim != null)
        {
            humanController = anim.runtimeAnimatorController;
        }
        else if (anim != null && anim.runtimeAnimatorController == null && humanController != null)
        {
            anim.runtimeAnimatorController = humanController;
        }
        ApplyVisualForm(IsCatForm);
    }

    /// <summary>
    /// Ajusta escala y desplazamiento del objeto Visual según la forma activa,
    /// para que el gato mida ~42% de la bruja y ambos pisen el suelo.
    /// </summary>
    private void ApplyVisualForm(bool cat)
    {
        if (anim == null) return;
        Transform v = anim.transform;
        if (v == transform) return;
        float s = cat ? catVisualScale : humanVisualScale;
        v.localScale = new Vector3(s, s, 1f);
        Vector3 lp = v.localPosition;
        lp.y = cat ? catVisualOffsetY : humanVisualOffsetY;
        v.localPosition = lp;
    }

    /// <summary>
    /// Comprueba si hay espacio para la altura completa de la bruja.
    /// Ignora triggers (salvo pasajes de gato) y plataformas de un solo sentido.
    /// </summary>
    public bool HasRoomForHuman(out string blockerName)
    {
        blockerName = "";
        float bottom = boxCol != null ? (boxCol.offset.y - boxCol.size.y * 0.5f) : 0f;
        Vector2 basePos = (Vector2)transform.position;
        float humanBottom = humanColliderOffset.y - humanColliderSize.y * 0.5f;
        Vector2 checkCenter = basePos + new Vector2(humanColliderOffset.x, humanColliderOffset.y + (bottom - humanBottom) + 0.04f);
        Vector2 checkSize = new Vector2(humanColliderSize.x * 0.9f, humanColliderSize.y - 0.08f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, checkSize, 0f);
        foreach (var col in hits)
        {
            if (col == null || col.transform.root == transform.root) continue;
            if (col.GetComponent<CatOnlyPassage>() != null || col.GetComponentInParent<CatOnlyPassage>() != null)
            {
                blockerName = col.gameObject.name;
                return false;
            }
            if (col.isTrigger) continue;
            if (col.usedByEffector && col.GetComponent<PlatformEffector2D>() != null) continue;
            if (col.attachedRigidbody != null && col.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic) continue;
            blockerName = col.gameObject.name;
            return false;
        }

        // Rayos hacia arriba (izquierda, centro, derecha) como segunda verificación de techo
        float headroom = humanColliderSize.y - (boxCol != null ? boxCol.size.y : 0.5f);
        float startY = basePos.y + (boxCol != null ? boxCol.offset.y + boxCol.size.y * 0.5f : 0.5f) - 0.02f;
        float halfW = humanColliderSize.x * 0.45f;
        for (int i = -1; i <= 1; i++)
        {
            Vector2 origin = new Vector2(basePos.x + i * halfW, startY);
            RaycastHit2D[] rays = Physics2D.RaycastAll(origin, Vector2.up, headroom + 0.05f);
            foreach (var h in rays)
            {
                if (h.collider == null || h.collider.isTrigger || h.collider.transform.root == transform.root) continue;
                if (h.collider.usedByEffector && h.collider.GetComponent<PlatformEffector2D>() != null) continue;
                if (h.collider.attachedRigidbody != null && h.collider.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic) continue;
                blockerName = h.collider.gameObject.name;
                return false;
            }
        }
        return true;
    }

    private void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        UpdateCatEnergy(Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.T) && cooldownTimer <= 0f)
        {
            if (IsCatForm) TryTransformToHuman();
            else TransformToCat();
        }
    }

    /// <summary>Consume la energía en forma de gato y la recupera en forma humana.</summary>
    private void UpdateCatEnergy(float dt)
    {
        float before = catEnergy;
        if (IsCatForm)
        {
            catEnergy = Mathf.Max(0f, catEnergy - dt / Mathf.Max(0.1f, catMaxDuration));
            if (!lowEnergyWarned && catEnergy <= catLowEnergyWarning)
            {
                lowEnergyWarned = true;
                ShowHudMessage("¡La forma de gato se agota!", 1.6f);
            }
            if (catEnergy <= 0f)
            {
                // Sin energía: vuelve a ser bruja en cuanto haya espacio (en un túnel espera a salir)
                if (HasRoomForHuman(out _))
                {
                    exhausted = false;
                    ForceHuman();
                    ShowHudMessage("El hechizo felino se agotó", 1.8f);
                }
                else if (!exhausted)
                {
                    exhausted = true;
                    ShowHudMessage("¡Sin fuerzas! Sal del pasaje para volver a ser bruja", 2.2f);
                }
            }
        }
        else
        {
            catEnergy = Mathf.Min(1f, catEnergy + catRechargePerSecond * dt);
            if (catEnergy > catLowEnergyWarning) lowEnergyWarned = false;
        }
        if (!Mathf.Approximately(before, catEnergy)) OnCatEnergyChanged?.Invoke(catEnergy);
    }

    private void ForceHuman()
    {
        cooldownTimer = 0f;
        TryTransformToHuman();
    }

    private static void ShowHudMessage(string message, float seconds)
    {
        HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
        if (hud != null) hud.ShowMessage(message, seconds);
    }

    public void TransformToCat()
    {
        EnsureComponents();
        if (catEnergy < catMinEnergyToTransform)
        {
            cooldownTimer = 0.35f;
            ShowHudMessage("Aún no tienes fuerzas para ser gato", 1.6f);
            return;
        }
        IsCatForm = true;
        cooldownTimer = transformCooldown;
        PlaySmoke();
        
        if (catController != null && anim != null)
        {
            anim.runtimeAnimatorController = catController;
            anim.Rebind();
            anim.Update(0f);
        }
        
        if (boxCol != null)
        {
            boxCol.size = catColliderSize;
            boxCol.offset = catColliderOffset;
        }
        ApplyVisualForm(true);
        
        if (pc != null && pc.IsFlying) pc.ToggleFlight();
        
        if (moveSpeedField != null && pc != null)
        {
            moveSpeedField.SetValue(pc, originalMoveSpeed * catSpeedMultiplier);
        }
        
        OnTransformed?.Invoke(true);

        // La primera vez se explica el límite de tiempo
        if (TutorialManager.Instance != null && !TutorialManager.Instance.HasSeen("tip_gato_tiempo"))
        {
            TutorialManager.Instance.ShowTipById("tip_gato_tiempo");
        }
    }

    public void TryTransformToHuman()
    {
        EnsureComponents();
        // Verificar espacio libre (OverlapBox + Raycasts hacia arriba) para la altura completa de la bruja
        if (!HasRoomForHuman(out string blockerName))
        {
            Debug.Log($"[TransformationManager] No hay espacio para transformarse en humana. Bloqueado por: {blockerName}");
            cooldownTimer = 0.35f;
            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage(blockedMessage, 1.6f);
            }
            return;
        }

        IsCatForm = false;
        cooldownTimer = transformCooldown;
        PlaySmoke();
        
        if (humanController != null && anim != null)
        {
            anim.runtimeAnimatorController = humanController;
            anim.Rebind();
            anim.Update(0f);
        }
        
        if (boxCol != null)
        {
            boxCol.size = humanColliderSize;
            boxCol.offset = humanColliderOffset;
        }
        ApplyVisualForm(false);

        if (moveSpeedField != null && pc != null)
        {
            moveSpeedField.SetValue(pc, originalMoveSpeed);
        }

        OnTransformed?.Invoke(false);
    }

    /// <summary>
    /// Fuerza el retorno inmediato a forma humana (utilizado al reaparecer en checkpoint).
    /// </summary>
    public void ResetToHumanForm()
    {
        EnsureComponents();
        // Al reaparecer la energía felina vuelve a estar llena
        catEnergy = 1f;
        exhausted = false;
        lowEnergyWarned = false;
        OnCatEnergyChanged?.Invoke(catEnergy);
        if (!IsCatForm) return;

        IsCatForm = false;
        cooldownTimer = 0f;

        if (humanController != null && anim != null)
        {
            anim.runtimeAnimatorController = humanController;
            anim.Rebind();
            anim.Update(0f);
        }

        if (boxCol != null)
        {
            boxCol.size = humanColliderSize;
            boxCol.offset = humanColliderOffset;
        }
        ApplyVisualForm(false);

        if (moveSpeedField != null && pc != null)
        {
            moveSpeedField.SetValue(pc, originalMoveSpeed);
        }

        OnTransformed?.Invoke(false);
    }

    private void PlaySmoke()
    {
        if (smokeEffect != null) smokeEffect.Play();
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayTransformSound();
        }
    }
}
