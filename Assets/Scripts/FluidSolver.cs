using System;
using UnityEngine;

public class FluidSolver
{
    private const int Threads = 128;
    public int Groups { get; private set; }
    
    public int SubSteps { get; set; }
    public int ConstraintIterations { get; set; }
    
    public FluidBoundary Boundary { get; private set; }
    public FluidBody Body { get; private set; }
    
    public SmoothingKernel Kernel { get; private set; }

    private ComputeShader _shader;

    public FluidSolver(FluidBody body, FluidBoundary boundary)
    {
        SubSteps = 2;
        ConstraintIterations = 2;
        Body = body;
        Boundary = boundary;
        Kernel = new SmoothingKernel(ParticleConfig.Radius * 4f);

        int numParticles = Body.Particles.NumParticles;
        Groups = numParticles / Threads;
        if (numParticles % Threads != 0) Groups++;
        
        _shader = Resources.Load("FluidSolver") as ComputeShader;
    }

    public void Step(float dt)
    {
        if (dt <= 0.0f) 
            throw new ArgumentException("dt must be greater than 0");
        if (SubSteps <= 0 || ConstraintIterations <= 0) 
            throw new ArgumentException("SolverIterations and ConstraintIterations must be greater than 0");

        dt /= SubSteps;
        
        _shader.SetInt("NumParticles", Body.Particles.NumParticles);
        _shader.SetInt("NumBoundaryParticles", Boundary.Particles.NumParticles);
        _shader.SetFloat("Mass", ParticleConfig.Mass);
        _shader.SetFloat("DeltaTime", dt);
        _shader.SetFloat("RestDensity", ParticleConfig.RestDensity);
        _shader.SetFloat("Viscosity", ParticleConfig.Viscosity);
        _shader.SetVector("Gravity", new Vector4(0f, -9.81f, 0f, 0f));
        
        _shader.SetFloat("KernelRadius", Kernel.H);
        _shader.SetFloat("KernelRadius2", Kernel.H2);
        _shader.SetFloat("Poly6Zero", Kernel.Poly6(Vector3.zero));
        _shader.SetFloat("Poly6Coeff", Kernel.Poly6Coeff);
        _shader.SetFloat("SpikyGradCoeff", Kernel.SpikyGradCoeff);
        _shader.SetFloat("ViscLaplacianCoeff", Kernel.ViscLaplacianCoeff);
        
        for (int i = 0; i < SubSteps; i++)
        {
            PredictPositions();
            SolveConstraints();
            UpdateVelocities();
            ApplyViscosity();
            UpdatePositions();
        }
    }
    
    public void PredictPositions()
    {
        
    }
    
    public void SolveConstraints()
    {
        
    }
    
    public void UpdateVelocities()
    {
        
    }
    
    public void ApplyViscosity()
    {
        
    }
    
    public void UpdatePositions()
    {
        
    }
}
