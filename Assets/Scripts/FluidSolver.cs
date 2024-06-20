using System;
using UnityEngine;

public class FluidSolver
{
    private const int Threads = 128;
    private const int Read = 0;
    private const int Write = 1;
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
        
        _shader.SetFloat("KernelRadius", Kernel.H);
        _shader.SetFloat("KernelRadius2", Kernel.H2);
        _shader.SetFloat("Poly6Zero", Kernel.Poly6(Vector3.zero));
        _shader.SetFloat("Poly6Coeff", Kernel.Poly6Coeff);
        _shader.SetFloat("SpikyGradCoeff", Kernel.SpikyGradCoeff);
        _shader.SetFloat("ViscLapCoeff", Kernel.ViscLaplacianCoeff);
        
        _shader.SetVector("Gravity", new Vector4(0f, -9.81f, 0f, 0f));
        _shader.SetFloat("DeltaTime", dt);
        _shader.SetFloat("RestDensity", ParticleConfig.RestDensity);
        _shader.SetFloat("Viscosity", ParticleConfig.Viscosity);
        _shader.SetFloat("Mass", ParticleConfig.Mass);
        _shader.SetInt("NumFluidParticles", Body.Particles.NumParticles);
        _shader.SetInt("NumBoundaryParticles", Boundary.Particles.NumParticles);
        _shader.SetFloat("Psi", Boundary.Psi);
        
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
        int kernel = _shader.FindKernel("PredictPositions");
        
        _shader.SetBuffer(kernel, "FluidPositions", Body.PositionsBuf);
        _shader.SetBuffer(kernel, "PredictedPositionsWrite", Body.PredictedPositionsBuf[Write]);
        _shader.SetBuffer(kernel, "VelocitiesRead", Body.VelocitiesBuf[Read]);
        
        _shader.Dispatch(kernel, Groups, 1, 1);
        
        CBUtility.Swap(Body.PredictedPositionsBuf);
        CBUtility.Swap(Body.VelocitiesBuf);
    }
    
    public void SolveConstraints()
    {
        int computeKernel = _shader.FindKernel("ComputeLambda");
        int solveKernel = _shader.FindKernel("SolveConstraint");
        
        _shader.SetBuffer(computeKernel, "Densities", Body.DensitiesBuf);
        _shader.SetBuffer(computeKernel, "Lambdas", Body.LambdasBuf);
        _shader.SetBuffer(computeKernel, "BoundaryPositions", Boundary.PositionsBuf);
        
        _shader.SetBuffer(solveKernel, "BoundaryPositions", Boundary.PositionsBuf);
        _shader.SetBuffer(solveKernel, "Lambdas", Body.LambdasBuf);

        for (int i = 0; i < ConstraintIterations; i++)
        {
            _shader.SetBuffer(computeKernel, "PredictedPositionsRead", Body.PredictedPositionsBuf[Read]);
            _shader.Dispatch(computeKernel, Groups, 1, 1);
            
            _shader.SetBuffer(solveKernel, "PredictedPositionsRead", Body.PredictedPositionsBuf[Read]);
            _shader.SetBuffer(solveKernel, "PredictedPositionsWrite", Body.PredictedPositionsBuf[Write]);
            _shader.Dispatch(solveKernel, Groups, 1, 1);
            
            CBUtility.Swap(Body.PredictedPositionsBuf);
        }
    }
    
    public void UpdateVelocities()
    {
        int kernel = _shader.FindKernel("UpdateVelocities");
        
        _shader.SetBuffer(kernel, "FluidPositions", Body.PositionsBuf);
        _shader.SetBuffer(kernel, "PredictedPositionsRead", Body.PredictedPositionsBuf[Read]);
        _shader.SetBuffer(kernel, "VelocitiesWrite", Body.VelocitiesBuf[Write]);
        
        _shader.Dispatch(kernel, Groups, 1, 1);
        
        CBUtility.Swap(Body.VelocitiesBuf);
    }
    
    public void ApplyViscosity()
    {
        int kernel = _shader.FindKernel("SolveViscosity");
        
        _shader.SetBuffer(kernel, "VelocitiesRead", Body.VelocitiesBuf[Read]);
        _shader.SetBuffer(kernel, "VelocitiesWrite", Body.VelocitiesBuf[Write]);
        _shader.SetBuffer(kernel, "PredictedPositionsRead", Body.PredictedPositionsBuf[Read]);
        
        _shader.Dispatch(kernel, Groups, 1, 1);
        
        CBUtility.Swap(Body.VelocitiesBuf);
    }
    
    public void UpdatePositions()
    {
        int kernel = _shader.FindKernel("UpdatePositions");
        
        _shader.SetBuffer(kernel, "FluidPositions", Body.PositionsBuf);
        _shader.SetBuffer(kernel, "PredictedPositionsRead", Body.PredictedPositionsBuf[Read]);
        
        _shader.Dispatch(kernel, Groups, 1, 1);
    }
}
