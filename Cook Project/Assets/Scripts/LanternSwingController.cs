using UnityEngine;

public class LanternSwingController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody stickParentRb;        // The RB moved by the player
    public CharacterJoint cj;              // The CharacterJoint
    public Rigidbody lanternRb;            // The lantern RB

    [Header("Swing Behavior")]
    public float torqueStrength = 4f;      // How strongly motion adds swing
    public float maxSwingAngle = 45f;      // Should match joint swing limit
    public float returnSpring = 2f;        // How strongly the lantern returns to down
    public float airDamping = 0.2f;        // Additional drag applied manually

    Vector3 lastStickVelocity;

    void Start()
    {
        if (stickParentRb == null)
            Debug.LogError("StickParent RB missing!");

        if (lanternRb == null)
            Debug.LogError("Lantern RB missing!");

        if (cj == null)
            Debug.LogError("CharacterJoint missing!");
    }

    void FixedUpdate()
    {
        // Get stick velocity
        Vector3 stickVel = stickParentRb.linearVelocity;
        Vector3 accel = (stickVel - lastStickVelocity) / Time.fixedDeltaTime;
        lastStickVelocity = stickVel;

        // No motion = no swing force added
        if (accel.sqrMagnitude > 0.0001f)
        {
            AddSwingForce(accel);
        }

        ApplyReturnToRest();
        ApplyDamping();
    }

    void AddSwingForce(Vector3 accel)
    {
        // Torque direction should be perpendicular to acceleration
        // so lantern sways naturally in 3D.
        Vector3 torqueDir = Vector3.Cross(accel, Vector3.up).normalized;

        // Compute current swing angle relative to "down"
        float angle = Vector3.Angle(transform.up, Vector3.up); // 0� = straight down

        // Scale torque so it weakens near the swing limit
        float limitRatio = 1f - Mathf.Clamp01(angle / maxSwingAngle);

        lanternRb.AddTorque(torqueDir * torqueStrength * limitRatio, ForceMode.Acceleration);
    }

    void ApplyReturnToRest()
    {
        // Calculate offset from straight down
        Vector3 down = Vector3.up; // Up in world = lantern down direction
        Vector3 lanternUp = transform.up;

        float angle = Vector3.Angle(lanternUp, down);

        if (angle < 1f) return;

        // Direction to rotate lantern back down
        Vector3 correctionAxis = Vector3.Cross(lanternUp, down).normalized;

        lanternRb.AddTorque(correctionAxis * returnSpring, ForceMode.Acceleration);
    }

    void ApplyDamping()
    {
        // CharacterJoint has weak damping, so we add our own
        lanternRb.angularVelocity *= (1f - airDamping * Time.fixedDeltaTime);
    }
}
