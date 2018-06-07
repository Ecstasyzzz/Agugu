using System.Collections.Generic;

using UnityEngine;

namespace Agugu.Editor
{
    public class UiTreeRoot
    {
        public string Name;

        public float Width;
        public float Height;

        public Vector2     Pivot;
        public XAnchorType XAnchor;
        public YAnchorType YAnchor;

        public float HorizontalPixelPerInch;

        public PsdLayerConfigSet Configs  = new PsdLayerConfigSet();
        public List<UiNode>    Children = new List<UiNode>();

        public void AddChild(UiNode node)
        {
            Children.Add(node);
        }
    }
}