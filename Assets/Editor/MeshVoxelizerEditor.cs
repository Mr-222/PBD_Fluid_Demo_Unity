using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MeshVoxelizer))]
public class MeshVoxelizerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        MeshVoxelizer voxelizer = (MeshVoxelizer)target;
        if (GUILayout.Button("Voxelize Mesh"))
        {
            voxelizer.VoxelizeMeshWithGPU();
        }
    }
}