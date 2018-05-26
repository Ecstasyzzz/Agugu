using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

using AdInfinitum;


public class PsdImporter
{
    private static readonly ParallelCoroutineExecutor Executor = new ParallelCoroutineExecutor();

    static PsdImporter()
    {
        EditorApplication.update += _EditorUpdate;
    }

    private static void _EditorUpdate()
    {
        Executor.Resume();
    }

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

    public static void ImportPsdAsPrefab
    (
        string     psdPath,
        UiTreeRoot uiTree,
        bool       keepGameObject = false,
        Transform  parent         = null
    )
    {
        Executor.Add(AdInfinitum.Coroutine.Create(
            _ImportPsdAsPrefabProcess(psdPath, uiTree, keepGameObject, parent)));
    }

    private static IEnumerator _ImportPsdAsPrefabProcess
    (
        string     psdPath,
        UiTreeRoot uiTree,
        bool       keepGameObject = false,
        Transform  parent         = null
    )
    {
        _SaveTextureAsAsset(psdPath, uiTree);

        yield return null;

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
        _ClearFolder(importedTexturesFolder);

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

    private static void _ClearFolder(string folderPath)
    {
        _DeleteFolder(folderPath);
        _EnsureFolder(folderPath);
    }

    private static void _DeleteFolder(string folderPath)
    {
        if (Directory.Exists(folderPath))
        {
            Directory.Delete(folderPath, recursive: true);
        }
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
            var prefabInstance = GameObject.Instantiate(prefabObject) as GameObject;
            _MigrateAppliedPrefabModification(prefabInstance, uiGameObject);
            GameObject.DestroyImmediate(prefabInstance);
        }

        PrefabUtility.ReplacePrefab(uiGameObject, prefabObject, ReplacePrefabOptions.ReplaceNameBased);
    }

    private static void _MigrateAppliedPrefabModification
    (
        GameObject sourceGameObjectRoot,
        GameObject targetGameObjectRoot
    )
    {
        Dictionary<int, GameObject> sourceMapping =
            _BuildLayerIdToGameObjectMapping(sourceGameObjectRoot);
        Dictionary<int, GameObject> targetMapping =
            _BuildLayerIdToGameObjectMapping(targetGameObjectRoot);

        _MoveNonImportedGameObjects(sourceGameObjectRoot, targetGameObjectRoot);

        foreach (var idAndGameObjectPair in targetMapping)
        {
            if (sourceMapping.ContainsKey(idAndGameObjectPair.Key))
            {
                GameObject source = sourceMapping[idAndGameObjectPair.Key];
                GameObject target = idAndGameObjectPair.Value;

                _MoveNonImportedGameObjects(source, target);
            }
        }

        _CopyNonImportedComponents(sourceGameObjectRoot, targetGameObjectRoot, targetMapping);

        foreach (var idAndGameObjectPair in targetMapping)
        {
            if (sourceMapping.ContainsKey(idAndGameObjectPair.Key))
            {
                GameObject source = sourceMapping[idAndGameObjectPair.Key];
                GameObject target = idAndGameObjectPair.Value;
               
                _CopyNonImportedComponents(source, target, targetMapping);
            }
        }
    }

    private static Dictionary<int, GameObject> _BuildLayerIdToGameObjectMapping(GameObject root)
    {
        var psdLayerIdTagList = root.GetComponentsInChildren<PsdLayerIdTag>();

        var mapping = new Dictionary<int, GameObject>();
        foreach (var layerIdTag in psdLayerIdTagList)
        {
            mapping.Add(layerIdTag.LayerId, layerIdTag.gameObject);
        }

        return mapping;
    }

    private static void _CopyNonImportedComponents
    (
        GameObject source, 
        GameObject target,
        Dictionary<int, GameObject> targetLayerIdGameObjectMapping
    )
    {
        var components = source.GetComponents<Component>();
        foreach (var component in components)
        {
            if (!(component is Graphic) &&
                !(component is CanvasRenderer) &&
                !(component is Canvas) &&
                !(component is CanvasScaler) &&
                !(component is RectTransform) &&
                !(component is GraphicRaycaster) &&
                !(component is PsdLayerIdTag))
            {
                UnityEditorInternal.ComponentUtility.CopyComponent(component);
                var targetComponent = target.AddComponent(component.GetType());
                UnityEditorInternal.ComponentUtility.PasteComponentValues(targetComponent);
                _RetargetObjectReference(component, targetComponent, targetLayerIdGameObjectMapping);
            }
        }
    }

    private static void _RetargetObjectReference
    (
        Component                   source,
        Component                   target,
        Dictionary<int, GameObject> targetLayerIdGameObjectMapping
    )
    {
        var sourceSO = new SerializedObject(source);

        var targetSO = new SerializedObject(target);
        SerializedProperty targetSPIter = targetSO.GetIterator();
        bool isIteratorValid = targetSPIter.Next(true);
        while (isIteratorValid)
        {
            if (targetSPIter.propertyType == SerializedPropertyType.ObjectReference &&
                !string.Equals(targetSPIter.name, "m_PrefabParentObject") &&
                !string.Equals(targetSPIter.name, "m_PrefabInternal") &&
                !string.Equals(targetSPIter.name, "m_GameObject") &&
                !string.Equals(targetSPIter.name, "m_Script"))
            {
                string propertyPath = targetSPIter.propertyPath;
                var sourceSP = sourceSO.FindProperty(propertyPath);
                if (sourceSP != null)
                {
                    UnityEngine.Object sourceObjectReference = sourceSP.objectReferenceValue;
                    GameObject sourceReferencedGameObject;
                    if (sourceObjectReference is GameObject)
                    {
                        sourceReferencedGameObject = sourceObjectReference as GameObject;
                    }
                    else if(sourceObjectReference is Component)
                    {
                        sourceReferencedGameObject = (sourceObjectReference as Component).gameObject;
                    }
                    else
                    {
                        sourceReferencedGameObject = null;
                    }

                    if (sourceReferencedGameObject != null)
                    {
                        var sourceLayerIdTag = sourceReferencedGameObject.GetComponent<PsdLayerIdTag>();
                        if (sourceLayerIdTag != null)
                        {
                            int layerId = sourceLayerIdTag.LayerId;

                            if (targetLayerIdGameObjectMapping.ContainsKey(layerId))
                            {
                                if (sourceObjectReference is GameObject)
                                {
                                    targetSPIter.objectReferenceValue = targetLayerIdGameObjectMapping[layerId];
                                }
                                else if (sourceObjectReference is Component)
                                {
                                    GameObject retargetGameObject = targetLayerIdGameObjectMapping[layerId];
                                    Component retargetComponent =
                                        retargetGameObject.GetComponent(sourceObjectReference.GetType());
                                    targetSPIter.objectReferenceValue = retargetComponent;
                                }
                            }
                        }
                    }
                }
            }

            isIteratorValid = targetSPIter.Next(false);
        }

        targetSO.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void _MoveNonImportedGameObjects(GameObject source, GameObject target)
    {
        var gameObjectsShouldMove = new List<GameObject>();
        foreach (Transform child in source.transform)
        {
            if (child.GetComponent<PsdLayerIdTag>() == null)
            {
                gameObjectsShouldMove.Add(child.gameObject);
            }
        }

        gameObjectsShouldMove.ForEach(go => go.transform.SetParent(target.transform));
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