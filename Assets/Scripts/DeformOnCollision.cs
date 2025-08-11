using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
public class DeformOnCollision : MonoBehaviour
{
    // cached data
    [SerializeField] private Transform _meshOwner;
    Mesh _mesh; // unique per instance

    // working copy used by jobs
    NativeArray<float3> _vertices;

    // immutable copy of the original (for reset)
    NativeArray<float3> _originalVertices;

    bool _dirty; // marks when a job altered _vertices

    // deformation parameters
    [SerializeField] float radiusPerMps = 0.4f;     // metres of influence /- relative speed
    [SerializeField] float noiseCoefficient = 0.002f; // metres of influence /- relative speed
    [SerializeField] float pushPerMps = 0.01f;      // metres of dent /- relative speed
    [SerializeField] int batchSize = 16;            // job inner-loop granularity

    void Awake()
    {
        _mesh = Instantiate(_meshOwner.GetComponent<MeshFilter>().mesh); // leave the shared mesh untouched
        _meshOwner.GetComponent<MeshFilter>().mesh = _mesh;

        Vector3[] verts = _mesh.vertices;

        _vertices = new NativeArray<float3>(verts.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        _originalVertices = new NativeArray<float3>(verts.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        for (int i = 0; i < verts.Length; i++)
        {
            float3 v = verts[i];
            _vertices[i] = v;
            _originalVertices[i] = v; // snapshot of the original for reset
        }
    }

    void OnDestroy()
    {
        if (_vertices.IsCreated) _vertices.Dispose();
        if (_originalVertices.IsCreated) _originalVertices.Dispose();
    }

    void OnCollisionEnter(Collision c)
    {
        foreach (var contact in c.contacts)
        {
            float speed = c.relativeVelocity.magnitude; // how hard we hit
            if (speed < 0.1f) continue; // ignore micro-scratches

            var job = new DeformJob
            {
                vertices         = _vertices,
                hitPoint         = _meshOwner.InverseTransformPoint(contact.point),
                radius           = math.clamp(speed * radiusPerMps, 0f, 1.5f),
                depth            = speed * pushPerMps,
                seed             = (uint)Random.Range(1, int.MaxValue),
                noiseCoefficient = noiseCoefficient
            };

            // Schedule once, complete immediately (simple path).
            job.Schedule(_vertices.Length, batchSize).Complete();
            _dirty = true;
        }
    }

    void LateUpdate()
    {
        if (!_dirty) return;
        _dirty = false;

        // Copy NativeArray → managed Vert[] once; fast enough at ~40 k verts
        var managed = _mesh.vertices;
        for (int i = 0; i < managed.Length; i++) managed[i] = _vertices[i];
        _mesh.vertices = managed;

        _mesh.RecalculateNormals(); // optional: or schedule another Burst job
        _mesh.RecalculateBounds();
    }

    /// <summary>
    /// Restores the mesh to its original (startup) shape.
    /// </summary>
    [ContextMenu("Reset Deformation")]
    public void ResetDeformation()
    {
        if (!_vertices.IsCreated || !_originalVertices.IsCreated) return;

        // Copy original snapshot back into working vertices
        NativeArray<float3>.Copy(_originalVertices, _vertices);

        // Push straight to the mesh so it's visible immediately this frame
        var managed = _mesh.vertices;
        for (int i = 0; i < managed.Length; i++) managed[i] = _originalVertices[i];
        _mesh.vertices = managed;

        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        _dirty = false; // we've already synced the mesh
    }
}
