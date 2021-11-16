namespace GameFoundation.Scripts.Utilities.Extension
{
    using System;
    using TMPro;
    using UniRx;

    /// <summary>
    /// Extension for UniRx, use for Subcribe with TextMeshProUGUI
    /// </summary>
    public static class UniRxSubcribeTextExtension
    {
        public static IDisposable SubscribeToText(this IObservable<string> source, TextMeshProUGUI text)
        {
            return source.SubscribeWithState(text, (x, t) => t.text = x);
        }
        
        public static IDisposable SubscribeToText<T>(this IObservable<T> source, TextMeshProUGUI text)
        {
            return source.SubscribeWithState(text, (x, t) => t.text = x.ToString());
        }
    }
}
