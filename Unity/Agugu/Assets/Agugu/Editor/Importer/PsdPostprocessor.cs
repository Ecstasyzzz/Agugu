using System;
using System.IO;
using UnityEditor;

namespace Agugu.Editor
{
    public class PSDPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets,
            string[]                                        deletedAssets,
            string[]                                        movedAssets,
            string[]                                        movedFromAssetPaths)
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

                bool isTracked = AguguConfig.Instance.IsTracked(importedAssetPath);

                if (!isTracked)
                {
                    continue;
                }

                PsdImporter.ImportPsdAsPrefab(importedAssetPath, PsdParser.Parse(importedAssetPath));
            }
        }
    }
}