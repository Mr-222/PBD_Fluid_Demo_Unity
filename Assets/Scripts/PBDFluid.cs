using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PBDFluid : MonoBehaviour, IDisposable
{
    [SerializeField] private Mesh sphereMesh;
    [SerializeField] private Material fluidParticleMat;
    [SerializeField] private Material boundaryParticleMat;
    [SerializeField] private bool drawFluidParticle = true;
    [SerializeField] private bool drawBoundaryParticle = false;
    [SerializeField] private bool drawLines = true;
    [SerializeField] private bool run = true;
    
    private FluidBody _fluid;
    private FluidBoundary _boundary;
    private FluidSolver _solver;

    private const float TimeStep = 1f / 60f;
    
    void Start()
    {
        CreateFluid();
        CreateBoundary();
        _solver = new FluidSolver(_fluid, _boundary);
    }
    
    void Update()
    {
        if (run)
        {
            _solver.Step(TimeStep);
        }
        
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
    
    private Vector4[] GetCorners(Bounds b)
    {
        Vector4[] corners = new Vector4[8];
        
        corners[0] = new Vector4(b.min.x, b.min.y, b.min.z, 1);
        corners[1] = new Vector4(b.min.x, b.min.y, b.max.z, 1);
        corners[2] = new Vector4(b.max.x, b.min.y, b.max.z, 1);
        corners[3] = new Vector4(b.max.x, b.min.y, b.min.z, 1);

        corners[4] = new Vector4(b.min.x, b.max.y, b.min.z, 1);
        corners[5] = new Vector4(b.min.x, b.max.y, b.max.z, 1);
        corners[6] = new Vector4(b.max.x, b.max.y, b.max.z, 1);
        corners[7] = new Vector4(b.max.x, b.max.y, b.min.z, 1);

        return corners;
    }
    
    private static IList<int> _cube = new int[]
    {
        0, 1, 1, 2, 2, 3, 3, 0,
        4, 5, 5, 6, 6, 7, 7, 4,
        0, 4, 1, 5, 2, 6, 3, 7
    };
    
    private void DrawBounds(Camera cam, Color color, Bounds bounds)
    {
        Vector4[] corners = GetCorners(bounds);
        DrawLines.LineMode = LINE_MODE.LINES;
        DrawLines.Draw(cam, corners, color, Matrix4x4.identity, _cube);
    }

    private void OnRenderObject()
    {
        if (drawLines)
        {
            Camera cam = Camera.current;
            DrawBounds(cam, Color.green, _fluid.Particles.Bounds);
            DrawBounds(cam, Color.red, _boundary.Particles.Bounds);
        }
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
