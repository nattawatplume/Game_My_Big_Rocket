using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// Player-facing settings saved in PlayerPrefs: volumes, fullscreen, screen shake, match length and island HP.
    /// Match length and island HP are copied into the runtime GameConfig when a match starts.
    /// </summary>
    public static class GameSettings
    {
        public static readonly int[] MatchMinutes = { 3, 4, 5, 7 };
        public static readonly int[] IslandHpOptions = { 600, 1000, 1500 };

        const string Prefix = "IRR_Set_";
        const float BaseMusic = 0.35f;
        const float BaseSfx = 0.8f;

        static bool loaded;
        static int master = 8, music = 8, sfx = 8, matchIndex = 2, hpIndex = 1;
        static bool fullscreen = true, shake = true;

        /// <summary>True while the settings screen pauses the game.</summary>
        public static bool Paused { get; set; }

        public static int Master { get { Load(); return master; } set { Load(); master = Mathf.Clamp(value, 0, 10); Save(); ApplyAudio(); } }
        public static int Music { get { Load(); return music; } set { Load(); music = Mathf.Clamp(value, 0, 10); Save(); ApplyAudio(); } }
        public static int Sfx { get { Load(); return sfx; } set { Load(); sfx = Mathf.Clamp(value, 0, 10); Save(); ApplyAudio(); } }
        public static bool Fullscreen { get { Load(); return fullscreen; } set { Load(); fullscreen = value; Save(); ApplyScreen(); } }
        public static bool ScreenShake { get { Load(); return shake; } set { Load(); shake = value; Save(); } }

        public static int MatchIndex
        {
            get { Load(); return matchIndex; }
            set { Load(); matchIndex = (value + MatchMinutes.Length) % MatchMinutes.Length; Save(); }
        }

        public static int HpIndex
        {
            get { Load(); return hpIndex; }
            set { Load(); hpIndex = (value + IslandHpOptions.Length) % IslandHpOptions.Length; Save(); }
        }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            master = PlayerPrefs.GetInt(Prefix + "Master", master);
            music = PlayerPrefs.GetInt(Prefix + "Music", music);
            sfx = PlayerPrefs.GetInt(Prefix + "Sfx", sfx);
            fullscreen = PlayerPrefs.GetInt(Prefix + "Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            shake = PlayerPrefs.GetInt(Prefix + "Shake", 1) == 1;
            matchIndex = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Match", matchIndex), 0, MatchMinutes.Length - 1);
            hpIndex = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Hp", hpIndex), 0, IslandHpOptions.Length - 1);
        }

        static void Save()
        {
            PlayerPrefs.SetInt(Prefix + "Master", master);
            PlayerPrefs.SetInt(Prefix + "Music", music);
            PlayerPrefs.SetInt(Prefix + "Sfx", sfx);
            PlayerPrefs.SetInt(Prefix + "Fullscreen", fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "Shake", shake ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "Match", matchIndex);
            PlayerPrefs.SetInt(Prefix + "Hp", hpIndex);
            PlayerPrefs.Save();
        }

        public static void ApplyAudio()
        {
            AudioListener.volume = Master / 10f;
            var audio = AudioManager.Instance;
            if (audio == null) return;
            audio.musicVolume = BaseMusic * Music / 10f;
            audio.sfxVolume = BaseSfx * Sfx / 10f;
        }

        static void ApplyScreen()
        {
#if !UNITY_EDITOR
            Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
#endif
        }

        /// <summary>Copies match length (tiers keep the same share of the match) and island HP into the runtime config.</summary>
        public static void ApplyToConfig(GameConfig cfg)
        {
            if (cfg == null) return;
            cfg.matchDuration = MatchMinutes[MatchIndex] * 60f;
            cfg.tierInterval = cfg.matchDuration * 0.15f;
            cfg.islandMaxHp = IslandHpOptions[HpIndex];
        }
    }
}
