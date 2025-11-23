using UnityEngine;

public class LanternSwingController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody stickParentRb;        // The RB moved by the player
    public CharacterJoint cj;              // The CharacterJoint
    public Rigidbody lanternRb;            // The lantern RB

    [Header("Swing Dynamics")]
    public float movementForce = 5f;       // Reduced default
    public float maxSwingAngle = 45f;      
    public float gravityRestoringForce = 10f; 
    public float airDrag = 1f;
    [Tooltip("Angle (in degrees) to tilt the lantern downwards to help aim at the ground.")]
    public float aimPitchBias = 15f;

    [Header("Smart Constraints")]
    [Tooltip("How strongly we prevent the lantern from tipping upwards (Pitch < 0).")]
    public float antiTipStrength = 50f;
    [Tooltip("Multiplier for the swing force when moving forward, specifically to counteract drag.")]
    public float forwardCounterForce = 2.0f;

    [Header("Aim Stabilization")]
    [Tooltip("How strictly we lock the lantern's Yaw to the player's aim.")]
    public float aimLockStrength = 10f;    // Strength of the velocity override
    [Tooltip("How fast the lantern slowly aligns to center when drift occurs.")]
    public float alignmentSpeed = 5f;
    
    private Vector3 lastStickPos;
    private Vector3 lastStickEuler;
    private Vector3 smoothedAccel;
    private Vector3 lastStickVelocity;

    void Start()
    {
        if (stickParentRb == null) Debug.LogError("StickParent RB missing!");
        if (lanternRb == null) Debug.LogError("Lantern RB missing!");
        
        if (lanternRb != null)
        {
            lanternRb.maxAngularVelocity = 30f;
            
            if (stickParentRb != null)
            {
                lastStickPos = stickParentRb.position;
                lastStickEuler = stickParentRb.rotation.eulerAngles;
            }
        }
    }

    void FixedUpdate()
    {
        if (stickParentRb == null || lanternRb == null) return;

        float dt = Time.fixedDeltaTime;
        if (dt <= 0) return;

        // --- 1. Calculate Inputs ---
        Vector3 currentStickPos = stickParentRb.position;
        Vector3 stickVel = (currentStickPos - lastStickPos) / dt;
        Vector3 rawAccel = (stickVel - lastStickVelocity) / dt;
        
        lastStickPos = currentStickPos;
        lastStickVelocity = stickVel;

        // Smooth acceleration
        smoothedAccel = Vector3.Lerp(smoothedAccel, rawAccel, dt * 8f);

        // Calculate Parent Yaw Speed
        Vector3 currentEuler = stickParentRb.rotation.eulerAngles;
        float deltaYaw = Mathf.DeltaAngle(lastStickEuler.y, currentEuler.y);
        float parentYawSpeed = deltaYaw / dt; // Degrees per second
        lastStickEuler = currentEuler;

        // --- 2. Apply Logic ---

        // A. Swing Force (Movement)
        ApplySwingForce(smoothedAccel);
        
        // B. Gravity / Restoring (Return to Center)
        ApplyGravityRestoration();

        // C. Damping (General Air Resistance)
        ApplyDamping();

        // D. Aim Locking (The Fix)
        ApplyYawLock(parentYawSpeed);

        // E. Anti-Tip (Smart Constraint)
        ApplyAntiTip();
    }

    void ApplySwingForce(Vector3 accel)
    {
        if (accel.sqrMagnitude < 0.1f) return;

        // Decompose acceleration
        Vector3 forwardDir = Vector3.ProjectOnPlane(stickParentRb.transform.forward, Vector3.up).normalized;
        float forwardAccelMag = Vector3.Dot(accel, forwardDir);
        
        // If accelerating forward (positive dot), we want to apply EXTRA force to keep nose down
        // If accelerating backward/strafing, use normal force
        float forceMultiplier = (forwardAccelMag > 0) ? (movementForce * forwardCounterForce) : movementForce;

        // Swing axis is perpendicular to acceleration and Up
        Vector3 swingAxis = Vector3.Cross(accel, Vector3.up).normalized;
        
        // Swing limit check
        float currentAngle = Vector3.Angle(transform.up, Vector3.up);
        float limitFactor = 1f - Mathf.Clamp01(currentAngle / maxSwingAngle);

        // Apply force
        lanternRb.AddTorque(swingAxis * (accel.magnitude * forceMultiplier * limitFactor * Time.fixedDeltaTime), ForceMode.Impulse);
    }

    void ApplyAntiTip()
    {
        // "Smart System" to prevent upward tipping
        // Check if the lantern is pointing UP relative to the horizon
        
        // Dot Product: Forward * Up
        // > 0 means pointing Skyward (Bad)
        // < 0 means pointing Groundward (Good)
        float pitchDot = Vector3.Dot(transform.forward, Vector3.up);

        // If we are pitching up (or close to it, give it a small buffer)
        // We want to aggressively dampen the upward velocity and push it down
        if (pitchDot > -0.1f) 
        {
            // 1. Damping: If moving UP, kill it.
            // Convert world angular velocity to local to find Pitch velocity (X axis usually)
            Vector3 localAV = transform.InverseTransformDirection(lanternRb.angularVelocity);
            
            // Local X is pitch. If rotating BACK (-X), kill it.
            // Wait, depends on axis. Let's stick to world space to be safe.
            
            // Calculate axis that would rotate "Forward" towards "Down"
            Vector3 correctionAxis = Vector3.Cross(transform.forward, Vector3.down).normalized;
            
            // Apply torque along this axis
            // Strength scales with how bad the pitch is
            float strength = (pitchDot + 0.1f) * antiTipStrength; 
            lanternRb.AddTorque(correctionAxis * (strength * Time.fixedDeltaTime), ForceMode.Impulse);
            
            // Also dampen vertical swing velocity explicitly
            // If angular velocity is lifting the nose, dampen it
            // Check if Angular Velocity aligns with the axis that lifts the nose
             Vector3 liftAxis = Vector3.Cross(transform.forward, Vector3.up).normalized; // Axis pointing Right
             float liftVel = Vector3.Dot(lanternRb.angularVelocity, liftAxis);
             
             if (liftVel > 0) // Lifting up
             {
                 lanternRb.angularVelocity -= liftAxis * (liftVel * 0.5f); // Kill 50% of lift per frame
             }
        }
    }

    void ApplyGravityRestoration()
    {
        // 1. Calculate Reference Frame
        // We use the stick's flat forward direction to define "Forward" for the bias
        Vector3 stickForward = stickParentRb.transform.forward;
        Vector3 stickForwardFlat = Vector3.ProjectOnPlane(stickForward, Vector3.up).normalized;
        
        // Handle case where looking straight up/down
        if (stickForwardFlat.sqrMagnitude < 0.001f) 
            stickForwardFlat = transform.forward; // Use current lantern forward as fallback

        Vector3 stickRight = Vector3.Cross(Vector3.up, stickForwardFlat);

        // 2. Calculate Target Up Vector
        // Rotate standard Up BACKWARDS around the Right axis.
        // This tilts the "hanging string" backwards, causing the lantern front to tilt DOWN.
        Quaternion biasRot = Quaternion.AngleAxis(-aimPitchBias, stickRight);
        Vector3 targetUp = biasRot * Vector3.up;

        // 3. Apply Torque
        // Pull transform.up towards targetUp
        Vector3 axis = Vector3.Cross(transform.up, targetUp);
        float angle = Vector3.Angle(transform.up, targetUp);
        
        // Apply restoring force
        lanternRb.AddTorque(axis * (angle * gravityRestoringForce * Time.fixedDeltaTime), ForceMode.Acceleration);
    }

    void ApplyYawLock(float targetYawSpeed)
    {
        // 1. Match Angular Velocity
        Vector3 currentAV = lanternRb.angularVelocity;
        
        // We want to force the Y component of Angular Velocity to match the parent's turn speed
        // But we leave X and Z (swinging) alone.
        
        float targetYawRad = targetYawSpeed * Mathf.Deg2Rad;
        float newYaw = Mathf.Lerp(currentAV.y, targetYawRad, aimLockStrength * Time.fixedDeltaTime);
        
        lanternRb.angularVelocity = new Vector3(currentAV.x, newYaw, currentAV.z);

        // 2. Correction for drift (Alignment)
        // If we are facing the wrong way, gently push Yaw towards target
        Vector3 forwardFlat = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 targetFlat = Vector3.ProjectOnPlane(stickParentRb.transform.forward, Vector3.up).normalized;
        
        float angleError = Vector3.SignedAngle(forwardFlat, targetFlat, Vector3.up);
        
        // Always apply gentle corrective torque to bring it to center
        // We modify the angular velocity directly for stability
        lanternRb.angularVelocity += Vector3.up * (angleError * Mathf.Deg2Rad * alignmentSpeed * Time.fixedDeltaTime);
    }

    void ApplyDamping()
    {
        // Dampen X/Z only (Swing damping)
        Vector3 av = lanternRb.angularVelocity;
        av.x *= (1f - airDrag * Time.fixedDeltaTime);
        av.z *= (1f - airDrag * Time.fixedDeltaTime);
        lanternRb.angularVelocity = av;
    }
}
