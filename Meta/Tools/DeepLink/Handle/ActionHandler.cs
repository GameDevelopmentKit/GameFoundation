namespace DeepLink.Handle
{
    using System;
    using System.Globalization;
    using Cysharp.Threading.Tasks;
    using JetBrains.Annotations;
    using Newtonsoft.Json;

    public interface IActionHandle : IDisposable
    {
        string  Type { get; }
        UniTask Process(string data);
    }

    public interface IActionData
    {
    }

    public abstract class ActionHandler<T> : IActionHandle
    {
        public abstract string Type { get; }

        public async UniTask Process(string data) { await this.ProcessInternal(this.DeserializeData(data)); }

        [CanBeNull]
        private T DeserializeData(string data)
        {
            return System.Type.GetTypeCode(typeof(T)) switch
            {
                TypeCode.String => (T)(object)data,
                TypeCode.Int32 => (T)(object)int.Parse(data),
                TypeCode.Single => (T)(object)float.Parse(data, CultureInfo.InvariantCulture),
                TypeCode.Double => (T)(object)double.Parse(data, CultureInfo.InvariantCulture),
                TypeCode.Boolean => (T)(object)bool.Parse(data),
                TypeCode.Int64 => (T)(object)long.Parse(data),
                _ => JsonConvert.DeserializeObject<T>(data)
            };
        }

        protected abstract UniTask ProcessInternal(T data);
        public             void    Dispose() { }
    }
}