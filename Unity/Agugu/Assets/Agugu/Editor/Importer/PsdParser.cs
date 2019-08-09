using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Ntreev.Library.Psd;
using Ntreev.Library.Psd.Readers.ImageResources;
using Ntreev.Library.Psd.Structures;

namespace Agugu.Editor
{
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
        Middle,
        Top,
        Stretch
    }

    public enum WidgetType
    {
        None,
        Image,
        Text,
        EmptyGraphic
    }

    public class PsdParser
    {
        private static readonly XNamespace _aguguNamespace = "http://www.agugu.org/";
        private static readonly XNamespace _rdfNamespace   = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

        private const int DocumentRootMagicLayerId = -1;

        private const string ConfigRootTag = "Config";
        private const string LayersRootTag = "Layers";
        private const string BagTag        = "Bag";
        private const string IdTag         = "Id";
        private const string PropertiesTag = "Properties";

        private const string IsSkippedPropertyTag = "isSkipped";

        private const string WidgetTypePropertyTag = "widgetType";

        private const string XAnchorPropertyTag = "xAnchor";
        private const string YAnchorPropertyTag = "yAnchor";

        private const string XPivotPropertyTag = "xPivot";
        private const string YPivotPropertyTag = "yPivot";
        

        public static UiTreeRoot Parse(string psdPath)
        {
            using (var document = PsdDocument.Create(psdPath))
            {
                var uiTree = new UiTreeRoot();

                uiTree.Name = Path.GetFileName(psdPath);
                uiTree.Width = document.Width;
                uiTree.Height = document.Height;
                uiTree.Configs = _ParseConfig(document);

                PsdLayerConfig config = uiTree.Configs.GetLayerConfig(DocumentRootMagicLayerId);

                uiTree.Pivot = _GetPivot(config);

                uiTree.XAnchor = _GetXAnchorType(config);
                uiTree.YAnchor = _GetYAnchorType(config);

                var imageResource = document.ImageResources;
                var resolutionProperty = imageResource["Resolution"] as Reader_ResolutionInfo;
                int horizontalResolution = Convert.ToInt32(resolutionProperty.Value["HorizontalRes"]);

                uiTree.HorizontalPixelPerInch = horizontalResolution;

                foreach (PsdLayer layer in document.Childs)
                {
                    uiTree.Children.Add(_ParsePsdLayerRecursive(uiTree, layer));
                }

                return uiTree;
            }
        }

        private static PsdLayerConfigSet _ParseConfig(PsdDocument document)
        {
            IProperties imageResources = document.ImageResources;
            if (imageResources.Contains("XmpMetadata"))
            {
                var xmpImageResource = imageResources["XmpMetadata"] as Reader_XmpMetadata;
                var xmpValue = xmpImageResource.Value["Xmp"] as string;

                return ParseXmp(xmpValue);
            }
            else
            {
                return new PsdLayerConfigSet();
            }
        }

        public static PsdLayerConfigSet ParseXmp(string xmpString)
        {
            var result = new PsdLayerConfigSet();
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

                result.SetLayerConfig(layerId, new PsdLayerConfig(propertyDictionary));
            }

            return result;
        }

        private static UiNode _ParsePsdLayerRecursive(UiTreeRoot tree, PsdLayer layer)
        {
            int id = (int) layer.Resources["lyid.ID"];
            string name = layer.Name;
            bool isVisible = layer.IsVisible;

            PsdLayerConfig config = tree.Configs.GetLayerConfig(id);

            bool isSkipped = config.GetLayerConfigAsBool(IsSkippedPropertyTag);

            Vector2 pivot = _GetPivot(config);

            XAnchorType xAnchor = _GetXAnchorType(config);
            YAnchorType yAnchor = _GetYAnchorType(config);

            var rect = new Rect
            {
                xMin = layer.Left,
                xMax = layer.Right,
                yMin = tree.Height - layer.Bottom,
                yMax = tree.Height - layer.Top
            };

            bool isGroup = _IsGroupLayer(layer);
            bool isText = _IsTextLayer(layer);

            var baseUiNode = new UiNode
            {
                Id = id,
                Name = name,
                IsVisible = isVisible,
                IsSkipped = isSkipped,

                Pivot = pivot,
                XAnchor = xAnchor,
                YAnchor = yAnchor,
                Rect = rect
            };

            if (isGroup)
            {
                var children = new List<UiNode>();

                foreach (PsdLayer childLayer in layer.Childs)
                {
                    children.Add(_ParsePsdLayerRecursive(tree, childLayer));
                }

                return new GroupNode(baseUiNode)
                {
                    Children = children
                };
            }
            else if (isText)
            {
                switch (AguguConfig.Instance.TextImportMode)
                {
                    case TextImportMode.Text:
                        return _CreateTextNode(layer, baseUiNode);
                    case TextImportMode.Image:
                        return _CreateImageNode(layer, config, baseUiNode);
                    case TextImportMode.TextAndImage:
                        return new ImageTextNode
                        {
                            Text = _CreateTextNode(layer, baseUiNode),
                            Image = _CreateImageNode(layer, config, baseUiNode)
                        };
                    default:
                        return _CreateTextNode(layer, baseUiNode);
                }
            }
            else
            {
                return _CreateImageNode(layer, config, baseUiNode);
            }
        }

        private static TextNode _CreateTextNode(PsdLayer layer, UiNode baseUiNode)
        {
            var engineData           = (StructureEngineData)layer.Resources["TySh.Text.EngineData"];
            var engineDict           = (Properties)engineData["EngineDict"];
            var styleRun             = (Properties)engineDict["StyleRun"];
            var runArray             = (ArrayList)styleRun["RunArray"];
            var firstRunArrayElement = (Properties)runArray[0];
            var firstStyleSheet      = (Properties)firstRunArrayElement["StyleSheet"];
            var firstStyleSheetData  = (Properties)firstStyleSheet["StyleSheetData"];

            var fontIndex = (int)firstStyleSheetData["Font"];

            var fontSize = _GetFontSizeFromStyleSheetData(firstStyleSheetData);
            // TODO: Fix this hack
            fontSize = fontSize / 75 * 18;
            var textColor = _GetTextColorFromStyleSheetData(firstStyleSheetData);

            var documentResources = (Properties)engineData["DocumentResources"];
            var fontSet           = (ArrayList)documentResources["FontSet"];
            var font              = (Properties)fontSet[fontIndex];
            var fontName          = (string)font["Name"];

            var text = (string)layer.Resources["TySh.Text.Txt"];

            return new TextNode(baseUiNode)
            {
                FontSize = fontSize,
                FontName = fontName,

                Text = text,
                TextColor = textColor
            };
        }

        private static ImageNode _CreateImageNode(PsdLayer layer, PsdLayerConfig config, UiNode baseUiNode)
        {
            WidgetType widgetType = config.GetLayerConfigAsWidgetType(WidgetTypePropertyTag);

            Texture2D texture2D = GetTexture2DFromPsdLayer(layer);

            return new ImageNode(baseUiNode)
            {
                WidgetType = widgetType,
                SpriteSource = texture2D != null ?
                    new InMemoryTextureSpriteSource { Texture2D = texture2D } :
                    (ISpriteSource)new NullSpriteSource()
            };
        }


        // RectTransform
        private static Vector2 _GetPivot(PsdLayerConfig config)
        {
            float pivotX = config.GetLayerConfigAsFloat(XPivotPropertyTag, 0.5f);
            float pivotY = config.GetLayerConfigAsFloat(YPivotPropertyTag, 0.5f);
            return new Vector2(pivotX, pivotY);
        }

        private static XAnchorType _GetXAnchorType(PsdLayerConfig config)
        {
            return config.GetLayerConfigAsXAnchorType(XAnchorPropertyTag);
        }

        private static YAnchorType _GetYAnchorType(PsdLayerConfig config)
        {
            return config.GetLayerConfigAsYAnchorType(YAnchorPropertyTag);
        }

        private static WidgetType _GetWidgetType(PsdLayerConfig config)
        {
            return config.GetLayerConfigAsWidgetType(WidgetTypePropertyTag);
        }


        // Layer Type
        private static bool _IsGroupLayer(PsdLayer psdLayer)
        {
            return psdLayer.SectionType == SectionType.Opend ||
                   psdLayer.SectionType == SectionType.Closed;
        }

        private static bool _IsTextLayer(PsdLayer psdLayer)
        {
            return psdLayer.Resources.Contains("TySh");
        }


        // Text
        private static float _GetFontSizeFromStyleSheetData(Properties styleSheetData)
        {
            // Font size could be omitted TODO: Find official default Value
            if (styleSheetData.Contains("FontSize"))
            {
                return (float) styleSheetData["FontSize"];
            }

            return 42;
        }

        private static Color _GetTextColorFromStyleSheetData(Properties styleSheetData)
        {
            // FillColor also could be omitted
            if (styleSheetData.Contains("FillColor"))
            {
                var fillColor = (Properties) styleSheetData["FillColor"];
                var fillColorValue = (ArrayList) fillColor["Values"];
                //ARGB
                var textColor = new Color((float) fillColorValue[1],
                    (float) fillColorValue[2],
                    (float) fillColorValue[3],
                    (float) fillColorValue[0]);

                return textColor;
            }

            return Color.black;
        }

        
        // Image
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

            if (pixelCount == 0)
            {
                Debug.LogWarningFormat("Encounter 0 pixel layer at {0}", layer.Name);
                return null;
            }

            var pixelArray = new Color32[pixelCount];

            // Unity texture coordinates start at lower left corner.
            // Photoshop coordinates start at upper left corner.
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int photoshopIndex = x + y * width;
                    int unityTextureIndex = x + (height - 1 - y) * width;

                    byte r = rChannel != null ? rChannel.Data[photoshopIndex] : (byte) 0;
                    byte g = gChannel != null ? gChannel.Data[photoshopIndex] : (byte) 0;
                    byte b = bChannel != null ? bChannel.Data[photoshopIndex] : (byte) 0;
                    byte a = aChannel != null ? aChannel.Data[photoshopIndex] : (byte) 255;

                    pixelArray[unityTextureIndex] = new Color32(r, g, b, a);
                }
            }

            var outputTexture2D = new Texture2D(width, height);
            outputTexture2D.SetPixels32(pixelArray);
            outputTexture2D.Apply();

            return outputTexture2D;
        }
    }
}