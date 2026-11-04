using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UText = UnityEngine.UI.Text;

namespace IslandRocketRumble
{
    /// <summary>
    /// K.O. finisher played before the result screen: the camera closes in on the loser's island, a giant golden
    /// rocket drops from the sky, and the loser is blasted off into the sky until they twinkle away.
    /// Added by GameManager. Space/Enter skips it after a moment; the settings screen pauses it.
    /// </summary>
    public class FatalitySequence : MonoBehaviour
    {
        const float SkipAfter = 1.2f;
        const float ZoomSize = 4.8f;

        public bool Running { get; private set; }

        Camera cam;
        Vector3 camPos;
        float camSize;
        bool camSaved;
        GameObject rocket;
        Transform loser;
        Vector3 loserScale;

        Canvas overlay;
        Image dim;
        Image flash;
        UText bigText;
        UText smallText;
        float started;
        bool skip;

        public IEnumerator Run(PlayerId winner)
        {
            var ctx = MatchContext.Instance;
            PlayerId loserId = winner.Opponent();
            loser = ctx != null ? ctx.Player(loserId) : null;
            var island = ctx != null ? ctx.Island(loserId) as IslandHealth : null;
            Vector3 target = island != null ? island.transform.position : (loser != null ? loser.position : Vector3.zero);

            Running = true;
            skip = false;
            started = Time.unscaledTime;
            BuildOverlay();
            overlay.gameObject.SetActive(true);
            SetAlpha(dim, 0f);
            SetAlpha(flash, 0f);
            bigText.text = "";
            smallText.text = "";

            cam = Camera.main;
            CameraShake.StopAll();
            if (cam != null)
            {
                camPos = cam.transform.position;
                camSize = cam.orthographicSize;
                camSaved = true;
            }
            Vector3 focusPos = FocusPosition(target + Vector3.up * 1.2f);

            // 1. Lights down, camera closes in on the loser's island.
            yield return Tween(1f, k =>
            {
                float e = Mathf.SmoothStep(0f, 1f, k);
                SetAlpha(dim, 0.5f * e);
                if (cam == null) return;
                cam.orthographicSize = Mathf.Lerp(camSize, ZoomSize, e);
                cam.transform.position = Vector3.Lerp(camPos, focusPos, e);
            });

            // 2. The loser panics.
            Vector3 loserStart = loser != null ? loser.position : target;
            if (loser != null)
            {
                loserScale = loser.localScale;
                RobotFloatingText.Spawn("!!", loserStart + Vector3.up * 2.6f, Palette.Danger, 0.1f, 0.8f, 0.3f);
            }
            yield return Tween(0.5f, k =>
            {
                if (loser == null) return;
                float s = Mathf.Sin(k * 60f) * 0.08f;
                loser.position = loserStart + new Vector3(s, 0f, 0f);
            });

            // 3. A giant golden rocket drops from the sky.
            Vector3 skyStart = target + Vector3.up * 16f;
            Vector3 ground = target + Vector3.up * 0.6f;
            rocket = SpawnRocket(ctx, skyStart);
            Play(GameSfx.Launch(5), 0.9f);
            yield return Tween(0.8f, k =>
            {
                if (rocket != null) rocket.transform.position = Vector3.Lerp(skyStart, ground, k * k);
            });

            // 4. Impact: flash, fire, shake, and the loser flies away.
            if (rocket != null) Destroy(rocket);
            Impact(ground);
            ShowBanner(loserId);
            Vector3 flyEnd = loserStart + new Vector3(loserId == PlayerId.P1 ? -6f : 6f, 15f, 9f);
            yield return Tween(1.6f, k =>
            {
                SetAlpha(flash, Mathf.Clamp01(1f - k * 4f) * 0.9f);
                float bigK = Mathf.Clamp01(k * 5f);
                bigText.rectTransform.localScale = Vector3.one * Mathf.Lerp(2.6f, 1f, GameplayUtil.EaseOutBack(bigK));
                if (loser == null) return;
                loser.position = Vector3.Lerp(loserStart, flyEnd, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 3f);
                loser.rotation = Quaternion.AngleAxis(1080f * k, new Vector3(1f, 1f, 0.3f).normalized);
                loser.localScale = loserScale * Mathf.Lerp(1f, 0.15f, k);
            });

            // 5. Gone with a twinkle.
            if (loser != null)
            {
                loser.localScale = Vector3.zero;
                RobotFx.Burst("Twinkle", flyEnd, RobotFx.DotMaterial, 24, Color.white, Palette.HelmetYellow, 1f, 3f, 0.12f, 0.3f, 0.4f, 0.8f, 0f);
                RobotFx.Burst("TwinkleRing", flyEnd, RobotFx.RingMaterial, 6, Color.white, Palette.HelmetYellow, 0.5f, 1.2f, 0.4f, 0.7f, 0.4f, 0.6f, 0f);
            }
            Play(GameSfx.ReadyChime, 0.8f);
            yield return Tween(1.5f, k => { });

            // 6. Fade back for the result screen.
            yield return Tween(0.4f, k =>
            {
                SetAlpha(dim, 0.5f * (1f - k));
                var c = bigText.color;
                bigText.color = new Color(c.r, c.g, c.b, 1f - k);
                smallText.color = new Color(1f, 1f, 1f, 1f - k);
            });
            Finish();
        }

        /// <summary>Stops the sequence's leftovers and puts the camera back (match reset).</summary>
        public void Abort()
        {
            if (!Running) return;
            if (rocket != null) Destroy(rocket);
            Finish();
        }

        void Finish()
        {
            if (overlay != null) overlay.gameObject.SetActive(false);
            if (camSaved && cam != null)
            {
                CameraShake.StopAll();
                cam.transform.position = camPos;
                cam.orthographicSize = camSize;
            }
            camSaved = false;
            Running = false;
        }

        void Update()
        {
            if (!Running || skip || Time.unscaledTime - started < SkipAfter) return;
            var kb = Keyboard.current;
            if (kb != null && !GameSettings.Paused && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                skip = true;
        }

        IEnumerator Tween(float duration, System.Action<float> step)
        {
            float t = 0f;
            while (t < duration && !skip)
            {
                if (!GameSettings.Paused) t += Time.unscaledDeltaTime;
                step(Mathf.Clamp01(t / duration));
                yield return null;
            }
            step(1f);
        }

        /// <summary>Camera position that puts <paramref name="point"/> where the camera's center ray met the ground.</summary>
        Vector3 FocusPosition(Vector3 point)
        {
            if (cam == null) return Vector3.zero;
            Vector3 f = cam.transform.forward;
            if (Mathf.Abs(f.y) < 0.01f) return camPos;
            Vector3 hit = camPos + f * ((point.y - camPos.y) / f.y);
            return camPos + (point - hit);
        }

        GameObject SpawnRocket(MatchContext ctx, Vector3 at)
        {
            GameObject prefab = null;
            if (ctx != null && ctx.recipes != null && ctx.recipes.Length > 0)
            {
                var biggest = ctx.recipes[ctx.recipes.Length - 1];
                if (biggest != null) prefab = biggest.flightPrefab;
            }
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, at, Quaternion.Euler(180f, 0f, 0f));
                foreach (var col in go.GetComponentsInChildren<Collider>()) Destroy(col);
                foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>()) Destroy(mb);
                go.transform.localScale *= 4.2f;
            }
            else
            {
                go = GameplayUtil.CreatePrimitive(PrimitiveType.Capsule, "FatalityRocket");
                go.transform.position = at;
                go.transform.localScale = new Vector3(1.5f, 3f, 1.5f);
                GameplayUtil.Tint(go, Palette.Tier(PartTier.Gold));
            }
            go.name = "FatalityRocket";
            return go;
        }

        void Impact(Vector3 at)
        {
            RobotFx.Burst("FatalityFire", at, RobotFx.DotMaterial, 140, Palette.HelmetYellow, Palette.Danger, 4f, 11f, 0.3f, 0.8f, 0.6f, 1.3f, 0.4f);
            RobotFx.Burst("FatalitySmoke", at + Vector3.up, RobotFx.DotMaterial, 60, Palette.Tier(PartTier.Scrap), Palette.Cream, 2f, 5f, 0.6f, 1.2f, 1f, 1.8f, -0.1f);
            RobotFx.Burst("FatalityRing", at, RobotFx.RingMaterial, 14, Palette.Cream, Palette.HelmetOrange, 3f, 6f, 0.8f, 1.4f, 0.5f, 0.8f, 0f);
            CameraShake.Shake(0.9f, 0.9f);
            Play(GameSfx.Explosion(3), 1f);
            Play(GameSfx.Fatality, 0.9f, 0.15f);
        }

        void ShowBanner(PlayerId loserId)
        {
            bigText.text = "FATALITY!";
            bigText.color = Palette.Danger;
            smallText.text = UIText.PlayerThai(loserId) + " ปลิวไปสุดขอบฟ้า!";
            smallText.color = Color.white;
            UITween.Punch(smallText.rectTransform, 0.3f, 0.4f);
        }

        static void Play(AudioClip clip, float volume, float delay = 0f)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.Play(clip, volume, 0f, 0.01f, delay, minInterval: 0f);
        }

        static void SetAlpha(Graphic g, float a)
        {
            var c = g.color;
            g.color = new Color(c.r, c.g, c.b, a);
        }

        void BuildOverlay()
        {
            if (overlay != null) return;
            var go = new GameObject("FatalityOverlay", typeof(RectTransform));
            go.layer = UIFactory.UILayer;
            overlay = go.AddComponent<Canvas>();
            overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            overlay.sortingOrder = 45; // under the UI canvas, so settings stay on top
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var rt = (RectTransform)go.transform;

            dim = UIFactory.StretchImg(rt, "Dim", null, Palette.Ink);
            flash = UIFactory.StretchImg(rt, "Flash", null, Color.white);
            bigText = UIFactory.Txt(rt, "Fatality", "", 200, Palette.Danger, TextAnchor.MiddleCenter, UIFactory.Center,
                new Vector2(0f, 300f), new Vector2(1800f, 260f), true);
            UIFactory.Chunky(bigText, 9f, 14f);
            smallText = UIFactory.Txt(rt, "Subtitle", "", 54, Color.white, TextAnchor.MiddleCenter, UIFactory.Center,
                new Vector2(0f, 150f), new Vector2(1600f, 80f), true);
            UIFactory.AddOutline(smallText, Palette.Ink, 4f);
            go.SetActive(false);
        }
    }
}
