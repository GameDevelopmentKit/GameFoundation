namespace GameBusiness.LootPool.Service
{
    using System;
    using System.Collections.Generic;
    using GameBusiness.LootPool.Blueprint;
    using GameBusiness.Transactions.Blueprint;
    using GameBusiness.Transactions.Model;

    public class LootPoolService
    {
        private readonly LootPoolTablesBlueprint lootPoolTablesBlueprint;
        private readonly ILootPoolRandomProvider randomProvider;

        public LootPoolService(
            LootPoolTablesBlueprint lootPoolTablesBlueprint,
            ILootPoolRandomProvider randomProvider)
        {
            this.lootPoolTablesBlueprint = lootPoolTablesBlueprint;
            this.randomProvider = randomProvider;
        }

        public DropPoolTables GetPoolWithId(string poolId)
        {
            return this.lootPoolTablesBlueprint.GetDataById(poolId);
        }

        public List<Asset> GetAllPossibleDropAssets(string poolId, int amount = 1)
        {
            var itemPool = this.GetPoolWithId(poolId);
            var dropList = new List<Asset>();

            foreach (var possibleDrop in itemPool.DropPoolItems)
            {
                if (possibleDrop.AssetType == AssetDefaultType.Pool)
                {
                    dropList.AddRange(this.GetAllPossibleDropAssets(possibleDrop.ElementId, amount));
                }
                else if (!string.IsNullOrEmpty(possibleDrop.AssetType))
                {
                    var selectedItem = new Asset
                    {
                        AssetId = possibleDrop.ElementId,
                        AssetType = possibleDrop.AssetType,
                        Amount = possibleDrop.IsInteger
                            ? (int)possibleDrop.MaxValue * amount
                            : possibleDrop.MaxValue * amount,
                    };

                    dropList.Add(selectedItem);
                }
            }

            return dropList;
        }

        public List<Asset> GetDropAssets(string poolId, List<string> excludedAssetIds = null)
        {
            return this.GetDropAssets(this.GetPoolWithId(poolId), excludedAssetIds);
        }

        public List<Asset> GetDropAssets(DropPoolTables itemPool, List<string> excludedAssetIds = null)
        {
            var dropList = new List<Asset>();
            var totalWeight = 0f;

            foreach (var possibleDrop in itemPool.DropPoolItems)
            {
                if (excludedAssetIds != null && excludedAssetIds.Contains(possibleDrop.ElementId))
                {
                    continue;
                }

                if (possibleDrop.Weight < 0)
                {
                    dropList.Add(new Asset
                    {
                        AssetId = possibleDrop.ElementId,
                        AssetType = possibleDrop.AssetType,
                        Amount = possibleDrop.IsInteger
                            ? this.randomProvider.RangeInt((int)possibleDrop.MinValue, (int)possibleDrop.MaxValue)
                            : this.randomProvider.RangeFloat(possibleDrop.MinValue, possibleDrop.MaxValue),
                    });
                }
                else
                {
                    totalWeight += possibleDrop.Weight;
                }
            }

            var dropCount = this.randomProvider.RangeInt(itemPool.MinDropCount, itemPool.MaxDropCount + 1);
            var randomItems = this.GetAssetsWithWeight(itemPool.DropPoolItems, totalWeight, dropCount, excludedAssetIds);
            dropList.AddRange(randomItems);
            this.RemoveUnresolvedPools(dropList);

            return dropList;
        }

        private List<Asset> GetAssetsWithWeight(
            List<DropTableElement> dropPool,
            float totalWeight,
            int dropCount,
            List<string> excludedAssetIds = null)
        {
            var selectedItems = new List<Asset>();

            if (totalWeight <= 0)
            {
                return selectedItems;
            }

            for (var i = 0; i < dropCount; i++)
            {
                var randomValue = this.randomProvider.RangeFloat(0f, totalWeight);
                var cumulativeWeight = 0f;

                foreach (var item in dropPool)
                {
                    if (item.Weight < 0 || (excludedAssetIds != null && excludedAssetIds.Contains(item.ElementId)))
                    {
                        continue;
                    }

                    cumulativeWeight += item.Weight;

                    if (randomValue <= cumulativeWeight)
                    {
                        if (item.AssetType == AssetDefaultType.Pool)
                        {
                            selectedItems.AddRange(this.GetDropAssets(this.lootPoolTablesBlueprint[item.ElementId], excludedAssetIds));
                        }
                        else if (!string.IsNullOrEmpty(item.AssetType))
                        {
                            selectedItems.Add(new Asset
                            {
                                AssetId = item.ElementId,
                                AssetType = item.AssetType,
                                Amount = item.IsInteger
                                    ? this.randomProvider.RangeInt((int)item.MinValue, (int)item.MaxValue)
                                    : this.randomProvider.RangeFloat(item.MinValue, item.MaxValue),
                            });
                        }

                        break;
                    }
                }
            }

            return selectedItems;
        }

        private void RemoveUnresolvedPools(List<Asset> dropList)
        {
            dropList.RemoveAll(asset => string.Equals(asset.AssetType, AssetDefaultType.Pool, StringComparison.Ordinal));
        }
    }
}
