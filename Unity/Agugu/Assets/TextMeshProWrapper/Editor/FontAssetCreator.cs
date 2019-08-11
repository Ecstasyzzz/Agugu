using System.Collections.Generic;

using UnityEngine;

using TMPro.EditorUtilities;

namespace TextMeshProWrapper
{
    public class FontAssetCreator
    {
        public static void CreateFontAssetByConfig(string targetPath, FontAssetConfig config)
        {
            var  fontAssetCreator = ScriptableObject.CreateInstance<TMPro_FontAssetCreatorWindow>();
            bool isAutoSizing     = config.FontSizeOption == FontSizeOption.AutoSizing;

            int fontPluginInitializeStatusCode = TMPro_FontPlugin.Initialize_FontEngine();
            if (fontPluginInitializeStatusCode != 0 && fontPluginInitializeStatusCode != 240)
            {
                Debug.LogWarning(
                    "Cannot initialize TextMesh Pro font plugin, status: " + fontPluginInitializeStatusCode);
                return;
            }

            int fontPluginLoadStatusCode = TMPro_FontPlugin.Load_TrueType_Font(config.FontSourcePath);
            if (fontPluginLoadStatusCode != 0 && fontPluginLoadStatusCode != 241)
            {
                Debug.LogWarning("TextMesh Pro font plugin cannot load font, status: " + fontPluginLoadStatusCode);
                return;
            }

            int fontPluginSetFontSizeStatusCode =
                TMPro_FontPlugin.FT_Size_Font(isAutoSizing ? 72 : config.CustomFontSize);

            if (fontPluginSetFontSizeStatusCode != 0)
            {
                Debug.LogWarning("TextMesh Pro font plugin cannot set font size, status: " +
                                 fontPluginSetFontSizeStatusCode);
                return;
            }

            var characterList = new List<int>();
            foreach (var c in config.CharacterSet)
            {
                characterList.Add(c);
            }

            var textureBuffer = new byte[config.AtlasWidth * config.AtlasHeight];
            var faceInfo      = new FT_FaceInfo();
            var glyphInfo     = new FT_GlyphInfo[config.CharacterSet.Count];

            TMPro_FontPlugin.Render_Characters
            (
                textureBuffer,
                config.AtlasWidth,
                config.AtlasHeight,
                config.FontPadding,
                characterList.ToArray(),
                characterList.Count,
                FaceStyles.Normal,
                2,
                isAutoSizing,
                RenderModes.DistanceField16,
                (int) config.FontPackingMode,
                ref faceInfo,
                glyphInfo
            );


            System.Reflection.BindingFlags nonPublicInstance =
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;



            var fontAssetCreatorWindowType = fontAssetCreator.GetType();
            fontAssetCreatorWindowType.GetField("characterSequence", nonPublicInstance)
                                      .SetValue(fontAssetCreator, config.CharacterSetString);
            fontAssetCreatorWindowType.GetField("m_font_faceInfo", nonPublicInstance)
                                      .SetValue(fontAssetCreator, faceInfo);
            fontAssetCreatorWindowType.GetField("m_font_glyphInfo", nonPublicInstance)
                                      .SetValue(fontAssetCreator, glyphInfo);
            fontAssetCreatorWindowType.GetField("m_texture_buffer", nonPublicInstance)
                                      .SetValue(fontAssetCreator, textureBuffer);

            var funcCreateTexture = fontAssetCreator.GetType().GetMethod
            (
                "CreateFontTexture",
                nonPublicInstance
            );

            funcCreateTexture.Invoke(fontAssetCreator, null);

            var funcSaveSdf = fontAssetCreator.GetType().GetMethod
            (
                "Save_SDF_FontAsset",
                nonPublicInstance, null,
                new[] {typeof(string)},
                null
            );

            var fontAssetPathParam = Application.dataPath + targetPath.Remove(0, "Assets".Length);
            Debug.Log(fontAssetPathParam);
            funcSaveSdf.Invoke(fontAssetCreator, new[]
            {
                fontAssetPathParam
            });
        }
    }
}