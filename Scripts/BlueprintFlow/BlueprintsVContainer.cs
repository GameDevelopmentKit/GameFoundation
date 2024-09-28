#if GDK_VCONTAINER
#nullable enable
using BlueprintConfig = BlueprintFlow.BlueprintControlFlow.BlueprintConfig;
using BlueprintDownloader = BlueprintFlow.APIHandler.BlueprintDownloader;
using BlueprintReaderManager = BlueprintFlow.BlueprintControlFlow.BlueprintReaderManager;
using FetchBlueprintInfo = BlueprintFlow.APIHandler.FetchBlueprintInfo;
using IGenericBlueprintReader = BlueprintFlow.BlueprintReader.IGenericBlueprintReader;
using LoadBlueprintDataProgressSignal = BlueprintFlow.Signals.LoadBlueprintDataProgressSignal;
using LoadBlueprintDataSucceedSignal = BlueprintFlow.Signals.LoadBlueprintDataSucceedSignal;
using PreProcessBlueprintMobile = BlueprintFlow.BlueprintControlFlow.PreProcessBlueprintMobile;
using ReadBlueprintProgressSignal = BlueprintFlow.Signals.ReadBlueprintProgressSignal;

namespace GameFoundation.BlueprintFlow
{
    using GameFoundation.DI;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameFoundation.Signals;
    using Models;
    using VContainer;

    public static class BlueprintsVContainer
    {
        public static void RegisterBlueprints(this IContainerBuilder builder)
        {
            builder.Register<PreProcessBlueprintMobile>(Lifetime.Singleton);
            builder.Register<FetchBlueprintInfo>(Lifetime.Singleton);
            builder.Register<BlueprintDownloader>(Lifetime.Singleton);
            builder.Register<BlueprintReaderManager>(Lifetime.Singleton);
            builder.Register(container => container.Resolve<GDKConfig>().GetGameConfig<BlueprintConfig>(), Lifetime.Singleton);

            typeof(IGenericBlueprintReader).GetDerivedTypes().ForEach(type => builder.Register(type, Lifetime.Singleton).AsInterfacesAndSelf());

            builder.DeclareSignal<ReadBlueprintProgressSignal>();
            builder.DeclareSignal<LoadBlueprintDataProgressSignal>();
            builder.DeclareSignal<LoadBlueprintDataSucceedSignal>();
        }
    }
}
#endif