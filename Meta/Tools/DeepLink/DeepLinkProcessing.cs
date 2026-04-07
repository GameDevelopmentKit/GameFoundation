namespace DeepLink
{
    using System.Collections.Generic;
    using System.Linq;
    using DeepLink.Blueprint;
    using DeepLink.Handle;
    using DeepLink.Signal;
    using UnityEngine;
    using Zenject;

    public class DeepLinkProcessing : IInitializable
    {
        public static string URL_HOST = "unitydl://monster-hunter-takeover/";

        private readonly DeepLinkBlueprint                 blueprint;
        private readonly SignalBus                         signalBus;
        private readonly DiContainer                       diContainer;
        private          Dictionary<string, IActionHandle> middlewares;

        public DeepLinkProcessing(DiContainer diContainer, SignalBus signalBus, DeepLinkBlueprint blueprint)
        {
            this.signalBus   = signalBus;
            this.diContainer = diContainer;
            this.blueprint   = blueprint;

            Application.deepLinkActivated += this.OnDeepLinkActivated;
            this.signalBus.Subscribe<DeepLinkSignal>(this.OnDeepLinkActivated);

            if (!string.IsNullOrEmpty(Application.absoluteURL)) this.OnDeepLinkActivated(Application.absoluteURL);
        }

        public void Initialize() { this.middlewares = this.diContainer.ResolveAll<IActionHandle>().ToDictionary(middleware => middleware.Type); }

        private void OnDeepLinkActivated(DeepLinkSignal signal) { this.OnDeepLinkActivated(signal.DeeplinkUri); }

        private async void OnDeepLinkActivated(string deeplinkUri)
        {
            var blueprintIds = deeplinkUri.Replace(URL_HOST, string.Empty).Split('/');

            foreach (var blueprintId in blueprintIds)
            {
                if (this.blueprint.TryGetValue(blueprintId, out var record))
                {
                    var middlewareId = record.ActionId;

                    if (this.GetMiddleware(middlewareId, out var middleware))
                    {
                        await middleware.Process(record.Value);
                    }
                }
                else
                {
                    Debug.LogError($"DeepLinkProcessing: No record found for {blueprintIds}");
                }
            }
        }

        private bool GetMiddleware(string routeType, out IActionHandle middleware) { return this.middlewares.TryGetValue(routeType, out middleware); }
    }
}