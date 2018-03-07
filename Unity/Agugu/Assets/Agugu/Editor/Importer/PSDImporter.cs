using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

using Ntreev.Library.Psd;
using Ntreev.Library.Psd.Structures;
using Ntreev.Library.Psd.Readers.ImageResources;


public enum XAnchorType
{
    None,
    Left,
    Center,
    Right,
    Stretch
}

public enum YAnchorType
{
    None,
    Bottom,
    Center,
    Top,
    Stretch
}

public enum WidgetType
{
    None,
    Button
}


public class UiNode
{
    public int Id;
    public string Name;

    public XAnchorType XAnchor;
    public YAnchorType YAnchor;
    public Rect Rect;

    public virtual void Accept(IUiNodeVisitor visitor) { }
}

public class UiTreeRoot
{
    public float Width;
    public float Height;

    public PsdLayerConfigs Configs = new PsdLayerConfigs();
    public List<UiNode> Children = new List<UiNode>();

    public void AddChild(UiNode node)
    {
        Children.Add(node);
    }
}

public class GroupNode : UiNode
{
    public List<UiNode> Children = new List<UiNode>();

    public void AddChild(UiNode node)
    {
        Children.Add(node);
    }

    public override void Accept(IUiNodeVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class ImageNode : UiNode
{
    public ISpriteSource SpriteSource;
    public WidgetType WidgetType;

    public override void Accept(IUiNodeVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class TextNode : UiNode
{
    public float FontSize;
    public string FontName;

    public string Text;
    public Color TextColor;

    public override void Accept(IUiNodeVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class PSDImporter
{
    private static readonly XNamespace _aguguNamespace = "http://www.agugu.org/";
    private static readonly XNamespace _rdfNamespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

    private const string ConfigRootTag = "Config";
    private const string LayersRootTag = "Layers";
    private const string BagTag = "Bag";
    private const string IdTag = "Id";
    private const string PropertiesTag = "Properties";

    private const string XAnchorPropertyTag = "xAnchor";
    private const string YAnchorPropertyTag = "yAnchor";
    private const string WidgetTypePropertyTag = "widgetType";

    

    private static XAnchorType _GetXAnchorType(string value)
    {
        switch (value)
        {
            case "left": return XAnchorType.Left;
            case "center": return XAnchorType.Center;
            case "right": return XAnchorType.Right;
            case "stretch": return XAnchorType.Stretch;
            default: return XAnchorType.None;
        }
    }

    private static YAnchorType _GetYAnchorType(string value)
    {
        switch (value)
        {
            case "top": return YAnchorType.Top;
            case "center": return YAnchorType.Center;
            case "bottom": return YAnchorType.Bottom;
            case "stretch": return YAnchorType.Stretch;
            default: return YAnchorType.None;
        }
    }


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
        UiTreeRoot uiTree = _ParsePsd(psdPath);

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

    private static UiTreeRoot _ParsePsd(string psdPath)
    {
        using (var document = PsdDocument.Create(psdPath))
        {
            var uiTree = new UiTreeRoot();
            uiTree.Width = document.Width;
            uiTree.Height = document.Height;
            uiTree.Configs = _ParseConfig(document);

            foreach (PsdLayer layer in document.Childs)
            {
                uiTree.Children.Add(_ParsePsdLayerRecursive(uiTree, layer));
            }

            return uiTree;
        }
    }

    private static UiNode _ParsePsdLayerRecursive(UiTreeRoot tree, PsdLayer layer)
    {
        int id = (int)layer.Resources["lyid.ID"];
        string name = layer.Name;

        var config = tree.Configs.GetLayerConfig(id);
        XAnchorType xAnchor = _GetXAnchorType(config.GetValueOrDefault(XAnchorPropertyTag));
        YAnchorType yAnchor = _GetYAnchorType(config.GetValueOrDefault(YAnchorPropertyTag));
        var rect = new Rect
        {
            xMin = layer.Left,
            xMax = layer.Right,
            yMin = tree.Height - layer.Bottom,
            yMax = tree.Height - layer.Top
        };

        bool isGroup = _IsGroupLayer(layer);
        bool isText = _IsTextLayer(layer);

        if (isGroup)
        {
            var children = new List<UiNode>();

            foreach (PsdLayer childlayer in layer.Childs)
            {
                children.Add(_ParsePsdLayerRecursive(tree, childlayer));
            }

            return new GroupNode
            {
                Id = id,
                Name = name,

                XAnchor = xAnchor,
                YAnchor = yAnchor,
                Rect = rect,

                Children = children
            };
        }
        else if (isText)
        {
            var engineData = (StructureEngineData)layer.Resources["TySh.Text.EngineData"];
            var engineDict = (Properties)engineData["EngineDict"];
            var styleRun = (Properties)engineDict["StyleRun"];
            var runArray = (ArrayList)styleRun["RunArray"];
            var firstRunArrayElement = (Properties)runArray[0];
            var firstStyleSheet = (Properties)firstRunArrayElement["StyleSheet"];
            var firstStyelSheetData = (Properties)firstStyleSheet["StyleSheetData"];

            var fontIndex = (int)firstStyelSheetData["Font"];
            // Font size could be omitted TODO: Find official default Value
            var fontSize = firstStyelSheetData.Contains("FontSize") ? (float)firstStyelSheetData["FontSize"] : 42;
            var fillColor = (Properties)firstStyelSheetData["FillColor"];
            var fillColorValue = (ArrayList)fillColor["Values"];
            //ARGB
            var textColor = new Color((float)fillColorValue[1],
                                      (float)fillColorValue[2],
                                      (float)fillColorValue[3],
                                      (float)fillColorValue[0]);

            var documentResources = (Properties)engineData["DocumentResources"];
            var fontSet = (ArrayList)documentResources["FontSet"];
            var font = (Properties)fontSet[fontIndex];
            var fontName = (string)font["Name"];

            var text = (string)layer.Resources["TySh.Text.Txt"];

            return new TextNode
            {
                Id = id,
                Name = name,

                XAnchor = xAnchor,
                YAnchor = yAnchor,
                Rect = rect,

                FontSize = fontSize,
                FontName = fontName,

                Text = text,
                TextColor = textColor
            };
        }
        else
        {
            string widgetTypeString = config.GetValueOrDefault(WidgetTypePropertyTag);
            WidgetType widgetType = string.Equals(widgetTypeString, "button") ? WidgetType.Button : WidgetType.None;

            Texture2D texture2D = GetTexture2DFromPsdLayer(layer);

            return new ImageNode
            {
                Id = id,
                Name = name,

                XAnchor = xAnchor,
                YAnchor = yAnchor,
                Rect = rect,

                WidgetType = widgetType,
                SpriteSource = new InMemoryTextureSpriteSource{Texture2D = texture2D}
            };
        }
    }

    private static void _ImportLayersRecursive
    (
        PsdLayer layerToImport, 
        RectTransform parentRectTransform,
        PsdLayerConfigs layerConfigs, 
        string importedTexturesFolder,
        int parentWidth, 
        int parentHeight
    )
    {
        if (!layerToImport.IsVisible) { return; }

        int layerId = (int)layerToImport.Resources["lyid.ID"];
        string layerName = layerToImport.Name;

        bool isGroup = _IsGroupLayer(layerToImport);
        bool isText = _IsTextLayer(layerToImport);

        if (isGroup)
        {
            

            /*if (layerConfigs.HasLayerConfig(layerId))
            {
                var layerConfig = layerConfigs.GetLayerConfig(layerId);

                Rect rect = Rect.MinMaxRect(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
                foreach (PsdLayer childLayer in layerToImport.Childs)
                {
                    rect = _GetBoundingRectRecursive(rect, childLayer);
                }

                

                Vector3[] parentWorldCorners = new Vector3[4];
                parentRectTransform.GetWorldCorners(parentWorldCorners);

                Vector3 parentBottomLeft = parentWorldCorners[0];
                Vector3 parentTopRight = parentWorldCorners[2];
                Vector2 anchorMinWorldPosition = new Vector2(
                        Mathf.LerpUnclamped(parentBottomLeft.x, parentTopRight.x, groupRectTransform.anchorMin.x),
                        Mathf.LerpUnclamped(parentBottomLeft.y, parentTopRight.y, groupRectTransform.anchorMin.y)
                    );
                Vector2 anchorMaxWorldPosition = new Vector2(
                    Mathf.LerpUnclamped(parentBottomLeft.x, parentTopRight.x, groupRectTransform.anchorMax.x),
                    Mathf.LerpUnclamped(parentBottomLeft.y, parentTopRight.y, groupRectTransform.anchorMax.y)
                    );
                Vector2 pivotAnchor = new Vector2(
                    Mathf.LerpUnclamped(anchorMinWorldPosition.x, anchorMaxWorldPosition.x, groupRectTransform.pivot.x),
                    Mathf.LerpUnclamped(anchorMinWorldPosition.y, anchorMaxWorldPosition.y, groupRectTransform.pivot.y)
                );
                groupRectTransform.anchoredPosition = rect.center - pivotAnchor;
            }

            foreach (PsdLayer childLayer in layerToImport.Childs)
            {
                _ImportLayersRecursive
                (
                    childLayer, groupRectTransform,
                    layerConfigs, importedTexturesFolder,
                    parentWidth, parentHeight
                );
            }*/
        }
        else if (isText)
        {
            
            /*Font textFont = AguguFontLookup.Instance.GetFont(fontName);

            var uiGameObject = new GameObject(layerName);
            var uiRectTransform = uiGameObject.AddComponent<RectTransform>();
            var text = uiGameObject.AddComponent<Text>();
            var TySh = (Reader_TySh)layerToImport.Resources["TySh"];
            text.text = (string)layerToImport.Resources["TySh.Text.Txt"];
            text.color = textColor;
            text.font = textFont;
            // TODO: Wild guess, cannot find any reference about Unity font size
            // 25/6
            text.fontSize = (int)(fontSize / 4.16);
            text.resizeTextForBestFit = true;

           

            _SetRectTransform(uiRectTransform,
                layerToImport.Left, layerToImport.Right,
                layerToImport.Bottom, layerToImport.Top,
                layerToImport.Width, layerToImport.Height * 1.3f,
                parentWidth, parentHeight);

            uiGameObject.transform.SetParent(parentRectTransform, worldPositionStays: false);*/
        }
        else
        {
            Texture2D texture = GetTexture2DFromPsdLayer(layerToImport);

            string outputTextureFilename = string.Format("{0}.png", layerName);
            string outputTexturePath = Path.Combine(importedTexturesFolder, outputTextureFilename);
            File.WriteAllBytes(outputTexturePath, texture.EncodeToPNG());
            AssetDatabase.Refresh();
            

            
        }
    }


    private static Rect _GetBoundingRectRecursive
        (Rect boundingRect, PsdLayer layerToBound)
    {
        boundingRect.xMin = Mathf.Min(boundingRect.xMin, layerToBound.Left);
        boundingRect.xMax = Mathf.Max(boundingRect.xMax, layerToBound.Right);

        boundingRect.yMin = Mathf.Min(boundingRect.yMin, layerToBound.Document.Height - layerToBound.Bottom);
        boundingRect.yMax = Mathf.Max(boundingRect.yMax, layerToBound.Document.Height - layerToBound.Top);

        foreach (PsdLayer childLayer in layerToBound.Childs)
        {
            boundingRect = _GetBoundingRectRecursive(boundingRect, childLayer);
        }

        return boundingRect;
    }

    private static bool _IsGroupLayer(PsdLayer psdLayer)
    {
        return psdLayer.SectionType == SectionType.Opend || 
               psdLayer.SectionType == SectionType.Closed;
    }

    private static bool _IsTextLayer(PsdLayer psdLayer)
    {
        return psdLayer.Resources.Contains("TySh");
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


    private static PsdLayerConfigs _ParseConfig(PsdDocument document)
    {
        IProperties imageResources = document.ImageResources;
        var xmpImageResource = imageResources["XmpMetadata"] as Reader_XmpMetadata;
        var xmpValue = xmpImageResource.Value["Xmp"] as string;

        return ParseXMP(xmpValue);
    }

    public static PsdLayerConfigs ParseXMP(string xmpString) 
    {
        var result = new PsdLayerConfigs();
        var xmp = XDocument.Parse(xmpString);
        
        XElement configRoot = xmp.Descendants(_aguguNamespace + ConfigRootTag).FirstOrDefault();
        if (configRoot == null)
        {
            return result;
        }

        XElement layersConfigRoot = configRoot.Descendants(_aguguNamespace + LayersRootTag).FirstOrDefault();
        if (layersConfigRoot == null)
        {
            return result;
        }

        XElement bag = layersConfigRoot.Element(_rdfNamespace + BagTag);
        if (bag == null)
        {
            return result;
        }

        var layerItems = bag.Elements();
        foreach (XElement listItem in layerItems)
        {
            XElement idElement = listItem.Element(_aguguNamespace + IdTag);
            if (idElement == null)
            {
                continue;
            }

            int layerId = Int32.Parse(idElement.Value);
            var propertyDictionary = new Dictionary<string, string>();

            XElement propertiesRoot = listItem.Element(_aguguNamespace + PropertiesTag);
            if (propertiesRoot == null)
            {
                continue;
            }

            foreach (XElement layerProperty in propertiesRoot.Elements())
            {
                string propertyName = layerProperty.Name.LocalName;
                string propertyValue = layerProperty.Value;

                propertyDictionary.Add(propertyName, propertyValue);
            }

            result.SetLayerConfig(layerId, propertyDictionary);
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