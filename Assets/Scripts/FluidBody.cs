using System;
using UnityEngine;
using UnityEngine.Rendering;
using Color = UnityEngine.Color;

public class FluidBody : IDisposable
{
    public ParticlesCPU Particles { get; private set; }
    public ComputeBuffer PositionsBuf { get; private set; }
    public ComputeBuffer[] PredictedPositionsBuf { get; private set; }
    public ComputeBuffer[] VelocitiesBuf { get; private set; }
    public ComputeBuffer DensitiesBuf { get; private set;  }
    public ComputeBuffer LambdasBuf { get; private set; }
    public ComputeBuffer VorticitiesBuf { get; private set; }
    private ComputeBuffer _drawArgsBuf;

    public FluidBody(Bounds bounds, Vector3 velocity)
    {
        Particles = new ParticlesCPU(bounds);

        PositionsBuf = new ComputeBuffer(Particles.NumParticles, 4 * sizeof(float));

        PredictedPositionsBuf = new ComputeBuffer[2];
        PredictedPositionsBuf[0] = new ComputeBuffer(Particles.NumParticles, 4 * sizeof(float));
        PredictedPositionsBuf[1] = new ComputeBuffer(Particles.NumParticles, 4 * sizeof(float));

        VelocitiesBuf = new ComputeBuffer[2];
        VelocitiesBuf[0] = new ComputeBuffer(Particles.NumParticles, 4 * sizeof(float));
        VelocitiesBuf[1] = new ComputeBuffer(Particles.NumParticles, 4 * sizeof(float));
        
        DensitiesBuf = new ComputeBuffer(Particles.NumParticles, sizeof(float));
        LambdasBuf = new ComputeBuffer(Particles.NumParticles, sizeof(float));
        VorticitiesBuf = new ComputeBuffer(Particles.NumParticles, 4 * sizeof(float));
        
        PositionsBuf.SetData(Particles.Positions);
        PredictedPositionsBuf[0].SetData(Particles.Positions);
        PredictedPositionsBuf[1].SetData(Particles.Positions);

        Vector4[] initialVelocities = new Vector4[Particles.NumParticles];
        for (int i = 0; i < initialVelocities.Length; ++i)
            initialVelocities[i] = new Vector4(velocity.x, velocity.y, velocity.z, 0);
        VelocitiesBuf[0].SetData(initialVelocities);
        VelocitiesBuf[1].SetData(initialVelocities);
    }

    public void Draw(Camera cam, Mesh mesh, Material material, int layer)
    {
        if (_drawArgsBuf == null)
            CreateArgsBuffer(mesh.GetIndexCount(0));
        
        material.SetBuffer("_Positions", PositionsBuf);
        material.SetColor("_Color", Color.blue);
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

    public void Dispose()
    {
        CBUtility.Release(PositionsBuf);
        CBUtility.Release(PredictedPositionsBuf);
        CBUtility.Release(VelocitiesBuf);
        CBUtility.Release(DensitiesBuf);
        CBUtility.Release(LambdasBuf);
        CBUtility.Release(VorticitiesBuf);
        CBUtility.Release(_drawArgsBuf);
    }
}
