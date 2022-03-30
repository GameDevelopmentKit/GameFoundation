﻿namespace GameFoundation.Scripts.Utilities.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Random = UnityEngine.Random;

    public static class ListExtension
    {
        public static List<T> GetListRandom<T>(this object obj, List<T> seedData, int amount)
        {
            var result = new List<T>();
            for (int i = 0; i < amount; i++)
            {
                result.Add(seedData[Random.Range(0, seedData.Count)]);
            }

            return result;
        }
        
        public static List<int> GetRandomListInt(this object obj,int amount, int rangeMin = 0, int rangeMax = 100)
        {
            var result = new List<int>();
            for (var i = 0; i < amount; i++)
            {
                result.Add(Random.Range(rangeMin, rangeMax));
            }

            return result;
        }

        public static T RandomElement<T>(this IList<T> list) { return list[Random.Range(0, list.Count)]; }
        
        public static T PickRandom<T>(this IEnumerable<T> source)
        {
            return source.PickRandom(1).Single();
        }

        public static IEnumerable<T> PickRandom<T>(this IEnumerable<T> source, int count)
        {
            return source.Shuffle().Take(count);
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(x => Guid.NewGuid());
        }
    }
}