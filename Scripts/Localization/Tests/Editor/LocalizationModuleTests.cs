namespace Localization.Tests.Editor
{
    using System.Collections.Generic;
    using Localization.Blueprint;
    using Localization.Interfaces;
    using Localization.Signals;
    using Localization.UnityLocalization;
    using NUnit.Framework;
    using UnityEngine.Localization;
    using Zenject;

    [TestFixture]
    public class LocalizationModuleTests
    {
        [Test]
        public void LocalizationDataOnline_ReturnsTextAndEmptyMissingValues()
        {
            var data = new LocalizationDataOnline
            {
                LocalizationDatas = new Dictionary<string, LocalizationDataModel>
                {
                    ["en-US"] = new LocalizationDataModel
                    {
                        LocalizedTexts = new Dictionary<string, string>
                        {
                            ["hello"] = "Hello"
                        }
                    }
                }
            };

            Assert.AreEqual("Hello", data.GetLocalizedText("en-US", "hello"));
            Assert.IsTrue(data.TryGetLocalizedText("en-US", "hello", out var localizedText));
            Assert.AreEqual("Hello", localizedText);

            Assert.AreEqual(string.Empty, data.GetLocalizedText("en-US", "missing"));
            Assert.IsFalse(data.TryGetLocalizedText("en-US", "missing", out var missingText));
            Assert.AreEqual(string.Empty, missingText);
            Assert.IsNull(data.GetLocalizationData("vi-VN"));
        }

        [Test]
        public void LocalizationHelper_NullStringReferenceReturnsFallback()
        {
            LocalizedString localizedString = null;

            Assert.AreEqual("Fallback", localizedString.GetLocalizedStringWithFallback("Fallback"));
            Assert.IsFalse(LocalizationHelper.IsDefaultNoTranslationMsg(null));
            Assert.IsFalse(LocalizationHelper.IsDefaultNoTranslationMsg(" "));
        }

        [Test]
        public void LocalizationInstaller_BindsServiceDataAndSignal()
        {
            var container = new DiContainer();

            SignalBusInstaller.Install(container);
            LocalizationInstaller.Install(container);

            Assert.IsNotNull(container.Resolve<LocalizationDataOnline>());
            Assert.IsInstanceOf<UnityLocalizationServices>(container.Resolve<ILocalizationService>());
            Assert.DoesNotThrow(() => container.Resolve<SignalBus>().Fire(new LocaleChangedSignal(null)));
        }
    }
}
