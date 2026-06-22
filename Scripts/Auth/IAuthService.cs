namespace GameFoundation.Scripts.Auth
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;

    /// <summary>
    /// Abstraction for authentication services.
    /// Implementations wrap platform-specific SDKs (UGS, Firebase, PlayFab, etc.)
    /// while providing a unified API for the game layer.
    ///
    /// Lifecycle:
    ///   1. InitializeAsync() — initialize SDK, restore cached session if available
    ///   2. SignInAnonymouslyAsync() — frictionless guest login
    ///   3. (Optional) LinkAccountAsync() — upgrade anonymous account to persistent
    ///   4. (Optional) SignInWithProviderAsync() — sign in with linked provider on new device
    ///   5. SignOutAsync() — end session
    ///
    /// Thread safety: All methods must be called from the Unity main thread.
    /// </summary>
    public interface IAuthService
    {
        #region State

        /// <summary>
        /// Current authentication state.
        /// </summary>
        AuthState State { get; }

        /// <summary>
        /// Whether the player is currently authenticated (anonymous or linked).
        /// </summary>
        bool IsSignedIn { get; }

        /// <summary>
        /// Whether the current session is an anonymous (guest) account.
        /// Returns false if not signed in.
        /// </summary>
        bool IsAnonymous { get; }

        /// <summary>
        /// The globally unique player identifier assigned by the auth backend.
        /// Null if not signed in.
        /// </summary>
        string PlayerId { get; }

        /// <summary>
        /// List of providers linked to the current account.
        /// Empty if anonymous-only.
        /// </summary>
        IReadOnlyList<AuthProvider> LinkedProviders { get; }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Initialize the authentication SDK.
        /// Must be called before any other method.
        /// If a cached session exists, it will be restored automatically.
        /// </summary>
        /// <returns>AuthResult with session info if a cached session was restored, or a failure result.</returns>
        UniTask<AuthResult> InitializeAsync();

        /// <summary>
        /// Sign in anonymously (guest account).
        /// Creates a new anonymous identity if none exists, or restores the cached one.
        /// The resulting PlayerId is globally unique and device-bound until linked.
        /// </summary>
        UniTask<AuthResult> SignInAnonymouslyAsync();

        /// <summary>
        /// Sign in with a previously linked provider (used on new device for account recovery).
        /// This replaces the current anonymous session with the linked account.
        /// </summary>
        /// <param name="provider">The auth provider to sign in with.</param>
        /// <param name="token">Provider-specific auth token or code.</param>
        UniTask<AuthResult> SignInWithProviderAsync(AuthProvider provider, string token);

        /// <summary>
        /// Sign in with a provider credential. Use this for providers that need multiple fields,
        /// such as Apple Game Center.
        /// </summary>
        /// <param name="credential">Provider-specific credential payload.</param>
        UniTask<AuthResult> SignInWithProviderAsync(AuthCredential credential);

        /// <summary>
        /// Link the current anonymous account to a platform provider.
        /// After linking, the account is recoverable across devices.
        /// </summary>
        /// <param name="provider">The auth provider to link.</param>
        /// <param name="token">Provider-specific auth token or code.</param>
        /// <returns>
        /// Success — account linked.
        /// AlreadyLinked — that provider account belongs to a different PlayerId.
        /// Failed — network or other error.
        /// </returns>
        UniTask<LinkResult> LinkAccountAsync(AuthProvider provider, string token);

        /// <summary>
        /// Link the current account to a provider credential. Use this for providers that need
        /// multiple fields, such as Apple Game Center.
        /// </summary>
        /// <param name="credential">Provider-specific credential payload.</param>
        UniTask<LinkResult> LinkAccountAsync(AuthCredential credential);

        /// <summary>
        /// Unlink a provider from the current account.
        /// The account reverts to anonymous if no providers remain.
        /// </summary>
        /// <param name="provider">The auth provider to unlink.</param>
        UniTask UnlinkAccountAsync(AuthProvider provider);

        /// <summary>
        /// Sign out and clear the local session.
        /// The player will need to sign in again on next launch.
        /// </summary>
        UniTask SignOutAsync();

        #endregion

        #region Events

        /// <summary>
        /// Fired whenever the auth state changes (sign in, sign out, link, error).
        /// Subscribe to this for UI updates and system reactions.
        /// </summary>
        event Action<AuthSessionInfo> OnAuthStateChanged;

        #endregion
    }
}
