using UnityEngine;
using UnityEditor;

using Agugu.Runtime;

namespace Agugu.Editor
{
    [CustomEditor(typeof(PsdLayerIdTag))]
    public class PsdLayerIdTagEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            GUI.enabled = false;
            DrawDefaultInspector();
            GUI.enabled = true;
        }
    }
}