# GameBusiness Module

Shared shop and interstitial offer logic for Buzzle Studio games.
Extracted from BackpackAdventures (BA) as the first reusable module.

## Quick Start (for a new game)

### 1. Add the dependency

In your game's `.asmdef`, add `"GameBusiness"` to `references`.

### 2. Provide your data class

```csharp
// Implement IShopData. Add any game-specific fields you need.
public class MyGameShopData : IShopData
{
    public Dictionary<string, ExchangePackageData> PurchasedPackages { get; set; } = new();
    // your game-specific fields here...
}
```

### 3. Create your concrete ShopManager

```csharp
public class MyGameShopManager : ShopManager<MyGameShopData>
{
    public MyGameShopManager(SignalBus signalBus,
        ShopLayoutBlueprint shopLayoutBlueprint,
        ExchangePackageBlueprint exchangePackageBlueprint,
        ITransactionManager transactionManager,
        IIapServices iapServices) : base(signalBus, shopLayoutBlueprint,
            exchangePackageBlueprint, transactionManager, iapServices) { }
}
```

### 4. Bind in your Zenject installer

```csharp
Container.Bind<MyGameShopManager>().AsSingle();
Container.Bind<ShopManager<MyGameShopData>>().FromResolve<MyGameShopManager>();
Container.DeclareSignal<ShopPurchasePackageSuccessSignal>();
```

### 5. Wrap with a game-specific handler (recommended)

The module's `ShopManager<T>` is intentionally bare — it throws exceptions
instead of showing toasts and does not initialize IAP. Create a handler class
in your game that wraps it with your UI feedback, localization, and IAP setup.

See `BackpackAdventures/Assets/Scripts/GameBusiness/Shop/Manager/ShopServiceHandler.cs`
for a complete reference implementation.

### 6. Interstitial Offers (optional)

```csharp
public class MyInterstitialManager : BaseInterstitialManager<InterstitialData>
{
    private readonly MyGameShopManager shopManager;

    public MyInterstitialManager(InterstitialBlueprint blueprint,
        SignalBus signalBus, MyGameShopManager shopManager) : base(blueprint, signalBus)
    {
        this.shopManager = shopManager;
    }

    protected override void OnOfferActivated(InterstitialOffer offer)
    {
        // Unlock packages, show UI popup
    }

    protected override bool IsPackageAvailable(string packageId)
    {
        var pkg = shopManager.QueryExchangePackage(packageId);
        return pkg != null && pkg.IsUnlocked && !pkg.IsExpired;
    }

    protected override bool IsPackageExpiredOrUnavailable(string packageId)
    {
        var pkg = shopManager.QueryExchangePackage(packageId);
        return pkg == null || (pkg.IsUnlocked && pkg.IsExpired);
    }
}
```

## Blueprint CSV Requirements

The module reads CSV blueprints via `GenericBlueprintReaderByRow`. Your game
must provide these blueprint CSV files:

| Blueprint | Key Column | Required Columns |
|-----------|-----------|-----------------|
| `ExchangePackage` | `PackageId` | PackageName, PackageDescription, PackageIcon, AvailableTime, DefaultUnlock, Payouts (sub-table), PurchaseOptions (sub-table) |
| `ShopLayout` | `LayoutId` | Sections (sub-table with SectionId, SectionName, SectionDisplayOrder, PackageRecords) |
| `Interstitial` | `Id` | Name, ExchangePackageName, PrefabAddressablePath, PresenterType, IconAddressablePath, ShowDiscount, DiscountValue |

## Assembly Dependencies

```
GameBusiness
  +-- GameFoundation.DataManager   (BaseDataManager, IUserData, Blueprint readers)
  +-- GameFoundation.Utilities     (Extension methods)
  +-- Transaction                  (ITransactionManager, CostRecord, PayoutRecord, Asset)
  +-- Wallet                       (Currency types)
  +-- 3rd.IAP                      (IIapServices, ProductType)
  +-- Zenject-Source               (DI container)
  +-- Zenject-Signals              (SignalBus)
  +-- UniTask                      (Async operations)
```

The module does **not** depend on:
- `GameFoundation.UIModule` (no UI code)
- `Unity.TextMeshPro` (no text rendering)
- `Unity.Localization` (no locale strings — games provide their own)
- Any `3rd.*` services beyond IAP
- Any game-specific code

## Running Tests

1. Open Unity Editor
2. Window > General > Test Runner
3. Select "EditMode" tab
4. Run tests in `GameBusiness.Tests` assembly

Tests cover: `ExchangePackageData`, `PurchaseOptionData`, `ShopPurchaseException`,
and `DateTimeUtils`. They do **not** require mocking DI — they test pure data logic.

## Phase 2 Roadmap (not yet implemented)

- `LootPool` module: Rarity enum, LootPoolTablesBlueprint, BaseLootDropService
- GachaBlueprint moves into GameBusiness (currently blocked by Rarity dependency)
- ShopSectionModel / ShopItemModel extracted to GameBusiness
- DateTimeUtils / NumberHelper consolidated to gdk.core Utilities
