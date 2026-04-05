namespace GameFoundation.Scripts.Auth
{
    using GameConfigs;
    using Zenject;

    /// <summary>
    /// Zenject installer for the Auth module.
    /// Resolves <see cref="AuthConfig"/> from <see cref="GDKConfig"/> and binds the appropriate
    /// <see cref="IAuthService"/> implementation:
    ///   - Editor + UseMockInEditor → <see cref="EditorAuthService"/>
    ///   - Runtime (or Editor with mock disabled) → <see cref="UGSAuthService"/> (requires UGS_AUTH define)
    ///
    /// Also declares the <see cref="AuthStateChangedSignal"/> and bridges the C# event
    /// from IAuthService to the Zenject SignalBus so that any subscriber can listen via either.
    ///
    /// Install in GameProjectInstaller:
    ///   AuthInstaller.Install(this.Container);
    /// </summary>
    public class AuthInstaller : Installer<AuthInstaller>
    {
        public override void InstallBindings()
        {
            // Declare auth signals
            this.Container.DeclareSignal<AuthStateChangedSignal>();

            // Bind AuthConfig from GDKConfig (optional — may not be added yet)
            this.Container.Bind<AuthConfig>()
                .FromMethod(ctx =>
                {
                    var gdkConfig = ctx.Container.TryResolve<GDKConfig>();
                    if (gdkConfig != null && gdkConfig.HasGameConfig<AuthConfig>())
                    {
                        return gdkConfig.GetGameConfig<AuthConfig>();
                    }

                    // No AuthConfig found — use defaults
                    return null;
                })
                .AsCached();

            // Bind IAuthService — pick implementation based on platform and config
            this.Container.Bind<IAuthService>()
                .FromMethod(ctx =>
                {
                    var config = ctx.Container.TryResolve<AuthConfig>();
                    var useMockInEditor = config == null || config.UseMockInEditor;

                    IAuthService service;

#if UNITY_EDITOR
                    if (useMockInEditor)
                    {
                        service = new EditorAuthService();
                    }
                    else
#endif
                    {
#if UGS_AUTH
                        service = new GameFoundation.Scripts.Auth.UGS.UGSAuthService();
#else
                        // Fallback: if UGS is not installed, use Editor mock
                        service = new EditorAuthService();
#endif
                    }

                    // Bridge IAuthService.OnAuthStateChanged (C# event) → Zenject SignalBus.
                    // This allows game-layer code to subscribe via either mechanism.
                    var signalBus = ctx.Container.TryResolve<SignalBus>();
                    if (signalBus != null)
                    {
                        service.OnAuthStateChanged += info =>
                        {
                            signalBus.Fire(new AuthStateChangedSignal(info));
                        };
                    }

                    return service;
                })
                .AsSingle();
        }
    }
}
