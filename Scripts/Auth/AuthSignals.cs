namespace GameFoundation.Scripts.Auth
{
    /// <summary>
    /// Zenject signal fired when the authentication state changes.
    /// Subscribe in presenters/managers that need to react to auth events.
    ///
    /// Usage:
    ///   signalBus.Subscribe&lt;AuthStateChangedSignal&gt;(OnAuthChanged);
    ///
    /// The signal carries an <see cref="AuthSessionInfo"/> with the current state,
    /// PlayerId, and linked providers.
    /// </summary>
    public class AuthStateChangedSignal
    {
        public AuthSessionInfo SessionInfo { get; }

        public AuthStateChangedSignal(AuthSessionInfo sessionInfo)
        {
            this.SessionInfo = sessionInfo;
        }
    }
}
