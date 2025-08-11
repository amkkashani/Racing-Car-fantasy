using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
struct DeformJob : IJobParallelFor
{
    [NativeDisableParallelForRestriction] public NativeArray<float3> vertices;

    [ReadOnly] public float3 hitPoint;        // centre of the influence area
    [ReadOnly] public float   radius;         // influence radius (local units)
    [ReadOnly] public float   depth;          // max offset magnitude
    [ReadOnly] public uint    seed;           // noise seed
    [ReadOnly] public float   noiseCoefficient;

    public void Execute (int index)
    {
        float3 v = vertices[index];

        // Use hitPoint only for the fall‑off test
        float  d2  = math.lengthsq(v - hitPoint);
        float  r2  = radius * radius;
        if (d2 > r2) return;                 // outside the dent patch

        // Fall‑off weight: (1‑d/r)²
        float w = 1f - math.sqrt(d2) / radius;
        w *= w;

        // Direction is now toward local origin (‑v)
        float3 dir = math.normalize(-v);     // ‼️ moves inward to (0,0,0)

        // Small wobble so the surface isn’t perfectly smooth
        uint  h     = (uint)index * 1103515245u + seed;
        float noise = ((h & 0xFFFFu) / 65535f - 0.5f) * noiseCoefficient;

        vertices[index] = v + dir * depth * (w + noise * w);
    }
}