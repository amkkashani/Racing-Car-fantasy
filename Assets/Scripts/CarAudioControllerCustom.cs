
using UnityEngine;

[RequireComponent(typeof(CarControllerCustom))]
public class CarAudioControllerCustom : MonoBehaviour
{
    [Header("Audio Clips (drop clips, not sources)")]
    public AudioClip engineClip;   // Seamless loop clip
    public AudioClip brakeClip;    // One‑shot skid / squeal

    [Header("Engine Pitch Settings")]
    [Range(0.1f, 2f)] public float minPitch = 0.8f;   // Idle
    [Range(0.1f, 4f)] public float maxPitch = 2f;     // Red‑line
    public float pitchSmooth = 2f;                    // How snappy pitch follows target

    private AudioSource _engineSource;
    private AudioSource _brakeSource;
    private CarControllerCustom _car;

    private void Awake()
    {
        _car = GetComponent<CarControllerCustom>();

        // ────────────── Engine AudioSource (created in code) ──────────────
        _engineSource = gameObject.AddComponent<AudioSource>();
        _engineSource.clip         = engineClip;
        _engineSource.loop         = true;
        _engineSource.spatialBlend = 1f;   // 3D
        _engineSource.playOnAwake  = false;
        if (engineClip) _engineSource.Play();

        // ────────────── Brake AudioSource ──────────────
        _brakeSource = gameObject.AddComponent<AudioSource>();
        _brakeSource.clip         = brakeClip;
        _brakeSource.loop         = false;
        _brakeSource.spatialBlend = 1f;
        _brakeSource.playOnAwake  = false;
    }

    private void Update()
    {
        UpdateEngineSound();
        UpdateBrakeSound();
    }

    // ------------------------------------------------------------------
    private void UpdateEngineSound()
    {
        if (!engineClip) return;  // nothing assigned

        float speedT     = Mathf.Clamp01(_car.CurrentSpeedKmh / _car.maxSpeed);
        float throttleT  = Mathf.Abs(_car.ThrottleInput);
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, Mathf.Max(speedT, throttleT));

        _engineSource.pitch  = Mathf.Lerp(_engineSource.pitch, targetPitch, Time.deltaTime * pitchSmooth);
        _engineSource.volume = Mathf.Lerp(0.5f, 1f, throttleT);
    }

    private void UpdateBrakeSound()
    {
        if (!brakeClip) return;

        bool braking = _car.IsBraking && _car.CurrentSpeedKmh > 5f;

        if (braking && !_brakeSource.isPlaying)
            _brakeSource.Play();
        else if (!braking && _brakeSource.isPlaying)
            _brakeSource.Stop();
    }
}