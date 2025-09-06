using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

public class CarControllerCustom : MonoBehaviour, ICar
{
    // ────────────────────────────────────────────────────────────
    //  Movement
    // ────────────────────────────────────────────────────────────
    [Header("Movement Settings")]
    public float motorTorque   = 1500f;   // Torque per wheel (Nm)
    public float brakeTorque   = 3000f;   // Max hand-brake force (Nm)
    public float maxSteerAngle = 30f;     // Degrees
    public float maxSpeed      = 50f;     // km/h speed cap
    public float steerSpeed    = 5f;      // Lerp speed for steering

    [Header("Wheel Colliders")]
    public WheelCollider frontLeftCollider;
    public WheelCollider frontRightCollider;
    public WheelCollider rearLeftCollider;
    public WheelCollider rearRightCollider;

    [Header("Wheel Meshes")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    // Telemetry for other scripts
    public float CurrentSpeedKmh => _rb.linearVelocity.magnitude * 3.6f;
    public float ThrottleInput   { get; private set; }
    public bool  IsBraking       { get; private set; }

    private Rigidbody _rb;

    [Header("Handling Settings")] 
    [SerializeField] private float stiffnessCoefficientSideWay = 1.5f;
    [SerializeField] private float StiffnessCoefficenitForward = 1.3f;

    // Reverse logic
    private bool reversing = false;
    [SerializeField] private float reverseDelay = 0.6f;     // Hold brake this long to reverse
    [SerializeField] private float stopThreshold = 0.2f;    // Speed threshold (m/s) considered "stopped"
    private float brakeHeldTime = 0f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        TuneTire(frontLeftCollider);
        TuneTire(frontRightCollider);
        TuneTire(rearLeftCollider);
        TuneTire(rearRightCollider);
    }

    private void TuneTire(WheelCollider col)
    {
        WheelFrictionCurve f = col.sidewaysFriction;
        f.stiffness *= stiffnessCoefficientSideWay;
        col.sidewaysFriction = f;

        f = col.forwardFriction;
        f.stiffness *= StiffnessCoefficenitForward;
        col.forwardFriction = f;
    }

    private void ApplyDrive(float torque)
    {
        frontLeftCollider.motorTorque  = torque;
        frontRightCollider.motorTorque = torque;
        rearLeftCollider.motorTorque   = torque;
        rearRightCollider.motorTorque  = torque;
    }

    private void ApplySteer(float steerInput)
    {
        float target = steerInput * maxSteerAngle;
        frontLeftCollider.steerAngle  = Mathf.Lerp(frontLeftCollider.steerAngle,  target, Time.deltaTime * steerSpeed);
        frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, target, Time.deltaTime * steerSpeed);
    }

    private void ApplyBrakes(float brake)
    {
        frontLeftCollider.brakeTorque  = brake;
        frontRightCollider.brakeTorque = brake;
        rearLeftCollider.brakeTorque   = brake;
        rearRightCollider.brakeTorque  = brake;
    }

    private void UpdateWheelVisuals()
    {
        UpdateWheel(frontLeftCollider,  frontLeftMesh);
        UpdateWheel(frontRightCollider, frontRightMesh);
        UpdateWheel(rearLeftCollider,   rearLeftMesh);
        UpdateWheel(rearRightCollider,  rearRightMesh);
    }

    private static void UpdateWheel(WheelCollider col, Transform mesh)
    {
        if (col == null || mesh == null) return;
        col.GetWorldPose(out Vector3 pos, out Quaternion rot);
        mesh.SetPositionAndRotation(pos, rot);
    }

    // ────────────────────────────────────────────────────────────
    //  Public API: ICar
    // ────────────────────────────────────────────────────────────
    public void Move(float steering, float accel, float footbrake, float handbrake)
    {
        float speedKmh = _rb.linearVelocity.magnitude * 3.6f;

        // braking input check
        bool brakingInput = (footbrake > 0 || handbrake > 0);

        // reverse detection
        if (brakingInput)
        {
            if (speedKmh < stopThreshold * 3.6f) // car nearly stopped
            {
                brakeHeldTime += Time.deltaTime;
                if (brakeHeldTime > reverseDelay)
                {
                    reversing = true;
                }
            }
            else
            {
                brakeHeldTime = 0f;
            }
        }
        else
        {
            brakeHeldTime = 0f;
            reversing = false;
        }

        float torque = 0f;
        if (reversing)
        {
            torque = -motorTorque * Mathf.Abs(footbrake); // reverse drive
            ApplyBrakes(0f);
        }
        else
        {
            torque = (speedKmh < maxSpeed || accel < 0f) ? motorTorque * accel : 0f;
            ApplyBrakes(brakingInput ? brakeTorque : 0f);
        }

        ApplyDrive(torque);
        ApplySteer(steering);

        ThrottleInput = accel;
        IsBraking     = brakingInput && !reversing;
        UpdateWheelVisuals();
    }
}
