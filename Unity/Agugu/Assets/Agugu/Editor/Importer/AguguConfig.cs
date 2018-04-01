using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;


[System.Serializable]
public class FontName
{
    public string Name;
    public Font Font;
}

[CreateAssetMenu]
public class AguguConfig : ScriptableObject
{
    [SerializeField]
    private List<FontName> _fontLookup = new List<FontName>();

    [SerializeField]
    private List<Object> _trackedPsd = new List<Object>();

    private static AguguConfig _instance;
    private Dictionary<string, Font> _lookUpTable;

    public static AguguConfig Instance
    {
        get
        {
            if (!_instance)
            {
                string assetGuid = AssetDatabase.FindAssets("t:AguguConfig").FirstOrDefault();
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                _instance = AssetDatabase.LoadAssetAtPath<AguguConfig>(assetPath);
                _instance.BuildTable();
            }
            return _instance;
        }
    }

    public void BuildTable()
    {
        _lookUpTable = new Dictionary<string, Font>();
        _fontLookup.ForEach(entry => _lookUpTable.Add(entry.Name, entry.Font));
    }

    public Font GetFont(string fontName)
    {
        Font outValue;
        _lookUpTable.TryGetValue(fontName, out outValue);
        return outValue;
    }

    public bool IsTracked(string assetPath)
    {
        var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        return _trackedPsd.Contains(asset);
    }
}