using UnityEngine;
using UnityEngine.UI;

using Agugu.Runtime;
using TMPro;

namespace Agugu.Editor
{
    public class BuildUguiGameObjectVisitor : IUiNodeVisitor
    {
        private readonly Rect _parentRect;
        private readonly RectTransform _parent;
        private readonly float _basePixelPerInch;

        public BuildUguiGameObjectVisitor
        (
            Rect parentRect, 
            RectTransform parent,
            float basePixelPerInch
        )
        {
            _parentRect = parentRect;
            _parent = parent;
            _basePixelPerInch = basePixelPerInch;
        }

        public GameObject Visit(UiTreeRoot root)
        {
            var uiRootGameObject = new GameObject(root.Name);
            var uiRootRectTransform = uiRootGameObject.AddComponent<RectTransform>();
            var fullDocumentRect = new Rect(0, 0, root.Width, root.Height);

            _SetPsdLayerIdTag(uiRootGameObject, -1);

            _SetRectTransform
            (
                uiRootRectTransform,
                fullDocumentRect, fullDocumentRect,
                root.GetAnchorMinValue(), root.GetAnchorMaxValue(),
                root.Pivot
            );
            uiRootRectTransform.ForceUpdateRectTransforms();

            var childrenVisitor = new BuildUguiGameObjectVisitor(fullDocumentRect, uiRootRectTransform, _basePixelPerInch);
            root.Children.ForEach(child => child.Accept(childrenVisitor));

            return uiRootGameObject;
        }


        public void Visit(GroupNode node)
        {
            if (node.IsSkipped) { return; }

            var groupGameObject = new GameObject(node.Name);
            var groupRectTransform = groupGameObject.AddComponent<RectTransform>();

            _SetPsdLayerIdTag(groupGameObject, node.Id);

            _SetRectTransform
            (
                groupRectTransform,
                node.Rect, _parentRect,
                node.GetAnchorMinValue(), node.GetAnchorMaxValue(),
                node.Pivot
            );

            var childrenVisitor = new BuildUguiGameObjectVisitor(node.Rect, groupRectTransform, _basePixelPerInch);
            node.Children.ForEach(child => child.Accept(childrenVisitor));

            groupRectTransform.SetParent(_parent, worldPositionStays: false);
            groupGameObject.SetActive(node.IsVisible);
        }


        public void Visit(TextNode node)
        {
            if (node.IsSkipped) { return; }

            GameObject textGameObject = _CreateText(node);
            _SetPsdLayerIdTag(textGameObject, node.Id);
        }

        private GameObject _CreateText(TextNode node)
        {
            if (AguguConfig.Instance.TextComponentType == TextComponentType.uGUI)
            {
                return _CreateUguiText(node);
            }
            else if (AguguConfig.Instance.TextComponentType == TextComponentType.TextMeshPro)
            {
                return _CreateTextMeshPro(node);
            }

            return null;
        }

        private GameObject _CreateUguiText(TextNode node)
        {
            var textGameObject    = new GameObject(node.Name);
            var textRectTransform = textGameObject.AddComponent<RectTransform>();

            var text = textGameObject.AddComponent<Text>();
            text.text = node.Text;
            text.color = node.TextColor;
            Font font = AguguConfig.Instance.GetFont(node.FontName);
            if (font == null)
            {
                Debug.LogWarningFormat("Font not found: {0}, at {1}", node.FontName, node.Name);
            }

            text.font = font;
            // Photoshop uses 72 points per inch
            text.fontSize = (int)(node.FontSize / 72 * _basePixelPerInch);
            text.resizeTextForBestFit = true;

            var originalHeight = node.Rect.height;
            var halfHeight     = originalHeight / 2;
            var adjustedRect   = new Rect(node.Rect);
            adjustedRect.yMin -= halfHeight;
            adjustedRect.yMax += halfHeight;

            _SetRectTransform
            (
                textRectTransform,
                adjustedRect, _parentRect,
                node.GetAnchorMinValue(), node.GetAnchorMaxValue(),
                node.Pivot
            );

            textRectTransform.SetParent(_parent, worldPositionStays: false);
            textGameObject.SetActive(node.IsVisible);

            return textGameObject;
        }

        private GameObject _CreateTextMeshPro(TextNode node)
        {
            var textGameObject    = new GameObject(node.Name);
            var textRectTransform = textGameObject.AddComponent<RectTransform>();

            var text = textGameObject.AddComponent<TextMeshProUGUI>();
            text.text = node.Text;
            text.color = node.TextColor;
            text.fontSize = node.FontSize;

            TMP_FontAsset font = AguguConfig.Instance.GetTextMeshProFontAsset(node.FontName);
            if (font == null)
            {
                Debug.LogWarningFormat("Font not found: {0}, at {1}", node.FontName, node.Name);
            }
            else
            {
                text.font = font;
            }


            _SetRectTransform
            (
                textRectTransform,
                node.Rect, _parentRect,
                node.GetAnchorMinValue(), node.GetAnchorMaxValue(),
                node.Pivot
            );

            textRectTransform.SetParent(_parent, worldPositionStays: false);
            textGameObject.SetActive(node.IsVisible);

            return textGameObject;
        }


        public void Visit(ImageNode node)
        {
            if (node.IsSkipped) { return; }

            GameObject imageGameObject = _CreateImage(node);
            _SetPsdLayerIdTag(imageGameObject, node.Id);
        }

        private GameObject _CreateImage(ImageNode node)
        {
            var imageGameObject = new GameObject(node.Name);
            var uiRectTransform = imageGameObject.AddComponent<RectTransform>();

            if (node.WidgetType != WidgetType.EmptyGraphic)
            {
                Sprite importedSprite = node.SpriteSource.GetSprite();
                var image = imageGameObject.AddComponent<Image>();
                image.sprite = importedSprite;
            }
            else
            {
                imageGameObject.AddComponent<EmptyGraphic>();
            }

            _SetRectTransform
            (
                uiRectTransform,
                node.Rect, _parentRect,
                node.GetAnchorMinValue(), node.GetAnchorMaxValue(),
                node.Pivot
            );

            // Have to set localPosition before parenting
            // Or the last imported layer will be reset to 0, 0, 0, I think it's a bug :(
            imageGameObject.transform.SetParent(_parent, worldPositionStays: false);
            imageGameObject.SetActive(node.IsVisible);

            return imageGameObject;
        }


        public void Visit(ImageTextNode node)
        {
            if (node.IsSkipped) { return; }

            // Skip PSD Layer Tagging since ImageTextNode is for debugging
            _CreateImage(node.Image);
            _CreateText(node.Text);
        }


        private static void _SetPsdLayerIdTag(GameObject gameObject, int layerId)
        {
            var layerIdTag = gameObject.AddComponent<PsdLayerIdTag>();
            layerIdTag.LayerId = layerId;
        }

        private static void _SetRectTransform
        (
            RectTransform rectTransform,
            Rect rect, Rect parentRect,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot
        )
        {
            Vector2 anchorMinPosition = Vector2Extension.LerpUnclamped(parentRect.min, parentRect.max, anchorMin);
            Vector2 anchorMaxPosition = Vector2Extension.LerpUnclamped(parentRect.min, parentRect.max, anchorMax);
            Vector2 anchorSize = anchorMaxPosition - anchorMinPosition;
            Vector2 anchorReferencePosition = Vector2Extension.LerpUnclamped(anchorMinPosition, 
                                                                             anchorMaxPosition, pivot);
            Vector2 pivotPosition = Vector2Extension.LerpUnclamped(rect.min, rect.max, pivot);

            rectTransform.pivot = pivot;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.anchoredPosition = pivotPosition - anchorReferencePosition;
            rectTransform.sizeDelta = new Vector2(rect.width, rect.height) - anchorSize;
        }
    }
}