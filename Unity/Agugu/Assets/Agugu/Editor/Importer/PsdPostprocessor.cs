using System;
using System.IO;

using UnityEditor;

public class PSDPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, 
                                       string[] deletedAssets, 
                                       string[] movedAssets, 
                                       string[] movedFromAssetPaths)
    {
        foreach (string importedAssetPath in importedAssets)
        {
            string fileExtension = Path.GetExtension(importedAssetPath);
            bool isPsdFile = string.Equals(fileExtension, ".psd",
                                           StringComparison.OrdinalIgnoreCase);
            bool isTracked = AguguConfig.Instance.IsTracked(importedAssetPath);

            if (!isPsdFile || !isTracked)
            {
                continue;
            }

            PsdImporter.ImportPsdAsPrefab(importedAssetPath, PsdParser.Parse(importedAssetPath));
        }
    }
}