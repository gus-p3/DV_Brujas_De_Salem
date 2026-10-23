using UnityEngine;

/// <summary>
/// Parámetros del Animator del gato que PlayerController no conoce:
/// - IsSneaking: camina agazapado cuando está escondido (maizal o escondite).
/// - IsSitting: se sienta tras quedarse quieto un rato en el suelo.
/// Solo actúa en forma de gato; los parámetros que el Animator no tenga se ignoran.
/// </summary>
public class CatAnimationDriver : MonoBehaviour
{
    [Tooltip("Segundos quieto antes de sentarse.")]
    [SerializeField] private float sitAfter = 3f;

    private static readonly int SneakingHash = Animator.StringToHash("IsSneaking");
    private static readonly int SittingHash = Animator.StringToHash("IsSitting");

    private TransformationManager transformation;
    private PlayerController player;
    private Rigidbody2D body;
    private Animator animator;
    private RuntimeAnimatorController cachedController;
    private bool hasSneaking, hasSitting;
    private float stillTime;

    private void Awake()
    {
        transformation = GetComponent<TransformationManager>();
        player = GetComponent<PlayerController>();
        body = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (animator == null || transformation == null || !transformation.IsCatForm) return;
        RefreshParameters();

        bool still = player != null && player.IsGrounded && body != null && Mathf.Abs(body.linearVelocity.x) < 0.05f;
        stillTime = still ? stillTime + Time.deltaTime : 0f;

        if (hasSneaking) animator.SetBool(SneakingHash, transformation.IsInHideout);
        if (hasSitting) animator.SetBool(SittingHash, stillTime >= sitAfter);
    }

    private void RefreshParameters()
    {
        // El controlador cambia al transformarse: se vuelve a comprobar qué parámetros tiene
        if (animator.runtimeAnimatorController == cachedController) return;
        cachedController = animator.runtimeAnimatorController;
        hasSneaking = hasSitting = false;
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.nameHash == SneakingHash) hasSneaking = true;
            else if (p.nameHash == SittingHash) hasSitting = true;
        }
    }
}
