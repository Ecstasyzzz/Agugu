using UnityEngine;

namespace Agugu.Editor
{
    public static class Vector2Extension
    {
        public static Vector2 GetXOverwriteCopy(this Vector2 target, float x)
        {
            return new Vector2(x, target.y);
        }

        public static Vector2 GetYOverwriteCopy(this Vector2 target, float y)
        {
            return new Vector2(target.x, y);
        }

        public static Vector2 LerpUnclamped(Vector2 a, Vector2 b, Vector2 t)
        {
            return new Vector2(Mathf.LerpUnclamped(a.x, b.x, t.x),
                Mathf.LerpUnclamped(a.y, b.y, t.y));
        }
    }
}