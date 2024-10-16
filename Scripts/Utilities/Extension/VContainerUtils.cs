#if GDK_VCONTAINER
namespace GameFoundation.Scripts.Utilities.Extension
{
    using System.Linq;
    using VContainer;

    public static class VContainerUtils
    {
        /// <summary>
        /// This method registers the type with its derived types.
        /// </summary>
        public static void RegisterFromDerivedType<T>(this IContainerBuilder builder, Lifetime lifetime = Lifetime.Singleton)
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