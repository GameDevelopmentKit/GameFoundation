namespace GameBusiness.InterstitialOffer.Manager
{
    using System.Collections.Generic;
    using DataManager.UserData;
    using GameBusiness.InterstitialOffer.Blueprint;
    using GameBusiness.InterstitialOffer.Model;
    using Zenject;

    /// <summary>
    /// Abstract base for interstitial offer management.
    /// Subclass in each game to provide game-specific UI, logging, and signal handling.
    /// </summary>
    public abstract class BaseInterstitialManager<TData> : BaseDataManager<TData>
        where TData : InterstitialData, new()
    {
        protected readonly InterstitialOfferBlueprint InterstitialOfferBlueprint;

        protected BaseInterstitialManager(InterstitialOfferBlueprint interstitialOfferBlueprint, SignalBus signalBus) : base(signalBus)
        {
            this.InterstitialOfferBlueprint = interstitialOfferBlueprint;
        }

        public override void OnDataInitialized()
        {
            base.OnDataInitialized();
            foreach (var interstitial in InterstitialOfferBlueprint.Values)
            {
                if (!this.Data.InterstitialOffers.TryGetValue(interstitial.Id, out var interstitialOffer))
                {
                    this.Data.InterstitialOffers.Add(interstitial.Id, new InterstitialOffer()
                    {
                        StaticData = interstitial,
                        State = InterstitialOfferState.InActive
                    });
                }
                else
                {
                    interstitialOffer.StaticData = interstitial;
                }
            }
        }

        public void MarkPurchasedOffer(string offerId)
        {
            if (this.Data.InterstitialOffers.TryGetValue(offerId, out var interstitialOffer))
            {
                interstitialOffer.PurchasedAmount++;
                if (interstitialOffer.PurchasedAmount == interstitialOffer.StaticData.ExchangePackageName.Count)
                    interstitialOffer.State = InterstitialOfferState.Purchased;
            }
        }

        public void RefreshInterstitialOffers()
        {
            foreach (var interstitialOffer in this.Data.InterstitialOffers)
            {
                var offer = interstitialOffer.Value;
                if (offer.State == InterstitialOfferState.Active)
                {
                    if (this.AreAllPackagesExpired(offer))
                    {
                        offer.State = InterstitialOfferState.Expired;
                    }
                }
            }
        }

        public List<InterstitialOffer> ActiveInterstitialOffers()
        {
            var activeInterstitialOffer = new List<InterstitialOffer>();
            foreach (var interstitialOffer in this.Data.InterstitialOffers)
            {
                var offer = interstitialOffer.Value;
                if (offer.State == InterstitialOfferState.Active && this.IsAnyPackageAvailable(offer))
                {
                    activeInterstitialOffer.Add(offer);
                }
            }
            return activeInterstitialOffer;
        }

        /// <summary>
        /// Called to verify/activate an offer and show it if packages are available.
        /// </summary>
        public virtual void Verified(string offerId)
        {
            if (this.Data.InterstitialOffers.TryGetValue(offerId, out var interstitialOffer))
            {
                interstitialOffer.State = InterstitialOfferState.Active;
                OnOfferActivated(interstitialOffer);
            }
            RefreshInterstitialOffers();
        }

        /// <summary>
        /// Override in game-specific subclass to unlock packages and show offer UI.
        /// </summary>
        protected abstract void OnOfferActivated(InterstitialOffer offer);

        /// <summary>
        /// Override to query package availability from your game's ShopManager.
        /// </summary>
        protected abstract bool IsPackageAvailable(string packageId);

        /// <summary>
        /// Override to query package expiry from your game's ShopManager.
        /// </summary>
        protected abstract bool IsPackageExpiredOrUnavailable(string packageId);

        /// <summary>
        /// Override to provide game-specific logging for warnings/errors.
        /// Defaults to Debug.LogWarning.
        /// </summary>
        protected virtual void LogWarning(string message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        private bool AreAllPackagesExpired(InterstitialOffer offer)
        {
            if (offer.StaticData.ExchangePackageName == null || offer.StaticData.ExchangePackageName.Count == 0) return true;
            foreach (var packageId in offer.StaticData.ExchangePackageName)
            {
                if (!IsPackageExpiredOrUnavailable(packageId))
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsAnyPackageAvailable(InterstitialOffer offer)
        {
            if (offer.StaticData.ExchangePackageName == null) return false;
            foreach (var packageId in offer.StaticData.ExchangePackageName)
            {
                if (IsPackageAvailable(packageId))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
