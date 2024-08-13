using UnityEngine;

// https://developer.download.nvidia.com/presentations/2010/gdc/Direct3D_Effects.pdf
public class FluidRenderer : ImageEffect
{
    public Color m_color = Color.blue;
    public Color m_specular = Color.white;
    public Color m_absortption = Color.gray;

    public float m_shininess = 64f;
    public float m_reflection = 0.3f;
    public float m_refraction = 0.7f;
    public float m_thickness = 0.6f;
    public float m_indexOfRefraction = 0.01f;

    public float m_xFactor = 0.001f;
    public float m_YFactor = 0.001f;
    
    public RenderTexture m_thicknessTexture;
    public RenderTexture m_blurredDepthTexture;
    
    public Cubemap m_cubemap;

    public bool draw = false;

    protected override void Start()
    {
        base.Start();
        
        material.SetTexture("_BlurredDepthTex", m_blurredDepthTexture);
        material.SetTexture("_ThicknessTex", m_thicknessTexture);
        material.SetTexture("_Cube", m_cubemap);
    }
    
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (draw)
        {
            material.SetFloat("_XFactor", m_xFactor);
            material.SetFloat("_YFactor", m_YFactor);

            material.SetColor("_Color", m_color);
            material.SetColor("_Specular", m_specular);
            material.SetColor("_Absorption", m_absortption);

            material.SetFloat("_Shininess", m_shininess);
            material.SetFloat("_Reflection", m_reflection);
            material.SetFloat("_Thickness", m_thickness);

            material.SetFloat("_Refraction", m_refraction);
            material.SetFloat("_IndexOfRefraction", m_indexOfRefraction);
        
            Graphics.Blit(source, destination, material);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}