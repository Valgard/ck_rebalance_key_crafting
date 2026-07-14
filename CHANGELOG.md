# Changelog

All notable changes to this mod are documented here. The publish pipeline
reads the topmost `## [x.y.z]` entry as the version to publish.

## [1.1.1] - 2026-07-14

### Fixed
- The in-game crafting-cost setting (including **1 ingot**) was ignored at
  database bake time and silently fell back to the 1/4 default, so only the
  cheapest key (`CopperKey`) reached 1 ingredient while the higher tier keys
  stayed at 2+. The settings are now bound in `EarlyInit` (before the recipe
  bake in `PostConvert`) instead of `Init` (after it), so the chosen cost
  applies to all affected keys. Takes effect on the next game restart.

## [1.1.0]

- **In-game settings** under Options -> Mod Settings (via the Mod Settings Menu
  framework): choose the crafting cost (1 ingot / 1/4 / 1/2 / vanilla), which keys
  are affected (tier keys or all craftable keys), and toggle the mod. Changes apply
  on the next game restart - the menu offers to restart for you.
- Now requires the **Mod Settings Menu** mod and CoreLib.

## [1.0.0] - 2026-06-28

### Added
- Reduce the material cost of crafting keys at database bake time. Default:
  the seven tier chest keys (`CopperKey`..`ReluciteKey`) at 25% of vanilla
  cost, with a floor of 1 per ingredient. The reduction is written into the
  recipe before it is baked into the runtime blob, so the workbench UI and the
  actual consumption both show the reduced cost.
- `ModConfig.scope` switch to widen coverage from the tier keys only
  (default) to all craftable keys (any object whose id name ends in "Key" with
  a non-empty recipe).
