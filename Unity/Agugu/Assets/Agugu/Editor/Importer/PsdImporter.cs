using System;
using System.IO;

using UnityEngine;
using UnityEditor;


public class PsdImporter
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

        ImportPsdAsPrefab(selectedObjectPath, true);
    }

    public static void ImportPsdAsPrefab(string psdPath, bool keepGameObject)
    {
        UiTreeRoot uiTree = PsdParser.Parse(psdPath);

        string importedTexturesFolder = _GetImportedTexturesSaveFolder(psdPath);
        _EnsureFolder(importedTexturesFolder);
        var saveTextureVisitor = new SaveTextureVisitor(importedTexturesFolder);
        saveTextureVisitor.Visit(uiTree);

        var uguiVisitor = new BuildUguiGameObjectVisitor(default(Rect), null);
        GameObject canvasGameObject = uguiVisitor.Visit(uiTree);

        var prefabPath = _GetImportedPrefabSavePath(psdPath);
        var prefabObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(prefabPath);
        if (prefabObject == null)
        {
            prefabObject = PrefabUtility.CreateEmptyPrefab(prefabPath);
        }

        PrefabUtility.ReplacePrefab(canvasGameObject, prefabObject, ReplacePrefabOptions.ReplaceNameBased);

        if (keepGameObject)
        {
            var prefabGameObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            PrefabUtility.ConnectGameObjectToPrefab(canvasGameObject, prefabGameObject);
        }
        else
        {
            GameObject.DestroyImmediate(canvasGameObject);
        }
    }

    private static string _GetImportedTexturesSaveFolder(string psdPath)
    {
        string psdFolder = Path.GetDirectoryName(psdPath);
        string importedTexturesFolder = Path.Combine(psdFolder, "ImportedTextures");

        return importedTexturesFolder;
    }

    private static string _GetImportedPrefabSavePath(string psdPath)
    {
        string psdFolder = Path.GetDirectoryName(psdPath);
        string psdName = Path.GetFileNameWithoutExtension(psdPath);

        return Path.Combine(psdFolder, string.Format("{0}.prefab", psdName)).Replace("\\", "/");
    }

    private static void _EnsureFolder(string folderPath)
    {
        Directory.CreateDirectory(folderPath);
    }
}