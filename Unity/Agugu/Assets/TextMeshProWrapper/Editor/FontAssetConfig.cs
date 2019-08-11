using System.Text;
using System.Collections.Generic;

using UnityEngine;

public enum FontSizeOption
{
    AutoSizing,
    CustomSize
}

public enum FontPackingMode
{
    Fast    = 0,
    Optimum = 4,
}

public class FontAssetConfig : ScriptableObject, ISerializationCallbackReceiver
{
    public string FontSourcePath;

    public FontSizeOption FontSizeOption;
    public int CustomFontSize;

    public int FontPadding;

    public FontPackingMode FontPackingMode;

    public int AtlasWidth;
    public int AtlasHeight;

    public string CharacterSetString;
    public HashSet<char> CharacterSet = new HashSet<char>();

    public FontAssetConfig(string fontSourcePath)
    {
        FontSourcePath = fontSourcePath;

        FontSizeOption = FontSizeOption.AutoSizing;

        FontPadding = 5;

        FontPackingMode = FontPackingMode.Fast;

        AtlasWidth = 512;
        AtlasHeight = 512;
    }

    public void AppendCharacters(HashSet<char> characters)
    {
        foreach (char c in characters)
        {
            CharacterSet.Add(c);
        }
    }

    public void OnBeforeSerialize()
    {
        if (CharacterSet != null)
        {
            var stringBuilder = new StringBuilder();
            foreach (var c in CharacterSet)
            {
                stringBuilder.Append(c);
            }

            CharacterSetString = stringBuilder.ToString();
        }
        else
        {
            CharacterSetString = string.Empty;
        }
    }

    public void OnAfterDeserialize()
    {
        if (CharacterSet == null)
        {
            CharacterSet = new HashSet<char>();
        }

        foreach (var c in CharacterSetString)
        {
            CharacterSet.Add(c);
        }
    }
}