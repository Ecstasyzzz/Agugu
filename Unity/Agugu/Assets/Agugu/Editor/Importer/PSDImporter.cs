using System;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEditor;

using Ntreev.Library.Psd;


public class PSDImporter
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

        using (var document = PsdDocument.Create(selectedObjectPath))
        {
            string selectedObjectFolder = Path.GetDirectoryName(selectedObjectPath);
            string outputPath = Path.Combine(selectedObjectFolder, "ImportedTextures");
            Directory.CreateDirectory(outputPath);

            foreach (IPsdLayer layer in document.Childs)
            {
                Debug.Log("LayerName : " + layer.Name);
                Texture2D texture = GetTexture2DFromPsdLayer(layer);

                string outputFilename = Path.Combine(outputPath, layer.Name + ".png");
                File.WriteAllBytes(outputFilename, texture.EncodeToPNG());
            }
        }
        AssetDatabase.Refresh();
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

