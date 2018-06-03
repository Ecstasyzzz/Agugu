namespace Agugu.Editor
{
    public interface IUiNodeVisitor
    {
        void Visit(GroupNode node);
        void Visit(TextNode  node);
        void Visit(ImageNode node);
    }
}