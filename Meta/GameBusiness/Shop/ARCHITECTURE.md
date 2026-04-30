# GameBusiness Architecture

This document is the single source of truth for the module's internal design.
Written for three audiences: humans reading code, maintainers extending the
module, and AI agents tasked with modifying or integrating it.

## Module Boundary

```
Assets/SharedModules/GameBusiness/     <-- THIS MODULE (Meta.Shop.asmdef)
  Blueprint/
    ExchangePackageBlueprint.cs      CSV reader for exchange packages
    ShopLayoutBlueprint.cs           CSV reader for shop UI layout
  Installer/
    BaseShopInstaller.cs             Abstract generic installer (Zenject Installer<T>)
  Manager/
    ShopManager.cs                   Generic ShopManager<TData> : IShopService (core logic)
    ShopPurchasePackageSuccessSignal.cs  Zenject signal on purchase
  Model/
    ExchangePackageData.cs           ExchangePackageData + PurchaseOptionData
    IShopData.cs                     Interface: Dictionary<string, ExchangePackageData>
    ShopPurchaseException.cs         Exception + ShopPurchaseError enum
  Services/
    IShopService.cs                  Non-generic shop interface (UI depends on this)
    ICostTextGenerator.cs            Strategy for cost text formatting
  UI/
    ThemeConfig.cs                   Serializable color palette applicator
    ShopItemModel.cs                 View-model for shop items (MultiplePrefabsModel)
    ShopSectionModel.cs              View-model for shop sections (MultiplePrefabsModel)
    BasePurchaseButton.cs            Abstract MonoBehaviour for purchase buttons
    BaseShopItemView.cs              Abstract TViewMono for shop item views
    BaseShopSectionPresenter.cs      Abstract presenter for shop sections + ShopSectionView
  Tests/
    GameBusiness.Tests.asmdef        Editor-only test assembly
    GameBusinessTests.cs             Unit tests for data models and utilities
```

## Design Principles

1. **No game-specific code.** The module must never reference a concrete game
   project. All game-specific behavior is injected via generics and abstract
   methods.

2. **Throw, don't toast.** `ShopManager<T>.PurchaseExchangePackageAsync` throws
   typed exceptions (`ShopPurchaseException`, `InsufficientAssetException`,
   `PaymentServiceException`). The game's handler catches and decides how to
   display errors.

3. **Data in, data out.** ShopManager operates on `ExchangePackageData` and
   returns `TransactionResult`. It never touches UI, localization, or assets.

4. **Generics over interfaces for managers.** `ShopManager<TData>` and
   `BaseInterstitialManager<TData>` use generic type parameters (not
   strategy interfaces) because the gdk.core `BaseDataManager<T>` pattern
   requires it for serialization/deserialization of user data.

## Class Relationships

```
                    +-----------------------+
                    | BaseDataManager<T>    |  (gdk.core)
                    | (serialization, save) |
                    +----------+------------+
                               |
              +----------------+----------------+
              |                                 |
  +-----------v-----------+      +--------------v--------------+
  | ShopManager<TData>    |      | BaseInterstitialManager<T>  |
  | : IShopService        |      | where T: InterstitialData   |
  | where TData: IShopData|      |                              |
  |                       |      | MarkPurchasedOffer           |
  | QueryExchangePackage  |      | RefreshInterstitialOffers    |
  | UnlockExchangePackage |      | Verified                     |
  | PurchaseAsync (throws)|      | ActiveInterstitialOffers     |
  | TryGetPossibleOptions |      |                              |
  | GetIapProductIds      |      | abstract OnOfferActivated    |
  | TryGetShopLayoutRecord|      | abstract IsPackageAvailable  |
  +-----------+-----------+      | abstract IsPackageExpired    |
              |                  +--------------+---------------+
   Game side  |                                |  Game side
              v                                v
  +-----------+-----------+      +--------------+--------------+
  | BAShopManager         |      | InterstitialManager         |
  | : ShopManager<ShopData|      | : BaseDataManager<Interst...| (*)
  |                       |      |                              |
  | + Gacha methods       |      | + PlayerRespawnSignal        |
  | + GachaBlueprint dep  |      | + Crashlytics logging        |
  +-----------+-----------+      | + GameQueueAction UI         |
              |                  +-----------------------------+
              v
  +-----------+-----------+
  | ShopServiceHandler    |
  | : IInitializable      |
  | : ICostTextGenerator  |
  |                       |
  | + IAP init (tangles)  |
  | + Toast messages      |
  | + Localized cost text |
  | + GetPossiblePayouts  |
  +-----------------------+

  Module Installer & UI Layer:

  +-----------------------------------+
  | BaseShopInstaller<TInst,TData,TMgr|  (module)
  | Binds: TManager, ShopManager<T>,  |
  |   IShopService, Signal            |
  | virtual InstallGameBindings()     |
  +----------------+------------------+
                   |  Game side
                   v
  +----------------+---------+
  | ShopInstaller            |
  | : BaseShopInstaller<     |
  |   ShopInstaller,         |
  |   ShopData, BAShopManager|
  | Binds: ShopServiceHandler|
  +-------- +-+--------------+

  +----------------------------+     +------------------------+
  | BasePurchaseButton (module)|     | BaseShopItemView       |
  | [Inject] IShopService      |     | (module, abstract)     |
  | [Inject] ICostTextGenerator|     | theme, title, tag,     |
  | abstract OnClickBtnPurchase|     | icon, buttons hooks    |
  | abstract OnPurchaseComplete|     +----------+-------------+
  +-------------+--------------+                |
                |                               v
                v                    +----------+-------------+
  +-------------+--------------+     | ShopItemCommonView (BA)|
  | PurchaseButton (BA)        |     | + LoadImageHelper      |
  | + ClaimRewardPopup         |     | + AssetService         |
  | + ShopServiceHandler       |     +-----------+------------+
  +----------------------------+                 |
                                                 v
  +------------------------------------------+   
  | BaseShopSectionPresenter (module)        |   
  | PrepareContent, CreateItemModel          |   
  | abstract BindContent(models, contentRoot)|   
  +-------------------+----------------------+   
                      |                          
                      v                          
  +-------------------+----------------------+   
  | ShopSectionPresenter (BA)                |   
  | + TextLocalizer section header           |   
  | subclasses: Grid, Flex, Page             |   
  +------------------------------------------+   

  (*) BA's InterstitialManager currently extends BaseDataManager directly.
      Migration to BaseInterstitialManager is a Phase 2 task.
```

## Key Types Reference

### ShopManager<TData> (Module)

| Method | Returns | Throws | Purpose |
|--------|---------|--------|---------|
| `QueryExchangePackage(packageId)` | `ExchangePackageData?` | - | Look up a package by ID |
| `QueryExchangePackages(packageIds)` | `List<ExchangePackageData>` | - | Batch query |
| `UnlockExchangePackage(packageId)` | void | - | Unlock and start timer |
| `TryGetPossiblePurchaseOptions(pkg, qty, out opt)` | bool | - | Find affordable option |
| `PurchaseExchangePackageAsync(pkg, qty)` | `UniTask<TransactionResult>` | `ShopPurchaseException`, `InsufficientAssetException`, `PaymentServiceException` | Execute purchase |
| `GetIapProductIds()` | `Dictionary<string, ProductType>` | - | Enumerate IAP product IDs for initialization |
| `TryGetShopLayoutRecord(id, out rec)` | bool | - | Query shop layout |
| `TryGetDefaultShopLayoutRecord(out rec)` | bool | - | Query "MainShop" layout |

### IShopData (Module)

```csharp
public interface IShopData : IUserData
{
    Dictionary<string, ExchangePackageData> PurchasedPackages { get; }
}
```

Your game's data class must implement this. It can add any additional fields
(e.g., GachaItems, DailyDeals, SeasonalData).

### BaseInterstitialManager<TData> (Module)

| Method | Type | Purpose |
|--------|------|---------|
| `OnDataInitialized()` | virtual | Syncs blueprint data to InterstitialOffers dictionary |
| `MarkPurchasedOffer(id)` | concrete | Increments purchase count, transitions to Purchased state |
| `RefreshInterstitialOffers()` | concrete | Expires offers whose packages are all expired |
| `Verified(id)` | virtual | Activates an offer, calls OnOfferActivated |
| `ActiveInterstitialOffers()` | concrete | Returns list of active offers with available packages |
| `OnOfferActivated(offer)` | **abstract** | Game unlocks packages and shows UI |
| `IsPackageAvailable(pkgId)` | **abstract** | Game checks if package is unlocked and not expired |
| `IsPackageExpiredOrUnavailable(pkgId)` | **abstract** | Game checks if package should be considered gone |
| `LogWarning(msg)` | virtual | Defaults to Debug.LogWarning; override for Crashlytics etc. |

### ShopPurchaseException (Module)

```csharp
public enum ShopPurchaseError { PackageExpired, PackageUnavailable }

public class ShopPurchaseException : Exception
{
    public ShopPurchaseError Error { get; }
}
```

### ExchangePackageData (Module)

Core data model for a purchasable package. Key properties:

| Property | Type | Notes |
|----------|------|-------|
| `InstancePackageId` | string | Unique instance ID (may differ from blueprint ID for multi-instance packages) |
| `BlueprintId` | string | Links to ExchangePackageBlueprint |
| `PurchaseOptions` | `List<PurchaseOptionData>` | Ordered by priority |
| `CachedGeneratedPayoutAssets` | `List<Asset>?` | Auto-generated payouts cached here |
| `IsUnlocked` | bool | Must be true to purchase |
| `IsExpired` | bool | Computed: EndTime < Now |
| `Record` | ExchangePackageRecord | Runtime-only link to blueprint record |

## Extension Points

### Adding a new payment type

1. Add the type to `PaymentTypes` enum (in Transaction module)
2. No changes needed in GameBusiness module
3. In your game's handler, add the cost text formatting case

### Adding a new package feature

If the feature is game-specific (e.g., "featured" badge):
- Add it to your game's data class or handler. Do not modify the module.

If the feature is universal (e.g., "gift wrapping" for all exchange packages):
- Add it to `ExchangePackageRecord` in the blueprint
- Add corresponding field to `ExchangePackageData` if it needs persistence
- The module handles it; games get it for free

### Adding a new offer trigger

The module does not handle triggers (e.g., "show after respawn", "show on
level up"). Triggers are game-specific. Subscribe to your game's signals in
your `InterstitialManager` subclass and call `Verified(offerId)` or
`ShowActiveInterstitialOfferInQueue()`.

## For AI Agents

### Context an agent needs before modifying this module

1. Read this file (`ARCHITECTURE.md`)
2. Read `GameBusiness.asmdef` to confirm assembly dependencies
3. Read the specific file being modified
4. Verify no game-specific imports are introduced (grep for ToastMessageService,
   StaticValue, AssetService, Crashlytics, SceneDirector)

### Invariants to preserve

- **No BA imports.** Every `using` in module files must resolve to assemblies
  listed in `GameBusiness.asmdef`. If you need a type from `Game.Scripts`,
  the design is wrong â€” inject it via generic/abstract instead.
- **Throw, don't handle.** `PurchaseExchangePackageAsync` must never catch
  its own exceptions. Let them propagate to the game handler.
- **IShopData contract.** Any property added to `IShopData` becomes a
  requirement for ALL games using the module. Prefer adding to concrete
  data classes instead.
- **Signal namespace.** Signals must live in the module so all games share
  the same signal types for analytics consistency.

### Common agent tasks

**"Add a field to ExchangePackageData"**
1. Edit `Assets/SharedModules/GameBusiness/Shop/Model/ExchangePackageData.cs`
2. If it needs JSON persistence, add as a public property
3. If it is runtime-only, add `[JsonIgnore]`
4. If it references a type outside the module's asmdef, STOP and reconsider

**"Add a new method to ShopManager"**
1. Edit `Assets/SharedModules/GameBusiness/Shop/Manager/ShopManager.cs`
2. The method must work with `TData` (generic), not a concrete ShopData
3. If the method needs game-specific types (localization, assets), it belongs
   in the game's handler, not in the module

**"Add a new interstitial offer state"**
1. Edit `Assets/SharedModules/GameBusiness/InterstitialOffer/Model/InterstitialData.cs`
2. Add the state to `InterstitialOfferState` enum
3. Update `BaseInterstitialManager.RefreshInterstitialOffers()` if the new
   state has expiry logic

### Verification after changes

```bash
# Check no BA deps leaked into module
grep -rn "ToastMessageService\|StaticValue\|AssetService\|GooglePlayTangle\|Crashlytics\|SceneDirector" Assets/SharedModules/GameBusiness/ --include="*.cs"
# Should return nothing

# Check no duplicate classes
for c in ExchangePackageData ShopManager ShopPurchaseException BaseInterstitialManager; do
  echo "=== $c ===" && grep -rn "class $c" Assets/Scripts/ Assets/SharedModules/ --include="*.cs"
done
# Each class should appear exactly once

# Check asmdef doesn't reference Game.Scripts
grep "Game.Scripts" Assets/SharedModules/GameBusiness/GameBusiness.asmdef
# Should return nothing
```

## Version History

| Date | Change | Author |
|------|--------|--------|
| 2026-04-07 | Initial extraction from BackpackAdventures (Phase 1: Shop + InterstitialOffer) | Claude Code + NINH |
| 2026-04-08 | Added ShopInstaller, IShopService, ICostTextGenerator, base UI layer (BasePurchaseButton, BaseShopItemView, BaseShopSectionPresenter, ThemeConfig, ShopItemModel, ShopSectionModel). Migrated BA to extend module bases. | Claude Code + NINH |

## Loot Pool Integration (Current)

Shop random payouts are integrated through `Meta.Transactions` + `Meta.LootPool`.

- `ExchangePackage` payout row may use `AssetType = Pool` and `PayoutAssetId = <pool id>`.
- `PurchaseOptionRecord.AutoGeneratePayout = true` triggers pre-generation/caching in `ShopManager`.
- `TransactionManager.FlattenPayoutAssets()` expands `Pool` via `IRandomGeneratePayoutService` (`PoolPayoutService`).
- Generated rewards are persisted in `ExchangePackageData.CachedGeneratedPayoutAssets` until option refresh.

This is the intended path for Daily Shop random offers.

## Documentation Drift Notes

- `ShopManager` currently contains `GenerateCostText(...)` helpers.
- Earlier docs described cost-text behavior as handler-owned only; treat this as historical guidance.
