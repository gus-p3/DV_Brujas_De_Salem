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

    [Header("Cat Form")]
    public RuntimeAnimatorController catController;
    public Vector2 catColliderSize = new Vector2(0.8f, 0.5f);
    public Vector2 catColliderOffset = new Vector2(0f, 0.306f);

    public bool IsCatForm { get; private set; }
    public bool IsInHideout { get; set; }

    public bool CanCollect => true;
    public bool CanCastSpells => !IsCatForm;

    public event Action<bool> OnTransformed;

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
    }

    private void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.T) && cooldownTimer <= 0f)
        {
            if (IsCatForm) TryTransformToHuman();
            else TransformToCat();
        }
    }

    public void TransformToCat()
    {
        EnsureComponents();
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
        
        if (pc != null && pc.IsFlying) pc.ToggleFlight();
        
        if (moveSpeedField != null && pc != null)
        {
            moveSpeedField.SetValue(pc, originalMoveSpeed * catSpeedMultiplier);
        }
        
        OnTransformed?.Invoke(true);
    }

    public void TryTransformToHuman()
    {
        EnsureComponents();
        // Verificar espacio libre para la altura completa de la forma humana
        Vector2 checkCenter = (Vector2)transform.position + humanColliderOffset;
        Vector2 checkSize = humanColliderSize * 0.95f;
        
        Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, checkSize, 0f);
        bool blocked = false;
        string blockerName = "";

        foreach (var col in hits)
        {
            if (col == null || col.transform.root == transform.root) continue;

            // Bloqueo si hay obstáculo sólido
            if (!col.isTrigger)
            {
                blocked = true;
                blockerName = col.gameObject.name;
                break;
            }

            // Bloqueo si se encuentra dentro del área de un pasaje exclusivo para gatos
            if (col.GetComponent<CatOnlyPassage>() != null || col.GetComponentInParent<CatOnlyPassage>() != null)
            {
                blocked = true;
                blockerName = col.gameObject.name;
                break;
            }
        }
        
        if (blocked)
        {
            Debug.Log($"[TransformationManager] No hay espacio para transformarse en humana. Bloqueado por: {blockerName}");
            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowMessage("Eres muy grande para pasar", 2.0f);
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
