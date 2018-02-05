using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu]
public class AguguFontLookup : ScriptableObject
{
    [SerializeField]
    private List<string> _fontNames = new List<string>();

    [SerializeField]
    private List<Font> _fonts = new List<Font>();

    private static AguguFontLookup _instance;
    private Dictionary<string, Font> _lookUpTable;

    public static AguguFontLookup Instance
    {
        get
        {
            if (!_instance)
            {
                string assetGuid = AssetDatabase.FindAssets("t:AguguFontLookup").FirstOrDefault();
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                _instance = AssetDatabase.LoadAssetAtPath<AguguFontLookup>(assetPath);
                _instance.BuildTable();
            }
            return _instance;
        }
    }

    public void BuildTable()
    {
        _lookUpTable = new Dictionary<string, Font>();
        for (int i = 0; i < _fontNames.Count; i++)
        {
            _lookUpTable.Add(_fontNames[i], _fonts[i]);
        }
    }

    public Font GetFont(string fontName)
    {
        Font outValue;
        _lookUpTable.TryGetValue(fontName, out outValue);
        return outValue;
    }
}