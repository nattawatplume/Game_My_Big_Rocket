using System;
using System.Collections;
using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// Procedural music: tiny scores (chords + bass roots + melody) rendered once into seamless looping clips.
    /// Rendering is spread over frames (one bar per frame) so startup never stalls.
    /// All melodies are original.
    /// </summary>
    public static class MusicComposer
    {
        public enum Style { Gentle, Bouncy, Tense }

        public class Score
        {
            public string name;
            public float bpm;
            public int transpose;
            public Style style;
            /// <summary>Triad (MIDI) per bar.</summary>
            public int[][] chords;
            /// <summary>Bass root (MIDI) per bar.</summary>
            public int[] bass;
            /// <summary>Per bar: pairs of (midi, beats); midi &lt; 0 = rest. Each bar sums to 4 beats.</summary>
            public float[][] melody;

            public int Bars => chords.Length;
            public float Beat => 60f / bpm;
            public float Length => Bars * 4f * Beat;
        }

        // ---------------------------------------------------------------- chords (MIDI triads, around C4)
        static readonly int[] C = { 60, 64, 67 };
        static readonly int[] Am = { 57, 60, 64 };
        static readonly int[] F = { 57, 60, 65 };
        static readonly int[] G = { 55, 59, 62 };
        static readonly int[] Em = { 55, 59, 64 };
        static readonly int[] Dm = { 57, 62, 65 };
        static readonly int[] E = { 56, 59, 64 };

        /// <summary>Title / how-to-play: gentle, music-box-ish.</summary>
        public static Score Title => new Score
        {
            name = "music_title", bpm = 108f, style = Style.Gentle,
            chords = new[] { C, F, Am, G, C, F, G, C },
            bass = new[] { 48, 41, 45, 43, 48, 41, 43, 48 },
            melody = new[]
            {
                new float[] { 79, 1, 76, 1, 72, 1, 76, 1 },
                new float[] { 77, 1.5f, 81, 0.5f, 84, 2 },
                new float[] { 81, 1, 79, 0.5f, 76, 0.5f, 72, 2 },
                new float[] { 74, 1, 76, 0.5f, 77, 0.5f, 79, 1, -1, 1 },
                new float[] { 76, 1, 79, 1, 84, 1, 83, 0.5f, 81, 0.5f },
                new float[] { 81, 1.5f, 77, 0.5f, 72, 1, 77, 1 },
                new float[] { 79, 0.5f, 81, 0.5f, 79, 0.5f, 77, 0.5f, 76, 1, 74, 1 },
                new float[] { 72, 2, -1, 2 },
            }
        };

        static float[][] BattleMelody => new[]
        {
            new float[] { 76, 1, 79, 0.5f, 76, 0.5f, 72, 1, 74, 1 },
            new float[] { 76, 1.5f, 72, 0.5f, 69, 2 },
            new float[] { 77, 0.5f, 77, 0.5f, 81, 1, 79, 0.5f, 77, 0.5f, 76, 1 },
            new float[] { 74, 1, 67, 0.5f, 71, 0.5f, 74, 1, -1, 1 },
            new float[] { 79, 0.5f, 76, 0.5f, 79, 0.5f, 84, 1.5f, 83, 0.5f, 81, 0.5f },
            new float[] { 79, 1.5f, 76, 0.5f, 71, 1, 76, 1 },
            new float[] { 81, 0.5f, 79, 0.5f, 77, 0.5f, 81, 0.5f, 84, 1, 81, 1 },
            new float[] { 79, 2, 74, 0.5f, 76, 0.5f, 77, 0.5f, 74, 0.5f },
            new float[] { 72, 0.5f, 76, 0.5f, 81, 1, 79, 0.5f, 76, 0.5f, 72, 1 },
            new float[] { 77, 0.5f, 81, 0.5f, 84, 1, 81, 1, 77, 1 },
            new float[] { 74, 0.5f, 77, 0.5f, 81, 1, 79, 0.5f, 77, 0.5f, 76, 0.5f, 74, 0.5f },
            new float[] { 71, 0.5f, 74, 0.5f, 79, 1, 77, 0.5f, 74, 0.5f, 71, 0.5f, 74, 0.5f },
        };

        static int[][] BattleChords => new[] { C, Am, F, G, C, Em, F, G, Am, F, Dm, G };
        static int[] BattleBass => new[] { 48, 45, 41, 43, 48, 40, 41, 43, 45, 41, 38, 43 };

        /// <summary>Main island battle loop: bouncy ukulele + marimba, C major.</summary>
        public static Score Battle => new Score
        {
            name = "music_battle", bpm = 132f, style = Style.Bouncy,
            chords = BattleChords, bass = BattleBass, melody = BattleMelody
        };

        /// <summary>Same tune, faster and a whole step up, driving drums: last 30 seconds.</summary>
        public static Score Hurry => new Score
        {
            name = "music_hurry", bpm = 152f, transpose = 2, style = Style.Tense,
            chords = BattleChords, bass = BattleBass, melody = BattleMelody
        };

        /// <summary>Sudden death: A minor, fast, tense (but still toy-like).</summary>
        public static Score SuddenDeath => new Score
        {
            name = "music_sudden", bpm = 160f, style = Style.Tense,
            chords = new[] { Am, F, G, E, Am, F, G, E, Dm, Am, E, E },
            bass = new[] { 45, 41, 43, 40, 45, 41, 43, 40, 38, 45, 40, 40 },
            melody = new[]
            {
                new float[] { 81, 0.5f, 76, 0.5f, 81, 0.5f, 84, 0.5f, 83, 0.5f, 81, 0.5f, 76, 1 },
                new float[] { 77, 0.5f, 81, 0.5f, 84, 0.5f, 81, 0.5f, 77, 1, 72, 1 },
                new float[] { 79, 0.5f, 83, 0.5f, 86, 0.5f, 83, 0.5f, 79, 1, 74, 1 },
                new float[] { 76, 0.5f, 80, 0.5f, 83, 0.5f, 88, 0.5f, 86, 0.5f, 83, 0.5f, 80, 1 },
                new float[] { 81, 0.5f, 84, 0.5f, 88, 0.5f, 84, 0.5f, 81, 0.5f, 84, 0.5f, 83, 1 },
                new float[] { 81, 0.5f, 77, 0.5f, 81, 0.5f, 84, 0.5f, 81, 1, 77, 1 },
                new float[] { 83, 0.5f, 79, 0.5f, 83, 0.5f, 86, 0.5f, 84, 0.5f, 83, 0.5f, 79, 1 },
                new float[] { 80, 1, 83, 1, 88, 2 },
                new float[] { 86, 0.5f, 84, 0.5f, 81, 0.5f, 77, 0.5f, 74, 1, 77, 1 },
                new float[] { 76, 0.5f, 81, 0.5f, 84, 0.5f, 88, 0.5f, 84, 1, 81, 1 },
                new float[] { 83, 0.5f, 80, 0.5f, 76, 0.5f, 80, 0.5f, 83, 1, 86, 1 },
                new float[] { 88, 0.5f, 86, 0.5f, 83, 0.5f, 80, 0.5f, 76, 1, -1, 1 },
            }
        };

        static float Hz(float midi, int transpose) => SfxSynth.Midi(midi + transpose);

        /// <summary>Renders the score into a looping clip, one bar per frame. Calls done(clip) at the end.</summary>
        public static IEnumerator Render(Score score, Action<AudioClip> done)
        {
            float len = score.Length;
            float[] buf = new float[Mathf.CeilToInt(len * SfxSynth.SampleRate)];
            for (int bar = 0; bar < score.Bars; bar++)
            {
                RenderBar(score, bar, buf);
                yield return null;
            }
            var clip = SfxSynth.ToClip(score.name, buf, 0.8f);
            SfxSynth.Store(score.name, clip);
            done?.Invoke(clip);
        }

        static void RenderBar(Score s, int bar, float[] b)
        {
            const bool W = true; // wrap tails into the loop start
            float beat = s.Beat;
            float T = bar * 4f * beat;
            int tr = s.transpose;
            int[] chord = s.chords[bar];
            int root = s.bass[bar];

            // ---------------- drums
            switch (s.style)
            {
                case Style.Gentle:
                    SfxSynth.Kick(b, T, 0.22f, wrap: W);
                    for (int i = 0; i < 4; i++)
                        SfxSynth.Noise(b, T + (i + 0.5f) * beat, 0.07f, 0.05f, new Adsr(0.012f, 0.04f, 0f, 0.01f), 9000f, 9000f, 4500f, W);
                    break;
                case Style.Bouncy:
                    SfxSynth.Kick(b, T, 0.45f, wrap: W);
                    SfxSynth.Kick(b, T + 2f * beat, 0.4f, wrap: W);
                    for (int i = 0; i < 8; i++)
                    {
                        float v = (i % 2 == 1) ? 0.09f : 0.045f;
                        SfxSynth.Noise(b, T + i * 0.5f * beat, 0.045f, v, Adsr.Pluck(0.015f), 10000f, 10000f, 6000f, W);
                    }
                    Clap(b, T + beat, 0.16f);
                    Clap(b, T + 3f * beat, 0.16f);
                    break;
                case Style.Tense:
                    for (int i = 0; i < 4; i++) SfxSynth.Kick(b, T + i * beat, 0.42f, wrap: W);
                    for (int i = 0; i < 8; i++)
                    {
                        float v = (i % 2 == 1) ? 0.1f : 0.055f;
                        SfxSynth.Noise(b, T + i * 0.5f * beat, 0.04f, v, Adsr.Pluck(0.012f), 10000f, 10000f, 6500f, W);
                    }
                    Clap(b, T + beat, 0.2f);
                    Clap(b, T + 3f * beat, 0.2f);
                    break;
            }

            // ---------------- bass
            switch (s.style)
            {
                case Style.Gentle:
                    Bass(b, T, beat * 1.6f, Hz(root, tr), 0.32f);
                    Bass(b, T + 2f * beat, beat * 1.6f, Hz(root + 7, tr), 0.28f);
                    break;
                case Style.Bouncy:
                    Bass(b, T, beat * 0.9f, Hz(root, tr), 0.38f);
                    Bass(b, T + 1.5f * beat, beat * 0.4f, Hz(root + 12, tr), 0.26f);
                    Bass(b, T + 2f * beat, beat * 0.9f, Hz(root + 7, tr), 0.34f);
                    Bass(b, T + 3f * beat, beat * 0.4f, Hz(root, tr), 0.3f);
                    Bass(b, T + 3.5f * beat, beat * 0.4f, Hz(root + 12, tr), 0.26f);
                    break;
                case Style.Tense:
                    for (int i = 0; i < 8; i++)
                        Bass(b, T + i * 0.5f * beat, beat * 0.42f, Hz(root + (i % 2 == 1 ? 12 : 0), tr), i % 2 == 1 ? 0.26f : 0.34f);
                    break;
            }

            // ---------------- chords
            switch (s.style)
            {
                case Style.Gentle:
                {
                    int[] pat = { 0, 1, 2, 1, 0, 1, 2, 1 };
                    for (int i = 0; i < 8; i++)
                        SfxSynth.Pluck(b, T + i * 0.5f * beat, Hz(chord[pat[i]] + 12, tr), 0.1f, 0.35f, W);
                    break;
                }
                case Style.Bouncy:
                {
                    // island strum: D . D U . U D U
                    float[] at = { 0f, 1f, 1.5f, 2.5f, 3f, 3.5f };
                    bool[] down = { true, true, false, false, true, false };
                    float[] vol = { 0.13f, 0.1f, 0.07f, 0.08f, 0.1f, 0.07f };
                    for (int k = 0; k < at.Length; k++) Strum(b, T + at[k] * beat, chord, tr, down[k], vol[k], 0.28f);
                    break;
                }
                case Style.Tense:
                    for (int i = 0; i < 4; i++) Strum(b, T + (i + 0.5f) * beat, chord, tr, true, 0.08f, 0.12f);
                    break;
            }

            // ---------------- melody
            float[] mel = s.melody[bar];
            float pos = 0f;
            for (int i = 0; i + 1 < mel.Length; i += 2)
            {
                float note = mel[i], beats = mel[i + 1];
                if (note >= 0f)
                {
                    float t = T + pos * beat;
                    float f = Hz(note, tr);
                    float durSec = beats * beat;
                    switch (s.style)
                    {
                        case Style.Gentle:
                            SfxSynth.Bell(b, t, f, 0.22f, Mathf.Min(0.6f, 0.25f + durSec * 0.3f), W);
                            break;
                        case Style.Bouncy:
                            SfxSynth.Marimba(b, t, f, 0.3f, Mathf.Min(0.4f, 0.18f + durSec * 0.2f), W);
                            SfxSynth.Pluck(b, t, f, 0.07f, Mathf.Min(0.3f, 0.12f + durSec * 0.2f), W);
                            break;
                        case Style.Tense:
                            SfxSynth.Marimba(b, t, f, 0.28f, 0.2f, W);
                            SfxSynth.Tone(b, t, durSec * 0.85f, f, f, SynthWave.Square, 0.05f,
                                new Adsr(0.004f, 0.05f, 0.6f, 0.03f), duty: 0.3f, wrap: W);
                            break;
                    }
                }
                pos += beats;
            }
        }

        static void Bass(float[] b, float t, float dur, float f, float vol)
        {
            var env = new Adsr(0.005f, dur * 0.6f, 0.45f, Mathf.Min(0.05f, dur * 0.3f));
            SfxSynth.Tone(b, t, dur, f, f, SynthWave.Triangle, vol, env, wrap: true);
            SfxSynth.Tone(b, t, dur, f * 0.5f, f * 0.5f, SynthWave.Sine, vol * 0.5f, env, wrap: true);
        }

        static void Strum(float[] b, float t, int[] chord, int tr, bool down, float vol, float decay)
        {
            for (int i = 0; i < chord.Length; i++)
            {
                int k = down ? i : chord.Length - 1 - i;
                SfxSynth.Pluck(b, t + i * 0.012f, Hz(chord[k], tr), vol, decay, true);
            }
        }

        static void Clap(float[] b, float t, float vol)
        {
            SfxSynth.Noise(b, t, 0.11f, vol, Adsr.Pluck(0.035f), 3200f, 1800f, 700f, true);
            SfxSynth.Tone(b, t, 0.06f, 210f, 170f, SynthWave.Triangle, vol * 0.35f, Adsr.Pluck(0.025f), wrap: true);
        }
    }
}
