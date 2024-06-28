using System;
using UnityEngine;

// Particle-Based Fluid Simulation for Interactive Applications
// https://matthias-research.github.io/pages/publications/sca03.pdf
public class SmoothingKernel
{
    public float Poly6Coeff { get; private set; }
    public float SpikyGradCoeff { get; private set; }
    public float ViscLaplacianCoeff { get; private set; }
    public float H { get; private set; }
    public float H2 { get; private set; }
    public float InvH { get; private set; }

    public SmoothingKernel(float radius)
    {
        H = radius;
        H2 = radius * radius;
        InvH = 1f / radius;
        
        float PI = Mathf.PI;

        Poly6Coeff = 315.0f / (64.0f * PI * Mathf.Pow(H, 9.0f));

        SpikyGradCoeff = -45.0f / (PI * Mathf.Pow(H, 6.0f));

        ViscLaplacianCoeff = 45.0f / (PI * Mathf.Pow(H, 6.0f));
    }
    
    float Pow3(float v)
    {
        return v * v * v;
    }

    public float Poly6(Vector3 p)
    {
        float r2 = p.sqrMagnitude;
        return Math.Max(0, Poly6Coeff * Pow3(H2 - r2));
    }
}
