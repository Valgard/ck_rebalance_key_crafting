# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with
code in this repository.

## What this repo is

A Core Keeper mod that **reduces the material cost of crafting keys** by
rewriting their recipe ingredient amounts at database bake time. By default it
affects the seven tier chest keys (`CopperKey`..`ReluciteKey`) at 25% of
vanilla cost (floor 1 per ingredient). One Harmony prefix against Pugstorm's
`CoreKeeperModSDK`. No content of its own; hard-depends on CoreLib + Mod Settings
Menu, which drive the four knobs as live in-game settings. Personal-use,
non-commercial (Pugstorm EULA).

The parent `../CLAUDE.md` holds the mod-agnostic SDK/CrossOver guidance shared
with the sibling mods.

## Build and deploy

```bash
source .envrc           # or, from a worktree: source ../../../.envrc && source .envrc
../utils/build.sh       # Unity batchmode build; on Darwin auto-runs install-macos.sh
                        # from a worktree: ../../../utils/build.sh
```

Unity Editor must be closed (it locks the project). `utils/link.sh` symlinks
the repo's `unity/` mirror into `$SDK_PATH/Assets/`: one **directory** symlink
for `unity/RebalanceKeyCrafting/`, plus three file symlinks for the
Assets-level files beside it (`RebalanceKeyCrafting.asset`, `.asset.meta`,
`.meta`). `build.sh` invokes it idempotently on every run, so worktree switches
and repo moves self-heal.

**Concurrent-build / shared-SDK caveat:** all sibling mods share one
`CoreKeeperModSDK` clone with a single `UnityLockfile`. If another session is
building, wait for the lock to release — do not kill it. Two cold-cache builds
running at once can exhaust memory and get one SIGKILLed by the OS (`Killed:
9`); that is a host memory-pressure issue, not a mod error.

No automated tests — verification is a manual in-game check: open a tier-key
recipe at its workbench, confirm the ingredient cost is a quarter of vanilla
(min 1), craft one and confirm exactly that amount is consumed, then reload the
world and confirm the cost does not drop further (idempotency).

## Architecture

Three runtime classes in the `RebalanceKeyCrafting` namespace, plus the shared
editor helpers symlinked in from `../utils/`:

- **`RebalanceKeyCraftingMod` (`IMod`)** — bootstrap. `EarlyInit()` registers the Mod
  Settings Menu section (Toggle `enabled`, Choice `reductionFactor`
  {OneIngot/Quarter/Half/Vanilla}, Choice `scope`), binds the handles into `ModConfig`,
  and logs the resolved config. Binding is in `EarlyInit`, **not `Init`**, because Core
  Keeper runs the database bake (`PugDatabasePostConverter.PostConvert`) *after* `EarlyInit`
  but *before* `Init` — binding in `Init` lets the bake read the handles before they exist,
  falling back to the hardcoded defaults (the original bug: cost stuck at the `Quarter`
  default, `enabled` off ignored). **No `BurstDisabler`** — the patch target is managed.
- **`KeyRecipeCostPatch` (`[HarmonyPatch]`)** — a `Prefix` on
  `PugDatabasePostConverter.PostConvert`. Walks the prefab list
  (`DatabaseConversionUtility.GetPrefabList`), selects target keys, and scales
  each `ObjectInfo.requiredObjectsToCraft` amount by `reductionFactor`
  (`max(minPerIngredient, round(amount * factor, AwayFromZero))`) **before**
  vanilla bakes the recipe into the immutable runtime blob. Always returns
  `true` (lets vanilla run).
- **`ModConfig`** — the settings adapter. `enabled` (Toggle, default on),
  `reductionFactor` (Choice of a `Reduction` enum: `OneIngot` → factor 0, `Quarter`
  → 0.25 [default], `Half` → 0.5, `Vanilla` → 1) and `scope` (Choice
  `TierKeysOnly` / `AllCraftableKeys`) read from bound `SettingHandle`s
  (`ModConfig.Bind`); the getters are source-compatible with the former fields, so
  `KeyRecipeCostPatch` reads `ModConfig.Instance.*` unchanged. `minPerIngredient` is
  **no longer a menu setting** — it is hardcoded to `1` (the floor that makes the
  `OneIngot` choice resolve to exactly 1 per ingredient, not 0/free). `tierKeyIds`
  (the 7 tier keys) stays a hardcoded data list. The framework persists the values (via CoreLib) to
  `mods/RebalanceKeyCrafting/config.cfg`; the mod's own code still touches no
  `System.IO`. **Bake-time caveat:** the recipe rewrite runs once per world in
  `PostConvert` (which fires after `EarlyInit`, before `Init` — hence the `EarlyInit`
  binding above) and is idempotent, so the getters feed that bake — a setting change
  takes effect on the **next game restart**, not mid-session (the section hint says
  so). Labels/hint/option strings live in `localization/localization.yaml`
  (EN/DE), generated into TextDataBlock assets at build.

### Why bake-time recipe rewrite

`PugDatabasePostConverter.PostConvert` copies each recipe amount straight from
the mutable `ObjectInfo.requiredObjectsToCraft` list into an immutable
`BlobArray<ObjectWithAmount>`:

```
Pug.Other.decompiled.cs:3547
blobBuilderArray2[j].amount = objectInfo2.requiredObjectsToCraft[j].amount;
```

`PostConvert` is **managed, not Burst** (it manipulates `List<>`, `GameObject`,
`BlobBuilder`, `EntityManager`), so it is Harmony-patchable with no
`BurstDisabler`. Scaling the list in a prefix makes vanilla bake the reduced
values, so the same blob backs both the UI cost display and the runtime
consumption — WYSIWYG, no per-craft overhead, no Burst patch. The runtime
crafting path itself (`InventoryUpdateSystem` → `ProcessCraftingJob` →
`InventoryUtility.Craft`) is double Burst-compiled and was deliberately not
chosen.

### Idempotency

`PostConvert` re-fires once per world conversion, but the `ObjectInfo`
instances persist on the prefabs across world loads. Without a guard the cost
would drop every load. `KeyRecipeCostPatch` keeps a `static
HashSet<ObjectInfo> _processed` and reduces each recipe exactly once, ever.

### The `CraftingObject` ambiguity

Two global `CraftingObject` types exist — a class in `Pug.Base`
(`objectID`/`amount`) and a struct in `Pug.ECS.Authoring`
(`objectName`/`amount`) — and the runtime asmdef references both DLLs. Naming
the type would raise `CS0104`. The patch therefore iterates ingredients with
`var` and keys its idempotency guard on `HashSet<ObjectInfo>`, never naming
`CraftingObject`.

`unity/` is the canonical source — a 1:1 mirror of the SDK's `Assets/` tree
holding every file the Editor generates for the mod: the `.cs` sources, both
`.asmdef` files, the ModBuilderSettings `.asset`, and all `.meta` GUID carriers.

## How this mod was scaffolded

Scaffolded by **copying the `disable-durability` sibling** (the closest pure
Harmony-patch mod) and regenerating identity, rather than running the SDK
"Create New Mod" wizard:

- All 14 own `.meta` GUIDs were remapped to fresh values (consistently, so the
  internal `.asset` ↔ `_modio.modSettings` cross-reference stayed intact).
- `metadata.guid` in the ModBuilderSettings `.asset` was regenerated (a
  duplicate would trigger the loader's "Data block loader already added").
- The three external SDK script GUIDs (ModBuilderSettings / Data / ModIO) were
  left untouched.
- `modId` was reset to 0; identity renamed to `RebalanceKeyCrafting` /
  "Rebalance Key Crafting"; `requiredOn` kept at `3` (ClientAndServer).
- The runtime `.asmdef` was inherited unchanged — its reference set already
  covers `Pug.Other`, `Pug.Base`, `Pug.ECS.Authoring`, `0Harmony`, etc.

**First build of a newly-symlinked mod:** clear the SDK's
`Library/SourceAssetDB` (plus `Bee`/`ScriptAssemblies`) before the first build,
or `ModBuilder.BuildMod` emits `files: []`. Keeping `Library/Artifacts` and
`Library/ArtifactDB` avoids a slow full shader/DOTS reimport.

## macOS / CrossOver

Deployed through the fake-mod.io workaround (see parent `../CLAUDE.md`). This
mod's fake mod.io ID is **`9999992`** (siblings use 9999993..9999999; they must
differ). Do not open the in-game Mods menu while a fake-ID install is active;
re-run `../utils/build.sh` to restore if the cache is wiped.

## Publishing to mod.io

`../utils/upload.sh` publishes this mod via the shared
`CoreKeeperModUtils.CLIPublishHelper.Publish` Editor class. The version comes
from the topmost `## [x.y.z]` entry of `CHANGELOG.md`; the profile logo is
`unity/RebalanceKeyCrafting/Editor/logo.png` (add before first publish); the
real mod ID lands in
`unity/RebalanceKeyCrafting/Editor/RebalanceKeyCrafting_modio.asset`. Set the
mod.io profile type tag to **`Script`** (an `Asset` tag silently disables the
mod's scripts).

## Conventions

- Commit messages: Conventional Commits (`type(scope): subject`), imperative,
  no emoji.
- Documentation files (`CLAUDE.md`, `README.md`, `docs/`) are English; chat
  answers are German.
- Prefer `git commit --amend` / `git reset --soft` over fix-up commits on a
  personal branch, and `git rebase` over `git merge`.
