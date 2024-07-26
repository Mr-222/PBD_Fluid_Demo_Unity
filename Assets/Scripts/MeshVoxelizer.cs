using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

[ExecuteAlways]
public class MeshVoxelizer : MonoBehaviour
{
    [SerializeField] MeshFilter _meshFilter;
    [SerializeField] MeshCollider _meshCollider;
    [SerializeField] float _halfSize = 0.05f;
    [SerializeField] Vector3 _boundsMin;

    [SerializeField] int _gridPointCount;

    [SerializeField] Material _blocksMaterial;

    ComputeShader _voxelizeComputeShader;
    ComputeBuffer _voxelPointsBuffer;
    ComputeBuffer _meshVerticesBuffer;
    ComputeBuffer _meshTrianglesBuffer;

    ComputeBuffer _pointsArgsBuffer;
    ComputeBuffer _blocksArgsBuffer;

    Mesh _voxelMesh;
    
    [SerializeField] bool _drawBlocks;

    static readonly int BoundsMin = Shader.PropertyToID("_BoundsMin");
    static readonly int VoxelGridPoints = Shader.PropertyToID("_VoxelGridPoints");

    Vector4[] _gridPoints;
    public List<Vector4> VoxelPositions;

    void OnEnable()
    {
        _voxelizeComputeShader = Resources.Load("VoxelizeMesh") as ComputeShader;
        _pointsArgsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        _blocksArgsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        
        VoxelizeMeshWithGPU();
    }

    void OnDisable()
    {
        _pointsArgsBuffer?.Dispose();
        _blocksArgsBuffer?.Dispose();

        _voxelPointsBuffer?.Dispose();

        _meshTrianglesBuffer?.Dispose();
        _meshVerticesBuffer?.Dispose();
    }

    void Update()
    {
        if (_drawBlocks)
        {
            _blocksArgsBuffer.SetData(new[] {_voxelMesh.triangles.Length, _gridPointCount, 0, 0, 0});
            _blocksMaterial.SetBuffer("_Positions", _voxelPointsBuffer);
            _blocksMaterial.SetFloat("_Diameter", 2.0f * _halfSize);
            Graphics.DrawMeshInstancedIndirect(_voxelMesh, 0, _blocksMaterial, _meshCollider.bounds, _blocksArgsBuffer);
        }
    }

    public void VoxelizeMeshWithGPU()
    {
        Profiler.BeginSample("Voxelize Mesh (GPU)");

        Bounds bounds = _meshCollider.bounds;
        _boundsMin = bounds.min;

        var voxelCount = new Vector3(bounds.extents.x / _halfSize, bounds.extents.y / _halfSize, bounds.extents.z / _halfSize);
        int xGridSize = Mathf.CeilToInt(voxelCount.x);
        int yGridSize = Mathf.CeilToInt(voxelCount.y);
        int zGridSize = Mathf.CeilToInt(voxelCount.z);

        bool resizeVoxelPointsBuffer = false;
        if (_gridPoints == null || _gridPoints.Length != xGridSize * yGridSize * zGridSize ||
            _voxelPointsBuffer == null)
        {
            _gridPoints = new Vector4[xGridSize * yGridSize * zGridSize];
            resizeVoxelPointsBuffer = true;
        }

        if (resizeVoxelPointsBuffer || _voxelPointsBuffer == null || !_voxelPointsBuffer.IsValid())
        {
            _voxelPointsBuffer?.Dispose();
            _voxelPointsBuffer = new ComputeBuffer(xGridSize * yGridSize * zGridSize, 4 * sizeof(float));
        }

        if (resizeVoxelPointsBuffer)
        {
            _voxelPointsBuffer.SetData(_gridPoints);

            _voxelMesh = GenerateVoxelMesh(0.5f);
        }

        if (_meshVerticesBuffer == null || !_meshVerticesBuffer.IsValid())
        {
            _meshVerticesBuffer?.Dispose();

            var sharedMesh = _meshFilter.sharedMesh;
            _meshVerticesBuffer = new ComputeBuffer(sharedMesh.vertexCount, 3 * sizeof(float));
            _meshVerticesBuffer.SetData(sharedMesh.vertices);
        }

        if (_meshTrianglesBuffer == null || !_meshTrianglesBuffer.IsValid())
        {
            _meshTrianglesBuffer?.Dispose();

            var sharedMesh = _meshFilter.sharedMesh;
            _meshTrianglesBuffer = new ComputeBuffer(sharedMesh.triangles.Length, sizeof(int));
            _meshTrianglesBuffer.SetData(sharedMesh.triangles);
        }

        var voxelizeKernel = _voxelizeComputeShader.FindKernel("VoxelizeMesh");
        _voxelizeComputeShader.SetInt("_GridWidth", xGridSize);
        _voxelizeComputeShader.SetInt("_GridHeight", yGridSize);
        _voxelizeComputeShader.SetInt("_GridDepth", zGridSize);

        var scale = transform.localScale;
        _voxelizeComputeShader.SetVector("_CellHalfSize", new Vector4(_halfSize , _halfSize , _halfSize , 0.0f));

        _voxelizeComputeShader.SetBuffer(voxelizeKernel, VoxelGridPoints, _voxelPointsBuffer);
        _voxelizeComputeShader.SetBuffer(voxelizeKernel, "_MeshVertices", _meshVerticesBuffer);
        _voxelizeComputeShader.SetBuffer(voxelizeKernel, "_MeshTriangleIndices", _meshTrianglesBuffer);
        _voxelizeComputeShader.SetInt("_TriangleCount", _meshFilter.sharedMesh.triangles.Length);

        _voxelizeComputeShader.SetVector(BoundsMin, _boundsMin);
        _voxelizeComputeShader.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);

        _voxelizeComputeShader.GetKernelThreadGroupSizes(voxelizeKernel, out uint xGroupSize, out uint yGroupSize,
            out uint zGroupSize);

        _voxelizeComputeShader.Dispatch(voxelizeKernel,
            Mathf.CeilToInt(xGridSize / (float) xGroupSize),
            Mathf.CeilToInt(yGridSize / (float) yGroupSize),
            Mathf.CeilToInt(zGridSize / (float) zGroupSize));
        _gridPointCount = _voxelPointsBuffer.count;
        _voxelPointsBuffer.GetData(_gridPoints);

        GenerateVoxels();
        
        Profiler.EndSample();
    }

    static Mesh GenerateVoxelMesh(float size)
    {
        var mesh = new Mesh();
        Vector3[] vertices =
        {
            //Front
            new Vector3(-size, -size, -size),       // Front Bottom Left    0
            new Vector3(size, -size, -size),    // Front Bottom Right   1
            new Vector3(size, size, -size), // Front Top Right      2
            new Vector3(-size, size, -size),    // Front Top Left       3

            //Top
            new Vector3(size, size, -size),     // Front Top Right      4
            new Vector3(-size, size, -size),        // Front Top Left          5
            new Vector3(-size, size, size),     // Back Top Left        6
            new Vector3(size, size, size),  // Back Top Right    7

            //Right
            new Vector3(size, -size, -size),        // Front Bottom Right      8
            new Vector3(size, size, -size),     // Front Top Right      9
            new Vector3(size, size, size),  // Back Top Right    10
            new Vector3(size, -size, size),     // Back Bottom Right    11

            //Left
            new Vector3(-size, -size, -size),       // Front Bottom Left          12
            new Vector3(-size, size, -size),    // Front Top Left          13
            new Vector3(-size, size, size), // Back Top Left        14
            new Vector3(-size, -size, size),    // Back Bottom Left        15

            //Back
            new Vector3(-size, size, size),     // Back Top Left        16
            new Vector3(size, size, size),  // Back Top Right    17
            new Vector3(size, -size, size),     // Back Bottom Right    18
            new Vector3(-size, -size, size),        // Back Bottom Left        19

            //Bottom
            new Vector3(-size, -size, -size),       // Front Bottom Left          20
            new Vector3(size, -size, -size),    // Front Bottom Right      21
            new Vector3(size, -size, size), // Back Bottom Right    22
            new Vector3(-size, -size, size)     // Back Bottom Left         23
        };

        int[] triangles =
        {
            //Front
            0, 2, 1,
            0, 3, 2,

            // Top
            4, 5, 6,
            4, 6, 7,

            // Right
            8, 9, 10,
            8, 10, 11,

            // Left
            12, 15, 14,
            12, 14, 13,

            // Back
            17, 16, 19,
            17, 19, 18,

            // Bottom
            20, 22, 23,
            20, 21, 22
        };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    void GenerateVoxels()
    {
        VoxelPositions.Clear();
        foreach (Vector4 pos in _gridPoints)
        {
            if (pos.w != 0)
            {
                VoxelPositions.Add(pos);
            }
        }
    }
}