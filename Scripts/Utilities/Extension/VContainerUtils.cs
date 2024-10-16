#if GDK_VCONTAINER
namespace GameFoundation.Scripts.Utilities.Extension
{
    using System.Linq;
    using VContainer;

    public static class VContainerUtils
    {
        /// <summary>
        /// Same as Zenject's ReBind method.
        /// </summary>
        public static void RegisterWithDerivedTypes<T>(this IContainerBuilder builder, Lifetime lifetime = Lifetime.Singleton)
        {
            var registerType = typeof(T)
                               .GetDerivedTypes()
                               .OrderBy(type => type == typeof(T))
                               .First();
            builder.Register(registerType, lifetime).As<T>();
        }
    }
}
#endif