using System;
using System.IO;

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;


public class PsdImporter
{
    [MenuItem("Agugu/Import Selection")]
    public static void ImportSelection()
    {
        string psdPath = _GetSelectedPsdPath();

        if (!string.IsNullOrEmpty(psdPath))
        {
            UiTreeRoot uiTree = PsdParser.Parse(psdPath);
            ImportPsdAsPrefab(psdPath, uiTree);
        }
    }

    private static string _GetSelectedPsdPath()
    {
        UnityEngine.Object selectedObject = Selection.activeObject;
        string selectedObjectPath = AssetDatabase.GetAssetPath(selectedObject);

        string fileExtension = Path.GetExtension(selectedObjectPath);
        bool isPsdFile = string.Equals(fileExtension, ".psd",
            StringComparison.OrdinalIgnoreCase);

        if (!isPsdFile)
        {
            Debug.LogError("Selected Asset is not a PSD file");
            return string.Empty;
        }

        return selectedObjectPath;
    }

    [MenuItem("Agugu/Import Selection With Canvas")]
    public static void ImportSelectionWithCanvas()
    {
        string psdPath = _GetSelectedPsdPath();

        if (!string.IsNullOrEmpty(psdPath))
        {
            UiTreeRoot uiTree = PsdParser.Parse(psdPath);

            var canvasGameObject = _CreateCanvasGameObject(uiTree.Width, uiTree.Height);
            var canvasRectTransform = canvasGameObject.GetComponent<RectTransform>();
            canvasRectTransform.ForceUpdateRectTransforms();

            ImportPsdAsPrefab(psdPath, uiTree, true, canvasRectTransform);
        }
    }

    private static GameObject _CreateCanvasGameObject(float width, float height)
    {
        var canvasGameObject = new GameObject("Canvas");

        var canvas = canvasGameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var canvasScaler = canvasGameObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.referenceResolution = new Vector2(width, height);
        canvasScaler.matchWidthOrHeight = 0;

        var graphicRaycaster = canvasGameObject.AddComponent<GraphicRaycaster>();

        return canvasGameObject;
    }

    public static void ImportPsdAsPrefab(string psdPath, 
                                         UiTreeRoot uiTree, 
                                         bool keepGameObject = false, 
                                         Transform parent = null)
    {
        _SaveTextureAsAsset(psdPath, uiTree);

        GameObject uiGameObject = _BuildUguiGameObject(uiTree);
        if (parent != null)
        {
            uiGameObject.GetComponent<Transform>().SetParent(parent, worldPositionStays: false);
        }

        var prefabPath = _GetImportedPrefabSavePath(psdPath);
        _UpdatePrefab(prefabPath, uiGameObject);
        

        if (keepGameObject)
        {
            var prefabGameObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            PrefabUtility.ConnectGameObjectToPrefab(uiGameObject, prefabGameObject);

            
        }
        else
        {
            GameObject.DestroyImmediate(uiGameObject);
        }
    }

    public static void _SaveTextureAsAsset(string psdPath, UiTreeRoot uiTree)
    {
        string importedTexturesFolder = _GetImportedTexturesSaveFolder(psdPath);
        _EnsureFolder(importedTexturesFolder);
        var saveTextureVisitor = new SaveTextureVisitor(importedTexturesFolder);
        saveTextureVisitor.Visit(uiTree);
    }

    private static string _GetImportedTexturesSaveFolder(string psdPath)
    {
        string psdFolder = Path.GetDirectoryName(psdPath);
        string psdName = Path.GetFileNameWithoutExtension(psdPath);
        string importedTexturesFolder = Path.Combine(psdFolder, string.Format("ImportedTextures-{0}", psdName)); 

        return importedTexturesFolder;
    }

    private static void _EnsureFolder(string folderPath)
    {
        Directory.CreateDirectory(folderPath);
    }

    private static void _UpdatePrefab(string prefabPath, GameObject uiGameObject)
    {
        var prefabObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(prefabPath);
        if (prefabObject == null)
        {
            prefabObject = PrefabUtility.CreateEmptyPrefab(prefabPath);
        }
        else
        {
            var components = (prefabObject as GameObject).GetComponents<Component>();
            foreach (var component in components)
            {
                if (!(component is Graphic) &&
                    !(component is Canvas) &&
                    !(component is CanvasScaler) &&
                    !(component is RectTransform) &&
                    !(component is GraphicRaycaster))
                {
                    UnityEditorInternal.ComponentUtility.CopyComponent(component);
                    UnityEditorInternal.ComponentUtility.PasteComponentAsNew(uiGameObject);
                }
            }
        }

        PrefabUtility.ReplacePrefab(uiGameObject, prefabObject, ReplacePrefabOptions.ReplaceNameBased);
    }


    public static GameObject _BuildUguiGameObject(UiTreeRoot uiTree)
    {
        var uguiVisitor = new BuildUguiGameObjectVisitor(default(Rect), null);
        GameObject canvasGameObject = uguiVisitor.Visit(uiTree);
        return canvasGameObject;
    }


    private static string _GetImportedPrefabSavePath(string psdPath)
    {
        string psdFolder = Path.GetDirectoryName(psdPath);
        string psdName = Path.GetFileNameWithoutExtension(psdPath);

        return Path.Combine(psdFolder, string.Format("{0}.prefab", psdName)).Replace("\\", "/");
    }
}