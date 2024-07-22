using System;
using UnityEngine;
using UnityEngine.Rendering;

public class FluidBoundary : IDisposable
{
    public ParticlesCPU Particles { get; private set; }
    public ComputeBuffer PositionsBuf { get; private set; }
    private ComputeBuffer _drawArgsBuf;
    public float Psi { get; private set; }

    public FluidBoundary(Bounds outerBounds, Bounds innerBounds)
    {
        Particles = new ParticlesCPU(outerBounds, innerBounds);
        
        // Find all MeshVoxelizers in the scene, add them to boundary particles
        MeshVoxelizer[] rigidbodies = GameObject.FindObjectsOfType<MeshVoxelizer>();
        foreach (var rb in rigidbodies)
        {
            Particles.Positions.AddRange(rb.GetParticlesWorld());
        }

        PositionsBuf = new ComputeBuffer(Particles.NumParticles, 4 * sizeof(float));
        PositionsBuf.SetData(Particles.Positions);
        
        ComputePsi();
    }

    public void Draw(Camera cam, Mesh mesh, Material material, int layer)
    {
        if (_drawArgsBuf == null)
            CreateArgsBuffer(mesh.GetIndexCount(0));
        
        material.SetBuffer("_Positions", PositionsBuf);
        material.SetColor("_Color", Color.red);
        material.SetFloat("_Diameter", ParticleConfig.Diameter);

        ShadowCastingMode castMode = ShadowCastingMode.On;
        bool receiveShadow = true;
        
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, Particles.Bounds, _drawArgsBuf, 0, null, castMode, receiveShadow, layer, cam);
    }

    public void CreateArgsBuffer(uint indexCount)
    {
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        args[0] = indexCount;
        args[1] = (uint)Particles.NumParticles;

        _drawArgsBuf = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        _drawArgsBuf.SetData(args);
    }

    // Precompute a coefficient to make up for imperfect sampling pattern since we are using one-layer boundary instead of two
    // Similar idea can be found in paper :
    // Smoothed Particle Hydrodynamics Techniques for the Physics Based Simulation of Fluids and Solids
    // https://sph-tutorial.physics-simulation.org/pdf/SPH_Tutorial.pdf Chapter 5.1.1
    private void ComputePsi()
    {
        var kernel = new SmoothingKernel(ParticleConfig.Radius * 4f);
        float delta = kernel.Poly6(Vector3.zero);
        float volume = 1f / delta;
        Psi = ParticleConfig.RestDensity * volume;
    }
    
    public void Dispose()
    {
        CBUtility.Release(PositionsBuf);
        CBUtility.Release(_drawArgsBuf);
    }
}
