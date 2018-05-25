using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PsdLayerIdTag))]
public class PsdLayerIdTagEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GUI.enabled = false;
        DrawDefaultInspector();
        GUI.enabled = true;
    }
}
