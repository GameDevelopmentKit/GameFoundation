# Meta.LootPool

Reusable loot pool module for meta-layer random rewards.

## Purpose

`Meta.LootPool` provides weighted pool selection and nested pool resolution for transaction payouts.
It is intentionally generic and independent from combat drop systems (enemy level, rarity tables, gacha, etc.).

## Runtime Flow

1. A transaction/shop payout emits an asset with:
   - `AssetType = Pool`
   - `AssetId = <DropPoolId>`
2. `TransactionManager.FlattenPayoutAssets()` detects `IRandomGeneratePayoutService` for `Pool`.
3. `PoolPayoutService` resolves pool entries through `LootPoolService`.
4. Pool entries may recursively reference other pools.
5. Final concrete assets are flattened and returned to normal payout services.

## Blueprint

Blueprint file name: `LootPoolTables.csv`

Columns:

- `DropPoolId`
- `MinDropCount`
- `MaxDropCount`
- `ElementId`
- `AssetType`
- `IsInteger`
- `MaxValue`
- `MinValue`
- `Weight`

Notes:

- `Weight < 0` means guaranteed include.
- `Weight >= 0` participates in weighted random picks.
- `AssetType = Pool` means nested pool.

## Installation

Install in scene context before or with transactions:

```csharp
LootPoolInstaller.Install(this.Container);
TransactionInstaller.Install(this.Container);
```

Also ensure consuming game asmdef references `Meta.LootPool`.

## Asset Types

`Meta.Transactions.AssetDefaultType` now includes:

- `Currency`
- `Item`
- `Pool`

Project-specific asset types (like `PlayerSkin`, `Booster`) are still supported by project payout services.
