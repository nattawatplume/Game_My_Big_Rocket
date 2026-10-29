# Module Contracts

Shared rules for everyone writing code in this project. The foundation lives in
`Assets/_Project/Scripts/Core` and `Assets/_Project/Scripts/Input` — read it first, don't change it
without coordinating (add new things instead).

## General
- Namespace `IslandRocketRumble` (editor code: `IslandRocketRumble.EditorTools`).
- Unity 6000.0 · URP 17 · Input System only (read keys through `PlayerInput2P`, never `UnityEngine.Input`).
- All tunable numbers come from `GameConfig` (via `MatchContext.Instance.config`). All colors from `Palette`.
- Modules talk through `MatchContext` (registry) and `GameEvents` (event bus) — no direct references
  between modules except the interfaces in `GameTypes.cs`.
- No `.meta` files by hand, no new packages, no files outside your assigned folders.

## World layout (units = meters, Y up)
| Thing | Position | Size |
|---|---|---|
| P1 island top center | (-7, 0, -2) | 8 × 8 top, side height 2.5 |
| P2 island top center | (7, 0, -2) | 8 × 8 |
| Robot island top center | (0, 0.8, 5) | 5 × 5 |
| Sea plane | y = -2.6 | large |
| Camera | orthographic, isometric: rotation (35, 45, 0) relative feel, framing all three islands |

Island top surface is at the island root's y. Players walk on the top, clamped to ±3.6 from center.
Each player island has a craft ring at local (0, 0, -1.8), radius 1.3.

## Art prefabs (built by `IRR/Art/Generate All` → `Assets/_Project/Prefabs/...`)
All meshes are low-poly, pivots at the bottom center, forward = +Z, using material `IRR_Toon`.
Tintable meshes expose `_BaseColor` (set via `MaterialPropertyBlock`).

| Prefab path | Notes / named children |
|---|---|
| `Characters/Pip.prefab`, `Characters/Momo.prefab` | ~1.0 tall. Children: `Visual` (scale this for squash/stretch), `Visual/Body`, `Visual/Helmet`, `Visual/EyeL`, `Visual/EyeR`, `Visual/Mouth`, `HeadAnchor` (empty, top of helmet — craft menu & stun stars attach here) |
| `Robot/Cpe40.prefab` | ~1.6 tall bulldozer. Children: `Visual`, `Visual/Body`, `Visual/Head`, `Visual/EyeL`, `Visual/EyeR`, `Visual/Bucket` (front scoop, can be rotated for throw), `FireRoot` (empty on top of head), `ThrowPoint` (empty above bucket) |
| `Islands/PlayerIsland.prefab` | top at y=0. Children: `Top`, `Side`, `Props` (trees, house, crabs as children), `CraftRing` (dashed ring at local (0,0.02,-1.8)) |
| `Islands/RobotIsland.prefab` | top at y=0, 5×5 |
| `Rockets/T1_Firecracker.prefab` … `Rockets/T5_MegaOverkill.prefab` | Nose points +Y (upright on the launch pad). Children: `Visual`, `Flame` (empty at tail) |
| `Parts/Gear.prefab`, `Nut`, `Fuel`, `Chip`, `Gem` | ~0.45 wide, white, tint with tier color |
| `Props/Tree`, `Props/House`, `Props/Crab`, `Props/Duck`, `Props/Seagull` | decoration |
| `FX/LightBeam.prefab` | tall translucent cylinder, tintable, height 1 (scale Y) |
| `FX/WarningRing.prefab` | flat ring, radius 1 (scale XZ), tintable |
| `FX/Crater.prefab` | flat dark scorch disc, radius 1 |
| `Environment/Sea.prefab` | large plane with gentle vertex waves (shader `IRR/Sea`) |

Shaders: `IRR/Toon` (stepped lighting + soft outline pass, `_BaseColor`, `_ShadeColor`, `_OutlineWidth`),
`IRR/Unlit Transparent` (for beams/rings, `_BaseColor` with alpha), `IRR/Sea`.

## Who owns what
| Module | Folders |
|---|---|
| Art | `Scripts/Editor/Art/`, `Scripts/Visual/`, `Art/` |
| Gameplay | `Scripts/Gameplay/` (GameManager, Player, Parts, Crafting, Rockets, Island) |
| Robot | `Scripts/Robot/` |
| UI | `Scripts/UI/`, `Resources/UI/` |
| Integration | `Scripts/Editor/SceneBuilder.cs`, `Scenes/` |
