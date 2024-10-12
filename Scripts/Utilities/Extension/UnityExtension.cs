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

        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            var result = gameObject.GetComponent<T>();
            return result != null ? result : gameObject.AddComponent<T>();
        }

        public static T GetOrAddComponent<T>(this Transform transform) where T : Component
        {
            return GetOrAddComponent<T>(transform.gameObject);
        }
    }
}