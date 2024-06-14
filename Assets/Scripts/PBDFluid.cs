using System;
using UnityEditor;
using UnityEngine;

public class PBDFluid : MonoBehaviour, IDisposable
{
    [SerializeField] private Mesh sphereMesh;
    [SerializeField] private Material fluidParticleMat;
    [SerializeField] private Material boundaryParticleMat;
    [SerializeField] private bool drawFluidParticle = true;
    [SerializeField] private bool drawBoundaryParticle = false;
    
    private FluidBody _fluid;
    private FluidBoundary _boundary;
    
    void Start()
    {
        CreateFluid();
        CreateBoundary();
    }
    
    void Update()
    {
        if (drawFluidParticle)
        {
            _fluid.Draw(Camera.main, sphereMesh, fluidParticleMat, 0); // game view camera
            _fluid.Draw(SceneView.lastActiveSceneView.camera, sphereMesh, fluidParticleMat, 0); // scene view camera
        }

        if (drawBoundaryParticle)
        { 
            _boundary.Draw(Camera.main, sphereMesh, boundaryParticleMat, 0); 
            _boundary.Draw(SceneView.lastActiveSceneView.camera, sphereMesh, boundaryParticleMat, 0);
        }
    }

    private void CreateFluid()
    {
        Bounds bounds = new Bounds();
        var min = new Vector3(-8, 0, -1);
        var max = new Vector3(-4, 8, 2);
        bounds.SetMinMax(min, max);

        _fluid = new FluidBody(bounds, Vector3.zero);
    }

    private void CreateBoundary()
    {
        Bounds innerBounds = new Bounds();
        var min = new Vector3(-8, 0, -2);
        var max = new Vector3(8, 10, 2);
        innerBounds.SetMinMax(min, max);
        
        // 1-layer boundary particle
        // https://sph-tutorial.physics-simulation.org/pdf/SPH_Tutorial.pdf  Chapter 5.1.1
        // The multiple by 1.2 adds a little of extra
        // thickness in case the radius does not evenly
        // divide into the bounds size. You might have
        // particles missing from one side of the source
        // bounds other wise.
        float diameter = ParticleConfig.Diameter;
        min.x -= diameter * 1.2f;
        min.y -= diameter * 1.2f;
        min.z -= diameter * 1.2f;
        
        max.x += diameter * 1.2f;
        max.y += diameter * 1.2f;
        max.z += diameter * 1.2f;

        Bounds outerBounds = new Bounds();
        outerBounds.SetMinMax(min, max);

        _boundary = new FluidBoundary(outerBounds, innerBounds);
    }

    public void Dispose()
    {
        _fluid.Dispose();
        _boundary.Dispose();
    }

    private void OnDestroy()
    {
        Dispose();
    }
}
