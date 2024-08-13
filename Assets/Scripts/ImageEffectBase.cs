using UnityEngine;

public class ImageEffect : MonoBehaviour
{
    public Shader shader;

    private Material _material;

    protected virtual void Start()
    {
        if (!shader || !shader.isSupported)
            enabled = false;
    }
    
    protected Material material
    {
        get
        {
            if (_material == null)
            {
                _material = new Material(shader);
                _material.hideFlags = HideFlags.HideAndDontSave;
            }

            return _material;
        }
    }


    protected virtual void OnDisable()
    {
        if (_material)
        {
            DestroyImmediate(_material);
        }
    }
}