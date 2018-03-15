﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using UnityEngine;

using Ntreev.Library.Psd;
using Ntreev.Library.Psd.Readers.ImageResources;
using Ntreev.Library.Psd.Structures;


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

public class PsdParser
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


    public static UiTreeRoot Parse(string psdPath)
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

    private static UiNode _ParsePsdLayerRecursive(UiTreeRoot tree, PsdLayer layer)
    {
        int id = (int)layer.Resources["lyid.ID"];
        string name = layer.Name;
        bool isVisible = layer.IsVisible;

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
                IsVisible = isVisible,

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
                IsVisible = isVisible,

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
                IsVisible = isVisible,

                XAnchor = xAnchor,
                YAnchor = yAnchor,
                Rect = rect,

                WidgetType = widgetType,
                SpriteSource = new InMemoryTextureSpriteSource { Texture2D = texture2D }
            };
        }
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
}