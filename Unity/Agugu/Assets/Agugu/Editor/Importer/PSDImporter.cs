using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

using Ntreev.Library.Psd;
using Ntreev.Library.Psd.Readers.ImageResources;


public class PSDImporter
{
    private const string AguguNamespace = "http://www.agugu.org/";
    private const string AguguNamespacePrefix = "agugu:";

    private const string ConfigRootTag = "Config";
    private const string LayersRootTag = "Layers";
    private const string BagTag = "{http://www.w3.org/1999/02/22-rdf-syntax-ns#}Bag";
    private const string NamePropertyTag = "Name";

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
        using (var document = PsdDocument.Create(psdPath))
        {
            var layersConfig = _ParseConfig(document);

            string importedTexturesFolder = _GetImportedTexturesSaveFolder(psdPath);
            _EnsureFolder(importedTexturesFolder);

            var canvasGameObject = _CreateCanvasGameObject(document.Width, document.Height);

            foreach (IPsdLayer layer in document.Childs)
            {
                Texture2D texture = GetTexture2DFromPsdLayer(layer);

                string outputTextureFilename = string.Format("{0}.png", layer.Name);
                string outputTexturePath = Path.Combine(importedTexturesFolder, outputTextureFilename);
                File.WriteAllBytes(outputTexturePath, texture.EncodeToPNG());
                AssetDatabase.Refresh();
                Sprite importedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(outputTexturePath);

                var uiGameObject = new GameObject(layer.Name);
                var uiRectTransform = uiGameObject.AddComponent<RectTransform>();
                var image = uiGameObject.AddComponent<Image>();
                image.sprite = importedSprite;

                var psdLayerCenter = new Vector2((layer.Left + layer.Right) / 2,
                                                 (layer.Bottom + layer.Top) / 2);
                uiRectTransform.sizeDelta = new Vector2(layer.Width, layer.Height);
                uiRectTransform.localPosition = new Vector3(psdLayerCenter.x - document.Width / 2,
                                                            document.Height - psdLayerCenter.y - document.Height / 2, 0);

                // Have to set localPosition before parenting
                // Or the last imported layer will be reset to 0, 0, 0, I think it's a bug :(
                uiGameObject.transform.SetParent(canvasGameObject.transform, worldPositionStays: false);

                if (layersConfig.ContainsKey(layer.Name))
                {
                    var layerConfig = layersConfig[layer.Name];
                    string widgetType;
                    bool hasWidgetType = layerConfig.TryGetValue("widgetType", out widgetType);
                    if (hasWidgetType && string.Equals(widgetType, "button"))
                    {
                        uiGameObject.AddComponent<Button>();
                    }
                }
            }

            canvasGameObject.AddComponent<GenericView>();

            var prefabPath = _GetImportedPrefabSavePath(psdPath);
            var prefabObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(prefabPath);
            if (prefabObject == null)
            {
                prefabObject = PrefabUtility.CreateEmptyPrefab("Assets/TestCase/01.prefab");
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

    private static Dictionary<string, Dictionary<string, string>> _ParseConfig(PsdDocument document)
    {
        var result = new Dictionary<string, Dictionary<string, string>>();

        IProperties imageResources = document.ImageResources;
        var xmpImageResource = imageResources["XmpMetadata"] as Reader_XmpMetadata;
        var xmpValue = xmpImageResource.Value["Xmp"] as string;
        var xmp = XDocument.Parse(xmpValue);

        XNamespace xnamespace = AguguNamespace;
        XElement configRoot = xmp.Descendants(xnamespace + ConfigRootTag).FirstOrDefault();
        if (configRoot == null)
        {
            return result;
        }

        XElement layersConfigRoot = configRoot.Descendants(xnamespace + LayersRootTag).FirstOrDefault();
        if (layersConfigRoot == null)
        {
            return result;
        }

        XElement bag = layersConfigRoot.Element(BagTag);
        if (bag == null)
        {
            return result;
        }

        foreach (XElement listItem in bag.Elements())
        {
            var propertyDictionary = new Dictionary<string, string>();

            foreach (XElement layerProperty in listItem.Elements())
            {
                string propertyName = layerProperty.Name.LocalName;
                string propertyValue = layerProperty.Value;

                propertyDictionary.Add(propertyName, propertyValue);
            }

            string layerName;
            bool hasName = propertyDictionary.TryGetValue(NamePropertyTag, out layerName);
            if (hasName)
            {
                propertyDictionary.Remove(NamePropertyTag);
                result.Add(layerName, propertyDictionary);
            }
        }

        return result;
    }

    public static Texture2D GetTexture2DFromPsdLayer(IPsdLayer layer)
    {
        IChannel[] channels = layer.Channels;

        IChannel rChannel = channels.FirstOrDefault(channel => channel.Type == ChannelType.Red);
        IChannel gChannel = channels.FirstOrDefault(channel => channel.Type == ChannelType.Green);
        IChannel bChannel = channels.FirstOrDefault(channel => channel.Type == ChannelType.Blue);
        IChannel aChannel = channels.FirstOrDefault(channel => channel.Type == ChannelType.Alpha);

        int width = layer.Width;
        int height = layer.Height;
        int pixelCount = width * height;

        var pixelArray = new Color32[pixelCount];

        // Unity texture coordinates start at lower left corner.
        // Photoshop coordinates start at upper left corner.
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int photoshopIndex = x + y * width;
                int unityTextureIndex = x + (height - 1 - y) * width;

                byte r = rChannel != null ? rChannel.Data[photoshopIndex] : (byte)0;
                byte g = gChannel != null ? gChannel.Data[photoshopIndex] : (byte)0;
                byte b = bChannel != null ? bChannel.Data[photoshopIndex] : (byte)0;
                byte a = aChannel != null ? aChannel.Data[photoshopIndex] : (byte)255;

                pixelArray[unityTextureIndex] = new Color32(r, g, b, a);
            }
        }

        var outputTexture2D = new Texture2D(width, height);
        outputTexture2D.SetPixels32(pixelArray);
        outputTexture2D.Apply();

        return outputTexture2D;
    }
}

