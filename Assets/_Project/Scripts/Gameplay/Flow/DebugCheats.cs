using UnityEngine;
using UnityEngine.InputSystem;

namespace IslandRocketRumble
{
    /// <summary>
    /// Editor / development-build shortcuts for play-testing.
    /// F1: give both players enough parts for every rocket · F2: skip ahead one tier · F3: last 5 seconds.
    /// </summary>
    public class DebugCheats : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            var go = new GameObject("DebugCheats");
            go.hideFlags = HideFlags.DontSave;
            go.AddComponent<DebugCheats>();
        }

        void Update()
        {
            var kb = Keyboard.current;
            var ctx = MatchContext.Instance;
            if (kb == null || ctx == null) return;

            if (kb.f1Key.wasPressedThisFrame)
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
                Debug.Log("[IRR cheat] Gave parts for every rocket to both players.");
            }

            if (kb.f2Key.wasPressedThisFrame && ctx.config != null)
            {
                ctx.Elapsed = Mathf.Min(ctx.Elapsed + ctx.config.tierInterval, ctx.config.matchDuration - 1f);
                Debug.Log("[IRR cheat] Skipped to " + ctx.CurrentTier);
            }

            if (kb.f3Key.wasPressedThisFrame && ctx.config != null && ctx.Phase == GamePhase.Playing)
            {
                ctx.Elapsed = ctx.config.matchDuration - 5f;
                Debug.Log("[IRR cheat] Jumped to the last 5 seconds.");
            }
        }
#endif
    }
}
