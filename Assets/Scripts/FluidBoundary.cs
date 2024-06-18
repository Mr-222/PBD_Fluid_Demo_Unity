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

    // Codebase computes a mass for boundary particle, don't understand the mechanism
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
