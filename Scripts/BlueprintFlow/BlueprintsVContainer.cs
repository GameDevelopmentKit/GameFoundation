#if GDK_VCONTAINER
#nullable enable
using BlueprintReaderManager = BlueprintFlow.BlueprintControlFlow.BlueprintReaderManager;
using IGenericBlueprintReader = BlueprintFlow.BlueprintReader.IGenericBlueprintReader;
using LoadBlueprintDataProgressSignal = BlueprintFlow.Signals.LoadBlueprintDataProgressSignal;
using LoadBlueprintDataSucceedSignal = BlueprintFlow.Signals.LoadBlueprintDataSucceedSignal;
using ReadBlueprintProgressSignal = BlueprintFlow.Signals.ReadBlueprintProgressSignal;

namespace GameFoundation.BlueprintFlow
{
    using GameFoundation.Signals;
    using TheOne.Data;
    using TheOne.Data.DI;
    using TheOne.Extensions;
    using VContainer;

    public static class BlueprintsVContainer
    {
        public static void RegisterBlueprints(this IContainerBuilder builder)
        {
            builder.Register<SeparatorConfig>(_ => new SeparatorConfig(","), Lifetime.Singleton);
            builder.RegisterDataManager();
            builder.Register<StringConverter>(Lifetime.Singleton).AsImplementedInterfaces();

            typeof(IGenericBlueprintReader).GetDerivedTypes().ForEach(type => builder.Register(type, Lifetime.Singleton).AsImplementedInterfaces().AsSelf());
            builder.Register<BlueprintReaderManager>(Lifetime.Singleton);

            builder.DeclareSignal<ReadBlueprintProgressSignal>();
            builder.DeclareSignal<LoadBlueprintDataProgressSignal>();
            builder.DeclareSignal<LoadBlueprintDataSucceedSignal>();
        }
    }
}
#endif