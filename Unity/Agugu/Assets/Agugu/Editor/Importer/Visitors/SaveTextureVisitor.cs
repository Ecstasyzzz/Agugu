using System.IO;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace Agugu.Editor
{
    public class SaveTextureVisitor : IUiNodeVisitor
    {
        private readonly string _basePath;
        private readonly string _prefix;

        private readonly List<string> _reusedTextureFilename = new List<string>();
        private readonly List<string> _createdTextureFilename = new List<string>();

        public List<string> ReusedTextureFilename
        {
            get { return _reusedTextureFilename; }
        }

        public List<string> CreatedTextureFilename
        {
            get { return _createdTextureFilename; }
        }

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
                var childVisitor = new SaveTextureVisitor(_basePath, _prefix + node.Name);
                node.Children.ForEach(child => child.Accept(childVisitor));
                _reusedTextureFilename.AddRange(childVisitor.ReusedTextureFilename);
                _createdTextureFilename.AddRange(childVisitor.CreatedTextureFilename);
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

                bool shouldWriteTexture = true;
                bool hasExistingTexture = File.Exists(outputTexturePath);
                if (hasExistingTexture)
                {
                    byte[] existingTexturePngData = File.ReadAllBytes(outputTexturePath);
                    byte[] newTexturePngData = inMemoryTexture.Texture2D.EncodeToPNG();
                    bool isSameTexture = existingTexturePngData.SequenceEqual(newTexturePngData);
                    if (isSameTexture)
                    {
                        shouldWriteTexture = false;
                    }
                }

                if (shouldWriteTexture)
                {
                    File.WriteAllBytes(outputTexturePath, inMemoryTexture.Texture2D.EncodeToPNG());
                    _createdTextureFilename.Add(outputTexturePath);

                    AssetDatabase.Refresh();
                }
                else
                {
                    _reusedTextureFilename.Add(outputTextureFilename);
                }

                node.SpriteSource = new AssetSpriteSource(outputTexturePath);
            }
        }
    }
}