using System;
using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>The four dress-up slots, in the order the customize panel lists them.</summary>
    public enum CosmeticSlot { Hat = 0, Color = 1, Face = 2, Back = 3 }

    /// <summary>One selectable option: Thai + English name, and a color for body-color options.</summary>
    public readonly struct CosmeticOption
    {
        public readonly string thai;
        public readonly string english;
        public readonly Color color;

        public CosmeticOption(string thai, string english) : this(thai, english, Color.white) { }

        public CosmeticOption(string thai, string english, Color color)
        {
            this.thai = thai;
            this.english = english;
            this.color = color;
        }
    }

    /// <summary>A character's outfit: an index into each <see cref="CosmeticCatalog"/> list.</summary>
    [Serializable]
    public struct PlayerLook : IEquatable<PlayerLook>
    {
        public int hat, color, face, back;

        public PlayerLook(int hat, int color, int face, int back)
        {
            this.hat = hat;
            this.color = color;
            this.face = face;
            this.back = back;
        }

        public int this[CosmeticSlot slot]
        {
            get => slot switch { CosmeticSlot.Hat => hat, CosmeticSlot.Color => color, CosmeticSlot.Face => face, _ => back };
            set
            {
                switch (slot)
                {
                    case CosmeticSlot.Hat: hat = value; break;
                    case CosmeticSlot.Color: color = value; break;
                    case CosmeticSlot.Face: face = value; break;
                    default: back = value; break;
                }
            }
        }

        public bool Equals(PlayerLook o) => hat == o.hat && color == o.color && face == o.face && back == o.back;
        public override bool Equals(object obj) => obj is PlayerLook o && Equals(o);
        public override int GetHashCode() => hat | (color << 8) | (face << 16) | (back << 24);
    }

    /// <summary>
    /// Every dress-up option. Indices match <c>CosmeticLibrary</c>'s prefab arrays (built by the art generator).
    /// Hat 0 keeps the character's own safety helmet; face/back 0 and the last hat mean "nothing".
    /// </summary>
    public static class CosmeticCatalog
    {
        /// <summary>Hat index that shows the character's built-in safety helmet.</summary>
        public const int HelmetHat = 0;

        public static readonly CosmeticOption[] Hats =
        {
            new CosmeticOption("หมวกนิรภัย", "Safety helmet"),
            new CosmeticOption("หมวกแก๊ป", "Baseball cap"),
            new CosmeticOption("มงกุฎ", "Crown"),
            new CosmeticOption("มงกุฎดอกไม้", "Flower crown"),
            new CosmeticOption("หูแมว", "Cat ears"),
            new CosmeticOption("หมวกปาร์ตี้", "Party hat"),
            new CosmeticOption("หมวกเชฟ", "Chef hat"),
            new CosmeticOption("หมวกกบ", "Frog hat"),
            new CosmeticOption("หูกระต่าย", "Bunny ears"),
            new CosmeticOption("ไม่ใส่", "None"),
        };

        public static readonly CosmeticOption[] Colors =
        {
            new CosmeticOption("ฟ้า", "Blue", Palette.PipBlue),
            new CosmeticOption("ชมพู", "Pink", Palette.MomoPink),
            new CosmeticOption("มิ้นต์", "Mint", Palette.Hex("6FD6B0")),
            new CosmeticOption("ลาเวนเดอร์", "Lavender", Palette.Hex("B293F5")),
            new CosmeticOption("เหลืองมะนาว", "Lemon", Palette.Hex("FFD95E")),
            new CosmeticOption("พีช", "Peach", Palette.Hex("FFA483")),
            new CosmeticOption("ฟ้าทะเล", "Aqua", Palette.Hex("5FD0E8")),
            new CosmeticOption("ขาวเมฆ", "Cloud", Palette.Hex("F1EEF6")),
        };

        public static readonly CosmeticOption[] Faces =
        {
            new CosmeticOption("ไม่มี", "None"),
            new CosmeticOption("แว่นกลม", "Round glasses"),
            new CosmeticOption("แว่นกันแดด", "Sunglasses"),
            new CosmeticOption("หนวด", "Mustache"),
            new CosmeticOption("สติกเกอร์หัวใจ", "Heart stickers"),
        };

        public static readonly CosmeticOption[] Backs =
        {
            new CosmeticOption("ไม่มี", "None"),
            new CosmeticOption("เจ็ตแพ็คจิ๋ว", "Mini jetpack"),
            new CosmeticOption("ผ้าคลุม", "Cape"),
            new CosmeticOption("กระเป๋าเป้", "Backpack"),
            new CosmeticOption("ปีกนางฟ้า", "Angel wings"),
        };

        /// <summary>Thai row labels for the customize panel.</summary>
        public static string SlotThai(CosmeticSlot slot) => slot switch
        {
            CosmeticSlot.Hat => "หมวก",
            CosmeticSlot.Color => "สีตัว",
            CosmeticSlot.Face => "หน้า",
            _ => "ของหลัง",
        };

        public static CosmeticOption[] Options(CosmeticSlot slot) => slot switch
        {
            CosmeticSlot.Hat => Hats,
            CosmeticSlot.Color => Colors,
            CosmeticSlot.Face => Faces,
            _ => Backs,
        };

        public static int Count(CosmeticSlot slot) => Options(slot).Length;

        public static CosmeticOption Get(CosmeticSlot slot, int index)
        {
            var list = Options(slot);
            return list[Mathf.Clamp(index, 0, list.Length - 1)];
        }

        /// <summary>Today's look: own safety helmet, P1 blue body, P2 pink body, nothing else.</summary>
        public static PlayerLook Default(PlayerId id) => new PlayerLook(HelmetHat, id == PlayerId.P1 ? 0 : 1, 0, 0);

        public static Color BodyColor(int index) => Get(CosmeticSlot.Color, index).color;

        /// <summary>Wraps every index into range.</summary>
        public static PlayerLook Clamp(PlayerLook look)
        {
            for (int s = 0; s < 4; s++)
            {
                var slot = (CosmeticSlot)s;
                int n = Count(slot);
                look[slot] = ((look[slot] % n) + n) % n;
            }
            return look;
        }
    }

    /// <summary>
    /// Current outfit per player, persisted in PlayerPrefs ("IRR_Look_P1" / "IRR_Look_P2" as "hat,color,face,back").
    /// Also carries the customize-screen preview flag that makes the characters float up for a closer look.
    /// </summary>
    public static class CosmeticsStore
    {
        /// <summary>Raised after a player's look changed.</summary>
        public static event Action<PlayerId> Changed;
        /// <summary>Raised when <see cref="PreviewMode"/> flips.</summary>
        public static event Action<bool> PreviewModeChanged;

        static readonly PlayerLook[] looks = new PlayerLook[2];
        static readonly bool[] loaded = new bool[2];
        static bool previewMode;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Changed = null;
            PreviewModeChanged = null;
            loaded[0] = loaded[1] = false;
            previewMode = false;
        }

        static string Key(PlayerId id) => id == PlayerId.P1 ? "IRR_Look_P1" : "IRR_Look_P2";

        /// <summary>True while the customize screen shows the characters up close.</summary>
        public static bool PreviewMode
        {
            get => previewMode;
            set
            {
                if (previewMode == value) return;
                previewMode = value;
                PreviewModeChanged?.Invoke(value);
            }
        }

        public static PlayerLook Get(PlayerId id)
        {
            int i = (int)id;
            if (!loaded[i])
            {
                looks[i] = Load(id);
                loaded[i] = true;
            }
            return looks[i];
        }

        /// <summary>Sets (and stores in PlayerPrefs) a player's look; call <see cref="Save"/> to flush to disk.</summary>
        public static void Set(PlayerId id, PlayerLook look)
        {
            look = CosmeticCatalog.Clamp(look);
            bool same = Get(id).Equals(look);
            looks[(int)id] = look;
            if (same) return;
            PlayerPrefs.SetString(Key(id), look.hat + "," + look.color + "," + look.face + "," + look.back);
            Changed?.Invoke(id);
        }

        public static void Save() => PlayerPrefs.Save();

        static PlayerLook Load(PlayerId id)
        {
            var look = CosmeticCatalog.Default(id);
            string s = PlayerPrefs.GetString(Key(id), "");
            var parts = s.Split(',');
            if (parts.Length == 4 &&
                int.TryParse(parts[0], out int h) && int.TryParse(parts[1], out int c) &&
                int.TryParse(parts[2], out int f) && int.TryParse(parts[3], out int b))
                look = CosmeticCatalog.Clamp(new PlayerLook(h, c, f, b));
            return look;
        }
    }
}
