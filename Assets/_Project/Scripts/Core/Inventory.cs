using System;
using System.Collections.Generic;

namespace IslandRocketRumble
{
    /// <summary>Parts one player is carrying. Plain C# so UI and gameplay can share it.</summary>
    public class Inventory
    {
        readonly Dictionary<PartKey, int> counts = new Dictionary<PartKey, int>();

        public event Action Changed;

        public int Count(PartKey key) => counts.TryGetValue(key, out var n) ? n : 0;

        public void Add(PartKey key, int amount = 1)
        {
            counts[key] = Count(key) + amount;
            Changed?.Invoke();
        }

        public bool CanAfford(RocketRecipe recipe)
        {
            foreach (var cost in recipe.costs)
                if (Count(cost.Key) < cost.count) return false;
            return true;
        }

        public bool TrySpend(RocketRecipe recipe)
        {
            if (!CanAfford(recipe)) return false;
            foreach (var cost in recipe.costs)
                counts[cost.Key] = Count(cost.Key) - cost.count;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Turns every part below <paramref name="tier"/> into parts of that tier, same type: ratio old = 1 new, rounded up.</summary>
        public void ConvertBelow(PartTier tier, int ratio)
        {
            ratio = Math.Max(1, ratio);
            var old = new List<PartKey>();
            foreach (var kv in counts)
                if (kv.Key.tier < tier && kv.Value > 0) old.Add(kv.Key);
            if (old.Count == 0) return;
            foreach (var key in old)
            {
                int converted = (counts[key] + ratio - 1) / ratio;
                counts[key] = 0;
                var fresh = new PartKey(key.type, tier);
                counts[fresh] = Count(fresh) + converted;
            }
            Changed?.Invoke();
        }

        public IEnumerable<KeyValuePair<PartKey, int>> All() => counts;

        public void Clear()
        {
            counts.Clear();
            Changed?.Invoke();
        }
    }
}
