using UnityEngine;

public static class ParticleConfig
{
    public const float RestDensity = 1000f;
    public const float Radius = 0.1f;
    public const float Diameter = 2f * Radius;
    public const float Radius2 = Radius * Radius;
    public const float InvRadius = 1f / Radius;
    public const float Volume = 4f / 3f * Mathf.PI * Radius * Radius * Radius;
    public const float Mass = RestDensity * Volume;
    public const float InvMass = 1f / Mass;
    public const float Viscosity = 0.002f;
}
