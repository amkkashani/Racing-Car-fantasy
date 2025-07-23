// using UnityEngine;
//
// public class EasyLaneChange : MonoBehaviour
// {
//     [Header("Movement")]
//     [Tooltip("Units per second the car travels forward.")]
//     [SerializeField] private float forwardSpeed = 20f;
//
//     [Header("Lane Settings")]
//     [Tooltip("Distance between the centre of two adjacent lanes (world units).")]
//     [SerializeField] private float laneWidth = 3f;
//     [Tooltip("Time in seconds for a lane change to complete.")]
//     [SerializeField] private float laneChangeDuration = 0.35f;
//     [Tooltip("Animation curve that shapes the lateral offset during a lane change \n            (X‑axis: 0‑1 time, Y‑axis: 0‑1 interpolation).\n            Leave at default EaseInOut for a gentle S‑curve, or make it snappier.")]
//     [SerializeField] private AnimationCurve laneChangeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
//
//     [Header("Optional Limits")]
//     [Tooltip("Clamp the car to these lane indices (0 = centre lane).")]
//     [SerializeField] private int minLaneIndex = -1;
//     [SerializeField] private int maxLaneIndex = 1;
//
//     /// <summary>True while a lane change is occurring.</summary>
//     private bool isChangingLane;
//     /// <summary>Lane index we started the change from.</summary>
//     private int startLaneIndex = 0;
//     /// <summary>‑1 for left, +1 for right.</summary>
//     private int laneChangeDirection;
//     /// <summary>Timer tracking progress of the current lane change.</summary>
//     private float laneChangeTimer;
//     /// <summary>Current lane index once snap completed.</summary>
//     private int currentLaneIndex = 0;
//
//     private void Start()
//     {
//         // If the car is placed off‑centre in the editor, honour that as the starting lane
//         currentLaneIndex = Mathf.RoundToInt(transform.position.x / laneWidth);
//         SnapToLane(currentLaneIndex);
//     }
//
//     private void Update()
//     {
//         // Cache starting position for rotation calc.
//         Vector3 startPos = transform.position;
//
//         HandleLaneChangeInput();
//         UpdateLaneChange();
//         MoveForward();
//
//         UpdateRotation(startPos);
//     }
//
//     /// <summary>
//     /// Constant forward motion in world space.
//     /// </summary>
//     private void MoveForward()
//     {
//         transform.Translate(Vector3.forward * forwardSpeed * Time.deltaTime, Space.World);
//     }
//
//     /// <summary>
//     /// Keyboard events trigger a lane change if we're not already moving.
//     /// </summary>
//     private void HandleLaneChangeInput()
//     {
//         if (isChangingLane) return;
//
//         int directionInput = 0;
//         if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
//             directionInput = -1;
//         else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
//             directionInput = 1;
//
//         if (directionInput == 0) return; // no key pressed this frame
//
//         int targetLane = Mathf.Clamp(currentLaneIndex + directionInput, minLaneIndex, maxLaneIndex);
//         if (targetLane == currentLaneIndex) return; // at boundary
//
//         // Begin lane change
//         startLaneIndex = currentLaneIndex;
//         currentLaneIndex = targetLane;
//         laneChangeDirection = directionInput;
//         laneChangeTimer = 0f;
//         isChangingLane = true;
//     }
//
//     /// <summary>
//     /// Adds lateral offset while changing lane using the provided curve.
//     /// </summary>
//     private void UpdateLaneChange()
//     {
//         if (!isChangingLane) return;
//
//         laneChangeTimer += Time.deltaTime;
//         float t = Mathf.Clamp01(laneChangeTimer / laneChangeDuration);
//         float curvedT = laneChangeCurve.Evaluate(t);
//
//         // Lateral offset relative to start lane centre
//         float lateralOffset = laneChangeDirection * laneWidth * curvedT;
//         float baseX = startLaneIndex * laneWidth;
//
//         Vector3 pos = transform.position;
//         pos.x = baseX + lateralOffset;
//         transform.position = pos;
//
//         if (t >= 1f)
//         {
//             // Snap exactly into lane centre at the end
//             SnapToLane(currentLaneIndex);
//             isChangingLane = false;
//         }
//     }
//
//     /// <summary>
//     /// Calculates yaw from actual movement between frames so the car nose follows the path.
//     /// </summary>
//     private void UpdateRotation(Vector3 previousPosition)
//     {
//         Vector3 delta = transform.position - previousPosition;
//         if (delta.sqrMagnitude < 0.0001f) return; // too small to matter
//
//         // Only yaw (around Y) – keep ground alignment.
//         delta.y = 0f;
//         Quaternion targetRot = Quaternion.LookRotation(delta.normalized, Vector3.up);
//         transform.rotation = targetRot;
//     }
//
//     /// <summary>
//     /// Instantly move to a lane without animation.
//     /// </summary>
//     public void SnapToLane(int laneIndex)
//     {
//         currentLaneIndex = Mathf.Clamp(laneIndex, minLaneIndex, maxLaneIndex);
//         Vector3 p = transform.position;
//         p.x = currentLaneIndex * laneWidth;
//         transform.position = p;
//     }
//
//     /// <summary>
//     /// Dynamically change the lane width at runtime (e.g. variable‑width roads).
//     /// </summary>
//     public void SetLaneWidth(float newWidth)
//     {
//         laneWidth = newWidth;
//         SnapToLane(currentLaneIndex);
//     }
// }



// Second
using UnityEngine;

public class EasyLaneChange : MonoBehaviour
{
      /* ───────── inspector ───────── */
    [Header("Movement (will be overridden by CarStats if assigned)")]
    [SerializeField] private float forwardSpeed = 20f;         //  m/s fallback
    [SerializeField] private float laneChangeDuration = 0.35f; //  s  fallback
    [SerializeField] private AnimationCurve laneChangeCurve = AnimationCurve.EaseInOut(0,0,1,1);

    [Header("Physics")]
    [Tooltip("WheelColliders that receive motor/brake torque.")]
    [SerializeField] private WheelCollider[] driveWheels;
    
    
    /* ───────── private ───────── */
    private Rigidbody rb;
    private float maxSpeedMS;     // cached as metres/second
    private float motorPower;     // Nm
    private float brakePower;     // Nm

    [Header("Lane Settings")]
    [SerializeField] private float laneWidth = 3f;

    [Header("Optional Limits")]
    [SerializeField] private int minLaneIndex = -1;
    [SerializeField] private int maxLaneIndex = 1;

    // ──────────────────────────────────────────────────────────────────────────────
    private bool  isChangingLane;
    private int   startLaneIndex;          // lane we begin a change from
    private int   currentLaneIndex;        // lane we’re headed to / resting in
    private int   laneChangeDirection;     // -1 left, +1 right
    private float laneChangeTimer;         // sec. into current change

    /// <summary>World‑space X of lane 0, captured once at start‑up.</summary>
    private float laneOriginX;
    // ──────────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        laneOriginX       = transform.position.x; // treat this as lane 0
        currentLaneIndex  = 0;
        startLaneIndex    = 0;
        // No snapping / rounding here – we stay exactly where placed.
    }

    private void Update()
    {
        Vector3 startPos = transform.position;

        HandleLaneChangeInput();
        UpdateLaneChange();
        MoveForward();

        FaceMovementDirection(startPos);
    }

    // ───────────────────────── movement helpers ─────────────────────────

    private void MoveForward() =>
        transform.Translate(Vector3.forward * forwardSpeed * Time.deltaTime, Space.World);

    private void FaceMovementDirection(Vector3 previousPosition)
    {
        Vector3 delta = transform.position - previousPosition;
        if (delta.sqrMagnitude < 0.0001f) return;
        delta.y = 0f;
        transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
    }

    // ─────────────────────── lane‑change logic ─────────────────────────

    private void HandleLaneChangeInput()
    {
        if (isChangingLane) return;

        int dir = 0;
        if (Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A)) dir = -1;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) dir =  1;
        if (dir == 0) return;                      // nothing pressed
       
        int target = Mathf.Clamp(currentLaneIndex + dir, minLaneIndex, maxLaneIndex);
        if (target == currentLaneIndex) return;    // at boundary

        startLaneIndex      = currentLaneIndex;
        currentLaneIndex    = target;
        laneChangeDirection = dir;
        laneChangeTimer     = 0f;
        isChangingLane      = true;
    }

    private void UpdateLaneChange()
    {
        if (!isChangingLane) return;

        laneChangeTimer += Time.deltaTime;
        float t        = Mathf.Clamp01(laneChangeTimer / laneChangeDuration);
        float curvedT  = laneChangeCurve.Evaluate(t);

        float baseX          = laneOriginX + startLaneIndex      * laneWidth;
        float lateralOffset  = laneChangeDirection * laneWidth * curvedT;

        Vector3 pos = transform.position;
        pos.x = baseX + lateralOffset;
        transform.position = pos;

        if (t >= 1f)
        {
            SnapToLane(currentLaneIndex);
            isChangingLane = false;
        }
    }

    // ───────────────────────── utility API ────────────────────────────

    /// <summary>Instantly centre the car on the requested lane (still relative to lane 0).</summary>
    public void SnapToLane(int laneIndex)
    {
        currentLaneIndex = Mathf.Clamp(laneIndex, minLaneIndex, maxLaneIndex);
        Vector3 p = transform.position;
        p.x = laneOriginX + currentLaneIndex * laneWidth;
        transform.position = p;
    }

    /// <summary>Dynamically adjust lane spacing; car keeps its current lane index.</summary>
    public void SetLaneWidth(float newWidth)
    {
        laneWidth = newWidth;
        SnapToLane(currentLaneIndex);
    }
}


