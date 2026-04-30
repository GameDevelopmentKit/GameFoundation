namespace GameBusiness.LootPool.Service
{
    public interface ILootPoolRandomProvider
    {
        int RangeInt(int minInclusive, int maxExclusive);
        float RangeFloat(float minInclusive, float maxInclusive);
    }
}
