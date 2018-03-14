using System.Collections.Generic;

using UnityEngine;


public class UiNode
{
    public int Id;
    public string Name;

    public XAnchorType XAnchor;
    public YAnchorType YAnchor;
    public Rect Rect;

    public virtual void Accept(IUiNodeVisitor visitor) { }
}


public class GroupNode : UiNode
{
    public List<UiNode> Children = new List<UiNode>();

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
    public WidgetType WidgetType;

    public override void Accept(IUiNodeVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public class TextNode : UiNode
{
    public float FontSize;
    public string FontName;

    public string Text;
    public Color TextColor;

    public override void Accept(IUiNodeVisitor visitor)
    {
        visitor.Visit(this);
    }
}