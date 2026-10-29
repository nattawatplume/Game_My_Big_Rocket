using UnityEngine;

namespace IslandRocketRumble.EditorTools
{
    public static partial class ArtGenerator
    {
        static readonly Color OutlineColor = new Color(0.22f, 0.23f, 0.32f, 1f);

        // Shared toon materials
        static Material mWhite, mEyeWhite, mCream, mInk, mBlush, mShine;
        static Material mPip, mMomo, mHelmetYellow, mHelmetOrange, mBandAid, mStar;
        static Material mIslandTop, mIslandSide, mLeaf, mLeafDark, mTrunk, mRock, mCraftRing;
        static Material mRoof, mDoor, mCrab, mDuck, mBeak, mGullGrey;
        static Material mRobotBlue, mRobotYellow, mTire, mMetal;
        static Material mRed, mCopper, mCopperDark, mSilver, mSilverBlue, mGold, mGoldCream, mCrystal, mCrystalLight, mLavender;
        // Transparent / special
        static Material mBeam, mRing, mScorch, mSea;

        static void CreateMaterials()
        {
            mWhite = Toon("IRR_Toon", Color.white);                       // tintable default
            mEyeWhite = Toon("IRR_Toon_EyeWhite", Color.white, 1.2f);
            mCream = Toon("IRR_Toon_Cream", Palette.Cream);
            mInk = Toon("IRR_Toon_Ink", Palette.Ink, 0f);
            mBlush = Toon("IRR_Toon_Blush", Palette.Hex("FF9DB4"), 0f);
            mShine = Toon("IRR_Toon_Shine", Color.white, 0f);

            mPip = Toon("IRR_Toon_PipBlue", Palette.PipBlue);
            mMomo = Toon("IRR_Toon_MomoPink", Palette.MomoPink);
            mHelmetYellow = Toon("IRR_Toon_HelmetYellow", Palette.HelmetYellow);
            mHelmetOrange = Toon("IRR_Toon_HelmetOrange", Palette.HelmetOrange);
            mBandAid = Toon("IRR_Toon_BandAid", Palette.Hex("F5C9A0"), 1f);
            mStar = Toon("IRR_Toon_Star", Palette.Hex("FFE15C"), 1f);

            mIslandTop = Toon("IRR_Toon_IslandTop", Palette.IslandTop);
            mIslandSide = Toon("IRR_Toon_IslandSide", Palette.IslandSide);
            mLeaf = Toon("IRR_Toon_Leaf", Palette.Leaf);
            mLeafDark = Toon("IRR_Toon_LeafDark", Palette.Hex("6FA84A"));
            mTrunk = Toon("IRR_Toon_Trunk", Palette.Trunk);
            mRock = Toon("IRR_Toon_Rock", Palette.Hex("B9B6C9"));
            mCraftRing = Toon("IRR_Toon_CraftRing", Palette.CraftRing, 0f);

            mRoof = Toon("IRR_Toon_Roof", Palette.CraftRing);
            mDoor = Toon("IRR_Toon_Door", Palette.Trunk);
            mCrab = Toon("IRR_Toon_Crab", Palette.Hex("FF7A5C"));
            mDuck = Toon("IRR_Toon_Duck", Palette.Hex("FFD84D"));
            mBeak = Toon("IRR_Toon_Beak", Palette.HelmetOrange);
            mGullGrey = Toon("IRR_Toon_GullGrey", Palette.Hex("9FA8B8"));

            mRobotBlue = Toon("IRR_Toon_RobotBlue", Palette.Hex("4A86E0"));
            mRobotYellow = Toon("IRR_Toon_RobotYellow", Palette.HelmetYellow);
            mTire = Toon("IRR_Toon_Tire", Palette.Hex("3A3F4D"));
            mMetal = Toon("IRR_Toon_Metal", Palette.Hex("A9B2BE"));

            mRed = Toon("IRR_Toon_Red", Palette.Danger);
            mCopper = Toon("IRR_Toon_Copper", Palette.Tier(PartTier.Copper));
            mCopperDark = Toon("IRR_Toon_CopperDark", Palette.Hex("C46B3A"));
            mSilver = Toon("IRR_Toon_Silver", Palette.Hex("D3DCE6"));
            mSilverBlue = Toon("IRR_Toon_SilverBlue", Palette.Tier(PartTier.Silver));
            mGold = Toon("IRR_Toon_Gold", Palette.Tier(PartTier.Gold));
            mGoldCream = Toon("IRR_Toon_GoldCream", Palette.Hex("FFF3D1"));
            mCrystal = Toon("IRR_Toon_Crystal", Palette.Tier(PartTier.Crystal));
            mCrystalLight = Toon("IRR_Toon_CrystalLight", Palette.Hex("FFC4F0"));
            mLavender = Toon("IRR_Toon_Lavender", Palette.Hex("D9CCFF"));

            mBeam = Unlit("IRR_Beam", new Color(1f, 1f, 1f, 0.55f), true);
            mRing = Unlit("IRR_Ring", new Color(1f, 1f, 1f, 0.75f), false);
            var scorch = Palette.Hex("3B3436");
            scorch.a = 0.75f;
            mScorch = Unlit("IRR_Scorch", scorch, false);

            mSea = GetMaterial("IRR_Sea", seaShader);
            SetColor(mSea, "_BaseColor", Palette.Sea);
            SetColor(mSea, "_DeepColor", Palette.SeaDeep);
            SetColor(mSea, "_FoamColor", Palette.Hex("EEFFF8"));
            UnityEditor.EditorUtility.SetDirty(mSea);
        }
    }
}
