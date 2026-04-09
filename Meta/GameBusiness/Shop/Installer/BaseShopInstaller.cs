namespace GameBusiness.Shop.Installer
{
    using DataManager.UserData;
    using GameBusiness.Shop.Manager;
    using GameBusiness.Shop.Model;
    using Zenject;

    /// <summary>
    /// Abstract base installer for the Meta.Shop module.
    /// Binds TManager as singleton, wires ShopManager&lt;TData&gt; and IShopService
    /// resolution to TManager, and declares the standard purchase-success signal.
    /// </summary>
    /// <typeparam name="TInstaller">The concrete installer type (CRTP for Installer&lt;T&gt;).</typeparam>
    /// <typeparam name="TData">Shop data type implementing <see cref="IShopData"/> and <see cref="IUserData"/>.</typeparam>
    /// <typeparam name="TManager">Concrete shop manager type deriving from <see cref="ShopManager{TData}"/>.</typeparam>
    /// <example>
    /// <code>
    /// // In your game project:
    /// public class MyShopInstaller : BaseShopInstaller&lt;MyShopInstaller, MyShopData, MyShopManager&gt;
    /// {
    ///     protected override void InstallGameBindings()
    ///     {
    ///         this.Container.BindInterfacesAndSelfTo&lt;MyShopServiceHandler&gt;().AsSingle();
    ///     }
    /// }
    /// </code>
    /// </example>
    public class BaseShopInstaller<TData, TManager> : Installer<BaseShopInstaller<TData, TManager> >
        where TData      : class, IShopData, IUserData, new()
        where TManager   : ShopManager<TData>
    {
        /// <inheritdoc />
        public override void InstallBindings()
        {
            this.Container.BindInterfacesAndSelfTo<TManager>().AsSingle().NonLazy();

            // Declare the standard purchase-success signal.
            this.Container.DeclareSignal<ShopPurchasePackageSuccessSignal>();

            // Game-specific bindings (handlers, extra services, etc.).
            this.InstallGameBindings();
        }

        /// <summary>
        /// Override to add game-specific bindings (e.g. ShopServiceHandler, ICostTextGenerator).
        /// Called after all base bindings are registered.
        /// </summary>
        protected virtual void InstallGameBindings() { }
    }
}
