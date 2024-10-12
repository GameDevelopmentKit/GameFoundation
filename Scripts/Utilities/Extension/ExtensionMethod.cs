namespace GameFoundation.Scripts.Utilities.Extension
{
    using Newtonsoft.Json;
    using UnityEngine;

    //<summary>
    //Manager all extension method
    //</summary>
    public static class ExtensionMethod
    {
        public static string ToJson<T>(this T obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static string GetPath(this Transform current)
        {
            if (current.parent == null) return current.name;
            return current.parent.GetPath() + "/" + current.name;
        }

        public static string Path(this Component component)
        {
            return GetPath(component.transform);
        }

        public static string Path(this GameObject gameObject)
        {
            return GetPath(gameObject.transform);
        }

        public static Vector2 AsUnityVector2(this System.Numerics.Vector2 v)
        {
            return new(v.X, v.Y);
        }

        public static Vector3 AsUnityVector3(this System.Numerics.Vector3 v)
        {
            return new(v.X, v.Y, v.Z);
        }
    }
}