using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>Spawns a rocket at a launch pad and sends it to a target.</summary>
    public static class RocketLauncher
    {
        /// <summary>
        /// Launches <paramref name="recipe"/> from <paramref name="launchPoint"/> at <paramref name="target"/>.
        /// Raises GameEvents.RocketLaunched immediately. <paramref name="target"/> may be null (rocket lands on <paramref name="fallbackPoint"/>).
        /// </summary>
        public static RocketFlight Launch(PlayerId shooter, RocketRecipe recipe, TargetKind kind, IRocketTarget target,
            Vector3 launchPoint, Vector3 fallbackPoint)
        {
            if (recipe == null) return null;

            GameObject go;
            if (recipe.flightPrefab != null)
            {
                go = Object.Instantiate(recipe.flightPrefab, launchPoint, Quaternion.identity);
                go.name = "Rocket_" + recipe.displayName;
            }
            else
            {
                // Placeholder: capsule child lifted so the root pivot is at the tail, nose +Y (same as the art prefabs).
                float tierScale = 0.8f + 0.15f * ((int)recipe.tier - 1);
                go = new GameObject("Rocket_" + recipe.displayName);
                go.transform.position = launchPoint;
                var body = GameplayUtil.CreatePrimitive(PrimitiveType.Capsule, "Visual");
                body.transform.SetParent(go.transform, false);
                Vector3 s = new Vector3(0.3f, 0.45f, 0.3f) * tierScale;
                body.transform.localScale = s;
                body.transform.localPosition = new Vector3(0f, s.y, 0f); // capsule is 2 units tall
                GameplayUtil.Tint(body, Palette.Tier(recipe.tier));
            }

            var flight = go.AddComponent<RocketFlight>();
            flight.Init(shooter, recipe, kind, target, launchPoint, fallbackPoint);

            GameEvents.RaiseRocketLaunched(shooter, recipe, kind);
            return flight;
        }
    }
}
