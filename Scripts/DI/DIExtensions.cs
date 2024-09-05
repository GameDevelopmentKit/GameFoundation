#if !GDK_ZENJECT && !GDK_VCONTAINER
#nullable enable
namespace GameFoundation.DI
{
    using System;

    public static class DIExtensions
    {
        public static IDependencyContainer GetCurrentContainer()
        {
            throw new NotSupportedException("Please use Zenject or VContainer");
        }

        public static IDependencyContainer GetCurrentContainer(this object _) => GetCurrentContainer();
    }
}
#endif