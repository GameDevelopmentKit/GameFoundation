namespace GameFoundation.Scripts.Auth
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the current authentication state of the player.
    /// </summary>
    public enum AuthState
    {
        /// <summary>
        /// SDK not yet initialized. Call InitializeAsync() first.
        /// </summary>
        Uninitialized,

        /// <summary>
        /// SDK initialization is in progress.
        /// </summary>
        Initializing,

        /// <summary>
        /// SDK initialized but no active session. Need to sign in.
        /// </summary>
        SignedOut,

        /// <summary>
        /// Player is authenticated (anonymous or linked).
        /// </summary>
        Authenticated,

        /// <summary>
        /// Authentication failed due to an error.
        /// </summary>
        Error
    }

    /// <summary>
    /// Result of a sign-in operation.
    /// </summary>
    public class AuthResult
    {
        /// <summary>
        /// Whether the sign-in was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// The player's unique identifier (globally unique, assigned by the auth backend).
        /// This is NOT a display name — it's an opaque ID like "abc123xyz789".
        /// </summary>
        public string PlayerId { get; set; }

        /// <summary>
        /// Whether this is an anonymous (guest) account.
        /// </summary>
        public bool IsAnonymous { get; set; }

        /// <summary>
        /// Error message if sign-in failed. Null on success.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Whether the cached session was restored (returning player, same device).
        /// </summary>
        public bool IsSessionRestored { get; set; }

        public static AuthResult Success(string playerId, bool isAnonymous, bool isSessionRestored = false)
        {
            return new AuthResult
            {
                IsSuccess = true,
                PlayerId = playerId,
                IsAnonymous = isAnonymous,
                IsSessionRestored = isSessionRestored
            };
        }

        public static AuthResult Failure(string errorMessage)
        {
            return new AuthResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Result of an account linking operation.
    /// </summary>
    public enum LinkResult
    {
        /// <summary>
        /// Account successfully linked to the provider.
        /// </summary>
        Success,

        /// <summary>
        /// The provider account is already linked to a different PlayerId.
        /// Player should be asked: "Switch to that account?"
        /// </summary>
        AlreadyLinked,

        /// <summary>
        /// Linking failed due to a network or provider error.
        /// </summary>
        Failed
    }

    /// <summary>
    /// Supported authentication providers for account linking.
    /// </summary>
    public enum AuthProvider
    {
        Anonymous,
        GooglePlayGames,
        Apple,
        UnityPlayerAccount,
    }

    /// <summary>
    /// Information about the current auth session, broadcast via signals.
    /// </summary>
    public class AuthSessionInfo
    {
        public AuthState State { get; set; }
        public string PlayerId { get; set; }
        public bool IsAnonymous { get; set; }
        public IReadOnlyList<AuthProvider> LinkedProviders { get; set; }

        /// <summary>
        /// Error details when State == AuthState.Error.
        /// </summary>
        public string ErrorMessage { get; set; }

        public static AuthSessionInfo FromResult(AuthResult result, IReadOnlyList<AuthProvider> linkedProviders = null)
        {
            return new AuthSessionInfo
            {
                State = result.IsSuccess ? AuthState.Authenticated : AuthState.Error,
                PlayerId = result.PlayerId,
                IsAnonymous = result.IsAnonymous,
                LinkedProviders = linkedProviders ?? Array.Empty<AuthProvider>(),
                ErrorMessage = result.ErrorMessage
            };
        }
    }
}
