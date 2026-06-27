using System;
using System.Collections.Generic;
using System.Linq;

namespace Arrowgene.Ddon.Shared
{
    public static class ListExtension
    {
        public static T GetWeightedRandomElement<T>(this List<T> list, double bias)
        {
            double totalWeight = 0;
            for (int i = 0; i < list.Count; i++)
            {
                totalWeight += Math.Pow(list.Count - i, bias);
            }

            double x = Random.Shared.NextDouble() * totalWeight;

            double cumulativeWeight = 0;
            for (int i = 0; i < list.Count; i++)
            {
                cumulativeWeight += Math.Pow(list.Count - i, bias);
                if (x < cumulativeWeight)
                {
                    return list[i];
                }
            }

            return list[^1];
        }

        public static T Mode<T>(this IEnumerable<T> collection)
        {
            return collection
                .GroupBy(value => value) // Group elements by their value
                .OrderByDescending(group => group.Count()) // Order groups by the count of elements
                .Select(group => group.Key) // Select the key (the element value) of the group
                .First(); // Take the first key (the mode)
        }
    }
}
