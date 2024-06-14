#ifndef SMOOTHING_KERNEL
#define SMOOTHING_KERNEL

float KernelRadius;
float KernelRadius2;

float Poly6Zero;
float Poly6;
float SpikyGrad;
float ViscLap;

float Pow2(float v)
{
    return v * v;
}

float Pow3(float v)
{
    return v * v * v;
}

float Poly6Kernel(float len2)
{
    return Poly6 * Pow3(KernelRadius2 - len2);
}

float3 SpikyGradKernel(float3 p, float len2)
{
    float r = sqrt(len2);
    return (p / r) * SpikyGrad * Pow2(KernelRadius - r);
}

float ViscLapKernel(float len2)
{
    float r = sqrt(len2);
    return ViscLap * (KernelRadius - r);
}

#endif