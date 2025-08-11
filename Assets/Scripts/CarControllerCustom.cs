using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

public class CarControllerCustom : MonoBehaviour,ICar
{
    // ────────────────────────────────────────────────────────────
    //  Movement
    // ────────────────────────────────────────────────────────────
    [Header("Movement Settings")]
    public float motorTorque   = 1500f;   // Torque per wheel (Nm)
    public float brakeTorque   = 3000f;   // Max hand‑brake force (Nm)
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

    [Header("Health")]
    public int health = 100;
    
    // ── Telemetry for other scripts ──
    public float CurrentSpeedKmh => _rb.linearVelocity.magnitude * 3.6f;
    public float ThrottleInput   { get; private set; }   // –1…1
    public bool  IsBraking       { get; private set; }

    
    
    // ────────────────────────────────────────────────────────────
    //  Optional section reskinning
    // ────────────────────────────────────────────────────────────
    [Header("Optional Section To Reskin")]
    [Tooltip("Assign the Renderer you’d like to swap material on at runtime.")]
    public Renderer sectionRenderer;

    private Rigidbody _rb;

    // ────────────────────────────────────────────────────────────
    //  Unity Callbacks
    // ────────────────────────────────────────────────────────────

    // private void Update()
    // {
    //     HandleInput();
    //     UpdateWheelVisuals();
    // }


    // if set damage return false it means the car is dead
    public bool setDamage(int damage)
    {
        health -= damage;

        if (health <= 0)
        {
            health = 0;
            return false;
        }

        return true;
    }
    
    
    
    // ────────────────────────────────────────────────────────────
    //  Input & Movement
    // ────────────────────────────────────────────────────────────
    private void HandleInput()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // Clamp velocity to maxSpeed (converted from m/s ➔ km/h)
        float speedKmh = _rb.linearVelocity.magnitude * 3.6f;
        
        float torque   = (speedKmh < maxSpeed || v < 0f) ? motorTorque * v : 0f;

        ApplyDrive(torque);
        ApplySteer(h);
        ApplyBrakes(Input.GetKey(KeyCode.Space));
    }
    
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        // Example: tighten both curves a bit right after you grab the colliders
        TuneTire(frontLeftCollider);
        TuneTire(frontRightCollider);
        TuneTire(rearLeftCollider);
        TuneTire(rearRightCollider);
    }

    [Header("Handling Settings")] [SerializeField]
    private float stiffnessCoefficientSideWay = 1.5f;
    [SerializeField]private float StiffnessCoefficenitForward = 1.3f;
    void TuneTire(WheelCollider col)
    {
        WheelFrictionCurve f = col.sidewaysFriction;
        f.stiffness *= stiffnessCoefficientSideWay;          // 1 = default. 1.5–2 is grippier. Don’t exceed ~5.
        col.sidewaysFriction = f;

        f = col.forwardFriction;
        f.stiffness *= StiffnessCoefficenitForward;          // Helps with traction under throttle/brake.
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

    private void ApplyBrakes(bool braking)
    {
        float brake = braking ? brakeTorque : 0f;
        frontLeftCollider.brakeTorque  = brake;
        frontRightCollider.brakeTorque = brake;
        rearLeftCollider.brakeTorque   = brake;
        rearRightCollider.brakeTorque  = brake;
    }

    // ────────────────────────────────────────────────────────────
    //  Wheel mesh syncing
    // ────────────────────────────────────────────────────────────
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
    //  Public API: reskinning the assigned section
    // ────────────────────────────────────────────────────────────
    /// <summary>
    /// Swaps the material on the optional <see cref="sectionRenderer"/>, if both the
    /// renderer and the new material are non‑null.
    /// </summary>
    /// <param name="newMaterial">Material to apply.</param>
    public void ApplySectionMaterial(Material newMaterial)
    {
        if (sectionRenderer != null && newMaterial != null)
        {
            sectionRenderer.material = newMaterial;
        }
    }

    public void Move(float steering, float accel, float footbrake, float handbrake)
    {
        // Clamp velocity to maxSpeed (converted from m/s ➔ km/h)
        float speedKmh = _rb.linearVelocity.magnitude * 3.6f;
        float torque   = (speedKmh < maxSpeed || accel < 0f) ? motorTorque * accel : 0f;

        ApplyDrive(torque);
        ApplySteer(steering);
        ApplyBrakes(footbrake != 0 || handbrake != 0);
        
        
        ThrottleInput = accel;
        IsBraking     = footbrake != 0 || handbrake != 0;
        UpdateWheelVisuals();
    }
}
