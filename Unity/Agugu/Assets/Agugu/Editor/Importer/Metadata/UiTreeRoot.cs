using System.Collections.Generic;

namespace Agugu.Editor
{
    public class UiTreeRoot
    {
        public string Name;

        public float Width;
        public float Height;

        public PsdLayerConfigs Configs  = new PsdLayerConfigs();
        public List<UiNode>    Children = new List<UiNode>();

        public void AddChild(UiNode node)
        {
            Children.Add(node);
        }
    }
}