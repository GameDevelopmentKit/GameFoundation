#if UGS_AUTH
namespace GameFoundation.Scripts.Auth.UGS
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using Unity.Services.Authentication;
    using Unity.Services.Core;
    using UnityEngine;

    /// <summary>
    /// Concrete <see cref="IAuthService"/> implementation wrapping Unity Gaming Services (UGS) Authentication SDK.
    ///
    /// Requires the following UGS packages:
    ///   - com.unity.services.core
    ///   - com.unity.services.authentication
    ///
    /// Enable by adding UGS_AUTH to Player Settings → Scripting Define Symbols.
    ///
    /// Lifecycle:
    ///   1. InitializeAsync() — calls UnityServices.InitializeAsync(), restores cached session
    ///   2. SignInAnonymouslyAsync() — creates or restores anonymous PlayerId
    ///   3. LinkAccountAsync() / SignInWithProviderAsync() — for account upgrade / device transfer
    /// </summary>
    public class UGSAuthService : IAuthService
    {
        private AuthState state = AuthState.Uninitialized;
        private readonly List<AuthProvider> linkedProviders = new();

        #region IAuthService — State

        public AuthState State => this.state;
        public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;
        public bool IsAnonymous => this.IsSignedIn && !this.linkedProviders.Any();
        public string PlayerId => this.IsSignedIn ? AuthenticationService.Instance.PlayerId : null;
        public IReadOnlyList<AuthProvider> LinkedProviders => this.linkedProviders;

        #endregion

        #region IAuthService — Lifecycle

        public async UniTask<AuthResult> InitializeAsync()
        {
            if (this.state == AuthState.Authenticated)
            {
                return AuthResult.Success(this.PlayerId, this.IsAnonymous, true);
            }

            this.state = AuthState.Initializing;

            try
            {
                // Initialize UGS Core (idempotent — safe to call multiple times)
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    await UnityServices.InitializeAsync();
                }

                // Subscribe to SDK events
                this.SubscribeToEvents();

                // Check if a cached session was restored
                if (AuthenticationService.Instance.IsSignedIn)
                {
                    this.state = AuthState.Authenticated;
                    this.RefreshLinkedProviders();

                    var result = AuthResult.Success(this.PlayerId, this.IsAnonymous, true);
                    Debug.Log("[UGSAuth] Session restored.");
                    this.RaiseStateChanged(result);
                    return result;
                }

                this.state = AuthState.SignedOut;
                return AuthResult.Failure("Initialized but no cached session. Call SignInAnonymouslyAsync().");
            }
            catch (Exception ex)
            {
                this.state = AuthState.Error;
                Debug.LogError($"[UGSAuth] Init failed: {ex.Message}");
                this.RaiseStateChanged(AuthResult.Failure(ex.Message));
                return AuthResult.Failure(ex.Message);
            }
        }

        public async UniTask<AuthResult> SignInAnonymouslyAsync()
        {
            if (this.IsSignedIn)
            {
                return AuthResult.Success(this.PlayerId, this.IsAnonymous, true);
            }

            // Ensure initialized first
            if (this.state == AuthState.Uninitialized)
            {
                var initResult = await this.InitializeAsync();
                if (initResult.IsSuccess)
                {
                    return initResult; // Session was restored during init
                }
            }

            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                this.state = AuthState.Authenticated;
                this.RefreshLinkedProviders();

                var result = AuthResult.Success(this.PlayerId, this.IsAnonymous);
                Debug.Log("[UGSAuth] Anonymous sign-in successful.");
                this.RaiseStateChanged(result);
                return result;
            }
            catch (AuthenticationException ex)
            {
                this.state = AuthState.Error;
                Debug.LogError($"[UGSAuth] Anonymous sign-in failed: {ex.Message}");
                var result = AuthResult.Failure(ex.Message);
                this.RaiseStateChanged(result);
                return result;
            }
            catch (RequestFailedException ex)
            {
                this.state = AuthState.Error;
                Debug.LogError($"[UGSAuth] Anonymous sign-in request failed: {ex.Message}");
                var result = AuthResult.Failure(ex.Message);
                this.RaiseStateChanged(result);
                return result;
            }
        }

        public async UniTask<AuthResult> SignInWithProviderAsync(AuthProvider provider, string token)
        {
            try
            {
                switch (provider)
                {
                    case AuthProvider.GooglePlayGames:
                        await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(token);
                        break;
                    case AuthProvider.Apple:
                        await AuthenticationService.Instance.SignInWithAppleAsync(token);
                        break;
                    case AuthProvider.UnityPlayerAccount:
                        await AuthenticationService.Instance.SignInWithUnityAsync(token);
                        break;
                    default:
                        return AuthResult.Failure($"Unsupported provider for sign-in: {provider}");
                }

                this.state = AuthState.Authenticated;
                this.RefreshLinkedProviders();

                var result = AuthResult.Success(this.PlayerId, false);
                Debug.Log($"[UGSAuth] Signed in with {provider}.");
                this.RaiseStateChanged(result);
                return result;
            }
            catch (AuthenticationException ex)
            {
                Debug.LogError($"[UGSAuth] Sign-in with {provider} failed: {ex.Message}");
                return AuthResult.Failure(ex.Message);
            }
            catch (RequestFailedException ex)
            {
                Debug.LogError($"[UGSAuth] Sign-in with {provider} request failed: {ex.Message}");
                return AuthResult.Failure(ex.Message);
            }
        }

        public async UniTask<LinkResult> LinkAccountAsync(AuthProvider provider, string token)
        {
            if (!this.IsSignedIn)
            {
                Debug.LogError("[UGSAuth] Cannot link account — not signed in.");
                return LinkResult.Failed;
            }

            try
            {
                switch (provider)
                {
                    case AuthProvider.GooglePlayGames:
                        await AuthenticationService.Instance.LinkWithGooglePlayGamesAsync(token);
                        break;
                    case AuthProvider.Apple:
                        await AuthenticationService.Instance.LinkWithAppleAsync(token);
                        break;
                    case AuthProvider.UnityPlayerAccount:
                        await AuthenticationService.Instance.LinkWithUnityAsync(token);
                        break;
                    default:
                        Debug.LogError($"[UGSAuth] Unsupported provider for linking: {provider}");
                        return LinkResult.Failed;
                }

                this.RefreshLinkedProviders();
                Debug.Log($"[UGSAuth] Account linked with {provider}.");
                this.RaiseStateChanged(AuthResult.Success(this.PlayerId, false));
                return LinkResult.Success;
            }
            catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
            {
                Debug.LogWarning($"[UGSAuth] Account already linked to another PlayerId via {provider}.");
                return LinkResult.AlreadyLinked;
            }
            catch (AuthenticationException ex)
            {
                Debug.LogError($"[UGSAuth] Link with {provider} failed: {ex.Message}");
                return LinkResult.Failed;
            }
            catch (RequestFailedException ex)
            {
                Debug.LogError($"[UGSAuth] Link with {provider} request failed: {ex.Message}");
                return LinkResult.Failed;
            }
        }

        public async UniTask UnlinkAccountAsync(AuthProvider provider)
        {
            if (!this.IsSignedIn)
            {
                Debug.LogError("[UGSAuth] Cannot unlink — not signed in.");
                return;
            }

            try
            {
                switch (provider)
                {
                    case AuthProvider.GooglePlayGames:
                        await AuthenticationService.Instance.UnlinkGooglePlayGamesAsync();
                        break;
                    case AuthProvider.Apple:
                        await AuthenticationService.Instance.UnlinkAppleAsync();
                        break;
                    case AuthProvider.UnityPlayerAccount:
                        await AuthenticationService.Instance.UnlinkUnityAsync();
                        break;
                }

                this.RefreshLinkedProviders();
                Debug.Log($"[UGSAuth] Unlinked {provider}.");
                this.RaiseStateChanged(AuthResult.Success(this.PlayerId, this.IsAnonymous));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UGSAuth] Unlink {provider} failed: {ex.Message}");
            }
        }

        public UniTask SignOutAsync()
        {
            if (!this.IsSignedIn)
            {
                return UniTask.CompletedTask;
            }

            AuthenticationService.Instance.SignOut(true);
            this.state = AuthState.SignedOut;
            this.linkedProviders.Clear();

            Debug.Log("[UGSAuth] Signed out.");
            this.OnAuthStateChanged?.Invoke(new AuthSessionInfo { State = AuthState.SignedOut });
            return UniTask.CompletedTask;
        }

        #endregion

        #region IAuthService — Events

        public event Action<AuthSessionInfo> OnAuthStateChanged;

        #endregion

        #region Internal Helpers

        private void SubscribeToEvents()
        {
            AuthenticationService.Instance.SignedIn += () => { Debug.Log("[UGSAuth] SDK Event: SignedIn."); };

            AuthenticationService.Instance.SignInFailed += (err) =>
            {
                Debug.LogError($"[UGSAuth] SDK Event: SignInFailed. Error: {err.Message}");
            };

            AuthenticationService.Instance.SignedOut += () =>
            {
                Debug.Log("[UGSAuth] SDK Event: SignedOut.");
                this.state = AuthState.SignedOut;
            };

            AuthenticationService.Instance.Expired += () =>
            {
                Debug.LogWarning("[UGSAuth] SDK Event: Session expired. Will attempt re-auth on next operation.");
            };
        }

        /// <summary>
        /// Refresh the local linked providers list from the UGS PlayerInfo.
        /// </summary>
        private void RefreshLinkedProviders()
        {
            this.linkedProviders.Clear();

            var playerInfo = AuthenticationService.Instance.PlayerInfo;
            if (playerInfo?.Identities == null) return;

            foreach (var identity in playerInfo.Identities)
            {
                switch (identity.TypeId)
                {
                    case "google-play-games":
                        this.linkedProviders.Add(AuthProvider.GooglePlayGames);
                        break;
                    case "apple":
                        this.linkedProviders.Add(AuthProvider.Apple);
                        break;
                    case "unity":
                        this.linkedProviders.Add(AuthProvider.UnityPlayerAccount);
                        break;
                }
            }
        }

        private void RaiseStateChanged(AuthResult result)
        {
            this.OnAuthStateChanged?.Invoke(AuthSessionInfo.FromResult(result, this.linkedProviders));
        }

        #endregion
    }
}
#endif
