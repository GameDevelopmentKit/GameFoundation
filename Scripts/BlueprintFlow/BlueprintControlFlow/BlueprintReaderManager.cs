namespace BlueprintFlow.BlueprintControlFlow
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using BlueprintFlow.BlueprintReader;
    using BlueprintFlow.Signals;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Signals;
    using TheOne.Data;
    using TheOne.Extensions;
    using UnityEngine.Scripting;

    public sealed class BlueprintReaderManager
    {
        private readonly IDataManager                           dataManager;
        private readonly IReadOnlyList<IGenericBlueprintReader> blueprints;
        private readonly SignalBus                              signalBus;

        [Preserve]
        public BlueprintReaderManager(IDataManager dataManager, IEnumerable<IGenericBlueprintReader> blueprints, SignalBus signalBus)
        {
            this.dataManager = dataManager;
            this.signalBus   = signalBus;
            this.blueprints  = blueprints.ToArray();
        }

        public async UniTask LoadBlueprint()
        {
            await this.blueprints.ForEachAsync(blueprint => this.dataManager.LoadAsync($"BlueprintData/{blueprint.GetType().GetCustomAttribute<BlueprintReaderAttribute>().Key}.csv", blueprint.GetType()).ContinueWith(data => data.CopyTo(blueprint)));
            this.signalBus.Fire(new LoadBlueprintDataProgressSignal(1f));
            this.signalBus.Fire(new ReadBlueprintProgressSignal(this.blueprints.Count, this.blueprints.Count));
            this.signalBus.Fire(new LoadBlueprintDataSucceedSignal());
        }
    }
}