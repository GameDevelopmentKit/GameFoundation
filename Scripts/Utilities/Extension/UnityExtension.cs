namespace GameFoundation.Scripts.Utilities.Extension
{
    using UnityEngine;

    public static class UnityExtension
    {
        public static Color CloneAndSetAlpha(this Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}