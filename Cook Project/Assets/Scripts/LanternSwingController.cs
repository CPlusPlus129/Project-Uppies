using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class LanternSwingController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody lanternRb;    // Lantern Rigidbody
    public Transform player;       // Player transform
    public HingeJoint hinge;       // Hinge connecting lantern to stick

    [Header("Swing Settings")]
    public Vector3 hingeAxis = Vector3.right;   // Local hinge axis
    public float swingForceMultiplier = 20f;    // Player movement torque
    public float rotationForceMultiplier = 10f; // Player rotation torque
    public float spring = 50f;                  // Hinge spring
    public float damper = 5f;                   // Hinge damper
    public float maxSwingAngle = 60f;           // Maximum swing angle (degrees)

    private Vector3 lastPlayerPos;
    private Quaternion lastPlayerRot;

    void Start()
    {
        if (player == null || lanternRb == null || hinge == null)
        {
            Debug.LogError("Assign all references in LanternSwingController.");
            enabled = false;
            return;
        }

        lastPlayerPos = player.position;
        lastPlayerRot = player.rotation;

        // Setup Hinge Joint spring
        JointSpring js = hinge.spring;
        js.spring = spring;
        js.damper = damper;
        js.targetPosition = 0f;
        hinge.spring = js;
        hinge.useSpring = true;

        // Limit swing
        JointLimits limits = hinge.limits;
        limits.min = -maxSwingAngle;
        limits.max = maxSwingAngle;
        hinge.limits = limits;
        hinge.useLimits = true;
    }

    void FixedUpdate()
    {
        Vector3 hingeDir = lanternRb.transform.TransformDirection(hingeAxis);

        // --- 1. Player movement torque ---
        Vector3 playerDelta = (player.position - lastPlayerPos) / Time.fixedDeltaTime;

        // Project player movement onto plane perpendicular to hinge axis
        Vector3 swingPlaneDelta = playerDelta - Vector3.Project(playerDelta, hingeDir);

        // Convert swing direction to scalar along hinge
        Vector3 crossDir = Vector3.Cross(hingeDir, Vector3.up).normalized; // perpendicular direction
        float torqueAlongHinge = Vector3.Dot(swingPlaneDelta, crossDir);

        // Scale torque based on proximity to swing limit
        float currentAngle = hinge.angle;
        float limitRatio = 1f - Mathf.Clamp01(Mathf.Abs(currentAngle) / maxSwingAngle);
        lanternRb.AddTorque(hingeDir * torqueAlongHinge * swingForceMultiplier * limitRatio, ForceMode.Force);

        // --- 2. Player rotation torque ---
        Quaternion deltaRot = player.rotation * Quaternion.Inverse(lastPlayerRot);
        deltaRot.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;
        Vector3 rotVel = axis * angle * Mathf.Deg2Rad / Time.fixedDeltaTime;
        float rotAlongHinge = Vector3.Dot(rotVel, hingeDir);

        // Scale rotational torque by proximity to limit
        lanternRb.AddTorque(hingeDir * rotAlongHinge * rotationForceMultiplier * limitRatio, ForceMode.Force);

        lastPlayerPos = player.position;
        lastPlayerRot = player.rotation;
    }
}
