using System;
using System.IO;
using System.Collections;

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

using AdInfinitum;


namespace Agugu.Editor
{
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
                Executor.Add(AdInfinitum.Coroutine.Create(
                    _ImportSelectionWithCanvasProcess(psdPath)));
            }
        }

        private static IEnumerator _ImportSelectionWithCanvasProcess(string psdPath)
        {
            UiTreeRoot uiTree = PsdParser.Parse(psdPath);

            var canvasGameObject = _CreateCanvasGameObject(uiTree.Width, uiTree.Height);
            var canvasRectTransform = canvasGameObject.GetComponent<RectTransform>();
            canvasRectTransform.ForceUpdateRectTransforms();

            yield return _ImportPsdAsPrefabProcess(psdPath, uiTree);

            string prefabPath = _GetImportedPrefabSavePath(psdPath);
            var uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var uiInstance = GameObject.Instantiate(uiPrefab);
            uiInstance.GetComponent<Transform>().SetParent(canvasRectTransform, worldPositionStays: false);
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

            canvasGameObject.AddComponent<GraphicRaycaster>();

            return canvasGameObject;
        }

        public static void ImportPsdAsPrefab(string psdPath, UiTreeRoot uiTree)
        {
            Executor.Add(AdInfinitum.Coroutine.Create(
                _ImportPsdAsPrefabProcess(psdPath, uiTree)));
        }

        // Cannot import texture then get the Sprite reference on the same frame
        private static IEnumerator _ImportPsdAsPrefabProcess(string psdPath, UiTreeRoot uiTree)
        {
            _SaveTextureAsAsset(psdPath, uiTree);

            yield return null;

            GameObject uiGameObject = _BuildUguiGameObjectTree(uiTree);

            var prefabPath = _GetImportedPrefabSavePath(psdPath);
            _SavePrefab(prefabPath, uiGameObject);
            GameObject.DestroyImmediate(uiGameObject);
        }

        public static void _SaveTextureAsAsset(string psdPath, UiTreeRoot uiTree)
        {
            string importedTexturesFolder = _GetImportedTexturesSavePath(psdPath);
            _ClearFolder(importedTexturesFolder);

            var saveTextureVisitor = new SaveTextureVisitor(importedTexturesFolder);
            saveTextureVisitor.Visit(uiTree);
        }

        private static string _GetImportedTexturesSavePath(string psdPath)
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

        private static void _SavePrefab(string prefabPath, GameObject uiGameObject)
        {
            var prefabObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(prefabPath);
            if (prefabObject == null)
            {
                prefabObject = PrefabUtility.CreateEmptyPrefab(prefabPath);
            }
            else
            {
                var sourceGameObject = GameObject.Instantiate(prefabObject);
                UguiTreeMigrator.MigrateAppliedPrefabModification(sourceGameObject as GameObject, uiGameObject);
                GameObject.DestroyImmediate(sourceGameObject);
            }

            PrefabUtility.ReplacePrefab(uiGameObject, prefabObject, ReplacePrefabOptions.ReplaceNameBased);
        }

        private static GameObject _BuildUguiGameObjectTree(UiTreeRoot uiTree)
        {
            var uguiVisitor = new BuildUguiGameObjectVisitor(default(Rect), null, uiTree.HorizontalPixelPerInch);
            GameObject rootGameObject = uguiVisitor.Visit(uiTree);
            return rootGameObject;
        }

        private static string _GetImportedPrefabSavePath(string psdPath)
        {
            string psdFolder = Path.GetDirectoryName(psdPath);
            string psdName = Path.GetFileNameWithoutExtension(psdPath);

            return Path.Combine(psdFolder, string.Format("{0}.prefab", psdName)).Replace("\\", "/");
        }
    }
}