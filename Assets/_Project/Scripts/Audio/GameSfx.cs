using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// Every sound effect in the game, designed with SfxSynth. Each getter builds once and is cached.
    /// </summary>
    public static class GameSfx
    {
        static float M(float note) => SfxSynth.Midi(note);

        // ------------------------------------------------------------ UI / craft menu

        public static AudioClip UiMove => SfxSynth.Get("ui_move", () =>
        {
            var b = SfxSynth.Buffer(0.09f);
            SfxSynth.Tone(b, 0f, 0.07f, 1046f, 1175f, SynthWave.Square, 0.5f, new Adsr(0.003f, 0.03f, 0.5f, 0.03f), duty: 0.3f);
            SfxSynth.Tone(b, 0f, 0.07f, 2092f, 2350f, SynthWave.Sine, 0.15f, Adsr.Pluck(0.03f));
            return b;
        });

        public static AudioClip UiTarget => SfxSynth.Get("ui_target", () =>
        {
            var b = SfxSynth.Buffer(0.14f);
            SfxSynth.Tone(b, 0f, 0.06f, 740f, 740f, SynthWave.Triangle, 0.6f, Adsr.Blip);
            SfxSynth.Tone(b, 0.055f, 0.07f, 988f, 988f, SynthWave.Triangle, 0.6f, Adsr.Blip);
            return b;
        });

        public static AudioClip MenuOpen => SfxSynth.Get("menu_open", () =>
        {
            var b = SfxSynth.Buffer(0.22f);
            SfxSynth.Tone(b, 0f, 0.07f, 520f, 700f, SynthWave.Square, 0.45f, new Adsr(0.002f, 0.04f, 0.4f, 0.025f), duty: 0.35f);
            SfxSynth.Tone(b, 0.07f, 0.12f, 900f, 1050f, SynthWave.Square, 0.45f, new Adsr(0.002f, 0.05f, 0.4f, 0.05f), duty: 0.35f, slideCurve: 0.4f);
            SfxSynth.Tone(b, 0.07f, 0.12f, 1800f, 2100f, SynthWave.Sine, 0.15f, Adsr.Pluck(0.05f));
            return b;
        });

        public static AudioClip MenuClose => SfxSynth.Get("menu_close", () =>
        {
            var b = SfxSynth.Buffer(0.22f);
            SfxSynth.Tone(b, 0f, 0.07f, 880f, 820f, SynthWave.Square, 0.4f, new Adsr(0.002f, 0.04f, 0.4f, 0.025f), duty: 0.35f);
            SfxSynth.Tone(b, 0.07f, 0.12f, 600f, 480f, SynthWave.Square, 0.4f, new Adsr(0.002f, 0.05f, 0.4f, 0.05f), duty: 0.35f);
            return b;
        });

        public static AudioClip CraftSuccess => SfxSynth.Get("craft_ok", () =>
        {
            var b = SfxSynth.Buffer(0.9f);
            float[] notes = { 84, 88, 91, 96, 100 }; // C6 E6 G6 C7 E7
            for (int i = 0; i < notes.Length; i++)
            {
                float t = i * 0.055f;
                SfxSynth.Bell(b, t, M(notes[i]), 0.35f, 0.25f + i * 0.05f);
                SfxSynth.Tone(b, t, 0.07f, M(notes[i]), M(notes[i]), SynthWave.Square, 0.12f, Adsr.Pluck(0.05f), duty: 0.25f);
            }
            // sparkle tail: little random tinkles
            var r = new System.Random(7);
            for (int i = 0; i < 7; i++)
            {
                float t = 0.3f + i * 0.06f + (float)r.NextDouble() * 0.03f;
                float f = 2600f + (float)r.NextDouble() * 2400f;
                SfxSynth.DecaySine(b, t, f, 0.12f * (1f - i / 8f), 0.05f);
            }
            return b;
        });

        public static AudioClip Denied => SfxSynth.Get("denied", () =>
        {
            var b = SfxSynth.Buffer(0.36f);
            SfxSynth.Tone(b, 0f, 0.12f, 240f, 170f, SynthWave.Square, 0.5f, new Adsr(0.002f, 0.06f, 0.3f, 0.04f), duty: 0.4f);
            SfxSynth.Tone(b, 0f, 0.12f, 120f, 85f, SynthWave.Sine, 0.4f, Adsr.Pluck(0.06f));
            SfxSynth.Tone(b, 0.15f, 0.16f, 200f, 140f, SynthWave.Square, 0.5f, new Adsr(0.002f, 0.07f, 0.3f, 0.06f), duty: 0.4f);
            SfxSynth.Tone(b, 0.15f, 0.16f, 100f, 70f, SynthWave.Sine, 0.4f, Adsr.Pluck(0.07f));
            return b;
        });

        /// <summary>Robot form change: rising servo whine into a heavy metal stamp.</summary>
        public static AudioClip RobotTransform => SfxSynth.Get("robot_transform", () =>
        {
            var b = SfxSynth.Buffer(1.1f);
            SfxSynth.Tone(b, 0f, 0.6f, 180f, 1200f, SynthWave.Square, 0.3f, new Adsr(0.02f, 0.1f, 0.7f, 0.05f), duty: 0.3f);
            SfxSynth.Tone(b, 0f, 0.6f, 90f, 600f, SynthWave.Triangle, 0.35f, new Adsr(0.02f, 0.1f, 0.7f, 0.05f));
            SfxSynth.Noise(b, 0.6f, 0.2f, 0.6f, Adsr.Pluck(0.12f), 5000f, 800f, 900f);
            SfxSynth.Tone(b, 0.6f, 0.4f, 140f, 45f, SynthWave.Sine, 0.8f, Adsr.Pluck(0.28f));
            SfxSynth.DecaySine(b, 0.62f, 1320f, 0.25f, 0.12f);
            SfxSynth.DecaySine(b, 0.7f, 1760f, 0.2f, 0.15f);
            return b;
        });

        /// <summary>Weak point opens: quick alarm ping-ping.</summary>
        public static AudioClip WeakPointOpen => SfxSynth.Get("weak_point", () =>
        {
            var b = SfxSynth.Buffer(0.4f);
            SfxSynth.Tone(b, 0f, 0.12f, 1319f, 1319f, SynthWave.Square, 0.4f, Adsr.Pluck(0.08f), duty: 0.3f);
            SfxSynth.Tone(b, 0.15f, 0.2f, 1760f, 1760f, SynthWave.Square, 0.4f, Adsr.Pluck(0.14f), duty: 0.3f);
            return b;
        });

        /// <summary>Power-up pickup: bright rising arpeggio.</summary>
        public static AudioClip PowerUp => SfxSynth.Get("power_up", () =>
        {
            var b = SfxSynth.Buffer(0.6f);
            float[] notes = { 76, 79, 84, 88 };
            for (int i = 0; i < notes.Length; i++)
                SfxSynth.Bell(b, i * 0.06f, M(notes[i]), 0.4f, 0.3f);
            SfxSynth.Tone(b, 0f, 0.25f, 400f, 1500f, SynthWave.Triangle, 0.25f, new Adsr(0.01f, 0.1f, 0.5f, 0.05f));
            return b;
        });

        /// <summary>Shield absorbs a rocket: glassy clang.</summary>
        public static AudioClip ShieldBlock => SfxSynth.Get("shield_block", () =>
        {
            var b = SfxSynth.Buffer(0.7f);
            SfxSynth.Noise(b, 0f, 0.1f, 0.5f, Adsr.Pluck(0.06f), 7000f, 3000f, 2500f);
            SfxSynth.DecaySine(b, 0f, 880f, 0.45f, 0.35f);
            SfxSynth.DecaySine(b, 0f, 1397f, 0.35f, 0.3f);
            SfxSynth.DecaySine(b, 0f, 2093f, 0.25f, 0.2f);
            return b;
        });

        /// <summary>Workshop piece snaps into place: quick ratchet clicks rising into a two-note metal "clink-clink".</summary>
        public static AudioClip BuildClink => SfxSynth.Get("build_clink", () =>
        {
            var b = SfxSynth.Buffer(0.42f);
            // ratchet: three tiny clicks climbing in pitch
            for (int i = 0; i < 3; i++)
            {
                float t = i * 0.028f;
                SfxSynth.Noise(b, t, 0.018f, 0.35f, Adsr.Pluck(0.006f), 6000f, 4000f, 1800f);
                SfxSynth.DecaySine(b, t, 1500f + 260f * i, 0.18f, 0.008f);
            }
            // two rising clinks (inharmonic partials = little metal plate)
            float[] notes = { 88, 95 }; // E6, B6
            float[] ratios = { 1f, 2.76f, 5.4f };
            for (int n = 0; n < notes.Length; n++)
            {
                float t = 0.09f + n * 0.075f;
                float f = M(notes[n]);
                for (int k = 0; k < ratios.Length; k++)
                    SfxSynth.DecaySine(b, t, f * ratios[k], (n == 1 ? 0.45f : 0.35f) / (1f + k * 1.2f), (n == 1 ? 0.09f : 0.05f) / (1f + k * 0.6f));
                SfxSynth.Tone(b, t, 0.04f, f, f, SynthWave.Square, 0.1f, Adsr.Pluck(0.025f), duty: 0.25f);
            }
            return b;
        });

        /// <summary>Workshop rocket fully built: sparkly rising arpeggio with a glittery tail.</summary>
        public static AudioClip ReadyChime => SfxSynth.Get("ready_chime", () =>
        {
            var b = SfxSynth.Buffer(1.0f);
            float[] notes = { 84, 88, 91, 96 }; // C6 E6 G6 C7
            for (int i = 0; i < notes.Length; i++)
            {
                float t = i * 0.07f;
                bool last = i == notes.Length - 1;
                SfxSynth.Bell(b, t, M(notes[i]), last ? 0.4f : 0.3f, last ? 0.5f : 0.2f);
                SfxSynth.Tone(b, t, last ? 0.3f : 0.06f, M(notes[i]), M(notes[i]), SynthWave.Triangle, 0.12f,
                    new Adsr(0.003f, 0.05f, 0.5f, last ? 0.2f : 0.03f), vibDepth: last ? 0.01f : 0f, vibRate: 7f);
            }
            var r = new System.Random(21);
            for (int i = 0; i < 8; i++)
            {
                float t = 0.25f + i * 0.055f + (float)r.NextDouble() * 0.02f;
                SfxSynth.DecaySine(b, t, 3000f + (float)r.NextDouble() * 2600f, 0.1f * (1f - i / 10f), 0.045f);
            }
            return b;
        });

        // ------------------------------------------------------------ rockets / impacts

        /// <summary>Whoosh: noise sweep + rising tone. Small rockets are short and high, T5 is long with rumble.</summary>
        public static AudioClip Launch(int tier)
        {
            tier = Mathf.Clamp(tier, 1, 5);
            return SfxSynth.Get("launch_" + tier, () =>
            {
                float len = 0.35f + 0.22f * (tier - 1) + (tier == 5 ? 0.5f : 0f);
                var b = SfxSynth.Buffer(len + 0.1f);
                // ignition "fwump"
                SfxSynth.Noise(b, 0f, 0.12f, 0.6f, Adsr.Pluck(0.04f), 900f, 300f);
                // the whoosh itself
                SfxSynth.Noise(b, 0.02f, len, 0.9f, new Adsr(len * 0.25f, len * 0.3f, 0.6f, len * 0.4f),
                    500f + 100f * tier, 3500f + 700f * tier, 150f);
                // rising toy whistle (smaller rockets = higher)
                float f0 = 520f - 60f * tier, f1 = f0 * (2.2f + 0.15f * tier);
                SfxSynth.Tone(b, 0.02f, len, f0, f1, SynthWave.Triangle, 0.28f, new Adsr(0.03f, len * 0.3f, 0.7f, len * 0.4f),
                    vibDepth: 0.015f, vibRate: 9f, slideCurve: 0.8f);
                if (tier >= 4)
                {
                    // rumble: deep filtered noise + wobbly low sine
                    SfxSynth.Noise(b, 0f, len, tier == 5 ? 0.9f : 0.5f, new Adsr(0.08f, len * 0.3f, 0.8f, len * 0.5f), 180f, 120f);
                    SfxSynth.Tone(b, 0f, len, 55f, 70f, SynthWave.Sine, tier == 5 ? 0.6f : 0.35f, new Adsr(0.05f, 0.2f, 0.8f, len * 0.5f),
                        vibDepth: 0.08f, vibRate: 11f);
                }
                if (tier == 5)
                {
                    // sparkly rainbow trail on top
                    for (int i = 0; i < 6; i++)
                        SfxSynth.Bell(b, 0.25f + i * 0.14f, M(84 + i * 2), 0.07f, 0.12f);
                }
                return b;
            });
        }

        /// <summary>Explosion: filtered noise burst + low thump. size 0..3.</summary>
        public static AudioClip Explosion(int size)
        {
            size = Mathf.Clamp(size, 0, 3);
            return SfxSynth.Get("boom_" + size, () =>
            {
                float len = 0.35f + 0.28f * size;
                var b = SfxSynth.Buffer(len + 0.05f);
                SfxSynth.Noise(b, 0f, len, 1f, new Adsr(0.002f, len * 0.85f, 0f, 0.02f), 2600f + 400f * size, 220f);
                SfxSynth.Noise(b, 0f, 0.05f, 0.4f, Adsr.Pluck(0.02f), 5000f, 3000f, 800f); // crack
                SfxSynth.Tone(b, 0f, 0.22f + 0.08f * size, 130f, 38f, SynthWave.Sine, 0.9f + 0.15f * size,
                    new Adsr(0.002f, 0.2f + 0.08f * size, 0f, 0.02f), slideCurve: 0.5f);
                if (size >= 2)
                {
                    // rolling after-rumble
                    SfxSynth.Noise(b, 0.1f, len - 0.1f, 0.5f, new Adsr(0.05f, len * 0.6f, 0f, 0.05f), 300f, 120f);
                    // a couple of cute debris "plinks"
                    SfxSynth.DecaySine(b, 0.25f, 1400f, 0.08f, 0.04f);
                    SfxSynth.DecaySine(b, 0.36f, 1700f, 0.06f, 0.04f);
                }
                SfxSynth.SoftClip(b, 1.1f);
                return b;
            });
        }

        /// <summary>Tiny "wa-iii!" voice. variant 0 = higher (Momo-ish), 1 = lower (Pip-ish).</summary>
        public static AudioClip Wah(int variant)
        {
            variant = Mathf.Clamp(variant, 0, 1);
            return SfxSynth.Get("wah_" + variant, () =>
            {
                var b = SfxSynth.Buffer(0.55f);
                float k = variant == 0 ? 1.12f : 1f;
                SfxSynth.Voice(b, 0f, 0.5f, 560f * k, 880f * k, 640f * k, 0.6f, 0.22f, 0.035f);
                return b;
            });
        }

        // ------------------------------------------------------------ parts / robot

        public static AudioClip PartPop(PartTier tier)
        {
            int t = Mathf.Clamp((int)tier, 1, 5);
            return SfxSynth.Get("part_" + t, () =>
            {
                var b = SfxSynth.Buffer(t == 5 ? 0.7f : 0.3f);
                float[] baseNotes = { 79, 81, 83, 86, 88 }; // G5 A5 B5 D6 E6
                float f = M(baseNotes[t - 1]);
                SfxSynth.Tone(b, 0f, 0.035f, f * 0.4f, f * 1.1f, SynthWave.Triangle, 0.5f, new Adsr(0.002f, 0.02f, 0.5f, 0.01f));
                SfxSynth.Tone(b, 0.035f, 0.16f, f * 1.5f, f * 1.5f, SynthWave.Square, 0.28f, Adsr.Pluck(0.07f), duty: 0.3f);
                SfxSynth.Bell(b, 0.035f, f * 1.5f, 0.3f, 0.12f);
                if (t == 5)
                {
                    var r = new System.Random(11);
                    for (int i = 0; i < 6; i++)
                    {
                        float tt = 0.12f + i * 0.07f;
                        SfxSynth.DecaySine(b, tt, M(96 + (i % 3) * 4) * (1f + (float)r.NextDouble() * 0.01f), 0.14f - i * 0.015f, 0.07f);
                    }
                }
                return b;
            });
        }

        public static AudioClip RobotHit => SfxSynth.Get("robot_hit", () =>
        {
            var b = SfxSynth.Buffer(0.75f);
            // metallic clank: inharmonic partials
            float f = 480f;
            float[] ratios = { 1f, 2.76f, 5.4f, 8.93f };
            float[] amps = { 0.6f, 0.4f, 0.25f, 0.12f };
            for (int i = 0; i < ratios.Length; i++)
                SfxSynth.DecaySine(b, 0f, f * ratios[i], amps[i], 0.12f / (1f + i * 0.5f));
            SfxSynth.Noise(b, 0f, 0.04f, 0.5f, Adsr.Pluck(0.015f), 7000f, 4000f, 1500f);
            // boing: springy wobble
            SfxSynth.Tone(b, 0.08f, 0.6f, 160f, 340f, SynthWave.Triangle, 0.55f, new Adsr(0.005f, 0.4f, 0.2f, 0.15f),
                vibDepth: 0.12f, vibRate: 13f, slideCurve: 0.5f);
            return b;
        });

        public static AudioClip RageUp(int level)
        {
            level = Mathf.Clamp(level, 1, 3);
            return SfxSynth.Get("rage_" + level, () =>
            {
                float len = 0.7f + 0.15f * level;
                var b = SfxSynth.Buffer(len + 0.05f);
                float lift = 1f + 0.18f * level;
                // angry grumble: low soft square with a fast flutter
                SfxSynth.Tone(b, 0f, len, 80f * lift, 150f * lift, SynthWave.Square, 0.5f, new Adsr(0.04f, len * 0.4f, 0.7f, 0.2f),
                    duty: 0.35f, vibDepth: 0.09f, vibRate: 17f, slideCurve: 1.6f);
                SfxSynth.Tone(b, 0f, len, 160f * lift, 300f * lift, SynthWave.Triangle, 0.25f, new Adsr(0.04f, len * 0.4f, 0.7f, 0.2f),
                    vibDepth: 0.09f, vibRate: 17f, slideCurve: 1.6f);
                // sizzle rising
                SfxSynth.Noise(b, len * 0.2f, len * 0.8f, 0.25f + 0.08f * level, new Adsr(len * 0.4f, 0.1f, 0.8f, 0.15f),
                    2500f, 9000f, 2500f);
                return b;
            });
        }

        public static AudioClip GemWarning => SfxSynth.Get("gem_warn", () =>
        {
            var b = SfxSynth.Buffer(0.34f);
            SfxSynth.Tone(b, 0f, 0.13f, 1175f, 1175f, SynthWave.Square, 0.45f, new Adsr(0.004f, 0.04f, 0.6f, 0.04f), duty: 0.4f);
            SfxSynth.Tone(b, 0.15f, 0.17f, 880f, 880f, SynthWave.Square, 0.45f, new Adsr(0.004f, 0.04f, 0.6f, 0.07f), duty: 0.4f);
            SfxSynth.Tone(b, 0f, 0.13f, 2350f, 2350f, SynthWave.Sine, 0.1f, Adsr.Blip);
            return b;
        });

        public static AudioClip Stunned => SfxSynth.Get("stun", () =>
        {
            var b = SfxSynth.Buffer(1.05f);
            SfxSynth.Tone(b, 0f, 0.6f, 950f, 300f, SynthWave.Triangle, 0.55f, new Adsr(0.005f, 0.2f, 0.7f, 0.15f),
                vibDepth: 0.07f, vibRate: 10f);
            SfxSynth.Tone(b, 0f, 0.6f, 1900f, 600f, SynthWave.Sine, 0.12f, new Adsr(0.005f, 0.2f, 0.7f, 0.15f),
                vibDepth: 0.07f, vibRate: 10f);
            // tweet-tweet (birdies around the head)
            for (int i = 0; i < 2; i++)
            {
                float t = 0.62f + i * 0.17f;
                SfxSynth.Tone(b, t, 0.06f, 2400f, 3300f, SynthWave.Sine, 0.35f, new Adsr(0.004f, 0.03f, 0.5f, 0.02f));
                SfxSynth.Tone(b, t + 0.065f, 0.05f, 3300f, 2800f, SynthWave.Sine, 0.25f, new Adsr(0.004f, 0.02f, 0.5f, 0.02f));
            }
            return b;
        });

        // ------------------------------------------------------------ match flow

        public static AudioClip TierUp => SfxSynth.Get("tier_up", () =>
        {
            var b = SfxSynth.Buffer(1.0f);
            float[] notes = { 79, 84, 88 }; // G5 C6 E6
            for (int i = 0; i < 3; i++)
            {
                float t = i * 0.12f;
                bool last = i == 2;
                SfxSynth.Bell(b, t, M(notes[i]), 0.4f, last ? 0.5f : 0.22f);
                SfxSynth.Tone(b, t, last ? 0.5f : 0.11f, M(notes[i]), M(notes[i]), SynthWave.Square, 0.12f,
                    new Adsr(0.005f, 0.08f, 0.5f, last ? 0.3f : 0.04f), duty: 0.3f, vibDepth: last ? 0.01f : 0f, vibRate: 6f);
            }
            SfxSynth.Bell(b, 0.24f, M(76), 0.18f, 0.45f); // harmony under the last note
            return b;
        });

        public static AudioClip CountBeep => SfxSynth.Get("count_beep", () =>
        {
            var b = SfxSynth.Buffer(0.2f);
            SfxSynth.Tone(b, 0f, 0.16f, 660f, 660f, SynthWave.Square, 0.45f, new Adsr(0.004f, 0.05f, 0.6f, 0.06f), duty: 0.4f);
            SfxSynth.Bell(b, 0f, 660f, 0.3f, 0.1f);
            return b;
        });

        public static AudioClip CountGo => SfxSynth.Get("count_go", () =>
        {
            var b = SfxSynth.Buffer(0.6f);
            SfxSynth.Tone(b, 0f, 0.5f, 1320f, 1320f, SynthWave.Square, 0.35f, new Adsr(0.004f, 0.1f, 0.6f, 0.25f), duty: 0.4f,
                vibDepth: 0.012f, vibRate: 7f);
            SfxSynth.Tone(b, 0f, 0.5f, 1980f, 1980f, SynthWave.Triangle, 0.25f, new Adsr(0.004f, 0.1f, 0.6f, 0.25f));
            SfxSynth.Bell(b, 0f, 1320f, 0.35f, 0.25f);
            SfxSynth.Kick(b, 0f, 0.5f);
            return b;
        });

        public static AudioClip Tick => SfxSynth.Get("tick", () =>
        {
            var b = SfxSynth.Buffer(0.08f);
            SfxSynth.DecaySine(b, 0f, 1250f, 0.6f, 0.018f, 0.0005f);
            SfxSynth.DecaySine(b, 0f, 2600f, 0.2f, 0.008f, 0.0005f);
            return b;
        });

        public static AudioClip Whistle => SfxSynth.Get("whistle", () =>
        {
            var b = SfxSynth.Buffer(0.95f);
            SfxSynth.Tone(b, 0f, 0.15f, 2650f, 2700f, SynthWave.Sine, 0.5f, new Adsr(0.01f, 0.03f, 0.9f, 0.03f), vibDepth: 0.025f, vibRate: 32f);
            SfxSynth.Tone(b, 0.2f, 0.7f, 2700f, 2650f, SynthWave.Sine, 0.5f, new Adsr(0.01f, 0.05f, 0.9f, 0.12f), vibDepth: 0.025f, vibRate: 32f);
            SfxSynth.Noise(b, 0.2f, 0.7f, 0.08f, new Adsr(0.01f, 0.05f, 0.9f, 0.12f), 5000f, 5000f, 2000f);
            return b;
        });

        /// <summary>Little victory tune: quick rising run, then a bright held chord.</summary>
        public static AudioClip Victory => SfxSynth.Get("victory", () =>
        {
            var b = SfxSynth.Buffer(2.6f);
            float step = 0.11f;
            float[] run = { 72, 76, 79, 84, 79, 84, 88 }; // C5 E5 G5 C6 G5 C6 E6
            for (int i = 0; i < run.Length; i++)
            {
                float t = i * step;
                SfxSynth.Marimba(b, t, M(run[i]), 0.45f, 0.25f);
                SfxSynth.Tone(b, t, step * 0.9f, M(run[i]), M(run[i]), SynthWave.Square, 0.1f, Adsr.Pluck(0.06f), duty: 0.3f);
            }
            float t2 = run.Length * step + 0.05f;
            // "ta-da": two pickup notes then the big chord
            SfxSynth.Marimba(b, t2, M(86), 0.4f, 0.2f);
            SfxSynth.Marimba(b, t2 + 0.14f, M(88), 0.4f, 0.2f);
            float tc = t2 + 0.3f;
            float[] chord = { 72, 76, 79, 84, 91 };
            for (int i = 0; i < chord.Length; i++)
            {
                SfxSynth.Bell(b, tc + i * 0.015f, M(chord[i]), 0.22f, 0.8f);
                SfxSynth.Tone(b, tc, 1.2f, M(chord[i]), M(chord[i]), SynthWave.Triangle, 0.08f, new Adsr(0.01f, 0.3f, 0.6f, 0.6f),
                    vibDepth: 0.006f, vibRate: 5.5f);
            }
            SfxSynth.Kick(b, tc, 0.5f);
            SfxSynth.Noise(b, tc, 1.0f, 0.15f, new Adsr(0.002f, 0.9f, 0f, 0.05f), 9000f, 5000f, 4000f); // soft cymbal
            var r = new System.Random(3);
            for (int i = 0; i < 8; i++)
                SfxSynth.DecaySine(b, tc + 0.1f + i * 0.09f, 2800f + (float)r.NextDouble() * 2500f, 0.08f, 0.05f);
            return b;
        });

        /// <summary>Sudden death sting: accelerating snare roll + crash + low hit.</summary>
        public static AudioClip DrumRoll => SfxSynth.Get("drum_roll", () =>
        {
            var b = SfxSynth.Buffer(2.4f);
            float t = 0f, gap = 0.09f;
            int i = 0;
            while (t < 1.2f)
            {
                float x = t / 1.2f;
                float v = 0.15f + 0.45f * x;
                SfxSynth.Noise(b, t, 0.07f, v, Adsr.Pluck(0.025f), 4500f, 2500f, 700f);
                SfxSynth.Tone(b, t, 0.05f, 220f, 180f, SynthWave.Triangle, v * 0.4f, Adsr.Pluck(0.02f));
                t += gap;
                gap = Mathf.Max(0.035f, gap * 0.93f);
                i++;
            }
            float tc = 1.25f;
            SfxSynth.Kick(b, tc, 1f, 110f, 40f, 0.4f);
            SfxSynth.Tone(b, tc, 0.5f, 110f, 90f, SynthWave.Triangle, 0.4f, Adsr.Pluck(0.2f));
            SfxSynth.Noise(b, tc, 1.1f, 0.5f, new Adsr(0.002f, 1.0f, 0f, 0.05f), 8000f, 3000f, 1500f);
            // dramatic minor stab
            float[] stab = { 57, 60, 64, 69 };
            foreach (var n in stab) SfxSynth.Tone(b, tc, 0.7f, M(n), M(n), SynthWave.Square, 0.1f, new Adsr(0.005f, 0.2f, 0.4f, 0.4f), duty: 0.35f);
            return b;
        });

        /// <summary>Builds every effect once (call over several frames to avoid first-use hitches).</summary>
        public static System.Collections.IEnumerator Prewarm()
        {
            AudioClip c;
            c = UiMove; c = UiTarget; c = MenuOpen; c = MenuClose; yield return null;
            c = CraftSuccess; c = Denied; yield return null;
            c = BuildClink; c = ReadyChime; yield return null;
            for (int t = 1; t <= 5; t++) { c = Launch(t); yield return null; }
            for (int s = 0; s <= 3; s++) { c = Explosion(s); yield return null; }
            c = Wah(0); c = Wah(1); yield return null;
            for (int t = 1; t <= 5; t++) c = PartPop((PartTier)t);
            yield return null;
            c = RobotHit; c = RageUp(1); c = RageUp(2); c = RageUp(3); yield return null;
            c = GemWarning; c = Stunned; c = TierUp; yield return null;
            c = CountBeep; c = CountGo; c = Tick; c = Whistle; yield return null;
            c = Victory; yield return null;
            c = DrumRoll;
            if (c == null) yield break;
        }
    }
}
