using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>Pastel palette from the concept board. Use these instead of hard-coded colors.</summary>
    public static class Palette
    {
        public static readonly Color Sea = Hex("A6DCCB");
        public static readonly Color SeaDeep = Hex("7FC7B4");
        public static readonly Color IslandTop = Hex("E6DC7A");
        public static readonly Color IslandSide = Hex("E58F80");
        public static readonly Color Cream = Hex("FFF6E3");
        public static readonly Color Ink = Hex("2E3A48");
        public static readonly Color CraftRing = Hex("EF7B5C");
        public static readonly Color Leaf = Hex("8DBF5A");
        public static readonly Color Trunk = Hex("A9714B");

        public static readonly Color PipBlue = Hex("3D9BE9");
        public static readonly Color MomoPink = Hex("FF79A8");
        public static readonly Color HelmetYellow = Hex("F7C531");
        public static readonly Color HelmetOrange = Hex("FF9F43");

        public static readonly Color HpP1 = Hex("6CC550");
        public static readonly Color HpP2 = Hex("FFA63D");
        public static readonly Color Danger = Hex("F2545B");

        public static Color Tier(PartTier tier) => tier switch
        {
            PartTier.Scrap => Hex("B5BCC4"),
            PartTier.Copper => Hex("E8955A"),
            PartTier.Silver => Hex("8EC8F2"),
            PartTier.Gold => Hex("F7C531"),
            PartTier.Crystal => Hex("FF7AD9"),
            _ => Color.white
        };

        public static Color Player(PlayerId id) => id == PlayerId.P1 ? PipBlue : MomoPink;

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
