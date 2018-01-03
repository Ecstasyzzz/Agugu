using System;
using System.IO;

using UnityEngine;
using UnityEditor;

using Ntreev.Library.Psd;


public class PSDImporter
{
    [MenuItem("Agugu/Import Selection")]
    public static void ImportSelection()
    {
        UnityEngine.Object selectedObject = Selection.activeObject;
        string selectedObjectPath = AssetDatabase.GetAssetPath(selectedObject);

        string fileExtension = Path.GetExtension(selectedObjectPath);
        bool isPsdFile = string.Equals(fileExtension, ".psd", 
                                       StringComparison.OrdinalIgnoreCase);

        if (!isPsdFile)
        {
            Debug.LogError("Selected Asset is not a PSD file");
            return;
        }

        using (var document = PsdDocument.Create(selectedObjectPath))
        {
            foreach (var item in document.Childs)
            {
                Debug.Log("LayerName : " + item.Name);
            }
        }
    }
}

