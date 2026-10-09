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
    public Vector2 humanColliderSize = new Vector2(0.6f, 1.8f);
    public Vector2 humanColliderOffset = new Vector2(0f, 0.9f);

    [Header("Cat Form")]
    public RuntimeAnimatorController catController;
    public Vector2 catColliderSize = new Vector2(0.8f, 0.5f);
    public Vector2 catColliderOffset = new Vector2(0f, 0.25f);

    public bool IsCatForm { get; private set; }
    public bool IsInHideout { get; set; }

    public bool CanCollect => !IsCatForm;
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
        pc = GetComponent<PlayerController>();
        boxCol = GetComponent<BoxCollider2D>();
        anim = GetComponentInChildren<Animator>();
        
        // Guardar la velocidad original por reflexión para no alterar la firma pública
        moveSpeedField = typeof(PlayerController).GetField("moveSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
        if (moveSpeedField != null)
        {
            originalMoveSpeed = (float)moveSpeedField.GetValue(pc);
        }
    }

    private void Start()
    {
        if (humanController == null && anim != null)
        {
            humanController = anim.runtimeAnimatorController;
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

    private void TransformToCat()
    {
        IsCatForm = true;
        cooldownTimer = transformCooldown;
        PlaySmoke();
        
        if (catController != null && anim != null)
        {
            anim.runtimeAnimatorController = catController;
            anim.Rebind();
            anim.Update(0f);
        }
        
        boxCol.size = catColliderSize;
        boxCol.offset = catColliderOffset;
        
        if (pc.IsFlying) pc.ToggleFlight();
        
        if (moveSpeedField != null)
        {
            moveSpeedField.SetValue(pc, originalMoveSpeed * catSpeedMultiplier);
        }
        
        OnTransformed?.Invoke(true);
    }

    private void TryTransformToHuman()
    {
        // Verificar espacio libre sobre la cabeza del gato (entre y = 0.6 y 1.6)
        Vector2 checkCenter = (Vector2)transform.position + new Vector2(0f, 1.1f);
        Vector2 checkSize = new Vector2(humanColliderSize.x * 0.8f, 0.9f);
        
        LayerMask mask = obstacleMask.value != 0 ? obstacleMask : (LayerMask)(1 << LayerMask.NameToLayer("Ground"));
        Collider2D hit = Physics2D.OverlapBox(checkCenter, checkSize, 0f, mask);
        
        if (hit != null && !hit.isTrigger && hit.transform.root != transform.root)
        {
            Debug.Log($"No hay espacio para transformarse en humana. Obstáculo: {hit.gameObject.name}");
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
        
        boxCol.size = humanColliderSize;
        boxCol.offset = humanColliderOffset;

        if (moveSpeedField != null)
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
            // Opcional: reproducir sonido
        }
    }
}
