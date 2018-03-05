using UnityEngine;

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
}