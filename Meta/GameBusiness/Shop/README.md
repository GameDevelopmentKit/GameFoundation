# GameBusiness Module

Shared shop logic for Buzzle Studio games.

## Current Scope

Implemented in this module:

- Exchange package data/query/purchase flow
- Shop layout/section rendering model support
- Purchase option limits/refresh handling
- Auto-generated payout caching (`AutoGeneratePayout`)
- Daily-shop compatible random payout flow via `Meta.LootPool` + `Pool` asset type

## Quick Start (for a new game)

### 1. Add dependencies

In your game asmdef, include at least:

- `Meta.Shop`
- `Meta.Transactions`
- `Meta.LootPool` (if using pool/random payouts)

### 2. Provide your data class

```csharp
public class MyGameShopData : IShopData
{
    public Dictionary<string, ExchangePackageData> PurchasedPackages { get; set; } = new();
}
```

### 3. Create your concrete manager

```csharp
public class MyGameShopManager : ShopManager<MyGameShopData>
{
    public MyGameShopManager(
        SignalBus signalBus,
        ShopLayoutBlueprint shopLayoutBlueprint,
        ExchangePackageBlueprint exchangePackageBlueprint,
        ITransactionManager transactionManager,
        IIapServices iapServices) : base(
            signalBus,
            shopLayoutBlueprint,
            exchangePackageBlueprint,
            transactionManager,
            iapServices)
    {
    }
}
```

### 4. Bind installers

```csharp
LootPoolInstaller.Install(this.Container); // if using Pool payouts
TransactionInstaller.Install(this.Container);
ShopInstaller.Install(this.Container);
```

## Blueprint CSV Requirements

| Blueprint | Key Column | Required Columns |
|---|---|---|
| `ExchangePackage` | `PackageId` | PackageName, PackageDescription, PackageIcon, AvailableTime, DefaultUnlock, Payouts, PurchaseOptions |
| `ShopLayout` | `LayoutId` | Sections (SectionId, SectionName, SectionDisplayOrder, PackageRecords) |
| `LootPoolTables` (optional) | `DropPoolId` | MinDropCount, MaxDropCount, DropPoolItems (ElementId, AssetType, IsInteger, MinValue, MaxValue, Weight) |

## Daily Shop + Loot Pool Pattern

To make a daily package roll a random item and keep it stable until refresh:

1. In `ExchangePackage.csv`, set payout row to:
   - `AssetType = Pool`
   - `PayoutAssetId = <pool id>`
   - `PayoutAmount = 1`
2. In purchase option row:
   - `AutoGeneratePayout = TRUE`
   - `RefreshLimitTime = 86400` (or target interval)
3. Define `<pool id>` entries in `LootPoolTables.csv`.

`ShopManager.TryGetPossiblePurchaseOptions()` will cache generated assets in `CachedGeneratedPayoutAssets`, so UI preview and actual payout stay consistent until option refresh.

## Notes

- The manager still throws domain exceptions; game handlers own UX.
- Cost text generation currently exists in `ShopManager` and can be overridden by game side if needed.
