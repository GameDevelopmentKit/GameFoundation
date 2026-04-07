namespace GameBusiness.InterstitialOffer.Manager
{
    using GameBusiness.InterstitialOffer.Model;

    /// <summary>
    /// Zenject signal fired when an interstitial offer is shown or interacted with.
    /// Used for analytics tracking across all games.
    /// ActionType constants: AutoShow (triggered automatically), ClickOpen (user opened),
    /// ClickNext (user navigated to next offer page).
    /// </summary>
    public class ShowInterstitialOfferSignal
    {
        public InterstitialOffer Offer { get; }
        public string ActionType { get; }
        public string PackageId { get; }

        public const string AutoShow = "auto_show";
        public const string ClickOpen = "click_open";
        public const string ClickNext = "click_next";

        public ShowInterstitialOfferSignal(InterstitialOffer offer, string packageId, string actionType)
        {
            Offer = offer;
            ActionType = actionType;
            PackageId = packageId;
        }
    }
}
