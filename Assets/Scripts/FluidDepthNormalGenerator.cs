using System;
using UnityEngine;

// https://developer.download.nvidia.com/presentations/2010/gdc/Direct3D_Effects.pdf
public class FluidDepthNormalGenerator : MonoBehaviour, IDisposable
{
    private int _particlesCount;
    
    public RenderTexture m_depthTexture;
    public RenderTexture m_blurredDepthTexture;
    public RenderTexture m_blurredDepthTempTexture;
    public RenderTexture m_thicknessTexture;

    public float m_pointRadius = 1.0f;
    
    public float m_blurScale = 1f;
    public int m_blurRadius = 10;
    public float m_blurDepthFalloff = 100.0f;
    
    private Material m_depthMaterial;
    private Material m_blurDepthMaterial;
    private Material m_thicknessMaterial;
    
    private ComputeBuffer m_quadVerticesBuffer;

    public void Init(int numParticle, ComputeBuffer positionsBuf)
    {
        _particlesCount = numParticle;
        
        m_depthMaterial = new Material(Shader.Find("ScreenSpaceFluids/DrawDepth"));
        m_thicknessMaterial = new Material(Shader.Find("ScreenSpaceFluids/DrawThickness"));
        m_blurDepthMaterial = new Material(Shader.Find("ScreenSpaceFluids/BlurDepth"));

        m_depthMaterial.hideFlags = HideFlags.HideAndDontSave;
        m_thicknessMaterial.hideFlags = HideFlags.HideAndDontSave;
        m_blurDepthMaterial.hideFlags = HideFlags.HideAndDontSave;

        m_quadVerticesBuffer = new ComputeBuffer(6, 16);
        // To generate particle depth texture, first draw a quad then discard pixels outside circle in pixel shader
        m_quadVerticesBuffer.SetData(new[]
        {
            new Vector4(-0.5f, 0.5f),
            new Vector4(0.5f, 0.5f),
            new Vector4(0.5f, -0.5f),
            new Vector4(0.5f, -0.5f),
            new Vector4(-0.5f, -0.5f),
            new Vector4(-0.5f, 0.5f),
        });

        m_depthMaterial.SetBuffer("buf_Positions", positionsBuf);
        m_depthMaterial.SetBuffer("buf_Vertices", m_quadVerticesBuffer);

        m_thicknessMaterial.SetBuffer("buf_Positions", positionsBuf);
        m_thicknessMaterial.SetBuffer("buf_Vertices", m_quadVerticesBuffer);
    }

    public void Draw(ComputeBuffer positionsBuf)
    {
        var target = Camera.current.targetTexture;
        
        DrawDepth(positionsBuf);

        BlurDepth();

        DrawThickness(positionsBuf);
        
        Graphics.SetRenderTarget(target);
    }

    void DrawDepth(ComputeBuffer positionsBuf)
    {
        m_depthMaterial.SetFloat("_PointRadius", m_pointRadius);
        
        m_depthMaterial.SetBuffer("_Positions", positionsBuf);
        m_depthMaterial.SetBuffer("_Vertices", m_quadVerticesBuffer);

        Graphics.SetRenderTarget(m_depthTexture);
        GL.Clear(true, true, Color.white);

        m_depthMaterial.SetPass(0);
        
        Graphics.DrawProceduralNow(MeshTopology.Triangles, 6, _particlesCount);
    }

    void BlurDepth()
    {
        m_blurDepthMaterial.SetTexture("_DepthTex", m_depthTexture);

        m_blurDepthMaterial.SetInt("radius", m_blurRadius);
        m_blurDepthMaterial.SetFloat("blurDepthFalloff", m_blurDepthFalloff);

        m_blurDepthMaterial.SetTexture("_DepthTex", m_depthTexture);
        m_blurDepthMaterial.SetFloat("scaleX", 1.0f / 1024 * m_blurScale);
        m_blurDepthMaterial.SetFloat("scaleY", 0.0f);
        Graphics.Blit(m_depthTexture, m_blurredDepthTempTexture, m_blurDepthMaterial);

        m_blurDepthMaterial.SetTexture("_DepthTex", m_blurredDepthTempTexture);
        m_blurDepthMaterial.SetFloat("scaleX", 0.0f);
        m_blurDepthMaterial.SetFloat("scaleY", 1.0f / 1024 * m_blurScale);
        Graphics.Blit(m_blurredDepthTempTexture, m_blurredDepthTexture, m_blurDepthMaterial);
    }

    void DrawThickness(ComputeBuffer positionsBuf)
    {
        m_thicknessMaterial.SetFloat("_PointRadius", m_pointRadius);

        m_thicknessMaterial.SetBuffer("_Positions", positionsBuf);
        m_thicknessMaterial.SetBuffer("_Vertices", m_quadVerticesBuffer);

        Graphics.SetRenderTarget(m_thicknessTexture);
        GL.Clear(true, true, Color.black);

        m_thicknessMaterial.SetPass(0);

        Graphics.DrawProceduralNow(MeshTopology.Triangles, 6, _particlesCount);
    }

    public void Dispose()
    {
        CBUtility.Release(m_quadVerticesBuffer);
    }
}