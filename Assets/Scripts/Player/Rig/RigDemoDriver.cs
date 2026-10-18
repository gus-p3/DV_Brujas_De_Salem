using UnityEngine;

/// <summary>
/// Conduce un WitchRigAnimator en modo demo para la escena de prueba de personajes:
/// reproduce en bucle un estado (idle, caminar, correr, salto, hechizo, daño, muerte, vuelo).
/// </summary>
[RequireComponent(typeof(WitchRigAnimator))]
public class RigDemoDriver : MonoBehaviour
{
    public enum Mode { Idle, Walk, Run, Jump, Cast, Hurt, Die, Fly, Showcase }

    [SerializeField] private Mode mode = Mode.Idle;
    [Tooltip("Para Walk/Run: ida y vuelta entre -range y +range en X (0 = en el sitio).")]
    [SerializeField] private float patrolRange = 0f;

    private WitchRigAnimator rig;
    private float t;
    private float startX;
    private float vyJump;
    private float heightJump;
    private bool cycleFlag;
    private bool airborne;
    private float baseY;

    private void Awake()
    {
        rig = GetComponent<WitchRigAnimator>();
        rig.demoMode = true;
        startX = transform.localPosition.x;
        baseY = transform.localPosition.y;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        t += dt;
        Mode m = mode == Mode.Showcase ? ShowcaseMode() : mode;

        rig.demoFlying = m == Mode.Fly;
        rig.demoGrounded = true;
        rig.demoVelocity = Vector2.zero;

        switch (m)
        {
            case Mode.Walk: Locomote(2.2f, dt); break;
            case Mode.Run: Locomote(6f, dt); break;
            case Mode.Jump: Jump(dt); break;
            case Mode.Cast: Every(1.4f, rig.TriggerCast); break;
            case Mode.Hurt: Every(1.3f, rig.TriggerHurt); break;
            case Mode.Die: Every(3.2f, () => { rig.ResetDeath(); rig.TriggerDie(); }); break;
            case Mode.Fly: rig.demoVelocity = new Vector2(3f * (rig.demoFacingRight ? 1f : -1f), 0.4f * Mathf.Sin(t * 2f)); break;
        }
    }

    private Mode ShowcaseMode()
    {
        // Recorrido de 16 s por todos los estados, útil para grabar o revisar transiciones
        float k = t % 16f;
        if (k < 3f) return Mode.Idle;
        if (k < 6f) return Mode.Walk;
        if (k < 8.5f) return Mode.Run;
        if (k < 11f) return Mode.Jump;
        if (k < 13f) return Mode.Cast;
        return Mode.Idle;
    }

    private void Locomote(float speed, float dt)
    {
        float dir = rig.demoFacingRight ? 1f : -1f;
        rig.demoVelocity = new Vector2(speed * dir, 0f);
        if (patrolRange <= 0f) return;

        Vector3 p = transform.localPosition;
        p.x += speed * dir * dt;
        if (p.x > startX + patrolRange) rig.demoFacingRight = false;
        else if (p.x < startX - patrolRange) rig.demoFacingRight = true;
        transform.localPosition = p;
    }

    private void Jump(float dt)
    {
        // Salto simulado con la gravedad aproximada del PlayerController (caída 1.5x más rápida)
        const float g = 30f;
        if (!airborne && (t % 1.6f) < dt)
        {
            airborne = true;
            vyJump = Mathf.Sqrt(2f * g * 2f);
        }
        if (airborne)
        {
            vyJump -= g * (vyJump < 0f ? 1.5f : 1f) * dt;
            heightJump += vyJump * dt;
            if (heightJump <= 0f)
            {
                heightJump = 0f;
                airborne = false;
            }
        }
        rig.demoGrounded = !airborne;
        rig.demoVelocity = new Vector2(0f, airborne ? vyJump : 0f);
        Vector3 lp = transform.localPosition;
        lp.y = baseY + heightJump;
        transform.localPosition = lp;
    }

    private void Every(float period, System.Action action)
    {
        bool phase = (t % period) < period * 0.5f;
        if (phase && !cycleFlag) action();
        cycleFlag = phase;
    }
}
