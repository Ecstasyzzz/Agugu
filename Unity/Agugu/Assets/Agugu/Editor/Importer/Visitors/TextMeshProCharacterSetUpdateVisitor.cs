using System.Collections.Generic;

namespace Agugu.Editor
{
    public class TextMeshProCharacterSetUpdateVisitor : IUiNodeVisitor
    {
        private readonly Dictionary<string, HashSet<char>> _usedCharacters = 
                     new Dictionary<string, HashSet<char>>();

        public void Visit(UiTreeRoot root)
        {
            if (AguguConfig.Instance.TextComponentType == TextComponentType.TextMeshPro)
            {
                root.Children.ForEach(child => child.Accept(this));
                foreach (var cs in _usedCharacters)
                {
                    AguguConfig.Instance.AppendTextMeshProFontAssetCharacters(cs.Key, cs.Value);
                }
            }
        }

        public void Visit(GroupNode node)
        {
            if (!node.IsSkipped)
            {
                node.Children.ForEach(child => child.Accept(this));
            }
        }

        public void Visit(TextNode node)
        {
            if (!_usedCharacters.ContainsKey(node.FontName))
            {
                _usedCharacters.Add(node.FontName, new HashSet<char>());
            }

            foreach (char c in node.Text)
            {
                _usedCharacters[node.FontName].Add(c);
            }
        }

        public void Visit(ImageNode node)
        {
        }

        public void Visit(ImageTextNode node)
        {
            Visit(node.Text);
        }
    }
}