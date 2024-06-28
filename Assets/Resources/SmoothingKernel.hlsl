#ifndef SMOOTHING_KERNEL
#define SMOOTHING_KERNEL

// Particle-Based Fluid Simulation for Interactive Applications
// https://matthias-research.github.io/pages/publications/sca03.pdf

float KernelRadius;
float KernelRadius2;

float Poly6Zero;
float Poly6Coeff;
float SpikyGradCoeff;
float ViscLapCoeff;

float Pow2(float v)
{
    return v * v;
}

float Pow3(float v)
{
    return v * v * v;
}

float Poly6Kernel(float len)
{
    return Poly6Coeff * Pow3(KernelRadius2 - Pow2(len));
}

float3 SpikyGradKernel(float3 p, float len)
{
    return p / len * SpikyGradCoeff * Pow2(KernelRadius - len);
}

float ViscLapKernel(float len)
{
    return ViscLapCoeff * (KernelRadius - len);
}

#endif