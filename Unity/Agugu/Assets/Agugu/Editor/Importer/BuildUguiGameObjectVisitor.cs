using UnityEngine;
using UnityEngine.UI;

public class BuildUguiGameObjectVisitor : IUiNodeVisitor
{
    private readonly Rect _parentRect;
    private readonly RectTransform _parent;

    public BuildUguiGameObjectVisitor(Rect parentRect, RectTransform parent)
    {
        _parentRect = parentRect;
        _parent = parent;
    }

    public GameObject Visit(UiTreeRoot root)
    {
        var canvasGameObject = _CreateCanvasGameObject(root.Width, root.Height);
        canvasGameObject.AddComponent<GenericView>();

        var canvasRectTransform = canvasGameObject.GetComponent<RectTransform>();
        canvasRectTransform.ForceUpdateRectTransforms();

        var childrenVisitor = new BuildUguiGameObjectVisitor(new Rect(0,0,root.Width,root.Height), canvasRectTransform);
        root.Children.ForEach(child => child.Accept(childrenVisitor));

        return canvasGameObject;
    }

    private static GameObject _CreateCanvasGameObject(float width, float height)
    {
        var canvasGameObject = new GameObject("Canvas");

        var canvas = canvasGameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var canvasScaler = canvasGameObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.referenceResolution = new Vector2(width, height);
        canvasScaler.matchWidthOrHeight = 0;

        var graphicRaycaster = canvasGameObject.AddComponent<GraphicRaycaster>();

        return canvasGameObject;
    }

    public void Visit(GroupNode node)
    {
        var groupGameObject = new GameObject(node.Name);
        var groupRectTransform = groupGameObject.AddComponent<RectTransform>();
        groupGameObject.transform.SetParent(_parent, worldPositionStays: false);

        if (node.XAnchor == XAnchorType.Stretch)
        {
            groupRectTransform.anchorMin = groupRectTransform.anchorMin.GetXOverwriteCopy(0);
            groupRectTransform.anchorMax = groupRectTransform.anchorMax.GetXOverwriteCopy(1);

            groupRectTransform.sizeDelta = groupRectTransform.sizeDelta.GetXOverwriteCopy(0);
        }

        if (node.YAnchor == YAnchorType.Stretch)
        {
            groupRectTransform.anchorMin = groupRectTransform.anchorMin.GetYOverwriteCopy(0);
            groupRectTransform.anchorMax = groupRectTransform.anchorMax.GetYOverwriteCopy(1);

            groupRectTransform.sizeDelta = groupRectTransform.sizeDelta.GetYOverwriteCopy(0);
        }

        var childrenVisitor = new BuildUguiGameObjectVisitor(_parentRect, groupRectTransform);
        node.Children.ForEach(child => child.Accept(childrenVisitor));
    }

    public void Visit(TextNode node)
    {
        var uiGameObject = new GameObject(node.Name);
        var uiRectTransform = uiGameObject.AddComponent<RectTransform>();

        var text = uiGameObject.AddComponent<Text>();
        text.text = node.Text;
        text.color = node.TextColor;
        text.font = AguguFontLookup.Instance.GetFont(node.FontName);
        // TODO: Wild guess, cannot find any reference about Unity font size
        // 25/6
        text.fontSize = (int)(node.FontSize / 4.16);
        text.resizeTextForBestFit = true;

        _SetRectTransform(uiRectTransform,
            node.Rect.xMin, node.Rect.xMax,
            node.Rect.yMin, node.Rect.yMax,
            node.Rect.width, node.Rect.height * 1.3f,
            _parentRect.width, _parentRect.height);

        uiGameObject.transform.SetParent(_parent, worldPositionStays: false);
    }

    public void Visit(ImageNode node)
    {
        var importedSprite = node.SpriteSource.GetSprite();

        var uiGameObject = new GameObject(node.Name);
        var uiRectTransform = uiGameObject.AddComponent<RectTransform>();
        var image = uiGameObject.AddComponent<Image>();
        image.sprite = importedSprite;

        _SetRectTransform(uiRectTransform,
            node.Rect.xMin, node.Rect.xMax,
            node.Rect.yMin, node.Rect.yMax,
            node.Rect.width, node.Rect.height,
            _parentRect.width, _parentRect.height);

        // Have to set localPosition before parenting
        // Or the last imported layer will be reset to 0, 0, 0, I think it's a bug :(
        uiGameObject.transform.SetParent(_parent, worldPositionStays: false);

        if (node.WidgetType == WidgetType.Button)
        {
            uiGameObject.AddComponent<Button>();
        }

        switch (node.XAnchor)
        {
            case XAnchorType.Left:
                uiRectTransform.anchorMin = uiRectTransform.anchorMin.GetXOverwriteCopy(0);
                uiRectTransform.anchorMax = uiRectTransform.anchorMax.GetXOverwriteCopy(0);
                uiRectTransform.anchoredPosition =
                    uiRectTransform.anchoredPosition.GetXOverwriteCopy(uiRectTransform.sizeDelta.x * 0.5f);
                break;
            case XAnchorType.Center:
                uiRectTransform.anchorMin = uiRectTransform.anchorMin.GetXOverwriteCopy(0.5f);
                uiRectTransform.anchorMax = uiRectTransform.anchorMax.GetXOverwriteCopy(0.5f);
                break;
            case XAnchorType.Right:
                uiRectTransform.anchorMin = uiRectTransform.anchorMin.GetXOverwriteCopy(1);
                uiRectTransform.anchorMax = uiRectTransform.anchorMax.GetXOverwriteCopy(1);
                uiRectTransform.anchoredPosition =
                    uiRectTransform.anchoredPosition.GetXOverwriteCopy(uiRectTransform.sizeDelta.x * -0.5f);
                break;
            case XAnchorType.Stretch:
                float size = uiRectTransform.sizeDelta.x;
                uiRectTransform.anchorMin = uiRectTransform.anchorMin.GetXOverwriteCopy(0);
                uiRectTransform.anchorMax = uiRectTransform.anchorMax.GetXOverwriteCopy(1);

                uiRectTransform.sizeDelta =
                    uiRectTransform.sizeDelta.GetXOverwriteCopy(node.Rect.width - _parentRect.width);
                break;
        }

        switch (node.YAnchor)
        {
            case YAnchorType.Bottom:
                uiRectTransform.anchorMin = uiRectTransform.anchorMin.GetYOverwriteCopy(0);
                uiRectTransform.anchorMax = uiRectTransform.anchorMax.GetYOverwriteCopy(0);
                uiRectTransform.anchoredPosition =
                    uiRectTransform.anchoredPosition.GetYOverwriteCopy(uiRectTransform.sizeDelta.y * 0.5f);
                break;
            case YAnchorType.Center:
                uiRectTransform.anchorMin = uiRectTransform.anchorMin.GetYOverwriteCopy(0.5f);
                uiRectTransform.anchorMax = uiRectTransform.anchorMax.GetYOverwriteCopy(0.5f);
                break;
            case YAnchorType.Top:
                uiRectTransform.anchorMin = uiRectTransform.anchorMin.GetYOverwriteCopy(1);
                uiRectTransform.anchorMax = uiRectTransform.anchorMax.GetYOverwriteCopy(1);
                uiRectTransform.anchoredPosition =
                    uiRectTransform.anchoredPosition.GetYOverwriteCopy(uiRectTransform.sizeDelta.y * -0.5f);
                break;
            case YAnchorType.Stretch:
                uiRectTransform.anchorMin = uiRectTransform.anchorMin.GetYOverwriteCopy(0);
                uiRectTransform.anchorMax = uiRectTransform.anchorMax.GetYOverwriteCopy(1);

                uiRectTransform.sizeDelta =
                    uiRectTransform.sizeDelta.GetYOverwriteCopy(node.Rect.height - _parentRect.height);
                break;
        }
    }

    private static void _SetRectTransform
    (
        RectTransform rectTransform,
        float left, float right,
        float bottom, float top,
        float width, float height,
        float parentWidth, float parentHeight
    )
    {
        var psdLayerCenter = new Vector2((left + right) / 2, (bottom + top) / 2);

        rectTransform.sizeDelta = new Vector2(width, height);
        rectTransform.localPosition = new Vector3
        (
            psdLayerCenter.x - parentWidth / 2,
            psdLayerCenter.y
        );
    }
}