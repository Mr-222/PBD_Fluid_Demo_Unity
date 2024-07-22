using System.Collections.Generic;
using UnityEngine;

public class MeshVoxelizer : MonoBehaviour
{
    public List<Vector3Int> GridPoints = new List<Vector3Int>();
    public float HalfSize = ParticleConfig.Radius;
    public Vector3 LocalOrigin;

    private void Start()
    {
        VoxelizeMesh();
    }

    public Vector3 PointToPosition(Vector3Int point)
    {
        float size = HalfSize * 2f;
        Vector3 pos = new Vector3(HalfSize + point.x * size, HalfSize + point.y * size, HalfSize + point.z * size);
        return LocalOrigin + transform.TransformPoint(pos);
    }
    
    public void VoxelizeMesh()
    {
        if (!TryGetComponent(out MeshCollider meshCollider))
        {
            meshCollider = this.gameObject.AddComponent<MeshCollider>();
        }
        
        Bounds bounds = meshCollider.bounds;
        Vector3 minExtents = bounds.center - bounds.extents;
        float halfSize = HalfSize;
        Vector3 count = bounds.extents / halfSize;

        int xGridSize = Mathf.CeilToInt(count.x);
        int yGridSize = Mathf.CeilToInt(count.y);
        int zGridSize = Mathf.CeilToInt(count.z);

        GridPoints.Clear();
        LocalOrigin = transform.InverseTransformPoint(minExtents);

        for (int x = 0; x < xGridSize; ++x)
        {
            for (int z = 0; z < zGridSize; ++z)
            {
                for (int y = 0; y < yGridSize; ++y)
                {
                    Vector3 pos = PointToPosition(new Vector3Int(x, y, z));
                    if (Physics.CheckBox(pos, new Vector3(halfSize, halfSize, halfSize)))
                        GridPoints.Add(new Vector3Int(x, y, z));
                }
            }
        }
    }

    public List<Vector4> GetParticlesWorld()
    {
        var particles = new List<Vector4>();
        foreach (Vector3Int pos in GridPoints)
        {
            Vector3 worldPos = PointToPosition(pos);
            particles.Add(worldPos);
        }
        
        return particles;
    }
}
