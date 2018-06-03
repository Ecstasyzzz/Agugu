using UnityEngine.UI;

namespace Agugu.Runtime
{
    public class EmptyGraphic : Graphic
    {
        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
        }
    }
}