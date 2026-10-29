using System;
using System.Collections.Generic;
using UnityEngine;

namespace IslandRocketRumble
{
    public enum SynthWave { Sine, Square, Triangle, Saw, Noise }

    /// <summary>Attack / decay / sustain / release envelope (times in seconds, sustain = level 0..1).</summary>
    public struct Adsr
    {
        public float attack, decay, sustain, release;

        public Adsr(float attack, float decay, float sustain, float release)
        {
            this.attack = attack; this.decay = decay; this.sustain = sustain; this.release = release;
        }

        /// <summary>Short plucky shape: instant attack, exponential-ish fall to silence.</summary>
        public static Adsr Pluck(float decay) => new Adsr(0.003f, decay, 0f, 0.01f);
        /// <summary>Soft blip that holds a little and fades out at the end of the note.</summary>
        public static Adsr Blip => new Adsr(0.004f, 0.05f, 0.6f, 0.04f);
        public static Adsr Pad(float attack, float release) => new Adsr(attack, 0.05f, 1f, release);

        /// <summary>Envelope level at time t of a note that lasts dur seconds (release ends at dur).</summary>
        public float At(float t, float dur)
        {
            if (t < 0f || t >= dur) return 0f;
            float v;
            if (attack > 0f && t < attack) v = t / attack;
            else
            {
                float td = t - attack;
                if (decay > 0f && td < decay)
                {
                    float x = td / decay;
                    // exponential-looking fall that still reaches sustain at x = 1
                    float k = (Mathf.Exp(-4f * x) - 0.0183f) / (1f - 0.0183f);
                    v = sustain + (1f - sustain) * k;
                }
                else v = sustain;
            }
            if (release > 0f)
            {
                float relStart = dur - release;
                if (t > relStart) v *= Mathf.Clamp01((dur - t) / release);
            }
            return v;
        }
    }

    /// <summary>
    /// Tiny runtime synthesizer. Everything the game hears is generated here as float samples
    /// (44.1 kHz mono) and turned into cached AudioClips. Tone goal: soft, toy-like, pastel.
    /// All primitives ADD into a buffer, so sounds are built by layering calls.
    /// Pass wrap = true to fold tails past the end back to the start (seamless music loops).
    /// </summary>
    public static class SfxSynth
    {
        public const int SampleRate = 44100;
        const float TwoPi = Mathf.PI * 2f;

        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        static System.Random rng = new System.Random(1234);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            cache.Clear();
            rng = new System.Random(1234);
        }

        // ------------------------------------------------------------ cache / clip helpers

        /// <summary>Returns the cached clip for key, building it on first use.</summary>
        public static AudioClip Get(string key, Func<float[]> build, float peak = 0.8f)
        {
            if (cache.TryGetValue(key, out var clip) && clip != null) return clip;
            float[] data = build();
            clip = ToClip(key, data, peak);
            cache[key] = clip;
            return clip;
        }

        public static bool IsCached(string key) => cache.TryGetValue(key, out var c) && c != null;

        public static void Store(string key, AudioClip clip) => cache[key] = clip;

        public static float[] Buffer(float seconds) => new float[Mathf.Max(1, Mathf.CeilToInt(seconds * SampleRate))];

        public static AudioClip ToClip(string name, float[] data, float peak = 0.8f)
        {
            Normalize(data, peak);
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Scales so the loudest sample is at peak. Also removes DC offset and adds 2 ms edge fades.</summary>
        public static void Normalize(float[] data, float peak = 0.8f)
        {
            if (data.Length == 0) return;
            double sum = 0;
            for (int i = 0; i < data.Length; i++) sum += data[i];
            float dc = (float)(sum / data.Length);
            float max = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] -= dc;
                float a = data[i] < 0 ? -data[i] : data[i];
                if (a > max) max = a;
            }
            if (max < 1e-6f) return;
            float g = peak / max;
            for (int i = 0; i < data.Length; i++) data[i] *= g;
        }

        public static float Midi(float note) => 440f * Mathf.Pow(2f, (note - 69f) / 12f);

        // ------------------------------------------------------------ oscillators

        /// <summary>One oscillator sample. phase in cycles [0,1). duty only affects Square (0.5 = even).</summary>
        public static float Osc(SynthWave wave, double phase, float duty = 0.5f)
        {
            float p = (float)(phase - Math.Floor(phase));
            switch (wave)
            {
                case SynthWave.Sine: return Mathf.Sin(TwoPi * p);
                case SynthWave.Triangle: return 4f * Mathf.Abs(p - 0.5f) - 1f;
                case SynthWave.Saw:
                    // soft saw: blend toward triangle to round the edge
                    return 0.7f * (2f * p - 1f) + 0.3f * (4f * Mathf.Abs(p - 0.5f) - 1f);
                case SynthWave.Square:
                {
                    // soft-edged pulse: high while frac(p) < duty, edges rounded with tanh
                    float c = Mathf.Cos(TwoPi * (p - duty * 0.5f)) - Mathf.Cos(Mathf.PI * duty);
                    return Tanh(c * 5f);
                }
                case SynthWave.Noise: return (float)(rng.NextDouble() * 2.0 - 1.0);
            }
            return 0f;
        }

        public static float Tanh(float x)
        {
            if (x > 9f) return 1f;
            if (x < -9f) return -1f;
            float e = Mathf.Exp(2f * x);
            return (e - 1f) / (e + 1f);
        }

        static bool Index(ref int idx, int len, bool wrap)
        {
            if (idx < 0) return false;
            if (idx >= len)
            {
                if (!wrap) return false;
                idx %= len;
            }
            return true;
        }

        // ------------------------------------------------------------ primitives

        /// <summary>
        /// Pitched tone with an exponential pitch slide f0 -> f1 (slideCurve &gt; 1 = slide late, &lt; 1 = slide early),
        /// optional vibrato (depth as fraction of pitch).
        /// </summary>
        public static void Tone(float[] buf, float start, float dur, float f0, float f1, SynthWave wave, float vol, Adsr env,
            float duty = 0.5f, float vibDepth = 0f, float vibRate = 6f, float slideCurve = 1f, bool wrap = false)
        {
            int s0 = Mathf.RoundToInt(start * SampleRate);
            int n = Mathf.CeilToInt(dur * SampleRate);
            float lnRatio = Mathf.Log(Mathf.Max(1f, f1) / Mathf.Max(1f, f0));
            bool slide = Mathf.Abs(lnRatio) > 1e-5f;
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                int idx = s0 + i;
                if (idx >= buf.Length && !wrap) break;
                float t = i / (float)SampleRate;
                float f = f0;
                if (slide)
                {
                    float x = t / dur;
                    if (slideCurve != 1f) x = Mathf.Pow(x, slideCurve);
                    f = f0 * Mathf.Exp(lnRatio * x);
                }
                if (vibDepth > 0f) f *= 1f + vibDepth * Mathf.Sin(TwoPi * vibRate * t);
                phase += f / SampleRate;
                if (phase > 1e6) phase -= Math.Floor(phase);
                if (!Index(ref idx, buf.Length, wrap)) continue;
                buf[idx] += Osc(wave, phase, duty) * env.At(t, dur) * vol;
            }
        }

        /// <summary>
        /// Filtered noise. Low-pass cutoff glides exponentially cut0 -> cut1 (2-pole), optional high-pass (Hz, 0 = off).
        /// </summary>
        public static void Noise(float[] buf, float start, float dur, float vol, Adsr env, float cut0, float cut1,
            float highPass = 0f, bool wrap = false)
        {
            int s0 = Mathf.RoundToInt(start * SampleRate);
            int n = Mathf.CeilToInt(dur * SampleRate);
            float lnRatio = Mathf.Log(Mathf.Max(20f, cut1) / Mathf.Max(20f, cut0));
            float lp1 = 0f, lp2 = 0f, hpState = 0f;
            float hpA = highPass > 0f ? 1f - Mathf.Exp(-TwoPi * highPass / SampleRate) : 0f;
            for (int i = 0; i < n; i++)
            {
                int idx = s0 + i;
                if (idx >= buf.Length && !wrap) break;
                float t = i / (float)SampleRate;
                float cut = cut0 * Mathf.Exp(lnRatio * (t / dur));
                float a = 1f - Mathf.Exp(-TwoPi * Mathf.Min(cut, 18000f) / SampleRate);
                float x = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp1 += a * (x - lp1);
                lp2 += a * (lp1 - lp2);
                float y = lp2;
                if (hpA > 0f)
                {
                    hpState += hpA * (y - hpState);
                    y -= hpState;
                }
                if (!Index(ref idx, buf.Length, wrap)) continue;
                buf[idx] += y * env.At(t, dur) * vol;
            }
        }

        /// <summary>Bell / chime: a few sine partials, upper ones fade faster.</summary>
        public static void Bell(float[] buf, float start, float freq, float vol, float decay = 0.9f, bool wrap = false)
        {
            float[] ratio = { 1f, 2f, 3.01f, 4.2f };
            float[] amp = { 1f, 0.45f, 0.2f, 0.1f };
            for (int k = 0; k < ratio.Length; k++)
                DecaySine(buf, start, freq * ratio[k], vol * amp[k], decay / (1f + k * 0.8f), 0.002f, wrap);
        }

        /// <summary>Marimba-ish: fundamental + 4th partial with a fast decay, short mallet click.</summary>
        public static void Marimba(float[] buf, float start, float freq, float vol, float decay = 0.35f, bool wrap = false)
        {
            DecaySine(buf, start, freq, vol, decay, 0.002f, wrap);
            DecaySine(buf, start, freq * 4f, vol * 0.22f, decay * 0.18f, 0.001f, wrap);
            DecaySine(buf, start, freq * 10f, vol * 0.05f, decay * 0.06f, 0.0005f, wrap);
        }

        /// <summary>Ukulele-ish pluck: decaying triangle with a touch of sine 2nd harmonic.</summary>
        public static void Pluck(float[] buf, float start, float freq, float vol, float decay = 0.3f, bool wrap = false)
        {
            int s0 = Mathf.RoundToInt(start * SampleRate);
            float len = decay * 4.5f;
            int n = Mathf.CeilToInt(len * SampleRate);
            float mul = Mathf.Exp(-1f / (decay * SampleRate));
            float mul2 = Mathf.Exp(-1f / (decay * 0.35f * SampleRate));
            float e = vol, e2 = vol * 0.5f;
            double ph = 0, inc = freq / (double)SampleRate;
            int attack = (int)(0.002f * SampleRate);
            for (int i = 0; i < n; i++)
            {
                int idx = s0 + i;
                if (idx >= buf.Length && !wrap) break;
                ph += inc; if (ph >= 1.0) ph -= 1.0;
                float p = (float)ph;
                float tri = 4f * Mathf.Abs(p - 0.5f) - 1f;
                float h2 = Mathf.Sin(TwoPi * 2f * p);
                float a = i < attack ? i / (float)attack : 1f;
                float fadeEnd = i > n - 200 ? (n - i) / 200f : 1f;
                float v = (tri * e + h2 * e2) * a * fadeEnd;
                e *= mul; e2 *= mul2;
                if (!Index(ref idx, buf.Length, wrap)) continue;
                buf[idx] += v;
            }
        }

        /// <summary>Sine with instant attack and exponential decay (time constant = decay seconds).</summary>
        public static void DecaySine(float[] buf, float start, float freq, float vol, float decay, float attack = 0.002f, bool wrap = false)
        {
            if (freq > SampleRate * 0.45f) return;
            int s0 = Mathf.RoundToInt(start * SampleRate);
            int n = Mathf.CeilToInt(decay * 5f * SampleRate);
            float mul = Mathf.Exp(-1f / (decay * SampleRate));
            float e = vol;
            // rotate a phasor instead of calling Sin per sample
            double w = TwoPi * freq / SampleRate;
            float c = (float)Math.Cos(w), s = (float)Math.Sin(w);
            float re = 1f, im = 0f;
            int att = Mathf.Max(1, (int)(attack * SampleRate));
            for (int i = 0; i < n; i++)
            {
                int idx = s0 + i;
                if (idx >= buf.Length && !wrap) break;
                float nre = re * c - im * s;
                im = re * s + im * c;
                re = nre;
                if ((i & 1023) == 0) { float m = 1f / Mathf.Sqrt(re * re + im * im); re *= m; im *= m; }
                float a = i < att ? i / (float)att : 1f;
                float fadeEnd = i > n - 200 ? (n - i) / 200f : 1f;
                float v = im * e * a * fadeEnd;
                e *= mul;
                if (!Index(ref idx, buf.Length, wrap)) continue;
                buf[idx] += v;
            }
        }

        /// <summary>Soft kick: sine with a fast downward pitch sweep.</summary>
        public static void Kick(float[] buf, float start, float vol, float fTop = 150f, float fBottom = 48f, float len = 0.16f, bool wrap = false)
        {
            Tone(buf, start, len, fTop, fBottom, SynthWave.Sine, vol, new Adsr(0.002f, len * 0.9f, 0f, 0.01f), slideCurve: 0.35f, wrap: wrap);
        }

        /// <summary>
        /// Cute formant "voice": a buzzy source shaped by two vowel formants that glide
        /// u -> a -> i (a little "wa-iii!"). f0 goes start -> peak -> end.
        /// </summary>
        public static void Voice(float[] buf, float start, float dur, float f0Start, float f0Peak, float f0End, float vol,
            float peakAt = 0.25f, float vibDepth = 0.03f)
        {
            int s0 = Mathf.RoundToInt(start * SampleRate);
            int n = Mathf.CeilToInt(dur * SampleRate);
            var env = new Adsr(0.015f, 0.1f, 0.75f, dur * 0.45f);
            double ph = 0;
            const int H = 10;
            for (int i = 0; i < n; i++)
            {
                int idx = s0 + i;
                if (idx >= buf.Length) break;
                float t = i / (float)SampleRate;
                float x = t / dur;
                float f0 = x < peakAt
                    ? Mathf.Lerp(f0Start, f0Peak, Mathf.SmoothStep(0f, 1f, x / peakAt))
                    : Mathf.Lerp(f0Peak, f0End, (x - peakAt) / (1f - peakAt));
                f0 *= 1f + vibDepth * Mathf.Sin(TwoPi * 7f * t) * Mathf.Clamp01(x * 3f);
                // vowel formants: u (350, 700) -> a (850, 1300) -> i (330, 2400)
                float F1, F2;
                if (x < 0.3f) { float k = x / 0.3f; F1 = Mathf.Lerp(350f, 850f, k); F2 = Mathf.Lerp(700f, 1300f, k); }
                else { float k = Mathf.Clamp01((x - 0.3f) / 0.5f); F1 = Mathf.Lerp(850f, 330f, k); F2 = Mathf.Lerp(1300f, 2400f, k); }
                ph += f0 / SampleRate;
                if (ph > 1e6) ph -= Math.Floor(ph);
                float p = (float)(ph - Math.Floor(ph));
                float sum = 0f;
                for (int h = 1; h <= H; h++)
                {
                    float fh = f0 * h;
                    if (fh > 5000f) break;
                    float d1 = (fh - F1) / 260f, d2 = (fh - F2) / 320f;
                    float a = Mathf.Exp(-d1 * d1) + 0.55f * Mathf.Exp(-d2 * d2) + 0.12f / h;
                    sum += a * Mathf.Sin(TwoPi * h * p);
                }
                buf[idx] += sum * env.At(t, dur) * vol;
            }
        }

        /// <summary>Applies a gentle tanh saturation (keeps sudden layered peaks round).</summary>
        public static void SoftClip(float[] buf, float drive = 1.2f)
        {
            for (int i = 0; i < buf.Length; i++) buf[i] = Tanh(buf[i] * drive);
        }
    }
}
