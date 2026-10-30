using System.Collections.Generic;
using UnityEngine;

namespace IslandRocketRumble
{
    public enum UpgradeType
    {
        SharpWarhead, QuickRail, RobotHunter, Recycler,
        IslandArmor, AutoRepair, EmergencyShield, Craftsman,
        TurboShoes, LongArm, HardHead, KeenEye,
    }

    public enum UpgradeBranch { Attack, Defense, Mobility }

    /// <summary>Display data of one upgrade card.</summary>
    public class UpgradeDef
    {
        public UpgradeType type;
        public UpgradeBranch branch;
        public string name;
        public string shortName;
        public string description;
    }

    /// <summary>All upgrade cards and how a hand of three is dealt.</summary>
    public static class UpgradeCatalog
    {
        public const int MaxStacks = 2;
        public const int HandSize = 3;

        public static readonly UpgradeDef[] All =
        {
            Def(UpgradeType.SharpWarhead, UpgradeBranch.Attack, "หัวรบคม", "หัวรบ", "ดาเมจใส่เกาะ +15%"),
            Def(UpgradeType.QuickRail, UpgradeBranch.Attack, "รางยิงไว", "รางยิง", "คูลดาวน์ยิง -20%"),
            Def(UpgradeType.RobotHunter, UpgradeBranch.Attack, "นักล่าหุ่น", "นักล่า", "ยิงหุ่นดรอป +1 ชิ้น"),
            Def(UpgradeType.Recycler, UpgradeBranch.Attack, "นักรีไซเคิล", "รีไซเคิล", "คราฟต์จรวด 25% ได้คืน 1 ชิ้น"),
            Def(UpgradeType.IslandArmor, UpgradeBranch.Defense, "เกราะเกาะ", "เกราะ", "เลือดสูงสุด +150 ฟื้นทันที 150"),
            Def(UpgradeType.AutoRepair, UpgradeBranch.Defense, "ซ่อมอัตโนมัติ", "ซ่อมเอง", "ฟื้น 2 HP ต่อวินาที"),
            Def(UpgradeType.EmergencyShield, UpgradeBranch.Defense, "โล่ฉุกเฉิน", "โล่", "ได้โล่ฟรีทุก 60 วินาที"),
            Def(UpgradeType.Craftsman, UpgradeBranch.Defense, "ช่างฝีมือ", "ช่าง", "ซ่อมเกาะฟื้นเพิ่ม 50%"),
            Def(UpgradeType.TurboShoes, UpgradeBranch.Mobility, "รองเท้าเทอร์โบ", "เทอร์โบ", "เดินเร็ว +15%"),
            Def(UpgradeType.LongArm, UpgradeBranch.Mobility, "มือยาว", "มือยาว", "ระยะเก็บของ +40%"),
            Def(UpgradeType.HardHead, UpgradeBranch.Mobility, "หัวแข็ง", "หัวแข็ง", "โดนสตันสั้นลง 40%"),
            Def(UpgradeType.KeenEye, UpgradeBranch.Mobility, "ตาไว", "ตาไว", "ไอเทมพิเศษโผล่ถี่ขึ้น 30%"),
        };

        static UpgradeDef Def(UpgradeType type, UpgradeBranch branch, string name, string shortName, string description)
            => new UpgradeDef { type = type, branch = branch, name = name, shortName = shortName, description = description };

        public static UpgradeDef Get(UpgradeType type) => All[(int)type];

        public static string BranchName(UpgradeBranch branch)
        {
            switch (branch)
            {
                case UpgradeBranch.Attack: return "โจมตี";
                case UpgradeBranch.Defense: return "ป้องกัน";
                default: return "คล่องตัว";
            }
        }

        /// <summary>One random card per branch, skipping cards already at max stacks.</summary>
        public static UpgradeType[] Deal(PlayerUpgrades owned)
        {
            var hand = new UpgradeType[HandSize];
            var pool = new List<UpgradeType>();
            for (int b = 0; b < HandSize; b++)
            {
                pool.Clear();
                foreach (var def in All)
                    if ((int)def.branch == b && owned.Count(def.type) < MaxStacks) pool.Add(def.type);
                hand[b] = pool[Random.Range(0, pool.Count)];
            }
            return hand;
        }
    }

    /// <summary>Upgrades one player has bought this match, and the stats they add up to.</summary>
    public class PlayerUpgrades
    {
        readonly int[] counts = new int[UpgradeCatalog.All.Length];

        public int Purchased { get; private set; }

        public int Count(UpgradeType type) => counts[(int)type];

        public void Add(UpgradeType type)
        {
            counts[(int)type]++;
            Purchased++;
        }

        public void Reset()
        {
            System.Array.Clear(counts, 0, counts.Length);
            Purchased = 0;
        }

        public float DamageMultiplier => 1f + 0.15f * Count(UpgradeType.SharpWarhead);
        public float CooldownMultiplier => Mathf.Pow(0.8f, Count(UpgradeType.QuickRail));
        public int RobotDropBonus => Count(UpgradeType.RobotHunter);
        public float RecycleChance => 0.25f * Count(UpgradeType.Recycler);
        public float RepairPerSecond => 2f * Count(UpgradeType.AutoRepair);
        /// <summary>Seconds between free shields; 0 when the card is not owned.</summary>
        public float ShieldInterval => Count(UpgradeType.EmergencyShield) > 0 ? 60f / Count(UpgradeType.EmergencyShield) : 0f;
        public float RepairMultiplier => 1f + 0.5f * Count(UpgradeType.Craftsman);
        public float SpeedMultiplier => 1f + 0.15f * Count(UpgradeType.TurboShoes);
        public float PickupRadiusMultiplier => 1f + 0.4f * Count(UpgradeType.LongArm);
        public float StunMultiplier => Mathf.Pow(0.6f, Count(UpgradeType.HardHead));
        public float PowerUpIntervalMultiplier => Mathf.Pow(0.7f, Count(UpgradeType.KeenEye));
    }
}
