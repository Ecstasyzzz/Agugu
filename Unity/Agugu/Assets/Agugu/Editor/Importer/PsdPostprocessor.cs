using System;
using System.IO;

using UnityEditor;

public class PSDPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, 
                                       string[] deletedAssets, 
                                       string[] movedAssets, 
                                       string[] movedFromAssetPaths)
    {
        foreach (string importedAssetPath in importedAssets)
        {
            string fileExtension = Path.GetExtension(importedAssetPath);
            bool isPsdFile = string.Equals(fileExtension, ".psd",
                                           StringComparison.OrdinalIgnoreCase);

            if (!isPsdFile)
            {
                continue;
            }

            if (!AguguConfig.Instance.IsTracked(importedAssetPath))
            {
                continue;
            }

            PsdImporter.ImportPsdAsPrefab(importedAssetPath, PsdParser.Parse(importedAssetPath));
        }
    }
}