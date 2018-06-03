using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;


[Serializable]
public class FontName
{
    public string Name;
    public Font   Font;
}

[CreateAssetMenu]
public class AguguConfig : ScriptableObject
{
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

    public Font GetFont(string fontName)
    {
        FontName targetEntry = _fontLookup.Find(entry =>
            string.Equals(entry.Name, fontName, StringComparison.OrdinalIgnoreCase));
        return targetEntry != null ? targetEntry.Font : null;
    }

    public bool IsTracked(string assetPath)
    {
        var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        return _trackedPsd.Contains(asset);
    }
}