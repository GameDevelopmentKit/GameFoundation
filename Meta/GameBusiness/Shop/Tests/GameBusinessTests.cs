namespace GameBusiness.Shop.Tests
{
    using System;
    using GameBusiness.Shop.Model;
    using GameFoundation.Scripts.Utilities.Extension;
    using NUnit.Framework;

    [TestFixture]
    public class PurchaseOptionDataTests
    {
        [Test]
        public void IsAvailableToPurchase_NoLimit_ReturnsTrue()
        {
            var option = new PurchaseOptionData
            {
                Record = new GameBusiness.Shop.Blueprint.PurchaseOptionRecord { LimitAmount = -1 }
            };

            bool isRefresh;
            Assert.IsTrue(option.IsAvailableToPurchase(out isRefresh));
            Assert.IsFalse(isRefresh);
        }

        [Test]
        public void IsAvailableToPurchase_WithinLimit_ReturnsTrue()
        {
            var option = new PurchaseOptionData
            {
                PurchasedAmount = 2,
                Record = new GameBusiness.Shop.Blueprint.PurchaseOptionRecord { LimitAmount = 5, RefreshLimitTime = -1 }
            };

            bool isRefresh;
            Assert.IsTrue(option.IsAvailableToPurchase(out isRefresh));
        }

        [Test]
        public void IsAvailableToPurchase_AtLimit_ReturnsFalse()
        {
            var option = new PurchaseOptionData
            {
                PurchasedAmount = 5,
                Record = new GameBusiness.Shop.Blueprint.PurchaseOptionRecord { LimitAmount = 5, RefreshLimitTime = -1 }
            };

            bool isRefresh;
            Assert.IsFalse(option.IsAvailableToPurchase(out isRefresh));
        }

        [Test]
        public void OnPurchase_IncrementsPurchasedAmount()
        {
            var option = new PurchaseOptionData
            {
                PurchasedAmount = 0,
                Record = new GameBusiness.Shop.Blueprint.PurchaseOptionRecord { LimitAmount = 5 }
            };

            option.OnPurchase(2);
            Assert.AreEqual(2, option.PurchasedAmount);
        }

        [Test]
        public void IsLimited_WithPositiveLimit_ReturnsTrue()
        {
            var option = new PurchaseOptionData
            {
                Record = new GameBusiness.Shop.Blueprint.PurchaseOptionRecord { LimitAmount = 5 }
            };

            Assert.IsTrue(option.IsLimited);
        }

        [Test]
        public void IsLimited_WithNoLimit_ReturnsFalse()
        {
            var option = new PurchaseOptionData
            {
                Record = new GameBusiness.Shop.Blueprint.PurchaseOptionRecord { LimitAmount = -1 }
            };

            Assert.IsFalse(option.IsLimited);
        }

        [Test]
        public void IsOneTimePurchase_LimitedNoRefresh_ReturnsTrue()
        {
            var option = new PurchaseOptionData
            {
                Record = new GameBusiness.Shop.Blueprint.PurchaseOptionRecord { LimitAmount = 1, RefreshLimitTime = -1 }
            };

            Assert.IsTrue(option.IsOneTimePurchase);
        }

        [Test]
        public void RemainAmount_CalculatesCorrectly()
        {
            var option = new PurchaseOptionData
            {
                PurchasedAmount = 3,
                Record = new GameBusiness.Shop.Blueprint.PurchaseOptionRecord { LimitAmount = 10 }
            };

            Assert.AreEqual(7, option.RemainAmount);
        }
    }

    [TestFixture]
    public class ShopPurchaseExceptionTests
    {
        [Test]
        public void Constructor_SetsError()
        {
            var ex = new ShopPurchaseException(ShopPurchaseError.PackageExpired);
            Assert.AreEqual(ShopPurchaseError.PackageExpired, ex.Error);
        }

        [Test]
        public void Constructor_SetsMessage()
        {
            var ex = new ShopPurchaseException(ShopPurchaseError.PackageExpired);
            Assert.IsTrue(ex.Message.Contains("PackageExpired"));
        }

        [Test]
        public void Constructor_WithMessage_UsesCustomMessage()
        {
            var ex = new ShopPurchaseException(ShopPurchaseError.PackageUnavailable, "Custom error");
            Assert.AreEqual("Custom error", ex.Message);
            Assert.AreEqual(ShopPurchaseError.PackageUnavailable, ex.Error);
        }
    }

    [TestFixture]
    public class DateTimeUtilsTests
    {
        [Test]
        public void GetNearestTimeFromPeriod_ZeroPeriod_ReturnsSameTime()
        {
            var time = new DateTime(2025, 1, 15, 12, 0, 0);
            var result = time.GetNearestTimeFromPeriod(0);
            Assert.AreEqual(time, result);
        }

        [Test]
        public void GetNearestTimeFromPeriod_NegativePeriod_ReturnsSameTime()
        {
            var time = new DateTime(2025, 1, 15, 12, 0, 0);
            var result = time.GetNearestTimeFromPeriod(-100);
            Assert.AreEqual(time, result);
        }

        [Test]
        public void GetNearestTimeFromPeriod_MinValue_UsesUtcNow()
        {
            var result = DateTime.MinValue.GetNearestTimeFromPeriod(3600);
            Assert.AreNotEqual(DateTime.MinValue, result);
        }
    }

    [TestFixture]
    public class ExchangePackageDataTests
    {
        [Test]
        public void IsExpired_MinValueEndTime_ReturnsFalse()
        {
            var data = new ExchangePackageData { EndTime = DateTime.MinValue };
            Assert.IsFalse(data.IsExpired);
        }

        [Test]
        public void IsExpired_PastEndTime_ReturnsTrue()
        {
            var data = new ExchangePackageData { EndTime = DateTime.Now.AddSeconds(-10) };
            Assert.IsTrue(data.IsExpired);
        }

        [Test]
        public void IsExpired_FutureEndTime_ReturnsFalse()
        {
            var data = new ExchangePackageData { EndTime = DateTime.Now.AddSeconds(3600) };
            Assert.IsFalse(data.IsExpired);
        }

        [Test]
        public void RemainingTime_MinValueEndTime_ReturnsMaxValue()
        {
            var data = new ExchangePackageData { EndTime = DateTime.MinValue };
            Assert.AreEqual(TimeSpan.MaxValue, data.RemainingTime);
        }
    }
}
