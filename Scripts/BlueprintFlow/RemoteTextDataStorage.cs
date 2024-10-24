#nullable enable
namespace TheOne.Data.Storage
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Security.Cryptography;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Newtonsoft.Json;
    using TheOne.Extensions;
    using TheOne.Logging;
    using TheOne.ResourceManagement;
    using UnityEngine;
    using UnityEngine.Scripting;
    using ILogger = TheOne.Logging.ILogger;

    public readonly struct VersionInfo
    {
        public string Hash { get; }

        [Preserve]
        public VersionInfo(string hash)
        {
            this.Hash = hash;
        }

        public override string ToString()
        {
            return $"{nameof(VersionInfo)} {this.Hash}";
        }
    }

    public interface IRemoteDataStorageConfig
    {
        public string VersionInfoUrl { get; }

        public string GetDownloadUrl(VersionInfo versionInfo);
    }

    public sealed class RemoteTextDataStorage : AssetTextDataStorage
    {
        private const string VERSION_INFO_KEY = nameof(RemoteTextDataStorage) + "/" + nameof(VersionInfo);

        private static readonly string PersistentDataPath = Application.persistentDataPath;
        private static readonly string TemporaryCachePath = Application.temporaryCachePath;

        private readonly IRemoteDataStorageConfig config;
        private readonly IExternalAssetsManager   externalAssetsManager;
        private readonly ILogger                  logger;

        [Preserve]
        public RemoteTextDataStorage(IAssetsManager assetsManager, IRemoteDataStorageConfig config, IExternalAssetsManager externalAssetsManager, ILoggerManager loggerManager) : base(assetsManager)
        {
            this.config                = config;
            this.externalAssetsManager = externalAssetsManager;
            this.logger                = loggerManager.GetLogger(this);
        }

        protected override string? Read(string key)
        {
            this.logger.Warning("`Read` only use cached data. Use `ReadAsync` to fetch new version from remote.");
            if (!this.ValidateAndExtract() || !File.Exists(this.GetFilePath(key)))
            {
                this.logger.Warning($"Failed to read {key} from remote storage. Fallback to local storage.");
                return base.Read(key);
            }
            return File.ReadAllText(this.GetFilePath(key)).NullIfEmpty();
        }

        protected override async UniTask<string?> ReadAsync(string key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            await this.FetchInfoAndDownloadAsync(cancellationToken);
            if (!await this.ValidateAndExtractAsync(cancellationToken) || !File.Exists(this.GetFilePath(key)))
            {
                this.logger.Warning($"Failed to read {key} from remote storage. Fallback to local storage.");
                return await base.ReadAsync(key, progress, cancellationToken);
            }
            return (await File.ReadAllTextAsync(this.GetFilePath(key), cancellationToken)).NullIfEmpty();
        }

        private VersionInfo versionInfo = JsonConvert.DeserializeObject<VersionInfo>(PlayerPrefs.GetString(VERSION_INFO_KEY, "{}"));

        private bool fetching;
        private bool fetched;
        private bool validating;
        private bool validated;

        private async UniTask FetchInfoAndDownloadAsync(CancellationToken cancellationToken)
        {
            await UniTask.WaitUntil(this, state => !state.fetching, cancellationToken: cancellationToken);
            if (this.fetched) return;
            var cancelled = false;
            this.fetching = true;
            try
            {
                var versionInfoStr = await this.externalAssetsManager.DownloadTextAsync(
                    url: this.config.VersionInfoUrl,
                    cache: false,
                    cancellationToken: cancellationToken
                );
                this.versionInfo = JsonConvert.DeserializeObject<VersionInfo>(versionInfoStr);
                PlayerPrefs.SetString(VERSION_INFO_KEY, versionInfoStr);
                PlayerPrefs.Save();
                this.logger.Debug($"Got {this.versionInfo}");

                if (await this.ValidateAndExtractAsync(cancellationToken))
                {
                    this.logger.Debug("Skipping download");
                    return;
                }

                await this.externalAssetsManager.DownloadFileAsync(
                    url: this.config.GetDownloadUrl(this.versionInfo),
                    savePath: this.GetZipFilePath(),
                    cache: false,
                    cancellationToken: cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
                throw;
            }
            catch (Exception e)
            {
                this.logger.Exception(e);
                this.logger.Error("Failed to fetch info or download. Using cached data");
            }
            finally
            {
                this.fetching = false;
                this.fetched  = !cancelled;
            }
        }

        private bool ValidateAndExtract()
        {
            if (this.validated) return true;
            this.logger.Debug($"Validating {this.versionInfo}");
            var zipFilePath = this.GetZipFilePath();
            if (!File.Exists(zipFilePath))
            {
                this.logger.Error($"Zip file not found: {zipFilePath}");
                PlayerPrefs.DeleteKey(VERSION_INFO_KEY);
                PlayerPrefs.Save();
                return false;
            }
            var hash = this.ComputeHash();
            if (hash != this.versionInfo.Hash)
            {
                this.logger.Error($"Hash mismatch. Expected: {this.versionInfo.Hash}, Got: {hash}");
                File.Delete(zipFilePath);
                return false;
            }
            this.ExtractZipFile();
            this.logger.Debug("Validated");
            return this.validated = true;
        }

        private async UniTask<bool> ValidateAndExtractAsync(CancellationToken cancellationToken)
        {
            #if UNITY_WEBGL
            return this.ValidateAndExtract();
            #endif
            await UniTask.WaitUntil(this, state => !state.validating, cancellationToken: cancellationToken);
            if (this.validated) return true;
            this.validating = true;
            try
            {
                this.logger.Debug($"Validating {this.versionInfo}");
                var zipFilePath = this.GetZipFilePath();
                if (!File.Exists(zipFilePath))
                {
                    this.logger.Error($"Zip file not found: {zipFilePath}");
                    PlayerPrefs.DeleteKey(VERSION_INFO_KEY);
                    PlayerPrefs.Save();
                    return false;
                }
                var hash = await UniTask.RunOnThreadPool(this.ComputeHash, cancellationToken: cancellationToken);
                if (hash != this.versionInfo.Hash)
                {
                    this.logger.Error($"Hash mismatch. Expected: {this.versionInfo.Hash}, Got: {hash}");
                    File.Delete(zipFilePath);
                    return false;
                }
                await UniTask.RunOnThreadPool(this.ExtractZipFile, cancellationToken: cancellationToken);
                this.logger.Debug("Validated");
                return this.validated = true;
            }
            finally
            {
                this.validating = false;
            }
        }

        private string ComputeHash()
        {
            using var sha256  = SHA256.Create();
            using var zipFile = File.OpenRead(this.GetZipFilePath());
            return BitConverter.ToString(sha256.ComputeHash(zipFile)).Replace("-", "");
        }

        private void ExtractZipFile()
        {
            var zipFilePath      = this.GetZipFilePath();
            var extractDirectory = this.GetExtractDirectory();
            this.logger.Debug($"Extracting {zipFilePath} to {extractDirectory}");
            ZipFile.ExtractToDirectory(zipFilePath, extractDirectory, true);
            this.logger.Debug("Extracted");
        }

        private string GetZipFilePath() => PersistentDataPath + "/" + this.versionInfo.Hash;

        private string GetExtractDirectory() => TemporaryCachePath + "/" + this.versionInfo.Hash;

        private string GetFilePath(string key) => this.GetExtractDirectory() + "/" + key;
    }
}