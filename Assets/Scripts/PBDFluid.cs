using System;
using UnityEngine;

public class PBDFluid : MonoBehaviour, IDisposable
{
    private FluidBody _fluid;
    [SerializeField] private Mesh _sphereMesh;
    [SerializeField] private Material _fluidParticleMat;
    
    void Start()
    {
        CreateFluid();
    }
    
    void Update()
    {
        _fluid.Draw(Camera.main, _sphereMesh, _fluidParticleMat, 0);
    }

    private void CreateFluid()
    {
        Bounds bounds = new Bounds();
        Vector3 min = new Vector3(-8, 0, -1);
        Vector3 max = new Vector3(-4, 8, 2);
        bounds.SetMinMax(min, max);

        _fluid = new FluidBody(bounds, Vector3.zero);
    }

    private void OnRenderObject()
    {
        
    }

    public void Dispose()
    {
        _fluid.Dispose();
    }

    private void OnDestroy()
    {
        Dispose();
    }
}
