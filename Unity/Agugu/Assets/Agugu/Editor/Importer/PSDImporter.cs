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

        using (var document = PsdDocument.Create(selectedObjectPath))
        {
            var layersConfig = _ParseConfig(document);

            string selectedObjectFolder = Path.GetDirectoryName(selectedObjectPath);
            string outputPath = Path.Combine(selectedObjectFolder, "ImportedTextures");
            Directory.CreateDirectory(outputPath);

            var canvasGameObject = new GameObject("Canvas", typeof(Canvas));
            var canvasRectTransform = canvasGameObject.GetComponent<RectTransform>();
            canvasRectTransform.sizeDelta = new Vector2(document.Width, document.Height);

            foreach (IPsdLayer layer in document.Childs)
            {
                Debug.Log("LayerName : " + layer.Name);
                Texture2D texture = GetTexture2DFromPsdLayer(layer);

                string outputFilename = Path.Combine(outputPath, layer.Name + ".png");
                File.WriteAllBytes(outputFilename, texture.EncodeToPNG());

                var imageGameObject = new GameObject(layer.Name, typeof(Image));
                var image = imageGameObject.GetComponent<Image>();
                image.transform.SetParent(canvasGameObject.transform, worldPositionStays:false);
                var imageRectTransform = image.GetComponent<RectTransform>();
                imageRectTransform.position = new Vector3((layer.Left + layer.Right)/2, 
                                document.Height - (layer.Bottom + layer.Top)/2, 0);
                imageRectTransform.sizeDelta = new Vector2(layer.Width, layer.Height);

                image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(outputFilename);
            }
        }
        AssetDatabase.Refresh();
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

