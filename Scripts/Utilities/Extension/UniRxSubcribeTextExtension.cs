namespace GameFoundation.Scripts.Utilities.Extension
{
    using System;
    using R3;
    using TMPro;

    /// <summary>
    /// Extension for UniRx, use for Subcribe with TextMeshProUGUI
    /// </summary>
    public static class UniRxSubcribeTextExtension
    {
        public static IDisposable SubscribeToText(this ReactiveProperty<string> source, TextMeshProUGUI text)
        {
            return source.Subscribe(text, (x, t) => t.text = x);
        }

        public static IDisposable SubscribeToText<T>(this ReactiveProperty<T> source, TextMeshProUGUI text)
        {
            return source.Subscribe(text, (x, t) => t.text = x.ToString());
        }
    }
}