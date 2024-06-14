using System.Collections.Generic;
using UnityEngine;

public class ParticlesCPU
{
    public Bounds Bounds { get; private set; }
    private List<Bounds> _exclusion;
    public int NumParticles => Positions.Count;
    public List<Vector4> Positions { get; private set; }

    public ParticlesCPU(Bounds bounds)
    {
        Bounds = bounds;
        _exclusion = new List<Bounds>();
        CreateParticles();
    }

    public ParticlesCPU(Bounds bounds, Bounds exclusion)
    {
        Bounds = bounds;
        _exclusion = new List<Bounds>();
        _exclusion.Add(exclusion);
        CreateParticles();
    }
    
    public void CreateParticles()
    {
        float r = ParticleConfig.Radius;
        float d = ParticleConfig.Diameter;
        
        // Avoid seams between boundaries or fluid will leak out
        int numX = (int)((Bounds.size.x + r) / d);
        int numY = (int)((Bounds.size.y + r) / d);
        int numZ = (int)((Bounds.size.z + r) / d);

        Positions = new List<Vector4>(numX * numY * numZ);

        for (int z = 0; z < numZ; z++)
        {
            for (int y = 0; y < numY; y++)
            {
                for (int x = 0; x < numX; x++)
                {
                    Vector4 pos = Vector4.zero;
                    pos.x = Bounds.min.x + r + x * d;
                    pos.y = Bounds.min.y + r + y * d;
                    pos.z = Bounds.min.z + r + z * d;

                    bool exclude = false;
                    for (int i = 0; i < _exclusion.Count; ++i)
                    {
                        if (_exclusion[i].Contains(pos))
                        {
                            exclude = true;
                            break;
                        }
                    }
                    
                    if (!exclude)
                        Positions.Add(pos);
                }
            }
        }
    }
}
