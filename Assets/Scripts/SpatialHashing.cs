using System;
using UnityEngine;

public class SpatialHashing : IDisposable
{
    private int Threads = 512;
    public int GroupsBin { get; private set; }
    public int GroupsParticle { get; private set; }
    public int TotalParticles { get; private set; }
    public int FluidParticles { get; private set; }
    public Bounds Bounds { get; private set; }
    public float CellSize { get; private set; }
    public Vector3 Dimension { get; private set; }
    public int Bins { get; private set; }
    
    private ScanHelper _scanHelper;
    
    private ComputeShader _shader;

    public ComputeBuffer BinCountsBuffer { get; private set; }
    public ComputeBuffer BinCountsScannedBuffer { get; private set; }
    public ComputeBuffer ParticleIndicesBuffer { get; private set; }

    public SpatialHashing(Bounds bounds, int totalParticles, int fluidParticles, float cellSize)
    {
        TotalParticles = totalParticles;
        FluidParticles = fluidParticles;
        CellSize = cellSize;
        _scanHelper = new ScanHelper();
        _shader = Resources.Load("SpatialHashing") as ComputeShader;

        BinCountsBuffer = new ComputeBuffer(Bins, sizeof(int));
        BinCountsScannedBuffer = new ComputeBuffer(Bins + 1, sizeof(int));
        ParticleIndicesBuffer = new ComputeBuffer(TotalParticles, sizeof(int));

        Vector3 min, max;
        min = bounds.min;

        max.x = min.x + Mathf.Ceil(bounds.size.x / cellSize);
        max.y = min.y + Mathf.Ceil(bounds.size.y / cellSize);
        max.z = min.z + Mathf.Ceil(bounds.size.z / cellSize);

        Bounds = new Bounds();
        Bounds.SetMinMax(min, max);

        int width = (int)Bounds.size.x;
        int height = (int)Bounds.size.y;
        int depth = (int)Bounds.size.z;
        Dimension = new Vector3(width, height, depth);
        Bins = width * height * depth;

        GroupsBin = Mathf.CeilToInt((float)Bins / Threads);
        GroupsParticle = Mathf.CeilToInt((float)TotalParticles / Threads);
    }

    public void GPUSort(ComputeBuffer fluidPositionsBuf, ComputeBuffer boundaryPositionBuf)
    {
        int resetKernel = _shader.FindKernel("FillZeroes");
        int countKernel = _shader.FindKernel("Count");
        int sortKernel = _shader.FindKernel("Sort");
        
        _shader.SetInt("NumTotalParticles", TotalParticles);
        _shader.SetInt("NumFluidParticles", FluidParticles);
        _shader.SetInt("NumBins", Bins);
        _shader.SetVector("GridBoundsMin", Bounds.min);
        _shader.SetVector("GridDimension", Dimension);
        _shader.SetFloat("CellSize", CellSize);
        
        _shader.SetBuffer(resetKernel, "BinCounts", BinCountsBuffer);
        
        _shader.SetBuffer(countKernel, "FluidPositions", fluidPositionsBuf);
        _shader.SetBuffer(countKernel, "BoundaryPositions", boundaryPositionBuf);
        _shader.SetBuffer(countKernel, "BinCounts", BinCountsBuffer);
        
        _shader.SetBuffer(sortKernel, "BinCounts", BinCountsBuffer);
        _shader.SetBuffer(sortKernel, "FluidPositions", fluidPositionsBuf);
        _shader.SetBuffer(sortKernel, "BoundaryPositions", boundaryPositionBuf);
        _shader.SetBuffer(sortKernel, "BinCountsScanned", BinCountsScannedBuffer);
        _shader.SetBuffer(sortKernel, "ParticleIndicesRead", ParticleIndicesBuffer);
        
        _shader.Dispatch(resetKernel, GroupsBin, 1, 1);
        _shader.Dispatch(countKernel, GroupsParticle, 1, 1);
        _scanHelper.InclusiveScan(Bins, Resources.Load("PrefixSum") as ComputeShader, 
            BinCountsBuffer, BinCountsScannedBuffer);
        _shader.Dispatch(sortKernel, GroupsParticle, 1, 1);
    }

    public void Dispose()
    {
        _scanHelper.Release();
        CBUtility.Release(BinCountsBuffer);
        CBUtility.Release(BinCountsScannedBuffer);
        CBUtility.Release(ParticleIndicesBuffer);
    }
}
