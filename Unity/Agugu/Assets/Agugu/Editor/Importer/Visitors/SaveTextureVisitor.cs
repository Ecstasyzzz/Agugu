using System.IO;

using UnityEngine;
using UnityEditor;

namespace Agugu.Editor
{
    public class SaveTextureVisitor : IUiNodeVisitor
    {
        private readonly string _basePath;
        private readonly string _prefix;

        public SaveTextureVisitor(string basePath, string prefix = "")
        {
            _basePath = basePath;
            _prefix = prefix;
        }

        public void Visit(UiTreeRoot root)
        {
            root.Children.ForEach(child => child.Accept(this));
        }

        public void Visit(GroupNode node)
        {
            if (!node.IsSkipped)
            {
                node.Children.ForEach(child => child.Accept(new SaveTextureVisitor(_basePath, _prefix + node.Name)));
            }
        }

        public void Visit(TextNode node)
        {
        }

        public void Visit(ImageNode node)
        {
            if (!node.IsSkipped &&
                node.WidgetType != WidgetType.EmptyGraphic &&
                node.SpriteSource is InMemoryTextureSpriteSource)
            {
                var inMemoryTexture = (InMemoryTextureSpriteSource) node.SpriteSource;

                string outputTextureFilename = string.Format(_prefix + "{0}.png", node.Name);
                string outputTexturePath = Path.Combine(_basePath, outputTextureFilename);

                File.WriteAllBytes(outputTexturePath, inMemoryTexture.Texture2D.EncodeToPNG());

                AssetDatabase.Refresh();

                node.SpriteSource = new AssetSpriteSource(outputTexturePath);
            }
        }
    }
}