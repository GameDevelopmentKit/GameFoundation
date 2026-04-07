namespace GameBusiness.Wallet.Manager
{
    using System.Collections.Generic;
    using GameBusiness.Wallet.Blueprint;
    using GameBusiness.Wallet.Model;

    /// <summary>
    ///     Manages the player currency balances.
    /// </summary>
    public interface IWalletManager
    {
        /// <summary>
        ///     Gets the balance of the specified <see cref="currencyId"/>.
        /// </summary>
        Currency Get(string currencyId);
        public bool HasCurrency(string currencyId);
        
        ResourceRecord GetStaticData(string currencyId);
        
        /// <summary>
        ///   Gets all the balances.
        /// </summary>
        /// <returns></returns>
        IReadOnlyCollection<Currency> GetAllBalances();

        /// <summary>
        ///   Checks if the player can pay the specified currency value.
        /// </summary>
        /// <param name="currencyId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool CanPay(string currencyId, int value);
        
        /// <summary>
        ///     Increases the balance of the specified currency value.
        /// </summary>
        /// <param name="currencyId"> The currency you want to increase the balance. </param>
        /// <param name="value">The amount to add to the balance. </param>
        void Add(string currencyId, int value);

        /// <summary>
        ///     Decreases the balance of the specified currency value.
        /// </summary>
        /// <param name="currencyId"> The currency you want to decrease the balance.</param>
        /// <param name="value"> The amount to remove to the balance. </param>
        /// <returns><c>true</c> if the update is valid, <c>false</c> otherwise.</returns>
        bool Pay(string currencyId, int value);
        
        /// <summary>
        ///  Decreases the balance of the specified currency value until reaching 0.
        /// </summary>
        /// <param name="currencyId"></param>
        /// <param name="value"></param>
        /// <returns>Remain Value</returns>
        int TryPay(string currencyId, int value);
    }
}