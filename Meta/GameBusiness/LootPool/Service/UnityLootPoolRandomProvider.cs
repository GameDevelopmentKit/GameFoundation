namespace GameBusiness.LootPool.Service
{
    using Random = UnityEngine.Random;

    public class UnityLootPoolRandomProvider : ILootPoolRandomProvider
    {
        public int RangeInt(int minInclusive, int maxExclusive)
        {
            return Random.Range(minInclusive, maxExclusive);
        }

        public float RangeFloat(float minInclusive, float maxInclusive)
        {
            return Random.Range(minInclusive, maxInclusive);
        }
    }
}
