using UnityEngine;

namespace Agugu.Editor
{
    public static class UiNodeUguiExtension
    {
        public static Vector2 GetAnchorMinValue(this UiTreeRoot uiTreeRoot)
        {
            float x = _GetAnchorMin(uiTreeRoot.XAnchor, 0);
            float y = _GetAnchorMin(uiTreeRoot.YAnchor, 0);
            return new Vector2(x, y);
        }

        public static Vector2 GetAnchorMaxValue(this UiTreeRoot uiTreeRoot)
        {
            float x = _GetAnchorMax(uiTreeRoot.XAnchor, 1);
            float y = _GetAnchorMax(uiTreeRoot.YAnchor, 1);
            return new Vector2(x, y);
        }

        public static Vector2 GetAnchorMinValue(this UiNode uiNode)
        {
            float x = _GetAnchorMin(uiNode.XAnchor);
            float y = _GetAnchorMin(uiNode.YAnchor);
            return new Vector2(x, y);
        }

        public static Vector2 GetAnchorMaxValue(this UiNode uiNode)
        {
            float x = _GetAnchorMax(uiNode.XAnchor);
            float y = _GetAnchorMax(uiNode.YAnchor);
            return new Vector2(x, y);
        }

        private static float _GetAnchorMin(XAnchorType xAnchor, float defaultValue = 0.5f)
        {
            switch (xAnchor)
            {
                case XAnchorType.Left: return 0;
                case XAnchorType.Center: return 0.5f;
                case XAnchorType.Right: return 1;
                case XAnchorType.Stretch: return 0;
                default: return defaultValue;
            }
        }

        private static float _GetAnchorMin(YAnchorType yAnchor, float defaultValue = 0.5f)
        {
            switch (yAnchor)
            {
                case YAnchorType.Bottom: return 0;
                case YAnchorType.Middle: return 0.5f;
                case YAnchorType.Top: return 1;
                case YAnchorType.Stretch: return 0;
                default: return defaultValue;
            }
        }

        private static float _GetAnchorMax(XAnchorType xAnchor, float defaultValue = 0.5f)
        {
            switch (xAnchor)
            {
                case XAnchorType.Left: return 0;
                case XAnchorType.Center: return 0.5f;
                case XAnchorType.Right: return 1;
                case XAnchorType.Stretch: return 1;
                default: return defaultValue;
            }
        }

        private static float _GetAnchorMax(YAnchorType yAnchor, float defaultValue = 0.5f)
        {
            switch (yAnchor)
            {
                case YAnchorType.Bottom: return 0;
                case YAnchorType.Middle: return 0.5f;
                case YAnchorType.Top: return 1;
                case YAnchorType.Stretch: return 1;
                default: return defaultValue;
            }
        }
    }
}