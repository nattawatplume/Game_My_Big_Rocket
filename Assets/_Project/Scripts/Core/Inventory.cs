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

        public int Total
        {
            get
            {
                int n = 0;
                foreach (var kv in counts) n += kv.Value;
                return n;
            }
        }

        /// <summary>Spends parts of any kind: lowest tier first, then the most plentiful, so good parts last longest.</summary>
        public bool TrySpendAny(int amount)
        {
            if (Total < amount) return false;
            for (int i = 0; i < amount; i++)
            {
                PartKey best = default;
                int most = 0;
                foreach (var kv in counts)
                {
                    if (kv.Value <= 0) continue;
                    if (most == 0 || kv.Key.tier < best.tier || (kv.Key.tier == best.tier && kv.Value > most))
                    {
                        most = kv.Value;
                        best = kv.Key;
                    }
                }
                counts[best] = most - 1;
            }
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
