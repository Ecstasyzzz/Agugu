using UnityEngine;
using UnityEditor;

namespace Agugu.Editor
{
    public interface ISpriteSource
    {
        Sprite GetSprite();
    }

    public class InMemoryTextureSpriteSource : ISpriteSource
    {
        public Texture2D Texture2D;

        public Sprite GetSprite()
        {
            return Sprite.Create(Texture2D, new Rect(0, 0, Texture2D.width, Texture2D.height), new Vector2(0.5f, 0.5f));
        }
    }

    public class AssetSpriteSource : ISpriteSource
    {
        private readonly string _spritePath;

        public AssetSpriteSource(string spritePath)
        {
            _spritePath = spritePath;
        }

        public Sprite GetSprite()
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(_spritePath);
        }
    }
}