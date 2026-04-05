namespace GameFoundation.Scripts.Auth
{
    using GameConfigs;
    using UnityEngine;

    /// <summary>
    /// Configuration for the Auth module.
    /// Add this to the GDKConfig's game configs list in the Inspector.
    ///
    /// Controls which auth features are enabled and how the module behaves.
    /// This ScriptableObject is game-agnostic and can be reused across projects.
    /// </summary>
    [CreateAssetMenu(fileName = "AuthConfig", menuName = "GDK/Auth Config")]
    public class AuthConfig : ScriptableObject, IGameConfig
    {
        [Header("Authentication")]
        [Tooltip("Whether to automatically sign in anonymously on launch. " +
                 "If false, the game must call SignInAnonymouslyAsync() explicitly.")]
        [SerializeField] private bool autoSignInAnonymous = true;

        [Tooltip("Whether to use the mock EditorAuthService in the Unity Editor. " +
                 "Useful for offline development without UGS dashboard setup.")]
        [SerializeField] private bool useMockInEditor = true;

        [Header("Profile Migration")]
        [Tooltip("Whether to migrate the 'default' profile to the PlayerId on first auth. " +
                 "Disable if profiles are already using PlayerId.")]
        [SerializeField] private bool migrateDefaultProfile = true;



        #region Properties

        /// <summary>
        /// Whether to auto sign-in anonymously during initialization.
        /// </summary>
        public bool AutoSignInAnonymous => this.autoSignInAnonymous;

        /// <summary>
        /// Whether to use mock auth in the Unity Editor.
        /// </summary>
        public bool UseMockInEditor => this.useMockInEditor;

        /// <summary>
        /// Whether to migrate the "default" profile folder to PlayerId on first auth.
        /// </summary>
        public bool MigrateDefaultProfile => this.migrateDefaultProfile;



        #endregion
    }
}
