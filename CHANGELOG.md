# Changelog

All notable changes to this mod are documented here. The publish pipeline
reads the topmost `## [x.y.z]` entry as the version to publish.

## [0.9.0] - 2026-06-28

### Added
- Reduce the material cost of crafting keys at database bake time. Default:
  the seven tier chest keys (`CopperKey`..`ReluciteKey`) at 25% of vanilla
  cost, with a floor of 1 per ingredient. The reduction is written into the
  recipe before it is baked into the runtime blob, so the workbench UI and the
  actual consumption both show the reduced cost.
- `ModConfig.scope` switch to widen coverage from the tier keys only
  (default) to all craftable keys (any object whose id name ends in "Key" with
  a non-empty recipe).
