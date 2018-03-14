using System.IO;

using UnityEngine;
using UnityEditor;


public class SaveTextureVisitor : IUiNodeVisitor
{
    private readonly string _basePath;

    public SaveTextureVisitor(string basePath)
    {
        _basePath = basePath;
    }

    public void Visit(UiTreeRoot root)
    {
        root.Children.ForEach(child => child.Accept(this));
    }

    public void Visit(GroupNode node)
    {
        node.Children.ForEach(child => child.Accept(this));
    }

    public void Visit(TextNode node) { }

    public void Visit(ImageNode node)
    {
        if (node.SpriteSource is InMemoryTextureSpriteSource)
        {
            var inMemoryTexture = (InMemoryTextureSpriteSource) node.SpriteSource;

            string outputTextureFilename = string.Format("{0}.png", node.Name);
            string outputTexturePath = Path.Combine(_basePath, outputTextureFilename);

            File.WriteAllBytes(outputTexturePath, inMemoryTexture.Texture2D.EncodeToPNG());

            AssetDatabase.Refresh();

            node.SpriteSource = new AssetSpriteSource(outputTexturePath);
        }
    }
}