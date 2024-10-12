namespace GameFoundation.Scripts.Utilities.Utils
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class HttpRequestDefinitionAttribute : Attribute
    {
        public HttpRequestDefinitionAttribute(string route)
        {
            this.Route = route;
        }

        public string Route { get; set; }
    }
}