using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TestTerrain))]
public class TerrainEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TestTerrain map = target as TestTerrain;

        if (DrawDefaultInspector())
        {
            map.Generate();
        }

        if (GUILayout.Button("Generate"))
        {
            map.Generate();
        }
    }
}
