using System;
using UnityEngine;

public class FluidSolver : IDisposable
{
    private const int Threads = 128;
    private const int Read = 0;
    private const int Write = 1;
    public int Groups { get; private set; }
    public int SubSteps { get; set; }
    public int ConstraintIterations { get; set; }
    public float Viscosity;
    // Constraint relaxation 
    // https://mmacklin.com/pbf_sig_preprint.pdf Chapter 3
    public float Relaxation = 60.0f;
    // Surface tension
    // https://mmacklin.com/pbf_sig_preprint.pdf Chapter 4
    public float K = 0.01f;
    public float N = 4;
    // Vorticity Confinement
    // https://mmacklin.com/pbf_sig_preprint.pdf Chapter 5
    public float Vorticity = 1e-40f;
    
    public FluidBoundary Boundary { get; private set; }
    public FluidBody Body { get; private set; }
    public SmoothingKernel Kernel { get; private set; }

    private ComputeShader _shader;
    private SpatialHashing _grid;

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

        _grid = new SpatialHashing(Boundary.Particles.Bounds, Body.Particles.NumParticles + Boundary.Particles.NumParticles,
            Body.Particles.NumParticles, 2 * ParticleConfig.Diameter);
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
        _shader.SetFloat("SurfaceTensionDenom", Kernel.Poly6(new Vector3(1, 0, 0) * 0.1f * Kernel.H));
        _shader.SetFloat("Poly6Coeff", Kernel.Poly6Coeff);
        _shader.SetFloat("SpikyGradCoeff", Kernel.SpikyGradCoeff);
        _shader.SetFloat("ViscLapCoeff", Kernel.ViscLaplacianCoeff);
        
        _shader.SetVector("Gravity", new Vector4(0f, -9.81f, 0f, 0f));
        _shader.SetFloat("DeltaTime", dt);
        _shader.SetFloat("RestDensity", ParticleConfig.RestDensity);
        _shader.SetFloat("Viscosity", Viscosity);
        _shader.SetFloat("Mass", ParticleConfig.Mass);
        _shader.SetInt("NumFluidParticles", Body.Particles.NumParticles);
        _shader.SetInt("NumBoundaryParticles", Boundary.Particles.NumParticles);
        _shader.SetInt("NumTotalParticles", Body.Particles.NumParticles + Boundary.Particles.NumParticles);
        _shader.SetFloat("Psi", Boundary.Psi);
        _shader.SetFloat("Epsilon", Relaxation);
        _shader.SetFloat("K", K);
        _shader.SetFloat("N", N);
        _shader.SetFloat("Vorticity", Vorticity);
        _shader.SetVector("GridBoundsMin", _grid.Bounds.min);
        _shader.SetVector("GridDimension", _grid.Dimension);
        _shader.SetFloat("CellSize", _grid.CellSize);
        
        for (int i = 0; i < SubSteps; i++)
        {
            PredictPositions();
            _grid.GPUSort(Body.PredictedPositionsBuf[Read], Boundary.PositionsBuf);
            SolveConstraints();
            UpdateVelocities();
            ApplyViscosity();
            VorticityConfinement();
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
    }
    
    public void SolveConstraints()
    {
        int computeKernel = _shader.FindKernel("ComputeLambda");
        int solveKernel = _shader.FindKernel("SolveConstraint");
        
        _shader.SetBuffer(computeKernel, "Densities", Body.DensitiesBuf);
        _shader.SetBuffer(computeKernel, "Lambdas", Body.LambdasBuf);
        _shader.SetBuffer(computeKernel, "BoundaryPositions", Boundary.PositionsBuf);
        _shader.SetBuffer(computeKernel, "BinCountsScanned", _grid.BinCountsScannedBuffer);
        _shader.SetBuffer(computeKernel, "ParticleIndices", _grid.ParticleIndicesBuffer);
        
        _shader.SetBuffer(solveKernel, "BoundaryPositions", Boundary.PositionsBuf);
        _shader.SetBuffer(solveKernel, "Lambdas", Body.LambdasBuf);
        _shader.SetBuffer(solveKernel, "BinCountsScanned", _grid.BinCountsScannedBuffer);
        _shader.SetBuffer(solveKernel, "ParticleIndices", _grid.ParticleIndicesBuffer);

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
        int kernel = _shader.FindKernel("SolveViscosityAndCurl");
        
        _shader.SetBuffer(kernel, "VelocitiesRead", Body.VelocitiesBuf[Read]);
        _shader.SetBuffer(kernel, "VelocitiesWrite", Body.VelocitiesBuf[Write]);
        _shader.SetBuffer(kernel, "PredictedPositionsRead", Body.PredictedPositionsBuf[Read]);
        _shader.SetBuffer(kernel, "BinCountsScanned", _grid.BinCountsScannedBuffer);
        _shader.SetBuffer(kernel, "ParticleIndices", _grid.ParticleIndicesBuffer);
        _shader.SetBuffer(kernel, "VorticitiesWrite", Body.VorticitiesBuf);
        _shader.SetBuffer(kernel,"BoundaryPositions", Boundary.PositionsBuf);
        
        _shader.Dispatch(kernel, Groups, 1, 1);
        
        CBUtility.Swap(Body.VelocitiesBuf);
    }

    public void VorticityConfinement()
    {
        int kernel = _shader.FindKernel("SolveVorticity");
        
        _shader.SetBuffer(kernel, "VelocitiesRead", Body.VelocitiesBuf[Read]);
        _shader.SetBuffer(kernel, "VelocitiesWrite", Body.VelocitiesBuf[Write]);
        _shader.SetBuffer(kernel, "PredictedPositionsRead", Body.PredictedPositionsBuf[Read]);
        _shader.SetBuffer(kernel, "BinCountsScanned", _grid.BinCountsScannedBuffer);
        _shader.SetBuffer(kernel, "ParticleIndices", _grid.ParticleIndicesBuffer);
        _shader.SetBuffer(kernel, "VorticitiesRead", Body.VorticitiesBuf);
        _shader.SetBuffer(kernel,"BoundaryPositions", Boundary.PositionsBuf);
        
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

    public void Dispose()
    {
        _grid.Dispose();
    }
}
