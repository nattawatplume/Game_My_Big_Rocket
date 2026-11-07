using System.Collections;
using System.IO;
using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// Store-page screenshots. Run the game with "-irrShowcase": it plays a scripted tour (title, how to play,
    /// customize, battle, upgrade cards, a rocket in flight, the fatality, the result), saves PNGs into a
    /// "Showcase" folder next to the game, then quits. Does nothing without the flag.
    /// </summary>
    public class ShowcaseCapture : MonoBehaviour
    {
        string folder;
        int shot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-irrShowcase") < 0) return;
            new GameObject("ShowcaseCapture").AddComponent<ShowcaseCapture>();
        }

        IEnumerator Start()
        {
            folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Showcase");
            Directory.CreateDirectory(folder);
            yield return new WaitForSecondsRealtime(4f);
            var ctx = MatchContext.Instance;
            var ui = FindAnyObjectByType<UIRoot>();

            yield return Shot("title");

            GameEvents.RaiseRequestHowToPlay();
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("how_to_play");

            GameEvents.RaiseRequestTitle();
            yield return new WaitForSecondsRealtime(0.8f);
            if (ui != null) ui.OpenCustomize();
            yield return new WaitForSecondsRealtime(2.5f);
            yield return Shot("customize");
            if (ui != null) ui.CloseCustomize();

            GameEvents.RaiseRequestStartMatch();
            yield return new WaitForSecondsRealtime(5f);
            GiveParts(ctx);
            if (ctx.Robot != null)
                for (int i = 0; i < 3; i++) ctx.Robot.TakeRocketHit(new RocketHit { shooter = PlayerId.P1, recipe = ctx.recipes[3] });
            foreach (var s in FindObjectsByType<PowerUpSpawner>(FindObjectsSortMode.None)) s.SpawnNow();
            yield return new WaitForSecondsRealtime(4f);
            yield return Shot("battle");

            var menu = ctx.CraftMenu(PlayerId.P1);
            menu.Open();
            menu.BeginPicking(UpgradeCatalog.Deal(ctx.Upgrades(PlayerId.P1)));
            ctx.CraftMenu(PlayerId.P2).Open();
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("upgrade_cards");
            menu.EndPicking();
            menu.Close();
            ctx.CraftMenu(PlayerId.P2).Close();

            foreach (var zone in FindObjectsByType<CraftZone>(FindObjectsSortMode.None))
            {
                if (zone.owner != PlayerId.P1) continue;
                var target = ctx.Island(PlayerId.P2);
                RocketLauncher.Launch(PlayerId.P1, ctx.recipes[4], TargetKind.Opponent, target, zone.LaunchPad, zone.LaunchPad + Vector3.right * 14f);
            }
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("rocket");
            yield return new WaitForSecondsRealtime(2f);

            if (ctx.Island(PlayerId.P2) is IslandHealth island)
                for (int i = 0; i < 20 && island.Hp > 0; i++)
                    island.TakeRocketHit(new RocketHit { shooter = PlayerId.P1, recipe = ctx.recipes[4], point = island.transform.position });
            yield return new WaitForSecondsRealtime(3f);
            yield return Shot("fatality");
            yield return new WaitForSecondsRealtime(5f);
            yield return Shot("result");

            Application.Quit();
        }

        static void GiveParts(MatchContext ctx)
        {
            foreach (var recipe in ctx.recipes)
            {
                if (recipe == null) continue;
                foreach (var cost in recipe.costs)
                {
                    ctx.Inventory(PlayerId.P1).Add(cost.Key, cost.count);
                    ctx.Inventory(PlayerId.P2).Add(cost.Key, cost.count);
                }
            }
        }

        IEnumerator Shot(string name)
        {
            shot++;
            yield return new WaitForEndOfFrame();
            var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(folder, shot.ToString("00") + "_" + name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
            yield return new WaitForSecondsRealtime(0.3f);
        }
    }
}
