using System.Collections.Generic;

using UnityEngine;

namespace Agugu.Editor
{
    public class UiNode
    {
        public int    Id;
        public string Name;
        public bool   IsVisible;
        public bool   IsSkipped;

        public Vector2     Pivot;
        public XAnchorType XAnchor;
        public YAnchorType YAnchor;
        public Rect        Rect;

        public UiNode()
        {
        }

        public UiNode(UiNode copySource)
        {
            Id = copySource.Id;
            Name = copySource.Name;
            IsVisible = copySource.IsVisible;
            IsSkipped = copySource.IsSkipped;

            Pivot = copySource.Pivot;
            XAnchor = copySource.XAnchor;
            YAnchor = copySource.YAnchor;
            Rect = copySource.Rect;
        }

        public virtual void Accept(IUiNodeVisitor visitor)
        {
        }
    }


    public class GroupNode : UiNode
    {
        public List<UiNode> Children = new List<UiNode>();

        public bool HasScrollRect;
        public bool IsScrollRectHorizontal;
        public bool IsScrollRectVertical;

        public bool    HasGrid;
        public Vector2 CellSize;
        public Vector2 Spacing;

        public GroupNode()
        {
        }

        public GroupNode(UiNode baseNode) : base(baseNode)
        {
        }

        public void AddChild(UiNode node)
        {
            Children.Add(node);
        }

        public override void Accept(IUiNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class ImageNode : UiNode
    {
        public ISpriteSource SpriteSource;
        public WidgetType    WidgetType;

        public ImageNode()
        {
        }

        public ImageNode(UiNode baseNode) : base(baseNode)
        {
        }

        public override void Accept(IUiNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class TextNode : UiNode
    {
        public float  FontSize;
        public string FontName;

        public string Text;
        public Color  TextColor;

        public TextNode()
        {
        }

        public TextNode(UiNode baseNode) : base(baseNode)
        {
        }

        public override void Accept(IUiNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}