namespace GameFoundation.Scripts.Auth
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    /// <summary>
    /// Mock auth service for Unity Editor and testing.
    /// Returns a deterministic PlayerId based on SystemInfo.deviceUniqueIdentifier
    /// so local file paths remain consistent across Editor sessions.
    ///
    /// Use this when:
    /// - Developing offline without UGS dashboard setup
    /// - Running automated tests
    /// - AuthConfig.UseMockInEditor is true
    /// </summary>
    public class EditorAuthService : IAuthService
    {
        private const string EditorPlayerIdPrefix = "editor_";

        private AuthState state = AuthState.Uninitialized;
        private string playerId;
        private bool isAnonymous = true;
        private readonly List<AuthProvider> linkedProviders = new();

        #region IAuthService — State

        public AuthState State => this.state;
        public bool IsSignedIn => this.state == AuthState.Authenticated;
        public bool IsAnonymous => this.isAnonymous;
        public string PlayerId => this.playerId;
        public IReadOnlyList<AuthProvider> LinkedProviders => this.linkedProviders;

        #endregion

        #region IAuthService — Lifecycle

        public UniTask<AuthResult> InitializeAsync()
        {
            if (this.state != AuthState.Uninitialized)
            {
                Debug.Log("[EditorAuth] Already initialized.");
                return UniTask.FromResult(AuthResult.Success(this.playerId, this.isAnonymous, true));
            }

            this.state = AuthState.Initializing;
            Debug.Log("[EditorAuth] Initializing mock auth service...");

            // Generate deterministic PlayerId from device ID
            // This ensures the same Editor machine always gets the same PlayerId,
            // so local file paths (SaveData/{PlayerId}/) remain stable.
            var deviceHash = SystemInfo.deviceUniqueIdentifier.GetHashCode();
            this.playerId = $"{EditorPlayerIdPrefix}{Mathf.Abs(deviceHash):X8}";

            this.state = AuthState.Authenticated;
            this.isAnonymous = true;

            var result = AuthResult.Success(this.playerId, true, false);
            Debug.Log($"[EditorAuth] Mock auth complete. PlayerId: {this.playerId}");

            this.RaiseStateChanged(result);
            return UniTask.FromResult(result);
        }

        public UniTask<AuthResult> SignInAnonymouslyAsync()
        {
            if (this.state == AuthState.Authenticated)
            {
                return UniTask.FromResult(AuthResult.Success(this.playerId, this.isAnonymous, true));
            }

            // In Editor, InitializeAsync already signs in. But support explicit calls too.
            return this.InitializeAsync();
        }

        public UniTask<AuthResult> SignInWithProviderAsync(AuthProvider provider, string token)
        {
            Debug.Log($"[EditorAuth] Mock sign-in with provider: {provider} (token ignored in Editor)");

            // Simulate switching to a "linked" account — keeps same PlayerId in Editor
            this.isAnonymous = false;
            if (!this.linkedProviders.Contains(provider))
            {
                this.linkedProviders.Add(provider);
            }

            var result = AuthResult.Success(this.playerId, false, false);
            this.RaiseStateChanged(result);
            return UniTask.FromResult(result);
        }

        public UniTask<LinkResult> LinkAccountAsync(AuthProvider provider, string token)
        {
            Debug.Log($"[EditorAuth] Mock link account: {provider}");

            if (this.linkedProviders.Contains(provider))
            {
                return UniTask.FromResult(LinkResult.AlreadyLinked);
            }

            this.linkedProviders.Add(provider);
            this.isAnonymous = false;

            this.RaiseStateChanged(AuthResult.Success(this.playerId, false));
            return UniTask.FromResult(LinkResult.Success);
        }

        public UniTask UnlinkAccountAsync(AuthProvider provider)
        {
            Debug.Log($"[EditorAuth] Mock unlink account: {provider}");
            this.linkedProviders.Remove(provider);

            if (this.linkedProviders.Count == 0)
            {
                this.isAnonymous = true;
            }

            this.RaiseStateChanged(AuthResult.Success(this.playerId, this.isAnonymous));
            return UniTask.CompletedTask;
        }

        public UniTask SignOutAsync()
        {
            Debug.Log("[EditorAuth] Mock sign out.");
            this.state = AuthState.SignedOut;
            this.playerId = null;
            this.isAnonymous = true;
            this.linkedProviders.Clear();

            this.OnAuthStateChanged?.Invoke(new AuthSessionInfo
            {
                State = AuthState.SignedOut
            });
            return UniTask.CompletedTask;
        }

        #endregion

        #region IAuthService — Events

        public event Action<AuthSessionInfo> OnAuthStateChanged;

        private void RaiseStateChanged(AuthResult result)
        {
            this.OnAuthStateChanged?.Invoke(AuthSessionInfo.FromResult(result, this.linkedProviders));
        }

        #endregion
    }
}
