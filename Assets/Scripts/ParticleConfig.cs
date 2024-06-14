using UnityEngine;

public static class ParticleConfig
{
    public const float RestDensity = 1000f;
    public const float Radius = 0.08f;
    public const float Diameter = 2f * Radius;
    public const float Radius2 = Radius * Radius;
    public const float Volume = 4f / 3f * Mathf.PI * Radius * Radius * Radius;
    public const float Mass = RestDensity * Volume;
}
