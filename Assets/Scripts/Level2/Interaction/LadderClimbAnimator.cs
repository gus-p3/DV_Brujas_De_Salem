using UnityEngine;

namespace Level2
{
    /// <summary>
    /// Pose de escalada de la bruja en las escaleras del Nivel 2, sin modificar WitchRigAnimator (compartido con el
    /// Nivel 1). Mientras LadderClimber está activo pone el rig en modo demo "apoyado" (sin pose de caída) y, después
    /// de que el rig aplica su pose, sobrescribe brazos y piernas con un ciclo alterno mano-pie cuya fase avanza con
    /// la velocidad vertical: quieta en la escalera se queda agarrada, al subir o bajar trepa.
    /// </summary>
    [DefaultExecutionOrder(100)] // después del LateUpdate del rig
    public class LadderClimbAnimator : MonoBehaviour
    {
        [SerializeField] private float rungsPerUnit = 1.3f;   // ciclos completos (2 peldaños) por unidad recorrida
        [SerializeField] private float armReach = 95f;        // brazos al frente, al peldaño (más de ~120 los esconde tras la capucha)
        [SerializeField] private float armSwing = 20f;
        [SerializeField] private float legLift = 42f;

        private LadderClimber climber;
        private WitchRigAnimator rig;
        private Rigidbody2D body;
        private Transform armUF, armLF, armUB, armLB, legUF, legLF, legUB, legLB;
        private bool driving;
        private bool savedDemoMode;
        private float phase;

        public bool IsDriving => driving;

        private void Awake()
        {
            climber = GetComponent<LadderClimber>();
            body = GetComponent<Rigidbody2D>();
            rig = GetComponentInChildren<WitchRigAnimator>(true);
            if (rig == null) return;
            armUF = Bone("arm_upper_front"); armLF = Bone("arm_lower_front");
            armUB = Bone("arm_upper_back"); armLB = Bone("arm_lower_back");
            legUF = Bone("leg_upper_front"); legLF = Bone("leg_lower_front");
            legUB = Bone("leg_upper_back"); legLB = Bone("leg_lower_back");
        }

        private Transform Bone(string boneName)
        {
            foreach (Transform t in rig.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == boneName) return t;
            }
            return null;
        }

        private void OnDisable() => Release();

        private void LateUpdate()
        {
            if (climber == null) climber = GetComponent<LadderClimber>();
            if (rig == null || climber == null || armUF == null) return;

            if (!climber.IsClimbing || !rig.isActiveAndEnabled)
            {
                Release();
                return;
            }

            if (!driving)
            {
                driving = true;
                savedDemoMode = rig.demoMode;
                rig.demoMode = true;
            }
            // Apoyada y quieta para el rig: sin pose de salto ni de caída (la escalada la pone este componente)
            rig.demoGrounded = true;
            rig.demoFlying = false;
            rig.demoVelocity = Vector2.zero;
            rig.demoFacingRight = rig.FacingRight;

            float vy = body != null ? body.linearVelocity.y : 0f;
            phase += Mathf.Abs(vy) * rungsPerUnit * Mathf.PI * Time.deltaTime;
            float s = Mathf.Sin(phase);

            // Mano y pie contrarios suben juntos, como al trepar
            Rotate(armUF, armReach + armSwing * s);
            Rotate(armLF, 20f - 15f * s);
            Rotate(armUB, armReach - armSwing * s);
            Rotate(armLB, 20f + 15f * s);
            Rotate(legUF, legLift * Mathf.Max(0f, -s) + 8f);
            Rotate(legLF, -legLift * 1.6f * Mathf.Max(0f, -s) - 6f);
            Rotate(legUB, legLift * Mathf.Max(0f, s) + 8f);
            Rotate(legLB, -legLift * 1.6f * Mathf.Max(0f, s) - 6f);
        }

        private static void Rotate(Transform bone, float degrees)
        {
            if (bone != null) bone.localRotation *= Quaternion.Euler(0f, 0f, degrees);
        }

        private void Release()
        {
            if (!driving) return;
            driving = false;
            if (rig != null) rig.demoMode = savedDemoMode;
        }
    }
}
