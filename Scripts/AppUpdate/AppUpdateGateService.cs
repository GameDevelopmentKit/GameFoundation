namespace GameFoundation.Scripts.AppUpdate
{
    using System;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Utilities.ApplicationServices;
    using UnityEngine;

    public enum AppUpdateGateStatus
    {
        NoUpdate,
        OptionalSkippedByCooldown,
        OptionalShown,
        ConfigError,
    }

    public class AppUpdateGateResult
    {
        public AppUpdateGateStatus Status;
        public AppUpdateDecision   Decision;
        public string              Message;
    }

    public class AppUpdateGateService
    {
        private const string OptionalPromptNextUtcTicksKeyPrefix = "GameFoundation.AppUpdate.OptionalPromptNextUtcTicks.";

        private readonly IAppUpdateConfigProvider configProvider;
        private readonly IApplicationBuildInfoProvider buildInfoProvider;
        private readonly AppUpdatePolicy          policy;
        private readonly IAppUpdateTextProvider   textProvider;
        private readonly IAppUpdatePromptService  promptService;

        public AppUpdateGateService(
            IAppUpdateConfigProvider configProvider,
            IApplicationBuildInfoProvider buildInfoProvider,
            AppUpdatePolicy policy,
            IAppUpdateTextProvider textProvider,
            IAppUpdatePromptService promptService)
        {
            this.configProvider    = configProvider;
            this.buildInfoProvider = buildInfoProvider;
            this.policy            = policy;
            this.textProvider      = textProvider;
            this.promptService     = promptService;
        }

        public async UniTask<AppUpdateGateResult> CheckForUpdateAsync()
        {
            AppUpdateConfig config;

            try
            {
                config = await this.configProvider.GetConfigAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppUpdate] Failed to load config: {ex.Message}");
                return this.Result(AppUpdateGateStatus.ConfigError, null, "Failed to load config.");
            }

            var buildInfo = this.buildInfoProvider.GetBuildInfo();
            if (buildInfo == null)
            {
                Debug.LogError("[AppUpdate] Build info provider returned null. Continuing startup.");
                return this.Result(AppUpdateGateStatus.ConfigError, null, "Build info unavailable.");
            }

            var decision  = this.policy.Evaluate(config, buildInfo);

            if (decision.Type == AppUpdateDecisionType.None)
            {
                Debug.Log($"[AppUpdate] No update prompt. {decision.Reason}");
                return this.Result(AppUpdateGateStatus.NoUpdate, decision, decision.Reason);
            }

            if (string.IsNullOrWhiteSpace(decision.StoreUrl))
            {
                Debug.LogError("[AppUpdate] Update required but store URL is empty. Continuing to avoid trapping the player.");
                return this.Result(AppUpdateGateStatus.ConfigError, decision, "Missing store URL.");
            }

            if (decision.IsOptionalUpdate)
            {
                return await this.HandleOptionalUpdateAsync(decision);
            }

            await this.BlockForForceUpdateAsync(decision);
            return this.Result(AppUpdateGateStatus.ConfigError, decision, "Force update gate unexpectedly completed.");
        }

        private async UniTask<AppUpdateGateResult> HandleOptionalUpdateAsync(AppUpdateDecision decision)
        {
            if (this.IsOptionalPromptCoolingDown(decision))
            {
                return this.Result(AppUpdateGateStatus.OptionalSkippedByCooldown, decision, "Optional prompt cooldown is active.");
            }

            var result = await this.promptService.ShowPromptAsync(decision, this.textProvider.GetTexts(decision));
            this.SaveOptionalPromptCooldown(decision);

            if (result == AppUpdatePromptResult.Update)
            {
                this.OpenStoreUrl(decision);
            }

            return this.Result(AppUpdateGateStatus.OptionalShown, decision, "Optional update prompt shown.");
        }

        private async UniTask BlockForForceUpdateAsync(AppUpdateDecision decision)
        {
            while (true)
            {
                var result = await this.promptService.ShowPromptAsync(decision, this.textProvider.GetTexts(decision));
                if (result == AppUpdatePromptResult.Update)
                {
                    this.OpenStoreUrl(decision);
                }

                await UniTask.Delay(1000);
            }
        }

        private void OpenStoreUrl(AppUpdateDecision decision)
        {
            try
            {
                Application.OpenURL(decision.StoreUrl);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppUpdate] Failed to open store URL '{decision.StoreUrl}': {ex.Message}");
            }
        }

        private bool IsOptionalPromptCoolingDown(AppUpdateDecision decision)
        {
            if ((decision.Config?.SoftPromptCooldownHours ?? 0) <= 0)
            {
                return false;
            }

            var key = this.GetOptionalPromptCooldownKey(decision);
            if (!long.TryParse(PlayerPrefs.GetString(key, string.Empty), out var nextPromptUtcTicks))
            {
                return false;
            }

            return DateTime.UtcNow.Ticks < nextPromptUtcTicks;
        }

        private void SaveOptionalPromptCooldown(AppUpdateDecision decision)
        {
            var hours = decision.Config?.SoftPromptCooldownHours ?? 0;
            if (hours <= 0) return;

            var key = this.GetOptionalPromptCooldownKey(decision);
            PlayerPrefs.SetString(key, DateTime.UtcNow.AddHours(hours).Ticks.ToString());
            PlayerPrefs.Save();
        }

        private string GetOptionalPromptCooldownKey(AppUpdateDecision decision)
        {
            var updateTarget = $"{decision.PlatformConfig?.LatestVersion}_{decision.PlatformConfig?.LatestBuild}";

            return $"{OptionalPromptNextUtcTicksKeyPrefix}{decision.BuildInfo.Platform}.{updateTarget}";
        }

        private AppUpdateGateResult Result(AppUpdateGateStatus status, AppUpdateDecision decision, string message)
        {
            return new AppUpdateGateResult
            {
                Status   = status,
                Decision = decision,
                Message  = message,
            };
        }
    }
}
