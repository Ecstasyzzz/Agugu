using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

using TMPro;

using TextMeshProWrapper;

public enum TextImportMode
{
    Text,
    Image,
    TextAndImage
}

public enum TextComponentType
{
    uGUI,
    TextMeshPro
}

[Serializable]
public class FontName
{
    public string        Name;
    public Font          Font;
    public TMP_FontAsset FontAsset;
}

[CreateAssetMenu]
public class AguguConfig : ScriptableObject
{
    [SerializeField] private TextImportMode _textImportMode = TextImportMode.Text;
    [SerializeField] private TextComponentType _textComponentType = TextComponentType.uGUI;

    [SerializeField] private List<FontName> _fontLookup = new List<FontName>();

    [SerializeField] private List<Object> _trackedPsd = new List<Object>();

    private static AguguConfig              _instance;
    private        Dictionary<string, Font> _lookUpTable;

    public static AguguConfig Instance
    {
        get
        {
            if (!_instance)
            {
                string assetGuid = AssetDatabase.FindAssets("t:AguguConfig").FirstOrDefault();
                if (string.IsNullOrEmpty(assetGuid))
                {
                    Debug.LogError(
                        "AguguConfig not created in Project. Create it via \"Assets\\Create\\Agugu Config\"");
                }

                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                _instance = AssetDatabase.LoadAssetAtPath<AguguConfig>(assetPath);
            }

            return _instance;
        }
    }

    public TextImportMode TextImportMode
    {
        get { return _textImportMode; }
    }

    public TextComponentType TextComponentType
    {
        get { return _textComponentType; }
    }

    public Font GetFont(string fontName)
    {
        FontName targetEntry = _GetFontByName(fontName);
        return targetEntry != null ? targetEntry.Font : null;
    }

    public TMP_FontAsset GetTextMeshProFontAsset(string fontName)
    {
        FontName targetEntry = _GetFontByName(fontName);
        return targetEntry != null ? targetEntry.FontAsset: null;
    }

    private FontName _GetFontByName(string fontName)
    {
        return _fontLookup.Find(entry =>
            string.Equals(entry.Name, fontName, StringComparison.OrdinalIgnoreCase));
    }

    public void AppendTextMeshProFontAssetCharacters(string fontName, HashSet<Char> characters)
    {
        FontName targetEntry = _GetFontByName(fontName);
        if (targetEntry == null)
        {
            Debug.LogWarningFormat("Font {0} does not exist, cannot append TextMesh Pro character set", fontName);
            return;
        }

        if (targetEntry.Font == null)
        {
            Debug.LogWarningFormat("Font {0} has no Font, cannot determine TextMesh Pro font source", fontName);
            return;
        }

        if (targetEntry.FontAsset == null)
        {
            Debug.LogWarningFormat("Font {0} has no TextMesh Pro FontAsset, cannot append character set", fontName);
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(targetEntry.FontAsset);
        string fontAssetConfigPath = _GetTextMeshProConfigPath(assetPath);

        if (!File.Exists(fontAssetConfigPath))
        {
            var newFontAssetConfig = new FontAssetConfig(AssetDatabase.GetAssetPath(targetEntry.Font));
            AssetDatabase.CreateAsset(newFontAssetConfig, fontAssetConfigPath);
            newFontAssetConfig.AppendCharacters(characters);
        }
        else
        {
            var fontAssetConfig = AssetDatabase.LoadAssetAtPath<FontAssetConfig>(fontAssetConfigPath);
            fontAssetConfig.AppendCharacters(characters);
        }

        AssetDatabase.SaveAssets();
    }

    public void UpdateTextMeshProFontAssets()
    {
        foreach (TMP_FontAsset fa in _fontLookup.Where(f => f.FontAsset != null).Select(f => f.FontAsset))
        {
            string fontAssetPath = AssetDatabase.GetAssetPath(fa);
            string fontAssetConfigPath = _GetTextMeshProConfigPath(fontAssetPath);
            var fontAssetConfig = AssetDatabase.LoadAssetAtPath<FontAssetConfig>(fontAssetConfigPath);
            if (fontAssetConfig == null)
            {
                continue;
            }

            FontAssetCreator.CreateFontAssetByConfig(fontAssetPath, fontAssetConfig);
        }
    }

    private static string _GetTextMeshProConfigPath(string textMeshProFontAssetPath)
    {
        return Path.GetDirectoryName(textMeshProFontAssetPath) +
               Path.DirectorySeparatorChar +
               Path.GetFileNameWithoutExtension(textMeshProFontAssetPath) +
               "_config.asset";
    }

    public bool IsTracked(string assetPath)
    {
        var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        return _trackedPsd.Contains(asset);
    }
}