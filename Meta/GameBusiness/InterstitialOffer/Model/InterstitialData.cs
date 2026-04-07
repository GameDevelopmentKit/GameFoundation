namespace GameBusiness.InterstitialOffer.Model
{
    using System.Collections.Generic;
    using DataManager.UserData;
    using GameBusiness.InterstitialOffer.Blueprint;
    using Newtonsoft.Json;
    /// <summary>
    /// User data for interstitial offers. Persisted to save file.
    /// Keyed by offer ID from the InterstitialBlueprint.
    /// </summary>
    public class InterstitialData : IUserData
    {
        public Dictionary<string, InterstitialOffer> InterstitialOffers = new Dictionary<string, InterstitialOffer>();
    }

    /// <summary>
    /// Runtime state of a single interstitial offer.
    /// Tracks how many times it was purchased and its lifecycle state.
    /// StaticData is linked at runtime from the blueprint (not serialized).
    /// </summary>
    public class InterstitialOffer
    {
        public              InterstitialOfferState State;
        public              int                    PurchasedAmount;
        [JsonIgnore] public InterstitialRecord     StaticData;
    }

    /// <summary>
    /// Lifecycle states for an interstitial offer.
    /// InActive -> Active (via Verified) -> Expired (all packages expired) or Purchased (all bought).
    /// </summary>
    public enum InterstitialOfferState
    {
        InActive,
        Active,
        Expired,
        Purchased
    }
}
